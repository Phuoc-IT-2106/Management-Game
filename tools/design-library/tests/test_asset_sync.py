import importlib.util
from pathlib import Path
import hashlib
import json
import tempfile
import types
import unittest
from unittest.mock import patch

MODULE_PATH = Path(__file__).resolve().parents[1] / "asset_sync.py"
spec = importlib.util.spec_from_file_location("asset_sync", MODULE_PATH)
sync = importlib.util.module_from_spec(spec)
spec.loader.exec_module(sync)

MAPPING = {"folders": {
    "mockups": "M", "art_direction": "A", "references": "R", "ui_assets": "U",
    "game_art": "G", "approved": "P", "exports": "E", "archive": "X",
    "github_documentation_evidence": "DOC", "github_ui_mockups_import": "MOCK",
    "github_runtime_asset_backup": "RUN"
}}

class AssetSyncTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.inbox = self.root / "artifacts" / "design-inbox"
        self.statepath = self.root / "artifacts" / "design-library-sync" / "state.json"
        self.patchers = [
            patch.object(sync, "REPO", self.root),
            patch.object(sync, "INBOX", self.inbox),
            patch.object(sync, "STATE_PATH", self.statepath),
        ]
        for p in self.patchers:
            p.start()
            self.addCleanup(p.stop)

    def file(self, relative, data=b"image content"):
        p = self.root / relative
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(data)
        return p

    def test_inbox_portal_routing(self):
        f = self.file("artifacts/design-inbox/02_UI_Mockups/01_Portal/Editorial_Command/rev04.png")
        self.assertEqual(sync.destination(f, "inbox", MAPPING),
                         ("mockups", "M", "01_Portal/Editorial_Command/rev04.png"))

    def test_inbox_invalid_category(self):
        f = self.file("artifacts/design-inbox/unknown/test.png")
        with self.assertRaises(ValueError):
            sync.destination(f, "inbox", MAPPING)

    def test_legacy_documentation_classification(self):
        f = self.file("docs/10-vertical-slice/ui-kit/evidence/qa.png")
        self.assertEqual(sync.destination(f, "git", MAPPING),
                         ("github_documentation_evidence", "DOC",
                          "docs/10-vertical-slice/ui-kit/evidence/qa.png"))

    def test_legacy_mockup_classification(self):
        f = self.file("docs/10-vertical-slice/ui-research/mockups/generated-reference-set/01_portal.webp")
        self.assertEqual(sync.destination(f, "git", MAPPING)[0], "github_ui_mockups_import")

    def test_runtime_asset_protected(self):
        f = self.file("game/Client/Assets/UI/icon.png")
        with self.assertRaises(ValueError):
            sync.destination(f, "git", MAPPING)
        self.assertEqual(sync.destination(f, "git", MAPPING, True)[0], "github_runtime_asset_backup")

    def test_dry_run_never_invokes_rclone_or_removes_file(self):
        f = self.file("artifacts/design-inbox/03_UI_Assets/Icons/arrow.png")
        with patch.object(sync, "execute", side_effect=AssertionError("network invoked")):
            status = sync.process(f, "inbox", MAPPING, {"records": {}},
                                  "gdrive", False, True, False)
        self.assertEqual(status, "PLAN")
        self.assertTrue(f.exists())

    def test_verified_upload_then_remove_inbox_only(self):
        f = self.file("artifacts/design-inbox/03_UI_Assets/Icons/arrow.png")
        md5 = hashlib.md5(f.read_bytes()).hexdigest()
        state = {"records": {}}
        with patch.object(sync, "remote_md5", side_effect=[None, md5]), \
             patch.object(sync, "execute", return_value="") as upload:
            status = sync.process(f, "inbox", MAPPING, state, "gdrive", True, True, False)
        self.assertEqual(status, "VERIFIED")
        self.assertFalse(f.exists())
        self.assertTrue(upload.called)
        self.assertTrue(self.statepath.exists())
        self.assertEqual(next(iter(state["records"].values()))["md5"], md5)

    def test_collision_prevents_overwrite_and_deletion(self):
        f = self.file("artifacts/design-inbox/03_UI_Assets/Icons/arrow.png")
        with patch.object(sync, "remote_md5", return_value="0" * 32), \
             patch.object(sync, "execute", side_effect=AssertionError("Should not upload")):
            with self.assertRaises(RuntimeError):
                sync.process(f, "inbox", MAPPING, {"records": {}},
                             "gdrive", True, True, False)
        self.assertTrue(f.exists())


if __name__ == "__main__":
    unittest.main()
