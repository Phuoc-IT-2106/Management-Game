"""Bounded PandaScore LoL ingestion: raw snapshots + source-keyed SQLite.

This is a research/staging tool, NOT a current-roster/active-player classifier.
Run with --max-pages 1 first. Needs PANDASCORE_TOKEN in environment/.env.

Two modes:
  --collections ...   the starter's unfiltered listing of whole collections (default order, page cap);
  --preset NAME       a bounded filtered crawl: each query is filtered to a fixed window, sorted ascending
                      (so records created during the crawl cannot shift pages), paged to its X-Total, and its
                      collected row count is checked against X-Total in the manifest.
                        core-2026  the 2026 calibration crawl (leagues, 2026 series, stages and past matches);
                        full-2026  every league, team and player record, plus every series, stage and match that
                                   touches 2026 (past and upcoming), with running/upcoming listings as cross-checks.
Preset runs write their manifest after every page, so a stopped run (quota floor, request budget, error) can be
continued with --resume RUN_ID: finished queries are skipped and a stopped query continues at its next page.
"""
import argparse
import hashlib
import json
import math
import os
from datetime import datetime, timezone
from pathlib import Path
import sqlite3
import sys
import time

import requests
from dotenv import load_dotenv

BASE = "https://api.pandascore.co"
# Default outputs live next to this script (git-ignored data/), whatever the current directory is.
DATA_DIR = Path(__file__).resolve().parent / "data"
# Any PASTE_YOUR_... value from .env.example counts as "no token". Never put a real token in code.
PLACEHOLDER_PREFIX = "PASTE_YOUR_"
COLLECTIONS = {
    "leagues": "/lol/leagues",
    "series": "/lol/series",
    "tournaments": "/lol/tournaments",
    "teams": "/lol/teams",
    "players": "/lol/players",
    "matches": "/lol/matches/past",
}
MATCHES_ALL = "/lol/matches"
KEPT_HEADERS = ("X-Page", "X-Per-Page", "X-Total", "X-Rate-Limit-Remaining")
# Stop before the hourly quota runs dry, leaving room for other tools (discovery uses the same floor).
MIN_REMAINING = 50
YEAR_2026 = "2026-01-01T00:00:00Z,2026-12-31T23:59:59Z"
BEFORE_2026 = "1970-01-01T00:00:00Z,2025-12-31T23:59:59Z"
AFTER_2026 = "2027-01-01T00:00:00Z,2099-12-31T23:59:59Z"
# Each query: (query name, storage collection, query parameters[, path]). Windows are fixed; sorting is ascending and
# total. The path defaults to the collection's listing in COLLECTIONS. Two range filters combine as AND (scope probe
# 2026-10-13: begin before 2026 AND end in 2026 returned 1 stage, not the union).
PRESETS = {
    "core-2026": [
        ("leagues", "leagues", {"sort": "id"}),
        ("series-2026", "series", {"filter[year]": "2026", "sort": "id"}),
        ("tournaments-begin-2026", "tournaments", {"range[begin_at]": YEAR_2026, "sort": "begin_at,id"}),
        # Stages that started in late 2025 but ran into 2026 (overlap with the query above is deduplicated by id).
        ("tournaments-end-2026", "tournaments", {"range[end_at]": YEAR_2026, "sort": "begin_at,id"}),
        ("matches-2026", "matches", {"range[begin_at]": YEAR_2026, "sort": "begin_at,id"}),
    ],
    "full-2026": [
        ("leagues", "leagues", {"sort": "id"}),
        # Series and stages that touch 2026: start in it, start earlier and end in it, or run across all of it.
        ("series-begin-2026", "series", {"range[begin_at]": YEAR_2026, "sort": "id"}),
        ("series-from-2025", "series", {"range[begin_at]": BEFORE_2026, "range[end_at]": YEAR_2026, "sort": "id"}),
        ("series-across-2026", "series", {"range[begin_at]": BEFORE_2026, "range[end_at]": AFTER_2026, "sort": "id"}),
        # The provider's season label; it differs from the calendar year (a "2027" promotion series starts in 2026).
        ("series-year-2026", "series", {"filter[year]": "2026", "sort": "id"}),
        ("tournaments-begin-2026", "tournaments", {"range[begin_at]": YEAR_2026, "sort": "id"}),
        ("tournaments-from-2025", "tournaments", {"range[begin_at]": BEFORE_2026, "range[end_at]": YEAR_2026, "sort": "id"}),
        ("tournaments-across-2026", "tournaments", {"range[begin_at]": BEFORE_2026, "range[end_at]": AFTER_2026, "sort": "id"}),
        # Every match status (finished, cancelled, running, not started). scheduled_at is set on every match, while
        # begin_at is null on some cancelled ones, so the scheduled window is the wider one; the two checks below must
        # stay empty for it to cover the begin_at window too.
        ("matches-scheduled-2026", "matches", {"range[scheduled_at]": YEAR_2026, "sort": "id"}, MATCHES_ALL),
        ("matches-begin-2026-scheduled-before", "matches",
         {"range[begin_at]": YEAR_2026, "range[scheduled_at]": BEFORE_2026, "sort": "id"}, MATCHES_ALL),
        ("matches-begin-2026-scheduled-after", "matches",
         {"range[begin_at]": YEAR_2026, "range[scheduled_at]": AFTER_2026, "sort": "id"}, MATCHES_ALL),
        # Cross-checks: what the provider lists as running or upcoming must already be inside the windows above.
        ("series-running", "series", {"sort": "id"}, "/lol/series/running"),
        ("series-upcoming", "series", {"sort": "id"}, "/lol/series/upcoming"),
        ("tournaments-running", "tournaments", {"sort": "id"}, "/lol/tournaments/running"),
        ("tournaments-upcoming", "tournaments", {"sort": "id"}, "/lol/tournaments/upcoming"),
        ("matches-running", "matches", {"sort": "id"}, "/lol/matches/running"),
        ("matches-upcoming", "matches", {"sort": "id"}, "/lol/matches/upcoming"),
        # The whole catalogue, whatever the year: presence here says nothing about activity in 2026.
        ("teams", "teams", {"sort": "id"}),
        ("players", "players", {"sort": "id"}),
    ],
}


