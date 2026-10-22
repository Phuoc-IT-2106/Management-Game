"""Cross-provider identity reconciliation: per-game player rows -> PandaScore players (offline).

Two sources use the same rules: Oracle's Elixir (oe_player_games -> xp_* tables) and Leaguepedia
(lp_player_games -> lpx_* tables, fetch_leaguepedia_stats.py). The years 2023 to 2026 are reconciled.

Evidence is combined, never a single signal:
  handle   a normalized handle of the row (NFKC, casefold, letters and digits only) equals a name PandaScore reported
           for the player: the catalogue name, or a name embedded in any observed stage roster or team listing
           (aliases the provider itself reported). An OE row has one handle (playername); a Leaguepedia row has its
           canonical player page, the page it links and the displayed name, each without a "(Real Name)" suffix;
  team     the source team maps to a PandaScore team through a HIGH_CONFIDENCE_MATCH team alias for that year (below);
  window   the player is registered for that PandaScore team on a stage touching a target year, and the stage's dates,
           widened by WINDOW_SLACK_DAYS, contain the game date. The provider's current team is never used: it is a
           snapshot of today, not evidence about January;
  role     the position (top/jng/mid/bot/sup) equals the role on that registration (or the catalogue role when the
           registration carries none).
Team aliases link a source team (OE teamid or normalized name; Leaguepedia team page) to a PandaScore team per year,
by roster overlap only: source rows (player-games) whose handle is registered for that team in a window containing the
game date. HIGH_CONFIDENCE_MATCH needs >= MIN_ALIAS_PLAYERS distinct overlapping players, at least twice the
runner-up's overlapping rows (an academy squad sharing a few registered players with the main team overlaps on far
fewer rows), and a compatible name (normalized name, acronym or slug) unless STRONG_ALIAS_PLAYERS overlap. Names
alone never link teams.
A per-year alias lets a team that changed its PandaScore record between seasons map correctly in each.
A source identity is the source's own person id: an OE playerid, a Leaguepedia canonical player page (renames are
resolved by Leaguepedia's redirects), or, for OE rows without an id, the rows sharing league, team and normalized
handle (identity_kind league_team_handle). Because the person id spans both years, a player matched by evidence in one
year carries the other year's rows with it; a second identity claiming the same player stops that. Its status:
  HIGH_CONFIDENCE_MATCH  exactly one PandaScore player meets handle + team + window + role on at least one row, no row
                         is ambiguous, and no other id-bearing identity claims that player; or, when single-candidate
                         rows point to several players, one of them holds >= 95% and >= 20 of those rows and the real
                         names agree where both sources give one (a stray registration elsewhere is outvoted, recorded
                         in the reason);
  REVIEW_REQUIRED        a handle matches but evidence is missing, conflicting or shared (reason recorded);
  UNMATCHED              no PandaScore name matches the identity's handles, or the rows carry no handle.
Only HIGH identities write participation: <prefix>participation_by_year (every target year) and
<prefix>participation_2026; normalize_pandascore's player index reads match_participation_verified_2026 = 'verified'
from the 2026 tables. Nothing else sets that status, and no academy, reserve or roster status is inferred. Tables are
rebuilt on every run (idempotent); source and PandaScore tables are only read.
"""
import argparse
from collections import Counter, defaultdict
from datetime import date, timedelta
import json
from pathlib import Path
import re
import sqlite3
import sys
import unicodedata

import normalize_pandascore as norm

DATA_DIR = Path(__file__).resolve().parent / "data"
REPORTS = Path(__file__).resolve().parent / "reports"
TARGET_YEAR = 2026
YEARS = (2023, 2024, 2025, 2026)
WINDOW_SLACK_DAYS = 2
MIN_ALIAS_PLAYERS = 3
STRONG_ALIAS_PLAYERS = 4
# An identity whose evidence splits across provider players resolves only to a candidate holding >= DOMINANT_SHARE of
# its supporting rows and >= DOMINANT_MIN_ROWS of them (see dominant_candidate); otherwise it stays REVIEW_REQUIRED.
DOMINANT_SHARE = 0.95
DOMINANT_MIN_ROWS = 20
OE_TO_PS_ROLE = {"top": "top", "jng": "jun", "mid": "mid", "bot": "adc", "sup": "sup"}
LP_ROLES = {"top": "top", "jungle": "jng", "mid": "mid", "bot": "bot", "support": "sup"}
ID_KINDS = ("oe_playerid", "lp_page")
TEAM_NOISE = {"esports", "esport", "gaming", "team", "club"}
HIGH, REVIEW, UNMATCHED = "HIGH_CONFIDENCE_MATCH", "REVIEW_REQUIRED", "UNMATCHED"

