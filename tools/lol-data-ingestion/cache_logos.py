"""Gated PandaScore portrait/logo research cache; default execution is entirely offline.

Source: PandaScore. --license-approved records an existing scoped decision, not a licence.
Only API-supplied original URLs are used. No assets belong in game/distributable content.
Every original asset is keyed by (provider, entity_type, entity_id, field); never by name.
"""
import argparse
from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import hashlib
import io
import json
import math
import os
from pathlib import Path
import re
import sqlite3
import sys
import tempfile
import unicodedata
import threading
import time
from urllib.parse import urljoin, urlsplit
import warnings

import requests
from PIL import Image, PngImagePlugin, UnidentifiedImageError

DATA_DIR = Path(__file__).resolve().parent / "data"
ASSET_DIR = DATA_DIR / "assets"
ALLOWED_HOSTS = ("cdn-api.pandascore.co", "cdn.pandascore.co")
# Limits fit the largest provider originals seen in the 2026 collection (6.2 MB; 37 MP team logos; 3 MB XMP chunk).
MAX_BYTES = 8 * 1024 * 1024
MAX_PIXELS = 40_000_000
MAX_TEXT_CHUNK = 16 * 1024 * 1024
PngImagePlugin.MAX_TEXT_CHUNK = MAX_TEXT_CHUNK
MAX_FRAMES = 100
MAX_REDIRECTS = 3
SIGNATURES = (("png", "image/png", lambda b: b.startswith(b"\x89PNG\r\n\x1a\n")),
              ("jpg", "image/jpeg", lambda b: b.startswith(b"\xff\xd8\xff")),
              ("gif", "image/gif", lambda b: b[:6] in (b"GIF87a", b"GIF89a")),
              ("webp", "image/webp", lambda b: b[:4] == b"RIFF" and b[8:12] == b"WEBP"))
GENERIC_TYPES = ("application/octet-stream", "binary/octet-stream")
# (directory, suffix). Files are named after the entity (player nickname, team, league), not its provider ID.
TARGETS = {("player", "image_url"): ("players", ""), ("team", "image_url"): ("team-logos", ""),
           ("team", "dark_mode_image_url"): ("team-logos", "-dark"), ("league", "image_url"): ("league-logos", "")}
RESERVED_NAMES = {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))}
TABLES = {"player": ("ps_players", "player_id"), "team": ("ps_teams", "team_id"),
          "league": ("ps_leagues", "league_id")}
REVIEW = "LICENSE_REVIEW_REQUIRED"
APPROVED = "APPROVAL_RECORDED_INTERNAL_RESEARCH"
STATUSES = ("AVAILABLE", "CACHED", "MISSING_URL", "DOWNLOAD_FAILED", "INVALID_IMAGE", REVIEW)
# Preserve the existing primary key and columns; additive migration below. This table is PandaScore-only.
SCHEMA = """CREATE TABLE IF NOT EXISTS image_cache (
    entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, field TEXT NOT NULL, variant TEXT NOT NULL,
    source_url TEXT NOT NULL, requested_url TEXT NOT NULL, status TEXT NOT NULL, local_path TEXT, content_type TEXT,
    format TEXT, bytes INTEGER, sha256 TEXT, error TEXT, attempted_at TEXT NOT NULL, license_note TEXT NOT NULL,
    PRIMARY KEY (entity_type, entity_id, field, variant))"""
ADDITIONS = {"provider": "TEXT NOT NULL DEFAULT 'pandascore'", "use_status": "TEXT",
             "cached_at": "TEXT", "validated_at": "TEXT", "source_run": "TEXT", "observed_at": "TEXT",
             "retryable": "INTEGER NOT NULL DEFAULT 0"}


def now_iso():
    return datetime.now(timezone.utc).isoformat()


class ImageError(Exception):
    def __init__(self, message, status="DOWNLOAD_FAILED", retryable=False):
        super().__init__(message)
        self.status, self.retryable = status, retryable


class Transient(Exception):
    pass


