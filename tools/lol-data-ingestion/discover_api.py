"""Bounded PandaScore discovery: endpoint access, pagination, field coverage and 2026 filters.

Fetch mode spends at most MAX_REQUESTS requests, saves every response body unchanged under
data/raw/discovery/<run_id>/ (git-ignored) with a SHA-256 manifest, and stops on HTTP 429 or a low quota.
--analyze RUN_DIR re-reads a saved run offline and writes aggregate-only statistics (no names, no payloads).
Research tool added during API discovery; not part of the original starter.
"""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import sys
import time

import requests
from dotenv import load_dotenv

from sync_pandascore import BASE, DATA_DIR, PLACEHOLDER_PREFIX, now_iso

MAX_REQUESTS = 25
DELAY_SECONDS = 4.0
MIN_REMAINING = 50
YEAR_RANGE = "2026-01-01T00:00:00Z,2026-12-31T23:59:59Z"
KEPT_HEADERS = ("X-Page", "X-Per-Page", "X-Total", "Link", "X-Rate-Limit-Remaining", "Content-Type", "Date")
COLLECTIONS = {
    "leagues": "/lol/leagues",
    "series": "/lol/series",
    "tournaments": "/lol/tournaments",
    "teams": "/lol/teams",
    "players": "/lol/players",
    "matches_past": "/lol/matches/past",
}
ACADEMY_WORDS = ("academy", "youth", "challengers", "rookie", " ac", "junior")
BEFORE_2026 = "1970-01-01T00:00:00Z,2025-12-31T23:59:59Z"
AFTER_2026 = "2027-01-01T00:00:00Z,2099-12-31T23:59:59Z"


def probe_full_2026():
    """Scope probes for the full 2026 collection: do two range filters combine (AND), which match listing holds
    upcoming matches, and which stages, series and matches touch 2026 without starting in it. per_page=1 where only
    X-Total matters."""
    one, hundred = {"page": 1, "per_page": 1}, {"page": 1, "per_page": 100}
    return [
        ("matches-all-begin-2026", "/lol/matches", {"range[begin_at]": YEAR_RANGE} | one),
        ("matches-past-begin-2026", "/lol/matches/past", {"range[begin_at]": YEAR_RANGE} | one),
        ("matches-all-scheduled-2026", "/lol/matches", {"range[scheduled_at]": YEAR_RANGE} | one),
        ("matches-scheduled-2026-begin-before", "/lol/matches", {"range[scheduled_at]": YEAR_RANGE, "range[begin_at]": BEFORE_2026} | hundred),
        ("matches-begin-2026-scheduled-before", "/lol/matches", {"range[begin_at]": YEAR_RANGE, "range[scheduled_at]": BEFORE_2026} | hundred),
        ("matches-upcoming", "/lol/matches/upcoming", dict(hundred)),
        ("matches-running", "/lol/matches/running", dict(hundred)),
        ("tournaments-begin-and-end-2026", "/lol/tournaments", {"range[begin_at]": YEAR_RANGE, "range[end_at]": YEAR_RANGE} | one),
        ("tournaments-begin-before-end-2026", "/lol/tournaments", {"range[begin_at]": BEFORE_2026, "range[end_at]": YEAR_RANGE} | hundred),
        ("tournaments-span-2026", "/lol/tournaments", {"range[begin_at]": BEFORE_2026, "range[end_at]": AFTER_2026} | hundred),
        ("tournaments-upcoming", "/lol/tournaments/upcoming", dict(hundred)),
        ("tournaments-running", "/lol/tournaments/running", dict(hundred)),
        ("series-begin-2026", "/lol/series", {"range[begin_at]": YEAR_RANGE} | one),
        ("series-begin-before-end-2026", "/lol/series", {"range[begin_at]": BEFORE_2026, "range[end_at]": YEAR_RANGE} | hundred),
        ("series-span-2026", "/lol/series", {"range[begin_at]": BEFORE_2026, "range[end_at]": AFTER_2026} | hundred),
        ("series-running", "/lol/series/running", dict(hundred)),
        ("series-upcoming", "/lol/series/upcoming", dict(hundred)),
        ("players-sort-id", "/lol/players", {"sort": "id"} | one),
        ("teams-sort-id", "/lol/teams", {"sort": "id"} | one),
    ]