class StopCollection(Exception):
    """A resumable stop: quota floor, request budget or page cap. The manifest says where to continue."""


class CountingSession:
    """Wraps a requests.Session: counts every HTTP attempt (retries included) and refuses to go past a budget."""

    def __init__(self, session, budget=None):
        self.session, self.budget, self.count = session, budget, 0
        self.headers = session.headers

    def get(self, *args, **kwargs):
        if self.budget is not None and self.count >= self.budget:
            raise StopCollection(f"request budget of {self.budget} reached")
        self.count += 1
        return self.session.get(*args, **kwargs)

    def close(self):
        self.session.close()


def now_iso():
    return datetime.now(timezone.utc).isoformat()


def open_database(db_path: Path):
    db_path.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(db_path)
    db.execute("PRAGMA journal_mode=WAL")
    db.execute("""CREATE TABLE IF NOT EXISTS provider_records (
        provider TEXT NOT NULL,
        collection TEXT NOT NULL,
        external_id TEXT NOT NULL,
        payload_json TEXT NOT NULL,
        source_updated_at TEXT,
        first_seen_at TEXT NOT NULL,
        last_seen_at TEXT NOT NULL,
        PRIMARY KEY (provider, collection, external_id)
    )""")
    return db


def store_page(db, collection, objects, fetched_at):
    for obj in objects:
        if not isinstance(obj, dict) or obj.get("id") is None:
            raise ValueError(f"{collection}: record without stable provider id; refusing ambiguous merge")
        db.execute("""INSERT INTO provider_records
            (provider, collection, external_id, payload_json, source_updated_at, first_seen_at, last_seen_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(provider, collection, external_id) DO UPDATE SET
              payload_json=excluded.payload_json,
              source_updated_at=excluded.source_updated_at,
              last_seen_at=excluded.last_seen_at""",
            ("pandascore", collection, str(obj["id"]), json.dumps(obj, ensure_ascii=False, sort_keys=True),
             obj.get("updated_at") or obj.get("modified_at"), fetched_at, fetched_at))
    db.commit()


