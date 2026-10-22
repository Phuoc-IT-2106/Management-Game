"""Import a locally downloaded, permitted Oracle's Elixir CSV into raw SQLite.

Source CSV is NOT bundled. Importer does not assign ratings, current roster,
commercial use rights, or permanent internal Person IDs.

Two layers:
  import_csv      the starter's raw table oracle_player_game_raw (one row per game and player; kept for report_2026.py).
  import_source   validated, versioned import (the default CLI path, which also runs import_csv):
    - validation before anything is written: a .csv file, 1 byte to MAX_BYTES, UTF-8 with a header, the required
      columns, numeric participant ids, parseable dates, and the reporting period (rows dated outside the target year
      are flagged; more than MAX_OUT_OF_PERIOD of them rejects the file, e.g. last year's file);
    - oe_source_files: one row per file version (SHA-256, name, size, header, missing expected columns, period);
    - oe_raw_rows + oe_row_observations: every CSV row exactly as read (values as text, content-addressed so a newer
      download stores only changed rows), with its file, row number and a duplicate marker;
    - current tables, rebuilt from the newest imported file: oe_player_games (source fields: game, player, team,
      league, date, position, champion, result, patch, duration), oe_player_game_stats (the statistics columns parsed
      to numbers, NULL when the file has no such column or the cell is empty) and oe_team_games (team rows);
    - several reporting years: every yearly file (2025, 2026, ...) is its own series of versions, and the current
      tables union the newest version of each year, so importing 2026 never drops 2025. A (game, participant) key in
      two years' files keeps the row dated inside its own file's year; both are listed in oe_key_collisions.
Provider-scoped ids are kept as given (gameid, playerid "oe:player:...", teamid "oe:team:..."). No value is invented:
a column the file lacks stays NULL and is listed in oe_source_files.missing_columns_json.
"""
import argparse
import csv
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import sqlite3
import sys

NEEDED = {"gameid", "participantid", "date", "playername", "teamname"}
# Default outputs live next to this script (git-ignored data/), whatever the current directory is.
DATA_DIR = Path(__file__).resolve().parent / "data"
IMPORT_DIR = DATA_DIR / "import" / "oracles-elixir"
OFFICIAL_SOURCE = "https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads"
TARGET_YEAR = 2026
MAX_BYTES = 1024 ** 3
MAX_OUT_OF_PERIOD = 0.05
REQUIRED = ("gameid", "participantid", "date", "league", "position", "playername", "teamname")
# Source fields of a player or team row: CSV column -> table column.
CONTEXT = {"gameid": "game_id", "participantid": "participant_id", "datacompleteness": "data_completeness", "url": "url",
           "league": "league", "year": "year", "split": "split", "playoffs": "playoffs", "date": "game_date",
           "game": "game_number", "patch": "patch", "side": "side", "position": "position", "playername": "player_name",
           "playerid": "oe_player_id", "teamname": "team_name", "teamid": "oe_team_id", "champion": "champion",
           "gamelength": "game_length_seconds", "result": "result"}
# Statistics as published, parsed to numbers. Early-game columns at 10 and 15 minutes.
EARLY = [f"{stat}at{minute}" for minute in (10, 15)
         for stat in ("gold", "xp", "cs", "golddiff", "xpdiff", "csdiff", "kills", "assists", "deaths")]
STATS = ["kills", "deaths", "assists", "teamkills", "teamdeaths", "firstbloodkill", "firstbloodassist", "firstbloodvictim",
         "total cs", "minionkills", "monsterkills", "cspm", "totalgold", "earnedgold", "earned gpm", "goldspent",
         "damagetochampions", "dpm", "damageshare", "damagetakenperminute", "wardsplaced", "wpm", "wardskilled", "wcpm",
         "controlwardsbought", "visionscore", "vspm"] + EARLY
PLAYER_POSITIONS = ("top", "jng", "mid", "bot", "sup")


