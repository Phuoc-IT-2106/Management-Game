"""Reference data from Leaguepedia Cargo for the 2023-2026 ratings: tournament placements and player birthdates.

  TournamentResults  per event and team: date, place, place number, phase, achievement and showmatch flags. Used for
                     achievements, credited to a player only for events in which they played for that team;
  Players            canonical player page -> birthdate (and primary nationality, retired flag), used for age,
                     potential and aging. Personal data: kept in the git-ignored database, used locally, never reported
                     per person.
Same safeguards as fetch_leaguepedia_stats.py: logged-in bot, paced, COUNT(*) before and after each window, page
total and unique-key checks, immutable pages with a SHA-256 manifest under data/raw/leaguepedia-reference/<run>/.
`--normalize` (offline) rebuilds lp_tournament_results and lp_player_birthdates.

  python fetch_leaguepedia_reference.py --since 2023 --cutoff 2026-10-14
  python fetch_leaguepedia_reference.py --normalize
"""
import argparse
from datetime import date, datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import sys

from fetch_leaguepedia_stats import PAGE, blank, write_json

DATA_DIR = Path(__file__).resolve().parent / "data"
RAW_ROOT = DATA_DIR / "raw" / "leaguepedia-reference"
RESULT_FIELDS = {"OverviewPage": "TR.OverviewPage", "Event": "TR.Event", "Tier": "TR.Tier", "Date": "TR.Date",
                 "Place": "TR.Place", "PlaceNumber": "TR.Place_Number", "Team": "TR.Team", "Phase": "TR.Phase",
                 "IsAchievement": "TR.IsAchievement", "Showmatch": "TR.Showmatch", "Qualified": "TR.Qualified",
                 "UniqueLine": "TR.UniqueLine"}
PLAYER_FIELDS = {"OverviewPage": "P.OverviewPage", "ID": "P.ID", "Birthdate": "P.Birthdate",
                 "NationalityPrimary": "P.NationalityPrimary", "IsRetired": "P.IsRetired", "Role": "P.Role"}


def queries(since, cutoff):
    out = []
    for year in range(since, cutoff.year + 1):
        end = min(date(year + 1, 1, 1), cutoff)
        out.append((f"results-{year}", "TournamentResults=TR", RESULT_FIELDS,
                    f"TR.Date >= '{year}-01-01' AND TR.Date < '{end.isoformat()}'", "TR.Date,TR.OverviewPage,TR.UniqueLine",
                    "UniqueLine"))
    out.append(("birthdates", "Players=P", PLAYER_FIELDS, "P.Birthdate IS NOT NULL", "P.OverviewPage", "OverviewPage"))
    return out


def collect(client, since, cutoff, root=RAW_ROOT, log=print):
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    run_dir = root / run_id
    run_dir.mkdir(parents=True, exist_ok=True)
    manifest = {"source": "leaguepedia", "run_id": run_id, "since": since, "cutoff": cutoff.isoformat(), "page_size": PAGE,
                "status": "running", "queries": {}}
    for name, tables, fields, where, order_by, key in queries(since, cutoff):
        for attempt in range(2):
            before = int(client.call(action="cargoquery", tables=tables, fields="COUNT(*)=n", where=where)["cargoquery"][0]["title"]["n"])
            pages, keys, offset, total = [], set(), 0, 0
            field_text = ",".join(f"{field}={alias}" for alias, field in fields.items())
            while True:
                body = client.call(action="cargoquery", tables=tables, fields=field_text, where=where, order_by=order_by,
                                   limit=PAGE, offset=offset)
                rows = [item["title"] for item in body.get("cargoquery", [])]
                file_name = f"{name}-page-{len(pages) + 1:04d}.json"
                pages.append({"page": len(pages) + 1, "rows": len(rows), "file": file_name, "sha256": write_json(run_dir / file_name, rows)})
                keys.update(r.get(key) for r in rows)
                total += len(rows)
                if len(rows) < PAGE:
                    break
                offset += PAGE
            after = int(client.call(action="cargoquery", tables=tables, fields="COUNT(*)=n", where=where)["cargoquery"][0]["title"]["n"])
            if before == after:
                break
        manifest["queries"][name] = {"tables": tables, "where": where, "count_before": before, "count_after": after, "rows": total,
                                     "unique_keys": len(keys), "pages": pages, "complete": before == after == total}
        log(f"{name}: count {before}, fetched {total}, unique {len(keys)}")
        manifest["requests"] = getattr(client, "requests", None)
        write_json(run_dir / "manifest.json", manifest)
    manifest["status"] = "completed" if all(q["complete"] for q in manifest["queries"].values()) else "incomplete"
    manifest["finished_at"] = datetime.now(timezone.utc).isoformat()
    write_json(run_dir / "manifest.json", manifest)
    return manifest


