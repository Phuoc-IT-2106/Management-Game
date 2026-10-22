"""Offline tests for build_real_rosters.py (synthetic data only, no network, no real names).

Covers: a verified starting five chosen by participation, not by rating; substitutes from the latest registration;
unresolved identities, unregistered starters and thin participation leaving a team INCOMPLETE; the authoritative employer
and ownership conflicts; national-team games never deciding a club; superseded entities; stages after the as-of day
being ignored; and the squad limit.
"""
from datetime import date
from pathlib import Path
import sqlite3
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import build_real_rosters as br

ROLES = ("top", "jng", "mid", "bot", "sup")


class Fixture:
    def __init__(self):
        self.db = sqlite3.connect(":memory:")
        self.db.executescript("""
            CREATE TABLE ps_tournaments (tournament_id TEXT, league_id TEXT, serie_id TEXT, name TEXT, tier TEXT, begin_at TEXT, end_at TEXT, type TEXT);
            CREATE TABLE ps_leagues (league_id TEXT, name TEXT);
            CREATE TABLE ps_teams (team_id TEXT, name TEXT, acronym TEXT, location TEXT);
            CREATE TABLE ps_players (player_id TEXT, name TEXT, nationality TEXT, role TEXT);
            CREATE TABLE ps_registrations (tournament_id TEXT, team_id TEXT, player_id TEXT, role TEXT);
            CREATE TABLE pf_games (game_key TEXT, game_date TEXT, game_ts TEXT, blue_ps_team_id TEXT, red_ps_team_id TEXT,
                                   ps_stage_id TEXT, ps_tier TEXT, is_primary INTEGER, year INTEGER);
            CREATE TABLE pf_player_games (game_key TEXT, side TEXT, role TEXT, identity_status TEXT, ps_player_id TEXT,
                                          identity_key TEXT, handle TEXT);
        """)
        self.games = 0
        self.db.executemany("INSERT INTO ps_leagues VALUES (?, ?)", [("L1", "Test League"), ("L2", "Other League"), ("NAT", "Asian Games")])

    def stage(self, sid, league, begin, end, tier="a"):
        self.db.execute("INSERT INTO ps_tournaments VALUES (?, ?, 'S', ?, ?, ?, ?, 'online')", (sid, league, "Stage " + sid, tier, begin + "T00:00:00Z", end + "T00:00:00Z"))

    def team(self, tid):
        self.db.execute("INSERT INTO ps_teams VALUES (?, ?, ?, 'XX')", (tid, "Team " + tid, tid))

    def register(self, stage, team, players):
        for pid, role in players:
            self.db.execute("INSERT OR IGNORE INTO ps_players VALUES (?, ?, 'XX', ?)", (pid, "h" + pid, role))
            self.db.execute("INSERT INTO ps_registrations VALUES (?, ?, ?, ?)", (stage, team, pid, role))

    def game(self, day, stage, blue, red, blue_seats, red_seats):
        """seats: list of (role, ps_id or None, status) in ROLES order."""
        self.games += 1
        key = f"g{self.games:04d}"
        self.db.execute("INSERT INTO pf_games VALUES (?, ?, ?, ?, ?, ?, 'a', 1, 2026)", (key, day, day + "T12:00:00", blue, red, stage))
        for side, seats in (("Blue", blue_seats), ("Red", red_seats)):
            for role, pid, status in seats:
                self.db.execute("INSERT INTO pf_player_games VALUES (?, ?, ?, ?, ?, ?, ?)",
                                (key, side, role, status, pid if status == br.HIGH else None, "id:" + str(pid), "h" + str(pid)))

    @staticmethod
    def five(prefix, status=br.HIGH, override=None):
        seats = [(r, f"{prefix}{i}", status) for i, r in enumerate(ROLES, 1)]
        for index, seat in (override or {}).items():
            seats[index] = seat
        return seats


def run(fx, as_of=date(2026, 10, 13), ratings=None):
    teams, people, conflicts, history = br.build(fx.db, as_of, ratings or {})
    return {t["team_id"]: t for t in teams}, people, conflicts