def trusted(url):
    try:
        parts = urlsplit(url)
        return (parts.scheme == "https" and parts.hostname in ALLOWED_HOSTS and parts.port in (None, 443)
                and not parts.username and not parts.password and not parts.fragment
                and not any(c.isspace() or ord(c) < 32 for c in url) and "\\" not in url)
    except (ValueError, TypeError):
        return False


def variant_url(url, entity, field, variant):
    """Never construct unverified thumb_/normal_ URLs, including on the documented host."""
    return url if variant == "original" else None


def sniff(body):
    for extension, mime, test in SIGNATURES:
        if test(body):
            return extension, mime
    return None, None


def validate_image(body, content_type=""):
    extension, mime = sniff(body)
    if not body or len(body) > MAX_BYTES or not extension:
        raise ImageError("not a supported bounded PNG/JPEG/GIF/WebP image", "INVALID_IMAGE")
    if content_type and content_type != mime and content_type not in GENERIC_TYPES:
        raise ImageError(f"content type {content_type} does not match {mime} bytes", "INVALID_IMAGE")
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("error", Image.DecompressionBombWarning)
            with Image.open(io.BytesIO(body), formats=["PNG", "JPEG", "GIF", "WEBP"]) as im:
                if im.width * im.height > MAX_PIXELS:
                    raise ImageError("image exceeds pixel limit", "INVALID_IMAGE")
                im.verify()
            # verify() alone does not decode pixel data for every format. Bound all animation frames too.
            with Image.open(io.BytesIO(body), formats=["PNG", "JPEG", "GIF", "WEBP"]) as im:
                frames = getattr(im, "n_frames", 1)
                if frames > MAX_FRAMES or frames * im.width * im.height > MAX_PIXELS:
                    raise ImageError("image exceeds frame/pixel budget", "INVALID_IMAGE")
                for frame in range(frames):
                    im.seek(frame)
                    im.load()
    except (OSError, ValueError, SyntaxError, EOFError, UnidentifiedImageError,
            Image.DecompressionBombWarning, Image.DecompressionBombError) as exc:
        raise ImageError(f"corrupt image: {type(exc).__name__}", "INVALID_IMAGE") from exc
    return extension, mime


def fetch_image(session, url, sleep=time.sleep, retries=3, max_bytes=MAX_BYTES, metrics=None, before_request=None):
    for attempt in range(retries + 1):
        try:
            return fetch_once(session, url, max_bytes, metrics, before_request)
        except Transient as exc:
            if attempt == retries:
                raise ImageError(f"retries exhausted: {exc}", retryable=True) from exc
            if metrics is not None:
                metrics["retries"] += 1
            delay = getattr(exc, "retry_after", None)
            sleep(min(max(0, delay if delay is not None else 2 ** attempt * 2), 60))


def fetch_once(session, url, max_bytes, metrics=None, before_request=None):
    current = url
    for _ in range(MAX_REDIRECTS + 1):
        if not trusted(current):
            raise ImageError("untrusted URL host, credentials, port or scheme")
        if before_request:
            before_request()
        if metrics is not None:
            metrics["http_requests"] += 1
        response = None
        try:
            response = session.get(current, stream=True, allow_redirects=False, timeout=(10, 30))
            if response.status_code in (301, 302, 303, 307, 308):
                location = response.headers.get("Location")
                if not location:
                    raise ImageError("redirect without Location")
                current = urljoin(current, location)
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
                raise ImageError(f"declared size over {max_bytes}", "INVALID_IMAGE")
            body = bytearray()
            for chunk in response.iter_content(65536):
                body.extend(chunk)
                if metrics is not None:
                    metrics["response_bytes"] += len(chunk)
                if len(body) > max_bytes:
                    raise ImageError(f"body over {max_bytes} bytes", "INVALID_IMAGE")
            content_type = (response.headers.get("Content-Type") or "").split(";")[0].strip().lower()
            extension, mime = validate_image(bytes(body), content_type)
            return bytes(body), mime, extension
        except (requests.Timeout, requests.ConnectionError, requests.exceptions.ChunkedEncodingError,
                requests.exceptions.ContentDecodingError) as exc:
            raise Transient(type(exc).__name__) from exc
        except requests.RequestException as exc:
            raise ImageError(type(exc).__name__) from exc
        finally:
            if response is not None:
                response.close()
    raise ImageError("too many redirects")


