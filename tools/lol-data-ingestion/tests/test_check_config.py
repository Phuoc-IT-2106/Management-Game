import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock

import requests

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_config import read_token, verify_connection
import sync_pandascore

TOKEN = "offline-test-token-0123456789"


class FakeResponse:
    def __init__(self, status_code, body=None, headers=None):
        self.status_code = status_code
        self.body = body if body is not None else []
        self.headers = headers or {}

    def json(self):
        return self.body

    def raise_for_status(self):
        if self.status_code >= 400:
            raise requests.HTTPError(f"{self.status_code} Server Error (sent {TOKEN})")


class FakeSession:
    """Stands in for requests.Session: no sockets, records calls, runs response hooks."""

    def __init__(self, response):
        self.response = response
        self.headers = {}
        self.hooks = {"response": []}
        self.calls = []

    def get(self, url, params=None, timeout=None):
        self.calls.append((url, params))
        for hook in self.hooks["response"]:
            hook(self.response)
        return self.response

    def close(self):
        pass


class VerifyConnectionTests(unittest.TestCase):
    def test_success_spends_exactly_one_minimal_request(self):
        session = FakeSession(FakeResponse(200, [{"id": 1}], {"X-Rate-Limit-Remaining": "999"}))
        ok, status, message = verify_connection(TOKEN, session)
        self.assertTrue(ok)
        self.assertEqual(status, 200)
        self.assertIn("999", message)
        self.assertEqual(len(session.calls), 1)
        self.assertTrue(session.calls[0][0].endswith("/lol/leagues"))
        self.assertEqual(session.calls[0][1], {"page": 1, "per_page": 1})
        self.assertEqual(session.headers["Authorization"], f"Bearer {TOKEN}")

    def test_failures_report_status_without_retry_or_token(self):
        for code in (401, 403, 429, 500):
            with self.subTest(code=code), mock.patch("sync_pandascore.time.sleep") as sleep:
                session = FakeSession(FakeResponse(code))
                ok, status, message = verify_connection(TOKEN, session)
                self.assertFalse(ok)
                self.assertEqual(status, code)
                self.assertIn(str(code), message)
                self.assertNotIn(TOKEN, message)
                self.assertEqual(len(session.calls), 1)
                sleep.assert_not_called()


class ReadTokenTests(unittest.TestCase):
    def read(self, content):
        with tempfile.TemporaryDirectory() as d, mock.patch.dict(os.environ, {}, clear=False):
            os.environ.pop("PANDASCORE_TOKEN", None)
            env_file = Path(d) / ".env"
            if content is not None:
                env_file.write_text(content, encoding="utf-8")
            return read_token(env_file)[0]

    def test_placeholder_missing_or_empty_is_not_configured(self):
        self.assertEqual(self.read("PANDASCORE_TOKEN=PASTE_YOUR_TOKEN_HERE\n"), "")
        self.assertEqual(self.read("PANDASCORE_TOKEN=PASTE_YOUR_API_KEY_HERE\n"), "")
        self.assertEqual(self.read("PANDASCORE_TOKEN=\n"), "")
        self.assertEqual(self.read(None), "")

    def test_real_value_is_read_and_trimmed(self):
        self.assertEqual(self.read(f"# comment\nPANDASCORE_TOKEN= {TOKEN} \n"), TOKEN)
        self.assertEqual(self.read(f'PANDASCORE_TOKEN="{TOKEN}"\n'), TOKEN)


class SyncGuardTests(unittest.TestCase):
    def test_sync_refuses_placeholders_before_creating_a_session(self):
        for value in ("PASTE_YOUR_TOKEN_HERE", "PASTE_YOUR_API_KEY_HERE", ""):
            with self.subTest(value=value), tempfile.TemporaryDirectory() as d, \
                    mock.patch.dict(os.environ, {"PANDASCORE_TOKEN": value}), \
                    mock.patch.object(sync_pandascore, "load_dotenv"), \
                    mock.patch.object(sync_pandascore.requests, "Session") as session, \
                    mock.patch.object(sys, "argv", ["sync_pandascore.py", "--max-pages", "1",
                                                    "--db", f"{d}/db.sqlite3", "--snapshot-root", f"{d}/raw"]), \
                    mock.patch("sys.stderr"):
                with self.assertRaises(SystemExit) as stop:
                    sync_pandascore.main()
                self.assertEqual(stop.exception.code, 2)
                session.assert_not_called()


if __name__ == "__main__":
    unittest.main()