SCHEMA = {
    "team_aliases": """oe_team_key TEXT NOT NULL, oe_team_names TEXT NOT NULL, ps_team_id TEXT, overlap_players INTEGER NOT NULL,
        overlap_rows INTEGER NOT NULL, runner_up_team_id TEXT, runner_up_players INTEGER NOT NULL, name_compatible INTEGER NOT NULL,
        status TEXT NOT NULL, reason TEXT NOT NULL, year INTEGER NOT NULL, PRIMARY KEY (oe_team_key, year)""",
    "identity_map": """oe_identity_key TEXT PRIMARY KEY, identity_kind TEXT NOT NULL, oe_player_id TEXT, oe_handles TEXT NOT NULL,
        oe_team_keys TEXT NOT NULL, leagues TEXT NOT NULL, rows INTEGER NOT NULL, rows_in_period INTEGER NOT NULL,
        ps_player_id TEXT, candidates TEXT NOT NULL, supporting_rows INTEGER NOT NULL, status TEXT NOT NULL, reason TEXT NOT NULL""",
    "row_matches": """game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, oe_identity_key TEXT, oe_team_key TEXT,
        candidates INTEGER NOT NULL, ps_player_id TEXT, handle_match INTEGER NOT NULL, team_alias_high INTEGER NOT NULL,
        window_match INTEGER NOT NULL, role_match INTEGER NOT NULL, row_status TEXT NOT NULL, identity_status TEXT NOT NULL,
        reason TEXT NOT NULL, PRIMARY KEY (game_id, participant_id)""",
    "participation_2026": """ps_player_id TEXT PRIMARY KEY, oe_identity_keys TEXT NOT NULL, games INTEGER NOT NULL,
        supporting_rows INTEGER NOT NULL, first_game_date TEXT NOT NULL, last_game_date TEXT NOT NULL, leagues TEXT NOT NULL,
        oe_team_keys TEXT NOT NULL, source_file_sha256 TEXT NOT NULL""",
    "participation_by_year": """ps_player_id TEXT NOT NULL, year INTEGER NOT NULL, oe_identity_keys TEXT NOT NULL, games INTEGER NOT NULL,
        first_game_date TEXT NOT NULL, last_game_date TEXT NOT NULL, leagues TEXT NOT NULL, oe_team_keys TEXT NOT NULL,
        PRIMARY KEY (ps_player_id, year)""",
    "parameters": "name TEXT PRIMARY KEY, value TEXT NOT NULL",
}


def norm_handle(name):
    if not name:
        return None
    folded = unicodedata.normalize("NFKC", name).casefold()
    return "".join(ch for ch in folded if ch.isalnum()) or None


def norm_team(name):
    if not name:
        return None
    plain = "".join(c for c in unicodedata.normalize("NFKD", name) if not unicodedata.combining(c)).casefold()
    words = [w for w in re.split(r"[^0-9a-z]+", plain) if w and w not in TEAM_NOISE]
    return "".join(words) or None


def strip_parenthetical(text):
    return re.sub(r"\s*\([^)]*\)\s*$", "", text or "").strip()


def day(stamp):
    try:
        return date.fromisoformat(stamp[:10]) if stamp else None
    except ValueError:
        return None


# ---------- PandaScore side ----------

def stage_touches(begin, end, years):
    """A stage touches a year when its dates overlap it; a stage without an end date counts for its start year only."""
    if begin is None:
        return False
    last = end or date(begin.year, 12, 31)
    return any(begin <= date(y, 12, 31) and last >= date(y, 1, 1) for y in years)


def ps_index(db, years=YEARS):
    """Provider-reported names, registrations with stage windows, catalogue roles and team names."""
    stages = {}
    for t, b, e in db.execute("SELECT tournament_id, begin_at, end_at FROM ps_tournaments"):
        if stage_touches(day(b), day(e), years):
            stages[t] = (day(b), day(e))
    registrations = defaultdict(set)
    for stage, team, player, role in db.execute("SELECT DISTINCT tournament_id, team_id, player_id, role FROM tournament_registrations"):
        if stage in stages:
            registrations[player].add((team, stage, *stages[stage], role))
    for stage, team, player, role in db.execute("SELECT tournament_id, team_id, player_id, role FROM ps_registrations"):
        if stage in stages:
            registrations[player].add((team, stage, *stages[stage], role))
    names = defaultdict(set)
    catalog_role, real_names = {}, {}
    for player, name, role, first, last in db.execute("SELECT player_id, name, role, first_name, last_name FROM ps_players"):
        catalog_role[player] = role
        real_names[player] = name_tokens(f"{first or ''} {last or ''}")
        if norm_handle(name):
            names[norm_handle(name)].add(player)
    for (payload,) in db.execute("""SELECT DISTINCT p.payload_json FROM record_payloads p JOIN record_observations o USING (payload_sha256)
                                    WHERE o.collection IN ('tournaments', 'teams')"""):
        record = json.loads(payload)
        embedded = [p for entry in record.get("expected_roster") or [] for p in (entry or {}).get("players") or []]
        embedded += record.get("players") or []
        for p in embedded:
            if isinstance(p, dict) and p.get("id") is not None and norm_handle(p.get("name")):
                names[norm_handle(p["name"])].add(str(p["id"]))
    team_names = defaultdict(set)
    for team, name, acronym, slug in db.execute("SELECT team_id, name, acronym, slug FROM ps_teams"):
        for value in (name, acronym, (slug or "").replace("-", " ")):
            if norm_team(value):
                team_names[team].add(norm_team(value))
    return {"registrations": registrations, "names": names, "catalog_role": catalog_role, "team_names": team_names,
            "real_names": real_names}