def request_page_with_headers(session, path, page, per_page, params=None, retries=3):
    """One page of a list endpoint and the paging/quota headers that came with it."""
    query = dict(params or {})
    query.update({"page": page, "per_page": per_page})
    for attempt in range(retries + 1):
        try:
            response = session.get(BASE + path, params=query, timeout=30)
        except (requests.Timeout, requests.ConnectionError):
            if attempt == retries:
                raise
            time.sleep(min(2 ** attempt, 16))
            continue
        if response.status_code in (401, 403):
            raise RuntimeError(f"HTTP {response.status_code}: invalid credentials or endpoint not in your plan")
        if response.status_code == 429:
            if attempt == retries:
                raise RuntimeError("HTTP 429: hourly quota exhausted; stop and schedule later")
            retry_after = response.headers.get("Retry-After", "")
            try:
                delay = min(max(int(retry_after), 1), 60)
            except ValueError:
                delay = min(2 ** attempt * 5, 60)
            time.sleep(delay)
            continue
        if 500 <= response.status_code < 600 and attempt < retries:
            time.sleep(min(2 ** attempt, 16))
            continue
        response.raise_for_status()
        data = response.json()
        if not isinstance(data, list):
            raise ValueError(f"Expected JSON list from {path}, got {type(data).__name__}")
        return data, {h: response.headers[h] for h in KEPT_HEADERS if h in response.headers}
    raise RuntimeError("Retry limit exhausted")


def request_page(session, path, page, per_page, retries=3):
    return request_page_with_headers(session, path, page, per_page, retries=retries)[0]


def header_int(headers, name):
    try:
        return int(headers.get(name, ""))
    except ValueError:
        return None


def page_ids(snapshot_dir, entry):
    """Every record id on the query's saved pages, in page order (duplicates kept)."""
    return [r.get("id") for p in entry["pages"] for r in json.loads((snapshot_dir / p["file"]).read_bytes())]


def crawl_query(session, db, snapshot_dir, name, collection, params, per_page, max_pages, delay, sleep=time.sleep,
                path=None, entry=None, on_page=None):
    """Pages one filtered, ascending query until its X-Total is collected (or the page cap / quota floor is hit).
    A partial `entry` from a stopped run continues at its next page; pages already saved are not fetched again.
    Returns the manifest entry: pages with hashes, rows, unique ids, x_total, whether paging ended and whether the
    collection is complete (unique ids equal to an X-Total that did not change during the crawl)."""
    path = path or COLLECTIONS[collection]
    entry = entry or {"collection": collection, "path": path, "params": params, "pages": [], "rows": 0, "x_total": None}
    for flag in ("stopped_for_quota", "truncated_by_page_limit"):
        entry.pop(flag, None)
    page = len(entry["pages"]) + 1
    while page <= max_pages:
        rows, headers = request_page_with_headers(session, path, page, per_page, params)
        fetched_at = now_iso()
        total = header_int(headers, "X-Total")
        if entry["x_total"] is None:
            entry["x_total"] = total
        elif total is not None and total != entry["x_total"]:
            # Page to the latest total, but a total that moved mid-crawl can hide a shifted record: not complete.
            entry["x_total_changed"] = True
            entry.setdefault("x_total_history", []).append({"page": page, "x_total": total})
            entry["x_total"] = total
        content = json.dumps(rows, ensure_ascii=False, indent=2).encode("utf-8")
        file = snapshot_dir / f"{name}-page-{page:04d}.json"
        file.write_bytes(content)
        store_page(db, collection, rows, fetched_at)
        remaining = header_int(headers, "X-Rate-Limit-Remaining")
        entry["pages"].append({"page": page, "count": len(rows), "sha256": hashlib.sha256(content).hexdigest(), "file": file.name,
                               "fetched_at": fetched_at, "x_total": total, "rate_limit_remaining": remaining})
        entry["rows"] += len(rows)
        print(f"{name}: page {page}, rows {len(rows)}, total {entry['rows']} of {entry['x_total']}, quota left {remaining}", flush=True)
        # With X-Total known, page to its last page even past a short page (the shortfall is then reported, not hidden);
        # without it, a short page is the end.
        expected_pages = math.ceil(entry["x_total"] / per_page) if entry["x_total"] is not None else None
        ended = (page >= expected_pages) if expected_pages is not None else len(rows) < per_page
        if ended:
            entry["ended"] = True
        if on_page:
            on_page(entry)
        if ended:
            break
        if remaining is not None and remaining < MIN_REMAINING:
            entry["stopped_for_quota"] = remaining
            break
        page += 1
        sleep(delay)
    ids = page_ids(snapshot_dir, entry)
    entry["unique_ids"] = len(set(ids))
    entry["duplicate_ids"] = len(ids) - len(set(ids))
    entry["complete"] = (entry["x_total"] is not None and entry["unique_ids"] == entry["x_total"] and entry.get("ended", False)
                         and not entry.get("x_total_changed", False))
    if not entry.get("ended") and page > max_pages:
        entry["truncated_by_page_limit"] = True
    return entry


