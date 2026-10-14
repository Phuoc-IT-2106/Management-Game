"""Cross-provider identity reconciliation: Oracle's Elixir player-game rows -> PandaScore players (offline).

Evidence is combined, never a single signal:
  handle   the OE row's playername, normalized (NFKC, casefold, letters and digits only), equals a name PandaScore
           reported for the player: the catalogue name, or a name embedded in any observed stage roster or team listing
           (aliases the provider itself reported);
  team     the OE team maps to a PandaScore team through a HIGH_CONFIDENCE_MATCH team alias (below);
  window   the player is registered for that PandaScore team on a stage touching the year, and the stage's dates,
           widened by WINDOW_SLACK_DAYS, contain the game date. The provider's current team is never used: it is a
           snapshot of today, not evidence about January;
  role     the OE position (top/jng/mid/bot/sup) equals the role on that registration (or the catalogue role when the
           registration carries none).
Team aliases link an OE team (its teamid, or its normalized name when the file has no id) to a PandaScore team by
roster overlap only: distinct OE players whose handle is registered for that team in a window containing their game
dates. HIGH_CONFIDENCE_MATCH needs >= MIN_ALIAS_PLAYERS such players, at least twice the runner-up, and a compatible
name (normalized name, acronym or slug) unless STRONG_ALIAS_PLAYERS overlap. Names alone never link teams.
An OE identity is an OE playerid (OE's own person id), or, for rows without one, the rows sharing league, team and
normalized handle (identity_kind league_team_handle). Its status:
  HIGH_CONFIDENCE_MATCH  exactly one PandaScore player meets handle + team + window + role on at least one row, no row
                         is ambiguous, and no other OE playerid claims that player;
  REVIEW_REQUIRED        a handle matches but evidence is missing, conflicting or shared (reason recorded);
  UNMATCHED              no PandaScore name matches the identity's handles, or the rows carry no handle.
Only HIGH identities with rows dated in the target year write xp_participation_2026; normalize_pandascore's player
index then reads match_participation_verified_2026 = 'verified' from it. Nothing else sets that status, and no
academy, reserve or roster status is inferred. Tables are rebuilt on every run (idempotent); OE and PandaScore source
tables are only read.
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
WINDOW_SLACK_DAYS = 2
MIN_ALIAS_PLAYERS = 3
STRONG_ALIAS_PLAYERS = 4
OE_TO_PS_ROLE = {"top": "top", "jng": "jun", "mid": "mid", "bot": "adc", "sup": "sup"}
YEAR_RELEVANCE = ("begins_in_2026", "ends_in_2026", "spans_2026")
TEAM_NOISE = {"esports", "esport", "gaming", "team", "club"}
HIGH, REVIEW, UNMATCHED = "HIGH_CONFIDENCE_MATCH", "REVIEW_REQUIRED", "UNMATCHED"

SCHEMA = {
    "xp_team_aliases": """oe_team_key TEXT PRIMARY KEY, oe_team_names TEXT NOT NULL, ps_team_id TEXT, overlap_players INTEGER NOT NULL,
        overlap_rows INTEGER NOT NULL, runner_up_team_id TEXT, runner_up_players INTEGER NOT NULL, name_compatible INTEGER NOT NULL,
        status TEXT NOT NULL, reason TEXT NOT NULL""",
    "xp_identity_map": """oe_identity_key TEXT PRIMARY KEY, identity_kind TEXT NOT NULL, oe_player_id TEXT, oe_handles TEXT NOT NULL,
        oe_team_keys TEXT NOT NULL, leagues TEXT NOT NULL, rows INTEGER NOT NULL, rows_in_period INTEGER NOT NULL,
        ps_player_id TEXT, candidates TEXT NOT NULL, supporting_rows INTEGER NOT NULL, status TEXT NOT NULL, reason TEXT NOT NULL""",
    "xp_row_matches": """game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, oe_identity_key TEXT, oe_team_key TEXT,
        candidates INTEGER NOT NULL, ps_player_id TEXT, handle_match INTEGER NOT NULL, team_alias_high INTEGER NOT NULL,
        window_match INTEGER NOT NULL, role_match INTEGER NOT NULL, row_status TEXT NOT NULL, identity_status TEXT NOT NULL,
        reason TEXT NOT NULL, PRIMARY KEY (game_id, participant_id)""",
    "xp_participation_2026": """ps_player_id TEXT PRIMARY KEY, oe_identity_keys TEXT NOT NULL, games INTEGER NOT NULL,
        supporting_rows INTEGER NOT NULL, first_game_date TEXT NOT NULL, last_game_date TEXT NOT NULL, leagues TEXT NOT NULL,
        oe_team_keys TEXT NOT NULL, source_file_sha256 TEXT NOT NULL""",
    "xp_parameters": "name TEXT PRIMARY KEY, value TEXT NOT NULL",
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


def day(stamp):
    try:
        return date.fromisoformat(stamp[:10]) if stamp else None
    except ValueError:
        return None


# ---------- PandaScore side ----------

def ps_index(db):
    """Provider-reported names, registrations with stage windows, catalogue roles and team names."""
    stages = {t: (day(b), day(e)) for t, b, e, rel in db.execute(
        "SELECT tournament_id, begin_at, end_at, relevance_2026 FROM ps_tournaments") if rel in YEAR_RELEVANCE}
    registrations = defaultdict(set)
    for stage, team, player, role in db.execute("SELECT DISTINCT tournament_id, team_id, player_id, role FROM tournament_registrations"):
        if stage in stages:
            registrations[player].add((team, stage, *stages[stage], role))
    for stage, team, player, role in db.execute("SELECT tournament_id, team_id, player_id, role FROM ps_registrations"):
        if stage in stages:
            registrations[player].add((team, stage, *stages[stage], role))
    names = defaultdict(set)
    catalog_role = {}
    for player, name, role in db.execute("SELECT player_id, name, role FROM ps_players"):
        catalog_role[player] = role
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
    return {"registrations": registrations, "names": names, "catalog_role": catalog_role, "team_names": team_names}


def within(game_day, begin, end):
    slack = timedelta(days=WINDOW_SLACK_DAYS)
    if game_day is None or begin is None:
        return False
    return begin - slack <= game_day <= (end or date(TARGET_YEAR, 12, 31)) + slack


# ---------- reconciliation ----------

def oe_rows(db):
    rows = []
    for game, participant, player_id, name, team_id, team_name, league, stamp, position, in_period, valid in db.execute(
            """SELECT game_id, participant_id, oe_player_id, player_name, oe_team_id, team_name, league, game_date, position,
                      in_period, valid_position FROM oe_player_games ORDER BY game_id, participant_id"""):
        handle = norm_handle(name)
        team_key = team_id or (f"name:{norm_team(team_name)}" if norm_team(team_name) else None)
        if player_id:
            unit, kind = player_id, "oe_playerid"
        elif handle and team_key:
            unit, kind = f"league_team_handle:{league}|{team_key}|{handle}", "league_team_handle"
        else:
            unit, kind = None, None
        rows.append({"game": game, "participant": participant, "player_id": player_id, "name": name, "handle": handle,
                     "team_key": team_key, "team_name": team_name, "league": league, "day": day(stamp),
                     "position": (position or "").strip().lower(), "in_period": bool(in_period), "valid": bool(valid),
                     "unit": unit, "kind": kind})
    return rows


def team_aliases(rows, ps):
    overlap, overlap_rows, oe_names = defaultdict(lambda: defaultdict(set)), Counter(), defaultdict(set)
    for r in rows:
        if r["team_key"] is None:
            continue
        if r["team_name"]:
            oe_names[r["team_key"]].add(r["team_name"])
        if not (r["in_period"] and r["unit"] and r["handle"]):
            continue
        teams = {team for player in ps["names"].get(r["handle"], ()) for team, _, begin, end, _ in ps["registrations"][player]
                 if within(r["day"], begin, end)}
        for team in teams:
            overlap[r["team_key"]][team].add(r["unit"])
            overlap_rows[(r["team_key"], team)] += 1
    aliases = {}
    for key in sorted(oe_names.keys() | overlap.keys()):
        ranked = sorted(overlap[key].items(), key=lambda kv: (-len(kv[1]), kv[0])) if key in overlap else []
        names = sorted(oe_names[key])
        if not ranked:
            aliases[key] = (json.dumps(names, ensure_ascii=False), None, 0, 0, None, 0, 0, UNMATCHED, "no registered handle overlap")
            continue
        (team, units), runner = ranked[0], (ranked[1] if len(ranked) > 1 else (None, set()))
        top, second = len(units), len(runner[1])
        compatible = int(bool({norm_team(n) for n in names} & ps["team_names"].get(team, set())))
        problems = []
        if top < MIN_ALIAS_PLAYERS:
            problems.append(f"only {top} overlapping player(s)")
        if top < 2 * second:
            problems.append(f"runner-up overlaps {second}")
        if not compatible and top < STRONG_ALIAS_PLAYERS:
            problems.append("names differ and overlap below strong threshold")
        aliases[key] = (json.dumps(names, ensure_ascii=False), team, top, overlap_rows[(key, team)], runner[0], second, compatible,
                        REVIEW if problems else HIGH, "; ".join(problems) or "roster overlap")
    return aliases


def row_evidence(r, ps, alias_team):
    """(strong candidates, best flags, reason) for one in-period row with a handle."""
    candidates = sorted(ps["names"].get(r["handle"], ()), key=int)
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
                  "invalid OE position" if not r["valid"] else "role differs from registration")
    return strong, best, reason


def reconcile(db, target_year=TARGET_YEAR):
    ps = ps_index(db)
    rows = oe_rows(db)
    aliases = team_aliases(rows, ps)
    high_alias = {key: a[1] for key, a in aliases.items() if a[7] == HIGH}
    units = defaultdict(lambda: {"rows": [], "strong": set(), "ambiguous": 0, "candidates": set(), "supporting": Counter(),
                                 "reasons": Counter()})
    row_out = []
    for r in rows:
        status, reason, strong, flags, count = UNMATCHED, None, [], (0, 0, 0, 0), 0
        if not r["in_period"]:
            reason = f"dated outside {target_year}"
        elif r["unit"] is None:
            reason = "row has neither OE playerid nor handle and team"
        elif not ps["names"].get(r["handle"]):
            reason = "no PandaScore name matches the handle"
        else:
            strong, flags, reason = row_evidence(r, ps, high_alias.get(r["team_key"]))
            count = len(ps["names"][r["handle"]])
            status = HIGH if len(strong) == 1 else REVIEW
            reason = reason or "handle + team alias + registration window + role"
        if r["unit"] is not None:
            u = units[r["unit"]]
            u["kind"], u["player_id"] = r["kind"], r["player_id"]
            u["rows"].append(r)
            u["strong"].update(strong)
            u["ambiguous"] += len(strong) > 1
            u["candidates"].update(ps["names"].get(r["handle"], ()) if r["handle"] else ())
            if len(strong) == 1:
                u["supporting"][strong[0]] += 1
            if status == REVIEW:
                u["reasons"][reason] += 1
        row_out.append([r["game"], r["participant"], r["unit"], r["team_key"], count, strong[0] if len(strong) == 1 else None,
                        flags[0], flags[1], flags[2], flags[3], status, None, reason])

    decisions = {}
    for key, u in units.items():
        if len(u["strong"]) == 1 and not u["ambiguous"]:
            decisions[key] = [HIGH, next(iter(u["strong"])), "handle + team alias + registration window + role"]
        elif u["strong"]:
            decisions[key] = [REVIEW, None, f"evidence points to {len(u['strong'])} provider players" if len(u["strong"]) > 1
                              else "a row matches several provider players"]
        elif u["candidates"]:
            decisions[key] = [REVIEW, None, u["reasons"].most_common(1)[0][0] if u["reasons"] else "handle match only"]
        else:
            decisions[key] = [UNMATCHED, None, "no PandaScore name matches the identity's handles"]
    # One provider player per OE person: two OE playerids claiming the same player are both held for review. Rows
    # without an OE id may join an OE-id claim only under one of that identity's own handles.
    claims = defaultdict(list)
    for key, d in decisions.items():
        if d[0] == HIGH:
            claims[d[1]].append(key)
    for player, keys in claims.items():
        with_id = [k for k in keys if units[k]["kind"] == "oe_playerid"]
        if len(with_id) > 1:
            for k in keys:
                decisions[k] = [REVIEW, None, f"provider player {player} claimed by {len(with_id)} OE playerids"]
        elif with_id:
            handles = {r["handle"] for r in units[with_id[0]]["rows"]}
            for k in keys:
                if k not in with_id and not {r["handle"] for r in units[k]["rows"]} <= handles:
                    decisions[k] = [REVIEW, None, "rows without OE id claim a player under another handle"]

    for row in row_out:
        if row[2] is not None:
            row[11] = decisions[row[2]][0]
        else:
            row[11] = UNMATCHED
    identity_rows, participation = [], defaultdict(lambda: {"keys": set(), "games": set(), "days": [], "leagues": set(),
                                                            "teams": set(), "supporting": 0})
    for key in sorted(units):
        u, (status, player, reason) = units[key], decisions[key]
        in_period = [r for r in u["rows"] if r["in_period"]]
        identity_rows.append((key, u["kind"], u["player_id"], json.dumps(sorted({r["name"] for r in u["rows"] if r["name"]}), ensure_ascii=False),
                              json.dumps(sorted({r["team_key"] for r in u["rows"] if r["team_key"]})),
                              json.dumps(sorted({r["league"] for r in u["rows"] if r["league"]})), len(u["rows"]), len(in_period),
                              player, json.dumps(sorted(u["candidates"], key=int)[:20]),
                              u["supporting"].get(player, 0) if player else 0, status, reason))
        if status == HIGH and in_period:
            p = participation[player]
            p["keys"].add(key)
            p["games"].update(r["game"] for r in in_period)
            p["days"] += [r["day"].isoformat() for r in in_period if r["day"]]
            p["leagues"].update(r["league"] for r in in_period if r["league"])
            p["teams"].update(r["team_key"] for r in in_period if r["team_key"])
            p["supporting"] += u["supporting"].get(player, 0)
    source = db.execute("SELECT file_sha256 FROM oe_source_files ORDER BY import_order DESC LIMIT 1").fetchone()[0]
    participation_rows = [(player, json.dumps(sorted(p["keys"])), len(p["games"]), p["supporting"], min(p["days"]), max(p["days"]),
                           json.dumps(sorted(p["leagues"])), json.dumps(sorted(p["teams"])), source)
                          for player, p in sorted(participation.items(), key=lambda kv: int(kv[0]))]
    alias_rows = [(key, *values) for key, values in sorted(aliases.items())]
    parameters = [("target_year", str(target_year)), ("window_slack_days", str(WINDOW_SLACK_DAYS)),
                  ("min_alias_players", str(MIN_ALIAS_PLAYERS)), ("strong_alias_players", str(STRONG_ALIAS_PLAYERS)),
                  ("source_file_sha256", source)]
    with db:
        for table, columns in SCHEMA.items():
            db.execute(f"DROP TABLE IF EXISTS {table}")
            db.execute(f"CREATE TABLE {table} ({columns})")
        for table, data in (("xp_team_aliases", alias_rows), ("xp_identity_map", identity_rows), ("xp_row_matches", row_out),
                            ("xp_participation_2026", participation_rows), ("xp_parameters", parameters)):
            if data:
                db.executemany(f"INSERT INTO {table} VALUES ({','.join('?' * len(data[0]))})", data)
        norm.build_player_index(db)
    return {"rows": len(row_out), "identities": len(identity_rows), "participation": len(participation_rows)}


# ---------- statistics ----------

def one(db, sql, *args):
    return db.execute(sql, args).fetchone()[0]


def counts(db, sql, *args):
    return {str(k): v for k, v in db.execute(sql, args).fetchall()}


def ps_readiness(db):
    """PandaScore-side facts the reconciliation will rely on (aggregates only)."""
    ps = ps_index(db)
    registered = set(ps["registrations"])
    by_player = defaultdict(set)
    for handle, players in ps["names"].items():
        for p in players:
            by_player[p].add(handle)
    shared = {h: ps_ids for h, ps_ids in ps["names"].items() if len(ps_ids & registered) > 1}
    return {"registered_players_with_windows": len(registered),
            "registered_players_with_a_provider_name": sum(1 for p in registered if by_player.get(p)),
            "registered_players_with_several_provider_names": sum(1 for p in registered if len(by_player.get(p, ())) > 1),
            "normalized_handles_shared_by_registered_players": len(shared),
            "registered_players_on_shared_handles": len(set().union(*shared.values()) & registered) if shared else 0,
            "catalogue_handles_shared_by_several_players": sum(1 for ids in ps["names"].values() if len(ids) > 1),
            "teams_on_2026_registrations": len({g[0] for regs in ps["registrations"].values() for g in regs}),
            "stage_windows": len({g[1] for regs in ps["registrations"].values() for g in regs})}


def statistics(db):
    source = db.execute("""SELECT file_name, file_sha256, bytes, rows_total, player_rows, team_rows, other_rows, duplicate_rows,
                                  conflicting_duplicates, invalid_position_rows, out_of_period_rows, period_from, period_to,
                                  missing_columns_json, imported_at FROM oe_source_files ORDER BY import_order DESC LIMIT 1""").fetchone()
    keys = ("file_name", "file_sha256", "bytes", "rows_total", "player_rows", "team_rows", "other_rows", "duplicate_rows",
            "conflicting_duplicates", "invalid_position_rows", "out_of_period_rows", "period_from", "period_to",
            "missing_columns", "imported_at")
    file_info = dict(zip(keys, source))
    file_info["missing_columns"] = json.loads(file_info["missing_columns"])
    stat_nulls = {c: one(db, f'SELECT count(*) FROM oe_player_game_stats WHERE "{c}" IS NULL') for c in
                  [r[1] for r in db.execute("PRAGMA table_info(oe_player_game_stats)") if r[1] not in ("game_id", "participant_id")]}
    league_rows = db.execute("""SELECT g.league, count(*), count(DISTINCT m.oe_identity_key),
                                       sum(m.identity_status = 'HIGH_CONFIDENCE_MATCH'), sum(m.identity_status = 'REVIEW_REQUIRED'),
                                       sum(m.identity_status = 'UNMATCHED')
                                FROM oe_player_games g JOIN xp_row_matches m USING (game_id, participant_id)
                                WHERE g.in_period = 1 GROUP BY 1 ORDER BY 2 DESC""").fetchall()
    covered_teams = "SELECT ps_team_id FROM xp_team_aliases WHERE status = 'HIGH_CONFIDENCE_MATCH'"
    return {
        "source_file": file_info,
        "player_game_rows": one(db, "SELECT count(*) FROM oe_player_games"),
        "player_game_rows_in_period": one(db, "SELECT count(*) FROM oe_player_games WHERE in_period = 1"),
        "games": one(db, "SELECT count(DISTINCT game_id) FROM oe_player_games WHERE in_period = 1"),
        "unique_oe_playerids": one(db, "SELECT count(DISTINCT oe_player_id) FROM oe_player_games WHERE oe_player_id IS NOT NULL"),
        "rows_without_oe_playerid": one(db, "SELECT count(*) FROM oe_player_games WHERE oe_player_id IS NULL"),
        "unique_handles": one(db, "SELECT count(DISTINCT lower(player_name)) FROM oe_player_games"),
        "unique_team_keys": one(db, "SELECT count(DISTINCT oe_team_key) FROM xp_row_matches WHERE oe_team_key IS NOT NULL"),
        "unique_competitions": one(db, "SELECT count(DISTINCT league) FROM oe_player_games"),
        "identities_by_status": counts(db, "SELECT status, count(*) FROM xp_identity_map GROUP BY 1"),
        "identities_by_kind_and_status": counts(db, "SELECT identity_kind || ':' || status, count(*) FROM xp_identity_map GROUP BY 1"),
        "rows_by_identity_status": counts(db, "SELECT identity_status, count(*) FROM xp_row_matches GROUP BY 1"),
        "review_reasons": counts(db, "SELECT reason, count(*) FROM xp_identity_map WHERE status = 'REVIEW_REQUIRED' GROUP BY 1"),
        "unmatched_reasons": counts(db, "SELECT reason, count(*) FROM xp_identity_map WHERE status = 'UNMATCHED' GROUP BY 1"),
        "team_aliases_by_status": counts(db, "SELECT status, count(*) FROM xp_team_aliases GROUP BY 1"),
        "participation_verified_players": one(db, "SELECT count(*) FROM xp_participation_2026"),
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


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--stats", type=Path, default=REPORTS / "oracles_elixir_2026_stats.json")
    args = parser.parse_args()
    db = sqlite3.connect(args.db)
    try:
        has_source = one(db, "SELECT count(*) FROM sqlite_master WHERE name = 'oe_source_files'") and \
            one(db, "SELECT count(*) FROM oe_source_files")
        if not has_source:
            readiness = ps_readiness(db)
            print("No Oracle's Elixir file imported: nothing to reconcile. PandaScore side:", json.dumps(readiness))
            args.stats.parent.mkdir(parents=True, exist_ok=True)
            args.stats.write_text(json.dumps({"status": "NO_SOURCE_FILE: no Oracle's Elixir CSV imported; no participation verified",
                                              "pandascore_readiness": readiness}, indent=1) + "\n", encoding="utf-8", newline="\n")
            return 2
        result = reconcile(db)
        stats = statistics(db)
        args.stats.parent.mkdir(parents=True, exist_ok=True)
        args.stats.write_text(json.dumps({"status": "AGGREGATE STATISTICS ONLY: league codes and counts; no person names, handles, person ids or team names"} | stats,
                                         indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
        print(json.dumps(result), f"verified participation: {stats['participation_verified_players']}; wrote {args.stats}")
        return 0
    finally:
        db.close()


if __name__ == "__main__":
    sys.exit(main())
