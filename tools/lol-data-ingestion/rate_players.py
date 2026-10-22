"""Research player ratings, 2023-2026 (model v2): every PandaScore catalogue player gets one record (offline).

Reads the staging database (pf_* performance tables from build_performance.py, Leaguepedia tournament results and
birthdates, PandaScore catalogue and registrations), content/fixture.json (role weights and LogitScale, read-only) and
the local image mappings. Writes only into data/ratings/ (git-ignored) and, with --write-db, rt_* tables in the staging
database.

How a record is built (details: reports/PLAYER_RATING_MODEL.md):
  1. linear Level   the candidate contribution model C1 on 2023-2026 games (time-decayed), 70 = tier-a domestic mean;
  2. raw quality R  Level + achievement points (placements 2023-2026 in events the player played, saturating at +4) +
                    consistency points (steadiness of game-to-game performance, +-1.5);
  3. Overall        the banded scale: R through a fixed monotone transform onto ELITE 90-93, VERY_GOOD 83-89,
                    AVERAGE 74-82, INEXPERIENCED 55-73, AMATEUR_ACADEMY 30-54 (no quotas). ELITE needs major
                    achievements (an international final, or two tier-a domestic titles, as a regular) and steady
                    play, otherwise the Overall stops at 89; a PRIOR_ONLY placeholder stops at 73;
  4. age            from PandaScore roster birthdays, else Leaguepedia birthdates of the matched identity. From 29 the
                    Overall falls at most 2 points a year (3 from 31) below the rating one year earlier;
  5. potential      age 25 or younger: the Overall projected to age 25 with the data-fitted development curve (upper
                    80% growth), at most 93; older: equal to the Overall (no growth through training).
Skills are the Overall's level plus the player's profile; the Overall is recomputed from the five rounded skills with
the game's role weights and round-half-even.

Evidence classes (one per catalogue player):
  MEASURED_ESTIMATE  >= MEASURED_GAMES verified games (2023-2026) and >= MEASURED_EFFECTIVE decay-weighted games;
  LIMITED_EVIDENCE   at least one verified game, fewer than that: observations plus the model's priors, wide interval;
  PRIOR_ONLY         no verified game: a provisional placeholder from the 25th percentile R of rated players in the tier
                     of the player's most recent PandaScore stage registration (or the declared global prior), at most
                     73. All five skills equal; it is not a claim about the person's ability.
A verified game is a complete, primary (deduplicated) game whose seat belongs to a HIGH_CONFIDENCE_MATCH identity.

  python rate_players.py                      # fit, rate, rank, write data/ratings/*
  python rate_players.py --as-of 2026-10-14   # games dated before this day only
"""
import argparse
from collections import Counter, defaultdict
import csv
from datetime import date, datetime, timedelta, timezone
import hashlib
import json
import math
from pathlib import Path
import sqlite3
import sys

import numpy as np
from scipy import sparse

import rating_model as rm

TOOL_DIR = Path(__file__).resolve().parent
REPO = TOOL_DIR.parents[1]
DATA_DIR = TOOL_DIR / "data"
OUT_DIR = DATA_DIR / "ratings"
ASSETS = DATA_DIR / "assets"
CONTENT = REPO / "content" / "fixture.json"
AS_OF = date(2026, 10, 14)
WINDOW_START = date(2023, 1, 1)
YEARS = (2023, 2024, 2025, 2026)
OUTPUT_STEM = "player_ratings_2023_2026"
TIERS = ("s", "a", "b", "c", "d", "unknown")
PS_ROLE = {"top": "top", "jun": "jng", "mid": "mid", "adc": "bot", "sup": "sup"}
ROLE_LABEL = {"top": "Top", "jng": "Jungle", "mid": "Mid", "bot": "Bot", "sup": "Support"}
MEASURED_GAMES, MEASURED_EFFECTIVE = 40, 20.0
# Ranking eligibility (declared): verified 2026 games in the ranked role, recency, identity, uncertainty.
RANK_MIN_GAMES_2026 = 20
RANK_MAX_DAYS_SINCE_LAST = 180
# The data must have narrowed the prior uncertainty of the Level (5 * k * tau) by at least a fifth.
RANK_SD_SHARE = 0.8
# Hyperparameters of the candidate model C1 (gamma, tau, half_life FITTED by temporal validation on the 2023-2026 data,
# 2026-10-14: best inner log loss 0.6155 at gamma 0.2, tau 0.3, half-life 365; 730 days 0.6177, 1095 days 0.6190).
CONFIG = {"gamma": 0.2, "tau": 0.3, "sigma_c": rm.COMPETITION_SD, "half_life": 365.0,
          "profile_scale": rm.PROFILE_SCALE, "form_points_per_sd": rm.FORM_POINTS_PER_SD}
# Statistics a seat carries only until its feature tuple is built (memory: four seasons of seats).
RAW_KEYS = ("kills", "deaths", "assists", "team_kills", "team_deaths", "gold", "earned_gold", "team_gold", "cs", "damage",
            "team_damage", "vision_score", "wards_placed", "wards_killed", "control_wards", "gd10", "gd15", "xpd10", "xpd15",
            "csd10", "csd15", "kills15", "assists15", "deaths15", "first_blood", "opp_gold", "opp_cs", "opp_damage",
            "game_length_min", "game_date", "row_key", "identity_status")

# AUTHORED placement points by event class (place 1, 2, 3-4, 5-8). Only the final stage of a split or event awards
# them in full; regular seasons, earlier playoff stages and qualifiers award REGULAR_STAGE_SHARE of them and no title.
PLACEMENT_POINTS = {
    "world_msi": (10.0, 7.0, 5.0, 3.0),
    "international": (6.0, 4.0, 2.5, 1.0),
    "a": (5.0, 3.0, 1.5, 0.5),
    "b": (2.5, 1.5, 0.75, 0.25),
    "c": (1.2, 0.7, 0.3, 0.0),
    "d": (0.5, 0.25, 0.0, 0.0),
    "unknown": (0.5, 0.25, 0.0, 0.0),
}
WORLD_MSI_LEAGUES = ("World Championship", "Mid-Season Invitational")
REGULAR_STAGE_SHARE = 0.3
ACHIEVEMENT_HALF_LIFE_YEARS = 2.0
REGULAR_SHARE, PARTIAL_SHARE = 0.5, 0.2  # share of the team's games in the event for full / half credit


# ---------- inputs ----------

def load_content(path=CONTENT):
    content = json.loads(path.read_text(encoding="utf-8"))
    raw = path.read_bytes()
    return {"weights": rm.role_weights(content), "points_per_logit": content["Balance"]["LogitScale"] / 100.0,
            "logit_scale": content["Balance"]["LogitScale"], "sha256": hashlib.sha256(raw).hexdigest(),
            "role_weights_raw": content["RoleWeights"]}


SEAT_SQL = """SELECT p.row_key, p.game_key, p.source, p.side, p.role, p.champion, p.win, p.team_key, p.team_name, p.identity_key,
                     p.identity_status, p.ps_player_id, p.kills, p.deaths, p.assists, p.team_kills, p.team_deaths, p.gold,
                     p.earned_gold, p.team_gold, p.cs, p.damage, p.team_damage, p.vision_score, p.wards_placed, p.wards_killed,
                     p.control_wards, p.gd10, p.gd15, p.xpd10, p.xpd15, p.csd10, p.csd15, p.kills15, p.assists15, p.deaths15,
                     p.first_blood, p.opp_gold, p.opp_cs, p.opp_damage,
                     g.game_date, g.year, g.game_length_min, g.league, g.region, g.ps_league_id, g.ps_tier, g.patch,
                     g.tournament, g.winner_side
              FROM pf_player_games p JOIN pf_games g USING (game_key)
              WHERE g.is_primary = 1 AND g.complete = 1 AND g.game_date >= ? AND g.game_date < ?"""


def load_seats(db, as_of=AS_OF, start=WINDOW_START):
    columns = [c.strip().split(".")[-1] for c in SEAT_SQL.split("SELECT", 1)[1].split("FROM")[0].replace("\n", " ").split(",")]
    competition_tier = {(s, l, y): t for s, l, y, t in db.execute("SELECT source, league, year, ps_tier FROM pf_competitions")}
    seats = []
    for row in db.execute(SEAT_SQL, (start.isoformat(), as_of.isoformat())):
        s = dict(zip(columns, row))
        if s["role"] not in rm.ROLES:
            continue
        s["date"] = date.fromisoformat(s["game_date"]).toordinal()
        s["f"] = rm.feature_tuple(s)
        s["has_wards"] = s["wards_placed"] is not None
        for key in RAW_KEYS:
            del s[key]
        s["competition"] = f"{s['source']}:{s['league']}"
        s["tier"] = s["ps_tier"] or competition_tier.get((s["source"], s["league"], s["year"])) or "unknown"
        s["model_id"] = f"ps:{s['ps_player_id']}" if s["ps_player_id"] else s["identity_key"]
        seats.append(s)
    seats.sort(key=lambda s: (s["date"], s["game_key"], s["side"], s["role"]))
    international = international_competitions(seats)
    for s in seats:
        s["international"] = s["competition"] in international or s["region"] == "International" or s["tier"] == "s"
    return seats