class ImportRejected(Exception):
    """The file failed validation; nothing was written."""


def column(name):
    return name.replace(" ", "_")


SCHEMA = f"""
CREATE TABLE IF NOT EXISTS oe_source_files (
    file_sha256 TEXT PRIMARY KEY, file_name TEXT NOT NULL, bytes INTEGER NOT NULL, header_sha256 TEXT NOT NULL,
    columns_json TEXT NOT NULL, missing_columns_json TEXT NOT NULL, rows_total INTEGER NOT NULL,
    player_rows INTEGER NOT NULL, team_rows INTEGER NOT NULL, other_rows INTEGER NOT NULL,
    duplicate_rows INTEGER NOT NULL, conflicting_duplicates INTEGER NOT NULL, invalid_position_rows INTEGER NOT NULL,
    out_of_period_rows INTEGER NOT NULL, target_year INTEGER NOT NULL, period_from TEXT, period_to TEXT,
    imported_at TEXT NOT NULL, import_order INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS oe_raw_rows (row_sha256 TEXT PRIMARY KEY, header_sha256 TEXT NOT NULL, values_json TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS oe_row_observations (
    file_sha256 TEXT NOT NULL REFERENCES oe_source_files(file_sha256), row_number INTEGER NOT NULL,
    game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, kind TEXT NOT NULL,
    row_sha256 TEXT NOT NULL REFERENCES oe_raw_rows(row_sha256), duplicate_of_row INTEGER,
    PRIMARY KEY (file_sha256, row_number));
"""
CURRENT = {
    "oe_player_games": "file_sha256 TEXT NOT NULL, row_number INTEGER NOT NULL, "
                       + ", ".join(f"{c} TEXT" if c not in ("participant_id",) else f"{c} INTEGER" for c in CONTEXT.values())
                       + ", in_period INTEGER NOT NULL, valid_position INTEGER NOT NULL, PRIMARY KEY (game_id, participant_id)",
    "oe_player_game_stats": "game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, "
                            + ", ".join(f'"{column(c)}" REAL' for c in STATS) + ", PRIMARY KEY (game_id, participant_id)",
    "oe_team_games": "file_sha256 TEXT NOT NULL, row_number INTEGER NOT NULL, game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, "
                     "league TEXT, game_date TEXT, side TEXT, team_name TEXT, oe_team_id TEXT, result TEXT, "
                     "game_length_seconds TEXT, patch TEXT, in_period INTEGER NOT NULL, PRIMARY KEY (game_id, participant_id)",
}


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


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


# ---------- validated, versioned import ----------

def row_kind(participant_id):
    return "player" if 1 <= participant_id <= 10 else "team" if participant_id in (100, 200) else "other"


def game_year(stamp):
    try:
        return datetime.fromisoformat(stamp.strip().replace("Z", "")).year
    except ValueError:
        return None


