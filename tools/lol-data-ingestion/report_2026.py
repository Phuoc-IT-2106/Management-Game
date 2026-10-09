"""Produce an evidence-only report of 2026 observed pro match participation.

NOT proof of current roster, continued employment, academy affiliation,
identity equivalence across teams, age, or license to distribute data.
"""
import argparse
from collections import defaultdict
import csv
from pathlib import Path
import sqlite3

# Default inputs/outputs live next to this script (git-ignored data/), whatever the current directory is.
DATA_DIR = Path(__file__).resolve().parent / "data"


def report(db_path: Path, output: Path, year: int):
    db = sqlite3.connect(db_path)
    rows = db.execute("""SELECT game_id, game_date, player_name, team_name,
                           json_extract(payload_json, '$.league')
                           FROM oracle_player_game_raw WHERE substr(game_date,1,4) = ?""", (str(year),))
    groups = defaultdict(lambda: {"games": set(), "latest_date": ""})
    for game_id, day, player, team, league in rows:
        key = (str(player or ""), str(team or ""), str(league or ""))
        groups[key]["games"].add(game_id)
        groups[key]["latest_date"] = max(groups[key]["latest_date"], day or "")
    db.close()
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(["observed_handle", "observed_team", "observed_league", "games_played_in_year", "last_observed_game_date", "interpretation"])
        for (p, t, league), g in sorted(groups.items()):
            writer.writerow([p, t, league, len(g["games"]), g["latest_date"], "played_in_year_only_not_current_roster"])
    print(f"Wrote {len(groups)} observed handle/team/league combinations to {output}; NOT unique resolved players")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--year", type=int, default=2026)
    parser.add_argument("--output", type=Path, default=DATA_DIR / "reports" / "participation_2026.csv")
    args = parser.parse_args()
    report(args.db, args.output, args.year)