def international_competitions(seats, min_games=3, min_share=0.5):
    """Competitions in which most games oppose lineups from different home leagues. A player's home in a year is the
    competition they played most that year (so a league renamed between seasons stays one home); a lineup's home is its
    players' most common home. Domestic leagues oppose lineups of the same home; Worlds, the Asian Games (national
    teams) or EMEA Masters do not, whatever region a source files them under."""
    counts = defaultdict(Counter)
    for s in seats:
        counts[(s["model_id"], s["year"])][s["competition"]] += 1
    home = {key: c.most_common(1)[0][0] for key, c in counts.items()}
    games = defaultdict(lambda: {"Blue": [], "Red": [], "competition": None})
    for s in seats:
        g = games[(s["game_key"], s["year"])]
        g["competition"] = (s["competition"], s["year"])
        g[s["side"]].append(home[(s["model_id"], s["year"])])
    tally = defaultdict(lambda: [0, 0])
    for g in games.values():
        homes = [Counter(h for h in g[side] if h).most_common(1) for side in ("Blue", "Red")]
        if homes[0] and homes[1]:
            tally[g["competition"]][0] += homes[0][0][0] != homes[1][0][0]
            tally[g["competition"]][1] += 1
    flagged = {c for c, (cross, total) in tally.items() if total >= min_games and cross >= min_share * total}
    return {c for c, _ in flagged}


def has_table(db, name):
    return bool(db.execute("SELECT count(*) FROM sqlite_master WHERE name = ?", (name,)).fetchone()[0])


def parse_day(text):
    try:
        return date.fromisoformat((text or "")[:10])
    except ValueError:
        return None


def load_birthdates(db):
    """ps player id -> {"birthdate", "source", "conflict"}. PandaScore stage-roster birthdays first, else the
    Leaguepedia birthdate of the player's HIGH_CONFIDENCE_MATCH Leaguepedia identity. Two sources more than a year apart
    are flagged and the PandaScore value is kept. Personal data: used locally for age only."""
    ps = {}
    for (payload,) in db.execute("""SELECT DISTINCT p.payload_json FROM record_payloads p JOIN record_observations o
                                    USING (payload_sha256) WHERE o.collection = 'tournaments'"""):
        for entry in json.loads(payload).get("expected_roster") or []:
            for p in (entry or {}).get("players") or []:
                if isinstance(p, dict) and p.get("id") is not None and parse_day(p.get("birthday")):
                    ps[str(p["id"])] = parse_day(p["birthday"])
    lp = defaultdict(set)
    if has_table(db, "lp_player_birthdates"):
        pages = {page: parse_day(b) for page, b in db.execute("SELECT overview_page, birthdate FROM lp_player_birthdates")}
        for ps_id, key in db.execute("""SELECT DISTINCT ps_player_id, identity_key FROM pf_player_games
                                         WHERE ps_player_id IS NOT NULL AND identity_key LIKE 'lp:%'"""):
            if pages.get(key[3:]):
                lp[ps_id].add(pages[key[3:]])
    out = {}
    for pid in set(ps) | set(lp):
        mine, theirs = ps.get(pid), sorted(lp.get(pid, ()))
        chosen = mine or (theirs[0] if len(theirs) == 1 else None)
        conflict = bool(mine and theirs and any(abs((mine - t).days) > 366 for t in theirs)) or len(theirs) > 1 and not mine
        if chosen:
            out[pid] = {"birthdate": chosen, "source": "pandascore" if mine else "leaguepedia", "conflict": conflict}
    return out


def age_on(birthdate, day):
    if birthdate is None:
        return None
    return day.year - birthdate.year - ((day.month, day.day) < (birthdate.month, birthdate.day))


# ---------- preparation: features, z-scores, indices ----------

class Prepared:
    """Box-score side of the model for one set of seats (all of them, or a training window)."""

    def __init__(self, seats, as_of_ordinal, weights_bp, half_life):
        self.weights_bp = weights_bp
        self.normalizer = rm.Normalizer(seats, [s["f"] for s in seats])
        per_role_perf = defaultdict(list)
        for s in seats:
            z = self.normalizer.z(s, s["f"])
            s["scores"] = rm.seat_scores(s["role"], z)
            s["available"] = rm.attribute_availability(s["role"], set(z))
            w = weights_bp[s["role"]]
            mass = sum(w[:4])
            s["perf"] = sum(w[i] * s["scores"][a] for i, a in enumerate(rm.SEAT_ATTRIBUTES)) / mass
            s["weight"] = rm.decay(as_of_ordinal - s["date"], half_life)
            per_role_perf[s["role"]].append(s["perf"])
        self.perf_sd = {r: float(np.std(v)) or 1.0 for r, v in per_role_perf.items()}
        groups = defaultdict(lambda: defaultdict(lambda: ([], [])))
        self.by_player_role = defaultdict(list)
        for s in seats:
            self.by_player_role[(s["model_id"], s["role"])].append(s)
            for a in rm.SEAT_ATTRIBUTES:
                values, weights = groups[(s["role"], a)][(s["model_id"], s["role"])]
                values.append(s["scores"][a])
                weights.append(s["weight"])
        self.eb = {key: rm.eb_fit(players) for key, players in groups.items()}
        self.pool_curves = {}
        for role in rm.ROLES:
            samples = [(len(rows), rm.effective_champions(Counter(r["champion"] for r in rows)))
                       for (pid, r_), rows in self.by_player_role.items() if r_ == role and rows]
            self.pool_curves[role] = rm.fit_diversity_curve(samples)
        pool_raw = {}
        for (pid, role), rows in self.by_player_role.items():
            counts = Counter(r["champion"] for r in rows)
            main = {c for c, _ in counts.most_common(2)}
            on = [r["perf"] for r in rows if r["champion"] in main]
            off = [r["perf"] for r in rows if r["champion"] not in main]
            gap = (np.mean(off) - np.mean(on)) / self.perf_sd[role] if len(off) >= 3 and len(on) >= 3 else 0.0
            curve, sd = self.pool_curves[role]
            pool_raw[(pid, role)] = (rm.champion_pool_raw(counts, curve, sd, gap, len(off)), len(rows))
        self.pool_sd = {}
        for role in rm.ROLES:
            values = [v for (pid, r), (v, n) in pool_raw.items() if r == role and n >= 20]
            self.pool_sd[role] = float(np.std(values)) if len(values) > 10 else 1.0
        self.indices = {}
        for key, rows in self.by_player_role.items():
            role = key[1]
            out = {}
            for a in rm.SEAT_ATTRIBUTES:
                signal, noise = self.eb[(role, a)]
                values = [r["scores"][a] for r in rows]
                weights = [r["weight"] for r in rows]
                coverage = float(np.mean([r["available"][a] for r in rows]))
                index, n_eff, reliability = rm.eb_index(values, weights, signal, noise)
                out[a] = {"index": index, "n_eff": n_eff, "reliability": reliability, "coverage": coverage,
                          "available": coverage >= rm.AVAILABILITY_MIN and signal > 1e-5}
            raw, n = pool_raw[key]
            reliability = n / (n + rm.POOL_PRIOR_GAMES)
            out["ChampionPool"] = {"index": raw / (self.pool_sd[role] or 1.0) * reliability, "n_eff": float(n),
                                   "reliability": reliability, "coverage": 1.0, "available": True}
            self.indices[key] = out
        # J: role-weighted individual index, games-weighted over a player's roles.
        self.box = {}
        mix = defaultdict(dict)
        for (pid, role), rows in self.by_player_role.items():
            mix[pid][role] = sum(r["weight"] for r in rows)
        for pid, roles in mix.items():
            total = sum(roles.values())
            self.box[pid] = sum(share / total * self.role_index(pid, role) for role, share in roles.items()) if total else 0.0

    def role_index(self, pid, role):
        """sum over attributes of weight * index (missing attributes contribute 0: no reweighting)."""
        idx = self.indices.get((pid, role))
        if not idx:
            return 0.0
        w = self.weights_bp[role]
        return sum(w[i] / 10_000 * (idx[a]["index"] if idx[a]["available"] else 0.0) for i, a in enumerate(rm.ATTRIBUTES))


