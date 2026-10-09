"""Import a locally downloaded, permitted Oracle's Elixir CSV into raw SQLite.

Source CSV is NOT bundled. Importer does not assign ratings, current roster,
commercial use rights, or permanent internal Person IDs.
"""
import argparse
import csv
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import sqlite3

NEEDED = {"gameid", "participantid", "date", "playername", "teamname"}
# Default outputs live next to this script (git-ignored data/), whatever the current directory is.
DATA_DIR = Path(__file__).resolve().parent / "data"


def import_csv(csv_path: Path, db_path: Path):
    if not csv_path.is_file():
        raise FileNotFoundError(csv_path)
    digest = hashlib.sha256()
    with csv_path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    db_path.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(db_path)
    db.execute("PRAGMA journal_mode=WAL")
    db.execute("""CREATE TABLE IF NOT EXISTS oracle_player_game_raw (
        game_id TEXT NOT NULL,
        participant_id INTEGER NOT NULL,
        game_date TEXT NOT NULL,
        player_name TEXT,
        team_name TEXT,
        source_file_sha256 TEXT NOT NULL,
        ingested_at TEXT NOT NULL,
        payload_json TEXT NOT NULL,
        PRIMARY KEY(game_id, participant_id)
    )""")
    ingested_at = datetime.now(timezone.utc).isoformat()
    rows = 0
    with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        missing = NEEDED - set(reader.fieldnames or [])
        if missing:
            raise ValueError(f"Missing required CSV columns: {sorted(missing)}")
        with db:
            for row in reader:
                try:
                    pid = int(row["participantid"])
                except (ValueError, TypeError):
                    continue
                if not 1 <= pid <= 10:  # 11-12 usually team totals
                    continue
                if not row["gameid"] or not row["date"]:
                    continue
                db.execute("""INSERT INTO oracle_player_game_raw
                    (game_id, participant_id, game_date, player_name, team_name, source_file_sha256, ingested_at, payload_json)
                    VALUES(?,?,?,?,?,?,?,?)
                    ON CONFLICT(game_id, participant_id) DO UPDATE SET
                    game_date=excluded.game_date, player_name=excluded.player_name,
                    team_name=excluded.team_name, source_file_sha256=excluded.source_file_sha256,
                    ingested_at=excluded.ingested_at, payload_json=excluded.payload_json""",
                    (row["gameid"], pid, row["date"], row["playername"], row["teamname"],
                     digest.hexdigest(), ingested_at, json.dumps(row, ensure_ascii=False)))
                rows += 1
    print(f"Imported {rows} player-game rows into {db_path}; file SHA256={digest.hexdigest()}")
    db.close()

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("csv", type=Path, help="Path to a CSV you downloaded under applicable terms")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    args = parser.parse_args()
    import_csv(args.csv, args.db)