def name_tokens(text):
    """Letters-and-digits tokens of a real name, order-free ("Choi Yong-hyeok" -> {"choi", "yonghyeok"})."""
    plain = "".join(c for c in unicodedata.normalize("NFKD", text or "") if not unicodedata.combining(c)).casefold()
    return frozenset(t for t in re.split(r"[\s,]+", re.sub(r"[^\w\s,]", "", plain)) if t)


def dominant_candidate(u, ps):
    """(player, rows, total) when an identity's single-candidate rows point to several provider players but one holds
    at least DOMINANT_SHARE of them and DOMINANT_MIN_ROWS rows, no row is ambiguous, and the real name in the source
    person id ("Handle (Real Name)") agrees with that player's provider real name whenever both exist; else None."""
    if u["ambiguous"] or len(u["strong"]) < 2:
        return None
    total = sum(u["supporting"].values())
    player, rows = u["supporting"].most_common(1)[0]
    if rows < DOMINANT_MIN_ROWS or rows < DOMINANT_SHARE * total:
        return None
    page = (u.get("player_id") or "")
    suffix = re.search(r"\(([^)]*)\)\s*$", page)
    source_name = name_tokens(suffix.group(1)) if suffix else frozenset()
    provider_name = ps["real_names"].get(player, frozenset())
    if source_name and provider_name and not (source_name <= provider_name or provider_name <= source_name):
        return None
    return player, rows, total


def within(game_day, begin, end):
    slack = timedelta(days=WINDOW_SLACK_DAYS)
    if game_day is None or begin is None:
        return False
    return begin - slack <= game_day <= (end or date(begin.year, 12, 31)) + slack


def candidates_for(r, ps):
    found = set()
    for handle in r["handles"]:
        found |= ps["names"].get(handle, set())
    return found


# ---------- source rows ----------

def oe_rows(db):
    years = dict(db.execute("SELECT file_sha256, target_year FROM oe_source_files"))
    rows = []
    for game, participant, player_id, name, team_id, team_name, league, stamp, position, in_period, valid, file_sha in db.execute(
            """SELECT game_id, participant_id, oe_player_id, player_name, oe_team_id, team_name, league, game_date, position,
                      in_period, valid_position, file_sha256 FROM oe_player_games ORDER BY game_id, participant_id"""):
        handle = norm_handle(name)
        team_key = team_id or (f"name:{norm_team(team_name)}" if norm_team(team_name) else None)
        if player_id:
            unit, kind = player_id, "oe_playerid"
        elif handle and team_key:
            unit, kind = f"league_team_handle:{league}|{team_key}|{handle}", "league_team_handle"
        else:
            unit, kind = None, None
        rows.append({"game": game, "participant": participant, "player_id": player_id, "name": name, "handle": handle,
                     "handles": [handle] if handle else [], "team_key": team_key, "team_name": team_name, "league": league,
                     "day": day(stamp), "year": years.get(file_sha), "position": (position or "").strip().lower(),
                     "in_period": bool(in_period), "valid": bool(valid), "unit": unit, "kind": kind})
    return rows


def lp_rows(db, years=YEARS):
    """Leaguepedia scoreboard rows. A seat is (side, role number); participant id = (side - 1) * 5 + role number, the
    same 1..10 numbering as Oracle's Elixir. Rows without a valid seat are skipped (counted in statistics)."""
    rows = []
    for game, side, number, link, canonical, name, team, role, stamp, league in db.execute(
            """SELECT p.game_id, p.side, p.role_number, p.link, p.canonical_link, p.name, p.team, p.role,
                      coalesce(p.game_ts, g.game_ts), coalesce(g.league, p.overview_page)
               FROM lp_player_games p LEFT JOIN lp_games g USING (game_id) ORDER BY p.game_id, p.side, p.role_number"""):
        try:
            side_n, role_n = int(side), int(number)
        except (TypeError, ValueError):
            continue
        if side_n not in (1, 2) or not 1 <= role_n <= 5:
            continue
        page = canonical or link
        handles = []
        for value in (strip_parenthetical(page), strip_parenthetical(link), name):
            h = norm_handle(value)
            if h and h not in handles:
                handles.append(h)
        game_day = day(stamp)
        position = LP_ROLES.get((role or "").strip().lower(), (role or "").strip().lower())
        rows.append({"game": game, "participant": (side_n - 1) * 5 + role_n, "player_id": f"lp:{page}" if page else None,
                     "name": page or name, "handle": handles[0] if handles else None, "handles": handles,
                     "team_key": f"lp:team:{team}" if team else None, "team_name": team, "league": league, "day": game_day,
                     "year": game_day.year if game_day else None, "position": position,
                     "in_period": bool(game_day and game_day.year in years), "valid": position in OE_TO_PS_ROLE,
                     "unit": f"lp:{page}" if page else None, "kind": "lp_page" if page else None})
    return rows


