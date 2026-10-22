"""Download Oracle's Elixir yearly match-data CSVs from the publisher's own public link, with provenance.

Source chain, every link read from the previous page rather than guessed:
  1. the official downloads page OFFICIAL_PAGE (a JavaScript app);
  2. its JavaScript bundle, which carries the public Google Drive folder link ("Access files via Google Drive");
  3. that folder's public listing, which carries the file id of each "<year>_LoL_esports_match_data_from_OraclesElixir.csv";
  4. Google Drive's public download endpoint for that file id.
The publisher's API (which needs a key) is never called. Drive's anonymous download quota ("Quota exceeded") is an
access limit: the script records QUOTA_EXCEEDED and stops; it does not work around it. Drive's "can't scan this file
for viruses" notice is a confirmation page, not an access control; the script submits that page's own form once, as
clicking "Download anyway" would.

Each attempt writes data/import/oracles-elixir/snapshots/<UTC stamp>/ (git-ignored): the CSV and provenance.json
(page, bundle, folder and download URLs, Drive file id, bytes, SHA-256, header facts, HTTP status). A snapshot is never
overwritten; bytes identical to an earlier snapshot are recorded as UNCHANGED and not stored twice. Terms shown on the
page (recorded in provenance): data is free for analysts, commentators and fans; game statistics are the property of
Riot Games and their use must follow Riot's policies; files update once per day.

  python fetch_oracles_elixir.py --years 2025 2026            # one attempt per year
  python fetch_oracles_elixir.py --years 2026 --dry-run       # discovery only, no file download
Exit codes: 0 every year downloaded or unchanged; 3 at least one year blocked (quota) or failed.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import html
import json
from pathlib import Path
import re
import sys
import time
from urllib.parse import urljoin

import requests

OFFICIAL_PAGE = "https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads"
DATA_DIR = Path(__file__).resolve().parent / "data"
SNAPSHOT_DIR = DATA_DIR / "import" / "oracles-elixir" / "snapshots"
FILE_PATTERN = "{year}_LoL_esports_match_data_from_OraclesElixir.csv"
DOWNLOAD = "https://drive.usercontent.google.com/download"
USER_AGENT = "ManagementGame-ResearchIngestion/0.4 (research; once-per-day download)"
TERMS = ("Provided free of charge, intended for use by analysts, commentators, and fans. Game statistics are the property of "
         "Riot Games, and any usage of such data must follow Riot Games' terms and policies "
         "(https://developer.riotgames.com/policies/general). Data files are updated once per day.")
MAX_BYTES = 1024 ** 3


class Blocked(Exception):
    """The source refused the download (quota or access); nothing is stored."""


def discover(session):
    """(bundle URL, folder URL, {year: file id}) read from the official page, its bundle and the folder listing."""
    page = session.get(OFFICIAL_PAGE, timeout=60)
    page.raise_for_status()
    bundle = re.search(r'src="(/static/js/main\.[0-9a-f]+\.js)"', page.text)
    if not bundle:
        raise RuntimeError("official page no longer names its JavaScript bundle")
    bundle_url = urljoin(OFFICIAL_PAGE, bundle.group(1))
    script = session.get(bundle_url, timeout=120)
    script.raise_for_status()
    folders = sorted(set(re.findall(r"https://drive\.google\.com/drive/(?:u/\d+/)?folders/[A-Za-z0-9_-]+", script.text)))
    if len(folders) != 1:
        raise RuntimeError(f"expected one Drive folder link in the bundle, found {len(folders)}")
    folder_id = folders[0].rsplit("/", 1)[1]
    folder_url = f"https://drive.google.com/drive/folders/{folder_id}"
    listing = session.get(folder_url, timeout=60)
    listing.raise_for_status()
    files = {}
    for match in re.finditer(r'data-id="([A-Za-z0-9_-]{20,})"', listing.text):
        name = re.search(r"(20\d\d)_LoL_esports_match_data_from_OraclesElixir\.csv", listing.text[match.start():match.start() + 4000])
        if name:
            files.setdefault(int(name.group(1)), match.group(1))
    return bundle_url, folders[0], folder_url, files


def _interstitial_form(text):
    """The action URL and fields of Drive's 'download anyway' form, or None."""
    form = re.search(r'<form[^>]*id="download-form"[^>]*action="([^"]+)"[^>]*>(.*?)</form>', text, re.S)
    if not form:
        return None
    fields = dict(re.findall(r'<input[^>]*type="hidden"[^>]*name="([^"]+)"[^>]*value="([^"]*)"', form.group(2)))
    return html.unescape(form.group(1)), {k: html.unescape(v) for k, v in fields.items()}