def probe_rosters(limit=5):
    """The per-stage roster endpoint for up to `limit` running or upcoming stages with teams, from the latest
    completed full-2026 run, to compare with the expected_roster embedded in the stage listing."""
    root = DATA_DIR / "raw" / "pandascore"
    runs = [p for p in sorted(root.glob("*")) if (p / "manifest.json").exists()
            and json.loads((p / "manifest.json").read_text(encoding="utf-8")).get("preset") == "full-2026"
            and json.loads((p / "manifest.json").read_text(encoding="utf-8")).get("status") == "completed"]
    if not runs:
        raise SystemExit("No completed full-2026 run to pick stages from")
    manifest = json.loads((runs[-1] / "manifest.json").read_text(encoding="utf-8"))
    stages = []
    for query in ("tournaments-running", "tournaments-upcoming"):
        for page in manifest["queries"][query]["pages"]:
            stages += [t["id"] for t in json.loads((runs[-1] / page["file"]).read_text(encoding="utf-8")) if t.get("teams")]
    return [(f"rosters-{sid}", f"/tournaments/{sid}/rosters", {}) for sid in sorted(set(stages))[:limit]]


PROBES = {"full-2026": probe_full_2026, "rosters": probe_rosters}


def plan():
    steps = [(f"{name}-p{page}", path, {"page": page, "per_page": 100})
             for name, path in COLLECTIONS.items() for page in (1, 2)]
    steps.append(("players-p2-bracket-params", "/lol/players", {"page[number]": 2, "page[size]": 100}))
    steps += [(f"matches_past-2026-p{page}", "/lol/matches/past",
               {"range[begin_at]": YEAR_RANGE, "sort": "begin_at,id", "page": page, "per_page": 100}) for page in (1, 2)]
    steps.append(("tournaments-2026-p1", "/lol/tournaments",
                  {"range[begin_at]": YEAR_RANGE, "sort": "begin_at,id", "page": 1, "per_page": 100}))
    steps.append(("series-2026-p1", "/lol/series", {"filter[year]": 2026, "page": 1, "per_page": 100}))
    steps.append(("players-active-p1", "/lol/players", {"filter[active]": "true", "page": 1, "per_page": 100}))
    return steps


def pick_roster_tournament(tournaments):
    """A started 2026 tournament with teams, preferring the highest tier."""
    now = datetime.now(timezone.utc).isoformat()
    candidates = [t for t in tournaments if t.get("teams") and (t.get("begin_at") or "9") <= now]
    rank = {"s": 0, "a": 1, "b": 2, "c": 3, "d": 4}
    candidates.sort(key=lambda t: (rank.get(t.get("tier"), 9), -len(t.get("teams") or [])))
    return candidates[0]["id"] if candidates else None