# ---------- reconciliation ----------

def team_aliases(rows, ps):
    """{(team key, year): alias tuple} from roster overlap within each year."""
    overlap, overlap_rows, names_seen = defaultdict(lambda: defaultdict(set)), Counter(), defaultdict(set)
    for r in rows:
        if r["team_key"] is None or r["year"] is None:
            continue
        key = (r["team_key"], r["year"])
        if r["team_name"]:
            names_seen[key].add(r["team_name"])
        if not (r["in_period"] and r["unit"] and r["handles"]):
            continue
        teams = {team for player in candidates_for(r, ps) for team, _, begin, end, _ in ps["registrations"][player]
                 if within(r["day"], begin, end)}
        for team in teams:
            overlap[key][team].add(r["unit"])
            overlap_rows[(key, team)] += 1
    aliases = {}
    for key in sorted(names_seen.keys() | overlap.keys()):
        # Ranked by overlapping rows (player-games), then players: a main team's games overlap its own registration on
        # every seat, while an academy squad that shares a few registered players overlaps on one or two.
        ranked = sorted(overlap[key].items(), key=lambda kv: (-overlap_rows[(key, kv[0])], -len(kv[1]), kv[0])) if key in overlap else []
        names = sorted(names_seen[key])
        if not ranked:
            aliases[key] = (json.dumps(names, ensure_ascii=False), None, 0, 0, None, 0, 0, UNMATCHED, "no registered handle overlap")
            continue
        (team, units), runner = ranked[0], (ranked[1] if len(ranked) > 1 else (None, set()))
        top, second = len(units), len(runner[1])
        top_rows, second_rows = overlap_rows[(key, team)], overlap_rows[(key, runner[0])] if runner[0] else 0
        compatible = int(bool({norm_team(n) for n in names} & ps["team_names"].get(team, set())))
        problems = []
        if top < MIN_ALIAS_PLAYERS:
            problems.append(f"only {top} overlapping player(s)")
        if top_rows < 2 * second_rows:
            problems.append(f"runner-up overlaps {second} player(s) on {second_rows} row(s)")
        if not compatible and top < STRONG_ALIAS_PLAYERS:
            problems.append("names differ and overlap below strong threshold")
        aliases[key] = (json.dumps(names, ensure_ascii=False), team, top, overlap_rows[(key, team)], runner[0], second, compatible,
                        REVIEW if problems else HIGH, "; ".join(problems) or "roster overlap")
    return aliases


def row_evidence(r, ps, alias_team):
    """(strong candidates, best flags, reason) for one in-period row with a handle."""
    candidates = sorted(candidates_for(r, ps), key=int)
    strong, best, reason = [], (1, 0, 0, 0), None
    for player in candidates:
        regs = [g for g in ps["registrations"][player] if g[0] == alias_team] if alias_team else []
        in_window = [g for g in regs if within(r["day"], g[2], g[3])]
        wanted = OE_TO_PS_ROLE.get(r["position"])
        role_ok = r["valid"] and any(wanted == (g[4] or ps["catalog_role"].get(player)) for g in in_window)
        flags = (1, int(alias_team is not None), int(bool(in_window)), int(role_ok))
        best = max(best, flags)
        if in_window and role_ok:
            strong.append(player)
    if len(strong) > 1:
        reason = "several provider players share this handle on the mapped team and window"
    elif not strong:
        reason = ("team alias not high-confidence" if alias_team is None else
                  "not registered for the mapped team" if not any(g[0] == alias_team for p in candidates for g in ps["registrations"][p]) else
                  "game date outside every registration window" if not best[2] else
                  ("invalid OE position" if r["kind"] != "lp_page" else "invalid position") if not r["valid"] else
                  "role differs from registration")
    return strong, best, reason