# ---------- level model data ----------

def build_games(seats):
    """Primary complete games with ten seats: lineups by model id, winner, tiers."""
    by_game = defaultdict(list)
    for s in seats:
        by_game[s["game_key"]].append(s)
    games = []
    for key, rows in by_game.items():
        blue = [r["model_id"] for r in rows if r["side"] == "Blue" and r["model_id"]]
        red = [r["model_id"] for r in rows if r["side"] == "Red" and r["model_id"]]
        if len(blue) != 5 or len(red) != 5 or len(set(blue + red)) != 10:
            continue
        first = rows[0]
        games.append({"key": key, "blue": blue, "red": red, "blue_win": int(first["winner_side"] == "Blue"),
                      "date": first["date"], "weight": first.get("weight", 1.0), "tier": first["tier"],
                      "competition": first["competition"],
                      "blue_team": next(r["team_key"] for r in rows if r["side"] == "Blue"),
                      "red_team": next(r["team_key"] for r in rows if r["side"] == "Red")})
    games.sort(key=lambda g: (g["date"], g["key"]))
    return games


def competition_structure(seats, players):
    """Player x competition shares (domestic games, decay-weighted; international-only players use their events) and
    competition -> tier (most common tier among its games)."""
    comp_tiers = defaultdict(Counter)
    shares = defaultdict(Counter)
    international = defaultdict(Counter)
    for s in seats:
        comp_tiers[s["competition"]][s["tier"]] += 1
        target = international if s["international"] else shares
        target[s["model_id"]][s["competition"]] += s.get("weight", 1.0)
    competitions = sorted(comp_tiers)
    comp_index = {c: i for i, c in enumerate(competitions)}
    tier_of = {c: comp_tiers[c].most_common(1)[0][0] for c in competitions}
    rows, cols, vals = [], [], []
    for pid, i in players.items():
        mix = shares.get(pid) or international.get(pid) or {}
        total = sum(mix.values())
        for c, w in mix.items():
            rows.append(i), cols.append(comp_index[c]), vals.append(w / total)
    S = sparse.csr_matrix((vals, (rows, cols)), shape=(len(players), len(competitions)))
    T = sparse.csr_matrix(([1.0] * len(competitions), (range(len(competitions)), [TIERS.index(tier_of[c]) for c in competitions])),
                          shape=(len(competitions), len(TIERS)))
    return competitions, tier_of, S, T


def fit_level(prepared, seats, games, config, box=None):
    players = {}
    for g in games:
        for p in g["blue"] + g["red"]:
            players.setdefault(p, len(players))
    data = rm.LineupData(games, players)
    competitions, tier_of, S, T = competition_structure([s for s in seats if s["model_id"] in players], players)
    box = np.array([prepared.box.get(p, 0.0) for p in players]) if box is None else box(players)
    fit = rm.fit_contributions(data, box, S, T, TIERS, config["gamma"], config["tau"], config["sigma_c"])
    fit.update({"players": players, "competitions": competitions, "tier_of": tier_of, "data": data, "box": box})
    return fit


def anchor_beta(fit, seats, as_of_year=2026):
    """Games-weighted mean contribution of players with >= 10 games in tier-a domestic competition in the as-of year."""
    counts = Counter(s["model_id"] for s in seats if s["tier"] == "a" and not s["international"] and s["year"] == as_of_year)
    chosen = {p: n for p, n in counts.items() if n >= 10 and p in fit["players"]}
    if not chosen:
        return float(np.mean(fit["beta"])), 0
    total = sum(chosen.values())
    return float(sum(fit["beta"][fit["players"][p]] * n / total for p, n in chosen.items())), len(chosen)


# ---------- ratings ----------

def rate(seats, content, config, as_of=AS_OF):
    as_of_ordinal = as_of.toordinal()
    prepared = Prepared(seats, as_of_ordinal, content["weights"], config["half_life"])
    games = build_games(seats)
    fit = fit_level(prepared, seats, games, config)
    anchor, anchor_players = anchor_beta(fit, seats, as_of.year if as_of.month > 1 else as_of.year - 1)
    k = content["points_per_logit"]
    levels = {p: rm.level(fit["beta"][i], anchor, k) for p, i in fit["players"].items()}
    level_sd = {p: 5.0 * k * fit["beta_sd"][i] for p, i in fit["players"].items()}
    return {"prepared": prepared, "games": games, "fit": fit, "anchor": anchor, "anchor_players": anchor_players,
            "levels": levels, "level_sd": level_sd, "points_per_logit": k}


# ---------- v2 components: achievements, consistency, development ----------

def event_catalogue(db):
    """overview page -> (league, split, year, is_playoffs, is_qualifier, last game day) from Leaguepedia games."""
    if not has_table(db, "lp_games"):
        return {}
    return {page: (league, split, year, playoffs == "1", qualifier == "1", parse_day(last))
            for page, league, split, year, playoffs, qualifier, last in db.execute(
                """SELECT overview_page, league, split, tournament_year, max(is_playoffs), max(is_qualifier), max(game_ts)
                   FROM lp_games GROUP BY overview_page""")}


def final_pages(events):
    """The deciding page of each (league, split, year): the playoffs page that ended last, or the only page."""
    groups = defaultdict(list)
    for page, (league, split, year, playoffs, qualifier, last) in events.items():
        if not qualifier:
            groups[(league, split, year)].append((playoffs, last or date.min, page))
    finals = set()
    for pages in groups.values():
        playoffs = [p for p in pages if p[0]]
        chosen = max(playoffs or pages)
        if playoffs or len(pages) == 1:
            finals.add(chosen[2])
    return finals


def achievements(db, seats, as_of):
    """model id -> achievement summary. A team's placement counts for a player who played at least PARTIAL_SHARE of
    that team's games in the event (full credit from REGULAR_SHARE). Points decay with a 2-year half-life."""
    out = defaultdict(lambda: {"score": 0.0, "titles": 0, "tier_a_titles": 0, "international_finals": 0,
                               "international_titles": 0, "placements": 0})
    if not has_table(db, "lp_tournament_results"):
        return out, {}
    events = event_catalogue(db)
    finals = final_pages(events)
    team_games, player_games, event_info = Counter(), defaultdict(Counter), {}
    for s in seats:
        if s["source"] != "leaguepedia" or not s["tournament"]:
            continue
        key = (s["tournament"], s["team_name"])
        player_games[key][s["model_id"]] += 1
        info = event_info.setdefault(s["tournament"], Counter())
        info[(s["tier"], s["international"], s["league"])] += 1
    # Some events publish final standings on a parent page whose games sit on sub-pages ("2024 Season World
    # Championship" -> "/Main Event", "/Play-In"): the parent's games are the union of its sub-pages' games.
    result_pages = {page for (page,) in db.execute("SELECT DISTINCT overview_page FROM lp_tournament_results")}
    parents = {}
    for page in result_pages - set(event_info):
        children = [p for p in event_info if p.startswith(page + "/")]
        if children:
            parents[page] = children
            info = event_info.setdefault(page, Counter())
            for child in children:
                info.update(event_info[child])
    for (child, team), players in list(player_games.items()):
        for parent, children in parents.items():
            if child in children:
                player_games[(parent, team)].update(players)
    for key, players in player_games.items():
        team_games[key] = max(players.values()) if players else 0
    stats = Counter({"parent_pages": len(parents)})
    for page, team, place, day, showmatch in db.execute(
            "SELECT overview_page, team, place_number, date, showmatch FROM lp_tournament_results"):
        day = parse_day(day)
        if day is None or not (WINDOW_START <= day < as_of) or (showmatch or "").strip() in ("1", "Yes", "yes", "true"):
            continue
        try:
            place = int(float(place))
        except (TypeError, ValueError):
            continue
        if page not in event_info or (page, team) not in player_games:
            stats["results_without_games"] += 1
            continue
        (tier, international, league), _ = event_info[page].most_common(1)[0]
        meta = events.get(page)
        qualifier = bool(meta and meta[4])
        event_class = ("world_msi" if league in WORLD_MSI_LEAGUES and not qualifier else
                       "international" if international and tier in ("s", "a") and not qualifier else tier)
        is_final = (page in finals or page in parents) and not qualifier
        bucket = 0 if place == 1 else 1 if place == 2 else 2 if place <= 4 else 3 if place <= 8 else None
        if bucket is None:
            continue
        points = PLACEMENT_POINTS.get(event_class, PLACEMENT_POINTS["unknown"])[bucket] * (1.0 if is_final else REGULAR_STAGE_SHARE)
        recency = 0.5 ** ((as_of - day).days / 365.25 / ACHIEVEMENT_HALF_LIFE_YEARS)
        total = team_games[(page, team)]
        stats["results_used"] += 1
        for pid, n in player_games[(page, team)].items():
            share = n / total if total else 0.0
            if share < PARTIAL_SHARE:
                continue
            credit = 1.0 if share >= REGULAR_SHARE else 0.5
            a = out[pid]
            a["score"] += points * credit * recency
            a["placements"] += 1
            if is_final and place == 1 and share >= REGULAR_SHARE:
                a["titles"] += 1
                a["tier_a_titles"] += event_class == "a"
                a["international_titles"] += event_class in ("world_msi", "international")
            if is_final and place <= 2 and share >= REGULAR_SHARE and event_class in ("world_msi", "international"):
                a["international_finals"] += 1
    return out, dict(stats)