def write_manifest(snapshot_dir, manifest):
    """Atomic replace, so a run killed mid-write still leaves the previous checkpoint readable."""
    temp = snapshot_dir / "manifest.json.tmp"
    temp.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    os.replace(temp, snapshot_dir / "manifest.json")


def run_preset(session, db, snapshot_dir, manifest, preset, per_page, max_pages, delay, sleep=None):
    """Runs (or continues) every query of a preset, checkpointing the manifest after each page. Raises StopCollection
    when the quota floor, the request budget or the page cap stops a query; the manifest then says where to resume."""
    sleep = sleep or time.sleep
    manifest.setdefault("queries", {})
    sessions = manifest.setdefault("sessions", [])
    sessions.append({"started_at": now_iso(), "requests": 0})
    spent_before = manifest.get("requests", 0)

    def checkpoint():
        sessions[-1]["requests"] = getattr(session, "count", 0)
        manifest["requests"] = spent_before + sessions[-1]["requests"]
        write_manifest(snapshot_dir, manifest)

    def on_page(name, entry):
        manifest["queries"][name] = entry
        checkpoint()

    first = True
    for name, collection, params, *path in PRESETS[preset]:
        entry = manifest["queries"].get(name)
        if entry and entry.get("ended"):
            continue
        if not first:
            sleep(delay)
        first = False
        try:
            entry = crawl_query(session, db, snapshot_dir, name, collection, params, per_page, max_pages, delay, sleep,
                                path=path[0] if path else None, entry=entry, on_page=lambda e, name=name: on_page(name, e))
        except StopCollection:
            checkpoint()
            raise
        manifest["queries"][name] = entry
        checkpoint()
        if entry.get("stopped_for_quota") is not None:
            raise StopCollection(f"quota floor ({entry['stopped_for_quota']} left) during {name}; resume next hour")
        if entry.get("truncated_by_page_limit"):
            raise StopCollection(f"page cap {max_pages} reached during {name}; resume with a higher --max-pages")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--collections", nargs="+", choices=list(COLLECTIONS), default=["leagues", "series", "tournaments", "teams", "players"])
    parser.add_argument("--preset", choices=list(PRESETS), help="Bounded filtered crawl; replaces --collections")
    parser.add_argument("--max-pages", type=int, default=1, help="Per collection or query. Explicitly increase after coverage review.")
    parser.add_argument("--per-page", type=int, default=100)
    parser.add_argument("--delay", type=float, default=4.0, help="Seconds between requests; 4 seconds ~900/hour under free-plan limit")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--snapshot-root", type=Path, default=DATA_DIR / "raw" / "pandascore")
    parser.add_argument("--max-requests", type=int, help="Preset runs: stop (resumable) after this many HTTP attempts this session")
    parser.add_argument("--resume", metavar="RUN_ID", help="Preset runs: continue a stopped run in its own snapshot directory")
    args = parser.parse_args()
    if args.max_pages < 1 or not 1 <= args.per_page <= 100 or args.delay < 4.0:
        parser.error("Use max-pages >= 1, per-page 1..100 and delay >= 4 seconds")
    if (args.resume or args.max_requests is not None) and not args.preset:
        parser.error("--resume and --max-requests apply to --preset runs")
    if args.max_requests is not None and args.max_requests < 1:
        parser.error("--max-requests must be at least 1")

    load_dotenv()
    token = os.getenv("PANDASCORE_TOKEN", "").strip()
    if not token or token.startswith(PLACEHOLDER_PREFIX):
        parser.error("PANDASCORE_TOKEN missing. Put your own token in .env (see SETUP_VI.md); check with: python check_config.py")

    if args.resume:
        snapshot_dir = args.snapshot_root / args.resume
        manifest = json.loads((snapshot_dir / "manifest.json").read_text(encoding="utf-8"))
        if manifest.get("preset") != args.preset:
            parser.error(f"Run {args.resume} is preset {manifest.get('preset')!r}, not {args.preset!r}")
        if manifest.get("status") == "completed":
            print(f"Run {args.resume} is already completed; nothing to fetch.")
            return
        run_id = manifest["run_id"]
        manifest["status"] = "running"
    else:
        run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        snapshot_dir = args.snapshot_root / run_id
        snapshot_dir.mkdir(parents=True, exist_ok=False)
        manifest = {"run_id": run_id, "started_at": now_iso(), "status": "running", "collections": {},
                    "source": "PandaScore", "scope": "BOUNDED RAW API SNAPSHOT; NOT verified all-2026 or academy coverage"}
    db = open_database(args.db)
    session = requests.Session()
    session.headers.update({"Authorization": f"Bearer {token}", "Accept": "application/json", "User-Agent": "ManagementGame-ResearchIngestion/0.1"})
    try:
        if args.preset:
            manifest["preset"] = args.preset
            session = CountingSession(session, args.max_requests)
            run_preset(session, db, snapshot_dir, manifest, args.preset, args.per_page, args.max_pages, args.delay)
            manifest.pop("stop_reason", None)
        else:
            for name in args.collections:
                collection_count = 0
                page_count = 0
                for page in range(1, args.max_pages + 1):
                    rows = request_page(session, COLLECTIONS[name], page, args.per_page)
                    observed_at = now_iso()
                    file = snapshot_dir / f"{name}-page-{page:04d}.json"
                    content = json.dumps(rows, ensure_ascii=False, indent=2).encode("utf-8")
                    file.write_bytes(content)
                    store_page(db, name, rows, observed_at)
                    manifest["collections"].setdefault(name, {"pages": [], "rows": 0})
                    manifest["collections"][name]["pages"].append({"page": page, "count": len(rows),
                        "sha256": hashlib.sha256(content).hexdigest(), "file": file.name})
                    manifest["collections"][name]["rows"] += len(rows)
                    page_count += 1
                    collection_count += len(rows)
                    print(f"{name}: page {page}, rows {len(rows)}, total {collection_count}", flush=True)
                    if len(rows) < args.per_page:
                        break
                    time.sleep(args.delay)
                if page_count == args.max_pages and rows and len(rows) == args.per_page:
                    manifest["collections"][name]["truncated_by_page_limit"] = True
                time.sleep(args.delay)
        manifest["status"] = "completed"
        manifest.pop("error_type", None)
    except StopCollection as stop:
        manifest["status"] = "partial"
        manifest["stop_reason"] = str(stop)
        print(f"STOPPED (resumable with --resume {run_id}): {stop}", file=sys.stderr)
    except Exception as exc:
        manifest["status"] = "failed"
        manifest["error_type"] = type(exc).__name__
        print(f"ERROR: {type(exc).__name__}: {str(exc).replace(token, '***')}", file=sys.stderr)
        raise
    finally:
        manifest["finished_at"] = now_iso()
        if args.preset and manifest.get("sessions"):
            manifest["sessions"][-1]["requests"] = session.count
            manifest["sessions"][-1]["finished_at"] = manifest["finished_at"]
            manifest["requests"] = sum(s["requests"] for s in manifest["sessions"])
        write_manifest(snapshot_dir, manifest)
        session.close()
        db.close()
    print(f"Snapshots: {snapshot_dir}\nStaging database: {args.db}")
    if manifest["status"] == "partial":
        sys.exit(3)

if __name__ == "__main__":
    main()
