"""Offline tests for the full-2026 collection: resumable preset crawl, normalization, and the gated logo cache."""
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest
from unittest import mock

import requests

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import cache_logos
import normalize_pandascore as norm
import sync_pandascore as sync

TOKEN = "offline-test-token-0123456789"


class FakeResponse:
    def __init__(self, rows, total, remaining):
        self.status_code = 200
        self._rows = rows
        self.headers = {"X-Total": str(total), "X-Rate-Limit-Remaining": str(remaining)}

    def json(self):
        return self._rows

    def raise_for_status(self):
        pass


class FakeApi:
    """Serves every list path from a fixed total (0 when unlisted); `drift` maps (path, page) to a new total."""

    def __init__(self, totals, remaining=900, drift=None, duplicate_at=None):
        self.totals, self.remaining, self.drift, self.duplicate_at = dict(totals), remaining, drift or {}, duplicate_at
        self.calls, self.headers = [], {}

    def get(self, url, params=None, timeout=None):
        path = url[len(sync.BASE):]
        page, per_page = params["page"], params["per_page"]
        self.calls.append((path, json.dumps({k: v for k, v in params.items() if k not in ("page", "per_page")}, sort_keys=True), page))
        if (path, page) in self.drift:
            self.totals[path] = self.drift[(path, page)]
        total = self.totals.get(path, 0)
        start = (page - 1) * per_page
        rows = [{"id": i + 1, "modified_at": "2026-01-01T00:00:00Z"} for i in range(start, min(start + per_page, total))]
        if self.duplicate_at == page and rows:
            rows[-1] = {"id": start, "modified_at": "2026-01-01T00:00:00Z"}
        return FakeResponse(rows, total, self.remaining)

    def close(self):
        pass


TOTALS = {"/lol/leagues": 3, "/lol/series": 2, "/lol/tournaments": 4, "/lol/matches": 25, "/lol/teams": 12, "/lol/players": 23}


