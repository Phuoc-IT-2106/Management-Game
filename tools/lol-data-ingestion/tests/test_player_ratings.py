"""Offline tests for the 2025-2026 performance and rating pipeline (synthetic data only, no network).

Covers: multi-year Oracle's Elixir import without data loss; the Leaguepedia adapter (paging, completeness, immutable
pages); per-year team aliases; the rating formulas (Overall = the game's role-weighted, round-half-even formula);
shrinkage of small samples; missing features not inflating ratings; form without recent games; and an end-to-end run
in which every catalogue player gets exactly one record and rankings hold only eligible, unique players.
"""
import csv
from datetime import date, timedelta
import hashlib
import json
from pathlib import Path
import random
import sqlite3
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_performance as bp
import fetch_leaguepedia_stats as lps
import import_oracles_elixir as oe
import normalize_pandascore as norm
import rate_players as rp
import rating_model as rm
import reconcile_identities as xp
from test_full_collection import write_run

WEIGHTS = rp.load_content()["weights"]
ROLES_LP = ("Top", "Jungle", "Mid", "Bot", "Support")


# ---------- Oracle's Elixir: two reporting years ----------

OE_HEADER = ["gameid", "datacompleteness", "league", "year", "date", "patch", "participantid", "side", "position",
             "playername", "playerid", "teamname", "teamid", "champion", "gamelength", "result", "kills", "deaths", "assists"]


def oe_game(game, stamp, kills="3"):
    rows = []
    for side_index, side in enumerate(("Blue", "Red")):
        for n, position in enumerate(("top", "jng", "mid", "bot", "sup"), start=1 + 5 * side_index):
            rows.append({"gameid": game, "datacompleteness": "complete", "league": "LCK", "year": stamp[:4], "date": stamp,
                         "patch": "15.1", "participantid": str(n), "side": side, "position": position, "playername": f"P{n}",
                         "playerid": f"oe:player:{n}", "teamname": side, "teamid": f"oe:team:{side}", "champion": "C",
                         "gamelength": "1800", "result": str(1 - side_index), "kills": kills, "deaths": "1", "assists": "2"})
        rows.append({"gameid": game, "datacompleteness": "complete", "league": "LCK", "year": stamp[:4], "date": stamp,
                     "patch": "15.1", "participantid": str(100 * (side_index + 1)), "side": side, "position": "team",
                     "teamname": side, "teamid": f"oe:team:{side}", "gamelength": "1800", "result": str(1 - side_index)})
    return rows


def write_oe(path, rows):
    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=OE_HEADER, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)
    return path


class MultiYearImportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.dir = Path(self.temp.name)
        self.db = self.dir / "db.sqlite3"

    def tearDown(self):
        self.temp.cleanup()

    def query(self, sql):
        db = sqlite3.connect(self.db)
        try:
            return db.execute(sql).fetchall()
        finally:
            db.close()

    def test_importing_2026_keeps_2025_and_versions_stay_per_year(self):
        y2025 = write_oe(self.dir / "2025_LoL_esports_match_data_from_OraclesElixir.csv",
                         oe_game("A1", "2025-03-01 10:00:00") + oe_game("A2", "2025-06-01 10:00:00"))
        oe.import_source(y2025, self.db, 2025)
        y2026 = write_oe(self.dir / "2026_LoL_esports_match_data_from_OraclesElixir.csv", oe_game("B1", "2026-03-01 10:00:00"))
        oe.import_source(y2026, self.db, 2026)
        self.assertEqual(self.query("SELECT count(*) FROM oe_player_games"), [(30,)])
        self.assertEqual(self.query("SELECT count(DISTINCT game_id) FROM oe_team_games"), [(3,)])
        # Same bytes again: nothing new. A newer 2026 download replaces 2026 only.
        self.assertEqual(oe.import_source(y2026, self.db, 2026)["status"], "already imported")
        (self.dir / "newer").mkdir()
        newer = write_oe(self.dir / "newer" / "2026_LoL_esports_match_data_from_OraclesElixir.csv",
                         oe_game("B1", "2026-03-01 10:00:00", kills="9") + oe_game("B2", "2026-04-01 10:00:00"))
        oe.import_source(newer, self.db, 2026)
        self.assertEqual(self.query("SELECT count(*) FROM oe_player_games"), [(40,)])
        self.assertEqual(self.query("SELECT DISTINCT kills FROM oe_player_game_stats WHERE game_id='B1'"), [(9.0,)])
        self.assertEqual(self.query("SELECT DISTINCT kills FROM oe_player_game_stats WHERE game_id='A1'"), [(3.0,)])
        self.assertEqual(self.query("SELECT count(*) FROM oe_source_files"), [(3,)])

    def test_a_key_in_two_years_keeps_the_row_dated_in_its_own_year(self):
        oe.import_source(write_oe(self.dir / "a.csv", oe_game("X1", "2025-12-31 10:00:00")), self.db, 2025)
        late = oe_game("X1", "2025-12-31 10:00:00", kills="7") + [r for g in range(30) for r in oe_game(f"Y{g}", "2026-02-01 10:00:00")]
        oe.import_source(write_oe(self.dir / "b.csv", late), self.db, 2026)
        self.assertEqual(self.query("SELECT DISTINCT kills FROM oe_player_game_stats WHERE game_id='X1'"), [(3.0,)])
        collisions = self.query("SELECT count(*), min(reason) FROM oe_key_collisions")
        self.assertEqual(collisions[0][0], 12)
        self.assertIn("own file's year", collisions[0][1])

    def test_yearly_files_are_found_in_snapshots_oldest_first(self):
        root = self.dir / "import"
        (root / "snapshots" / "20261001T000000Z").mkdir(parents=True)
        (root / "snapshots" / "20261010T000000Z").mkdir(parents=True)
        for stamp in ("20261001T000000Z", "20261010T000000Z"):
            write_oe(root / "snapshots" / stamp / "2026_LoL_esports_match_data_from_OraclesElixir.csv", oe_game("G", "2026-01-02"))
        write_oe(root / "snapshots" / "20261001T000000Z" / "2025_LoL_esports_match_data_from_OraclesElixir.csv", oe_game("H", "2025-01-02"))
        found = oe.find_yearly(root)
        self.assertEqual([(p.parent.name, oe.file_year(p)) for p in found][-1], ("20261010T000000Z", 2026))
        self.assertEqual(sorted({oe.file_year(p) for p in found}), [2025, 2026])
        self.assertIsNone(oe.file_year(Path("notes.csv")))


