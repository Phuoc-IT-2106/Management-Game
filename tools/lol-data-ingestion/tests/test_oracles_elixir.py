"""Offline tests for the Oracle's Elixir import and the cross-provider identity reconciliation (synthetic data only)."""
import csv
import hashlib
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parent))
import import_oracles_elixir as oe
import normalize_pandascore as norm
import reconcile_identities as xp
from test_full_collection import write_run

ROLES = {"top": "top", "jng": "jun", "mid": "mid", "bot": "adc", "sup": "sup"}
# PandaScore world: catalogue players (id, name, catalogue role, current team), teams, and two 2026 stages.
PLAYERS = [(1, "Alpha", "top", None), (2, "Bravo", "jun", None), (3, "Charlie", "mid", None), (4, "Delta", "adc", None),
           (5, "Echo", "sup", None), (6, "Mover", "mid", None), (7, "Twin", "sup", None), (8, "Twin", "sup", None),
           (9, "Bench", "top", None), (11, "Ghost", "mid", 10), (12, "Rolex", "mid", None), (13, "Late", "jun", None),
           (14, "Renamed", "adc", None), (21, "Golf", "top", None), (22, "Hotel", "jun", None), (23, "India", "mid", None),
           (24, "Juliet", "adc", None), (31, "Kilo", "top", None), (32, "Lima", "jun", None), (33, "Mike", "mid", None),
           (44, "Quebec", "top", None), (45, "Romeo", "jun", None), (46, "Sierra", "mid", None)]
TEAMS = [(10, "Team Ten", "TTN"), (20, "Twenty Gaming", "TWG"), (30, "Thirty", "TTY"), (40, "Ambig A", "AMA"), (50, "Ambig B", "AMB")]
NAMES = {p[0]: p[1] for p in PLAYERS}
SPRING = {10: [(1, "top"), (2, "jun"), (3, "mid"), (4, "adc"), (5, "sup"), (9, "top")],
          20: [(21, "top"), (22, "jun"), (23, "mid"), (24, "adc"), (7, "sup"), (6, "mid"), (13, "jun")],
          30: [(31, "top"), (32, "jun"), (33, "mid"), (14, "adc"), (8, "sup")],
          40: [(44, "top"), (45, "jun"), (46, "mid")], 50: [(44, "top"), (45, "jun"), (46, "mid")]}
SUMMER = {10: [(1, "top"), (2, "jun"), (6, "mid"), (4, "adc"), (12, "mid")],
          20: [(21, "top"), (22, "jun"), (23, "mid"), (24, "adc"), (7, "sup")],
          30: [(31, "top"), (32, "jun"), (33, "mid"), (14, "adc"), (8, "sup")]}


def stage(tid, begin, end, rosters):
    return {"id": tid, "league_id": 1, "serie_id": 10, "name": f"Stage {tid}", "tier": "a", "region": "ASIA", "begin_at": begin,
            "end_at": end, "modified_at": "2026-01-01T00:00:00Z", "teams": [{"id": t} for t in rosters], "matches": [],
            "expected_roster": [{"team": {"id": t}, "players": [{"id": p, "name": NAMES[p], "role": r} for p, r in roster]}
                                for t, roster in rosters.items()]}


def build_world(root, db):
    players = [{"id": i, "name": n, "role": r, "current_team": {"id": t} if t else None, "modified_at": "2026-01-01T00:00:00Z"}
               for i, n, r, t in PLAYERS]
    teams = [{"id": i, "name": n, "acronym": a, "slug": n.lower().replace(" ", "-"), "players": [], "modified_at": "2026-01-01T00:00:00Z"}
             for i, n, a in TEAMS]
    write_run(root, "20261013T000000Z", {
        "players": ("players", "/lol/players", {"sort": "id"}, [players]),
        "teams": ("teams", "/lol/teams", {"sort": "id"}, [teams]),
        "tournaments-begin-2026": ("tournaments", "/lol/tournaments", {"range[begin_at]": "2026"}, [[
            stage(101, "2026-01-10T00:00:00Z", "2026-04-10T00:00:00Z", SPRING),
            stage(102, "2026-06-01T00:00:00Z", "2026-08-31T00:00:00Z", SUMMER)]])}, "2026-10-13T00:00:00+00:00")
    norm.normalize(db, root)


