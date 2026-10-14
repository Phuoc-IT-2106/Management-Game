"""Read immutable raw PandaScore snapshots (data/raw/pandascore/<run>/) by collection.

The staging SQLite keeps only the latest payload per id; snapshots keep every page as fetched, so dated evidence
(tournament rosters, match results) is rebuilt from them. Each run's manifest says which file belongs to which
collection: `queries` for preset crawls, `collections` for the starter's listing mode. Only completed runs are read.
"""
import hashlib
import json
from pathlib import Path

DATA_DIR = Path(__file__).resolve().parent / "data"
SNAPSHOT_ROOT = DATA_DIR / "raw" / "pandascore"


def completed_runs(root: Path = SNAPSHOT_ROOT):
    """Run directories whose manifest says completed, oldest first."""
    runs = []
    for run in sorted(p for p in root.glob("*") if p.is_dir()):
        manifest = run / "manifest.json"
        if manifest.exists() and json.loads(manifest.read_text(encoding="utf-8")).get("status") == "completed":
            runs.append(run)
    return runs


def run_files(run: Path):
    """(collection, file path) for every page of a run, from its manifest."""
    manifest = json.loads((run / "manifest.json").read_text(encoding="utf-8"))
    for entry in (manifest.get("queries") or {}).values():
        for page in entry["pages"]:
            yield entry["collection"], run / page["file"]
    for collection, entry in (manifest.get("collections") or {}).items():
        for page in entry["pages"]:
            yield collection, run / page["file"]


def run_pages(run: Path):
    """(query, collection, page entry, file path) for every page of a run, from its manifest. Listing-mode runs name
    the query after the collection; pages of older runs carry no fetched_at of their own."""
    manifest = json.loads((run / "manifest.json").read_text(encoding="utf-8"))
    for query, entry in (manifest.get("queries") or {}).items():
        for page in entry["pages"]:
            yield query, entry["collection"], page, run / page["file"]
    for collection, entry in (manifest.get("collections") or {}).items():
        for page in entry["pages"]:
            yield collection, collection, page, run / page["file"]


def load_sourced(runs, collection):
    """Like load_records, with where each chosen copy came from: (record, {run, query, file, observed_at})."""
    chosen = {}
    for order, run in enumerate(runs):
        started = json.loads((run / "manifest.json").read_text(encoding="utf-8")).get("started_at")
        for query, name, page, file in run_pages(run):
            if name != collection:
                continue
            for record in json.loads(file.read_text(encoding="utf-8")):
                key = record["id"]
                rank = (record.get("modified_at") or "", order)
                if key not in chosen or rank >= chosen[key][0]:
                    chosen[key] = (rank, record, {"run": run.name, "query": query, "file": file.name,
                                                  "observed_at": page.get("fetched_at") or started})
    return [chosen[key][1:] for key in sorted(chosen)]


def load_records(runs, collection):
    """Records of one collection across runs, deduplicated by id: the copy with the latest modified_at wins (ties:
    the later run). Returned sorted by id so every consumer iterates deterministically."""
    return [record for record, _ in load_sourced(runs, collection)]


def provenance(runs):
    """Run ids, manifest hashes and per-query completeness: what a derived report was built from."""
    sources = []
    for run in runs:
        raw = (run / "manifest.json").read_bytes()
        manifest = json.loads(raw)
        queries = {name: {"rows": entry["rows"], "x_total": entry.get("x_total"), "complete": entry.get("complete")}
                   for name, entry in (manifest.get("queries") or {}).items()}
        collections = {name: {"rows": entry["rows"], "truncated": bool(entry.get("truncated_by_page_limit"))}
                       for name, entry in (manifest.get("collections") or {}).items()}
        sources.append({"run_id": manifest["run_id"], "manifest_sha256": hashlib.sha256(raw).hexdigest(),
                        "queries": queries, "collections": collections})
    return sources