# ---------- Leaguepedia adapter ----------

class FakeCargo:
    """Answers COUNT(*) and paged cargoquery calls from in-memory rows; can grow a table between calls."""

    def __init__(self, games, players, grow_after=None):
        self.tables = {"ScoreboardGames=SG": games, "ScoreboardPlayers=SP": players}
        self.requests, self.grow_after = 0, grow_after

    def call(self, action, tables, fields, where, **params):
        self.requests += 1
        key = tables.split(",")[0]
        rows = self.tables[key]
        if self.grow_after and self.requests == self.grow_after:
            rows.append(dict(rows[-1], GameId="late", RoleNumber="9"))
        if fields == "COUNT(*)=n":
            return {"cargoquery": [{"title": {"n": str(len(rows))}}]}
        offset, limit = int(params.get("offset", 0)), int(params["limit"])
        return {"cargoquery": [{"title": r} for r in rows[offset:offset + limit]]}


def lp_rows(n_games):
    games, players = [], []
    for g in range(n_games):
        games.append({"GameId": f"G{g}", "DateTimeUTC": "2026-01-05 10:00:00", "Team1": "A", "Team2": "B", "WinTeam": "A",
                      "GameLengthMin": "30", "League": "L"})
        for side in ("1", "2"):
            for n in range(1, 6):
                players.append({"GameId": f"G{g}", "Side": side, "RoleNumber": str(n), "Link": f"p{side}{n}", "Kills": "1"})
    return games, players


class LeaguepediaAdapterTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def test_windows_are_paged_counted_and_normalized(self):
        games, players = lp_rows(120)  # 1,200 player rows: three pages at the API cap
        manifest = lps.collect(FakeCargo(games, players), (2026, 1), date(2026, 2, 1), root=self.root, log=lambda _: None)
        self.assertEqual(manifest["status"], "completed")
        window = manifest["windows"]["2026-01"]["tables"]
        self.assertEqual((window["players"]["rows"], len(window["players"]["pages"])), (1200, 3))
        result = lps.normalize(self.root / "db.sqlite3", root=self.root)
        self.assertEqual((result["games"], result["player_rows"]), (120, 1200))

    def test_a_moving_count_is_refetched_and_tampered_pages_are_refused(self):
        games, players = lp_rows(3)
        manifest = lps.collect(FakeCargo(games, players, grow_after=3), (2026, 1), date(2026, 2, 1), root=self.root,
                               log=lambda _: None)
        self.assertTrue(manifest["windows"]["2026-01"]["tables"]["games"]["complete"])  # second attempt was stable
        page = next(self.root.glob("*/2026-01-players-page-0001.json"))
        page.write_text(page.read_text(encoding="utf-8").replace('"1"', '"2"', 1), encoding="utf-8")
        with self.assertRaises(RuntimeError):
            lps.normalize(self.root / "db.sqlite3", root=self.root)


