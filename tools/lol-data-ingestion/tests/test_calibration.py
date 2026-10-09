import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from sync_pandascore import crawl_query, open_database
from build_rosters import roster_rows
from calibrate import build_report, elo, playable


class FakeResponse:
    def __init__(self, rows, total, remaining=900):
        self.status_code = 200
        self._rows = rows
        self.headers = {"X-Total": str(total), "X-Rate-Limit-Remaining": str(remaining)}

    def json(self):
        return self._rows

    def raise_for_status(self):
        pass


class FakeSession:
    """Serves `total` records in pages; `short_at` truncates a page to simulate records vanishing mid-crawl."""
    def __init__(self, total, short_at=None, remaining=900):
        self.total, self.short_at, self.remaining, self.calls = total, short_at, remaining, []

    def get(self, url, params, timeout):
        self.calls.append(dict(params))
        page, per_page = params["page"], params["per_page"]
        start = (page - 1) * per_page
        rows = [{"id": i + 1} for i in range(start, min(start + per_page, self.total))]
        if self.short_at == page:
            rows = rows[:-1]
        return FakeResponse(rows, self.total, self.remaining)


def crawl(session, max_pages=10):
    d = tempfile.TemporaryDirectory()
    db = open_database(Path(d.name) / "db.sqlite3")
    entry = crawl_query(session, db, Path(d.name), "q", "matches", {"sort": "begin_at,id"}, 100, max_pages, 0, sleep=lambda s: None)
    count = db.execute("SELECT count(*) FROM provider_records").fetchone()[0]
    db.close()
    d.cleanup()
    return entry, count


class CrawlTests(unittest.TestCase):
    def test_pages_to_x_total_with_filters_and_sort(self):
        session = FakeSession(250)
        entry, stored = crawl(session)
        self.assertEqual((entry["rows"], entry["x_total"], entry["complete"], len(entry["pages"]), stored), (250, 250, True, 3, 250))
        self.assertTrue(all(c["sort"] == "begin_at,id" for c in session.calls))

    def test_exact_multiple_stops_without_an_empty_page(self):
        session = FakeSession(200)
        entry, _ = crawl(session)
        self.assertEqual((len(session.calls), entry["complete"]), (2, True))

    def test_shortfall_against_x_total_is_reported(self):
        entry, _ = crawl(FakeSession(250, short_at=2))
        self.assertFalse(entry["complete"])
        self.assertEqual(entry["rows"], 249)

    def test_page_cap_marks_truncation(self):
        entry, _ = crawl(FakeSession(450), max_pages=2)
        self.assertFalse(entry["complete"])
        self.assertTrue(entry["truncated_by_page_limit"])

    def test_quota_floor_stops_the_crawl(self):
        entry, _ = crawl(FakeSession(450, remaining=10))
        self.assertEqual((len(entry["pages"]), entry["stopped_for_quota"]), (1, 10))


def team(i):
    return {"id": i, "name": f"SecretTeam{i}", "acronym": f"ST{i}"}


def player(i, role, nationality="KR", birthday="2003-05-01"):
    return {"id": 1000 + i, "name": f"SecretHandle{i}", "first_name": f"SecretFirst{i}", "last_name": f"SecretLast{i}",
            "role": role, "nationality": nationality, "birthday": birthday, "slug": f"secret-slug-{i}"}


def tournament(tid, serie, begin, teams_players, tier="s", name="Regular Season"):
    return {"id": tid, "serie_id": serie, "league_id": 1, "name": name, "tier": tier, "region": "ASIA", "begin_at": begin,
            "end_at": begin, "expected_roster": [{"team": team(t), "players": ps} for t, ps in teams_players]}


def match(mid, a, b, winner, day, n=3, score=(2, 1), tier="s", name="Regular Season", forfeit=False):
    loser = b if winner == a else a
    return {"id": mid, "status": "finished", "forfeit": forfeit, "draw": False, "begin_at": f"2026-{day}T10:00:00Z", "league_id": 1,
            "serie_id": 1, "number_of_games": n, "winner_id": winner, "tournament": {"tier": tier, "name": name},
            "opponents": [{"opponent": team(a)}, {"opponent": team(b)}],
            "results": [{"team_id": winner, "score": score[0]}, {"team_id": loser, "score": score[1]}],
            "games": [{"finished": True, "length": 1800 + 60 * mid}]}


ROLES = ["top", "jun", "mid", "adc", "sup"]
FIRST = tournament(1, 10, "2026-01-10T00:00:00Z", [(1, [player(i, ROLES[i % 5]) for i in range(5)]),
                                                     (2, [player(10 + i, ROLES[i % 5], "CN", None) for i in range(6)])])
SECOND = tournament(2, 11, "2026-06-01T00:00:00Z", [(1, [player(i, ROLES[i % 5]) for i in range(4)] + [player(20, "sup")])])
MATCHES = [match(i, 1 + i % 2, 2 - i % 2, 1, f"0{1 + i // 10}-{10 + i % 10}") for i in range(12)]


class CalibrationTests(unittest.TestCase):
    def test_roster_rows_are_dated_per_stage(self):
        rows = roster_rows([FIRST, SECOND])
        self.assertEqual(len(rows), 16)
        self.assertEqual({r["begin_at"] for r in rows if r["player_id"] == 1000}, {"2026-01-10T00:00:00Z", "2026-06-01T00:00:00Z"})

    def test_elo_is_zero_sum_and_ignores_forfeits(self):
        ratings, counts, _, predictions, used = elo(MATCHES + [match(99, 1, 2, 2, "03-01", forfeit=True)])
        self.assertAlmostEqual(sum(ratings.values()), 1500 * len(ratings), places=6)
        self.assertEqual((len(used), counts[1]), (12, 12))
        self.assertFalse(playable(match(99, 1, 2, 2, "03-01", forfeit=True)))
        self.assertAlmostEqual(predictions[0][0], .5)

    def test_report_is_deterministic_and_has_no_identities(self):
        first = json.dumps(build_report([FIRST, SECOND], MATCHES, []), sort_keys=True)
        second = json.dumps(build_report([SECOND, FIRST], list(reversed(MATCHES)), []), sort_keys=True)
        self.assertEqual(first, second)
        for secret in ["SecretHandle", "SecretFirst", "SecretLast", "secret-slug", "SecretTeam", "ST1", "2003-05-01", "1000"]:
            self.assertNotIn(secret, first)

    def test_report_measures_turnover_sizes_and_ages(self):
        report = build_report([FIRST, SECOND], MATCHES, [])
        self.assertEqual(report["rosters"]["turnover_between_splits_by_tier"]["s"]["p50"], .2)
        self.assertEqual(report["rosters"]["roster_size_share_by_tier"]["s"], {"5": .6667, "6": .3333})
        self.assertEqual(report["demographics"]["age"]["coverage"], round(6 / 12, 4))
        self.assertEqual(report["formats"]["series_score_share"]["bo3"], {"2-1": 1.0})


if __name__ == "__main__":
    unittest.main()