def clean_name(value):
    """Readable, Windows-safe file stem that keeps the entity's own name (Unicode letters stay)."""
    text = unicodedata.normalize("NFC", str(value or ""))
    text = re.sub(r'[<>:"/\\|?*\x00-\x1f]', " ", text)
    text = re.sub(r"\s+", "_", text.strip()).strip(" ._-")[:80].rstrip(" ._-")
    return text + "_" if text.split(".")[0].upper() in RESERVED_NAMES else text


def asset_stems(db):
    """(entity, id) -> file stem. Names shared by several entities of one kind get '__<id>' on every member,
    so no entity silently owns the plain name and a later duplicate never renames a different image."""
    stems = {}
    for entity, (table, id_field) in TABLES.items():
        if not has_table(db, table):
            continue
        records = [(str(r[id_field]), clean_name(r.get("name")) or f"{entity}-{r[id_field]}")
                   for r in read_rows(db, table)]
        counts = Counter(name.casefold() for _, name in records)
        for entity_id, name in records:
            stems[(entity, entity_id)] = name if counts[name.casefold()] == 1 else f"{name}__{entity_id}"
    return stems


def target_stem(entity, field, entity_id, stems):
    directory, suffix = TARGETS[(entity, field)]
    return f"{directory}/{stems.get((entity, str(entity_id)), f'{entity}-{entity_id}')}{suffix}"


def safe_path(asset_dir, relative):
    root = Path(asset_dir).resolve()
    path = root / relative
    if Path(relative).is_absolute() or "\\" in relative or not path.resolve().is_relative_to(root):
        raise ImageError("cache path escapes asset directory", "INVALID_IMAGE")
    return path