def preset_calls(per_page):
    """Requests a full-2026 crawl needs with TOTALS: every query at least one page."""
    pages = 0
    for _, collection, _, *path in sync.PRESETS["full-2026"]:
        total = TOTALS.get(path[0] if path else sync.COLLECTIONS[collection], 0)
        pages += max(1, -(-total // per_page))
    return pages


class PresetCrawlTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.run_dir = self.root / "raw" / "R1"
        self.run_dir.mkdir(parents=True)
        self.db = sync.open_database(self.root / "db.sqlite3")

    def tearDown(self):
        self.db.close()
        self.temp.cleanup()

    def crawl(self, api, manifest, budget=None, per_page=10):
        session = sync.CountingSession(api, budget)
        try:
            sync.run_preset(session, self.db, self.run_dir, manifest, "full-2026", per_page, 50, 0, sleep=lambda s: None)
            return None
        except sync.StopCollection as stop:
            return str(stop)

    def test_query_paths_and_filters_are_sent_as_declared(self):
        api = FakeApi(TOTALS)
        self.assertIsNone(self.crawl(api, {}))
        sent = {(path, params) for path, params, _ in api.calls}
        for _, collection, params, *path in sync.PRESETS["full-2026"]:
            self.assertIn((path[0] if path else sync.COLLECTIONS[collection], json.dumps(params, sort_keys=True)), sent)
        self.assertIn(("/lol/matches", json.dumps({"range[scheduled_at]": sync.YEAR_2026, "sort": "id"}, sort_keys=True)), sent)

    def test_budget_stop_then_resume_fetches_every_page_exactly_once(self):
        api = FakeApi(TOTALS)
        manifest = {}
        stops = 0
        while (reason := self.crawl(api, manifest, budget=4)) is not None:
            self.assertIn("request budget", reason)
            on_disk = json.loads((self.run_dir / "manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(on_disk["requests"], manifest["requests"])
            stops += 1
        self.assertGreater(stops, 3)
        self.assertEqual(len(api.calls), len(set(api.calls)))
        self.assertEqual(len(api.calls), preset_calls(10))
        self.assertEqual(manifest["requests"], len(api.calls))
        self.assertTrue(all(q["ended"] and q["complete"] for q in manifest["queries"].values()))
        self.assertEqual(manifest["queries"]["players"]["unique_ids"], 23)
        self.assertEqual((manifest["queries"]["series-running"]["x_total"], manifest["queries"]["series-running"]["complete"]), (0, True))

    def test_quota_floor_is_a_resumable_stop(self):
        api = FakeApi(TOTALS, remaining=10)
        manifest = {}
        reason = self.crawl(api, manifest)
        self.assertIn("quota floor", reason)
        stopped = [n for n, q in manifest["queries"].items() if not q.get("ended")]
        self.assertEqual(len(stopped), 1)
        api.remaining = 900
        self.assertIsNone(self.crawl(api, manifest))
        self.assertEqual(len(api.calls), len(set(api.calls)))

    def test_total_drift_pages_to_latest_total_but_is_not_complete(self):
        api = FakeApi({"/lol/teams": 25}, drift={("/lol/teams", 2): 31})
        entry = sync.crawl_query(api, self.db, self.run_dir, "teams", "teams", {"sort": "id"}, 10, 10, 0, sleep=lambda s: None)
        self.assertEqual((len(entry["pages"]), entry["x_total"], entry["unique_ids"]), (4, 31, 31))
        self.assertTrue(entry["x_total_changed"])
        self.assertFalse(entry["complete"])

    def test_duplicate_ids_are_counted_and_block_completeness(self):
        api = FakeApi({"/lol/teams": 25}, duplicate_at=2)
        entry = sync.crawl_query(api, self.db, self.run_dir, "teams", "teams", {"sort": "id"}, 10, 10, 0, sleep=lambda s: None)
        self.assertEqual((entry["rows"], entry["unique_ids"], entry["duplicate_ids"], entry["complete"]), (25, 24, 1, False))

    def test_cli_partial_then_resume_never_writes_the_token(self):
        api = FakeApi(TOTALS)
        argv = ["sync_pandascore.py", "--preset", "full-2026", "--max-pages", "50", "--per-page", "100",
                "--db", str(self.root / "cli.sqlite3"), "--snapshot-root", str(self.root / "cli")]
        with mock.patch.dict(os.environ, {"PANDASCORE_TOKEN": TOKEN}), mock.patch.object(sync, "load_dotenv"), \
                mock.patch.object(sync.requests, "Session", return_value=api), mock.patch.object(sync.time, "sleep"), \
                mock.patch("sys.stdout"), mock.patch("sys.stderr"):
            with mock.patch.object(sys, "argv", argv + ["--max-requests", "5"]), self.assertRaises(SystemExit) as stop:
                sync.main()
            self.assertEqual(stop.exception.code, 3)
            run_id = next((self.root / "cli").iterdir()).name
            with mock.patch.object(sys, "argv", argv + ["--resume", run_id]):
                sync.main()
        manifest_text = (self.root / "cli" / run_id / "manifest.json").read_text(encoding="utf-8")
        manifest = json.loads(manifest_text)
        self.assertEqual((manifest["status"], manifest["requests"], len(manifest["sessions"])), ("completed", len(api.calls), 2))
        self.assertEqual(len(api.calls), len(set(api.calls)))
        self.assertNotIn(TOKEN, manifest_text)
        self.assertNotIn("stop_reason", manifest)


# ---------- normalization ----------

def tournament(tid, begin, end, roster, teams=None, modified="2026-01-01T00:00:00Z"):
    return {"id": tid, "league_id": 1, "serie_id": 10, "name": "Stage", "tier": "a", "region": "ASIA", "begin_at": begin,
            "end_at": end, "modified_at": modified, "teams": [{"id": t} for t in (teams if teams is not None else roster)],
            "expected_roster": [{"team": {"id": t}, "players": [{"id": p, "role": "mid"} for p in players]}
                                for t, players in roster.items()], "matches": []}


def write_run(root, run_id, queries, started_at):
    """A completed run directory: queries = {name: (collection, path, params, [rows per page])}."""
    run = root / run_id
    run.mkdir(parents=True)
    manifest = {"run_id": run_id, "started_at": started_at, "finished_at": started_at, "status": "completed",
                "preset": "full-2026", "queries": {}}
    for name, (collection, path, params, pages) in queries.items():
        entry = {"collection": collection, "path": path, "params": params, "pages": [], "rows": 0,
                 "x_total": sum(map(len, pages)), "ended": True, "complete": True}
        for number, rows in enumerate(pages, 1):
            body = json.dumps(rows).encode("utf-8")
            file = f"{name}-page-{number:04d}.json"
            (run / file).write_bytes(body)
            entry["pages"].append({"page": number, "count": len(rows), "sha256": hashlib.sha256(body).hexdigest(),
                                   "file": file, "fetched_at": started_at})
            entry["rows"] += len(rows)
        entry["unique_ids"] = entry["rows"]
        manifest["queries"][name] = entry
    (run / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
    return run


def table_digest(db_path):
    db = sqlite3.connect(db_path)
    try:
        digest = hashlib.sha256()
        for (table,) in db.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").fetchall():
            digest.update(table.encode())
            for row in sorted(db.execute(f"SELECT * FROM {table}").fetchall(), key=repr):
                digest.update(repr(row).encode())
        return digest.hexdigest()
    finally:
        db.close()


class NormalizeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name) / "raw"
        self.db = Path(self.temp.name) / "db.sqlite3"
        players = [{"id": 1, "name": "A", "slug": "a", "first_name": None, "last_name": "L", "nationality": "KR", "role": "mid",
                    "active": True, "current_team": {"id": 7}, "image_url": None, "modified_at": "2026-01-01T00:00:00Z"},
                   {"id": 2, "name": "B", "role": "top", "active": True, "current_team": None, "modified_at": "2026-01-01T00:00:00Z"}]
        teams = [{"id": 7, "name": "T", "players": [{"id": 1}], "image_url": "https://cdn-api.pandascore.co/images/team/image/7/t.png",
                  "dark_mode_image_url": None, "modified_at": "2026-01-01T00:00:00Z"},
                 {"id": 8, "name": "U", "players": [], "image_url": None, "modified_at": "2026-01-01T00:00:00Z"}]
        # Run 1 sees stage 100 (2026, ended) with players 1 and 3, stage 101 (still running) and a 2024 stage.
        write_run(self.root, "20261001T000000Z", {
            "players": ("players", "/lol/players", {"sort": "id"}, [players]),
            "teams": ("teams", "/lol/teams", {"sort": "id"}, [teams]),
            "tournaments-begin-2026": ("tournaments", "/lol/tournaments", {"range[begin_at]": "2026"}, [[
                tournament(100, "2026-02-01T00:00:00Z", "2026-03-01T00:00:00Z", {7: [1, 3]}),
                tournament(101, "2026-09-01T00:00:00Z", "2026-11-01T00:00:00Z", {7: [1]}, teams=[7, 9])]]),
            "old": ("tournaments", "/lol/tournaments", {}, [[tournament(50, "2024-02-01T00:00:00Z", "2024-03-01T00:00:00Z", {8: [2]})]]),
            "matches": ("matches", "/lol/matches", {}, [[{"id": 900, "status": "canceled", "scheduled_at": "2026-05-01T00:00:00Z",
                                                           "begin_at": None, "opponents": [], "results": [], "games": []}]]),
        }, "2026-10-01T00:00:00+00:00")

    def tearDown(self):
        self.temp.cleanup()

    def second_run(self):
        # Run 2: stage 100 corrected (player 3 removed, newer modified_at); nothing else.
        write_run(self.root, "20261005T000000Z", {"tournaments-begin-2026": ("tournaments", "/lol/tournaments", {"range[begin_at]": "2026"}, [[
            tournament(100, "2026-02-01T00:00:00Z", "2026-03-01T00:00:00Z", {7: [1]}, modified="2026-10-04T00:00:00Z")]])},
            "2026-10-05T00:00:00+00:00")

    def query(self, sql, *args):
        db = sqlite3.connect(self.db)
        try:
            return db.execute(sql, args).fetchall()
        finally:
            db.close()

    def test_rebuild_is_idempotent(self):
        norm.normalize(self.db, self.root)
        first = table_digest(self.db)
        norm.normalize(self.db, self.root)
        self.assertEqual(first, table_digest(self.db))

    def test_current_snapshot_follows_latest_copy_and_observations_keep_history(self):
        norm.normalize(self.db, self.root)
        self.second_run()
        norm.normalize(self.db, self.root)
        self.assertEqual(self.query("SELECT player_id FROM ps_registrations WHERE tournament_id='100'"), [("1",)])
        self.assertEqual(self.query("SELECT source_run FROM ps_tournaments WHERE tournament_id='100'"), [("20261005T000000Z",)])
        self.assertEqual(self.query("""SELECT run_id, player_id FROM tournament_registrations WHERE tournament_id='100'
                                       ORDER BY 1, 2"""),
                         [("20261001T000000Z", "1"), ("20261001T000000Z", "3"), ("20261005T000000Z", "1")])
        self.assertEqual(self.query("SELECT count(*) FROM record_observations WHERE external_id='100'"), [(2,)])
        self.assertEqual(self.query("SELECT count(*) FROM collection_runs"), [(2,)])

    def test_player_index_only_states_what_evidence_supports(self):
        norm.normalize(self.db, self.root)
        rows = {r[0]: r for r in self.query("SELECT * FROM ps_player_index_2026")}
        self.assertEqual(sorted(rows), ["1", "3"])  # player 2 is only on the 2024 stage
        one = rows["1"]
        self.assertEqual(one[2:5], ("verified", 2, "7"))
        self.assertEqual(one[7], "unknown")  # match participation: no lineup source
        self.assertEqual(one[8:10], ("2026-10-01", "7"))  # listed on stage 101, not ended at observation
        self.assertEqual(one[10:12], ("unknown", "unknown"))
        self.assertIsNone(rows["3"][8])  # stage 100 had ended: no current-roster claim
        self.assertEqual(rows["3"][1], 0)  # registered but not in the player catalogue: a broken reference to report

    def test_profiles_images_and_stage_teams(self):
        norm.normalize(self.db, self.root)
        self.assertEqual(self.query("SELECT missing_fields FROM ps_players ORDER BY player_id"),
                         [("first_name,image_url",), ("slug,first_name,last_name,nationality,current_team,image_url",)])
        images = {(r[0], r[1], r[2]): r[3:7] for r in self.query("SELECT * FROM ps_image_refs")}
        self.assertEqual(images[("team", "7", "image_url")],
                         ("value", "https://cdn-api.pandascore.co/images/team/image/7/t.png", "cdn-api.pandascore.co", "LICENSE_REVIEW_REQUIRED"))
        self.assertEqual(images[("team", "7", "dark_mode_image_url")][0], "null")
        self.assertEqual(images[("team", "8", "dark_mode_image_url")][0], "absent")
        self.assertEqual(images[("player", "2", "image_url")][0], "absent")
        self.assertEqual(self.query("SELECT team_id, in_teams_list, roster_status FROM ps_tournament_teams WHERE tournament_id='101' ORDER BY 1"),
                         [("7", 1, "players_listed"), ("9", 1, "no_roster_entry")])
        self.assertEqual(self.query("SELECT relevance_2026, opponent1_id FROM ps_matches"), [("scheduled_in_2026", None)])
        self.assertEqual(self.query("SELECT in_latest_listing FROM ps_players"), [(1,), (1,)])
        self.assertEqual(self.query("SELECT source FROM team_membership_observations ORDER BY 1"), [("player.current_team",), ("team.players",)])

    def test_edited_raw_page_is_refused(self):
        page = self.root / "20261001T000000Z" / "players-page-0001.json"
        page.write_bytes(page.read_bytes() + b" ")
        with self.assertRaises(ValueError):
            norm.normalize(self.db, self.root)
        self.assertEqual(self.query("SELECT name FROM sqlite_master WHERE name='ps_players'"), [])

    def test_stage_relevance(self):
        r = norm.stage_relevance
        self.assertEqual([r("2026-01-02", None), r("2025-12-20", "2026-01-03"), r("2025-01-01", "2027-01-01"),
                          r("2025-01-01", "2025-02-01"), r(None, None), r("2025-12-01", None)],
                         ["begins_in_2026", "ends_in_2026", "spans_2026", "outside_2026", "unknown_dates", "unknown_end"])


# ---------- logo cache ----------

PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 32


class ImageResponse:
    def __init__(self, status, body=b"", headers=None):
        self.status_code, self.body, self.headers = status, body, headers or {}
        self.closed = False

    def iter_content(self, size):
        for i in range(0, len(self.body), size):
            yield self.body[i:i + size]

    def close(self):
        self.closed = True


class ImageSession:
    def __init__(self, responses):
        self.responses, self.calls, self.headers = dict(responses), [], {}

    def get(self, url, **kwargs):
        self.calls.append(url)
        self.assert_no_auto_redirect = kwargs.get("allow_redirects")
        answer = self.responses[url]
        return answer.pop(0) if isinstance(answer, list) else answer


TEAM_URL = "https://cdn-api.pandascore.co/images/team/image/7/t.png"


class LogoCacheTests(unittest.TestCase):
    def fetch(self, responses, url=TEAM_URL):
        sleeps = []
        session = ImageSession(responses)
        return cache_logos.fetch_image(session, url, sleeps.append), session, sleeps

    def test_accepts_a_png_from_the_provider_cdn(self):
        (body, content_type, extension), session, _ = self.fetch({TEAM_URL: ImageResponse(200, PNG, {"Content-Type": "image/png"})})
        self.assertEqual((body, extension), (PNG, "png"))
        self.assertIs(session.assert_no_auto_redirect, False)

    def test_rejects_untrusted_hosts_schemes_and_redirects(self):
        for url in ("https://evil.example/x.png", "http://cdn-api.pandascore.co/x.png", "https://cdn-api.pandascore.co:8443/x.png"):
            with self.subTest(url=url), self.assertRaises(cache_logos.ImageError):
                self.fetch({}, url)
        with self.assertRaises(cache_logos.ImageError):
            self.fetch({TEAM_URL: ImageResponse(302, headers={"Location": "https://evil.example/x.png"})})
        moved = "https://cdn.pandascore.co/images/team/image/7/t.png"
        (body, _, _), session, _ = self.fetch({TEAM_URL: ImageResponse(301, headers={"Location": moved}),
                                               moved: ImageResponse(200, PNG, {"Content-Type": "image/png"})})
        self.assertEqual(session.calls, [TEAM_URL, moved])

    def test_rejects_non_images_mismatches_and_oversize(self):
        cases = [ImageResponse(200, b"<html>no</html>", {"Content-Type": "text/html"}),
                 ImageResponse(200, b"<svg xmlns='http://www.w3.org/2000/svg'/>", {"Content-Type": "image/svg+xml"}),
                 ImageResponse(200, PNG, {"Content-Type": "image/jpeg"}),
                 ImageResponse(200, PNG, {"Content-Length": str(cache_logos.MAX_BYTES + 1)}),
                 ImageResponse(404)]
        for response in cases:
            with self.subTest(status=response.status_code, headers=response.headers), self.assertRaises(cache_logos.ImageError):
                self.fetch({TEAM_URL: response})

    def test_transient_failures_retry_with_bounded_backoff(self):
        (body, _, _), _, sleeps = self.fetch({TEAM_URL: [ImageResponse(503), ImageResponse(429, headers={"Retry-After": "120"}),
                                                          ImageResponse(200, PNG, {"Content-Type": "application/octet-stream"})]})
        self.assertEqual((body, sleeps), (PNG, [2, 60]))
        with self.assertRaises(cache_logos.ImageError):
            self.fetch({TEAM_URL: [ImageResponse(500)] * 4})

    def test_variants_only_for_documented_team_paths(self):
        self.assertEqual(cache_logos.variant_url(TEAM_URL, "team", "image_url", "thumb"),
                         "https://cdn-api.pandascore.co/images/team/image/7/thumb_t.png")
        self.assertIsNone(cache_logos.variant_url("https://cdn-api.pandascore.co/images/league/image/3/l.png", "league", "image_url", "thumb"))
        self.assertIsNone(cache_logos.variant_url(TEAM_URL, "team", "dark_mode_image_url", "normal"))

    def test_run_caches_by_id_then_skips_known_files(self):
        with tempfile.TemporaryDirectory() as d:
            db_path, assets = Path(d) / "db.sqlite3", Path(d) / "assets"
            db = sqlite3.connect(db_path)
            db.execute(f"CREATE TABLE ps_image_refs ({norm.CURRENT_SCHEMA['ps_image_refs']})")
            db.executemany("INSERT INTO ps_image_refs VALUES (?,?,?,?,?,?,?,?,?)",
                           [("team", "7", "image_url", "value", TEAM_URL, "cdn-api.pandascore.co", "LICENSE_REVIEW_REQUIRED", "R", None),
                            ("team", "8", "image_url", "null", None, None, None, "R", None)])
            db.commit()
            db.close()
            session = ImageSession({TEAM_URL: [ImageResponse(200, PNG, {"Content-Type": "image/png"}) for _ in range(2)]})
            summary = cache_logos.run(db_path, assets, "test approval", session=session, sleep=lambda s: None)
            self.assertEqual((summary["candidates"], summary["cached"]), (1, 1))
            self.assertEqual((assets / "team-logos" / "7.png").read_bytes(), PNG)
            again = cache_logos.run(db_path, assets, "test approval", session=session, sleep=lambda s: None)
            self.assertEqual((again["requested"], again["skipped_known"]), (0, 1))
            refreshed = cache_logos.run(db_path, assets, "test approval", refresh=True, session=session, sleep=lambda s: None)
            self.assertEqual(refreshed["unchanged"], 1)
            manifest = json.loads((assets / "logo_manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(manifest["entries"][0]["sha256"], hashlib.sha256(PNG).hexdigest())

    def test_without_licence_approval_no_connection_is_opened(self):
        with tempfile.TemporaryDirectory() as d:
            db_path = Path(d) / "db.sqlite3"
            db = sqlite3.connect(db_path)
            db.execute(f"CREATE TABLE ps_image_refs ({norm.CURRENT_SCHEMA['ps_image_refs']})")
            db.close()
            with mock.patch.object(cache_logos.requests, "Session", side_effect=AssertionError("network")), \
                    mock.patch.object(sys, "argv", ["cache_logos.py", "--db", str(db_path)]), mock.patch("sys.stdout"):
                self.assertEqual(cache_logos.main(), 0)
            self.assertFalse((Path(d) / "assets").exists())


if __name__ == "__main__":
    unittest.main()