class RealRosterTests(unittest.TestCase):
    def base(self):
        fx = Fixture()
        fx.stage("S1", "L1", "2026-06-01", "2026-08-30")
        for t in ("A", "B"):
            fx.team(t)
        fx.register("S1", "A", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc"), ("a5", "sup"), ("a6", "mid")])
        fx.register("S1", "B", [("b1", "top"), ("b2", "jun"), ("b3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(6):
            fx.game(f"2026-07-{10 + d:02d}", "S1", "A", "B", fx.five("a"), fx.five("b"))
        return fx

    def test_verified_five_by_participation_not_rating(self):
        fx = self.base()
        # The registered substitute a6 is rated far higher but played one game only.
        fx.game("2026-07-20", "S1", "A", "B", fx.five("a", override={2: ("mid", "a6", br.HIGH)}), fx.five("b"))
        teams, people, _ = run(fx, ratings={"a6": {"overall_rating": 95}, "a3": {"overall_rating": 60}})
        a = teams["A"]
        self.assertEqual(a["status"], "VERIFIED")
        self.assertEqual({s["role"]: s["player_id"] for s in a["starters"]}["mid"], "a3")
        self.assertEqual(a["substitutes"], ["a6"])
        self.assertEqual(a["squad_size"], 6)
        self.assertEqual(people["a6"]["squad_role"], "SUBSTITUTE")

    def test_unresolved_identity_leaves_team_incomplete(self):
        fx = self.base()
        fx.stage("S2", "L1", "2026-09-01", "2026-09-30")
        fx.register("S2", "A", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc"), ("a5", "sup")])
        fx.register("S2", "B", [("b1", "top"), ("b2", "jun"), ("b3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(4):
            fx.game(f"2026-09-{10 + d:02d}", "S2", "A", "B", fx.five("a", override={4: ("sup", "x9", "REVIEW_REQUIRED")}), fx.five("b"))
        teams, _, _ = run(fx)
        self.assertEqual(teams["A"]["status"], "INCOMPLETE")
        self.assertIn("IDENTITY_UNRESOLVED", teams["A"]["reasons"])
        self.assertEqual(teams["B"]["status"], "VERIFIED")

    def test_unregistered_starter_is_not_verified(self):
        fx = self.base()
        fx.stage("S2", "L1", "2026-09-01", "2026-09-30")
        fx.register("S2", "A", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc")])  # a5 not registered
        fx.register("S2", "B", [("b1", "top"), ("b2", "jun"), ("b3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(4):
            fx.game(f"2026-09-{10 + d:02d}", "S2", "A", "B", fx.five("a"), fx.five("b"))
        teams, _, _ = run(fx)
        self.assertEqual(teams["A"]["status"], "INCOMPLETE")
        self.assertIn("NOT_REGISTERED", teams["A"]["reasons"])

    def test_thin_participation_is_not_a_starter(self):
        fx = Fixture()
        fx.stage("S1", "L1", "2026-06-01", "2026-08-30")
        fx.team("A"); fx.team("B")
        fx.register("S1", "A", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc"), ("a5", "sup")])
        fx.register("S1", "B", [("b1", "top"), ("b2", "jun"), ("b3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(2):   # two games only: below MIN_ROLE_GAMES
            fx.game(f"2026-07-{10 + d:02d}", "S1", "A", "B", fx.five("a"), fx.five("b"))
        teams, _, _ = run(fx)
        self.assertEqual(teams["A"]["status"], "INCOMPLETE")
        self.assertIn("INSUFFICIENT_PARTICIPATION", teams["A"]["reasons"])

    def test_ownership_conflict_and_superseded_entity(self):
        fx = self.base()
        # Team C is a later provider entity: four of A's starters play for it from September.
        fx.team("C"); fx.team("D")
        fx.stage("S3", "L2", "2026-09-01", "2026-10-10")
        fx.register("S3", "C", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc"), ("c5", "sup")])
        fx.register("S3", "D", [("d1", "top"), ("d2", "jun"), ("d3", "mid"), ("d4", "adc"), ("d5", "sup")])
        for d in range(5):
            fx.game(f"2026-09-{10 + d:02d}", "S3", "C", "D", fx.five("a", override={4: ("sup", "c5", br.HIGH)}), fx.five("d"))
        teams, people, conflicts = run(fx)
        self.assertEqual(teams["C"]["status"], "VERIFIED")
        self.assertEqual(teams["A"]["status"], "SUPERSEDED")
        self.assertEqual(teams["A"]["superseded_by"], "C")
        self.assertTrue(all(c["authoritative_team_id"] == "C" for c in conflicts if c["team_id"] == "A"))
        self.assertEqual(people["a1"]["team_id"], "C")
        # No player sits in two verified squads.
        memberships = [p for t in teams.values() if t["status"] == "VERIFIED" for p in [s["player_id"] for s in t["starters"]] + t["substitutes"]]
        self.assertEqual(len(memberships), len(set(memberships)))

    def test_national_team_games_never_decide_the_club(self):
        fx = self.base()
        fx.team("N")
        fx.stage("SN", "NAT", "2026-09-20", "2026-09-30", tier="d")
        fx.register("SN", "N", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(3):
            fx.game(f"2026-09-{21 + d:02d}", "SN", "N", "B", [("top", "a1", br.HIGH), ("jng", "a2", br.HIGH), ("mid", "a3", br.HIGH),
                                                              ("bot", "b4", br.HIGH), ("sup", "b5", br.HIGH)], fx.five("z"))
        teams, _, _ = run(fx)
        self.assertNotIn("N", teams)
        self.assertEqual(teams["A"]["status"], "VERIFIED")

    def test_stage_after_as_of_is_ignored(self):
        fx = self.base()
        fx.stage("S9", "L1", "2026-10-20", "2026-11-10")
        fx.register("S9", "A", [("z1", "top"), ("z2", "jun"), ("z3", "mid"), ("z4", "adc"), ("z5", "sup")])
        teams, _, _ = run(fx)
        self.assertEqual(teams["A"]["latest_stage"]["id"], "S1")
        self.assertEqual(teams["A"]["status"], "VERIFIED")

    def test_squad_limit_flag(self):
        fx = self.base()
        fx.stage("S2", "L1", "2026-09-01", "2026-09-30")
        fx.register("S2", "A", [("a1", "top"), ("a2", "jun"), ("a3", "mid"), ("a4", "adc"), ("a5", "sup")]
                    + [(f"s{i}", "mid") for i in range(12)])
        fx.register("S2", "B", [("b1", "top"), ("b2", "jun"), ("b3", "mid"), ("b4", "adc"), ("b5", "sup")])
        for d in range(4):
            fx.game(f"2026-09-{10 + d:02d}", "S2", "A", "B", fx.five("a"), fx.five("b"))
        teams, _, _ = run(fx)
        self.assertEqual(teams["A"]["squad_size"], 17)
        self.assertTrue(teams["A"]["squad_over_limit"])


if __name__ == "__main__":
    unittest.main()
