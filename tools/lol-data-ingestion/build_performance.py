"""Normalized per-game player performance, 2023-2026, from every imported source (offline).

Inputs (staging database): Leaguepedia scoreboards (lp_games, lp_player_games and the lpx_* reconciliation), Oracle's
Elixir (oe_player_games, oe_player_game_stats, oe_team_games and the xp_* reconciliation), PandaScore stages and
registrations. Nothing is fetched and no source table is changed.

Tables written (rebuilt every run):
  pf_games         one row per source game: date, league, tournament, patch, length, both teams, winner, the
                   PandaScore stage both teams were registered for when the game was played (so competition and tier
                   come from evidence, never from a league name), completeness, and whether it is the primary record
                   of that real game (a game present in two sources is kept once, Oracle's Elixir first);
  pf_player_games  one row per player and game: role, champion, side, result, identity (source person id, status,
                   PandaScore id only for HIGH_CONFIDENCE_MATCH identities), the published statistics, and the lane
                   opponent's end-of-game gold, CS and damage. A statistic the source lacks stays NULL;
  pf_competitions  per source league and year: games, mapped PandaScore league and tier with the share of games that
                   support it, region, level;
  pf_team_identity per source team and year: the PandaScore team of the HIGH team alias, or NULL.
A game is complete when it has ten seats, five distinct roles a side, one winner, a length, and every box statistic.
Team damage is the sum of the side's five damage values; team deaths are the other side's kills.
"""
import argparse
from collections import Counter, defaultdict
from datetime import date, timedelta
import json
from pathlib import Path
import sqlite3
import sys

from reconcile_identities import HIGH, LP_ROLES, day, strip_parenthetical  # noqa: F401 (strip_parenthetical re-exported)

DATA_DIR = Path(__file__).resolve().parent / "data"
YEARS = (2023, 2024, 2025, 2026)
ROLES = ("top", "jng", "mid", "bot", "sup")
STAGE_SLACK = timedelta(days=2)
BOX = ("kills", "deaths", "assists", "gold", "cs", "damage", "vision_score")
STAT_COLUMNS = ("kills", "deaths", "assists", "team_kills", "team_deaths", "gold", "earned_gold", "team_gold", "cs", "damage",
                "team_damage", "vision_score", "wards_placed", "wards_killed", "control_wards", "gd10", "gd15", "xpd10",
                "xpd15", "csd10", "csd15", "kills15", "assists15", "deaths15", "first_blood", "opp_gold", "opp_cs", "opp_damage")

SCHEMA = {
    "pf_games": """game_key TEXT PRIMARY KEY, source TEXT NOT NULL, source_game_id TEXT NOT NULL, game_ts TEXT, game_date TEXT,
        year INTEGER, league TEXT, tournament TEXT, region TEXT, tournament_level TEXT, patch TEXT, game_length_min REAL,
        blue_team_key TEXT, red_team_key TEXT, blue_team_name TEXT, red_team_name TEXT, winner_side TEXT,
        blue_ps_team_id TEXT, red_ps_team_id TEXT, ps_stage_id TEXT, ps_league_id TEXT, ps_tier TEXT, seats INTEGER NOT NULL,
        complete INTEGER NOT NULL, champions_key TEXT, is_primary INTEGER NOT NULL, duplicate_of TEXT""",
    "pf_player_games": """row_key TEXT PRIMARY KEY, game_key TEXT NOT NULL, source TEXT NOT NULL, participant INTEGER NOT NULL,
        side TEXT NOT NULL, role TEXT, champion TEXT, win INTEGER, team_key TEXT, team_name TEXT, opp_team_key TEXT,
        identity_key TEXT, identity_kind TEXT, handle TEXT, identity_status TEXT, ps_player_id TEXT, """
        + ", ".join(f"{c} REAL" for c in STAT_COLUMNS),
    "pf_competitions": """source TEXT NOT NULL, league TEXT NOT NULL, year INTEGER NOT NULL, games INTEGER NOT NULL,
        mapped_games INTEGER NOT NULL, ps_league_id TEXT, ps_league_name TEXT, ps_tier TEXT, tier_share REAL, region TEXT,
        tournament_level TEXT, PRIMARY KEY (source, league, year)""",
    "pf_team_identity": """source TEXT NOT NULL, team_key TEXT NOT NULL, year INTEGER NOT NULL, team_name TEXT,
        ps_team_id TEXT, alias_status TEXT, PRIMARY KEY (source, team_key, year)""",
}