# ---------- identity: split evidence ----------

class DominantEvidenceTests(unittest.TestCase):
    def unit(self, supporting, page="lp:Lucid (Choi Yong-hyeok)", ambiguous=0):
        from collections import Counter
        return {"strong": set(supporting), "ambiguous": ambiguous, "supporting": Counter(supporting), "player_id": page}

    def ps(self):
        return {"real_names": {"1": xp.name_tokens("Choi Yong-hyeok"), "2": xp.name_tokens("Arnav Chaudhary"),
                               "3": xp.name_tokens("Someone Else")}}

    def test_a_stray_registration_is_outvoted_only_by_overwhelming_agreeing_evidence(self):
        self.assertEqual(xp.dominant_candidate(self.unit({"1": 305, "2": 2}), self.ps()), ("1", 305, 307))
        self.assertIsNone(xp.dominant_candidate(self.unit({"1": 30, "2": 5}), self.ps()))          # 86%: not enough
        self.assertIsNone(xp.dominant_candidate(self.unit({"1": 15, "2": 0}), self.ps()))          # too few rows
        self.assertIsNone(xp.dominant_candidate(self.unit({"1": 19, "2": 1}), self.ps()))          # too few rows
        self.assertIsNone(xp.dominant_candidate(self.unit({"3": 300, "1": 2}), self.ps()))         # real names disagree
        self.assertIsNone(xp.dominant_candidate(self.unit({"1": 300, "2": 2}, ambiguous=1), self.ps()))
        self.assertEqual(xp.dominant_candidate(self.unit({"3": 300, "1": 2}, page="lp:Handle"), self.ps())[0], "3")


# ---------- v2: banded scale, caps, age ----------

class BandedScaleTests(unittest.TestCase):
    def test_transform_is_monotone_clamped_and_hits_the_band_edges(self):
        values = [rm.banded(x / 4) for x in range(-40, 480)]
        self.assertEqual(values, sorted(values))
        self.assertEqual((rm.banded(-50), rm.banded(500)), (rm.SCALE_MIN, rm.SCALE_MAX))
        for (x, y) in rm.BAND_KNOTS:
            self.assertAlmostEqual(rm.banded(x), y)
        self.assertEqual(rm.band_of(rm.round_half_even(rm.banded(80.0))), "ELITE")      # 89.5 rounds to 90
        self.assertEqual(rm.band_of(rm.round_half_even(rm.banded(79.0))), "VERY_GOOD")
        self.assertEqual(rm.band_of(54), "AMATEUR_ACADEMY")
        self.assertEqual(rm.band_of(55), "INEXPERIENCED")
        self.assertEqual(rm.band_of(74), "AVERAGE")
        self.assertEqual(rm.band_of(83), "VERY_GOOD")
        # The top is compressed: ten raw points above the elite edge add less than four Overall points.
        self.assertLess(rm.banded(90.0) - rm.banded(80.0), 4.0)

    def test_cap_lowers_the_overall_through_the_skills(self):
        weights = WEIGHTS["mid"]
        attrs = [95, 90, 91, 88, 92]
        self.assertGreater(rm.overall_from(attrs, weights), 89)
        capped = rm.enforce_cap(attrs, weights, 89)
        self.assertLessEqual(rm.overall_from(capped, weights), 89)
        self.assertTrue(all(c <= a for c, a in zip(capped, attrs)))
        self.assertEqual(rm.enforce_cap([80] * 5, weights, 89), [80] * 5)

    def test_achievement_and_consistency_points_are_bounded(self):
        self.assertEqual(rm.achievement_points(0), 0.0)
        self.assertLess(rm.achievement_points(10_000), rm.ACHIEVEMENT_MAX_POINTS + 1e-9)
        self.assertGreater(rm.achievement_points(30), rm.achievement_points(10))
        self.assertEqual(rm.consistency_points(9), rm.CONSISTENCY_MAX_POINTS)
        self.assertEqual(rm.consistency_points(-9), -rm.CONSISTENCY_MAX_POINTS)

    def test_age_rules(self):
        self.assertEqual([rm.career_stage(a) for a in (None, 19, 25, 26, 28, 29)],
                         ["UNKNOWN", "DEVELOPING", "DEVELOPING", "PEAK", "PEAK", "VETERAN"])
        self.assertEqual([rm.aging_drop(a) for a in (28, 29, 30, 31, 35)], [None, 2.0, 2.0, 3.0, 3.0])
        growth = {20: 3.0, 21: 2.5, 22: 2.0, 23: 1.0, 24: 1.0}
        self.assertEqual(rm.projected_growth(25, growth, 2.0), (0.0, 0.0))
        self.assertEqual(rm.projected_growth(30, growth, 2.0), (0.0, 0.0))
        expected, upper = rm.projected_growth(22, growth, 2.0)
        self.assertAlmostEqual(expected, 4.0)
        self.assertAlmostEqual(upper, 4.0 + rm.POTENTIAL_QUANTILE_Z * 2.0 * 3 ** 0.5)

    def test_isotonic_fit_is_non_increasing(self):
        fitted = rm.isotonic_decreasing([5.0, 3.0, 4.0, 1.0, 2.0], [1, 1, 1, 1, 1])
        self.assertEqual(fitted, sorted(fitted, reverse=True))
        self.assertAlmostEqual(sum(fitted), 15.0)