def elite_evidence(a):
    """Major achievements 2023-2026 as a regular: an international final (Worlds, MSI, First Stand, EWC and the like)
    or at least two tier-a domestic titles (user decision 2026-10-14)."""
    return a["international_finals"] >= 1 or a["tier_a_titles"] >= 2


def consistency(prepared, seats_by_player):
    """model id -> (z, n_eff): steadiness of game-to-game performance (role-SD units, decay-weighted SD), shrunk toward
    the population SD with CONSISTENCY_PRIOR_GAMES; z > 0 is steadier than usual."""
    raw = {}
    for pid, rows in seats_by_player.items():
        values = np.array([r["perf"] / prepared.perf_sd[r["role"]] for r in rows])
        weights = np.array([r["weight"] for r in rows])
        if len(values) < 2 or weights.sum() <= 0:
            continue
        mean = float((weights * values).sum() / weights.sum())
        n_eff = float(weights.sum() ** 2 / (weights ** 2).sum())
        var = float((weights * (values - mean) ** 2).sum() / weights.sum()) * (n_eff / (n_eff - 1) if n_eff > 1 else 1.0)
        raw[pid] = (math.sqrt(max(var, 0.0)), n_eff)
    population = [sd for sd, n in raw.values() if n >= 20]
    mean_sd = float(np.mean(population)) if population else 1.0
    spread = float(np.std(population)) if len(population) > 10 else 0.1
    out = {}
    for pid, (sd, n) in raw.items():
        shrunk = (n * sd + rm.CONSISTENCY_PRIOR_GAMES * mean_sd) / (n + rm.CONSISTENCY_PRIOR_GAMES)
        out[pid] = (max(-3.0, min(3.0, (mean_sd - shrunk) / (spread or 0.1))), n)
    return out


def season_levels(seats, content, config, years=YEARS):
    """year -> model id -> (Level, games, Level SD): C1 without the box prior on that calendar year's games alone,
    anchored on that year's tier-a domestic players. Independent seasons, so year-to-year changes are not smoothed."""
    k = content["points_per_logit"]
    out = {}
    for year in years:
        views = [{"game_key": s["game_key"], "side": s["side"], "model_id": s["model_id"], "winner_side": s["winner_side"],
                  "date": s["date"], "tier": s["tier"], "competition": s["competition"], "team_key": s["team_key"],
                  "international": s["international"], "year": year} for s in seats if s["year"] == year]
        games = build_games(views)
        if len(games) < 200:
            continue
        fit = fit_level(None, views, games, dict(config, gamma=0.0), box=lambda players: np.zeros(len(players)))
        anchor, _ = anchor_beta(fit, views, year)
        counts = Counter(v["model_id"] for v in views)
        out[year] = {p: (rm.level(fit["beta"][i], anchor, k), counts[p], 5.0 * k * fit["beta_sd"][i])
                     for p, i in fit["players"].items()}
    return out


def development_curve(season, births, min_games=30):
    """FITTED yearly growth by age from consecutive independent seasons: change = age effect + b * (Level - 70) + e.
    The b term absorbs regression to the mean (measurement noise and selection), and every age shares the season
    anchoring, so growth is read relative to age 25, where by the user's rule development has ended: growth(a) =
    effect(a) - effect(25), made non-increasing (isotonic) and floored at 0 for ages under 25. The spread of true growth
    is the residual variance of under-25s minus that of over-25s (same measurement process, no development)."""
    rows = []
    for year in sorted(season):
        nxt = season.get(year + 1)
        if not nxt:
            continue
        for pid, (level, games, sd) in season[year].items():
            if not pid.startswith("ps:") or pid not in nxt or games < min_games or nxt[pid][1] < min_games:
                continue
            birth = births.get(pid[3:])
            age = age_on(birth["birthdate"], date(year, 7, 1)) if birth and not birth["conflict"] else None
            if age is None or not 15 <= age <= 40:
                continue
            rows.append((min(max(age, 17), 30), nxt[pid][0] - level, level - rm.ANCHOR_LEVEL, sd ** 2 + nxt[pid][2] ** 2))
    if len(rows) < 50:
        return {"growth_by_age": {}, "sigma_true": 0.0, "transitions": len(rows), "note": "too few transitions"}
    ages = sorted({r[0] for r in rows})
    X = np.zeros((len(rows), len(ages) + 1))
    y = np.array([r[1] for r in rows])
    for i, r in enumerate(rows):
        X[i, ages.index(r[0])] = 1.0
        X[i, -1] = r[2]
    coef, *_ = np.linalg.lstsq(X, y, rcond=None)
    effects = dict(zip(ages, coef[:-1]))
    counts = Counter(r[0] for r in rows)
    end = rm.GROWTH_END_AGE
    reference_ages = [a for a in ages if end <= a <= end + 2]
    reference = (sum(effects[a] * counts[a] for a in reference_ages) / sum(counts[a] for a in reference_ages)
                 if reference_ages else 0.0)
    young = [a for a in ages if a < end]
    smoothed = rm.isotonic_decreasing([effects[a] - reference for a in young], [counts[a] for a in young])
    growth = {a: max(0.0, v) for a, v in zip(young, smoothed)}
    residuals = y - X @ coef
    young_rows = [i for i, r in enumerate(rows) if r[0] < end]
    mature_rows = [i for i, r in enumerate(rows) if r[0] > end]
    young_var = float(np.var(residuals[young_rows])) if young_rows else 0.0
    mature_var = float(np.var(residuals[mature_rows])) if len(mature_rows) >= 30 else young_var
    return {"growth_by_age": {int(a): round(float(v), 3) for a, v in growth.items()},
            "raw_age_effects": {int(a): round(float(v), 3) for a, v in effects.items()},
            "reference_effect_age_25_27": round(float(reference), 3),
            "transitions_by_age": {int(a): counts[a] for a in ages}, "slope_level": round(float(coef[-1]), 4),
            "sigma_true": round(math.sqrt(max(0.0, young_var - mature_var)), 3),
            "residual_sd_under_25": round(math.sqrt(young_var), 3), "residual_sd_over_25": round(math.sqrt(mature_var), 3),
            "transitions": len(rows)}


# ---------- records ----------

def role_ratings(pid, model, rows, content, config, extra_points, elite_ok, floor):
    """Per role: linear level, raw quality R, banded level, skills and Overall (caps and the aging floor applied)."""
    prepared = model["prepared"]
    roles = Counter(r["role"] for r in rows)
    roles_2026 = Counter(r["role"] for r in rows if r["year"] == 2026)
    primary = (roles_2026 or roles).most_common(1)[0][0]
    level = model["levels"].get(pid)
    k, gamma = model["points_per_logit"], config["gamma"]
    per_role = {}
    for role in roles:
        if level is None:
            continue
        idx = prepared.indices[(pid, role)]
        linear = level + 5.0 * k * gamma * (prepared.role_index(pid, role) - prepared.box.get(pid, 0.0))
        r_value = linear + extra_points
        target = rm.banded(r_value)
        floored = bool(floor is not None and target < floor)
        if floored:
            target = floor
        if not elite_ok:
            target = min(target, rm.CAP_WITHOUT_ELITE_EVIDENCE)
        indices = {a: idx[a]["index"] for a in rm.ATTRIBUTES}
        available = {a: idx[a]["available"] for a in rm.ATTRIBUTES}
        saved = rm.PROFILE_SCALE
        rm.PROFILE_SCALE = config["profile_scale"]
        attributes, _ = rm.attributes_from(target, indices, content["weights"][role], available)
        rm.PROFILE_SCALE = saved
        if not elite_ok:
            attributes = rm.enforce_cap(attributes, content["weights"][role], rm.CAP_WITHOUT_ELITE_EVIDENCE)
        per_role[role] = {"attributes": attributes, "overall": rm.overall_from(attributes, content["weights"][role]),
                          "level": linear, "r": r_value, "banded_level": target, "aging_floor_applied": floored,
                          "games": roles[role], "games_2026": roles_2026.get(role, 0), "available": available,
                          "reliability": {a: idx[a]["reliability"] for a in rm.ATTRIBUTES}}
    return primary, per_role