def reconcile_rows(db, rows, prefix, source_label, years=YEARS):
    """The shared engine: writes <prefix>team_aliases, identity_map, row_matches, participation_2026,
    participation_by_year and parameters. Returns counts."""
    ps = ps_index(db, years)
    aliases = team_aliases(rows, ps)
    high_alias = {key: a[1] for key, a in aliases.items() if a[7] == HIGH}
    units = defaultdict(lambda: {"rows": [], "strong": set(), "ambiguous": 0, "candidates": set(), "supporting": Counter(),
                                 "reasons": Counter()})
    row_out = []
    for r in rows:
        status, reason, strong, flags, count = UNMATCHED, None, [], (0, 0, 0, 0), 0
        found = candidates_for(r, ps)
        if not r["in_period"]:
            reason = f"dated outside {r['year']}" if r["year"] else "dated outside the target years"
        elif r["unit"] is None:
            reason = "row has neither a source person id nor handle and team"
        elif not found:
            reason = "no PandaScore name matches the handle"
        else:
            strong, flags, reason = row_evidence(r, ps, high_alias.get((r["team_key"], r["year"])))
            count = len(found)
            status = HIGH if len(strong) == 1 else REVIEW
            reason = reason or "handle + team alias + registration window + role"
        if r["unit"] is not None:
            u = units[r["unit"]]
            u["kind"], u["player_id"] = r["kind"], r["player_id"]
            u["rows"].append(r)
            u["strong"].update(strong)
            u["ambiguous"] += len(strong) > 1
            u["candidates"].update(found)
            if len(strong) == 1:
                u["supporting"][strong[0]] += 1
            if status == REVIEW:
                u["reasons"][reason] += 1
        row_out.append([r["game"], r["participant"], r["unit"], r["team_key"], count, strong[0] if len(strong) == 1 else None,
                        flags[0], flags[1], flags[2], flags[3], status, None, reason])

    decisions = {}
    for key, u in units.items():
        dominant = dominant_candidate(u, ps)
        if len(u["strong"]) == 1 and not u["ambiguous"]:
            decisions[key] = [HIGH, next(iter(u["strong"])), "handle + team alias + registration window + role"]
        elif dominant:
            decisions[key] = [HIGH, dominant[0], f"dominant evidence: {dominant[1]} of {dominant[2]} supporting rows; real names agree or absent"]
        elif u["strong"]:
            decisions[key] = [REVIEW, None, f"evidence points to {len(u['strong'])} provider players" if len(u["strong"]) > 1
                              else "a row matches several provider players"]
        elif u["candidates"]:
            decisions[key] = [REVIEW, None, u["reasons"].most_common(1)[0][0] if u["reasons"] else "handle match only"]
        else:
            decisions[key] = [UNMATCHED, None, "no PandaScore name matches the identity's handles"]
    # One provider player per source person: two id-bearing identities claiming the same player are both held for
    # review. Rows without a source id may join an id claim only under one of that identity's own handles.
    claims = defaultdict(list)
    for key, d in decisions.items():
        if d[0] == HIGH:
            claims[d[1]].append(key)
    for player, keys in claims.items():
        with_id = [k for k in keys if units[k]["kind"] in ID_KINDS]
        if len(with_id) > 1:
            for k in keys:
                decisions[k] = [REVIEW, None, f"provider player {player} claimed by {len(with_id)} source person ids"
                                if units[with_id[0]]["kind"] != "oe_playerid" else f"provider player {player} claimed by {len(with_id)} OE playerids"]
        elif with_id:
            handles = {h for r in units[with_id[0]]["rows"] for h in r["handles"]}
            for k in keys:
                if k not in with_id and not {h for r in units[k]["rows"] for h in r["handles"]} <= handles:
                    decisions[k] = [REVIEW, None, "rows without a source id claim a player under another handle"]

    for row in row_out:
        row[11] = decisions[row[2]][0] if row[2] is not None else UNMATCHED
    identity_rows = []
    by_year = defaultdict(lambda: {"keys": set(), "games": set(), "days": [], "leagues": set(), "teams": set(), "supporting": 0})
    for key in sorted(units):
        u, (status, player, reason) = units[key], decisions[key]
        in_period = [r for r in u["rows"] if r["in_period"]]
        identity_rows.append((key, u["kind"], u["player_id"], json.dumps(sorted({r["name"] for r in u["rows"] if r["name"]}), ensure_ascii=False),
                              json.dumps(sorted({r["team_key"] for r in u["rows"] if r["team_key"]}), ensure_ascii=False),
                              json.dumps(sorted({r["league"] for r in u["rows"] if r["league"]}), ensure_ascii=False), len(u["rows"]),
                              len(in_period), player, json.dumps(sorted(u["candidates"], key=int)[:20]),
                              u["supporting"].get(player, 0) if player else 0, status, reason))
        if status != HIGH:
            continue
        for r in in_period:
            p = by_year[(player, r["year"])]
            p["keys"].add(key)
            p["games"].add(r["game"])
            if r["day"]:
                p["days"].append(r["day"].isoformat())
            if r["league"]:
                p["leagues"].add(r["league"])
            if r["team_key"]:
                p["teams"].add(r["team_key"])
        if any(r["year"] == TARGET_YEAR for r in in_period):
            by_year[(player, TARGET_YEAR)]["supporting"] += u["supporting"].get(player, 0)
    source = source_label(db)
    participation_rows = [(player, json.dumps(sorted(p["keys"]), ensure_ascii=False), len(p["games"]), p["supporting"], min(p["days"]),
                           max(p["days"]), json.dumps(sorted(p["leagues"]), ensure_ascii=False),
                           json.dumps(sorted(p["teams"]), ensure_ascii=False), source)
                          for (player, year), p in sorted(by_year.items(), key=lambda kv: int(kv[0][0])) if year == TARGET_YEAR and p["days"]]
    year_rows = [(player, year, json.dumps(sorted(p["keys"]), ensure_ascii=False), len(p["games"]), min(p["days"]), max(p["days"]),
                  json.dumps(sorted(p["leagues"]), ensure_ascii=False), json.dumps(sorted(p["teams"]), ensure_ascii=False))
                 for (player, year), p in sorted(by_year.items(), key=lambda kv: (int(kv[0][0]), kv[0][1])) if p["days"]]
    alias_rows = [(key[0], *values, key[1]) for key, values in sorted(aliases.items())]
    parameters = [("years", json.dumps(list(years))), ("participation_year", str(TARGET_YEAR)),
                  ("window_slack_days", str(WINDOW_SLACK_DAYS)), ("min_alias_players", str(MIN_ALIAS_PLAYERS)),
                  ("strong_alias_players", str(STRONG_ALIAS_PLAYERS)), ("source", source)]
    with db:
        for table, columns in SCHEMA.items():
            db.execute(f"DROP TABLE IF EXISTS {prefix}{table}")
            db.execute(f"CREATE TABLE {prefix}{table} ({columns})")
        for table, data in (("team_aliases", alias_rows), ("identity_map", identity_rows), ("row_matches", row_out),
                            ("participation_2026", participation_rows), ("participation_by_year", year_rows),
                            ("parameters", parameters)):
            if data:
                db.executemany(f"INSERT INTO {prefix}{table} VALUES ({','.join('?' * len(data[0]))})", data)
        norm.build_player_index(db)
    return {"rows": len(row_out), "identities": len(identity_rows), "participation": len(participation_rows),
            "participation_by_year": len(year_rows)}