class DevelopmentCurveTests(unittest.TestCase):
    def test_growth_is_read_relative_to_age_25(self):
        rng = np.random.default_rng(5)
        true_growth = {17: 5.0, 18: 4.5, 19: 4.0, 20: 3.0, 21: 2.5, 22: 2.0, 23: 1.0, 24: 0.5}
        season, births = {2023: {}, 2024: {}}, {}
        for i in range(3000):
            age = int(rng.integers(17, 31))
            level = float(rng.normal(55, 12))
            pid = f"ps:{i}"
            births[str(i)] = {"birthdate": date(2023 - age, 1, 1), "source": "test", "conflict": False}
            # every age shares the season drift of -3; true growth on top; regression to the mean via -0.3 * (level - 70)
            change = -3.0 + true_growth.get(age, 0.0) - 0.3 * (level - 70) + rng.normal(0, 1.0)
            season[2023][pid] = (level, 60, 1.0)
            season[2024][pid] = (level + change, 60, 1.0)
        curve = rp.development_curve(season, births)
        for age, g in true_growth.items():
            self.assertAlmostEqual(curve["growth_by_age"][age], g, delta=0.6)
        self.assertNotIn(25, curve["growth_by_age"])


class AchievementTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.db = sqlite3.connect(Path(self.temp.name) / "a.sqlite3")
        self.db.execute("CREATE TABLE lp_games (overview_page TEXT, league TEXT, split TEXT, tournament_year TEXT, is_playoffs TEXT, "
                        "is_qualifier TEXT, game_ts TEXT)")
        self.db.execute("CREATE TABLE lp_tournament_results (overview_page TEXT, team TEXT, place_number TEXT, date TEXT, showmatch TEXT)")
        pages = [("L/2025/Spring Season", "League A", "Spring", "2025", "0", "0", "2025-03-01"),
                 ("L/2025/Spring Playoffs", "League A", "Spring", "2025", "1", "0", "2025-04-10"),
                 ("L/2025/Regional Finals", "League A", None, "2025", "0", "1", "2025-08-01"),
                 ("2025 Worlds/Main Event", "World Championship", None, "2025", "0", "0", "2025-11-01")]
        self.db.executemany("INSERT INTO lp_games VALUES (?,?,?,?,?,?,?)", pages)
        results = [("L/2025/Spring Season", "Alpha", "1", "2025-03-01", None),     # regular season: partial points, no title
                   ("L/2025/Spring Playoffs", "Alpha", "1", "2025-04-10", None),   # the split's final: a title
                   ("L/2025/Regional Finals", "Alpha", "1", "2025-08-01", None),   # qualifier: no title
                   ("2025 Worlds", "Alpha", "2", "2025-11-02", None),              # parent page of the Main Event: final
                   ("2025 Worlds/Classic Showmatch", "Alpha", "1", "2025-11-02", "1")]
        self.db.executemany("INSERT INTO lp_tournament_results VALUES (?,?,?,?,?)", results)

    def tearDown(self):
        self.db.close()
        self.temp.cleanup()

    def seats(self):
        out = []
        for page, tier, intl, league in (("L/2025/Spring Season", "a", False, "League A"), ("L/2025/Spring Playoffs", "a", False, "League A"),
                                         ("L/2025/Regional Finals", "a", False, "League A"),
                                         ("2025 Worlds/Main Event", "s", True, "World Championship")):
            for game in range(10):
                for pid in ("ps:regular", "ps:starter2"):
                    out.append({"source": "leaguepedia", "tournament": page, "team_name": "Alpha", "model_id": pid, "tier": tier,
                                "international": intl, "league": league})
                if game == 0:
                    out.append({"source": "leaguepedia", "tournament": page, "team_name": "Alpha", "model_id": "ps:sub", "tier": tier,
                                "international": intl, "league": league})
        return out

    def test_titles_need_the_final_stage_and_regular_play(self):
        ach, stats = rp.achievements(self.db, self.seats(), date(2026, 10, 14))
        regular = ach["ps:regular"]
        self.assertEqual((regular["titles"], regular["tier_a_titles"], regular["international_finals"]), (1, 1, 1))
        self.assertTrue(rp.elite_evidence(regular))
        self.assertNotIn("ps:sub", ach)                      # one game of ten: below the participation share
        self.assertEqual(stats["parent_pages"], 1)
        # Points: playoffs title 5 + regular season 1st 5 * 0.3 + qualifier 1st (tier table c? no: tier a, x0.3) + Worlds 2nd 7,
        # each decayed by the time to the as-of day: strictly positive and below the undecayed sum.
        self.assertGreater(regular["score"], 7.0)
        self.assertLess(regular["score"], 5 + 1.5 + 1.5 + 7)