def download(session, file_id, target):
    """Streams the file to target; returns (http status, url used, confirmation used). Raises Blocked on quota/HTML."""
    url, params, confirmed = DOWNLOAD, {"id": file_id, "export": "download"}, False
    for _ in range(2):
        with session.get(url, params=params, stream=True, timeout=300) as response:
            kind = response.headers.get("Content-Type", "")
            if "text/html" in kind:
                text = response.text
                if "Quota exceeded" in text or "Too many users have viewed or downloaded this file" in text:
                    raise Blocked("QUOTA_EXCEEDED: Drive's anonymous download quota for this file is used up; retry later")
                form = _interstitial_form(text)
                if form and not confirmed:
                    url, params, confirmed = form[0], form[1], True
                    continue
                raise Blocked(f"HTML instead of CSV (HTTP {response.status_code}); sign-in or access may be required")
            response.raise_for_status()
            size = 0
            with target.open("wb") as out:
                for chunk in response.iter_content(1024 * 1024):
                    size += len(chunk)
                    if size > MAX_BYTES:
                        raise Blocked(f"larger than {MAX_BYTES} bytes")
                    out.write(chunk)
            return response.status_code, response.url, confirmed
    raise Blocked("confirmation page repeated")


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def known_snapshots(root=SNAPSHOT_DIR):
    """SHA-256 -> relative path of every stored snapshot CSV."""
    known = {}
    for provenance in sorted(root.glob("*/provenance.json")):
        for entry in json.loads(provenance.read_text(encoding="utf-8")).get("files", []):
            if entry.get("status") == "DOWNLOADED":
                known[entry["sha256"]] = entry["stored_as"]
    return known


def header_facts(path):
    with path.open("r", encoding="utf-8-sig", errors="replace", newline="") as f:
        header = f.readline().rstrip("\r\n").split(",")
    return {"columns": len(header), "starts_with_gameid": header[:1] == ["gameid"]}


def run(years, dry_run=False, session=None, root=SNAPSHOT_DIR, delay=2.0):
    session = session or requests.Session()
    session.headers["User-Agent"] = USER_AGENT
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    bundle_url, folder_link, folder_url, files = discover(session)
    record = {"attempted_at": datetime.now(timezone.utc).isoformat(), "official_page": OFFICIAL_PAGE, "bundle": bundle_url,
              "folder_link_on_page": folder_link, "folder_listing": folder_url, "terms_shown_on_page": TERMS,
              "publisher_api_used": False, "files": []}
    known = known_snapshots(root)
    snapshot = root / stamp
    for year in years:
        entry = {"year": year, "file_name": FILE_PATTERN.format(year=year), "drive_file_id": files.get(year)}
        if entry["drive_file_id"] is None:
            entry["status"] = "NOT_LISTED"
        elif dry_run:
            entry["status"] = "DISCOVERED"
        else:
            snapshot.mkdir(parents=True, exist_ok=True)
            part = snapshot / (entry["file_name"] + ".part")
            try:
                status, used, confirmed = download(session, entry["drive_file_id"], part)
                digest = sha256(part)
                entry |= {"http_status": status, "download_url": used.split("&uuid=")[0], "confirmation_page": confirmed,
                          "bytes": part.stat().st_size, "sha256": digest, "downloaded_at": datetime.now(timezone.utc).isoformat()}
                entry |= header_facts(part)
                if digest in known:
                    part.unlink()
                    entry |= {"status": "UNCHANGED", "same_as": known[digest]}
                elif not entry["starts_with_gameid"]:
                    part.unlink()
                    entry["status"] = "NOT_A_MATCH_DATA_CSV"
                else:
                    final = snapshot / entry["file_name"]
                    part.rename(final)
                    entry |= {"status": "DOWNLOADED", "stored_as": final.relative_to(root).as_posix()}
                    known[digest] = entry["stored_as"]
            except Blocked as blocked:
                part.unlink(missing_ok=True)
                entry |= {"status": "BLOCKED", "reason": str(blocked)}
            except requests.RequestException as error:
                part.unlink(missing_ok=True)
                entry |= {"status": "FAILED", "reason": type(error).__name__}
            time.sleep(delay)
        record["files"].append(entry)
    if snapshot.exists():
        (snapshot / "provenance.json").write_text(json.dumps(record, indent=1) + "\n", encoding="utf-8", newline="\n")
        if not any(snapshot.glob("*.csv")) and all(e["status"] != "DOWNLOADED" for e in record["files"]):
            # Keep a record of blocked or unchanged attempts in one log instead of empty snapshot folders.
            log = root / "attempts.jsonl"
            with log.open("a", encoding="utf-8", newline="\n") as f:
                f.write(json.dumps(record) + "\n")
            (snapshot / "provenance.json").unlink()
            snapshot.rmdir()
    return record


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--years", type=int, nargs="+", default=[2025, 2026])
    parser.add_argument("--dry-run", action="store_true", help="discover the files, download nothing")
    args = parser.parse_args()
    SNAPSHOT_DIR.mkdir(parents=True, exist_ok=True)
    record = run(args.years, args.dry_run)
    for entry in record["files"]:
        print(entry["year"], entry["status"], entry.get("reason", ""), entry.get("stored_as", entry.get("same_as", "")))
    ok = {"DOWNLOADED", "UNCHANGED", "DISCOVERED"}
    return 0 if all(e["status"] in ok for e in record["files"]) else 3


if __name__ == "__main__":
    sys.exit(main())