def validate(csv_path: Path, target_year):
    """One streaming pass: header, required columns, row shapes, duplicates and reporting period. Raises
    ImportRejected; returns the facts the import records."""
    if csv_path.suffix.lower() != ".csv" or not csv_path.is_file():
        raise ImportRejected(f"not a .csv file: {csv_path.name}")
    size = csv_path.stat().st_size
    if not 0 < size <= MAX_BYTES:
        raise ImportRejected(f"size {size} bytes outside 1..{MAX_BYTES}")
    facts = {"bytes": size, "rows": 0, "kinds": {"player": 0, "team": 0, "other": 0}, "duplicates": 0, "conflicts": 0,
             "invalid_position": 0, "out_of_period": 0, "dates": [], "seen": {}}
    try:
        with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
            reader = csv.reader(f)
            header = next(reader, None)
            if not header or len(set(header)) != len(header):
                raise ImportRejected("missing or repeated header columns")
            missing = [c for c in REQUIRED if c not in header]
            if missing:
                raise ImportRejected(f"missing required columns {missing}")
            index = {c: i for i, c in enumerate(header)}
            for number, values in enumerate(reader, start=2):
                if len(values) != len(header):
                    raise ImportRejected(f"line {number}: {len(values)} fields, header has {len(header)}")
                facts["rows"] += 1
                try:
                    participant = int(values[index["participantid"]])
                except ValueError:
                    raise ImportRejected(f"line {number}: participantid is not a number")
                game, stamp = values[index["gameid"]], values[index["date"]]
                year = game_year(stamp)
                if not game or year is None:
                    raise ImportRejected(f"line {number}: empty gameid or unparseable date")
                kind = row_kind(participant)
                facts["kinds"][kind] += 1
                if year != target_year:
                    facts["out_of_period"] += 1
                else:
                    facts["dates"].append(stamp[:10])
                if kind == "player" and values[index["position"]].strip().lower() not in PLAYER_POSITIONS:
                    facts["invalid_position"] += 1
                key = (game, participant)
                digest = hashlib.sha256("\x1f".join(values).encode("utf-8")).hexdigest()
                if key in facts["seen"]:
                    facts["duplicates"] += 1
                    facts["conflicts"] += facts["seen"][key] != digest
                else:
                    facts["seen"][key] = digest
    except UnicodeDecodeError:
        raise ImportRejected("not UTF-8 text")
    except csv.Error as exc:
        raise ImportRejected(f"CSV parse error: {exc}")
    if facts["rows"] == 0:
        raise ImportRejected("no data rows")
    if facts["out_of_period"] / facts["rows"] > MAX_OUT_OF_PERIOD:
        raise ImportRejected(f"{facts['out_of_period']} of {facts['rows']} rows dated outside {target_year}: wrong reporting period")
    facts["header"] = header
    del facts["seen"]
    return facts


def open_db(db_path):
    db_path.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(db_path)
    db.execute("PRAGMA journal_mode=WAL")
    db.executescript(SCHEMA)
    return db


def import_source(csv_path: Path, db_path: Path, target_year=TARGET_YEAR):
    """Validates, then records one file version and rebuilds the current tables from the newest version. Importing
    the same bytes again records nothing new. Returns a summary dict."""
    facts = validate(csv_path, target_year)
    file_sha = sha256_file(csv_path)
    header = facts["header"]
    header_sha = hashlib.sha256(json.dumps(header, ensure_ascii=False).encode("utf-8")).hexdigest()
    db = open_db(db_path)
    try:
        if db.execute("SELECT 1 FROM oe_source_files WHERE file_sha256 = ?", (file_sha,)).fetchone():
            return {"file_sha256": file_sha, "status": "already imported"} | summary(db, file_sha)
        expected = list(CONTEXT) + STATS
        dates = facts["dates"]
        with db:
            order = db.execute("SELECT coalesce(max(import_order), 0) + 1 FROM oe_source_files").fetchone()[0]
            db.execute("INSERT INTO oe_source_files VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                       (file_sha, csv_path.name, facts["bytes"], header_sha, json.dumps(header, ensure_ascii=False),
                        json.dumps([c for c in expected if c not in header]), facts["rows"], facts["kinds"]["player"],
                        facts["kinds"]["team"], facts["kinds"]["other"], facts["duplicates"], facts["conflicts"],
                        facts["invalid_position"], facts["out_of_period"], target_year,
                        min(dates) if dates else None, max(dates) if dates else None,
                        datetime.now(timezone.utc).isoformat(), order))
            index = {c: i for i, c in enumerate(header)}
            first = {}
            with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
                reader = csv.reader(f)
                next(reader)
                for number, values in enumerate(reader, start=2):
                    payload = json.dumps(values, ensure_ascii=False, separators=(",", ":"))
                    row_sha = hashlib.sha256((header_sha + "\n" + payload).encode("utf-8")).hexdigest()
                    game, participant = values[index["gameid"]], int(values[index["participantid"]])
                    db.execute("INSERT OR IGNORE INTO oe_raw_rows VALUES (?,?,?)", (row_sha, header_sha, payload))
                    db.execute("INSERT INTO oe_row_observations VALUES (?,?,?,?,?,?,?)",
                               (file_sha, number, game, participant, row_kind(participant), row_sha, first.get((game, participant))))
                    first.setdefault((game, participant), number)
            rebuild_current(db, target_year)
        return {"file_sha256": file_sha, "status": "imported"} | summary(db, file_sha)
    finally:
        db.close()


