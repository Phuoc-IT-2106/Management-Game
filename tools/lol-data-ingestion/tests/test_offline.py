import csv
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from sync_pandascore import open_database, store_page
from import_oracles_elixir import import_csv


class StagingTests(unittest.TestCase):
    def test_upsert_preserves_first_seen_and_updates_last_seen(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "db.sqlite3"
            db = open_database(p)
            store_page(db, "players", [{"id": 42, "name": "A"}], "2026-01-01")
            store_page(db, "players", [{"id": 42, "name": "B"}], "2026-01-02")
            rec = db.execute("SELECT payload_json, first_seen_at, last_seen_at FROM provider_records").fetchone()
            self.assertEqual(json.loads(rec[0])["name"], "B")
            self.assertEqual(rec[1:], ("2026-01-01", "2026-01-02"))
            db.close()

    def test_oracle_csv_rows_idempotent(self):
        with tempfile.TemporaryDirectory() as d:
            file = Path(d) / "tiny.csv"
            with file.open("w", newline="", encoding="utf8") as f:
                writer = csv.DictWriter(f, fieldnames=["gameid", "participantid", "date", "playername", "teamname"])
                writer.writeheader()
                writer.writerow({"gameid": "g1", "participantid": "1", "date": "2026-04-01", "playername": "A", "teamname": "T"})
                writer.writerow({"gameid": "g1", "participantid": "100", "date": "2026-04-01", "playername": "", "teamname": "T"})
            db = Path(d) / "db.sqlite3"
            import_csv(file, db)
            import_csv(file, db)
            conn = sqlite3.connect(db)
            self.assertEqual(conn.execute("SELECT count(*) FROM oracle_player_game_raw").fetchone()[0], 1)
            conn.close()

if __name__ == "__main__":
    unittest.main()