PRIOR_QUANTILE = 25  # AUTHORED: conservative reference point for PRIOR_ONLY placeholders


def tier_reference(records):
    """PRIOR_ONLY reference: the 25th percentile raw quality R of rated players whose primary tier is t, when at least
    20 exist; the declared global prior is the same percentile over every rated player."""
    by_tier = defaultdict(list)
    for r in records.values():
        by_tier[r["_primary_tier"]].append(r["_r"])
    reference = {t: float(np.percentile(v, PRIOR_QUANTILE)) for t, v in by_tier.items() if len(v) >= 20 and t != "unknown"}
    every = [v for values in by_tier.values() for v in values]
    return reference, float(np.percentile(every, PRIOR_QUANTILE)) if every else rm.ANCHOR_LEVEL


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def image_maps():
    """player id -> (portrait path, source), team id -> logo path; paths relative to data/assets, CACHED files only."""
    portraits = {}
    for e in read_json(ASSETS / "mapping" / "player_images.json").get("entries", []):
        if e.get("image_status") == "CACHED" and e.get("image_path"):
            portraits[str(e["entity_id"])] = (e["image_path"], "pandascore")
    for e in read_json(ASSETS / "mapping" / "player_images_2026.json").get("entries", []):
        if e.get("confidence") == "HIGH" and e.get("status") == "CACHED" and e.get("local_path"):
            portraits[str(e["player_id"])] = (e["local_path"], "leaguepedia_2026")
    logos = {str(e["entity_id"]): e["logo_path"] for e in read_json(ASSETS / "mapping" / "team_images.json").get("entries", [])
             if e.get("image_status") == "CACHED" and e.get("logo_path")}
    return portraits, logos


def quality_inputs(db, seats, model, as_of):
    """Achievements and consistency for every model player as of `as_of`."""
    seats_by_player = defaultdict(list)
    for s in seats:
        seats_by_player[s["model_id"]].append(s)
    ach, ach_stats = achievements(db, seats, as_of)
    cons = consistency(model["prepared"], seats_by_player)
    return seats_by_player, ach, ach_stats, cons


def extra_points(pid, ach, cons):
    a = ach.get(pid)
    z = cons.get(pid, (0.0, 0.0))[0]
    return (rm.achievement_points(a["score"]) if a else 0.0) + rm.consistency_points(z)


def previous_year(db, seats, content, config, as_of):
    """Primary-role banded level one year before `as_of` (for the veterans' aging floor): model id -> banded level."""
    prev_as_of = as_of - timedelta(days=365)
    prev = [s for s in seats if s["date"] < prev_as_of.toordinal()]
    if not prev:
        return {}, prev_as_of
    model = rate(prev, content, config, prev_as_of)
    seats_by_player, ach, _, cons = quality_inputs(db, prev, model, prev_as_of)
    out = {}
    for pid, rows in seats_by_player.items():
        if pid not in model["levels"]:
            continue
        a = ach.get(pid) or {"international_finals": 0, "tier_a_titles": 0}
        elite_ok = elite_evidence(a) and cons.get(pid, (0.0, 0))[0] >= rm.ELITE_MIN_CONSISTENCY_Z
        primary, per_role = role_ratings(pid, model, rows, content, config, extra_points(pid, ach, cons), elite_ok, None)
        if primary in per_role:
            out[pid] = per_role[primary]["banded_level"]
    return out, prev_as_of


