"""Controlled local cache of team and league logos (research only; git-ignored data/assets/).

Off by default. PandaScore's terms (6.2-6.4) keep the database PandaScore's and forbid passing on raw data, and they
say nothing about team or league logos, which are third parties' marks and artwork. A public CDN URL is not a licence.
Until someone with authority records a licence decision, ps_image_refs rows stay LICENSE_REVIEW_REQUIRED and this
script only reports what it would fetch; it opens no connection.

With --license-approved "<who, what scope, date>" it caches one image per entity and field:
  - only https URLs on the provider's CDN hosts (ALLOWED_HOSTS); redirects are followed by hand, and only to them;
  - at most MAX_BYTES, streamed; the bytes must be PNG, JPEG, GIF or WebP by signature. SVG, HTML and anything else
    are rejected; nothing downloaded is executed, rendered or decoded here;
  - files are named by entity id (team-logos/<id>.<ext>, league-logos/<id>[-dark].<ext>), in the format received;
  - an entity already cached from the same URL with a matching file is skipped without a request (--refresh to
    re-check); identical bytes are never rewritten;
  - timeouts, 429 and 5xx retry with bounded backoff, honouring Retry-After up to 60 s; one request per --delay s.
Variants: `original` (default) is the URL exactly as supplied. `thumb_`/`normal_` prefixes are documented for team
logos on cdn.pandascore.co, while the API returns cdn-api.pandascore.co URLs, so they are unverified for these
hosts; --variant thumb|normal builds them for team image_url only and records that they were constructed.
Results: table image_cache in data/staging.sqlite3 and data/assets/logo_manifest.json. Never copy these files into
the game, a content pack or the repository.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import sqlite3
import sys
import time
from urllib.parse import urljoin, urlsplit

import requests

DATA_DIR = Path(__file__).resolve().parent / "data"
ASSET_DIR = DATA_DIR / "assets"
ALLOWED_HOSTS = ("cdn-api.pandascore.co", "cdn.pandascore.co")
MAX_BYTES = 5 * 1024 * 1024
MAX_REDIRECTS = 3
SIGNATURES = (("png", "image/png", lambda b: b.startswith(b"\x89PNG\r\n\x1a\n")),
              ("jpg", "image/jpeg", lambda b: b.startswith(b"\xff\xd8\xff")),
              ("gif", "image/gif", lambda b: b[:6] in (b"GIF87a", b"GIF89a")),
              ("webp", "image/webp", lambda b: b[:4] == b"RIFF" and b[8:12] == b"WEBP"))
GENERIC_TYPES = ("application/octet-stream", "binary/octet-stream")
TEAM_IMAGE_PATH = re.compile(r"^/images/team/image/\d+/[^/]+$")
TARGETS = {("team", "image_url"): "team-logos/{id}", ("team", "dark_mode_image_url"): "team-logos/{id}-dark",
           ("league", "image_url"): "league-logos/{id}"}
SCHEMA = """CREATE TABLE IF NOT EXISTS image_cache (
    entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, field TEXT NOT NULL, variant TEXT NOT NULL,
    source_url TEXT NOT NULL, requested_url TEXT NOT NULL, status TEXT NOT NULL, local_path TEXT, content_type TEXT,
    format TEXT, bytes INTEGER, sha256 TEXT, error TEXT, attempted_at TEXT NOT NULL, license_note TEXT NOT NULL,
    PRIMARY KEY (entity_type, entity_id, field, variant))"""


class ImageError(Exception):
    """A permanent rejection (untrusted host, not an image, too large) or retries exhausted."""


class Transient(Exception):
    pass


def trusted(url):
    parts = urlsplit(url)
    return parts.scheme == "https" and parts.hostname in ALLOWED_HOSTS and parts.port in (None, 443)


def variant_url(url, entity, field, variant):
    """The URL to request. thumb/normal only for team image_url on the documented path shape; else None."""
    if variant == "original":
        return url
    parts = urlsplit(url)
    if entity != "team" or field != "image_url" or not TEAM_IMAGE_PATH.match(parts.path):
        return None
    head, name = parts.path.rsplit("/", 1)
    return f"{parts.scheme}://{parts.netloc}{head}/{variant}_{name}"


def sniff(body):
    for extension, mime, test in SIGNATURES:
        if test(body):
            return extension, mime
    return None, None


def fetch_image(session, url, sleep=time.sleep, retries=3, max_bytes=MAX_BYTES):
    """(bytes, content type, format) of one image. Raises ImageError on anything that is not a trusted image."""
    for attempt in range(retries + 1):
        try:
            return fetch_once(session, url, max_bytes)
        except Transient as exc:
            if attempt == retries:
                raise ImageError(f"retries exhausted: {exc}")
            delay = getattr(exc, "retry_after", None) or min(2 ** attempt * 2, 30)
            sleep(min(delay, 60))


def fetch_once(session, url, max_bytes):
    current = url
    for _ in range(MAX_REDIRECTS + 1):
        if not trusted(current):
            raise ImageError(f"untrusted URL host or scheme: {urlsplit(current).hostname}")
        try:
            response = session.get(current, stream=True, allow_redirects=False, timeout=30)
        except (requests.Timeout, requests.ConnectionError) as exc:
            raise Transient(type(exc).__name__)
        try:
            if response.status_code in (301, 302, 303, 307, 308):
                current = urljoin(current, response.headers.get("Location", ""))
                continue
            if response.status_code == 429 or 500 <= response.status_code < 600:
                error = Transient(f"HTTP {response.status_code}")
                try:
                    error.retry_after = int(response.headers.get("Retry-After", ""))
                except ValueError:
                    pass
                raise error
            if response.status_code != 200:
                raise ImageError(f"HTTP {response.status_code}")
            declared = response.headers.get("Content-Length")
            if declared and declared.isdigit() and int(declared) > max_bytes:
                raise ImageError(f"declared size {declared} over {max_bytes}")
            body = b""
            for chunk in response.iter_content(65536):
                body += chunk
                if len(body) > max_bytes:
                    raise ImageError(f"body over {max_bytes} bytes")
            content_type = (response.headers.get("Content-Type") or "").split(";")[0].strip().lower()
            extension, mime = sniff(body)
            if extension is None:
                raise ImageError(f"not a PNG/JPEG/GIF/WebP image (content type {content_type or 'none'})")
            if content_type and content_type != mime and content_type not in GENERIC_TYPES:
                raise ImageError(f"content type {content_type} does not match {mime} bytes")
            return body, content_type, extension
        finally:
            response.close()
    raise ImageError("too many redirects")


def store(asset_dir, relative, body):
    """Writes the file unless identical bytes are already there. Returns ('cached'|'unchanged', path, sha256)."""
    digest = hashlib.sha256(body).hexdigest()
    path = asset_dir / relative
    if path.exists() and hashlib.sha256(path.read_bytes()).hexdigest() == digest:
        return "unchanged", path, digest
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".tmp")
    temp.write_bytes(body)
    os.replace(temp, path)
    return "cached", path, digest


def candidates(db):
    """(entity, id, field, url) for every team and league logo URL the provider supplied, in id order."""
    return db.execute("""SELECT entity_type, entity_id, field, url FROM ps_image_refs
                         WHERE url_state = 'value' AND entity_type IN ('team', 'league')
                         ORDER BY entity_type, CAST(entity_id AS INTEGER), field""").fetchall()


def run(db_path, asset_dir, license_note, variant="original", include_dark=False, delay=1.0, limit=None,
        refresh=False, session=None, sleep=time.sleep):
    db = sqlite3.connect(db_path)
    db.execute(SCHEMA)
    todo = [c for c in candidates(db) if include_dark or c[2] == "image_url"]
    if limit is not None:
        todo = todo[:limit]
    summary = {"candidates": len(todo), "requested": 0, "cached": 0, "unchanged": 0, "skipped_known": 0,
               "failed": 0, "no_variant": 0}
    session = session or requests.Session()
    session.headers.update({"User-Agent": "ManagementGame-ResearchIngestion/0.1 (logo cache)"})
    first = True
    for entity, entity_id, field, url in todo:
        target = TARGETS[(entity, field)].format(id=int(entity_id))
        requested = variant_url(url, entity, field, variant)
        now = datetime.now(timezone.utc).isoformat()
        if requested is None:
            summary["no_variant"] += 1
            continue
        known = db.execute("""SELECT local_path, sha256 FROM image_cache WHERE entity_type=? AND entity_id=? AND field=?
                              AND variant=? AND requested_url=? AND status IN ('cached','unchanged')""",
                           (entity, entity_id, field, variant, requested)).fetchone()
        if known and not refresh and (asset_dir / known[0]).exists() and \
                hashlib.sha256((asset_dir / known[0]).read_bytes()).hexdigest() == known[1]:
            summary["skipped_known"] += 1
            continue
        if not first:
            sleep(delay)
        first = False
        summary["requested"] += 1
        try:
            body, content_type, extension = fetch_image(session, requested, sleep)
            status, path, digest = store(asset_dir, f"{target}.{extension}", body)
            row = (status, path.relative_to(asset_dir).as_posix(), content_type, extension, len(body), digest, None)
        except ImageError as exc:
            status, row = "failed", ("failed", None, None, None, None, None, str(exc))
        summary[status] += 1
        db.execute("INSERT OR REPLACE INTO image_cache VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                   (entity, entity_id, field, variant, url, requested, *row, now, license_note))
        db.commit()
        print(f"{entity} {entity_id} {field}: {status}", flush=True)
    rows = db.execute("""SELECT entity_type, entity_id, field, variant, source_url, requested_url, status, local_path,
                                format, bytes, sha256, error FROM image_cache ORDER BY 1, CAST(entity_id AS INTEGER), 3, 4""").fetchall()
    db.close()
    keys = ("entity_type", "entity_id", "field", "variant", "source_url", "requested_url", "status", "local_path",
            "format", "bytes", "sha256", "error")
    asset_dir.mkdir(parents=True, exist_ok=True)
    (asset_dir / "logo_manifest.json").write_text(json.dumps(
        {"license_note": license_note, "variant": variant, "constructed_variant": variant != "original",
         "note": "Research cache only. Third-party marks: never copy into the game, a content pack or the repository.",
         "entries": [dict(zip(keys, r)) for r in rows]}, indent=1), encoding="utf-8")
    return summary


def report_only(db_path):
    db = sqlite3.connect(Path(db_path).resolve().as_uri() + "?mode=ro", uri=True)
    rows = db.execute("""SELECT entity_type, field, url_state, host, license_status, count(*) FROM ps_image_refs
                         WHERE entity_type IN ('team', 'league') GROUP BY 1, 2, 3, 4, 5 ORDER BY 1, 2, 3, 4""").fetchall()
    db.close()
    print("LICENSE_REVIEW_REQUIRED: no logo was fetched (no --license-approved). Image URLs by state:")
    for entity, field, state, host, licence, count in rows:
        print(f"  {entity} {field} {state} {host or '-'} {licence or '-'}: {count}")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--asset-dir", type=Path, default=ASSET_DIR)
    parser.add_argument("--license-approved", metavar="NOTE",
                        help="who approved internal research caching, for what scope, and when; without it nothing is fetched")
    parser.add_argument("--variant", choices=("original", "thumb", "normal"), default="original")
    parser.add_argument("--include-dark", action="store_true", help="also cache dark_mode_image_url")
    parser.add_argument("--delay", type=float, default=1.0)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--refresh", action="store_true", help="re-request entities already cached from the same URL")
    args = parser.parse_args()
    if not args.license_approved or not args.license_approved.strip():
        report_only(args.db)
        return 0
    if args.delay < 0.5:
        parser.error("--delay must be at least 0.5 seconds")
    summary = run(args.db, args.asset_dir, args.license_approved.strip(), args.variant, args.include_dark, args.delay,
                  args.limit, args.refresh)
    print(json.dumps(summary))
    return 0


if __name__ == "__main__":
    sys.exit(main())
