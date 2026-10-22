"""Normalized staging tables from the immutable raw PandaScore snapshots (offline: no network, no token).

Two kinds of table, kept apart on purpose:
  observations  append-only, keyed by run: what one collection run saw. collection_runs, collection_queries,
                record_observations (record id -> payload hash), tournament_registrations (expected_roster rows) and
                team_membership_observations (teams.players and players.current_team). Rebuilding never rewrites
                an earlier run's rows, so a later run cannot erase what an earlier one observed.
  current       ps_* tables, rebuilt on every run from the latest copy of each record (latest modified_at, ties to
                the later run; see snapshots.load_sourced). A current row is what the provider reported last, with
                the run, query, file and payload hash it came from; it is not a permanent or historical fact.
record_payloads keeps every distinct raw payload once, by SHA-256, so every provider field stays reachable even where
only some fields are normalized.

Only completed runs are read (snapshots.completed_runs), and every page's SHA-256 is checked against its manifest
before anything is written. Classification columns say 'verified' only with the evidence named in the column's
comment and 'unknown' otherwise. Nothing here infers ages, contracts, ratings, academy or reserve status, or a
player's participation in a match: PandaScore's Free plan matches name teams, not lineups.

Exports (--export) write local CSV files under data/reports/exports/ for research only. They are provider data:
do not commit, publish or redistribute them (PandaScore terms 6.3-6.4); cite "Source: PandaScore".
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import sqlite3

from snapshots import DATA_DIR, SNAPSHOT_ROOT, completed_runs, load_sourced, run_pages
from sync_pandascore import COLLECTIONS

DB_PATH = DATA_DIR / "staging.sqlite3"
EXPORT_DIR = DATA_DIR / "reports" / "exports"
YEAR = "2026"
LICENSE_REVIEW = "LICENSE_REVIEW_REQUIRED"
PLAYER_FIELDS = ("name", "slug", "first_name", "last_name", "nationality", "role", "current_team", "image_url")

OBSERVATION_SCHEMA = """
CREATE TABLE IF NOT EXISTS collection_runs (
    run_id TEXT PRIMARY KEY, preset TEXT, status TEXT NOT NULL, started_at TEXT, finished_at TEXT,
    requests INTEGER, scope TEXT, manifest_sha256 TEXT NOT NULL, pages INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS collection_queries (
    run_id TEXT NOT NULL REFERENCES collection_runs(run_id), query TEXT NOT NULL, collection TEXT NOT NULL,
    path TEXT, params_json TEXT, pages INTEGER, rows INTEGER, unique_ids INTEGER, duplicate_ids INTEGER,
    x_total INTEGER, x_total_changed INTEGER, ended INTEGER, complete INTEGER, PRIMARY KEY (run_id, query));
CREATE TABLE IF NOT EXISTS record_payloads (payload_sha256 TEXT PRIMARY KEY, payload_json TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS record_observations (
    run_id TEXT NOT NULL, query TEXT NOT NULL, collection TEXT NOT NULL, external_id TEXT NOT NULL,
    payload_sha256 TEXT NOT NULL REFERENCES record_payloads(payload_sha256), modified_at TEXT, observed_at TEXT,
    source_file TEXT NOT NULL, PRIMARY KEY (run_id, query, collection, external_id));
CREATE INDEX IF NOT EXISTS record_observations_by_id ON record_observations(collection, external_id);
CREATE TABLE IF NOT EXISTS tournament_registrations (
    run_id TEXT NOT NULL, tournament_id TEXT NOT NULL, team_id TEXT NOT NULL, player_id TEXT NOT NULL, role TEXT,
    observed_at TEXT, source_query TEXT NOT NULL, source_file TEXT NOT NULL,
    PRIMARY KEY (run_id, tournament_id, team_id, player_id));
CREATE TABLE IF NOT EXISTS team_membership_observations (
    run_id TEXT NOT NULL, team_id TEXT NOT NULL, player_id TEXT NOT NULL, source TEXT NOT NULL, observed_at TEXT,
    PRIMARY KEY (run_id, team_id, player_id, source));
"""

PROVENANCE = "source_run TEXT NOT NULL, source_query TEXT NOT NULL, source_file TEXT NOT NULL, observed_at TEXT, payload_sha256 TEXT NOT NULL, modified_at TEXT"

# Current-state tables: derived, so dropped and rebuilt each run. Ids are provider ids (text, as in provider_records).
CURRENT_SCHEMA = {
    "ps_leagues": f"""league_id TEXT PRIMARY KEY, name TEXT, slug TEXT, url TEXT, image_url TEXT, videogame_id TEXT,
        series_count INTEGER, in_latest_listing INTEGER, {PROVENANCE}""",
    "ps_series": f"""serie_id TEXT PRIMARY KEY, league_id TEXT, name TEXT, full_name TEXT, slug TEXT, season TEXT,
        year_label TEXT, begin_at TEXT, end_at TEXT, winner_id TEXT, winner_type TEXT, tournament_count INTEGER,
        relevance_2026 TEXT NOT NULL, {PROVENANCE}""",
    "ps_tournaments": f"""tournament_id TEXT PRIMARY KEY, league_id TEXT, serie_id TEXT, name TEXT, slug TEXT, tier TEXT,
        region TEXT, country TEXT, type TEXT, begin_at TEXT, end_at TEXT, has_bracket INTEGER, live_supported INTEGER,
        detailed_stats INTEGER, prizepool TEXT, winner_id TEXT, winner_type TEXT, team_count INTEGER,
        roster_team_count INTEGER, roster_player_rows INTEGER, match_count INTEGER, relevance_2026 TEXT NOT NULL,
        {PROVENANCE}""",
    # roster_status: players_listed | empty_roster (team listed, no players) | no_roster_entry (in teams[] only).
    "ps_tournament_teams": """tournament_id TEXT NOT NULL, team_id TEXT NOT NULL, in_teams_list INTEGER NOT NULL,
        roster_status TEXT NOT NULL, roster_players INTEGER NOT NULL, PRIMARY KEY (tournament_id, team_id)""",
    # The registration rows of each stage's current copy: listed on the expected roster, nothing more.
    "ps_registrations": """tournament_id TEXT NOT NULL, team_id TEXT NOT NULL, player_id TEXT NOT NULL, role TEXT,
        source_run TEXT NOT NULL, observed_at TEXT, PRIMARY KEY (tournament_id, team_id, player_id)""",
    "ps_matches": f"""match_id TEXT PRIMARY KEY, tournament_id TEXT, serie_id TEXT, league_id TEXT, name TEXT, slug TEXT,
        status TEXT, match_type TEXT, number_of_games INTEGER, scheduled_at TEXT, original_scheduled_at TEXT,
        begin_at TEXT, end_at TEXT, rescheduled INTEGER, forfeit INTEGER, draw INTEGER, detailed_stats INTEGER,
        opponent1_type TEXT, opponent1_id TEXT, opponent2_type TEXT, opponent2_id TEXT, score1 INTEGER,
        score2 INTEGER, winner_type TEXT, winner_id TEXT, game_count INTEGER, relevance_2026 TEXT NOT NULL,
        {PROVENANCE}""",
    "ps_match_games": """game_id TEXT PRIMARY KEY, match_id TEXT NOT NULL, position INTEGER, status TEXT,
        finished INTEGER, complete INTEGER, forfeit INTEGER, detailed_stats INTEGER, winner_type TEXT, winner_id TEXT,
        length_seconds INTEGER, begin_at TEXT, end_at TEXT""",
    "ps_teams": f"""team_id TEXT PRIMARY KEY, name TEXT, acronym TEXT, slug TEXT, location TEXT, image_url TEXT,
        dark_mode_image_url TEXT, provider_player_count INTEGER, videogame_id TEXT, in_latest_listing INTEGER,
        {PROVENANCE}""",
    # Provider-reported profile only. Birthday, age, salary, contract, ratings and potential are not columns because
    # /lol/players does not return them: they stay unknown. provider_active_flag is the raw flag, not proof of activity.
    "ps_players": f"""player_id TEXT PRIMARY KEY, name TEXT, slug TEXT, first_name TEXT, last_name TEXT,
        nationality TEXT, role TEXT, provider_active_flag INTEGER, current_team_id TEXT, image_url TEXT,
        videogame_id TEXT, missing_fields TEXT NOT NULL, in_latest_listing INTEGER, {PROVENANCE}""",
    # url_state: value | null (key present, null) | empty | absent (key missing). No row means the entity is unknown.
    "ps_image_refs": """entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, field TEXT NOT NULL, url_state TEXT NOT NULL,
        url TEXT, host TEXT, license_status TEXT, source_run TEXT NOT NULL, observed_at TEXT,
        PRIMARY KEY (entity_type, entity_id, field)""",
    # Dataset B. registered_in_2026 = 'verified': listed on the expected roster of a stage that touches 2026.
    # current_roster_verified_as_of: the observation date of a stage roster listing the player while the stage had
    # not ended (running or upcoming when observed). match_participation_verified_2026 is 'verified' only for players
    # in xp_participation_2026 or lpx_participation_2026 (reconcile_identities.py: a high-confidence Oracle's Elixir or
    # Leaguepedia identity with 2026 game rows; evidence names the source) and 'unknown' otherwise. academy_verified and
    # reserve_verified stay 'unknown': no source has them.
    "ps_player_index_2026": """player_id TEXT PRIMARY KEY, in_provider_catalog INTEGER NOT NULL,
        registered_in_2026 TEXT NOT NULL, registered_stages INTEGER NOT NULL, registered_team_ids TEXT NOT NULL,
        first_stage_begin_at TEXT, last_stage_end_at TEXT, match_participation_verified_2026 TEXT NOT NULL,
        current_roster_verified_as_of TEXT, current_roster_team_id TEXT, academy_verified TEXT NOT NULL,
        reserve_verified TEXT NOT NULL, provider_current_team_id TEXT, provider_active_flag INTEGER,
        evidence TEXT NOT NULL""",
}


def read_only(db_path):
    return Path(db_path).resolve().as_uri() + "?mode=ro"


def canonical(record):
    return json.dumps(record, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def sha(text):
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def text(value):
    return None if value is None else str(value)


def flag(value):
    return None if value is None else int(bool(value))


def ref(obj, key="id"):
    return text((obj or {}).get(key)) if isinstance(obj, dict) else None


def in_year(stamp):
    return isinstance(stamp, str) and stamp[:4] == YEAR


def stage_relevance(begin, end):
    """How a series or stage touches the calendar year, from its own dates only."""
    if in_year(begin):
        return "begins_in_2026"
    if not begin:
        return "unknown_dates"
    if begin[:4] < YEAR:
        if in_year(end):
            return "ends_in_2026"
        if end and end[:4] > YEAR:
            return "spans_2026"
        if not end:
            return "unknown_end"
    return "outside_2026"


def match_relevance(match):
    if in_year(match.get("scheduled_at")):
        return "scheduled_in_2026"
    if in_year(match.get("begin_at")):
        return "begins_in_2026"
    if not match.get("scheduled_at") and not match.get("begin_at"):
        return "unknown_dates"
    return "outside_2026"


def url_state(record, key):
    if key not in record:
        return "absent", None
    value = record[key]
    if value is None:
        return "null", None
    if value == "":
        return "empty", None
    return "value", str(value)


def missing_fields(player):
    return ",".join(f for f in PLAYER_FIELDS if player.get(f) in (None, "", [], {}))


# ---------- observations ----------

def verify_pages(run):
    """Every page's SHA-256 against its manifest; raises on the first mismatch (a corrupted or edited raw file)."""
    count = 0
    for _, _, page, file in run_pages(run):
        if hashlib.sha256(file.read_bytes()).hexdigest() != page["sha256"]:
            raise ValueError(f"SHA-256 mismatch: {run.name}/{file.name}")
        count += 1
    return count


def record_run(db, run):
    raw = (run / "manifest.json").read_bytes()
    manifest = json.loads(raw)
    pages = verify_pages(run)
    db.execute("INSERT OR REPLACE INTO collection_runs VALUES (?,?,?,?,?,?,?,?,?)",
               (manifest["run_id"], manifest.get("preset"), manifest["status"], manifest.get("started_at"),
                manifest.get("finished_at"), manifest.get("requests"), manifest.get("scope"),
                hashlib.sha256(raw).hexdigest(), pages))
    for query, entry in (manifest.get("queries") or {}).items():
        db.execute("INSERT OR REPLACE INTO collection_queries VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)",
                   (manifest["run_id"], query, entry["collection"], entry.get("path"),
                    json.dumps(entry.get("params") or {}, sort_keys=True), len(entry["pages"]), entry["rows"],
                    entry.get("unique_ids"), entry.get("duplicate_ids"), entry.get("x_total"),
                    flag(entry.get("x_total_changed", False)), flag(entry.get("ended")), flag(entry.get("complete"))))
    for collection, entry in (manifest.get("collections") or {}).items():
        db.execute("INSERT OR REPLACE INTO collection_queries VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)",
                   (manifest["run_id"], collection, collection, COLLECTIONS.get(collection), "{}", len(entry["pages"]),
                    entry["rows"], None, None, None, None, None, None))
    started = manifest.get("started_at")
    for query, collection, page, file in run_pages(run):
        observed = page.get("fetched_at") or started
        for record in json.loads(file.read_text(encoding="utf-8")):
            payload = canonical(record)
            digest = sha(payload)
            db.execute("INSERT OR IGNORE INTO record_payloads VALUES (?,?)", (digest, payload))
            db.execute("INSERT OR IGNORE INTO record_observations VALUES (?,?,?,?,?,?,?,?)",
                       (run.name, query, collection, str(record["id"]), digest, record.get("modified_at"), observed, file.name))
            if collection == "tournaments":
                for team_id, player in roster_entries(record):
                    db.execute("INSERT OR IGNORE INTO tournament_registrations VALUES (?,?,?,?,?,?,?,?)",
                               (run.name, str(record["id"]), team_id, str(player["id"]), player.get("role"), observed,
                                query, file.name))
            elif collection == "teams":
                for player in record.get("players") or []:
                    if isinstance(player, dict) and player.get("id") is not None:
                        db.execute("INSERT OR IGNORE INTO team_membership_observations VALUES (?,?,?,?,?)",
                                   (run.name, str(record["id"]), str(player["id"]), "team.players", observed))
            elif collection == "players" and ref(record.get("current_team")):
                db.execute("INSERT OR IGNORE INTO team_membership_observations VALUES (?,?,?,?,?)",
                           (run.name, ref(record["current_team"]), str(record["id"]), "player.current_team", observed))
    return pages


def roster_entries(tournament):
    """(team id, player object) for every player with an id on the stage's expected roster."""
    for entry in tournament.get("expected_roster") or []:
        team_id = ref((entry or {}).get("team"))
        if team_id is None:
            continue
        for player in entry.get("players") or []:
            if isinstance(player, dict) and player.get("id") is not None:
                yield team_id, player


# ---------- current state ----------

def latest_listing_ids(db, collection):
    """Ids in the most recent complete, unfiltered listing of a catalogue collection, or None if there is none."""
    row = db.execute("""SELECT q.run_id, q.query FROM collection_queries q JOIN collection_runs r USING (run_id)
                        WHERE q.collection = ? AND q.path = ? AND q.complete = 1 AND r.status = 'completed'
                          AND q.params_json NOT LIKE '%filter[%' AND q.params_json NOT LIKE '%range[%'
                        ORDER BY q.run_id DESC LIMIT 1""", (collection, COLLECTIONS[collection])).fetchone()
    if row is None:
        return None
    return {r[0] for r in db.execute("SELECT external_id FROM record_observations WHERE run_id = ? AND query = ?", row)}


def provenance(source, record):
    payload = canonical(record)
    return (source["run"], source["query"], source["file"], source["observed_at"], sha(payload), record.get("modified_at"))


def insert(db, table, rows):
    rows = list(rows)
    if rows:
        db.executemany(f"INSERT INTO {table} VALUES ({','.join('?' * len(rows[0]))})", rows)


def build_current(db, runs):
    for table, columns in CURRENT_SCHEMA.items():
        db.execute(f"DROP TABLE IF EXISTS {table}")
        db.execute(f"CREATE TABLE {table} ({columns})")
    sourced = {c: load_sourced(runs, c) for c in ("leagues", "series", "tournaments", "matches", "teams", "players")}
    listed = {c: latest_listing_ids(db, c) for c in ("leagues", "teams", "players")}

    def listing(collection, record):
        ids = listed[collection]
        return None if ids is None else int(str(record["id"]) in ids)

    insert(db, "ps_leagues", ((text(r["id"]), r.get("name"), r.get("slug"), r.get("url"), r.get("image_url"),
                               ref(r.get("videogame")), len(r.get("series") or []), listing("leagues", r), *provenance(s, r))
                              for r, s in sourced["leagues"]))
    insert(db, "ps_series", ((text(r["id"]), text(r.get("league_id")), r.get("name"), r.get("full_name"), r.get("slug"),
                              text(r.get("season")), text(r.get("year")), r.get("begin_at"), r.get("end_at"),
                              text(r.get("winner_id")), r.get("winner_type"), len(r.get("tournaments") or []),
                              stage_relevance(r.get("begin_at"), r.get("end_at")), *provenance(s, r))
                             for r, s in sourced["series"]))
    stage_rows, team_rows, registration_rows = [], [], []
    for r, s in sourced["tournaments"]:
        roster = list(roster_entries(r))
        rostered = {ref(e.get("team")): len(e.get("players") or []) for e in r.get("expected_roster") or [] if ref((e or {}).get("team"))}
        listed_teams = {ref(t) for t in r.get("teams") or [] if ref(t)}
        stage_rows.append((text(r["id"]), text(r.get("league_id")), text(r.get("serie_id")), r.get("name"), r.get("slug"),
                           r.get("tier"), r.get("region"), r.get("country"), r.get("type"), r.get("begin_at"), r.get("end_at"),
                           flag(r.get("has_bracket")), flag(r.get("live_supported")), flag(r.get("detailed_stats")),
                           text(r.get("prizepool")), text(r.get("winner_id")), r.get("winner_type"), len(listed_teams),
                           len(rostered), len(roster), len(r.get("matches") or []),
                           stage_relevance(r.get("begin_at"), r.get("end_at")), *provenance(s, r)))
        for team in sorted(listed_teams | rostered.keys()):
            players = rostered.get(team)
            status = "no_roster_entry" if players is None else "players_listed" if players else "empty_roster"
            team_rows.append((text(r["id"]), team, int(team in listed_teams), status, players or 0))
        seen = set()
        for team, player in roster:
            key = (team, str(player["id"]))
            if key not in seen:
                seen.add(key)
                registration_rows.append((text(r["id"]), team, str(player["id"]), player.get("role"), s["run"], s["observed_at"]))
    insert(db, "ps_tournaments", stage_rows)
    insert(db, "ps_tournament_teams", team_rows)
    insert(db, "ps_registrations", registration_rows)

    match_rows, game_rows = [], []
    for r, s in sourced["matches"]:
        opponents = [o for o in r.get("opponents") or [] if isinstance(o, dict)]
        sides = [(o.get("type"), ref(o.get("opponent"))) for o in opponents[:2]] + [(None, None)] * (2 - min(len(opponents), 2))
        scores = {text(x.get("team_id")): x.get("score") for x in r.get("results") or [] if isinstance(x, dict)}
        match_rows.append((text(r["id"]), text(r.get("tournament_id")), text(r.get("serie_id")), text(r.get("league_id")),
                           r.get("name"), r.get("slug"), r.get("status"), r.get("match_type"), r.get("number_of_games"),
                           r.get("scheduled_at"), r.get("original_scheduled_at"), r.get("begin_at"), r.get("end_at"),
                           flag(r.get("rescheduled")), flag(r.get("forfeit")), flag(r.get("draw")), flag(r.get("detailed_stats")),
                           sides[0][0], sides[0][1], sides[1][0], sides[1][1], scores.get(sides[0][1]), scores.get(sides[1][1]),
                           r.get("winner_type"), text(r.get("winner_id")), len(r.get("games") or []), match_relevance(r),
                           *provenance(s, r)))
        for g in r.get("games") or []:
            if isinstance(g, dict) and g.get("id") is not None:
                game_rows.append((text(g["id"]), text(r["id"]), g.get("position"), g.get("status"), flag(g.get("finished")),
                                  flag(g.get("complete")), flag(g.get("forfeit")), flag(g.get("detailed_stats")),
                                  g.get("winner_type"), ref(g.get("winner")), g.get("length"), g.get("begin_at"), g.get("end_at")))
    insert(db, "ps_matches", match_rows)
    insert(db, "ps_match_games", game_rows)

    insert(db, "ps_teams", ((text(r["id"]), r.get("name"), r.get("acronym"), r.get("slug"), r.get("location"), r.get("image_url"),
                             r.get("dark_mode_image_url"), len(r.get("players") or []), ref(r.get("current_videogame")),
                             listing("teams", r), *provenance(s, r)) for r, s in sourced["teams"]))
    insert(db, "ps_players", ((text(r["id"]), r.get("name"), r.get("slug"), r.get("first_name"), r.get("last_name"),
                               r.get("nationality"), r.get("role"), flag(r.get("active")), ref(r.get("current_team")),
                               r.get("image_url"), ref(r.get("current_videogame")), missing_fields(r), listing("players", r),
                               *provenance(s, r)) for r, s in sourced["players"]))

    image_rows = []
    for entity, collection, fields in (("league", "leagues", ("image_url",)), ("team", "teams", ("image_url", "dark_mode_image_url")),
                                       ("player", "players", ("image_url",))):
        for r, s in sourced[collection]:
            for field in fields:
                state, url = url_state(r, field)
                host = url.split("/")[2] if url and url.count("/") >= 2 else None
                image_rows.append((entity, text(r["id"]), field, state, url, host, LICENSE_REVIEW if url else None,
                                   s["run"], s["observed_at"]))
    insert(db, "ps_image_refs", image_rows)
    build_player_index(db)


def build_player_index(db):
    """Dataset B, rebuilt: also run by reconcile_identities.py after it writes xp_participation_2026."""
    db.execute("DROP TABLE IF EXISTS ps_player_index_2026")
    db.execute(f"CREATE TABLE ps_player_index_2026 ({CURRENT_SCHEMA['ps_player_index_2026']})")
    participation, lp_participation = {}, {}
    if db.execute("SELECT count(*) FROM sqlite_master WHERE name = 'xp_participation_2026'").fetchone()[0]:
        participation = {r[0]: r[1:] for r in db.execute("SELECT ps_player_id, games, source_file_sha256 FROM xp_participation_2026")}
    if db.execute("SELECT count(*) FROM sqlite_master WHERE name = 'lpx_participation_2026'").fetchone()[0]:
        lp_participation = {r[0]: r[1] for r in db.execute("SELECT ps_player_id, games FROM lpx_participation_2026")}
    stages = {r[0]: r[1:] for r in db.execute(
        "SELECT tournament_id, begin_at, end_at, relevance_2026 FROM ps_tournaments")}
    catalog = {r[0]: r[1:] for r in db.execute("SELECT player_id, current_team_id, provider_active_flag FROM ps_players")}
    evidence = {}
    for tournament, team, player, _, _, observed in db.execute(
            "SELECT * FROM ps_registrations ORDER BY tournament_id, team_id, player_id"):
        begin, end, relevance = stages[tournament]
        if relevance not in ("begins_in_2026", "ends_in_2026", "spans_2026"):
            continue
        e = evidence.setdefault(player, {"stages": set(), "teams": set(), "begins": [], "ends": [], "current": []})
        e["stages"].add(tournament)
        e["teams"].add(team)
        if begin:
            e["begins"].append(begin)
        if end:
            e["ends"].append(end)
        # A stage that had not ended when observed: the roster it lists is current as of that observation.
        if observed and end and end > observed:
            e["current"].append((observed[:10], begin or "", tournament, team))
    rows = []
    for player in sorted(evidence, key=int):
        e = evidence[player]
        current = max(e["current"]) if e["current"] else None
        team_now, active = catalog.get(player, (None, None))
        played, lp_played = participation.get(player), lp_participation.get(player)
        rows.append((player, int(player in catalog), "verified", len(e["stages"]), ",".join(sorted(e["teams"], key=int)),
                     min(e["begins"], default=None), max(e["ends"], default=None), "verified" if played or lp_played else "unknown",
                     current[0] if current else None, current[3] if current else None, "unknown", "unknown",
                     team_now, active,
                     f"expected_roster of {len(e['stages'])} stage(s) touching 2026"
                     + (f"; listed on stage {current[2]} not yet ended at observation" if current else "")
                     + (f"; {played[0]} game(s) in Oracle's Elixir file {played[1][:12]} via a high-confidence identity" if played else "")
                     + (f"; {lp_played} game(s) in Leaguepedia scoreboards via a high-confidence identity" if lp_played else "")))
    insert(db, "ps_player_index_2026", rows)


def normalize(db_path: Path, snapshot_root: Path):
    runs = completed_runs(snapshot_root)
    db = sqlite3.connect(db_path)
    try:
        db.execute("PRAGMA journal_mode=WAL")
        db.executescript(OBSERVATION_SCHEMA)
        pages = 0
        with db:
            for run in runs:
                pages += record_run(db, run)
            build_current(db, runs)
        counts = {t: db.execute(f"SELECT count(*) FROM {t}").fetchone()[0] for t in CURRENT_SCHEMA}
    finally:
        db.close()
    return {"runs": [r.name for r in runs], "pages_verified": pages, "current": counts}


# ---------- exports ----------

EXPORTS = {
    "players_catalog.csv": "SELECT * FROM ps_players ORDER BY CAST(player_id AS INTEGER)",
    "players_2026_index.csv": "SELECT * FROM ps_player_index_2026 ORDER BY CAST(player_id AS INTEGER)",
    "teams.csv": "SELECT * FROM ps_teams ORDER BY CAST(team_id AS INTEGER)",
    "leagues.csv": "SELECT * FROM ps_leagues ORDER BY CAST(league_id AS INTEGER)",
    "series_2026.csv": "SELECT * FROM ps_series WHERE relevance_2026 <> 'outside_2026' OR year_label = '2026' ORDER BY CAST(serie_id AS INTEGER)",
    "tournaments_2026.csv": "SELECT * FROM ps_tournaments WHERE relevance_2026 <> 'outside_2026' ORDER BY CAST(tournament_id AS INTEGER)",
    "tournament_teams_2026.csv": """SELECT tt.* FROM ps_tournament_teams tt JOIN ps_tournaments t USING (tournament_id)
        WHERE t.relevance_2026 <> 'outside_2026' ORDER BY CAST(tt.tournament_id AS INTEGER), CAST(tt.team_id AS INTEGER)""",
    "tournament_registrations_2026.csv": """SELECT r.* FROM ps_registrations r JOIN ps_tournaments t USING (tournament_id)
        WHERE t.relevance_2026 <> 'outside_2026'
        ORDER BY CAST(r.tournament_id AS INTEGER), CAST(r.team_id AS INTEGER), CAST(r.player_id AS INTEGER)""",
    "matches_2026.csv": "SELECT * FROM ps_matches WHERE relevance_2026 <> 'outside_2026' ORDER BY CAST(match_id AS INTEGER)",
    "match_games_2026.csv": """SELECT g.* FROM ps_match_games g JOIN ps_matches m USING (match_id)
        WHERE m.relevance_2026 <> 'outside_2026' ORDER BY CAST(g.game_id AS INTEGER)""",
    "image_refs.csv": "SELECT * FROM ps_image_refs ORDER BY entity_type, CAST(entity_id AS INTEGER), field",
}
EXPORT_NOTE = """Source: PandaScore. Local research exports from data/staging.sqlite3 (normalize_pandascore.py --export).
Provider data: do not commit, publish or redistribute (PandaScore terms 6.3-6.4). Images: URLs only; every logo is
LICENSE_REVIEW_REQUIRED (third-party marks; the provider terms grant no image rights).
Current rows are the provider's latest report, not historical facts. 'unknown' means no supporting evidence.
"""


def export(db_path: Path, out_dir: Path):
    out_dir.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(read_only(db_path), uri=True)
    written = {}
    for name, sql in EXPORTS.items():
        cursor = db.execute(sql)
        with (out_dir / name).open("w", newline="", encoding="utf-8-sig") as f:
            writer = csv.writer(f)
            writer.writerow([c[0] for c in cursor.description])
            rows = cursor.fetchall()
            writer.writerows(rows)
        written[name] = len(rows)
    db.close()
    (out_dir / "README.txt").write_text(EXPORT_NOTE, encoding="utf-8")
    return written


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DB_PATH)
    parser.add_argument("--snapshot-root", type=Path, default=SNAPSHOT_ROOT)
    parser.add_argument("--export", type=Path, nargs="?", const=EXPORT_DIR, metavar="DIR",
                        help=f"also write research CSV exports (default {EXPORT_DIR})")
    args = parser.parse_args()
    result = normalize(args.db, args.snapshot_root)
    print(f"Normalized {len(result['runs'])} completed runs ({result['pages_verified']} pages SHA-256 verified)")
    for table, count in result["current"].items():
        print(f"  {table}: {count}")
    if args.export:
        for name, count in export(args.db, args.export).items():
            print(f"  export {name}: {count} rows")