def has_rows(db, table):
    exists = db.execute("SELECT count(*) FROM sqlite_master WHERE name = ?", (table,)).fetchone()[0]
    return bool(exists and db.execute(f"SELECT count(*) FROM {table}").fetchone()[0])


# ---------- PandaScore stages: which competition a game belonged to ----------

def stage_index(db):
    """team id -> [(stage, begin, end, league id, tier)] for every registration."""
    stages = {t: (day(b), day(e), league, tier) for t, b, e, league, tier in db.execute(
        "SELECT tournament_id, begin_at, end_at, league_id, tier FROM ps_tournaments")}
    by_team = defaultdict(set)
    for stage, team in db.execute("SELECT DISTINCT tournament_id, team_id FROM ps_registrations"):
        if stage in stages:
            by_team[team].add((stage, *stages[stage]))
    return by_team


def game_stage(by_team, blue, red, game_day):
    """The PandaScore stage both teams were registered for when the game was played (the shortest such window when
    several qualify), else None."""
    if game_day is None or not blue or not red:
        return None

    def open_on(team):
        return {s for s in by_team.get(team, ()) if s[1] and s[1] - STAGE_SLACK <= game_day <= (s[2] or date(s[1].year, 12, 31)) + STAGE_SLACK}
    shared = open_on(blue) & open_on(red)
    if not shared:
        return None
    return min(shared, key=lambda s: ((s[2] or s[1]) - s[1], s[0]))


# ---------- sources ----------

def team_identity(db, prefix):
    if not has_rows(db, f"{prefix}team_aliases"):
        return {}
    return {(key, year): (ps_team if status == HIGH else None, status, json.loads(names)[0] if json.loads(names) else None)
            for key, names, ps_team, status, year in db.execute(
                f"SELECT oe_team_key, oe_team_names, ps_team_id, status, year FROM {prefix}team_aliases")}


def identities(db, prefix):
    if not has_rows(db, f"{prefix}identity_map"):
        return {}
    return {key: (kind, status, ps if status == HIGH else None) for key, kind, status, ps in db.execute(
        f"SELECT oe_identity_key, identity_kind, status, ps_player_id FROM {prefix}identity_map")}


def lp_source(db):
    """(games, seats) from Leaguepedia."""
    games, seats = {}, []
    for row in db.execute("""SELECT game_id, game_ts, league, overview_page, region, tournament_level, patch, game_length_min,
                                    team1, team2, win_team FROM lp_games"""):
        game, stamp, league, page, region, level, patch, length, team1, team2, winner = row
        games[game] = {"source": "leaguepedia", "game": game, "ts": stamp, "league": league or page, "tournament": page,
                       "region": region, "level": level, "patch": patch, "length": length,
                       "teams": {"Blue": team1, "Red": team2}, "winner": "Blue" if winner == team1 and winner else
                       "Red" if winner == team2 and winner else None}
    for row in db.execute("""SELECT game_id, side, role_number, coalesce(canonical_link, link), name, team, role, champion, player_win,
                                    kills, deaths, assists, gold, cs, damage_to_champions, vision_score, team_kills, team_gold
                             FROM lp_player_games"""):
        game, side, number, page, name, team, role, champion, win, *stats = row
        try:
            side_n, number_n = int(side), int(number)
        except (TypeError, ValueError):
            continue
        if side_n not in (1, 2) or not 1 <= number_n <= 5:
            continue
        kills, deaths, assists, gold, cs, damage, vision, team_kills, team_gold = stats
        seats.append({"source": "leaguepedia", "game": game, "participant": (side_n - 1) * 5 + number_n,
                      "side": "Blue" if side_n == 1 else "Red", "role": LP_ROLES.get((role or "").strip().lower()),
                      "champion": champion, "win": {"Yes": 1, "No": 0}.get(win), "team_key": f"lp:team:{team}" if team else None,
                      "team_name": team, "identity_key": f"lp:{page}" if page else None, "handle": strip_parenthetical(page) or name,
                      "stats": {"kills": kills, "deaths": deaths, "assists": assists, "gold": gold, "cs": cs, "damage": damage,
                                "vision_score": vision, "team_kills": team_kills, "team_gold": team_gold}})
    return games, seats