def latest_per_year(db):
    """(file SHA-256, header, reporting year) of the newest imported version of every reporting year, oldest year
    first. A newer download of one year never replaces another year's file."""
    return [(sha, json.loads(header), year) for sha, header, year in db.execute(
        """SELECT f.file_sha256, f.columns_json, f.target_year FROM oe_source_files f
           WHERE f.import_order = (SELECT max(g.import_order) FROM oe_source_files g WHERE g.target_year = f.target_year)
           ORDER BY f.target_year""")]


def rebuild_current(db, target_year=TARGET_YEAR):
    """Current tables: the newest version of each reporting year's file, unioned (first occurrence of each (game,
    participant) within a file). A key found in two years' files keeps the row dated inside its own file's year, the
    later year winning a tie, and both are listed in oe_key_collisions. target_year is kept for older callers."""
    for table, columns in CURRENT.items():
        db.execute(f"DROP TABLE IF EXISTS {table}")
        db.execute(f"CREATE TABLE {table} ({columns})")
    db.execute("DROP TABLE IF EXISTS oe_key_collisions")
    db.execute("CREATE TABLE oe_key_collisions (game_id TEXT NOT NULL, participant_id INTEGER NOT NULL, kept_file TEXT NOT NULL, "
               "dropped_file TEXT NOT NULL, reason TEXT NOT NULL)")
    chosen = {}
    for file_sha, header, year in latest_per_year(db):
        for key, entry in file_rows(db, file_sha, header, year).items():
            previous = chosen.get(key)
            if previous is not None:
                keep_new = entry[1] or not previous[1]
                kept, dropped = (entry, previous) if keep_new else (previous, entry)
                db.execute("INSERT INTO oe_key_collisions VALUES (?,?,?,?,?)",
                           (*key, kept[0], dropped[0], "kept the row dated in its own file's year" if kept[1] else "neither row in its file's year; later year kept"))
                if not keep_new:
                    continue
            chosen[key] = entry
    players = [e[3] for e in chosen.values() if e[2] == "player"]
    stats = [e[4] for e in chosen.values() if e[2] == "player"]
    teams = [e[3] for e in chosen.values() if e[2] == "team"]
    for table, rows in (("oe_player_games", players), ("oe_player_game_stats", stats), ("oe_team_games", teams)):
        if rows:
            db.executemany(f"INSERT INTO {table} VALUES ({','.join('?' * len(rows[0]))})", rows)


def file_rows(db, file_sha, header, year):
    """{(game, participant): (file, in its year, kind, row, stats row)} for the player and team rows of one file."""
    index = {c: i for i, c in enumerate(header)}

    def get(values, name):
        i = index.get(name)
        return None if i is None or values[i] == "" else values[i]

    def number(values, name):
        value = get(values, name)
        try:
            return None if value is None else float(value)
        except ValueError:
            return None

    rows = {}
    for number_, kind, values_json in db.execute("""SELECT o.row_number, o.kind, r.values_json FROM oe_row_observations o
            JOIN oe_raw_rows r USING (row_sha256) WHERE o.file_sha256 = ? AND o.duplicate_of_row IS NULL
            AND o.kind IN ('player', 'team') ORDER BY o.row_number""", (file_sha,)):
        values = json.loads(values_json)
        in_period = int(game_year(get(values, "date") or "") == year)
        game, participant = get(values, "gameid"), int(get(values, "participantid"))
        if kind == "team":
            rows[(game, participant)] = (file_sha, in_period, "team", (
                file_sha, number_, game, participant, get(values, "league"), get(values, "date"), get(values, "side"),
                get(values, "teamname"), get(values, "teamid"), get(values, "result"), get(values, "gamelength"),
                get(values, "patch"), in_period), None)
            continue
        context = [get(values, c) for c in CONTEXT]
        context[list(CONTEXT).index("participantid")] = participant
        position = (get(values, "position") or "").strip().lower()
        rows[(game, participant)] = (file_sha, in_period, "player",
                                     (file_sha, number_, *context, in_period, int(position in PLAYER_POSITIONS)),
                                     (game, participant, *[number(values, c) for c in STATS]))
    return rows