def atomic_write(path, body):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=path.parent, prefix=path.name + ".", suffix=".tmp", delete=False) as f:
            temporary = Path(f.name)
            f.write(body)
            f.flush()
            os.fsync(f.fileno())
        os.replace(temporary, path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def write_json(path, value):
    atomic_write(path, (json.dumps(value, indent=1, ensure_ascii=False) + "\n").encode("utf-8"))


def store(asset_dir, relative, body):
    digest = hashlib.sha256(body).hexdigest()
    path = safe_path(asset_dir, relative)
    if path.is_file() and path.stat().st_size <= MAX_BYTES and hashlib.sha256(path.read_bytes()).hexdigest() == digest:
        return "unchanged", path, digest
    atomic_write(path, body)
    return "cached", path, digest


def ensure_schema(db):
    db.execute(SCHEMA)
    columns = {r[1] for r in db.execute("PRAGMA table_info(image_cache)")}
    for name, declaration in ADDITIONS.items():
        if name not in columns:
            db.execute(f"ALTER TABLE image_cache ADD COLUMN {name} {declaration}")
    db.execute("UPDATE image_cache SET status='CACHED', cached_at=coalesce(cached_at, attempted_at) WHERE status IN ('cached','unchanged')")
    db.execute("UPDATE image_cache SET status='DOWNLOAD_FAILED' WHERE status='failed'")
    db.commit()


def read_rows(db, table):
    cursor = db.execute(f"SELECT * FROM {table}")
    keys = [c[0] for c in cursor.description]
    return [dict(zip(keys, row)) for row in cursor]


def has_table(db, table):
    return db.execute("SELECT 1 FROM sqlite_master WHERE type='table' AND name=?", (table,)).fetchone() is not None


def references(db):
    refs = {(r["entity_type"], r["entity_id"], r["field"]): r for r in read_rows(db, "ps_image_refs")
            if (r["entity_type"], r["field"]) in TARGETS}
    for entity, (table, id_field) in TABLES.items():
        if not has_table(db, table):
            continue
        for record in read_rows(db, table):
            for ent, field in TARGETS:
                if ent == entity:
                    key = (entity, record[id_field], field)
                    refs.setdefault(key, {"entity_type": entity, "entity_id": record[id_field], "field": field,
                                          "url_state": "absent_reference", "url": None,
                                          "source_run": record.get("source_run"), "observed_at": record.get("observed_at")})
    return [refs[k] for k in sorted(refs)]


def verify_cached(asset_dir, row, stems):
    """Reject a valid image belonging to a different ID just as strictly as corrupted bytes."""
    try:
        expected = target_stem(row["entity_type"], row["field"], row["entity_id"], stems)
        if row.get("provider") != "pandascore" or row.get("variant") != "original":
            return "wrong provider or variant"
        if row.get("format") not in {s[0] for s in SIGNATURES} or row.get("local_path") != f"{expected}.{row['format']}":
            return "path does not belong to entity and field"
        path = safe_path(asset_dir, row["local_path"])
        if not path.is_file() or path.stat().st_size > MAX_BYTES:
            return "file missing or exceeds size limit"
        body = path.read_bytes()
        if hashlib.sha256(body).hexdigest() != row.get("sha256") or len(body) != row.get("bytes"):
            return "hash or size mismatch"
        extension, _ = validate_image(body, row.get("content_type") or "")
        if extension != row["format"]:
            return "format mismatch"
    except (OSError, KeyError, ImageError) as exc:
        return str(exc)
    return None


def save_row(db, row):
    columns = [r[1] for r in db.execute("PRAGMA table_info(image_cache)")]
    db.execute(f"INSERT OR REPLACE INTO image_cache ({','.join(columns)}) VALUES ({','.join('?' for _ in columns)})",
               [row.get(c) for c in columns])


def relocate_cached(db, asset_dir, stems):
    """Move verified-cache files to their name-based paths (idempotent; also follows upstream renames)."""
    moves, in_place = [], set()
    for row in read_rows(db, "image_cache"):
        key = (row["entity_type"], row["field"])
        if (row["status"] != "CACHED" or not row.get("local_path") or key not in TARGETS
                or row.get("format") not in {s[0] for s in SIGNATURES} or row.get("variant") != "original"):
            continue
        wanted = f"{target_stem(key[0], key[1], row['entity_id'], stems)}.{row['format']}"
        if wanted == row["local_path"]:
            in_place.add(wanted)
        else:
            moves.append((row, wanted))
    # A path another entity already owns is never taken over; verification rejects the mismatch instead.
    moves = [(row, wanted) for row, wanted in moves if row["local_path"] not in in_place]
    if not moves:
        return 0
    sources = {row["local_path"] for row, _ in moves}
    staged, done = [], 0
    for row, wanted in moves:
        source, target = safe_path(asset_dir, row["local_path"]), safe_path(asset_dir, wanted)
        if not source.is_file():
            continue  # verification below reports the missing file
        if target.exists() and wanted not in sources:
            continue  # an unrelated file owns the name; verification below reports the mismatch
        if wanted in sources:  # chain or swap: park the file first
            parked = source.with_name(f"{source.name}.{os.getpid()}.moving")
            os.replace(source, parked)
            staged.append((row, wanted, parked))
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        os.replace(source, target)
        staged.append((row, wanted, None))
    for row, wanted, parked in staged:
        if parked is not None:
            target = safe_path(asset_dir, wanted)
            target.parent.mkdir(parents=True, exist_ok=True)
            os.replace(parked, target)
        db.execute("UPDATE image_cache SET local_path=? WHERE entity_type=? AND entity_id=? AND field=? AND variant=?",
                   (wanted, row["entity_type"], row["entity_id"], row["field"], row["variant"]))
        done += 1
    db.commit()
    return done


def inventory(db, asset_dir):
    stems = asset_stems(db)
    relocate_cached(db, asset_dir, stems)
    catalogs = {entity: {r[id_field]: r for r in read_rows(db, table)}
                for entity, (table, id_field) in TABLES.items() if has_table(db, table)}
    known = {(r["entity_type"], r["entity_id"], r["field"]): r for r in read_rows(db, "image_cache")
             if r["variant"] == "original" and r["provider"] == "pandascore"}
    entries = []
    for ref in references(db):
        key = (ref["entity_type"], ref["entity_id"], ref["field"])
        url = ref.get("url") if ref["url_state"] == "value" and ref.get("url") else ""
        old = known.get(key, {})
        row = dict(old) if old.get("source_url") == url else {}
        row.update(provider="pandascore", entity_type=key[0], entity_id=key[1], field=key[2], variant="original",
                   source_url=url, requested_url=url, source_run=ref.get("source_run"), observed_at=ref.get("observed_at"))
        row.setdefault("license_note", "")
        row.setdefault("attempted_at", "")
        row.setdefault("retryable", 0)
        row["use_status"] = APPROVED if row["license_note"].strip() else REVIEW
        if not url:
            row.update(status="MISSING_URL", local_path=None, sha256=None, error=None)
        elif key[1] not in catalogs.get(key[0], {}):
            row.update(status="DOWNLOAD_FAILED", local_path=None, sha256=None, error="image entity missing from catalog")
        elif not re.fullmatch(r"[1-9][0-9]*", key[1]) or not trusted(url):
            row.update(status="DOWNLOAD_FAILED", local_path=None, sha256=None, error="invalid identity or untrusted source URL")
        elif row.get("status") == "CACHED":
            problem = ("requested URL differs from original source" if old.get("requested_url") != url
                       else verify_cached(asset_dir, row, stems))
            row["validated_at"] = now_iso()
            if problem:
                row.update(status="INVALID_IMAGE", error=f"cache verification: {problem}", retryable=1)
        elif row.get("status") not in ("DOWNLOAD_FAILED", "INVALID_IMAGE"):
            row["status"] = "AVAILABLE" if row["license_note"].strip() else REVIEW
        save_row(db, row)
        row["url_state"] = ref["url_state"]
        row["asset_identity"] = f"pandascore:{key[0]}:{key[1]}:{key[2]}:original"
        record = catalogs.get(key[0], {}).get(key[1], {})
        row["provenance"] = {k: record.get(k) for k in
                             ("source_run", "source_query", "source_file", "observed_at", "payload_sha256")}
        row["reference_license_status"] = ref.get("license_status")
        # Never expose an unverified or stale local path to consumers.
        if row["status"] != "CACHED":
            row.update(local_path=None, sha256=None, content_type=None, format=None, bytes=None, cached_at=None)
        entries.append(row)
    db.commit()
    return entries


def export_assets(db, asset_dir, summary):
    from image_mappings import build_mappings
    entries = inventory(db, asset_dir)
    meta = {"provider": "pandascore", "source": "Source: PandaScore", "version": 2,
            "scope": "Internal research only; approval record is not proof of licence. No public/game distribution.",
            "generated_at": now_iso()}
    manifest = {**meta, "entries": entries}
    write_json(asset_dir / "image_manifest.json", manifest)
    mappings, coverage = build_mappings(db, entries)
    for name, rows in mappings.items():
        write_json(asset_dir / "mapping" / name, {**meta, "entries": rows})
    coverage["last_run"] = dict(summary)
    write_json(asset_dir / "image_coverage.json", coverage)
    return coverage


def run(db_path, asset_dir, license_note=None, variant="original", include_dark=False, delay=1.0, limit=None,
        refresh=False, session=None, sleep=time.sleep, *, entities=("player", "team", "league"),
        dry_run=False, pilot=False, batch_size=100, retry_failed=False, workers=1):
    if variant != "original":
        raise ValueError("Only original API URLs are supported")
    if not math.isfinite(delay) or delay < 0.1 or batch_size < 1 or (limit is not None and limit < 1) or not 1 <= workers <= 8:
        raise ValueError("delay >= 0.1, batch_size >= 1, limit >= 1 and 1 <= workers <= 8 required")
    if not entities or not set(entities) <= TABLES.keys():
        raise ValueError("unknown or empty entity selection")
    db_path, asset_dir = Path(db_path), Path(asset_dir)
    # Never silently create a different/empty staging database on a typo.
    db = sqlite3.connect(db_path.resolve().as_uri() + "?mode=rw", uri=True)
    start = time.monotonic()
    summary = {k: 0 for k in ("candidates", "requested", "http_requests", "response_bytes", "downloaded_bytes",
                              "cached", "unchanged", "skipped_known", "skipped_failed", "failed", "retries", "planned")}
    note = (license_note or "").strip()
    offline = dry_run or not note
    summary.update(mode="dry-run" if offline else "pilot" if pilot else "download", started_at=now_iso(),
                   license_status=APPROVED if note else REVIEW, interrupted=False, batch_size=batch_size,
                   workers=workers, request_delay_seconds=delay)
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    previous_request = None
    request_lock = threading.Lock()
    local = threading.local()
    owned_sessions = []
    executor = None

    def throttle():
        nonlocal previous_request
        with request_lock:
            if previous_request is not None:
                sleep(max(0, delay - (time.monotonic() - previous_request)))
            previous_request = time.monotonic()

    def download(entry):
        # Each worker has its own connection pool. Only the main thread writes SQLite/files.
        if session is not None:
            client = session
        else:
            if not hasattr(local, "session"):
                local.session = requests.Session()
                local.session.trust_env = False
                local.session.headers.update({"User-Agent": "ManagementGame-ResearchIngestion/0.2 (image cache)"})
                owned_sessions.append(local.session)
            client = local.session
        metrics = {"http_requests": 0, "response_bytes": 0, "retries": 0}
        try:
            payload = fetch_image(client, entry["source_url"], sleep, metrics=metrics, before_request=throttle)
        except (ImageError, OSError) as exc:
            payload = exc
        return payload, metrics

    def checkpoint():
        summary["duration_seconds"] = round(time.monotonic() - start, 3)
        write_json(asset_dir / "runs" / f"{run_id}.json", summary)

    try:
        ensure_schema(db)
        entries = inventory(db, asset_dir)
        stems = asset_stems(db)
        counts = Counter()
        pending = []
        for entry in entries:
            entity, field = entry["entity_type"], entry["field"]
            if entity not in entities or (field == "dark_mode_image_url" and not include_dark):
                continue
            if pilot and (entity not in ("player", "team") or field != "image_url" or counts[entity] >= 20):
                continue
            if not entry["source_url"]:
                continue
            summary["candidates"] += 1
            if entry.get("error") in ("image entity missing from catalog", "invalid identity or untrusted source URL"):
                summary["skipped_failed"] += 1
                continue
            if entry["status"] == "CACHED" and not refresh:
                summary["skipped_known"] += 1
                continue
            refetch_corruption = str(entry.get("error", "")).startswith("cache verification:")
            if entry["status"] in ("DOWNLOAD_FAILED", "INVALID_IMAGE") and not (retry_failed or refetch_corruption):
                summary["skipped_failed"] += 1
                continue
            if note and entry["status"] == REVIEW:
                entry.update(status="AVAILABLE", license_note=note, use_status=APPROVED)
                save_row(db, entry)
                db.commit()
            if limit is not None and summary["planned"] >= limit:
                continue
            counts[entity] += 1
            summary["planned"] += 1
            if offline:
                continue
            pending.append(entry)
        if pending and workers > 1:
            executor = ThreadPoolExecutor(max_workers=workers)

        def completed_downloads():
            for offset in range(0, len(pending), batch_size):
                batch = pending[offset:offset + batch_size]
                results = executor.map(download, batch) if executor else map(download, batch)
                yield from zip(batch, results)

        for entry, (payload, metrics) in completed_downloads():
            entity, field = entry["entity_type"], entry["field"]
            entry.update(license_note=note, use_status=APPROVED, attempted_at=now_iso())
            summary["requested"] += 1
            for key, value in metrics.items():
                summary[key] += value
            try:
                if isinstance(payload, Exception):
                    raise payload
                body, mime, extension = payload
                target = target_stem(entity, field, entry["entity_id"], stems)
                result, path, digest = store(asset_dir, f"{target}.{extension}", body)
                entry.update(status="CACHED", local_path=path.relative_to(asset_dir.resolve()).as_posix(),
                             content_type=mime, format=extension, bytes=len(body), sha256=digest, error=None,
                             cached_at=now_iso(), validated_at=now_iso(), retryable=0)
                # Remove only this entity/field's obsolete format after its replacement has been saved.
                for ext, _, _ in SIGNATURES:
                    if ext != extension:
                        safe_path(asset_dir, f"{target}.{ext}").unlink(missing_ok=True)
                summary[result] += 1
                summary["downloaded_bytes"] += len(body)
            except (ImageError, OSError) as exc:
                entry.update(status=getattr(exc, "status", "DOWNLOAD_FAILED"), error=str(exc), local_path=None,
                             sha256=None, content_type=None, format=None, bytes=None, cached_at=None,
                             retryable=int(getattr(exc, "retryable", isinstance(exc, OSError))))
                summary["failed"] += 1
            save_row(db, entry)
            db.commit()  # the database is the per-asset resume checkpoint
            print(f"{entity} {entry['entity_id']} {field}: {entry['status']}", flush=True)
            if summary["requested"] % batch_size == 0:
                checkpoint()
        checkpoint()
    except KeyboardInterrupt:
        summary["interrupted"] = True
        checkpoint()
    finally:
        try:
            if executor is not None:
                executor.shutdown(wait=True, cancel_futures=True)
            if has_table(db, "image_cache"):
                summary["duration_seconds"] = round(time.monotonic() - start, 3)
                coverage = export_assets(db, asset_dir, summary)
                checkpoint()  # include verification/export time in the final invocation duration
                coverage["last_run"] = dict(summary)
                write_json(asset_dir / "image_coverage.json", coverage)
                summary["coverage"] = coverage
        finally:
            db.close()
            for client in owned_sessions:
                client.close()
    return summary


def report_only(db_path):
    with sqlite3.connect(Path(db_path).resolve().as_uri() + "?mode=ro", uri=True) as db:
        for row in db.execute("SELECT entity_type, field, url_state, host, count(*) FROM ps_image_refs GROUP BY 1,2,3,4"):
            print(row)
    print("LICENSE_REVIEW_REQUIRED: no CDN request made")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--asset-dir", type=Path, default=ASSET_DIR)
    parser.add_argument("--license-approved", metavar="NOTE", help="existing approver, scope, date and evidence reference; not proof of licence")
    parser.add_argument("--variant", choices=("original",), default="original")
    parser.add_argument("--entities", nargs="+", choices=tuple(TABLES), default=list(TABLES))
    parser.add_argument("--include-dark", action="store_true")
    parser.add_argument("--delay", type=float, default=1.0)
    parser.add_argument("--limit", type=int, help="maximum pending assets per invocation; cache hits do not consume the budget")
    parser.add_argument("--batch-size", type=int, default=100)
    parser.add_argument("--workers", type=int, default=1, help="1–8 download workers sharing one global request delay")
    parser.add_argument("--pilot", action="store_true", help="up to 20 pending portraits and 20 regular team logos")
    parser.add_argument("--dry-run", action="store_true", help="validate cache and rebuild all mappings without network")
    parser.add_argument("--refresh", action="store_true")
    parser.add_argument("--retry-failed", action="store_true", help="explicitly retry previous failures once with bounded retries")
    args = parser.parse_args()
    try:
        summary = run(args.db, args.asset_dir, args.license_approved, args.variant, args.include_dark, args.delay,
                      args.limit, args.refresh, entities=args.entities, dry_run=args.dry_run, pilot=args.pilot,
                      batch_size=args.batch_size, retry_failed=args.retry_failed, workers=args.workers)
    except ValueError as exc:
        parser.error(str(exc))
    print(json.dumps(summary, indent=1))
    return 130 if summary["interrupted"] else 0


if __name__ == "__main__":
    sys.exit(main())