def oe_source_label(db):
    """The newest file of the participation year (or the newest file at all), as before."""
    row = db.execute("SELECT file_sha256 FROM oe_source_files WHERE target_year = ? ORDER BY import_order DESC LIMIT 1",
                     (TARGET_YEAR,)).fetchone() or db.execute(
        "SELECT file_sha256 FROM oe_source_files ORDER BY import_order DESC LIMIT 1").fetchone()
    return row[0]


def lp_source_label(db):
    runs = [r[0] for r in db.execute("SELECT DISTINCT run_id FROM lp_collection_windows ORDER BY 1")]
    return "leaguepedia:" + ",".join(runs)


def reconcile(db, target_year=TARGET_YEAR, years=YEARS):
    """Oracle's Elixir rows -> xp_* tables."""
    return reconcile_rows(db, oe_rows(db), "xp_", oe_source_label, years)


def reconcile_leaguepedia(db, years=YEARS):
    """Leaguepedia rows -> lpx_* tables."""
    return reconcile_rows(db, lp_rows(db, years), "lpx_", lp_source_label, years)


# ---------- statistics ----------

def one(db, sql, *args):
    return db.execute(sql, args).fetchone()[0]


def counts(db, sql, *args):
    return {str(k): v for k, v in db.execute(sql, args).fetchall()}


def has_table(db, name):
    return bool(one(db, "SELECT count(*) FROM sqlite_master WHERE name = ?", name))


def ps_readiness(db, years=YEARS):
    """PandaScore-side facts the reconciliation will rely on (aggregates only)."""
    ps = ps_index(db, years)
    registered = set(ps["registrations"])
    by_player = defaultdict(set)
    for handle, players in ps["names"].items():
        for p in players:
            by_player[p].add(handle)
    shared = {h: ps_ids for h, ps_ids in ps["names"].items() if len(ps_ids & registered) > 1}
    return {"years": list(years),
            "registered_players_with_windows": len(registered),
            "registered_players_with_a_provider_name": sum(1 for p in registered if by_player.get(p)),
            "registered_players_with_several_provider_names": sum(1 for p in registered if len(by_player.get(p, ())) > 1),
            "normalized_handles_shared_by_registered_players": len(shared),
            "registered_players_on_shared_handles": len(set().union(*shared.values()) & registered) if shared else 0,
            "catalogue_handles_shared_by_several_players": sum(1 for ids in ps["names"].values() if len(ids) > 1),
            "teams_on_registrations": len({g[0] for regs in ps["registrations"].values() for g in regs}),
            "stage_windows": len({g[1] for regs in ps["registrations"].values() for g in regs})}


def anonymous_reasons(db, sql):
    """Reason counts with provider ids and row counts replaced by placeholders (aggregate reports carry no person ids)."""
    out = Counter()
    for reason, n in db.execute(sql):
        out[re.sub(r"\d+", "<n>", reason or "")] += n
    return dict(out)