def summary(db, file_sha):
    row = db.execute("""SELECT file_name, bytes, rows_total, player_rows, team_rows, duplicate_rows, conflicting_duplicates,
                               invalid_position_rows, out_of_period_rows, period_from, period_to, missing_columns_json
                        FROM oe_source_files WHERE file_sha256 = ?""", (file_sha,)).fetchone()
    keys = ("file_name", "bytes", "rows_total", "player_rows", "team_rows", "duplicate_rows", "conflicting_duplicates",
            "invalid_position_rows", "out_of_period_rows", "period_from", "period_to", "missing_columns")
    result = dict(zip(keys, row))
    result["missing_columns"] = json.loads(result["missing_columns"])
    return result


def find_csv(import_dir: Path = IMPORT_DIR):
    """The newest *.csv in the import folder (by modification time), or None."""
    files = sorted(import_dir.glob("*.csv"), key=lambda p: p.stat().st_mtime) if import_dir.is_dir() else []
    return files[-1] if files else None


def file_year(path: Path):
    """The reporting year in an Oracle's Elixir file name ("2025_LoL_esports_match_data_from_OraclesElixir.csv")."""
    head = path.name[:5]
    return int(head[:4]) if len(head) == 5 and head[:4].isdigit() and head[4] == "_" else None


def find_yearly(import_dir: Path = IMPORT_DIR, years=None):
    """Every yearly CSV in the import folder and its snapshots/<stamp>/ folders, oldest first (snapshot stamp, then
    modification time), so each year's newest download is imported last and becomes that year's current file."""
    if not import_dir.is_dir():
        return []
    files = [p for p in import_dir.rglob("*.csv") if file_year(p) and (years is None or file_year(p) in years)]
    return sorted(files, key=lambda p: (p.parent.name if p.parent != import_dir else "", p.stat().st_mtime))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("csv", type=Path, nargs="?", help=f"Path to a CSV you downloaded under applicable terms (default: every yearly file under {IMPORT_DIR})")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--year", type=int, help="reporting year (default: from the file name; without a path, every year found)")
    parser.add_argument("--legacy-only", action="store_true", help="only the starter's oracle_player_game_raw table")
    args = parser.parse_args()
    if args.csv:
        sources = [(args.csv, args.year or file_year(args.csv) or TARGET_YEAR)]
    else:
        sources = [(p, file_year(p)) for p in find_yearly(years=[args.year] if args.year else None)]
    if not sources:
        print(f"No Oracle's Elixir CSV found. Run fetch_oracles_elixir.py, or download "
              f"<year>_LoL_esports_match_data_from_OraclesElixir.csv from {OFFICIAL_SOURCE} into {IMPORT_DIR}", file=sys.stderr)
        sys.exit(2)
    rejected_any = False
    for source, year in sources:
        if not args.legacy_only:
            try:
                result = import_source(source, args.db, year)
            except ImportRejected as rejected:
                print(f"REJECTED {source.name}: {rejected}. Nothing was written.", file=sys.stderr)
                rejected_any = True
                continue
            print(json.dumps({"year": year} | result, indent=1))
        import_csv(source, args.db)
    sys.exit(1 if rejected_any else 0)