OE_STATS = {"kills": "kills", "deaths": "deaths", "assists": "assists", "team_kills": "teamkills", "team_deaths": "teamdeaths",
            "gold": "totalgold", "earned_gold": "earnedgold", "cs": "total_cs", "damage": "damagetochampions",
            "vision_score": "visionscore", "wards_placed": "wardsplaced", "wards_killed": "wardskilled",
            "control_wards": "controlwardsbought", "gd10": "golddiffat10", "gd15": "golddiffat15", "xpd10": "xpdiffat10",
            "xpd15": "xpdiffat15", "csd10": "csdiffat10", "csd15": "csdiffat15", "kills15": "killsat15",
            "assists15": "assistsat15", "deaths15": "deathsat15"}


def oe_source(db):
    """(games, seats) from Oracle's Elixir: player rows with their published statistics; team rows give the sides."""
    games, seats = {}, []
    for game, participant, league, stamp, side, team, team_id, result, length, patch in db.execute(
            """SELECT game_id, participant_id, league, game_date, side, team_name, oe_team_id, result, game_length_seconds, patch
               FROM oe_team_games WHERE in_period = 1"""):
        g = games.setdefault(game, {"source": "oracles_elixir", "game": game, "ts": stamp, "league": league, "tournament": league,
                                    "region": None, "level": None, "patch": patch, "teams": {}, "winner": None,
                                    "length": float(length) / 60 if length not in (None, "") else None})
        g["teams"][side] = team
        if result == "1":
            g["winner"] = side
    columns = [r[1] for r in db.execute("PRAGMA table_info(oe_player_game_stats)")]
    wanted = [c for c in OE_STATS.values() if c in columns]
    select = ", ".join(f's."{c}"' for c in wanted + [c for c in ("firstbloodkill", "firstbloodassist") if c in columns])
    for row in db.execute(f"""SELECT g.game_id, g.participant_id, g.side, g.position, g.champion, g.result, g.team_name, g.oe_team_id,
                                     g.oe_player_id, g.player_name, g.league, {select}
                              FROM oe_player_games g JOIN oe_player_game_stats s USING (game_id, participant_id)
                              WHERE g.in_period = 1 AND g.valid_position = 1"""):
        game, participant, side, position, champion, result, team, team_id, player_id, name, league, *values = row
        named = dict(zip(wanted + [c for c in ("firstbloodkill", "firstbloodassist") if c in columns], values))
        stats = {key: named.get(column) for key, column in OE_STATS.items()}
        if "firstbloodkill" in named:
            fb = [named.get("firstbloodkill"), named.get("firstbloodassist")]
            stats["first_blood"] = None if all(v is None for v in fb) else float(any(v for v in fb if v))
        team_key = team_id or (f"name:{team}" if team else None)
        identity = player_id or (f"league_team_handle:{league}|{team_key}|{name}" if name and team_key else None)
        seats.append({"source": "oracles_elixir", "game": game, "participant": participant, "side": side,
                      "role": position.strip().lower(), "champion": champion, "win": int(result) if result in ("0", "1") else None,
                      "team_key": team_key, "team_name": team, "identity_key": identity, "handle": name, "stats": stats})
    return games, seats