# Oracle's Elixir side: (handle, OE playerid or "", position) per seat; teams by OE id and name.
OE_TEAMS = {"A": ("oe:team:A", "Team Ten"), "B": ("oe:team:B", "Twenty"), "C": ("oe:team:C", "Thirty Esports"), "D": ("oe:team:D", "Delta Squad")}


def seat(handle, position, pid=None):
    return (handle, "" if pid == "" else (pid or f"oe:player:{handle.lower()}"), position)


A_SPRING = [seat("Alpha", "top"), seat("Bravo", "jng"), seat("Charlie", "mid"), seat("Delta", "bot"), seat("Echo", "sup")]
A_SUMMER = [seat("Alpha", "top"), seat("Bravo", "jng"), seat("Mover", "mid"), seat("Delta", "bot"), seat("Rolex", "sup")]
B_SPRING = [seat("Golf", "top"), seat("Hotel", "jng"), seat("Mover", "mid"), seat("Juliet", "bot"), seat("Twin", "sup", "oe:player:twinB")]
B_SUMMER = [seat("Golf", "top"), seat("Late", "jng"), seat("India", "mid"), seat("Juliet", "bot"), seat("Twin", "sup", "oe:player:twinB")]
C_SPRING = [seat("Kilo", "top"), seat("Lima", "jng"), seat("Mike", "mid"), seat("OldName", "bot", "oe:player:ren"), seat("Twin", "sup", "oe:player:twinC")]
C_SUMMER = C_SPRING[:3] + [seat("Renamed", "bot", "oe:player:ren"), C_SPRING[4]]
GAMES = [("G1", "2026-02-01 10:00:00", ("A", A_SPRING), ("B", B_SPRING)),
         ("G2", "2026-02-08 10:00:00", ("A", A_SPRING), ("C", C_SPRING)),
         ("G3", "2026-07-01 10:00:00", ("A", A_SUMMER), ("B", B_SUMMER)),
         ("G4", "2026-07-08 10:00:00", ("A", A_SUMMER), ("C", C_SUMMER)),
         ("G5", "2026-03-01 10:00:00", ("D", [seat("Quebec", "top"), seat("Romeo", "jng"), seat("Sierra", "mid"), seat("Tango", "bot"),
                                               seat("Uniform", "flex")]), ("B", B_SPRING[:2] + [seat("India", "mid")] + B_SPRING[3:])),
         ("G6", "2026-03-15 10:00:00", ("A", A_SPRING[:2] + [seat("Ghost", "mid")] + A_SPRING[3:4] + [seat("Echo", "Support")]),
          ("C", C_SPRING[:2] + [seat("Mike", "mid", "")] + C_SPRING[3:]))]
HEADER = ["gameid", "datacompleteness", "url", "league", "year", "split", "playoffs", "date", "game", "patch", "participantid",
          "side", "position", "playername", "playerid", "teamname", "teamid", "champion", "gamelength", "result", "kills",
          "deaths", "assists", "teamkills", "teamdeaths", "total cs", "totalgold", "earnedgold", "damagetochampions", "dpm",
          "visionscore", "wardsplaced", "goldat10", "xpat10", "csat10", "golddiffat10"]


def game_rows(game, stamp, blue, red):
    rows = []
    for side_index, ((team, seats), side) in enumerate(((blue, "Blue"), (red, "Red"))):
        team_id, team_name = OE_TEAMS[team]
        base = {"gameid": game, "datacompleteness": "complete", "league": "LCK", "year": "2026", "split": "Spring",
                "playoffs": "0", "date": stamp, "game": "1", "patch": "26.01", "side": side, "teamname": team_name,
                "teamid": team_id, "gamelength": "1800", "result": str(1 - side_index)}
        for number, (handle, pid, position) in enumerate(seats, start=1 + 5 * side_index):
            rows.append(base | {"participantid": str(number), "position": position, "playername": handle, "playerid": pid,
                                "champion": "Champ", "kills": "3", "deaths": "1", "assists": "4", "teamkills": "12",
                                "teamdeaths": "8", "total cs": "250", "totalgold": "12000", "earnedgold": "8000",
                                "damagetochampions": "15000", "dpm": "500", "visionscore": "40", "wardsplaced": "12",
                                "goldat10": "3400", "xpat10": "4100", "csat10": "85", "golddiffat10": "150"})
        rows.append(base | {"participantid": str(100 * (side_index + 1)), "position": "team", "playername": "", "playerid": ""})
    return rows