def build_records(db, seats, model, content, config, as_of=AS_OF, births=None, prev=None, curve=None):
    """One record per PandaScore catalogue player, plus records for unresolved source identities."""
    prepared = model["prepared"]
    births = births or {}
    prev = prev or {}
    growth = (curve or {}).get("growth_by_age", {})
    sigma = (curve or {}).get("sigma_true", 0.0)
    as_of_ordinal = as_of.toordinal()
    seats_by_player, ach, ach_stats, cons = quality_inputs(db, seats, model, as_of)
    team_names = dict(db.execute("SELECT team_id, name FROM ps_teams"))
    team_alias = {(src, key, year): ps for src, key, year, ps in db.execute(
        "SELECT source, team_key, year, ps_team_id FROM pf_team_identity WHERE ps_team_id IS NOT NULL")}
    league_names = dict(db.execute("SELECT league_id, name FROM ps_leagues"))
    portraits, logos = image_maps()
    stage = {t: (tier, begin, league) for t, tier, begin, league in db.execute(
        "SELECT tournament_id, tier, begin_at, league_id FROM ps_tournaments")}
    registrations = defaultdict(list)
    for t, team, player, role in db.execute("SELECT tournament_id, team_id, player_id, role FROM ps_registrations"):
        if t in stage and stage[t][1]:
            registrations[player].append((stage[t][1], stage[t][0], team, role, stage[t][2]))
    records, unresolved = {}, []
    for pid, rows in seats_by_player.items():
        if pid not in model["levels"]:
            continue
        ps_id = pid[3:] if pid.startswith("ps:") else None
        birth = births.get(ps_id) if ps_id else None
        age = age_on(birth["birthdate"], as_of) if birth else None
        a = ach.get(pid) or {"score": 0.0, "titles": 0, "tier_a_titles": 0, "international_finals": 0,
                             "international_titles": 0, "placements": 0}
        cons_z = cons.get(pid, (0.0, 0.0))[0]
        elite_ok = elite_evidence(a) and cons_z >= rm.ELITE_MIN_CONSISTENCY_Z
        drop = rm.aging_drop(age)
        floor = prev[pid] - drop if drop is not None and pid in prev else None
        primary, per_role = role_ratings(pid, model, rows, content, config, extra_points(pid, ach, cons), elite_ok, floor)
        if primary not in per_role:
            continue
        rows_sorted = sorted(rows, key=lambda r: (r["date"], r["game_key"]))
        last = rows_sorted[-1]
        weights = [r["weight"] for r in rows]
        n_eff = float(sum(weights) ** 2 / sum(w * w for w in weights))
        games_by_year = Counter(r["year"] for r in rows)
        roles = Counter(r["role"] for r in rows)
        cls = "MEASURED_ESTIMATE" if len(rows) >= MEASURED_GAMES and n_eff >= MEASURED_EFFECTIVE else "LIMITED_EVIDENCE"
        sd = model["level_sd"][pid]
        tiers = Counter(r["tier"] for r in rows if not r["international"])
        primary_tier = (tiers or Counter(r["tier"] for r in rows)).most_common(1)[0][0]
        comps = Counter(r["competition"] for r in rows if not r["international"] and r["year"] == last["year"]) or Counter(r["competition"] for r in rows)
        primary_comp = comps.most_common(1)[0][0]
        perf = [(r["date"], r["perf"] / prepared.perf_sd[r["role"]]) for r in rows_sorted]
        score, trend, form_conf, recent_n = rm.form(perf, as_of_ordinal, config["form_points_per_sd"])
        days_since = as_of_ordinal - last["date"]
        form_status = "NO_RECENT_EVIDENCE" if days_since > rm.FORM_RECENT_DAYS else ("MEASURED" if score is not None else "INSUFFICIENT_RECENT_GAMES")

        def window_index(selected):
            if len(selected) < 5:
                return None
            return round(float(np.mean([p for _, p in selected])), 3)
        by_year_perf = {y: [(d, p) for (d, p), r in zip(perf, rows_sorted) if r["year"] == y] for y in YEARS}
        windows = {f"index_{y}": window_index(by_year_perf[y]) for y in YEARS}
        windows.update({"index_last10": window_index(perf[-10:]) if len(perf) >= 10 else None,
                        "index_last20": window_index(perf[-20:]) if len(perf) >= 20 else None,
                        "index_30d": window_index([x for x in perf if as_of_ordinal - x[0] <= 30]),
                        "index_90d": window_index([x for x in perf if as_of_ordinal - x[0] <= 90]),
                        "index_180d": window_index([x for x in perf if as_of_ordinal - x[0] <= 180])})
        windows["change_2025_2026"] = (round(windows["index_2026"] - windows["index_2025"], 3)
                                       if windows["index_2025"] is not None and windows["index_2026"] is not None else None)
        reliability = n_eff / (n_eff + 15.0)
        connected = model["competition_link"].get(primary_comp, "WEAK")
        confidence = reliability * (1.0 if connected == "STRONG" else 0.7)
        primary_role = per_role[primary]
        unavailable = sorted({f"{x}:{'EARLY_TIMELINE_UNAVAILABLE' if x == 'EarlyGame' else 'NO_FEATURE_EVIDENCE'}"
                              for x, ok in primary_role["available"].items() if not ok})
        if not any(r["has_wards"] for r in rows):
            unavailable.append("Macro:WARD_COUNTS_UNAVAILABLE")
        last_2026 = [r for r in rows_sorted if r["year"] == 2026]
        domestic_2026 = [r for r in last_2026 if not r["international"]]
        team_row = (domestic_2026 or last_2026 or [None])[-1]
        team_ps = team_alias.get((team_row["source"], team_row["team_key"], team_row["year"])) if team_row else None
        home = next((r for r in reversed(rows_sorted) if r["competition"] == primary_comp), last)
        overall = primary_role["overall"]
        if age is None:
            potential, potential_basis = None, "UNKNOWN_AGE"
        elif age >= rm.GROWTH_END_AGE:
            potential, potential_basis = overall, "AGE_25_OR_OVER_NO_GROWTH"
        else:
            expected, upper = rm.projected_growth(age, growth, sigma)
            projected = rm.banded(primary_role["r"] + upper)
            potential = int(min(rm.SCALE_MAX, max(overall, rm.round_half_even(projected))))
            potential_basis = f"development curve to age {rm.GROWTH_END_AGE}: +{expected:.1f} expected, +{upper:.1f} at the 80th percentile (raw points)"
        record = {
            "player_id": ps_id, "source_identity": None if ps_id else pid,
            "primary_role": ROLE_LABEL[primary], "overall_rating": overall, "band": rm.band_of(overall),
            "mechanics": primary_role["attributes"][0], "early_game": primary_role["attributes"][1],
            "teamfight": primary_role["attributes"][2], "macro": primary_role["attributes"][3],
            "champion_pool": primary_role["attributes"][4],
            "potential": potential, "potential_basis": potential_basis,
            "age": age, "age_source": birth["source"] if birth else None, "age_conflict": bool(birth and birth["conflict"]),
            "career_stage": rm.career_stage(age),
            "level_linear": round(primary_role["level"], 2), "raw_quality": round(primary_role["r"], 2),
            "level_interval_80": [round(primary_role["level"] - 1.2816 * sd, 1), round(primary_role["level"] + 1.2816 * sd, 1)],
            "achievement_score": round(a["score"], 2), "achievement_points": round(rm.achievement_points(a["score"]), 2),
            "titles": a["titles"], "tier_a_titles": a["tier_a_titles"], "international_finals": a["international_finals"],
            "international_titles": a["international_titles"], "elite_evidence": elite_ok,
            "consistency_z": round(cons_z, 2), "consistency_points": round(rm.consistency_points(cons_z), 2),
            "rating_prev_year": round(prev[pid], 1) if pid in prev else None,
            "aging_floor_applied": primary_role["aging_floor_applied"],
            "rating_evidence_class": cls, "confidence": round(confidence, 3), "confidence_label": rm.confidence_label(confidence),
            **{f"games_{y}": games_by_year.get(y, 0) for y in YEARS}, "effective_games": round(n_eff, 1),
            "last_verified_game": date.fromordinal(last["date"]).isoformat(),
            "team_2026": (team_names.get(team_ps) if team_ps else (team_row["team_name"] if team_row else None)),
            "team_2026_ps_id": team_ps, "team_2026_as_of": date.fromordinal(team_row["date"]).isoformat() if team_row else None,
            "league": league_names.get(home["ps_league_id"]) if home["ps_league_id"] else home["league"],
            "region": home["region"], "primary_competition": primary_comp, "competition_link": connected,
            "primary_tier": primary_tier,
            "form_score": score, "form_trend": trend if form_status == "MEASURED" else "UNKNOWN", "form_status": form_status,
            "form_confidence": form_conf, "recent_games": recent_n, **windows,
            "roles_played": {ROLE_LABEL[r]: n for r, n in roles.most_common()},
            "role_ratings": {ROLE_LABEL[r]: {"overall": v["overall"], "attributes": v["attributes"], "games_2026": v["games_2026"]}
                             for r, v in per_role.items()},
            "unavailable_feature_flags": unavailable,
            "source_references": sorted({r["source"] for r in rows}),
            "source_identities": sorted({r["identity_key"] for r in rows if r["identity_key"]}),
            "level_sd": round(sd, 2), "_level": primary_role["level"], "_r": primary_role["r"], "_primary_tier": primary_tier,
            "_per_role": per_role,
        }
        if ps_id:
            records[ps_id] = record
        else:
            unresolved.append(record)
    reference, global_prior = tier_reference(records)
    catalogue = list(db.execute("SELECT player_id, name, first_name, last_name, nationality, role, current_team_id FROM ps_players"))
    out = []
    for player_id, name, first, last_name, nationality, role, current_team in catalogue:
        r = records.get(player_id)
        if r is None:
            regs = sorted(registrations.get(player_id, []), key=lambda x: x[0])
            latest = regs[-1] if regs else None
            tier = latest[1] if latest and latest[1] else None
            prior = reference.get(tier) if tier else None
            basis = f"25th percentile raw quality of rated players whose primary PandaScore tier is {tier}" if prior is not None else \
                "declared global prior: 25th percentile raw quality of all rated players (no tier known)"
            r_value = prior if prior is not None else global_prior
            v = int(min(rm.CAP_PRIOR_ONLY, rm.round_half_even(rm.banded(r_value))))
            birth = births.get(player_id)
            age = age_on(birth["birthdate"], as_of) if birth else None
            research_role = PS_ROLE.get((latest[3] if latest and latest[3] else role) or "")
            r = {"player_id": player_id, "source_identity": None,
                 "primary_role": ROLE_LABEL.get(research_role) if research_role else None,
                 "overall_rating": v, "band": rm.band_of(v), "mechanics": v, "early_game": v, "teamfight": v, "macro": v,
                 "champion_pool": v, "potential": None, "potential_basis": "NOT_ESTABLISHED (no verified games)",
                 "age": age, "age_source": birth["source"] if birth else None, "age_conflict": bool(birth and birth["conflict"]),
                 "career_stage": rm.career_stage(age), "level_linear": None, "raw_quality": round(r_value, 2),
                 "level_interval_80": None, "achievement_score": 0.0, "achievement_points": 0.0, "titles": 0, "tier_a_titles": 0,
                 "international_finals": 0, "international_titles": 0, "elite_evidence": False, "consistency_z": None,
                 "consistency_points": 0.0, "rating_prev_year": None, "aging_floor_applied": False,
                 "rating_evidence_class": "PRIOR_ONLY", "confidence": 0.0, "confidence_label": "NONE",
                 **{f"games_{y}": 0 for y in YEARS}, "effective_games": 0.0, "last_verified_game": None,
                 "team_2026": team_names.get(latest[2]) if latest and latest[0][:4] == "2026" else None,
                 "team_2026_ps_id": latest[2] if latest and latest[0][:4] == "2026" else None,
                 "team_2026_as_of": latest[0][:10] if latest and latest[0][:4] == "2026" else None,
                 "league": league_names.get(latest[4]) if latest else None, "region": None, "primary_competition": None,
                 "competition_link": None, "primary_tier": tier, "form_score": None, "form_trend": "UNKNOWN",
                 "form_status": "NO_RECENT_EVIDENCE", "form_confidence": 0.0, "recent_games": 0,
                 **{f"index_{y}": None for y in YEARS}, "index_last10": None, "index_last20": None, "index_30d": None,
                 "index_90d": None, "index_180d": None, "change_2025_2026": None, "roles_played": {}, "role_ratings": {},
                 "unavailable_feature_flags": ["ALL:NO_VERIFIED_INDIVIDUAL_GAMES"], "source_references": ["pandascore"],
                 "source_identities": [], "level_sd": None, "prior_basis": basis,
                 "provisional_note": "PRIOR_ONLY: individual competitive ability is not established; placeholder value only.",
                 "_level": None, "_r": r_value, "_primary_tier": tier, "_per_role": {}}
        else:
            r["prior_basis"] = None
            r["provisional_note"] = None if r["rating_evidence_class"] == "MEASURED_ESTIMATE" else \
                "LIMITED_EVIDENCE: few verified games; the value leans on the model's priors."
        team_for_logo = r.get("team_2026_ps_id")
        portrait = portraits.get(player_id)
        r.update({"player": name, "real_name": " ".join(x for x in (first, last_name) if x) or None, "nationality": nationality,
                  "player_image": portrait[0] if portrait else None, "player_image_source": portrait[1] if portrait else None,
                  "team_logo": logos.get(team_for_logo) if team_for_logo else None,
                  "rating_model_version": rm.MODEL_VERSION, "as_of_date": as_of.isoformat()})
        out.append(r)
    for r in unresolved:
        r.update({"player": None, "real_name": None, "nationality": None, "player_image": None, "player_image_source": None,
                  "team_logo": None, "prior_basis": None, "rating_model_version": rm.MODEL_VERSION, "as_of_date": as_of.isoformat(),
                  "provisional_note": "UNRESOLVED: not reconciled to a PandaScore player; reported separately."})
    return out, unresolved, {"tier_reference": reference, "global_prior": global_prior, "achievement_stats": ach_stats}