# ---------- formulas ----------

class FormulaTests(unittest.TestCase):
    def test_overall_is_the_games_role_formula_with_round_half_even(self):
        rng = random.Random(7)
        for role, weights in WEIGHTS.items():
            for _ in range(200):
                skills = [rng.randint(1, 99) for _ in range(5)]
                exact = sum(w * s for w, s in zip(weights, skills)) / 10_000
                expected = int(exact) + (1 if exact - int(exact) > 0.5 or (exact - int(exact) == 0.5 and int(exact) % 2) else 0)
                self.assertEqual(rm.overall_from(skills, weights), expected)
        self.assertEqual(rm.overall_from([72, 73, 72, 73, 72], (5000, 5000, 0, 0, 0)), 72)  # 72.5 -> 72 (even)

    def test_attributes_centre_on_the_level_and_missing_ones_do_not_deviate(self):
        weights = WEIGHTS["mid"]
        indices = {"Mechanics": 1.5, "EarlyGame": 0.0, "Teamfight": -0.5, "Macro": 0.8, "ChampionPool": 0.2}
        available = dict.fromkeys(rm.ATTRIBUTES, True) | {"EarlyGame": False}
        rounded, raw = rm.attributes_from(70.0, indices, weights, available)
        self.assertAlmostEqual(sum(w / 10_000 * a for w, a in zip(weights, raw)), 70.0, places=9)
        self.assertAlmostEqual(raw[1], 70.0, places=9)  # EarlyGame without evidence equals the Level
        high, _ = rm.attributes_from(97.0, {a: 3.0 if a == "Mechanics" else -1.0 for a in rm.ATTRIBUTES}, weights,
                                     dict.fromkeys(rm.ATTRIBUTES, True))
        self.assertTrue(all(1 <= a <= 99 for a in high))

    def test_one_exceptional_game_is_shrunk(self):
        signal, noise = 0.04, 1.0
        one, n1, _ = rm.eb_index([3.0], [1.0], signal, noise)
        many, n2, _ = rm.eb_index([0.6] * 80, [1.0] * 80, signal, noise)
        self.assertLess(one, 0.6)          # one +3 SD game: less than 0.6 signal SD
        self.assertGreater(many, one)      # a long, consistently good record outranks it

    def test_missing_features_contribute_the_peer_average_not_a_reweighted_share(self):
        full = {f: 1.0 for f in rm.FEATURES}
        early_missing = {f: v for f, v in full.items() if f not in rm.TIMELINE_FEATURES}
        a, b = rm.seat_scores("top", full), rm.seat_scores("top", early_missing)
        self.assertEqual(b["EarlyGame"], 0.0)
        self.assertEqual(a["Mechanics"], b["Mechanics"])
        availability = rm.attribute_availability("top", set(early_missing))
        self.assertEqual(availability["EarlyGame"], 0.0)

    def test_form_needs_recent_games_and_reads_the_players_own_history(self):
        today = date(2026, 10, 14).toordinal()
        old = [(today - 200 + i, 0.0) for i in range(30)]
        self.assertEqual(rm.form(old, today)[:2], (None, "UNKNOWN"))
        improving = old + [(today - 10 + i, 1.5) for i in range(10)]
        score, trend, confidence, recent = rm.form(improving, today)
        self.assertGreater(score, 0)
        self.assertEqual((trend, recent), ("IMPROVING", 10))

    def test_contribution_model_recovers_strength_order(self):
        rng = np.random.default_rng(3)
        truth = {f"p{i}": rng.normal(0, 0.3) for i in range(40)}
        names = list(truth)
        games = []
        for g in range(3000):
            picks = rng.choice(names, 10, replace=False)
            blue, red = list(picks[:5]), list(picks[5:])
            p = 1 / (1 + np.exp(-(sum(truth[x] for x in blue) - sum(truth[x] for x in red))))
            games.append({"blue": blue, "red": red, "blue_win": int(rng.random() < p), "weight": 1.0})
        index = {n: i for i, n in enumerate(names)}
        data = rm.LineupData(games, index)
        share = rp.sparse.csr_matrix(np.ones((40, 1)))
        tier = rp.sparse.csr_matrix(np.ones((1, 1)))
        fit = rm.fit_contributions(data, np.zeros(40), share, tier, ("a",), gamma=0.0, tau=0.5)
        corr = np.corrcoef([truth[n] for n in names], fit["beta"])[0, 1]
        self.assertGreater(corr, 0.8)



