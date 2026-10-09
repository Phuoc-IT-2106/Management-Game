"""Report the PandaScore configuration state without ever printing the token.

  NOT_CONFIGURED       no usable PANDASCORE_TOKEN (missing .env, empty, or still the placeholder)
  CONFIGURED           a token is present; no request has been made
  CONNECTION_VERIFIED  --connect only: one real request with that token succeeded

Without --connect this script never touches the network. With --connect it spends
exactly one request (GET /lol/leagues, per_page=1) and stores nothing.
Added during installation; not part of the original starter.
"""
import argparse
import os
from pathlib import Path
import sys

import requests
from dotenv import load_dotenv

from sync_pandascore import PLACEHOLDER_PREFIX, request_page

ENV_FILE = Path(__file__).resolve().parent / ".env"
HEADERS = {"Accept": "application/json", "User-Agent": "ManagementGame-ResearchIngestion/0.1"}
HINTS = {
    401: "Check the token in .env; copy it again from https://app.pandascore.co/dashboard",
    403: "Token rejected or endpoint outside your plan; copy the token again from the dashboard",
    429: "Hourly request quota exhausted; wait for the next hour and retry",
}


def read_token(env_file=ENV_FILE):
    """Return (token, source). Like sync_pandascore, an existing environment variable wins over .env."""
    source = "environment variable" if os.getenv("PANDASCORE_TOKEN") else str(env_file)
    load_dotenv(env_file)
    token = os.getenv("PANDASCORE_TOKEN", "").strip()
    if not token or token.startswith(PLACEHOLDER_PREFIX):
        return "", source
    return token, source


def verify_connection(token, session=None):
    """Spend one request with the token. Returns (ok, http_status, message); the message never contains the token."""
    session = session or requests.Session()
    session.headers.update(HEADERS)
    session.headers["Authorization"] = f"Bearer {token}"
    seen = {}
    session.hooks["response"].append(
        lambda r, *args, **kwargs: seen.update(status=r.status_code, remaining=r.headers.get("X-Rate-Limit-Remaining")))
    ok = False
    try:
        request_page(session, "/lol/leagues", 1, 1, retries=0)
        ok = True
        message = (f"HTTP {seen.get('status', 200)} from /lol/leagues; "
                   f"requests remaining this hour: {seen.get('remaining') or 'unknown'}")
    except RuntimeError as exc:
        message = str(exc)
    except (requests.RequestException, ValueError) as exc:
        message = f"{type(exc).__name__}: {exc}"
    finally:
        session.close()
    return ok, seen.get("status"), message.replace(token, "***")


def main():
    parser = argparse.ArgumentParser(description="Report PANDASCORE_TOKEN state: NOT_CONFIGURED, CONFIGURED or CONNECTION_VERIFIED.")
    parser.add_argument("--connect", action="store_true", help="spend one real API request to verify the token (default: offline only)")
    args = parser.parse_args()

    token, source = read_token()
    if not token:
        print("STATUS: NOT_CONFIGURED")
        print(f"No usable PANDASCORE_TOKEN in {ENV_FILE}")
        print("Open .env, replace PASTE_YOUR_TOKEN_HERE with your own PandaScore token, save, then rerun. See SETUP_VI.md.")
        print("Network: not contacted.")
        return 2
    if not args.connect:
        print("STATUS: CONFIGURED")
        print(f"Token found in {source} ({len(token)} characters; value not shown). Not yet verified against the API.")
        print("Network: not contacted. Rerun with --connect to spend one request and verify it.")
        return 0

    ok, status, message = verify_connection(token)
    if ok:
        print("STATUS: CONNECTION_VERIFIED")
        print(message)
        print("Nothing was stored.")
        return 0
    print("STATUS: CONFIGURED (connection NOT verified)")
    print(f"Request failed: {message}")
    print(HINTS.get(status, "Check the internet connection or proxy settings, then retry"))
    return 1


if __name__ == "__main__":
    sys.exit(main())
