"""Aggregate completeness, coverage and integrity statistics for a full-2026 collection run (offline).

Reads the run's manifest and raw pages (SHA-256 re-checked) and the normalized tables in data/staging.sqlite3
(run normalize_pandascore.py first). Writes reports/pandascore_full_collection_2026_stats.json: counts, shares and
status words only. No names, handles, person ids or team names leave the local data/ folder.

A collection is COMPLETE only when every query in its scope paged to its end, its unique ids equal its X-Total, the
total did not move mid-crawl and no id repeated; otherwise PARTIAL (with the reason). Date-window queries of one
collection are disjoint by construction (begins in 2026 / begins earlier and ends in 2026 / begins earlier and ends
later), so their X-Totals add up to the scope's provider total.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sqlite3

from snapshots import DATA_DIR, SNAPSHOT_ROOT, run_pages

REPORTS = Path(__file__).resolve().parent / "reports"
SCOPES = {
    "players": ["players"],
    "teams": ["teams"],
    "leagues": ["leagues"],
    "series_2026": ["series-begin-2026", "series-from-2025", "series-across-2026"],
    "series_year_label_2026": ["series-year-2026"],
    "tournaments_2026": ["tournaments-begin-2026", "tournaments-from-2025", "tournaments-across-2026"],
    "matches_2026": ["matches-scheduled-2026"],
}
MUST_BE_EMPTY = ["matches-begin-2026-scheduled-before", "matches-begin-2026-scheduled-after"]
CROSS_CHECKS = {"series-running": "series_2026", "series-upcoming": "series_2026", "tournaments-running": "tournaments_2026",
                "tournaments-upcoming": "tournaments_2026", "matches-running": "matches_2026", "matches-upcoming": "matches_2026"}
IN_2026 = "('begins_in_2026', 'ends_in_2026', 'spans_2026', 'scheduled_in_2026')"


def latest_full_run(root):
    runs = sorted(p for p in root.glob("*") if (p / "manifest.json").exists()
                  and json.loads((p / "manifest.json").read_text(encoding="utf-8")).get("preset") == "full-2026")
    if not runs:
        raise SystemExit("No full-2026 run found")
    return runs[-1]


def query_ids(run, manifest):
    ids = {}
    for query, _, _, file in run_pages(run):
        ids.setdefault(query, []).extend(r["id"] for r in json.loads(file.read_text(encoding="utf-8")))
    return {q: ids.get(q, []) for q in manifest["queries"]}


def completeness(run, manifest):
    ids = query_ids(run, manifest)
    queries = manifest["queries"]
    sha_ok = sum(hashlib.sha256(f.read_bytes()).hexdigest() == p["sha256"] for _, _, p, f in run_pages(run))
    pages = sum(1 for _ in run_pages(run))
    out = {}
    for scope, names in SCOPES.items():
        present = [n for n in names if n in queries]
        union = set().union(*(set(ids[n]) for n in present)) if present else set()
        totals = [queries[n].get("x_total") for n in present]
        problems = []
        for n in names:
            q = queries.get(n)
            if q is None:
                problems.append(f"{n}: not run")
                continue
            if not q.get("ended"):
                problems.append(f"{n}: paging did not reach the end")
            if q.get("x_total_changed"):
                problems.append(f"{n}: X-Total changed during the crawl")
            if q.get("duplicate_ids"):
                problems.append(f"{n}: {q['duplicate_ids']} repeated ids")
            if q.get("unique_ids") != q.get("x_total"):
                problems.append(f"{n}: {q.get('unique_ids')} unique ids vs X-Total {q.get('x_total')}")
        provider_total = sum(totals) if totals and None not in totals else None
        if provider_total is not None and len(union) != provider_total:
            problems.append(f"union {len(union)} vs summed X-Total {provider_total} (windows overlap or records moved)")
        out[scope] = {"queries": present, "provider_total": provider_total, "collected_unique": len(union),
                      "rows_fetched": sum(len(ids[n]) for n in present),
                      "requests": sum(len(queries[n]["pages"]) for n in present),
                      "status": "COMPLETE" if not problems else "PARTIAL", "problems": problems}
    checks = {}
    for n in MUST_BE_EMPTY:
        q = queries.get(n)
        checks[n] = {"x_total": q and q.get("x_total"), "requests": q and len(q["pages"]),
                     "ok": bool(q) and q.get("x_total") == 0 and q.get("ended", False)}
    for n, scope in CROSS_CHECKS.items():
        q = queries.get(n)
        if q is None:
            checks[n] = {"ok": False, "reason": "not run"}
            continue
        scope_ids = set().union(*(set(ids[m]) for m in SCOPES[scope] if m in ids))
        outside = set(ids[n]) - scope_ids
        checks[n] = {"listed": len(set(ids[n])), "outside_scope": len(outside), "requests": len(q["pages"]),
                     "complete": q.get("complete"), "ok": not outside and bool(q.get("complete"))}
    remaining = [p.get("rate_limit_remaining") for q in queries.values() for p in q["pages"] if p.get("rate_limit_remaining") is not None]
    return out, checks, {"pages": pages, "sha256_ok": sha_ok, "requests_in_manifest": manifest.get("requests"),
                         "sessions": manifest.get("sessions"), "status": manifest.get("status"),
                         "stop_reason": manifest.get("stop_reason"),
                         "rate_limit_remaining_first_last": [remaining[0], remaining[-1]] if remaining else None}


def one(db, sql, *args):
    return db.execute(sql, args).fetchone()[0]


def counter(db, sql, *args, top=None):
    rows = db.execute(sql, args).fetchall()
    items = sorted(((str(k), v) for k, v in rows), key=lambda kv: (-kv[1], kv[0]))
    if top and len(items) > top:
        return dict(items[:top]) | {"other": sum(v for _, v in items[top:])}
    return dict(items)


def database_stats(db):
    players = {
        "total": one(db, "SELECT count(*) FROM ps_players"),
        "in_latest_full_listing": one(db, "SELECT count(*) FROM ps_players WHERE in_latest_listing = 1"),
        "only_in_earlier_runs": one(db, "SELECT count(*) FROM ps_players WHERE in_latest_listing = 0"),
        "with_handle": one(db, "SELECT count(*) FROM ps_players WHERE coalesce(name, '') <> ''"),
        "with_first_and_last_name": one(db, "SELECT count(*) FROM ps_players WHERE coalesce(first_name,'') <> '' AND coalesce(last_name,'') <> ''"),
        "with_nationality": one(db, "SELECT count(*) FROM ps_players WHERE coalesce(nationality, '') <> ''"),
        "with_role": one(db, "SELECT count(*) FROM ps_players WHERE coalesce(role, '') <> ''"),
        "with_current_team": one(db, "SELECT count(*) FROM ps_players WHERE current_team_id IS NOT NULL"),
        "with_image_url": one(db, "SELECT count(*) FROM ps_players WHERE coalesce(image_url, '') <> ''"),
        "with_no_missing_tracked_field": one(db, "SELECT count(*) FROM ps_players WHERE missing_fields = ''"),
        "missing_field_counts": {f: one(db, "SELECT count(*) FROM ps_players WHERE ',' || missing_fields || ',' LIKE ?", f"%,{f},%")
                                 for f in ("name", "slug", "first_name", "last_name", "nationality", "role", "current_team", "image_url")},
        "role": counter(db, "SELECT coalesce(role, 'null'), count(*) FROM ps_players GROUP BY 1"),
        "nationality_top15": counter(db, "SELECT coalesce(nationality, 'null'), count(*) FROM ps_players GROUP BY 1", top=15),
        "provider_active_flag": counter(db, "SELECT coalesce(provider_active_flag, 'null'), count(*) FROM ps_players GROUP BY 1"),
        "fields_not_returned_by_endpoint": ["birthday", "age", "salary", "contract", "rating", "potential"],
    }
    index = {
        "players": one(db, "SELECT count(*) FROM ps_player_index_2026"),
        "in_provider_catalog": one(db, "SELECT count(*) FROM ps_player_index_2026 WHERE in_provider_catalog = 1"),
        "registered_in_2026": counter(db, "SELECT registered_in_2026, count(*) FROM ps_player_index_2026 GROUP BY 1"),
        "current_roster_verified_as_of_set": one(db, "SELECT count(*) FROM ps_player_index_2026 WHERE current_roster_verified_as_of IS NOT NULL"),
        "match_participation_verified_2026": counter(db, "SELECT match_participation_verified_2026, count(*) FROM ps_player_index_2026 GROUP BY 1"),
        "academy_verified": counter(db, "SELECT academy_verified, count(*) FROM ps_player_index_2026 GROUP BY 1"),
        "reserve_verified": counter(db, "SELECT reserve_verified, count(*) FROM ps_player_index_2026 GROUP BY 1"),
        "registered_stages_per_player": counter(db, """SELECT CASE WHEN registered_stages >= 5 THEN '5+' ELSE registered_stages END,
                                                       count(*) FROM ps_player_index_2026 GROUP BY 1"""),
        "provider_current_team_agrees_with_current_roster": one(db, """SELECT count(*) FROM ps_player_index_2026
            WHERE current_roster_team_id IS NOT NULL AND provider_current_team_id = current_roster_team_id"""),
        "registered_role": counter(db, f"""SELECT coalesce(r.role, 'null'), count(DISTINCT r.player_id) FROM ps_registrations r
            JOIN ps_tournaments t USING (tournament_id) WHERE t.relevance_2026 IN {IN_2026} GROUP BY 1"""),
    }
    team_2026 = f"""SELECT team_id FROM ps_tournament_teams JOIN ps_tournaments USING (tournament_id) WHERE relevance_2026 IN {IN_2026}
                    UNION SELECT opponent1_id FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND opponent1_id IS NOT NULL
                    UNION SELECT opponent2_id FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND opponent2_id IS NOT NULL"""
    has_cache = one(db, "SELECT count(*) FROM sqlite_master WHERE name = 'image_cache'")
    teams = {
        "total": one(db, "SELECT count(*) FROM ps_teams"),
        "in_latest_full_listing": one(db, "SELECT count(*) FROM ps_teams WHERE in_latest_listing = 1"),
        "with_provider_roster": one(db, "SELECT count(*) FROM ps_teams WHERE provider_player_count > 0"),
        "without_provider_roster": one(db, "SELECT count(*) FROM ps_teams WHERE provider_player_count = 0"),
        "provider_roster_size": counter(db, """SELECT CASE WHEN provider_player_count >= 8 THEN '8+' ELSE provider_player_count END,
                                               count(*) FROM ps_teams GROUP BY 1"""),
        "with_image_url": one(db, "SELECT count(*) FROM ps_teams WHERE coalesce(image_url, '') <> ''"),
        "with_dark_mode_image_url": one(db, "SELECT count(*) FROM ps_teams WHERE coalesce(dark_mode_image_url, '') <> ''"),
        "with_location": one(db, "SELECT count(*) FROM ps_teams WHERE coalesce(location, '') <> ''"),
        "appearing_in_2026_competitions": one(db, f"SELECT count(*) FROM ({team_2026})"),
        "appearing_in_2026_and_in_catalog": one(db, f"SELECT count(*) FROM ({team_2026}) x JOIN ps_teams t ON t.team_id = x.team_id"),
        "cached_logos": one(db, "SELECT count(*) FROM image_cache WHERE entity_type='team' AND status IN ('cached','unchanged')") if has_cache else 0,
    }
    competitions = {
        "leagues": one(db, "SELECT count(*) FROM ps_leagues"),
        "series_by_relevance": counter(db, "SELECT relevance_2026, count(*) FROM ps_series GROUP BY 1"),
        "series_year_label_2026": one(db, "SELECT count(*) FROM ps_series WHERE year_label = '2026'"),
        "series_year_label_2026_outside_calendar_2026": one(db, "SELECT count(*) FROM ps_series WHERE year_label = '2026' AND relevance_2026 = 'outside_2026'"),
        "series_in_2026_with_other_year_label": counter(db, f"SELECT year_label, count(*) FROM ps_series WHERE relevance_2026 IN {IN_2026} AND year_label <> '2026' GROUP BY 1"),
        "tournaments_by_relevance": counter(db, "SELECT relevance_2026, count(*) FROM ps_tournaments GROUP BY 1"),
        "tournaments_2026_by_tier": counter(db, f"SELECT coalesce(tier, 'null'), count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "tournaments_2026_by_region": counter(db, f"SELECT coalesce(region, 'null'), count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "tournaments_2026_by_type": counter(db, f"SELECT coalesce(type, 'null'), count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "tournaments_2026_with_winner": one(db, f"SELECT count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} AND winner_id IS NOT NULL"),
        "tournaments_2026_with_prizepool": one(db, f"SELECT count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} AND prizepool IS NOT NULL"),
        "tournaments_2026_with_country": one(db, f"SELECT count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} AND country IS NOT NULL"),
        "tournaments_2026_without_teams": one(db, f"SELECT count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} AND team_count = 0"),
        "tournament_team_roster_status": counter(db, f"""SELECT roster_status, count(*) FROM ps_tournament_teams JOIN ps_tournaments USING (tournament_id)
                                                         WHERE relevance_2026 IN {IN_2026} GROUP BY 1"""),
        "registrations_2026": one(db, f"SELECT count(*) FROM ps_registrations JOIN ps_tournaments USING (tournament_id) WHERE relevance_2026 IN {IN_2026}"),
        "registered_players_2026": one(db, f"SELECT count(DISTINCT player_id) FROM ps_registrations JOIN ps_tournaments USING (tournament_id) WHERE relevance_2026 IN {IN_2026}"),
        "registered_teams_2026": one(db, f"SELECT count(DISTINCT team_id) FROM ps_registrations JOIN ps_tournaments USING (tournament_id) WHERE relevance_2026 IN {IN_2026}"),
        "players_per_registered_team_stage": counter(db, f"""SELECT CASE WHEN roster_players >= 8 THEN '8+' ELSE roster_players END, count(*)
            FROM ps_tournament_teams JOIN ps_tournaments USING (tournament_id) WHERE relevance_2026 IN {IN_2026} GROUP BY 1"""),
        "matches_by_relevance": counter(db, "SELECT relevance_2026, count(*) FROM ps_matches GROUP BY 1"),
        "matches_2026_by_status": counter(db, f"SELECT coalesce(status, 'null'), count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "matches_2026_by_format": counter(db, f"SELECT match_type || ':' || number_of_games, count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "matches_2026_begin_at_null": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND begin_at IS NULL"),
        "matches_2026_rescheduled": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND rescheduled = 1"),
        "matches_2026_forfeit": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND forfeit = 1"),
        "matches_2026_draw": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND draw = 1"),
        "matches_2026_with_two_opponents": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND opponent1_id IS NOT NULL AND opponent2_id IS NOT NULL"),
        "matches_2026_with_winner": one(db, f"SELECT count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} AND winner_id IS NOT NULL"),
        "matches_2026_detailed_stats_flag": counter(db, f"SELECT coalesce(detailed_stats, 'null'), count(*) FROM ps_matches WHERE relevance_2026 IN {IN_2026} GROUP BY 1"),
        "games_2026": one(db, f"SELECT count(*) FROM ps_match_games g JOIN ps_matches m USING (match_id) WHERE m.relevance_2026 IN {IN_2026}"),
        "games_2026_by_status": counter(db, f"SELECT coalesce(g.status, 'null'), count(*) FROM ps_match_games g JOIN ps_matches m USING (match_id) WHERE m.relevance_2026 IN {IN_2026} GROUP BY 1"),
    }
    broken = {
        "tournament_league_not_in_leagues": one(db, "SELECT count(*) FROM ps_tournaments WHERE league_id IS NOT NULL AND league_id NOT IN (SELECT league_id FROM ps_leagues)"),
        "tournament_serie_not_in_series": one(db, "SELECT count(*) FROM ps_tournaments WHERE serie_id IS NOT NULL AND serie_id NOT IN (SELECT serie_id FROM ps_series)"),
        "series_league_not_in_leagues": one(db, "SELECT count(*) FROM ps_series WHERE league_id IS NOT NULL AND league_id NOT IN (SELECT league_id FROM ps_leagues)"),
        "match_tournament_not_in_tournaments": one(db, "SELECT count(*) FROM ps_matches WHERE tournament_id IS NOT NULL AND tournament_id NOT IN (SELECT tournament_id FROM ps_tournaments)"),
        "match_serie_not_in_series": one(db, "SELECT count(*) FROM ps_matches WHERE serie_id IS NOT NULL AND serie_id NOT IN (SELECT serie_id FROM ps_series)"),
        "match_league_not_in_leagues": one(db, "SELECT count(*) FROM ps_matches WHERE league_id IS NOT NULL AND league_id NOT IN (SELECT league_id FROM ps_leagues)"),
        "match_opponent_not_in_teams": one(db, """SELECT count(*) FROM (SELECT opponent1_id AS t FROM ps_matches UNION ALL SELECT opponent2_id FROM ps_matches)
                                                  WHERE t IS NOT NULL AND t NOT IN (SELECT team_id FROM ps_teams)"""),
        "registration_player_not_in_players": one(db, "SELECT count(DISTINCT player_id) FROM ps_registrations WHERE player_id NOT IN (SELECT player_id FROM ps_players)"),
        "registration_team_not_in_teams": one(db, "SELECT count(DISTINCT team_id) FROM ps_registrations WHERE team_id NOT IN (SELECT team_id FROM ps_teams)"),
        "player_current_team_not_in_teams": one(db, "SELECT count(*) FROM ps_players WHERE current_team_id IS NOT NULL AND current_team_id NOT IN (SELECT team_id FROM ps_teams)"),
        "team_member_not_in_players": one(db, """SELECT count(DISTINCT player_id) FROM team_membership_observations
            WHERE source = 'team.players' AND run_id = (SELECT max(run_id) FROM collection_runs) AND player_id NOT IN (SELECT player_id FROM ps_players)"""),
        "tournaments_2026_missing_league_or_serie_id": one(db, f"SELECT count(*) FROM ps_tournaments WHERE relevance_2026 IN {IN_2026} AND (league_id IS NULL OR serie_id IS NULL)"),
    }
    latest = one(db, "SELECT max(run_id) FROM collection_runs")
    membership = {
        "team_players_links": one(db, "SELECT count(*) FROM team_membership_observations WHERE run_id = ? AND source = 'team.players'", latest),
        "player_current_team_links": one(db, "SELECT count(*) FROM team_membership_observations WHERE run_id = ? AND source = 'player.current_team'", latest),
        "links_in_both_directions": one(db, """SELECT count(*) FROM team_membership_observations a JOIN team_membership_observations b
            ON a.run_id = b.run_id AND a.team_id = b.team_id AND a.player_id = b.player_id
            WHERE a.run_id = ? AND a.source = 'team.players' AND b.source = 'player.current_team'""", latest),
    }
    images = {
        "by_entity_field_state": counter(db, "SELECT entity_type || ':' || field || ':' || url_state, count(*) FROM ps_image_refs GROUP BY 1"),
        "hosts": counter(db, "SELECT coalesce(host, 'none'), count(*) FROM ps_image_refs WHERE url_state = 'value' GROUP BY 1"),
        "license_review_required": one(db, "SELECT count(*) FROM ps_image_refs WHERE license_status = 'LICENSE_REVIEW_REQUIRED'"),
        "cache": counter(db, "SELECT entity_type || ':' || status, count(*) FROM image_cache GROUP BY 1") if has_cache else {},
    }
    previous = db.execute("""SELECT run_id FROM collection_runs WHERE run_id < ? AND run_id IN
                             (SELECT run_id FROM tournament_registrations) ORDER BY run_id DESC LIMIT 1""", (latest,)).fetchone()
    history = None
    if previous:
        both = "(SELECT tournament_id FROM tournament_registrations WHERE run_id = ? INTERSECT SELECT tournament_id FROM tournament_registrations WHERE run_id = ?)"
        pair = "SELECT tournament_id, team_id, player_id FROM tournament_registrations WHERE run_id = ? AND tournament_id IN " + both
        history = {"previous_run": previous[0], "latest_run": latest,
                   "stages_in_both": one(db, f"SELECT count(*) FROM {both}", previous[0], latest),
                   "rows_only_in_previous": one(db, f"SELECT count(*) FROM ({pair} EXCEPT {pair})", previous[0], previous[0], latest, latest, previous[0], latest),
                   "rows_only_in_latest": one(db, f"SELECT count(*) FROM ({pair} EXCEPT {pair})", latest, previous[0], latest, previous[0], previous[0], latest),
                   "note": "Registration rows that changed between observations of the same stage. Observation dates are not transfer dates."}
    integrity = {"sqlite_integrity_check": one(db, "PRAGMA integrity_check"),
                 "duplicate_observations": one(db, """SELECT count(*) FROM (SELECT run_id, query, collection, external_id, count(*) c
                                                     FROM record_observations GROUP BY 1, 2, 3, 4 HAVING c > 1)"""),
                 "observation_payloads_missing": one(db, "SELECT count(*) FROM record_observations WHERE payload_sha256 NOT IN (SELECT payload_sha256 FROM record_payloads)"),
                 "runs": [dict(zip(("run_id", "status", "pages"), r)) for r in db.execute("SELECT run_id, status, pages FROM collection_runs ORDER BY 1")]}
    return {"players": players, "player_index_2026": index, "teams": teams, "competitions": competitions,
            "broken_references": broken, "provider_team_membership_latest_run": membership, "images": images,
            "registration_history": history, "database": integrity}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--snapshot-root", type=Path, default=SNAPSHOT_ROOT)
    parser.add_argument("--run", help="full-2026 run id (default: the latest)")
    parser.add_argument("--output", type=Path, default=REPORTS / "pandascore_full_collection_2026_stats.json")
    args = parser.parse_args()
    run = args.snapshot_root / args.run if args.run else latest_full_run(args.snapshot_root)
    manifest = json.loads((run / "manifest.json").read_text(encoding="utf-8"))
    scopes, checks, run_info = completeness(run, manifest)
    db = sqlite3.connect(Path(args.db).resolve().as_uri() + "?mode=ro", uri=True)
    stats = database_stats(db)
    db.close()
    report = {"format": "pandascore-full-collection-stats", "version": 1,
              "status": "AGGREGATE STATISTICS ONLY: no names, handles, person ids or team names",
              "run_id": manifest["run_id"], "started_at": manifest.get("started_at"), "finished_at": manifest.get("finished_at"),
              "run": run_info, "collections": scopes, "checks": checks} | stats
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    for scope, s in scopes.items():
        print(f"{scope}: {s['status']} provider {s['provider_total']} collected {s['collected_unique']} requests {s['requests']}"
              + (f" ({'; '.join(s['problems'])})" if s["problems"] else ""))
    print(f"checks ok: {sum(c['ok'] for c in checks.values())}/{len(checks)}; pages {run_info['pages']}, sha256 ok {run_info['sha256_ok']}; "
          f"requests {run_info['requests_in_manifest']}; wrote {args.output}")


if __name__ == "__main__":
    main()