def normalize(db_path, root=RAW_ROOT):
    """Newest run wins per key; page hashes are checked."""
    results, births = {}, {}
    for manifest_path in sorted(root.glob("*/manifest.json")):
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        if manifest.get("status") not in ("completed", "incomplete"):
            continue
        for name, info in manifest["queries"].items():
            for page in info["pages"]:
                path = manifest_path.parent / page["file"]
                text = path.read_text(encoding="utf-8")
                if hashlib.sha256(text.encode("utf-8")).hexdigest() != page["sha256"]:
                    raise RuntimeError(f"{path} does not match its manifest hash")
                for row in json.loads(text):
                    if name.startswith("results"):
                        key = blank(row.get("UniqueLine")) or f"{row.get('OverviewPage')}|{row.get('Team')}|{row.get('Place')}"
                        results[key] = [key] + [blank(row.get(k)) for k in RESULT_FIELDS if k != "UniqueLine"] + [manifest["run_id"]]
                    else:
                        births[blank(row.get("OverviewPage"))] = [blank(row.get(k)) for k in PLAYER_FIELDS] + [manifest["run_id"]]
    db = sqlite3.connect(db_path)
    try:
        with db:
            db.execute("DROP TABLE IF EXISTS lp_tournament_results")
            db.execute("CREATE TABLE lp_tournament_results (unique_line TEXT PRIMARY KEY, overview_page TEXT, event TEXT, tier TEXT, "
                       "date TEXT, place TEXT, place_number TEXT, team TEXT, phase TEXT, is_achievement TEXT, showmatch TEXT, "
                       "qualified TEXT, run_id TEXT)")
            db.executemany("INSERT INTO lp_tournament_results VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)", list(results.values()))
            db.execute("DROP TABLE IF EXISTS lp_player_birthdates")
            db.execute("CREATE TABLE lp_player_birthdates (overview_page TEXT PRIMARY KEY, id TEXT, birthdate TEXT, nationality TEXT, "
                       "is_retired TEXT, role TEXT, run_id TEXT)")
            db.executemany("INSERT INTO lp_player_birthdates VALUES (?,?,?,?,?,?,?)", [v for k, v in births.items() if k])
        return {"results": len(results), "birthdates": len(births)}
    finally:
        db.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--since", type=int, default=2023)
    parser.add_argument("--cutoff", default=None, help="YYYY-MM-DD, exclusive; default today (UTC)")
    parser.add_argument("--delay", type=float, default=1.0)
    parser.add_argument("--normalize", action="store_true")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    args = parser.parse_args()
    if args.normalize:
        print(json.dumps(normalize(args.db)))
        return 0
    from dotenv import dotenv_values
    from fetch_leaguepedia_images import Leaguepedia
    env = {**dotenv_values(Path(__file__).resolve().parent / ".env"), **os.environ}
    username, password = env.get("LEAGUEPEDIA_BOT_USERNAME", "").strip(), env.get("LEAGUEPEDIA_BOT_PASSWORD", "").strip()
    if not username or not password:
        parser.error("LEAGUEPEDIA_BOT_USERNAME and LEAGUEPEDIA_BOT_PASSWORD must be set in .env")
    client = Leaguepedia(username, password, delay=args.delay)
    cutoff = date.fromisoformat(args.cutoff) if args.cutoff else datetime.now(timezone.utc).date()
    manifest = collect(client, args.since, cutoff, log=lambda t: print(t, flush=True))
    print(json.dumps({"run_id": manifest["run_id"], "status": manifest["status"], "requests": client.requests}))
    return 0 if manifest["status"] == "completed" else 3


if __name__ == "__main__":
    sys.exit(main())
