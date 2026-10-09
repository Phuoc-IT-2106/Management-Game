"""Dated tournament rosters from raw snapshots into data/staging.sqlite3, table tournament_roster.

A row says: this provider player id was listed on this team's expected roster for this tournament stage, which ran
from begin_at to end_at, at this tier and region. That is roster *evidence for a dated stage*, not a contract, a
start or end date, a starter/substitute flag or proof the player played. Rows are rebuilt from the immutable
snapshots every run (the UPSERT table keeps only the latest payload and loses history).
"""
import argparse
from pathlib import Path
import sqlite3

from snapshots import DATA_DIR, SNAPSHOT_ROOT, completed_runs, load_records

COLUMNS = ("tournament_id", "serie_id", "league_id", "stage_name", "tier", "region", "begin_at", "end_at",
           "team_id", "player_id", "role", "nationality", "birthday")


def roster_rows(tournaments):
    """One row per (tournament, team, player) listed in expected_roster; players without an id are skipped."""
    rows = {}
    for t in tournaments:
        for entry in t.get("expected_roster") or []:
            team = entry.get("team") or {}
            if team.get("id") is None:
                continue
            for player in entry.get("players") or []:
                if player.get("id") is None:
                    continue
                key = (t["id"], team["id"], player["id"])
                rows[key] = {"tournament_id": t["id"], "serie_id": t.get("serie_id"), "league_id": t.get("league_id"),
                             "stage_name": t.get("name") or "", "tier": t.get("tier"), "region": t.get("region"),
                             "begin_at": t.get("begin_at"), "end_at": t.get("end_at"), "team_id": team["id"],
                             "player_id": player["id"], "role": player.get("role"), "nationality": player.get("nationality"),
                             "birthday": player.get("birthday")}
    return [rows[key] for key in sorted(rows)]


def write_rows(db_path: Path, rows):
    db = sqlite3.connect(db_path)
    db.execute("DROP TABLE IF EXISTS tournament_roster")
    db.execute(f"""CREATE TABLE tournament_roster ({', '.join(c + ' TEXT' for c in COLUMNS)},
        PRIMARY KEY (tournament_id, team_id, player_id))""")
    db.executemany(f"INSERT INTO tournament_roster VALUES ({', '.join('?' for _ in COLUMNS)})",
                   [tuple(None if r[c] is None else str(r[c]) for c in COLUMNS) for r in rows])
    db.commit()
    db.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--snapshot-root", type=Path, default=SNAPSHOT_ROOT)
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    args = parser.parse_args()
    runs = completed_runs(args.snapshot_root)
    rows = roster_rows(load_records(runs, "tournaments"))
    write_rows(args.db, rows)
    print(f"tournament_roster: {len(rows)} dated rows from {len(runs)} completed runs -> {args.db}")
