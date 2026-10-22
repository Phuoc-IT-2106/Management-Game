"""Offline tests for the PandaScore image asset cache and its entity mappings (synthetic images only).

The cache is keyed by stable provider IDs; files are named after the entity (player nickname, team, league)
with '__<id>' appended when several entities of one kind share a name.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import cache_logos as cache
import image_mappings
import normalize_pandascore as norm

TOOL_DIR = Path(__file__).resolve().parents[1]
APPROVAL = "Synthetic fixture permission; tests only"


def picture(color="red", format="PNG", size=(4, 4)):
    buffer = io.BytesIO()
    Image.new("RGB", size, color).save(buffer, format=format)
    return buffer.getvalue()


def url(entity, ident, field="image_url"):
    return f"https://cdn-api.pandascore.co/images/{entity}/image/{ident}/{field}.png"


class ImageResponse:
    def __init__(self, status, body=b"", headers=None):
        self.status_code, self.body, self.headers = status, body, headers or {}

    def iter_content(self, size):
        for i in range(0, len(self.body), size):
            yield self.body[i:i + size]

    def close(self):
        pass


class ImageSession:
    def __init__(self, responses):
        self.responses, self.calls, self.headers = dict(responses), [], {}

    def get(self, url, **kwargs):
        self.calls.append(url)
        answer = self.responses[url]
        return answer.pop(0) if isinstance(answer, list) else answer


def png_response(body=None):
    return ImageResponse(200, body or picture(), {"Content-Type": "image/png"})


class AssetTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.db_path, self.assets = self.root / "staging.sqlite3", self.root / "assets"
        self.db = sqlite3.connect(self.db_path)
        self.addCleanup(self.db.close)
        self.db.executescript(norm.OBSERVATION_SCHEMA)
        for table, schema in norm.CURRENT_SCHEMA.items():
            self.db.execute(f"CREATE TABLE {table} ({schema})")
        self.prov = dict(source_run="R", source_query="fixture", source_file="fixture.json", payload_sha256="fixture",
                         observed_at="2026-03-01T00:00:00Z")
        for ident, team, name in (("1", "8", "SameHandle"), ("2", "7", "SameHandle"), ("3", None, "NoImage")):
            self.insert("ps_players", player_id=ident, current_team_id=team, name=name, missing_fields="", **self.prov)
            self.ref("player", ident, available=ident != "3")
        for ident, name in (("7", "Team Seven"), ("8", "Team Eight"), ("9", "Team Nine")):
            self.insert("ps_teams", team_id=ident, name=name, **self.prov)
            self.ref("team", ident, available=ident != "8")
            self.ref("team", ident, "dark_mode_image_url", available=ident == "9")
        self.insert("ps_leagues", league_id="5", name="League Five", **self.prov)
        self.ref("league", "5")
        for ident, start, end in (("100", "2026-01-01", "2026-01-31"), ("101", "2026-02-01", "2026-02-28"),
                                  ("102", "2026-01-10", "2026-01-20")):
            self.insert("ps_tournaments", tournament_id=ident, relevance_2026="begins_in_2026",
                        begin_at=start + "T00:00:00Z", end_at=end + "T00:00:00Z", **self.prov)
        for tournament, team, player in (("100", "7", "1"), ("100", "7", "2"), ("100", "8", "3"),
                                          ("101", "8", "1"), ("102", "9", "1")):
            self.insert("ps_registrations", tournament_id=tournament, team_id=team, player_id=player,
                        source_run="R", observed_at=self.prov["observed_at"])
            self.insert("tournament_registrations", run_id="R", tournament_id=tournament, team_id=team, player_id=player,
                        observed_at=self.prov["observed_at"], source_query="fixture", source_file="fixture.json")
        self.insert("team_membership_observations", run_id="older", team_id="7", player_id="1", source="team.players",
                    observed_at="2026-01-01T00:00:00Z")
        norm.build_player_index(self.db)
        self.db.commit()

    # ---------- helpers ----------

    def insert(self, table, **values):
        self.db.execute(f"INSERT INTO {table} ({','.join(values)}) VALUES ({','.join('?' for _ in values)})",
                        tuple(values.values()))

    def ref(self, entity, ident, field="image_url", available=True):
        self.insert("ps_image_refs", entity_type=entity, entity_id=ident, field=field,
                    url_state="value" if available else "null", url=url(entity, ident, field) if available else None,
                    host="cdn-api.pandascore.co" if available else None,
                    license_status=cache.REVIEW if available else None, source_run="R",
                    observed_at=self.prov["observed_at"])

    def session(self):
        return ImageSession({r[0]: png_response(picture("green" if "/2/" in r[0] else "red"))
                             for r in self.db.execute("SELECT url FROM ps_image_refs WHERE url IS NOT NULL")})

    def run_cache(self, session=None, note=APPROVAL, **kwargs):
        with mock.patch("sys.stdout"):
            return cache.run(self.db_path, self.assets, note, session=session or self.session(),
                             sleep=lambda _: None, **kwargs)

    def mapping(self, name):
        return json.loads((self.assets / "mapping" / name).read_text(encoding="utf-8"))["entries"]

    def players(self):
        return {r["entity_id"]: r for r in self.mapping("player_images.json")}

    def teams(self):
        return {r["entity_id"]: r for r in self.mapping("team_images.json")}

    def cache_row(self, entity, ident, field="image_url"):
        cursor = self.db.execute("SELECT * FROM image_cache WHERE entity_type=? AND entity_id=? AND field=?",
                                 (entity, ident, field))
        keys = [c[0] for c in cursor.description]
        row = cursor.fetchone()
        return dict(zip(keys, row)) if row else None

    def source_digest(self):
        digest = hashlib.sha256()
        for (table,) in self.db.execute("SELECT name FROM sqlite_master WHERE type='table' AND name!='image_cache' ORDER BY 1"):
            digest.update(table.encode())
            digest.update(repr(sorted(map(repr, self.db.execute(f"SELECT * FROM {table}")))).encode())
        return digest.hexdigest()

    # ---------- identity, naming and mapping ----------

    def test_id_mapping_duplicate_nicknames_and_no_cross_entity_paths(self):
        self.run_cache(include_dark=True)
        players = self.players()
        self.assertEqual(players["1"]["image_path"], "players/SameHandle__1.png")
        self.assertEqual(players["2"]["image_path"], "players/SameHandle__2.png")
        self.assertNotEqual((self.assets / "players/SameHandle__1.png").read_bytes(),
                            (self.assets / "players/SameHandle__2.png").read_bytes())
        teams = self.teams()
        self.assertEqual(teams["7"]["logo_path"], "team-logos/Team_Seven.png")
        self.assertEqual(teams["9"]["dark_logo_path"], "team-logos/Team_Nine-dark.png")
        league = next(r for r in self.mapping("league_images.json") if r["entity_id"] == "5") \
            if (self.assets / "mapping" / "league_images.json").exists() else None
        if league is not None:
            self.assertEqual(league.get("image_path") or league.get("logo_path"), "league-logos/League_Five.png")
        self.assertTrue((self.assets / "league-logos/League_Five.png").is_file())

    def test_file_names_keep_the_entity_name_and_stay_unique_and_safe(self):
        stems = cache.asset_stems(self.db)
        self.assertEqual(stems[("team", "7")], "Team_Seven")
        self.assertEqual(stems[("league", "5")], "League_Five")
        self.assertEqual(stems[("player", "3")], "NoImage")
        self.assertEqual(stems[("player", "1")], "SameHandle__1")
        self.assertEqual(cache.clean_name('  Gen.G: "Esports"/Academy.  '), "Gen.G_Esports_Academy")
        self.assertEqual(cache.clean_name("小明 Kiin"), "小明_Kiin")
        self.assertEqual(cache.clean_name("CON"), "CON_")
        self.assertEqual(cache.target_stem("team", "dark_mode_image_url", "9", stems), "team-logos/Team_Nine-dark")
        self.assertEqual(cache.target_stem("player", "image_url", "999", {}), "players/player-999")

    def test_names_that_differ_only_by_case_are_disambiguated(self):
        self.insert("ps_players", player_id="4", name="samehandle", missing_fields="", **self.prov)
        stems = cache.asset_stems(self.db)
        self.assertEqual({stems[("player", i)] for i in "124"},
                         {"SameHandle__1", "SameHandle__2", "samehandle__4"})

    def test_legacy_id_named_files_are_relocated_without_any_request(self):
        self.run_cache()
        for stem, ident in (("SameHandle__1", "1"), ("SameHandle__2", "2")):
            (self.assets / f"players/{stem}.png").rename(self.assets / f"players/{ident}.png")
            self.db.execute("UPDATE image_cache SET local_path=? WHERE entity_type='player' AND entity_id=?",
                            (f"players/{ident}.png", ident))
        self.db.commit()
        session = self.session()
        summary = self.run_cache(session)
        self.assertEqual((summary["http_requests"], session.calls), (0, []))
        self.assertTrue((self.assets / "players/SameHandle__1.png").is_file())
        self.assertFalse((self.assets / "players/1.png").exists())
        player = self.players()["2"]
        self.assertEqual((player["image_status"], player["image_path"]), ("CACHED", "players/SameHandle__2.png"))

    def test_an_upstream_rename_moves_the_file_instead_of_refetching(self):
        self.run_cache()
        self.db.execute("UPDATE ps_teams SET name='Renamed Seven' WHERE team_id='7'")
        self.db.commit()
        session = self.session()
        summary = self.run_cache(session)
        self.assertEqual(summary["http_requests"], 0)
        self.assertFalse((self.assets / "team-logos/Team_Seven.png").exists())
        self.assertEqual(self.teams()["7"]["logo_path"], "team-logos/Renamed_Seven.png")

    def test_transfer_preserves_historical_registration_and_reuses_portrait(self):
        before = self.db.execute("SELECT * FROM ps_registrations").fetchall()
        self.run_cache()
        roster = [r for r in self.mapping("roster_image_index_2026.json") if r["player_id"] == "1"]
        self.assertEqual({r["player_image_path"] for r in roster}, {"players/SameHandle__1.png"})
        self.assertEqual({r["team_id"] for r in roster}, {"7", "8", "9"})
        january = next(r for r in roster if r["tournament_id"] == "100")
        self.assertEqual((january["team_logo_path"], january["provider_current_team_id"]),
                         ("team-logos/Team_Seven.png", "8"))
        self.assertEqual(self.db.execute("SELECT * FROM ps_registrations").fetchall(), before)
        portrait = self.players()["1"]
        self.assertEqual(portrait["historical_team_observations"][0]["team_id"], "7")

    def test_missing_urls_and_dark_fallback_are_explicit(self):
        self.run_cache(include_dark=True)
        players, teams = self.players(), self.teams()
        self.assertEqual((players["3"]["image_status"], players["3"]["image_path"]), ("MISSING_URL", None))
        self.assertIsNone(teams["7"]["dark_logo_path"])
        self.assertEqual(image_mappings.select_team_logo(teams["7"], True), "team-logos/Team_Seven.png")
        self.assertEqual(image_mappings.select_team_logo(teams["9"], True), "team-logos/Team_Nine-dark.png")
        self.assertIsNone(image_mappings.select_team_logo(teams["8"], True))

    # ---------- licence gate, idempotency and resume ----------

    def test_without_approval_or_in_dry_run_no_connection_is_opened(self):
        with mock.patch.object(cache.requests, "Session", side_effect=AssertionError("network")):
            with mock.patch("sys.stdout"):
                summary = cache.run(self.db_path, self.assets, None)
                dry = cache.run(self.db_path, self.assets, APPROVAL, dry_run=True)
        self.assertEqual((summary["http_requests"], dry["http_requests"]), (0, 0))
        self.assertEqual(summary["license_status"], cache.REVIEW)
        self.assertFalse(list(self.assets.glob("*/*.png")))

    def test_repeat_run_is_idempotent_and_makes_no_requests(self):
        self.run_cache()
        session = self.session()
        again = self.run_cache(session)
        self.assertEqual((session.calls, again["http_requests"], again["cached"]), ([], 0, 0))
        self.assertEqual(len(list(self.assets.rglob("*.png"))), 5)
        self.assertEqual(self.db.execute("SELECT count(*) FROM image_cache").fetchone()[0],
                         len({(r[0], r[1], r[2]) for r in self.db.execute(
                             "SELECT entity_type, entity_id, field FROM image_cache")}))

    def test_source_url_change_refetches_and_removes_obsolete_format(self):
        self.run_cache()
        changed = url("player", "1") + "-v2"
        self.db.execute("UPDATE ps_image_refs SET url=? WHERE entity_type='player' AND entity_id='1'", (changed,))
        self.db.commit()
        session = ImageSession({changed: ImageResponse(200, picture(format="WEBP"), {"Content-Type": "image/webp"})})
        self.run_cache(session)
        self.assertEqual(session.calls, [changed])
        self.assertFalse((self.assets / "players/SameHandle__1.png").exists())
        self.assertTrue((self.assets / "players/SameHandle__1.webp").exists())

    def test_hash_mismatch_refetches_and_offline_export_never_claims_cached(self):
        self.run_cache()
        (self.assets / "players/SameHandle__1.png").write_bytes(picture("blue"))
        with mock.patch("sys.stdout"):
            cache.run(self.db_path, self.assets, dry_run=True)
        player = self.players()["1"]
        self.assertEqual((player["image_status"], player["image_path"]), ("INVALID_IMAGE", None))
        session = self.session()
        self.run_cache(session)
        self.assertEqual(session.calls, [url("player", "1")])
        self.assertEqual(self.players()["1"]["image_status"], "CACHED")

    def test_cross_entity_cache_assignment_is_rejected(self):
        self.run_cache()
        self.db.execute("UPDATE image_cache SET local_path='players/SameHandle__2.png' "
                        "WHERE entity_type='player' AND entity_id='1'")
        self.db.commit()
        with mock.patch("sys.stdout"):
            cache.run(self.db_path, self.assets, dry_run=True)
        player = self.players()["1"]
        self.assertEqual(player["image_status"], "INVALID_IMAGE")
        self.assertIsNone(player["image_path"])
        # The other player's file is untouched by the rejected claim.
        self.assertEqual(self.players()["2"]["image_path"], "players/SameHandle__2.png")

    def test_mismatched_requested_url_never_becomes_a_cache_hit(self):
        self.run_cache()
        self.db.execute("UPDATE image_cache SET requested_url='https://cdn-api.pandascore.co/other.png' "
                        "WHERE entity_type='player' AND entity_id='1'")
        self.db.commit()
        with mock.patch("sys.stdout"):
            cache.run(self.db_path, self.assets, dry_run=True)
        self.assertEqual(self.players()["1"]["image_status"], "INVALID_IMAGE")

    # ---------- failures ----------

    def test_corrupt_http_and_write_failures_are_recorded_and_retried_only_on_request(self):
        corrupt = ImageResponse(200, b"\x89PNG\r\n\x1a\n" + b"not really a png", {"Content-Type": "image/png"})
        session = ImageSession({url("player", "1"): corrupt, url("player", "2"): ImageResponse(404),
                                url("team", "7"): png_response(), url("team", "9"): png_response(),
                                url("league", "5"): png_response()})
        summary = self.run_cache(session)
        self.assertEqual(summary["failed"], 2)
        players = self.players()
        self.assertEqual((players["1"]["image_status"], players["1"]["image_path"]), ("INVALID_IMAGE", None))
        self.assertEqual((players["2"]["image_status"], players["2"]["image_path"]), ("DOWNLOAD_FAILED", None))
        self.assertFalse(list((self.assets / "players").glob("*")))
        # Known failures are skipped, not hammered, until --retry-failed asks for them.
        again = ImageSession({})
        skipped = self.run_cache(again)
        self.assertEqual((again.calls, skipped["skipped_failed"]), ([], 2))
        retry = ImageSession({url("player", "1"): png_response(), url("player", "2"): png_response()})
        self.run_cache(retry, retry_failed=True)
        self.assertEqual(sorted(retry.calls), [url("player", "1"), url("player", "2")])
        self.assertEqual({p["image_status"] for p in (self.players()["1"], self.players()["2"])}, {"CACHED"})

    def test_write_error_leaves_no_partial_file_and_records_failure(self):
        with mock.patch.object(cache, "store", side_effect=OSError("disk full")):
            summary = self.run_cache()
        self.assertEqual(summary["cached"], 0)
        self.assertEqual(summary["failed"], 5)
        self.assertFalse(list(self.assets.rglob("*.png")))
        self.assertEqual(self.players()["1"]["image_status"], "DOWNLOAD_FAILED")

    def test_keyboard_interrupt_checkpoints_and_resumes_without_refetching_cached(self):
        calls = {"n": 0}
        real = cache.store

        def interrupting(*args):
            calls["n"] += 1
            if calls["n"] == 3:
                raise KeyboardInterrupt
            return real(*args)

        with mock.patch.object(cache, "store", interrupting):
            summary = self.run_cache()
        self.assertTrue(summary["interrupted"])
        done = self.db.execute("SELECT count(*) FROM image_cache WHERE status='CACHED'").fetchone()[0]
        self.assertEqual(done, 2)
        session = self.session()
        self.run_cache(session)
        self.assertEqual(len(session.calls), 3)
        self.assertEqual(len(list(self.assets.rglob("*.png"))), 5)

    def test_all_four_formats_are_preserved_with_matching_extensions(self):
        for ident, fmt in (("1", "PNG"), ("2", "JPEG"), ("7", "GIF"), ("9", "WEBP")):
            entity = "player" if ident in "12" else "team"
            self.db.execute("UPDATE ps_image_refs SET url=? WHERE entity_type=? AND entity_id=? AND field='image_url'",
                            (url(entity, ident) + "." + fmt.lower(), entity, ident))
        self.db.commit()
        mime = {"PNG": "image/png", "JPEG": "image/jpeg", "GIF": "image/gif", "WEBP": "image/webp"}
        responses = {}
        for ident, fmt in (("1", "PNG"), ("2", "JPEG"), ("7", "GIF"), ("9", "WEBP")):
            entity = "player" if ident in "12" else "team"
            responses[url(entity, ident) + "." + fmt.lower()] = ImageResponse(200, picture(format=fmt), {"Content-Type": mime[fmt]})
        responses[url("league", "5")] = png_response()
        self.run_cache(ImageSession(responses))
        self.assertEqual(self.players()["2"]["image_path"], "players/SameHandle__2.jpg")
        self.assertEqual(self.teams()["7"]["logo_path"], "team-logos/Team_Seven.gif")
        self.assertEqual(self.teams()["9"]["logo_path"], "team-logos/Team_Nine.webp")

    def test_an_image_over_the_pixel_budget_is_rejected(self):
        huge = picture(size=(cache.MAX_PIXELS // 1000 + 1, 1001))
        with self.assertRaises(cache.ImageError):
            cache.validate_image(huge, "image/png")

    # ---------- limits, pilot and schema ----------

    def test_pilot_and_limit_bound_the_number_of_requests(self):
        for n in range(10, 60):
            self.insert("ps_players", player_id=str(n), name=f"P{n}", missing_fields="", **self.prov)
            self.ref("player", str(n))
        for n in range(20, 50):
            self.insert("ps_teams", team_id=str(n), name=f"T{n}", **self.prov)
            self.ref("team", str(n))
        self.db.commit()
        pilot = self.run_cache(pilot=True)
        self.assertEqual(pilot["requested"], 40)
        self.assertEqual(len(list((self.assets / "players").glob("*"))), 20)
        self.assertEqual(len(list((self.assets / "team-logos").glob("*"))), 20)
        limited = self.run_cache(limit=5)
        self.assertEqual(limited["requested"], 5)
        full = self.run_cache()
        self.assertEqual(full["requested"] + 45, self.db.execute(
            "SELECT count(*) FROM image_cache WHERE status='CACHED'").fetchone()[0])

    def test_legacy_image_cache_table_is_migrated_additively(self):
        self.db.execute("DROP TABLE IF EXISTS image_cache")
        self.db.execute("""CREATE TABLE image_cache (
            entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, field TEXT NOT NULL, variant TEXT NOT NULL,
            source_url TEXT NOT NULL, requested_url TEXT NOT NULL, status TEXT NOT NULL, local_path TEXT, content_type TEXT,
            format TEXT, bytes INTEGER, sha256 TEXT, error TEXT, attempted_at TEXT NOT NULL, license_note TEXT NOT NULL,
            PRIMARY KEY (entity_type, entity_id, field, variant))""")
        body = picture("red")
        _, _, digest = cache.store(self.assets, "players/1.png", body)
        self.db.execute("INSERT INTO image_cache VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                        ("player", "1", "image_url", "original", url("player", "1"), url("player", "1"), "CACHED",
                         "players/1.png", "image/png", "png", len(body), digest, None, "2026-01-01", "legacy decision"))
        self.db.commit()
        session = self.session()
        self.run_cache(session)
        self.assertNotIn(url("player", "1"), session.calls)
        columns = {r[1] for r in self.db.execute("PRAGMA table_info(image_cache)")}
        self.assertTrue({"provider", "use_status", "cached_at", "validated_at", "retryable"} <= columns)
        self.assertEqual(self.players()["1"]["image_path"], "players/SameHandle__1.png")
        self.assertTrue((self.assets / "players/SameHandle__1.png").is_file())
        self.assertFalse((self.assets / "players/1.png").exists())

    # ---------- references, reviews and preserved sources ----------

    def test_removed_entity_never_exposes_a_cached_path(self):
        self.run_cache()
        self.db.execute("DELETE FROM ps_players WHERE player_id='2'")
        self.db.commit()
        with mock.patch("sys.stdout"):
            cache.run(self.db_path, self.assets, dry_run=True)
        entry = next(e for e in json.loads((self.assets / "image_manifest.json").read_text(encoding="utf-8"))["entries"]
                     if e["entity_type"] == "player" and e["entity_id"] == "2")
        self.assertNotEqual(entry["status"], "CACHED")
        self.assertIsNone(entry["local_path"])

    def test_broken_and_overlapping_references_are_counted_not_repaired(self):
        self.db.execute("UPDATE ps_players SET current_team_id='404' WHERE player_id='1'")
        self.db.commit()
        before = self.source_digest()
        self.run_cache()
        coverage = json.loads((self.assets / "image_coverage.json").read_text(encoding="utf-8"))
        associations = coverage["associations"]
        self.assertEqual(associations["broken_references"]["current_teams"], 1)
        self.assertEqual(associations["overlapping_registration_pairs"], 1)
        self.assertEqual(associations["valid_roster_entity_links"], associations["roster_rows"])
        reviews = self.mapping("association_reviews_2026.json")
        self.assertEqual({r["player_id"] for r in reviews}, {"1"})
        self.assertTrue(all(r["review_required"] for r in reviews))
        self.assertEqual(self.source_digest(), before)

    def test_manifest_and_mappings_rebuild_offline_from_the_cache(self):
        self.run_cache()
        shutil_targets = [self.assets / "image_manifest.json", self.assets / "mapping" / "player_images.json"]
        for path in shutil_targets:
            path.unlink()
        with mock.patch.object(cache.requests, "Session", side_effect=AssertionError("network")):
            with mock.patch("sys.stdout"):
                summary = cache.run(self.db_path, self.assets, dry_run=True)
        self.assertEqual(summary["http_requests"], 0)
        self.assertTrue(all(path.is_file() for path in shutil_targets))
        self.assertEqual(self.players()["1"]["image_status"], "CACHED")

    def test_run_never_changes_normalized_source_tables(self):
        before = self.source_digest()
        self.run_cache(include_dark=True)
        self.run_cache(refresh=True)
        self.assertEqual(self.source_digest(), before)

    def test_downloaded_assets_are_git_ignored(self):
        paths = ["tools/lol-data-ingestion/data/assets/players/.png",
                 "tools/lol-data-ingestion/data/assets/team-logos/T1-dark.png",
                 "tools/lol-data-ingestion/data/assets/league-logos/LCK.png",
                 "tools/lol-data-ingestion/data/assets/image_manifest.json"]
        repo = TOOL_DIR.parents[1]
        if not (repo / ".git").exists():
            self.skipTest("not a git checkout")
        result = subprocess.run(["git", "check-ignore", *paths], cwd=repo, capture_output=True, text=True)
        self.assertEqual(sorted(result.stdout.split()), sorted(paths))


if __name__ == "__main__":
    unittest.main()