# ---------- end to end ----------

TEAM_NAMES = [f"Team {c}" for c in "ABCDEFGHIJ"]
HANDLES = ["Ace", "Bolt", "Cove", "Dusk", "Echo", "Fern", "Gale", "Haze", "Iris", "Jade"]
GHOST = len(TEAM_NAMES)


def build_world(root, db_path):
    """PandaScore side: 10 teams of 5, one 2025 stage (tier b) and one 2026 stage (tier a), a 2026 substitute on Team A,
    and two catalogue players who were never registered."""
    teams = [{"id": 100 * (i + 1), "name": name, "acronym": name[-1] * 2, "slug": name.lower().replace(" ", "-"), "players": [],
              "modified_at": "2026-01-01T00:00:00Z"} for i, name in enumerate(TEAM_NAMES)]
    players, rosters = [], {}
    roles_ps = ("top", "jun", "mid", "adc", "sup")
    for t, team in enumerate(teams):
        roster = []
        for r, role in enumerate(roles_ps):
            pid = 1000 + 10 * t + r
            players.append({"id": pid, "name": f"{HANDLES[t]}{ROLES_LP[r]}", "role": role, "first_name": "First",
                            "last_name": f"L{pid}", "nationality": "KR", "modified_at": "2026-01-01T00:00:00Z"})
            roster.append({"id": pid, "name": f"{HANDLES[t]}{ROLES_LP[r]}", "role": role})
        rosters[team["id"]] = roster
    players += [{"id": 2001, "name": "BenchTop", "role": "top", "modified_at": "2026-01-01T00:00:00Z"},
                {"id": 2002, "name": "Retired", "role": "mid", "modified_at": "2026-01-01T00:00:00Z"},
                {"id": 2003, "name": "Nobody", "role": None, "modified_at": "2026-01-01T00:00:00Z"}]

    def stage(tid, tier, begin, end, extra=None):
        expected = [{"team": {"id": t}, "players": list(r) + ((extra or {}).get(t) or [])} for t, r in rosters.items()]
        return {"id": tid, "league_id": 1, "serie_id": 10, "name": f"Stage {tid}", "tier": tier, "region": "ASIA",
                "begin_at": begin, "end_at": end, "modified_at": "2026-01-01T00:00:00Z", "teams": [{"id": t} for t in rosters],
                "matches": [], "expected_roster": expected}
    write_run(root, "20261013T000000Z", {
        "players": ("players", "/lol/players", {"sort": "id"}, [players]),
        "teams": ("teams", "/lol/teams", {"sort": "id"}, [teams]),
        "tournaments": ("tournaments", "/lol/tournaments", {"sort": "id"}, [[
            stage(501, "b", "2025-02-01T00:00:00Z", "2025-11-30T00:00:00Z"),
            stage(502, "a", "2026-01-10T00:00:00Z", "2026-09-30T00:00:00Z", {100: [{"id": 2001, "name": "BenchTop", "role": "top"}]})]])},
        "2026-10-13T00:00:00+00:00")
    norm.normalize(db_path, root)