def competition_links(seats, min_games=30):
    """STRONG when the players of a competition played >= min_games verified games in other competitions (incl.
    international events) in the window, which ties its level to the rest of the network; WEAK otherwise."""
    home = {}
    per_player = defaultdict(Counter)
    for s in seats:
        if not s["international"]:
            per_player[s["model_id"]][s["competition"]] += 1
    for pid, c in per_player.items():
        home[pid] = c.most_common(1)[0][0]
    outside = Counter()
    for s in seats:
        h = home.get(s["model_id"])
        if h and s["competition"] != h:
            outside[h] += 1
    comps = {s["competition"] for s in seats}
    return {c: "STRONG" if outside[c] >= min_games else "WEAK" for c in comps}


# ---------- rankings ----------

def max_level_sd(config, points_per_logit):
    return RANK_SD_SHARE * 5.0 * points_per_logit * config["tau"]


def rankings(records, as_of=AS_OF, max_sd=8.4):
    """Role lists use each player's rating in that role; the overall list uses the primary-role Overall. Ties are
    broken by the raw quality R, then 2026 games, then provider id."""
    as_of_ordinal = as_of.toordinal()
    eligible_base = [r for r in records if r["player_id"] and r["rating_evidence_class"] != "PRIOR_ONLY"
                     and r["last_verified_game"] and as_of_ordinal - date.fromisoformat(r["last_verified_game"]).toordinal() <= RANK_MAX_DAYS_SINCE_LAST
                     and r["level_sd"] is not None and r["level_sd"] <= max_sd]
    lists, deficits = {}, {}
    for role in rm.ROLES:
        label = ROLE_LABEL[role]
        rows = []
        for r in eligible_base:
            v = r["_per_role"].get(role)
            if v and v["games_2026"] >= RANK_MIN_GAMES_2026:
                rows.append((v["overall"], v["r"], v["games_2026"], r, v))
        rows.sort(key=lambda x: (-x[0], -x[1], -x[2], int(x[3]["player_id"])))
        lists[label] = [ranked(i + 1, r, label, v) for i, (_, _, _, r, v) in enumerate(rows[:100])]
        deficits[label] = {"eligible": len(rows), "published": min(100, len(rows)), "deficit": max(0, 100 - len(rows))}
    rows = []
    for r in eligible_base:
        role = next(k for k, v in ROLE_LABEL.items() if v == r["primary_role"])
        v = r["_per_role"].get(role)
        if v and v["games_2026"] >= RANK_MIN_GAMES_2026:
            rows.append((v["overall"], v["r"], v["games_2026"], r, v))
    rows.sort(key=lambda x: (-x[0], -x[1], -x[2], int(x[3]["player_id"])))
    lists["Overall"] = [ranked(i + 1, r, r["primary_role"], v) for i, (_, _, _, r, v) in enumerate(rows[:100])]
    deficits["Overall"] = {"eligible": len(rows), "published": min(100, len(rows)), "deficit": max(0, 100 - len(rows))}
    return lists, deficits


def ranked(rank, r, role_label, v):
    a = v["attributes"]
    form_text = f"{r['form_score']:+d} {r['form_trend']}" if r["form_score"] is not None else r["form_status"]
    return {"rank": rank, "player": r["player"], "real_name": r["real_name"], "role": role_label,
            "team": r["team_2026"], "team_as_of": r["team_2026_as_of"], "region": r["region"], "league": r["league"],
            "nationality": r["nationality"], "overall": v["overall"], "band": rm.band_of(v["overall"]),
            "mechanics": a[0], "early_game": a[1], "teamfight": a[2], "macro": a[3], "champion_pool": a[4], "form": form_text,
            "age": r["age"], "career_stage": r["career_stage"], "potential": r["potential"],
            "titles": r["titles"], "international_finals": r["international_finals"],
            **{f"games_{y}": r[f"games_{y}"] for y in YEARS}, "games_2026_in_role": v["games_2026"],
            "confidence": r["confidence_label"], "competition_link": r["competition_link"],
            "raw_quality": round(v["r"], 2), "player_image": r["player_image"], "team_logo": r["team_logo"],
            "player_id": r["player_id"], "evidence_class": r["rating_evidence_class"]}


# ---------- outputs ----------

CSV_FIELDS = ["player_id", "player", "real_name", "nationality", "primary_role", "overall_rating", "band", "mechanics",
              "early_game", "teamfight", "macro", "champion_pool", "potential", "potential_basis", "age", "age_source",
              "age_conflict", "career_stage", "rating_evidence_class", "confidence", "confidence_label", "level_linear",
              "raw_quality", "level_interval_80", "achievement_score", "achievement_points", "titles", "tier_a_titles",
              "international_finals", "international_titles", "elite_evidence", "consistency_z", "consistency_points",
              "rating_prev_year", "aging_floor_applied", "games_2023", "games_2024", "games_2025", "games_2026",
              "effective_games", "last_verified_game", "team_2026", "team_2026_as_of", "league", "region", "primary_tier",
              "competition_link", "form_score", "form_trend", "form_status", "form_confidence", "recent_games", "index_2023",
              "index_2024", "index_2025", "index_2026", "index_last10", "index_last20", "index_30d", "index_90d", "index_180d",
              "change_2025_2026", "unavailable_feature_flags", "source_references", "prior_basis", "provisional_note",
              "player_image", "team_logo", "rating_model_version", "as_of_date"]
EVIDENCE_FIELDS = ["player_id", "rating_evidence_class", "confidence", "confidence_label", "games_2023", "games_2024",
                   "games_2025", "games_2026", "effective_games", "level_sd", "level_interval_80", "primary_competition",
                   "competition_link", "primary_tier", "roles_played", "achievement_score", "titles", "tier_a_titles",
                   "international_finals", "elite_evidence", "consistency_z", "age_source", "age_conflict",
                   "source_references", "source_identities", "unavailable_feature_flags", "prior_basis", "player_image_source"]
RANK_FIELDS = ["rank", "player", "real_name", "role", "team", "team_as_of", "region", "league", "nationality", "overall", "band",
               "mechanics", "early_game", "teamfight", "macro", "champion_pool", "form", "age", "career_stage", "potential",
               "titles", "international_finals", "games_2023", "games_2024", "games_2025", "games_2026", "games_2026_in_role",
               "confidence", "competition_link", "raw_quality", "player_image", "team_logo", "player_id", "evidence_class"]
LIST_FILES = {"Top": "top100_top.csv", "Jungle": "top100_jungle.csv", "Mid": "top100_mid.csv", "Bot": "top100_bot.csv",
              "Support": "top100_support.csv", "Overall": "top100_overall.csv"}


def cell(value):
    if isinstance(value, (list, dict)):
        return json.dumps(value, ensure_ascii=False)
    return "" if value is None else value


def write_csv(path, rows, fields):
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        for r in rows:
            writer.writerow({k: cell(r.get(k)) for k in fields})


def public(record):
    return {k: v for k, v in record.items() if not k.startswith("_")}


def plain(value):
    """json default: numpy scalars as plain values, anything else (dates) as text."""
    return value.item() if hasattr(value, "item") else str(value)


def sha_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_outputs(out_dir, records, unresolved, lists, deficits, manifest):
    out_dir.mkdir(parents=True, exist_ok=True)
    records = sorted(records, key=lambda r: int(r["player_id"]))
    write_csv(out_dir / f"{OUTPUT_STEM}.csv", records, CSV_FIELDS)
    (out_dir / f"{OUTPUT_STEM}.json").write_text(
        json.dumps({"rating_model_version": rm.MODEL_VERSION, "as_of_date": manifest["as_of_date"],
                    "note": "Research model; not an official rating. PRIOR_ONLY values are placeholders.",
                    "players": [public(r) for r in records]}, ensure_ascii=False, indent=0, default=plain) + "\n",
        encoding="utf-8", newline="\n")
    write_csv(out_dir / "player_rating_evidence.csv", records, EVIDENCE_FIELDS)
    write_csv(out_dir / "unresolved_identities.csv", sorted(unresolved, key=lambda r: -r["overall_rating"]),
              ["source_identity"] + CSV_FIELDS[4:])
    for label, file_name in LIST_FILES.items():
        write_csv(out_dir / file_name, lists[label], RANK_FIELDS)
    produced = [f"{OUTPUT_STEM}.csv", f"{OUTPUT_STEM}.json", "player_rating_evidence.csv", "unresolved_identities.csv",
                *LIST_FILES.values()]
    manifest["outputs"] = {name: sha_file(out_dir / name) for name in sorted(produced)}
    manifest["ranking_deficits"] = deficits
    (out_dir / "rating_manifest.json").write_text(json.dumps(manifest, indent=1, ensure_ascii=False, default=plain) + "\n",
                                                  encoding="utf-8", newline="\n")