def identity_statistics(db, prefix):
    """Aggregates over one source's reconciliation tables (no names, no person ids)."""
    return {
        "identities_by_status": counts(db, f"SELECT status, count(*) FROM {prefix}identity_map GROUP BY 1"),
        "identities_by_kind_and_status": counts(db, f"SELECT identity_kind || ':' || status, count(*) FROM {prefix}identity_map GROUP BY 1"),
        "rows_by_identity_status": counts(db, f"SELECT identity_status, count(*) FROM {prefix}row_matches GROUP BY 1"),
        "high_by_reason": anonymous_reasons(db, f"SELECT reason, count(*) FROM {prefix}identity_map WHERE status = 'HIGH_CONFIDENCE_MATCH' GROUP BY 1"),
        "review_reasons": anonymous_reasons(db, f"SELECT reason, count(*) FROM {prefix}identity_map WHERE status = 'REVIEW_REQUIRED' GROUP BY 1"),
        "unmatched_reasons": counts(db, f"SELECT reason, count(*) FROM {prefix}identity_map WHERE status = 'UNMATCHED' GROUP BY 1"),
        "team_aliases_by_status": counts(db, f"SELECT status, count(*) FROM {prefix}team_aliases GROUP BY 1"),
        "team_aliases_by_year_and_status": counts(db, f"SELECT year || ':' || status, count(*) FROM {prefix}team_aliases GROUP BY 1"),
        "matched_players_by_year": counts(db, f"SELECT year, count(*) FROM {prefix}participation_by_year GROUP BY 1"),
        "participation_verified_players": one(db, f"SELECT count(*) FROM {prefix}participation_2026"),
    }


def statistics(db):
    files = db.execute("""SELECT file_name, file_sha256, bytes, rows_total, player_rows, team_rows, other_rows, duplicate_rows,
                                 conflicting_duplicates, invalid_position_rows, out_of_period_rows, period_from, period_to,
                                 missing_columns_json, imported_at, target_year FROM oe_source_files
                          WHERE import_order IN (SELECT max(import_order) FROM oe_source_files GROUP BY target_year)
                          ORDER BY target_year""").fetchall()
    keys = ("file_name", "file_sha256", "bytes", "rows_total", "player_rows", "team_rows", "other_rows", "duplicate_rows",
            "conflicting_duplicates", "invalid_position_rows", "out_of_period_rows", "period_from", "period_to",
            "missing_columns", "imported_at", "year")
    source_files = []
    for row in files:
        info = dict(zip(keys, row))
        info["missing_columns"] = json.loads(info["missing_columns"])
        source_files.append(info)
    stat_nulls = {c: one(db, f'SELECT count(*) FROM oe_player_game_stats WHERE "{c}" IS NULL') for c in
                  [r[1] for r in db.execute("PRAGMA table_info(oe_player_game_stats)") if r[1] not in ("game_id", "participant_id")]}
    league_rows = db.execute("""SELECT g.league, count(*), count(DISTINCT m.oe_identity_key),
                                       sum(m.identity_status = 'HIGH_CONFIDENCE_MATCH'), sum(m.identity_status = 'REVIEW_REQUIRED'),
                                       sum(m.identity_status = 'UNMATCHED')
                                FROM oe_player_games g JOIN xp_row_matches m USING (game_id, participant_id)
                                WHERE g.in_period = 1 GROUP BY 1 ORDER BY 2 DESC""").fetchall()
    covered_teams = "SELECT ps_team_id FROM xp_team_aliases WHERE status = 'HIGH_CONFIDENCE_MATCH'"
    return {
        "source_file": source_files[-1] if source_files else None,
        "source_files_by_year": source_files,
        "key_collisions_between_years": one(db, "SELECT count(*) FROM oe_key_collisions") if has_table(db, "oe_key_collisions") else 0,
        "player_game_rows": one(db, "SELECT count(*) FROM oe_player_games"),
        "player_game_rows_in_period": one(db, "SELECT count(*) FROM oe_player_games WHERE in_period = 1"),
        "player_game_rows_by_year": counts(db, """SELECT f.target_year, count(*) FROM oe_player_games g JOIN oe_source_files f
                                                  USING (file_sha256) WHERE g.in_period = 1 GROUP BY 1"""),
        "games": one(db, "SELECT count(DISTINCT game_id) FROM oe_player_games WHERE in_period = 1"),
        "unique_oe_playerids": one(db, "SELECT count(DISTINCT oe_player_id) FROM oe_player_games WHERE oe_player_id IS NOT NULL"),
        "rows_without_oe_playerid": one(db, "SELECT count(*) FROM oe_player_games WHERE oe_player_id IS NULL"),
        "unique_handles": one(db, "SELECT count(DISTINCT lower(player_name)) FROM oe_player_games"),
        "unique_team_keys": one(db, "SELECT count(DISTINCT oe_team_key) FROM xp_row_matches WHERE oe_team_key IS NOT NULL"),
        "unique_competitions": one(db, "SELECT count(DISTINCT league) FROM oe_player_games"),
        **identity_statistics(db, "xp_"),
        "participation_verified_in_index": one(db, "SELECT count(*) FROM ps_player_index_2026 WHERE match_participation_verified_2026 = 'verified'"),
        "registered_without_confirmed_participation": {
            "total": one(db, "SELECT count(*) FROM ps_player_index_2026 WHERE match_participation_verified_2026 <> 'verified'"),
            "with_a_registered_team_covered_by_oe": one(db, f"""SELECT count(DISTINCT i.player_id) FROM ps_player_index_2026 i
                JOIN ps_registrations r ON r.player_id = i.player_id
                WHERE i.match_participation_verified_2026 <> 'verified' AND r.team_id IN ({covered_teams})"""),
            "note": "Not proof of reserve or never-fielded status: OE may lack the games, or the identity is under review."},
        "coverage_by_league": [dict(zip(("league", "rows", "identities", "rows_high", "rows_review", "rows_unmatched"), r)) for r in league_rows],
        "statistics_null_cells": stat_nulls,
        "pandascore_readiness": ps_readiness(db),
    }