def write_csv(path, rows, header=HEADER):
    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=header, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)
    return path


def base_rows():
    rows = [r for g in GAMES for r in game_rows(*g)]
    rows.append(dict(rows[0]))                      # exact duplicate of a player row
    rows.append(dict(rows[1]) | {"kills": "9"})     # conflicting duplicate
    rows.append(game_rows("G0", "2025-12-30 10:00:00", ("A", A_SPRING), ("B", B_SPRING))[0])  # dated outside 2026
    return rows


def digest_tree(root):
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(root.rglob("*")) if p.is_file()}


class OracleBase(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.dir = Path(self.temp.name)
        self.raw = self.dir / "raw"
        self.db = self.dir / "db.sqlite3"
        build_world(self.raw, self.db)

    def tearDown(self):
        self.temp.cleanup()

    def query(self, sql, *args):
        db = sqlite3.connect(self.db)
        try:
            return db.execute(sql, args).fetchall()
        finally:
            db.close()

    def run_all(self, rows=None, name="2026_LoL_esports_match_data_from_OraclesElixir.csv"):
        path = write_csv(self.dir / name, rows if rows is not None else base_rows())
        result = oe.import_source(path, self.db)
        db = sqlite3.connect(self.db)
        try:
            xp.reconcile(db)
        finally:
            db.close()
        return result

    def identity(self, key):
        return self.query("SELECT status, ps_player_id, reason FROM xp_identity_map WHERE oe_identity_key = ?", key)[0]


class ImportTests(OracleBase):
    def test_validation_and_repeatable_import(self):
        first = self.run_all()
        self.assertEqual(first["status"], "imported")
        self.assertEqual((first["duplicate_rows"], first["conflicting_duplicates"], first["out_of_period_rows"]), (2, 1, 1))
        self.assertEqual(first["invalid_position_rows"], 2)  # "flex" and "Support"
        self.assertEqual((first["period_from"], first["period_to"]), ("2026-02-01", "2026-07-08"))
        self.assertIn("visionscore", HEADER)
        self.assertIn("vspm", first["missing_columns"])  # columns the file lacks are listed, never filled
        counts = self.query("SELECT (SELECT count(*) FROM oe_row_observations), (SELECT count(*) FROM oe_raw_rows), (SELECT count(*) FROM oe_player_games)")
        again = oe.import_source(self.dir / "2026_LoL_esports_match_data_from_OraclesElixir.csv", self.db)
        self.assertEqual(again["status"], "already imported")
        self.assertEqual(counts, self.query("SELECT (SELECT count(*) FROM oe_row_observations), (SELECT count(*) FROM oe_raw_rows), (SELECT count(*) FROM oe_player_games)"))
        # The first of two duplicate rows is current; the conflicting copy is kept as an observation only.
        self.assertEqual(self.query("SELECT kills FROM oe_player_game_stats WHERE game_id='G1' AND participant_id=2"), [(3.0,)])
        self.assertEqual(self.query("SELECT count(*) FROM oe_row_observations WHERE duplicate_of_row IS NOT NULL"), [(2,)])

    def test_a_newer_version_stores_only_changed_rows(self):
        rows = base_rows()
        self.run_all(rows)
        raw_before = self.query("SELECT count(*) FROM oe_raw_rows")[0][0]
        rows[3] = dict(rows[3]) | {"kills": "7"}
        oe.import_source(write_csv(self.dir / "newer.csv", rows), self.db)
        self.assertEqual(self.query("SELECT count(*) FROM oe_source_files"), [(2,)])
        self.assertEqual(self.query("SELECT count(*) FROM oe_raw_rows")[0][0], raw_before + 1)
        self.assertEqual(self.query("SELECT kills FROM oe_player_game_stats WHERE game_id='G1' AND participant_id=4"), [(7.0,)])

    def test_rejections_write_nothing(self):
        cases = {"missing position": write_csv(self.dir / "a.csv", base_rows(), [c for c in HEADER if c != "position"]),
                 "last year": write_csv(self.dir / "b.csv", [r | {"date": r["date"].replace("2026", "2025")} for r in base_rows()]),
                 "not csv": write_csv(self.dir / "c.txt", base_rows())}
        bad = self.dir / "d.csv"
        bad.write_bytes(b"gameid,participantid\n\xff\xfe\n")
        cases["not utf-8"] = bad
        for label, path in cases.items():
            with self.subTest(label), self.assertRaises(oe.ImportRejected):
                oe.import_source(path, self.db)
        self.assertEqual(self.query("SELECT count(*) FROM sqlite_master WHERE name LIKE 'oe_%'"), [(0,)])

    def test_missing_statistics_columns_stay_null(self):
        header = [c for c in HEADER if c not in ("visionscore", "goldat10", "golddiffat10")]
        result = oe.import_source(write_csv(self.dir / "thin.csv", base_rows(), header), self.db)
        self.assertTrue({"visionscore", "goldat10", "golddiffat10"} <= set(result["missing_columns"]))
        self.assertEqual(self.query("SELECT count(*) FROM oe_player_game_stats WHERE visionscore IS NOT NULL OR goldat10 IS NOT NULL"), [(0,)])
        self.assertEqual(self.query("SELECT count(*) FROM oe_player_game_stats WHERE kills IS NULL"), [(0,)])


class ReconcileTests(OracleBase):
    def test_team_aliases_need_roster_overlap_not_names(self):
        self.run_all()
        aliases = {r[0]: r[1:] for r in self.query("SELECT oe_team_key, ps_team_id, status, runner_up_players FROM xp_team_aliases")}
        self.assertEqual(aliases["oe:team:A"][:2], ("10", "HIGH_CONFIDENCE_MATCH"))
        self.assertEqual(aliases["oe:team:B"][:2], ("20", "HIGH_CONFIDENCE_MATCH"))
        self.assertEqual(aliases["oe:team:C"][:2], ("30", "HIGH_CONFIDENCE_MATCH"))
        self.assertEqual(aliases["oe:team:D"][1:], ("REVIEW_REQUIRED", 3))  # overlaps two provider teams equally

    def test_identity_outcomes(self):
        self.run_all()
        expect_high = {"oe:player:alpha": "1", "oe:player:mover": "6", "oe:player:twinB": "7",
                       "oe:player:twinC": "8", "oe:player:ren": "14", "oe:player:mike": "33"}
        for key, player in expect_high.items():
            with self.subTest(key):
                self.assertEqual(self.identity(key)[:2], ("HIGH_CONFIDENCE_MATCH", player))
        # Rows without an OE id join the OE-id claim under the same handle.
        self.assertEqual(self.identity("league_team_handle:LCK|oe:team:C|mike")[:2], ("HIGH_CONFIDENCE_MATCH", "33"))
        reviews = {"oe:player:ghost": "not registered for the mapped team",          # current team only: never enough
                   "oe:player:rolex": "role differs from registration",
                   "oe:player:late": "game date outside every registration window",
                   "oe:player:quebec": "team alias not high-confidence"}
        for key, reason in reviews.items():
            with self.subTest(key):
                self.assertEqual(self.identity(key), ("REVIEW_REQUIRED", None, reason))
        self.assertEqual(self.identity("oe:player:tango")[0], "UNMATCHED")
        self.assertEqual(self.query("SELECT row_status, reason FROM xp_row_matches WHERE game_id='G6' AND participant_id=5"),
                         [("REVIEW_REQUIRED", "invalid OE position")])
        self.assertEqual(self.query("SELECT row_status, reason FROM xp_row_matches WHERE game_id='G0'"), [("UNMATCHED", "dated outside 2026")])

    def test_participation_only_from_high_identities(self):
        self.run_all()
        verified = {r[0]: r[1:] for r in self.query("SELECT ps_player_id, games, supporting_rows, oe_team_keys FROM xp_participation_2026")}
        self.assertEqual(sorted(verified, key=int), ["1", "2", "3", "4", "5", "6", "7", "8", "14", "21", "22", "23", "24", "31", "32", "33"])
        self.assertEqual(verified["6"][2], json.dumps(["oe:team:A", "oe:team:B"]))  # a transfer during the year
        self.assertEqual(verified["14"][:2], (3, 1))  # three games under two handles, one row with direct evidence
        self.assertEqual(verified["1"][0], 5)  # the 2025 row and the duplicate do not count
        index = dict(self.query("SELECT player_id, match_participation_verified_2026 FROM ps_player_index_2026"))
        self.assertEqual({p for p, s in index.items() if s == "unknown"}, {"9", "12", "13", "44", "45", "46"})
        self.assertNotIn("11", index)  # current team without registration: not even in the 2026 index
        self.assertEqual(self.query("SELECT academy_verified, reserve_verified, count(*) FROM ps_player_index_2026 GROUP BY 1, 2"),
                         [("unknown", "unknown", 22)])

    def test_two_oe_ids_claiming_one_player_are_held_for_review(self):
        rows = base_rows()
        rows += [r | {"playerid": "oe:player:echo2"} if r.get("playername") == "Echo" else r
                 for r in game_rows("G7", "2026-03-20 10:00:00", ("A", A_SPRING), ("B", B_SPRING))]
        self.run_all(rows)
        for key in ("oe:player:echo", "oe:player:echo2"):
            self.assertEqual(self.identity(key)[0], "REVIEW_REQUIRED")
        self.assertEqual(self.query("SELECT count(*) FROM xp_participation_2026 WHERE ps_player_id = '5'"), [(0,)])

    def test_same_handle_on_the_mapped_team_is_not_forced(self):
        # A later observation lists the second "Twin" (id 8) on team 20 as well: both qualify for team B's rows.
        spring = {t: list(r) for t, r in SPRING.items()}
        spring[20].append((8, "sup"))
        later = stage(101, "2026-01-10T00:00:00Z", "2026-04-10T00:00:00Z", spring) | {"modified_at": "2026-10-14T00:00:00Z"}
        write_run(self.raw, "20261014T000000Z", {"tournaments-begin-2026": ("tournaments", "/lol/tournaments", {}, [[later]])},
                  "2026-10-14T00:00:00+00:00")
        norm.normalize(self.db, self.raw)
        self.run_all()
        status, player, reason = self.identity("oe:player:twinB")
        self.assertEqual((status, player), ("REVIEW_REQUIRED", None))
        self.assertIn("provider players", reason)
        self.assertEqual(self.query("SELECT count(*) FROM xp_participation_2026 WHERE ps_player_id IN ('7', '8')"), [(1,)])  # 8 via team C only

    def test_rebuild_is_idempotent_intact_and_leaves_raw_snapshots_alone(self):
        before = digest_tree(self.raw)
        self.run_all()
        first = self.query("SELECT * FROM xp_identity_map ORDER BY 1") + self.query("SELECT * FROM xp_row_matches ORDER BY 1, 2")
        db = sqlite3.connect(self.db)
        xp.reconcile(db)
        db.close()
        self.assertEqual(first, self.query("SELECT * FROM xp_identity_map ORDER BY 1") + self.query("SELECT * FROM xp_row_matches ORDER BY 1, 2"))
        self.assertEqual(self.query("PRAGMA integrity_check"), [("ok",)])
        self.assertEqual(before, digest_tree(self.raw))
        # A later normalize keeps the verified participation (it reads xp_participation_2026).
        norm.normalize(self.db, self.raw)
        self.assertEqual(self.query("SELECT count(*) FROM ps_player_index_2026 WHERE match_participation_verified_2026 = 'verified'"), [(16,)])

    def test_statistics_report_counts_without_names(self):
        self.run_all()
        db = sqlite3.connect(self.db)
        try:
            stats = xp.statistics(db)
        finally:
            db.close()
        self.assertEqual(stats["participation_verified_players"], 16)
        self.assertEqual(stats["registered_without_confirmed_participation"]["total"], 6)
        text = json.dumps(stats)
        for secret in ("Alpha", "Ghost", "Team Ten", "oe:player:"):
            self.assertNotIn(secret, text)


if __name__ == "__main__":
    unittest.main()
