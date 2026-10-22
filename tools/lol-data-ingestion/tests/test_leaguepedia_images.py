"""Offline tests for the Leaguepedia 2026 portrait fetcher (fake client and downloader; synthetic images)."""
import io
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import cache_logos as cache
import fetch_leaguepedia_images as lp
import normalize_pandascore as norm


def picture(color="red", format="PNG"):
    buffer = io.BytesIO()
    Image.new("RGB", (4, 4), color).save(buffer, format=format)
    return buffer.getvalue()


class FakeClient:
    def __init__(self, images, info):
        self.images, self.info, self.requests = images, info, 0

    def cargo(self, tables, fields, where, order_by, page=500):
        self.where = where
        return list(self.images)

    def file_info(self, names):
        return {n: self.info[n] for n in names if n in self.info}


class FakeResponse:
    def __init__(self, url, body, status=200, content_type="image/png"):
        self.url, self.status_code, self._body = url, status, body
        self.headers = {"Content-Type": content_type}

    def iter_content(self, size):
        for i in range(0, len(self._body), size):
            yield self._body[i:i + size]


class FakeDownloader:
    def __init__(self, bodies):
        self.bodies, self.calls = bodies, []

    def get(self, url, **kwargs):
        self.calls.append(url)
        body = self.bodies[url]
        return body if isinstance(body, FakeResponse) else FakeResponse(url, body)


def file_url(name):
    return f"https://static.wikia.nocookie.net/lolesports_gamepedia_en/images/a/aa/{name.replace(' ', '_')}"


class FetchTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.db_path = self.root / "staging.sqlite3"
        db = sqlite3.connect(self.db_path)
        self.addCleanup(db.close)
        db.executescript(norm.OBSERVATION_SCHEMA)
        for table, schema in norm.CURRENT_SCHEMA.items():
            db.execute(f"CREATE TABLE {table} ({schema})")
        prov = dict(source_run="R", source_query="q", source_file="f", payload_sha256="x", observed_at="2026-03-01T00:00:00Z")

        def insert(table, **values):
            db.execute(f"INSERT INTO {table} ({','.join(values)}) VALUES ({','.join('?' for _ in values)})", tuple(values.values()))

        for pid, name, image in (("1", "Faker", "https://cdn-api.pandascore.co/images/player/image/1/t1_faker_2023_split_1.png"),
                                 ("2", "Twin", None), ("3", "Twin", None), ("4", "Solo", None), ("5", "Ghost", None)):
            insert("ps_players", player_id=pid, name=name, image_url=image, missing_fields="", **prov)
        insert("ps_teams", team_id="10", name="T1", acronym="T1", **prov)
        insert("ps_teams", team_id="11", name="Gen.G Esports", acronym="GEN", **prov)
        insert("ps_tournaments", tournament_id="100", relevance_2026="begins_in_2026", **prov)
        for pid, team in (("1", "10"), ("2", "10"), ("3", "11"), ("4", "11")):
            insert("ps_registrations", tournament_id="100", team_id=team, player_id=pid, source_run="R",
                   observed_at=prov["observed_at"])
        for pid in ("1", "2", "3", "4"):
            insert("ps_player_index_2026", player_id=pid, in_provider_catalog=1, registered_in_2026="verified",
                   registered_stages=1, registered_team_ids="", match_participation_verified_2026="no",
                   academy_verified="no", reserve_verified="no", evidence="fixture")
        db.commit()
        self.images = [
            {"Link": "Faker", "FileName": "T1 Faker 2026 Split 1.png", "Team": "T1", "Tournament": "LCK/2026 Season/Rounds 1-2",
             "ImageType": "Headshot", "IsProfileImage": "1"},
            {"Link": "Faker", "FileName": "T1 Faker 2026 Split 2.png", "Team": "T1", "Tournament": "LCK/2026 Season/Rounds 3-5",
             "ImageType": "Headshot", "IsProfileImage": "1"},
            {"Link": "Twin (Alice)", "FileName": "T1 Twin 2026.png", "Team": "T1 (Korean Team)", "Tournament": "LCK/2026 Season",
             "ImageType": "Headshot", "IsProfileImage": "1"},
            {"Link": "Solo", "FileName": "Solo 2026.png", "Team": "Another Team", "Tournament": "LEC/2026 Season",
             "ImageType": "Headshot", "IsProfileImage": "1"},
            {"Link": "Nobody", "FileName": "Nobody 2026.png", "Team": "X", "Tournament": "LEC/2026 Season",
             "ImageType": "Headshot", "IsProfileImage": "1"},
        ]
        self.info = {r["FileName"]: {"url": file_url(r["FileName"]), "timestamp": "2026-02-01T00:00:00Z", "license": None,
                                     "artist": None} for r in self.images}
        self.info["T1 Faker 2026 Split 2.png"]["timestamp"] = "2026-06-01T00:00:00Z"

    def run_fetch(self, **kwargs):
        client = FakeClient(self.images, self.info)
        bodies = {file_url(n): picture(c) for n, c in (("T1 Faker 2026 Split 2.png", "red"), ("T1 Faker 2026 Split 1.png", "blue"),
                                                        ("T1 Twin 2026.png", "green"), ("Solo 2026.png", "yellow"))}
        downloader = FakeDownloader(bodies)
        out = self.root / "assets" / "players-2026"
        summary = lp.run(self.db_path, out, self.root / "manifest.json", self.root / "mapping.json", client, downloader,
                         sleep=lambda _: None, **kwargs)
        return summary, client, downloader, out

    def manifest(self):
        return {e["player_id"]: e for e in json.loads((self.root / "manifest.json").read_text(encoding="utf-8"))["entries"]}

    def test_matches_by_unique_name_team_and_never_guesses_ambiguous_names(self):
        summary, client, _, _ = self.run_fetch(dry_run=True)
        entries = self.manifest()
        self.assertIn("%2026%", client.where)
        self.assertEqual(set(entries), {"1", "2", "4"})
        self.assertEqual((entries["1"]["confidence"], entries["1"]["team_agrees"]), ("HIGH", True))
        self.assertEqual(entries["2"]["leaguepedia_link"], "Twin (Alice)")  # team agreement disambiguates "Twin"
        self.assertEqual(entries["4"]["confidence"], "MEDIUM")  # unique name, team differs: flagged, not hidden
        reviews = json.loads((self.root / "manifest.json").read_text(encoding="utf-8"))["review"]
        self.assertEqual([r["reason"] for r in reviews], ["no_catalog_player"])
        self.assertEqual(summary["downloaded"], 0)

    def test_downloads_the_latest_2026_file_and_names_it_after_the_player(self):
        summary, _, downloader, out = self.run_fetch()
        entry = self.manifest()["1"]
        self.assertEqual(entry["file_name"], "T1 Faker 2026 Split 2.png")
        self.assertEqual(entry["local_path"], "players-2026/Faker.png")
        self.assertEqual(entry["previous_pandascore_year_hint"], "2023")
        self.assertTrue((out / "Faker.png").is_file())
        self.assertEqual(summary["downloaded"], 3)
        self.assertNotIn(file_url("T1 Faker 2026 Split 1.png"), downloader.calls)
        mapping = json.loads((self.root / "mapping.json").read_text(encoding="utf-8"))["entries"]
        self.assertEqual({m["player_id"] for m in mapping}, {"1", "2", "4"})
        self.assertNotIn("source_url", mapping[0])

    def test_rerun_is_unchanged_and_untrusted_hosts_are_rejected(self):
        self.run_fetch()
        summary, _, _, _ = self.run_fetch()
        self.assertEqual((summary["downloaded"], summary["unchanged"]), (0, 3))
        self.info["Solo 2026.png"]["url"] = "https://evil.example/Solo.png"
        summary, _, _, _ = self.run_fetch()
        self.assertEqual(self.manifest()["4"]["status"], "DOWNLOAD_FAILED")
        self.assertEqual(summary["failed"], 1)

    def test_corrupt_download_is_recorded_and_leaves_no_file(self):
        client = FakeClient(self.images, self.info)
        bodies = {file_url(r["FileName"]): b"\x89PNG\r\n\x1a\nbroken" for r in self.images}
        out = self.root / "assets" / "players-2026"
        summary = lp.run(self.db_path, out, self.root / "manifest.json", self.root / "mapping.json", client,
                         FakeDownloader(bodies), sleep=lambda _: None)
        self.assertEqual((summary["downloaded"], summary["failed"]), (0, 3))
        self.assertFalse(list(out.glob("*")))
        self.assertEqual({e["status"] for e in self.manifest().values()}, {"INVALID_IMAGE"})

    def test_a_file_that_is_not_from_2026_is_never_used_as_a_2026_portrait(self):
        self.images.append({"Link": "Solo", "FileName": "Solo 2025 Split 3.png", "Team": "Another Team",
                            "Tournament": "LEC/2026 Season", "ImageType": "Headshot", "IsProfileImage": "1"})
        self.info["Solo 2025 Split 3.png"] = {"url": file_url("Solo 2025 Split 3.png"), "timestamp": "2025-07-01T00:00:00Z",
                                              "license": None, "artist": None}
        self.info["Solo 2026.png"]["timestamp"] = "2026-02-01T00:00:00Z"
        self.run_fetch()
        self.assertEqual(self.manifest()["4"]["file_name"], "Solo 2026.png")
        self.images[:] = [r for r in self.images if r["FileName"] != "Solo 2026.png"]
        summary, _, _, _ = self.run_fetch()
        self.assertNotIn("4", self.manifest())
        self.assertEqual(summary["no_usable_file"], 1)

    def test_team_agreement_ignores_parentheticals_and_short_fragments(self):
        self.assertTrue(lp.teams_agree("DRX Challengers", "Kiwoom DRX Challengers"))
        self.assertTrue(lp.teams_agree("T1 (Korean Team)", "T1"))
        self.assertFalse(lp.teams_agree("T1", "Gen.G"))
        self.assertFalse(lp.teams_agree("X", "Xenon"))
        self.assertEqual(lp.year_hint("https://x/y/tl_core_jj_2025_split_1.png"), "2025")


if __name__ == "__main__":
    unittest.main()