def oe_identity_key(seat, oe_ids):
    """xp_identity_map keys: OE playerid, or league_team_handle with the normalized handle."""
    from reconcile_identities import norm_handle, norm_team
    key = seat["identity_key"]
    if key in oe_ids:
        return key
    if key and key.startswith("league_team_handle:"):
        league, team_key, name = key[len("league_team_handle:"):].split("|", 2)
        team_key = team_key if not team_key.startswith("name:") else f"name:{norm_team(team_key[5:])}"
        return f"league_team_handle:{league}|{team_key}|{norm_handle(name)}"
    return key


# ---------- build ----------

def build(db, years=YEARS):
    sources = []
    if has_rows(db, "oe_player_games"):
        sources.append(("oracles_elixir", "xp_", *oe_source(db)))
    if has_rows(db, "lp_player_games"):
        sources.append(("leaguepedia", "lpx_", *lp_source(db)))
    by_team = stage_index(db)
    leagues = dict(db.execute("SELECT league_id, name FROM ps_leagues"))
    game_rows, seat_rows, team_rows = [], [], []
    competition = defaultdict(lambda: {"games": 0, "mapped": 0, "leagues": Counter(), "tiers": Counter(), "regions": Counter(),
                                       "levels": Counter()})
    seen_champions = {}
    for source, prefix, games, seats in sources:
        aliases, ids = team_identity(db, prefix), identities(db, prefix)
        for (team_key, year), (ps_team, status, name) in aliases.items():
            team_rows.append((source, team_key, year, name, ps_team, status))
        by_game = defaultdict(list)
        for seat in seats:
            by_game[seat["game"]].append(seat)
        for game_id, g in sorted(games.items()):
            game_day = day(g["ts"])
            if game_day is None or game_day.year not in years:
                continue
            members = by_game.get(game_id, [])
            sides = {"Blue": [s for s in members if s["side"] == "Blue"], "Red": [s for s in members if s["side"] == "Red"]}
            team_keys = {side: next((s["team_key"] for s in rows if s["team_key"]), None) for side, rows in sides.items()}
            if g["source"] == "leaguepedia":
                team_keys = {side: (f"lp:team:{g['teams'].get(side)}" if g["teams"].get(side) else team_keys[side]) for side in sides}
            ps_teams = {side: (aliases.get((key, game_day.year)) or (None,))[0] for side, key in team_keys.items()}
            stage = game_stage(by_team, ps_teams["Blue"], ps_teams["Red"], game_day)
            champions = sorted(s["champion"] or "" for s in members)
            roles_ok = all(sorted(s["role"] or "" for s in rows) == sorted(ROLES) for rows in sides.values())
            wins = {side: {s["win"] for s in rows} for side, rows in sides.items()}
            winner = g["winner"]
            consistent = winner is not None and wins["Blue"] == {int(winner == "Blue")} and wins["Red"] == {int(winner == "Red")}
            complete = int(len(members) == 10 and roles_ok and consistent and g["length"] is not None and
                           all(s["stats"].get(k) is not None for s in members for k in BOX))
            champions_key = f"{game_day.isoformat()}|{'/'.join(champions)}" if len(members) == 10 and all(champions) else None
            game_key = f"{source}|{game_id}"
            duplicate_of = None
            if champions_key:
                for offset in (0, -1, 1):
                    probe = f"{(game_day + timedelta(days=offset)).isoformat()}|{'/'.join(champions)}"
                    if probe in seen_champions:
                        duplicate_of = seen_champions[probe]
                        break
                if duplicate_of is None:
                    seen_champions[champions_key] = game_key
            year = game_day.year
            entry = competition[(source, g["league"] or "(none)", year)]
            entry["games"] += 1
            if g["region"]:
                entry["regions"][g["region"]] += 1
            if g["level"]:
                entry["levels"][g["level"]] += 1
            if stage:
                entry["mapped"] += 1
                entry["leagues"][stage[3]] += 1
                entry["tiers"][stage[4] or "unknown"] += 1
            game_rows.append([game_key, source, game_id, g["ts"], game_day.isoformat(), year, g["league"], g["tournament"],
                              g["region"], g["level"], g["patch"], g["length"], team_keys["Blue"], team_keys["Red"],
                              g["teams"].get("Blue"), g["teams"].get("Red"), winner, ps_teams["Blue"], ps_teams["Red"],
                              stage[0] if stage else None, stage[3] if stage else None, stage[4] if stage else None,
                              len(members), complete, champions_key, int(duplicate_of is None), duplicate_of])
            lanes = {(s["side"], s["role"]): s for s in members}
            team_damage = {side: (sum(s["stats"]["damage"] for s in rows) if len(rows) == 5 and all(s["stats"].get("damage") is not None for s in rows) else None)
                           for side, rows in sides.items()}
            team_kills = {side: (sum(s["stats"]["kills"] for s in rows) if len(rows) == 5 and all(s["stats"].get("kills") is not None for s in rows) else None)
                          for side, rows in sides.items()}
            for s in members:
                other = "Red" if s["side"] == "Blue" else "Blue"
                opponent = lanes.get((other, s["role"]))
                stats = dict(s["stats"])
                if stats.get("team_kills") is None:
                    stats["team_kills"] = team_kills[s["side"]]
                if stats.get("team_deaths") is None:
                    stats["team_deaths"] = team_kills[other]
                stats["team_damage"] = team_damage[s["side"]]
                for key, column in (("opp_gold", "gold"), ("opp_cs", "cs"), ("opp_damage", "damage")):
                    stats[key] = opponent["stats"].get(column) if opponent else None
                identity_key = oe_identity_key(s, ids) if source == "oracles_elixir" else s["identity_key"]
                kind, status, ps_player = ids.get(identity_key, (None, "UNMATCHED" if identity_key else None, None))
                seat_rows.append([f"{game_key}|{s['participant']}", game_key, source, s["participant"], s["side"], s["role"],
                                  s["champion"], s["win"], s["team_key"], s["team_name"], team_keys[other], identity_key, kind,
                                  s["handle"], status, ps_player] + [stats.get(c) for c in STAT_COLUMNS])
    competition_rows = []
    for (source, league, year), e in sorted(competition.items()):
        ps_league, league_count = (e["leagues"].most_common(1) or [(None, 0)])[0]
        tier, tier_count = (e["tiers"].most_common(1) or [(None, 0)])[0]
        competition_rows.append((source, league, year, e["games"], e["mapped"], ps_league, leagues.get(ps_league), tier,
                                 round(tier_count / e["mapped"], 3) if e["mapped"] else None,
                                 (e["regions"].most_common(1) or [(None,)])[0][0], (e["levels"].most_common(1) or [(None,)])[0][0]))
    with db:
        for table, columns in SCHEMA.items():
            db.execute(f"DROP TABLE IF EXISTS {table}")
            db.execute(f"CREATE TABLE {table} ({columns})")
        for table, rows in (("pf_games", game_rows), ("pf_player_games", seat_rows), ("pf_competitions", competition_rows),
                            ("pf_team_identity", team_rows)):
            if rows:
                db.executemany(f"INSERT INTO {table} VALUES ({','.join('?' * len(rows[0]))})", rows)
        db.execute("CREATE INDEX IF NOT EXISTS pf_player_games_game ON pf_player_games (game_key)")
        db.execute("CREATE INDEX IF NOT EXISTS pf_player_games_identity ON pf_player_games (identity_key)")
    return {"sources": [s[0] for s in sources], "games": len(game_rows), "seats": len(seat_rows),
            "primary_games": sum(r[25] for r in game_rows), "complete_games": sum(r[23] for r in game_rows),
            "games_with_stage": sum(1 for r in game_rows if r[19]), "competitions": len(competition_rows)}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    args = parser.parse_args()
    db = sqlite3.connect(args.db)
    try:
        print(json.dumps(build(db)))
    finally:
        db.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