def fetch(token, steps=None, scope="BOUNDED API DISCOVERY (<= 2 pages per query); NOT a complete dataset"):
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    run_dir = DATA_DIR / "raw" / "discovery" / run_id
    run_dir.mkdir(parents=True, exist_ok=False)
    session = requests.Session()
    session.headers.update({"Authorization": f"Bearer {token}", "Accept": "application/json",
                            "User-Agent": "ManagementGame-ResearchIngestion/0.1"})
    manifest = {"run_id": run_id, "started_at": now_iso(), "status": "running", "source": "PandaScore",
                "scope": scope,
                "max_requests": MAX_REQUESTS, "requests": []}

    def get(name, path, params):
        if len(manifest["requests"]) >= MAX_REQUESTS:
            raise RuntimeError("request budget exhausted")
        if manifest["requests"]:
            time.sleep(DELAY_SECONDS)
        response = session.get(BASE + path, params=params, timeout=30)
        body = response.content
        (run_dir / f"{name}.json").write_bytes(body)
        try:
            data = response.json()
        except ValueError:
            data = None
        entry = {"name": name, "path": path, "params": params, "status": response.status_code,
                 "records": len(data) if isinstance(data, list) else None, "bytes": len(body),
                 "sha256": hashlib.sha256(body).hexdigest(), "file": f"{name}.json", "fetched_at": now_iso(),
                 "headers": {h: response.headers[h] for h in KEPT_HEADERS if h in response.headers}}
        manifest["requests"].append(entry)
        print(f"{name}: HTTP {entry['status']} records={entry['records']} X-Total={entry['headers'].get('X-Total')} "
              f"remaining={entry['headers'].get('X-Rate-Limit-Remaining')}", flush=True)
        return response.status_code, data, response.headers

    try:
        for name, path, params in steps or plan():
            status, data, headers = get(name, path, params)
            if status == 429:
                manifest["stopped_reason"] = "HTTP 429"
                break
            remaining = headers.get("X-Rate-Limit-Remaining")
            if remaining is not None and int(remaining) < MIN_REMAINING:
                manifest["stopped_reason"] = f"quota below {MIN_REMAINING}"
                break
            if name == "tournaments-2026-p1" and status == 200 and isinstance(data, list):
                target = pick_roster_tournament(data)
                manifest["roster_tournament_id"] = target
                if target is not None:
                    get("tournament-rosters-2026-sample", f"/tournaments/{target}/rosters", {})
        manifest["status"] = "completed"
    except Exception as exc:
        manifest["status"] = "failed"
        manifest["error_type"] = type(exc).__name__
        print(f"ERROR: {type(exc).__name__}: {str(exc).replace(token, '***')}", file=sys.stderr)
        raise
    finally:
        manifest["finished_at"] = now_iso()
        manifest["requests_spent"] = len(manifest["requests"])
        (run_dir / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        session.close()
    print(f"Discovery run: {run_dir} ({manifest['requests_spent']} requests)")
    return run_dir


# ---------- offline analysis ----------

def state(value):
    if value is None:
        return "null"
    if value in ([], {}, ""):
        return "empty"
    return "value"


def coverage(records, depth=2):
    """Per field path: value/empty/null/absent counts over the objects that could carry it."""
    seen, parents = {}, Counter()

    def walk(obj, prefix, level):
        parents[prefix] += 1
        for key, value in obj.items():
            path = prefix + key
            seen.setdefault(path, Counter())[state(value)] += 1
            if level < depth:
                if isinstance(value, dict):
                    walk(value, path + ".", level + 1)
                elif isinstance(value, list):
                    for item in value:
                        if isinstance(item, dict):
                            walk(item, path + "[].", level + 1)

    for record in records:
        if isinstance(record, dict):
            walk(record, "", 1)
    result = {}
    for path, counts in sorted(seen.items()):
        parent = path[: path.rfind(".") + 1] if "." in path else ""
        result[path] = {"value": counts["value"], "empty": counts["empty"], "null": counts["null"],
                        "absent": parents[parent] - sum(counts.values()), "of": parents[parent]}
    return result


def ordering(ids):
    if len(ids) < 2:
        return "n/a"
    if all(a < b for a, b in zip(ids, ids[1:])):
        return "ascending id"
    if all(a > b for a, b in zip(ids, ids[1:])):
        return "descending id"
    return "not id-ordered"


def year(value):
    return value[:4] if isinstance(value, str) and len(value) >= 4 else "null"


def size_bucket(n):
    return "0" if n == 0 else "1-4" if n < 5 else "5" if n == 5 else "6-7" if n < 8 else "8+"


def profile(name, rows):
    """Aggregate-only facts per dataset (no names or free text)."""
    c = lambda f: dict(Counter(f(r) for r in rows).most_common())
    if name.startswith("players"):
        return {"role": c(lambda r: r.get("role")), "active": c(lambda r: r.get("active")),
                "nationality_present": c(lambda r: r.get("nationality") is not None),
                "current_team_present": c(lambda r: r.get("current_team") is not None),
                "birthday_present": c(lambda r: r.get("birthday") is not None),
                "image_url_present": c(lambda r: r.get("image_url") is not None),
                "modified_at_year": c(lambda r: year(r.get("modified_at")))}
    if name.startswith("teams"):
        return {"players_list_size": c(lambda r: size_bucket(len(r.get("players") or []))),
                "location_present": c(lambda r: r.get("location") is not None),
                "acronym_present": c(lambda r: r.get("acronym") is not None),
                "name_has_academy_like_word_heuristic": c(
                    lambda r: any(w in (" " + (r.get("name") or "").lower()) for w in ACADEMY_WORDS)),
                "modified_at_year": c(lambda r: year(r.get("modified_at")))}
    if name.startswith("tournaments"):
        return {"begin_at_year": c(lambda r: year(r.get("begin_at"))), "tier": c(lambda r: r.get("tier")),
                "region": c(lambda r: r.get("region")), "type": c(lambda r: r.get("type")),
                "teams_count": c(lambda r: size_bucket(len(r.get("teams") or []))),
                "expected_roster_teams": c(lambda r: size_bucket(len(r.get("expected_roster") or []))),
                "distinct_league_ids": len({(r.get("league") or {}).get("id") for r in rows})}
    if name.startswith("series"):
        return {"year": c(lambda r: r.get("year")), "season": c(lambda r: r.get("season")),
                "begin_at_year": c(lambda r: year(r.get("begin_at"))),
                "distinct_league_ids": len({r.get("league_id") for r in rows})}
    if name.startswith("leagues"):
        return {"series_count": c(lambda r: size_bucket(len(r.get("series") or []))),
                "modified_at_year": c(lambda r: year(r.get("modified_at")))}
    if name.startswith("matches"):
        return {"begin_at_year": c(lambda r: year(r.get("begin_at"))), "status": c(lambda r: r.get("status")),
                "opponents_count": c(lambda r: len(r.get("opponents") or [])),
                "results_count": c(lambda r: len(r.get("results") or [])),
                "games_count": c(lambda r: len(r.get("games") or [])),
                "detailed_stats": c(lambda r: r.get("detailed_stats")),
                "winner_present": c(lambda r: r.get("winner_id") is not None),
                "tournament_tier": c(lambda r: (r.get("tournament") or {}).get("tier")),
                "distinct_league_ids": len({(r.get("league") or {}).get("id") for r in rows}),
                "distinct_tournament_ids": len({(r.get("tournament") or {}).get("id") for r in rows})}
    return {}


def roster_profile(data):
    if not isinstance(data, dict):
        return {"shape": type(data).__name__}
    rosters = data.get("rosters") or []
    players = [p for team in rosters if isinstance(team, dict) for p in (team.get("players") or [])]
    return {"top_level_keys": sorted(data), "type": data.get("type"), "rosters": len(rosters),
            "players_per_roster": dict(Counter(len(t.get("players") or []) for t in rosters if isinstance(t, dict))),
            "player_active": dict(Counter(p.get("active") for p in players)),
            "player_role": dict(Counter(p.get("role") for p in players)),
            "player_birthday_present": dict(Counter(p.get("birthday") is not None for p in players)),
            "roster_field_coverage": coverage(rosters),
            "player_field_coverage": coverage(players, depth=1)}


def analyze(run_dir, output):
    manifest = json.loads((run_dir / "manifest.json").read_text(encoding="utf-8"))
    loaded, integrity = {}, []
    for entry in manifest["requests"]:
        body = (run_dir / entry["file"]).read_bytes()
        integrity.append({"name": entry["name"], "sha256_ok": hashlib.sha256(body).hexdigest() == entry["sha256"]})
        loaded[entry["name"]] = json.loads(body) if body else None

    requests_out = []
    for entry in manifest["requests"]:
        data = loaded[entry["name"]]
        rows = data if isinstance(data, list) else []
        ids = [r.get("id") for r in rows if isinstance(r, dict)]
        link_rels = sorted({part.split('rel="')[1].rstrip('"') for part in entry["headers"].get("Link", "").split(",")
                            if 'rel="' in part})
        requests_out.append({k: entry[k] for k in ("name", "path", "params", "status", "records")} | {
            "x_page": entry["headers"].get("X-Page"), "x_per_page": entry["headers"].get("X-Per-Page"),
            "x_total": entry["headers"].get("X-Total"), "link_rels": link_rels,
            "rate_limit_remaining": entry["headers"].get("X-Rate-Limit-Remaining"),
            "null_ids": sum(i is None for i in ids), "duplicate_ids_in_page": len(ids) - len(set(ids)),
            "ordering": ordering([i for i in ids if i is not None]),
            "error_body": data if entry["status"] != 200 else None})

    pairs = {}
    for name in list(COLLECTIONS) + ["matches_past-2026"]:
        p1, p2 = loaded.get(f"{name}-p1"), loaded.get(f"{name}-p2")
        if isinstance(p1, list) and isinstance(p2, list):
            a, b = [r.get("id") for r in p1], [r.get("id") for r in p2]
            pairs[name] = {"page1": len(a), "page2": len(b), "overlap_ids": len(set(a) & set(b)),
                           "pages_differ": set(a) != set(b),
                           "page1_id_range": [min(a), max(a)] if a else None,
                           "page2_id_range": [min(b), max(b)] if b else None}
    p2, alt = loaded.get("players-p2"), loaded.get("players-p2-bracket-params")
    if isinstance(p2, list) and isinstance(alt, list):
        pairs["players-p2 vs page[number]/page[size]"] = {"same_ids_same_order": [r["id"] for r in p2] == [r["id"] for r in alt]}

    snapshot_compare = {}
    sync_root = DATA_DIR / "raw" / "pandascore"
    for run in sorted(p for p in sync_root.glob("*") if p.is_dir()) if sync_root.exists() else []:
        for coll in ("leagues", "teams", "players"):
            f = run / f"{coll}-page-0001.json"
            if f.exists() and isinstance(loaded.get(f"{coll}-p1"), list):
                old = {r["id"] for r in json.loads(f.read_bytes())}
                new = {r["id"] for r in loaded[f"{coll}-p1"]}
                snapshot_compare[f"{run.name}/{coll}"] = {"same_id_set_as_discovery_p1": old == new,
                                                          "overlap": len(old & new)}

    datasets = {}
    for name, data in loaded.items():
        if isinstance(data, list):
            datasets[name] = {"records": len(data), "profile": profile(name, data)}
    field_coverage = {}
    for coll in COLLECTIONS:
        rows = [r for n in (f"{coll}-p1", f"{coll}-p2") for r in (loaded.get(n) or []) if isinstance(loaded.get(n), list)]
        field_coverage[coll] = {"records": len(rows), "fields": coverage(rows)}
    for name in ("matches_past-2026-p1", "tournaments-2026-p1", "series-2026-p1", "players-active-p1"):
        if isinstance(loaded.get(name), list):
            field_coverage[name] = {"records": len(loaded[name]), "fields": coverage(loaded[name])}

    stats = {"run_id": manifest["run_id"], "manifest_status": manifest["status"],
             "stopped_reason": manifest.get("stopped_reason"), "requests_spent": len(manifest["requests"]),
             "roster_tournament_id": manifest.get("roster_tournament_id"),
             "raw_integrity": integrity, "requests": requests_out, "pagination": pairs,
             "sync_snapshot_vs_discovery": snapshot_compare, "datasets": datasets,
             "roster_sample": roster_profile(loaded.get("tournament-rosters-2026-sample")),
             "field_coverage": field_coverage}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(stats, indent=1, ensure_ascii=False, default=str), encoding="utf-8")
    print(f"Wrote aggregate statistics to {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--analyze", type=Path, metavar="RUN_DIR", help="offline: analyze a saved discovery run")
    parser.add_argument("--probe", choices=list(PROBES), help="live: run a scope probe plan and keep its raw run; no analysis")
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parent / "reports" / "api_discovery_2026_stats.json")
    args = parser.parse_args()
    if args.analyze:
        analyze(args.analyze, args.output)
        return
    load_dotenv()
    token = os.getenv("PANDASCORE_TOKEN", "").strip()
    if not token or token.startswith(PLACEHOLDER_PREFIX):
        parser.error("PANDASCORE_TOKEN missing. Put your own token in .env (see SETUP_VI.md)")
    if args.probe:
        fetch(token, PROBES[args.probe](), f"SCOPE PROBE {args.probe} (single pages); NOT a dataset")
        return
    run_dir = fetch(token)
    analyze(run_dir, args.output)


if __name__ == "__main__":
    main()
