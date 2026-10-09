"""Bounded PandaScore LoL ingestion: raw snapshots + source-keyed SQLite.

This is a research/staging tool, NOT a current-roster/active-player classifier.
Run with --max-pages 1 first. Needs PANDASCORE_TOKEN in environment/.env.
"""
import argparse
import hashlib
import json
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
             obj.get("updated_at"), fetched_at, fetched_at))
    db.commit()


def request_page(session, path, page, per_page, retries=3):
    params = {"page": page, "per_page": per_page}
    for attempt in range(retries + 1):
        try:
            response = session.get(BASE + path, params=params, timeout=30)
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
        return data
    raise RuntimeError("Retry limit exhausted")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--collections", nargs="+", choices=list(COLLECTIONS), default=["leagues", "series", "tournaments", "teams", "players"])
    parser.add_argument("--max-pages", type=int, default=1, help="Per collection. Explicitly increase after coverage review.")
    parser.add_argument("--per-page", type=int, default=100)
    parser.add_argument("--delay", type=float, default=4.0, help="Seconds between requests; 4 seconds ~900/hour under free-plan limit")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--snapshot-root", type=Path, default=DATA_DIR / "raw" / "pandascore")
    args = parser.parse_args()
    if args.max_pages < 1 or not 1 <= args.per_page <= 100 or args.delay < 4.0:
        parser.error("Use max-pages >= 1, per-page 1..100 and delay >= 4 seconds")

    load_dotenv()
    token = os.getenv("PANDASCORE_TOKEN", "").strip()
    if not token or token.startswith(PLACEHOLDER_PREFIX):
        parser.error("PANDASCORE_TOKEN missing. Put your own token in .env (see SETUP_VI.md); check with: python check_config.py")

    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    snapshot_dir = args.snapshot_root / run_id
    snapshot_dir.mkdir(parents=True, exist_ok=False)
    db = open_database(args.db)
    session = requests.Session()
    session.headers.update({"Authorization": f"Bearer {token}", "Accept": "application/json", "User-Agent": "ManagementGame-ResearchIngestion/0.1"})
    manifest = {"run_id": run_id, "started_at": now_iso(), "status": "running", "collections": {},
                "source": "PandaScore", "scope": "BOUNDED RAW API SNAPSHOT; NOT verified all-2026 or academy coverage"}
    try:
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
    except Exception as exc:
        manifest["status"] = "failed"
        manifest["error_type"] = type(exc).__name__
        print(f"ERROR: {type(exc).__name__}: {exc}", file=sys.stderr)
        raise
    finally:
        manifest["finished_at"] = now_iso()
        (snapshot_dir / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        session.close()
        db.close()
    print(f"Snapshots: {snapshot_dir}\nStaging database: {args.db}")

if __name__ == "__main__":
    main()