def write_leaguepedia(root, rng, strength):
    """A Leaguepedia raw run: every pair of PandaScore teams twice a year, plus a team PandaScore does not know."""
    run = root / "20261014T000000Z"
    run.mkdir(parents=True)
    games, seats, windows = [], [], {}
    pairs = [(a, b) for a in range(GHOST) for b in range(GHOST) if a < b]
    g = 0
    for year in (2025, 2026):
        day = date(year, 3 if year == 2025 else 6, 1)
        for repeat in range(4):
            for a, b in pairs + [(0, GHOST), (3, GHOST)]:
                a, b = (a, b) if (repeat + a + b) % 2 == 0 else (b, a)   # sides alternate
                g += 1
                day += timedelta(days=1 if g % 8 == 0 else 0)
                stamp = f"{day.isoformat()} 10:00:00"
                names = TEAM_NAMES + ["Ghost Squad"]
                sa, sb = (list(strength) + [-1.5])[a], (list(strength) + [-1.5])[b]
                win_a = rng.random() < 1 / (1 + np.exp(-(sa - sb)))
                games.append({"GameId": f"g{g}", "DateTimeUTC": stamp, "Team1": names[a], "Team2": names[b],
                              "WinTeam": names[a] if win_a else names[b], "GameLengthMin": str(round(rng.uniform(25, 38), 2)),
                              "Patch": "15.1" if year == 2025 else "26.1", "League": "Synthetic League", "Region": "Testland",
                              "TournamentLevel": "Primary", "OverviewPage": f"Synthetic/{year}"})
                for side, team, won in (("1", a, win_a), ("2", b, not win_a)):
                    for r in range(5):
                        if team == GHOST:
                            handle = f"Ghost{ROLES_LP[r]}"
                        elif team == 0 and r == 0 and year == 2026 and repeat == 1 and {a, b} & {7, 8, 9}:
                            handle = "BenchTop"   # three substitute games
                        else:
                            handle = f"{HANDLES[team]}{ROLES_LP[r]}"
                        skill = strength[team] if team < GHOST else -0.5
                        kills = max(0, int(rng.normal(3 + 2 * won + skill, 1.5)))
                        seats.append({"GameId": f"g{g}", "Side": side, "RoleNumber": str(r + 1), "Link": handle,
                                      "CanonicalLink": handle, "Name": handle, "Team": names[team], "TeamVs": names[b if team == a else a],
                                      "Role": ROLES_LP[r], "Champion": f"Champ{r}{rng.randint(0, 6)}", "Kills": str(kills),
                                      "Deaths": str(max(0, int(rng.normal(3 - 2 * won, 1.2)))),
                                      "Assists": str(max(0, int(rng.normal(6 + 3 * won, 2)))),
                                      "Gold": str(int(rng.normal(11000 + 1500 * won + 300 * skill, 900))),
                                      "CS": str(int(rng.normal(220 if r not in (1, 4) else 140, 25))),
                                      "DamageToChampions": str(int(rng.normal(14000 + 3000 * won + 800 * skill, 2500))),
                                      "VisionScore": str(int(rng.normal(30 if r != 4 else 80, 8))), "PlayerWin": "Yes" if won else "No"})
    for table, rows in (("games", games), ("players", seats)):
        pages = []
        for n, start in enumerate(range(0, len(rows), 500), start=1):
            name = f"all-{table}-page-{n:04d}.json"
            pages.append({"page": n, "rows": len(rows[start:start + 500]), "file": name,
                          "sha256": lps.write_json(run / name, rows[start:start + 500])})
        windows.setdefault("all", {"tables": {}})["tables"][table] = {"count_before": len(rows), "count_after": len(rows),
                                                                       "rows": len(rows), "pages": pages, "complete": True}
    lps.write_json(run / "manifest.json", {"run_id": run.name, "status": "completed", "windows": windows})
    return len(games)


class EndToEndTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        root = Path(cls.temp.name)
        cls.db = root / "db.sqlite3"
        build_world(root / "ps", cls.db)
        rng = np.random.default_rng(11)
        cls.strength = list(np.linspace(1.0, -1.0, GHOST))
        write_leaguepedia(root / "lp", random_wrapper(rng), cls.strength)
        lps.normalize(cls.db, root=root / "lp")
        db = sqlite3.connect(cls.db)
        try:
            cls.reconciled = xp.reconcile_leaguepedia(db)
            cls.built = bp.build(db)
        finally:
            db.close()
        saved = rp.ASSETS
        rp.ASSETS = root / "no-assets"
        try:
            cls.result = rp.run(cls.db, date(2026, 10, 14), root / "out")
        finally:
            rp.ASSETS = saved
        cls.out = root / "out"

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def test_every_catalogue_player_has_exactly_one_record(self):
        records = self.result["records"]
        ids = [r["player_id"] for r in records]
        self.assertEqual(len(ids), 53)
        self.assertEqual(len(set(ids)), 53)
        with (self.out / f"{rp.OUTPUT_STEM}.csv").open(encoding="utf-8-sig") as f:
            self.assertEqual(sum(1 for _ in csv.DictReader(f)), 53)

    def test_evidence_classes_follow_verified_games(self):
        by_id = {r["player_id"]: r for r in self.result["records"]}
        self.assertEqual(by_id["1000"]["rating_evidence_class"], "MEASURED_ESTIMATE")
        self.assertEqual(by_id["2001"]["rating_evidence_class"], "LIMITED_EVIDENCE")
        self.assertEqual(by_id["2001"]["games_2026"], 3)
        for pid in ("2002", "2003"):
            r = by_id[pid]
            self.assertEqual((r["rating_evidence_class"], r["games_2025"], r["games_2026"], r["confidence"]), ("PRIOR_ONLY", 0, 0, 0.0))
            self.assertEqual(len({r["mechanics"], r["early_game"], r["teamfight"], r["macro"], r["champion_pool"], r["overall_rating"]}), 1)
            self.assertIn("not established", r["provisional_note"])
        for r in self.result["records"]:
            if r["rating_evidence_class"] != "PRIOR_ONLY":
                self.assertGreater(r["games_2025"] + r["games_2026"], 0)

    def test_overall_reproduces_the_role_formula_and_bounds(self):
        role_key = {v: k for k, v in rp.ROLE_LABEL.items()}
        for r in self.result["records"]:
            attrs = [r["mechanics"], r["early_game"], r["teamfight"], r["macro"], r["champion_pool"]]
            self.assertTrue(all(1 <= a <= 99 for a in attrs))
            if r["primary_role"]:
                self.assertEqual(rm.overall_from(attrs, WEIGHTS[role_key[r["primary_role"]]]), r["overall_rating"])

    def test_stronger_teams_rate_higher_and_the_substitute_stays_near_the_prior(self):
        by_id = {r["player_id"]: r for r in self.result["records"]}
        best = np.mean([by_id[str(1000 + r)]["_level"] for r in range(5)])
        worst = np.mean([by_id[str(1090 + r)]["_level"] for r in range(5)])
        self.assertGreater(best, worst + 2)
        sub = by_id["2001"]["_level"]
        league = np.mean([r["_level"] for r in self.result["records"] if r["rating_evidence_class"] == "MEASURED_ESTIMATE"])
        self.assertLess(abs(sub - league), abs(best - league) + 1)

    def test_rankings_hold_unique_eligible_players_and_report_the_deficit(self):
        for label, rows in self.result["lists"].items():
            ids = [r["player_id"] for r in rows]
            self.assertEqual(len(ids), len(set(ids)), label)
            for r in rows:
                self.assertGreaterEqual(r["games_2026_in_role"], rp.RANK_MIN_GAMES_2026)
                self.assertNotEqual(r["evidence_class"], "PRIOR_ONLY")
        self.assertEqual(self.result["deficits"]["Top"], {"eligible": 10, "published": 10, "deficit": 90})
        self.assertNotIn("2001", [r["player_id"] for r in self.result["lists"]["Top"]])

    def test_v2_caps_without_evidence(self):
        # No tournament results in this world: nobody has elite evidence, so nobody reaches 90; placeholders stop at 73.
        for r in self.result["records"]:
            self.assertLessEqual(r["overall_rating"], rm.CAP_WITHOUT_ELITE_EVIDENCE)
            self.assertIn(r["band"], {b[0] for b in rm.BANDS})
            if r["rating_evidence_class"] == "PRIOR_ONLY":
                self.assertLessEqual(r["overall_rating"], rm.CAP_PRIOR_ONLY)
                self.assertIsNone(r["potential"])
            self.assertIsNone(r["age"])  # no birthdays in this world
            self.assertEqual(r["career_stage"], "UNKNOWN")

    def test_unknown_team_players_are_reported_separately(self):
        unresolved = {r["source_identity"] for r in self.result["unresolved"]}
        self.assertIn("lp:GhostTop", unresolved)
        self.assertFalse(any(r["player_id"] is None for r in self.result["records"]))

    def test_rerun_is_reproducible(self):
        first = dict(self.result["manifest"]["outputs"])
        saved = rp.ASSETS
        rp.ASSETS = self.out.parent / "no-assets"
        try:
            again = rp.run(self.db, date(2026, 10, 14), self.out)
        finally:
            rp.ASSETS = saved
        self.assertEqual(first, again["manifest"]["outputs"])


class random_wrapper:
    """numpy Generator with the random.Random methods the generator above uses."""

    def __init__(self, rng):
        self.rng = rng

    def random(self):
        return self.rng.random()

    def normal(self, mean, sd):
        return self.rng.normal(mean, sd)

    def uniform(self, lo, hi):
        return self.rng.uniform(lo, hi)

    def randint(self, lo, hi):
        return int(self.rng.integers(lo, hi + 1))


if __name__ == "__main__":
    unittest.main()