def lp_statistics(db):
    """Aggregates for the Leaguepedia reconciliation (league names and counts only)."""
    league_rows = db.execute("""SELECT coalesce(g.league, '(none)'), substr(p.game_ts, 1, 4), count(*),
                                       sum(m.identity_status = 'HIGH_CONFIDENCE_MATCH'), sum(m.identity_status = 'REVIEW_REQUIRED'),
                                       sum(m.identity_status = 'UNMATCHED')
                                FROM lp_player_games p LEFT JOIN lp_games g USING (game_id)
                                JOIN lpx_row_matches m ON m.game_id = p.game_id
                                     AND m.participant_id = (CAST(p.side AS INTEGER) - 1) * 5 + CAST(p.role_number AS INTEGER)
                                GROUP BY 1, 2 ORDER BY 3 DESC""").fetchall()
    return {
        "source": "leaguepedia",
        "collection_windows": one(db, "SELECT count(*) FROM lp_collection_windows"),
        "incomplete_windows": one(db, "SELECT count(*) FROM lp_collection_windows WHERE complete = 0"),
        "games_by_year": counts(db, "SELECT substr(game_ts, 1, 4), count(*) FROM lp_games GROUP BY 1"),
        "player_rows_by_year": counts(db, "SELECT substr(game_ts, 1, 4), count(*) FROM lp_player_games GROUP BY 1"),
        "rows_without_valid_seat": one(db, """SELECT count(*) FROM lp_player_games WHERE side NOT IN ('1', '2')
                                              OR role_number NOT IN ('1', '2', '3', '4', '5') OR side IS NULL OR role_number IS NULL"""),
        "duplicated_seats": one(db, "SELECT coalesce(sum(duplicate_rows), 0) FROM lp_player_games"),
        "unique_player_pages": one(db, "SELECT count(DISTINCT coalesce(canonical_link, link)) FROM lp_player_games"),
        "rows_without_canonical_page": one(db, "SELECT count(*) FROM lp_player_games WHERE canonical_link IS NULL"),
        **identity_statistics(db, "lpx_"),
        "coverage_by_league_and_year": [dict(zip(("league", "year", "rows", "rows_high", "rows_review", "rows_unmatched"), r))
                                        for r in league_rows],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--source", choices=("oe", "leaguepedia", "all"), default="all")
    parser.add_argument("--stats", type=Path, default=REPORTS / "oracles_elixir_2026_stats.json")
    parser.add_argument("--lp-stats", type=Path, default=REPORTS / "leaguepedia_identity_stats.json")
    args = parser.parse_args()
    db = sqlite3.connect(args.db)
    code = 0
    try:
        if args.source in ("oe", "all"):
            has_source = has_table(db, "oe_source_files") and one(db, "SELECT count(*) FROM oe_source_files")
            if not has_source:
                readiness = ps_readiness(db)
                print("No Oracle's Elixir file imported: nothing to reconcile. PandaScore side:", json.dumps(readiness))
                args.stats.parent.mkdir(parents=True, exist_ok=True)
                args.stats.write_text(json.dumps({"status": "NO_SOURCE_FILE: no Oracle's Elixir CSV imported; no participation verified",
                                                  "pandascore_readiness": readiness}, indent=1) + "\n", encoding="utf-8", newline="\n")
                code = 2
            else:
                result = reconcile(db)
                stats = statistics(db)
                args.stats.parent.mkdir(parents=True, exist_ok=True)
                args.stats.write_text(json.dumps({"status": "AGGREGATE STATISTICS ONLY: league codes and counts; no person names, handles, person ids or team names"} | stats,
                                                 indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
                print("oe", json.dumps(result), f"verified participation: {stats['participation_verified_players']}; wrote {args.stats}")
        if args.source in ("leaguepedia", "all"):
            if not (has_table(db, "lp_player_games") and one(db, "SELECT count(*) FROM lp_player_games")):
                print("No Leaguepedia rows normalized (run fetch_leaguepedia_stats.py, then --normalize).")
                code = code or 2
            else:
                result = reconcile_leaguepedia(db)
                stats = lp_statistics(db)
                args.lp_stats.parent.mkdir(parents=True, exist_ok=True)
                args.lp_stats.write_text(json.dumps({"status": "AGGREGATE STATISTICS ONLY: league names and counts; no person names, handles, person ids or team names"} | stats,
                                                    indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
                print("leaguepedia", json.dumps(result), f"verified participation: {stats['participation_verified_players']}; wrote {args.lp_stats}")
        return code
    finally:
        db.close()


if __name__ == "__main__":
    sys.exit(main())