def source_hashes(db):
    hashes = {}
    for folder in ("leaguepedia", "leaguepedia-reference"):
        for run in sorted((DATA_DIR / "raw" / folder).glob("*/manifest.json")):
            hashes[f"{folder}:{run.parent.name}"] = sha_file(run)
    if has_table(db, "oe_source_files"):
        for sha, name, year in db.execute("SELECT file_sha256, file_name, target_year FROM oe_source_files ORDER BY import_order"):
            hashes[f"oracles_elixir:{year}:{name}"] = sha
    for run_id, manifest_sha in db.execute("SELECT run_id, manifest_sha256 FROM collection_runs ORDER BY run_id"):
        hashes[f"pandascore:{run_id}"] = manifest_sha
    return hashes


def run(db_path, as_of=AS_OF, out_dir=OUT_DIR, config=None, write_db=False):
    config = dict(CONFIG, **(config or {}))
    content = load_content()
    db = sqlite3.connect(db_path)
    try:
        seats = load_seats(db, as_of)
        births = load_birthdates(db)
        # Order matters: each rate() call writes its own weights and scores on the seats it is given.
        prev, prev_as_of = previous_year(db, seats, content, config, as_of)
        season = season_levels(seats, content, config)
        curve = development_curve(season, births)
        del season
        model = rate(seats, content, config, as_of)
        model["competition_link"] = competition_links(seats)
        records, unresolved, priors = build_records(db, seats, model, content, config, as_of, births, prev, curve)
        lists, deficits = rankings(records, as_of, max_level_sd(config, content["points_per_logit"]))
        rated = [r for r in records if r["rating_evidence_class"] != "PRIOR_ONLY"]
        manifest = {
            "rating_model_version": rm.MODEL_VERSION, "as_of_date": as_of.isoformat(),
            "window": [WINDOW_START.isoformat(), as_of.isoformat()],
            "as_of_rule": "games dated from the window start and before the as-of day (UTC)",
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "config": config, "config_sha256": hashlib.sha256(json.dumps(config, sort_keys=True).encode()).hexdigest(),
            "model_code_sha256": {name: sha_file(TOOL_DIR / name) for name in ("rating_model.py", "rate_players.py", "build_performance.py")},
            "content": {"path": "content/fixture.json", "sha256": content["sha256"], "role_weights": content["role_weights_raw"],
                        "logit_scale": content["logit_scale"], "points_per_logit": content["points_per_logit"]},
            "anchor": {"level": rm.ANCHOR_LEVEL, "beta": model["anchor"], "players": model["anchor_players"],
                       "definition": "games-weighted mean contribution of players with >= 10 games in PandaScore tier-a stages in the as-of year"},
            "banded_scale": {"bands": rm.BANDS, "knots_r_to_overall": rm.BAND_KNOTS,
                             "cap_without_elite_evidence": rm.CAP_WITHOUT_ELITE_EVIDENCE, "cap_prior_only": rm.CAP_PRIOR_ONLY,
                             "elite_evidence": "an international final (1st or 2nd) or >= 2 tier-a domestic titles, 2023-2026, "
                                               f"as a regular; consistency z >= {rm.ELITE_MIN_CONSISTENCY_Z}",
                             "achievement_points": {"max": rm.ACHIEVEMENT_MAX_POINTS, "scale": rm.ACHIEVEMENT_SCALE,
                                                    "placement_points": PLACEMENT_POINTS, "regular_stage_share": REGULAR_STAGE_SHARE,
                                                    "half_life_years": ACHIEVEMENT_HALF_LIFE_YEARS},
                             "consistency_points": {"per_z": rm.CONSISTENCY_POINTS, "max": rm.CONSISTENCY_MAX_POINTS}},
            "age_rules": {"growth_end_age": rm.GROWTH_END_AGE, "veteran_age": rm.VETERAN_AGE, "max_drop": {"29-30": 2, "31+": 3},
                          "previous_rating_as_of": prev_as_of.isoformat(), "players_with_previous_rating": len(prev),
                          "players_with_age": sum(1 for r in records if r["age"] is not None),
                          "rated_players_with_age": sum(1 for r in rated if r["age"] is not None)},
            "development_curve": curve,
            "achievement_stats": priors["achievement_stats"],
            "side_advantage_logit": model["fit"]["h"], "fit_converged": model["fit"]["converged"],
            "fit_iterations": model["fit"]["iterations"],
            "tier_levels_points": {t: round(5 * content["points_per_logit"] * (v - model["anchor"]) + rm.ANCHOR_LEVEL, 2)
                                   for t, v in model["fit"]["tier"].items()},
            "prior_only_reference": {"by_tier": {k: round(v, 2) for k, v in priors["tier_reference"].items()},
                                     "global": round(priors["global_prior"], 2)},
            "seats": len(seats), "games": len(model["games"]), "model_players": len(model["fit"]["players"]),
            "catalog_players": len(records), "unresolved_identities": len(unresolved),
            "band_counts": dict(Counter(r["band"] for r in records)),
            "band_counts_rated": dict(Counter(r["band"] for r in rated)),
            "source_hashes": source_hashes(db),
            "eligibility": {"min_games_2026_in_role": RANK_MIN_GAMES_2026, "max_days_since_last_game": RANK_MAX_DAYS_SINCE_LAST,
                            "max_level_sd": round(max_level_sd(config, content["points_per_logit"]), 2),
                            "max_level_sd_rule": f"{RANK_SD_SHARE} x the prior SD of the Level (5 x points per logit x tau)",
                            "identity": "HIGH_CONFIDENCE_MATCH to a PandaScore player",
                            "evidence": "MEASURED_ESTIMATE or LIMITED_EVIDENCE"},
            "global_ranking_status": "EXPERIMENTAL: cross-region levels rest on international games and player movement; see competition_link",
        }
        write_outputs(out_dir, records, unresolved, lists, deficits, manifest)
        if write_db:
            write_tables(db, records, unresolved)
        return {"records": records, "unresolved": unresolved, "lists": lists, "deficits": deficits, "manifest": manifest,
                "model": model, "seats": seats, "content": content, "config": config, "births": births, "prev": prev,
                "curve": curve}
    finally:
        db.close()


def write_tables(db, records, unresolved):
    with db:
        db.execute("DROP TABLE IF EXISTS rt_player_ratings")
        db.execute("CREATE TABLE rt_player_ratings (player_id TEXT PRIMARY KEY, record_json TEXT NOT NULL, overall_rating INTEGER NOT NULL, "
                   "band TEXT NOT NULL, potential INTEGER, rating_evidence_class TEXT NOT NULL, rating_model_version TEXT NOT NULL, "
                   "as_of_date TEXT NOT NULL)")
        db.executemany("INSERT INTO rt_player_ratings VALUES (?,?,?,?,?,?,?,?)",
                       [(r["player_id"], json.dumps(public(r), ensure_ascii=False, default=plain), r["overall_rating"], r["band"],
                         r["potential"], r["rating_evidence_class"], r["rating_model_version"], r["as_of_date"]) for r in records])
        db.execute("DROP TABLE IF EXISTS rt_unresolved_ratings")
        db.execute("CREATE TABLE rt_unresolved_ratings (source_identity TEXT PRIMARY KEY, record_json TEXT NOT NULL)")
        db.executemany("INSERT INTO rt_unresolved_ratings VALUES (?,?)",
                       [(r["source_identity"], json.dumps(public(r), ensure_ascii=False, default=plain)) for r in unresolved])


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--as-of", type=date.fromisoformat, default=AS_OF)
    parser.add_argument("--out", type=Path, default=OUT_DIR)
    parser.add_argument("--write-db", action="store_true", help="also write rt_* tables into the staging database")
    args = parser.parse_args()
    result = run(args.db, args.as_of, args.out, write_db=args.write_db)
    m = result["manifest"]
    print(json.dumps({k: m[k] for k in ("seats", "games", "model_players", "catalog_players", "unresolved_identities",
                                        "fit_converged", "band_counts", "band_counts_rated", "ranking_deficits")}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
