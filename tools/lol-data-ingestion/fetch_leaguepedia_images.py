"""Fetch 2026 player portraits from Leaguepedia (lol.fandom.com) and map them to PandaScore player IDs.

Source: Leaguepedia (Fandom). Needs LEAGUEPEDIA_BOT_USERNAME / LEAGUEPEDIA_BOT_PASSWORD (a Special:BotPasswords
login) in the environment or .env; the values are never printed or written to any output.
Images stay Git-ignored under data/assets/players-2026/ and remain the property of their rightsholders;
this is an internal research cache, not a distribution licence.

  python fetch_leaguepedia_images.py --dry-run     # query + match only, nothing downloaded
  python fetch_leaguepedia_images.py               # download the selected 2026 portraits
"""
import argparse
from collections import defaultdict
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import sqlite3
import sys
import time
from urllib.parse import urlsplit

from dotenv import dotenv_values
import requests

import cache_logos as cache

API = "https://lol.fandom.com/api.php"
HOST_SUFFIXES = (".wikia.nocookie.net", ".fandom.com")
PREFERRED_TYPES = ("headshot", "profile", "portrait", "head")
YEAR = "2026"
OUT_DIR = cache.ASSET_DIR / "players-2026"
MANIFEST = cache.ASSET_DIR / "leaguepedia_player_images_2026.json"
MAPPING = cache.ASSET_DIR / "mapping" / "player_images_2026.json"
USER_AGENT = "ManagementGame-ResearchIngestion/0.3 (internal research cache; bot login)"


def key(text):
    return re.sub(r"[^0-9a-z]+", "", (text or "").casefold())


def strip_parenthetical(text):
    return re.sub(r"\s*\([^)]*\)\s*$", "", text or "").strip()


def teams_agree(left, right):
    a, b = key(strip_parenthetical(left)), key(strip_parenthetical(right))
    return bool(a and b and (a == b or (min(len(a), len(b)) >= 3 and (a in b or b in a))))


class Leaguepedia:
    """Logged-in, paced MediaWiki client. Obeys ratelimited errors by waiting instead of retrying fast."""

    def __init__(self, username, password, delay=1.0, session=None, sleep=time.sleep):
        self.session = session or requests.Session()
        self.session.headers["User-Agent"] = USER_AGENT
        self.delay, self.sleep, self.last, self.requests = delay, sleep, 0.0, 0
        if not self._login(username, password):
            # Older bots require the account name as the login name and the suffix inside the password.
            name, _, rest = username.partition("@")
            self.session.cookies.clear()
            if not rest or not self._login(name, f"{rest}@{password}"):
                raise RuntimeError("Leaguepedia login failed for the configured bot credentials")

    def _pace(self):
        wait = self.delay - (time.monotonic() - self.last)
        if wait > 0:
            self.sleep(wait)
        self.last = time.monotonic()

    def call(self, method="get", **params):
        params["format"] = "json"
        for attempt in range(6):
            self._pace()
            self.requests += 1
            if method == "post":
                response = self.session.post(API, data=params, timeout=60)
            else:
                response = self.session.get(API, params=params, timeout=60)
            response.raise_for_status()
            body = response.json()
            error = body.get("error", {}).get("code")
            if error == "ratelimited":
                self.sleep(30 * (attempt + 1))
                continue
            if error:
                raise RuntimeError(f"Leaguepedia API error: {error}")
            return body
        raise RuntimeError("Leaguepedia API stayed rate limited")

    def _login(self, username, password):
        token = self.call(action="query", meta="tokens", type="login")["query"]["tokens"]["logintoken"]
        result = self.call("post", action="login", lgname=username, lgpassword=password, lgtoken=token)
        return result.get("login", {}).get("result") == "Success"

    def cargo(self, tables, fields, where, order_by, page=500):
        offset, rows = 0, []
        while True:
            body = self.call(action="cargoquery", tables=tables, fields=fields, where=where, order_by=order_by,
                             limit=page, offset=offset)
            batch = [item["title"] for item in body.get("cargoquery", [])]
            rows += batch
            if len(batch) < page:
                return rows
            offset += page

    def file_info(self, names):
        """File title -> {url, timestamp, size, mime, license} in batches of 50 titles."""
        info = {}
        for start in range(0, len(names), 50):
            batch = names[start:start + 50]
            body = self.call(action="query", prop="imageinfo", iiprop="url|timestamp|size|mime|extmetadata",
                             titles="|".join(f"File:{n}" for n in batch))
            pages = body.get("query", {}).get("pages", {})
            titles = {key_["title"].removeprefix("File:"): key_ for key_ in pages.values() if "title" in key_}
            normalized = {n["to"].removeprefix("File:"): n["from"].removeprefix("File:")
                          for n in body.get("query", {}).get("normalized", [])}
            for title, page in titles.items():
                image = (page.get("imageinfo") or [None])[0]
                if not image:
                    continue
                meta = image.get("extmetadata") or {}
                original = normalized.get(title, title)
                info[original] = {"url": image.get("url"), "timestamp": image.get("timestamp"),
                                  "width": image.get("width"), "height": image.get("height"),
                                  "mime": image.get("mime"), "size": image.get("size"),
                                  "license": (meta.get("LicenseShortName") or {}).get("value"),
                                  "artist": (meta.get("Artist") or {}).get("value")}
        return info


def load_catalog(db):
    """Provider IDs, nicknames and 2026 registration team names; no network."""
    players = {r[0]: {"name": r[1], "registered_2026": r[2] == "verified", "teams": set()}
               for r in db.execute("SELECT p.player_id, p.name, i.registered_in_2026 FROM ps_players p "
                                   "LEFT JOIN ps_player_index_2026 i USING(player_id)")}
    for player_id, team, acronym in db.execute("SELECT r.player_id, t.name, t.acronym FROM ps_registrations r "
                                               "JOIN ps_teams t USING(team_id)"):
        if player_id in players:
            players[player_id]["teams"] |= {t for t in (team, acronym) if t}
    return players


def match_players(images, players):
    """LP 2026 image rows -> {player_id: (confidence, [rows])}. Ambiguous names are never guessed."""
    by_link = defaultdict(list)
    for row in images:
        by_link[row["Link"]].append(row)
    registered, catalog = defaultdict(list), defaultdict(list)
    for player_id, record in players.items():
        nick = key(record["name"])
        if nick:
            catalog[nick].append(player_id)
            if record["registered_2026"]:
                registered[nick].append(player_id)
    by_nick = defaultdict(list)
    for link in by_link:
        by_nick[key(strip_parenthetical(link))].append(link)
    matched, review = {}, []
    for nick, links in by_nick.items():
        if not nick:
            continue
        rows = [r for link in links for r in by_link[link]]
        candidates = registered.get(nick) or []
        scope = "registered_2026"
        if not candidates:
            candidates, scope = catalog.get(nick) or [], "catalog_only"
        if not candidates:
            review.append({"link": links[0], "reason": "no_catalog_player"})
            continue
        agreeing = [p for p in candidates
                    if any(teams_agree(r["Team"], t) for r in rows for t in players[p]["teams"])]
        if len(candidates) == 1 and len(links) == 1:
            confidence = ("HIGH" if agreeing else "MEDIUM") if scope == "registered_2026" else \
                         ("MEDIUM" if agreeing else "LOW")
            matched[candidates[0]] = (confidence, scope, bool(agreeing), rows)
        elif len(agreeing) == 1:
            matched[agreeing[0]] = ("HIGH" if scope == "registered_2026" else "MEDIUM", scope, True, rows)
        else:
            review.append({"link": links[0], "reason": "ambiguous_name", "candidates": candidates[:6]})
    return matched, review


def is_year_image(row, meta):
    """A portrait counts as 2026 only if the file itself says so; a 2026 tournament page may link an older photo."""
    return YEAR in row["FileName"] or (meta.get("timestamp") or "").startswith(YEAR)


def choose_image(rows, info):
    """Prefer headshot/profile images, then the most recently uploaded file."""
    def rank(row):
        meta = info.get(row["FileName"], {})
        kind = (row.get("ImageType") or "").casefold()
        return (meta.get("timestamp") is not None, kind in PREFERRED_TYPES, meta.get("timestamp") or "")
    usable = [r for r in rows if info.get(r["FileName"], {}).get("url") and is_year_image(r, info[r["FileName"]])]
    return max(usable, key=rank) if usable else None


def year_hint(url_text):
    years = re.findall(r"(?<!\d)(20[0-3]\d)(?!\d)", (url_text or "").rsplit("/", 1)[-1])
    return years[0] if years else None


def trusted_source(url):
    parts = urlsplit(url or "")
    return parts.scheme == "https" and parts.hostname is not None and parts.hostname.endswith(HOST_SUFFIXES) \
        and parts.port in (None, 443)


def download(session, url, sleep=time.sleep):
    for attempt in range(4):
        try:
            response = session.get(url, timeout=60, stream=True, allow_redirects=True)
            if response.status_code == 429 or response.status_code >= 500:
                sleep(2 ** attempt * 2)
                continue
            if response.status_code != 200:
                raise cache.ImageError(f"HTTP {response.status_code}")
            if not trusted_source(response.url):
                raise cache.ImageError("redirect to untrusted host", "INVALID_IMAGE")
            body = bytearray()
            for chunk in response.iter_content(65536):
                body.extend(chunk)
                if len(body) > cache.MAX_BYTES:
                    raise cache.ImageError("body over size limit", "INVALID_IMAGE")
            content_type = (response.headers.get("Content-Type") or "").split(";")[0].strip().lower()
            extension, mime = cache.validate_image(bytes(body), content_type)
            return bytes(body), mime, extension
        except (requests.Timeout, requests.ConnectionError):
            sleep(2 ** attempt * 2)
    raise cache.ImageError("retries exhausted")


def run(db_path, out_dir=OUT_DIR, manifest=MANIFEST, mapping=MAPPING, client=None, downloader=None, dry_run=False,
        limit=None, delay=0.5, min_confidence="LOW", sleep=time.sleep):
    db = sqlite3.connect(Path(db_path).resolve().as_uri() + "?mode=ro", uri=True)
    try:
        players = load_catalog(db)
        stems = cache.asset_stems(db)
        previous = {r[0]: r[1] for r in db.execute("SELECT entity_id, local_path FROM image_cache "
                                                   "WHERE entity_type='player' AND status='CACHED'")}             if cache.has_table(db, "image_cache") else {}
        urls = {r[0]: r[1] for r in db.execute("SELECT player_id, image_url FROM ps_players")}
    finally:
        db.close()
    images = client.cargo("PlayerImages", "Link,FileName,Team,Tournament,ImageType,IsProfileImage",
                          f"Tournament LIKE '%{YEAR}%'", "Link,FileName")
    matched, review = match_players(images, players)
    order = {"HIGH": 0, "MEDIUM": 1, "LOW": 2}
    matched = {p: m for p, m in matched.items() if order[m[0]] <= order[min_confidence]}
    names = sorted({r["FileName"] for _, _, _, rows in matched.values() for r in rows})
    info = client.file_info(names)
    out_dir = Path(out_dir)
    entries, summary = [], {"lp_images": len(images), "matched_players": len(matched), "review": len(review),
                            "downloaded": 0, "unchanged": 0, "failed": 0, "no_usable_file": 0}
    session = downloader or requests.Session()
    if downloader is None:
        session.headers["User-Agent"] = USER_AGENT
    done = 0
    for player_id in sorted(matched, key=int):
        confidence, scope, team_agrees, rows = matched[player_id]
        row = choose_image(rows, info)
        if row is None:
            summary["no_usable_file"] += 1
            continue
        meta = info[row["FileName"]]
        entry = {"player_id": player_id, "nickname": players[player_id]["name"], "confidence": confidence,
                 "match_scope": scope, "team_agrees": team_agrees, "leaguepedia_link": row["Link"],
                 "file_name": row["FileName"], "team": row["Team"], "tournament": row["Tournament"],
                 "image_type": row.get("ImageType"), "source_url": meta["url"], "uploaded_at": meta["timestamp"],
                 "license": meta["license"], "artist": meta["artist"], "status": "PLANNED", "local_path": None,
                 "previous_pandascore_path": previous.get(player_id),
                 "previous_pandascore_year_hint": year_hint(urls.get(player_id)),
                 "year": YEAR, "source": "Source: Leaguepedia (lol.fandom.com)"}
        if not trusted_source(meta["url"]):
            entry.update(status="DOWNLOAD_FAILED", error="untrusted source host")
            summary["failed"] += 1
        elif not dry_run and (limit is None or done < limit):
            done += 1
            stem = cache.target_stem("player", "image_url", player_id, stems).split("/", 1)[1]
            try:
                body, mime, extension = download(session, meta["url"], sleep)
                relative = f"{stem}.{extension}"
                result, path, digest = cache.store(out_dir, relative, body)
                entry.update(status="CACHED", local_path=f"players-2026/{relative}", content_type=mime, format=extension,
                             bytes=len(body), sha256=digest, cached_at=cache.now_iso())
                summary["downloaded" if result == "cached" else "unchanged"] += 1
            except (cache.ImageError, OSError) as exc:
                entry.update(status=getattr(exc, "status", "DOWNLOAD_FAILED"), error=str(exc))
                summary["failed"] += 1
            sleep(delay)
        entries.append(entry)
    meta_block = {"provider": "leaguepedia", "source": "Source: Leaguepedia (lol.fandom.com)", "year": YEAR,
                  "scope": "Internal research cache only; image rights remain with their owners.",
                  "generated_at": cache.now_iso(), "summary": summary}
    cache.write_json(Path(manifest), {**meta_block, "entries": entries, "review": review})
    cache.write_json(Path(mapping), {**meta_block, "entries": [
        {k: e[k] for k in ("player_id", "nickname", "confidence", "match_scope", "team_agrees", "status", "local_path",
                           "previous_pandascore_path", "previous_pandascore_year_hint", "team", "tournament",
                           "file_name", "uploaded_at")} for e in entries]})
    return summary


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=cache.DATA_DIR / "staging.sqlite3")
    parser.add_argument("--dry-run", action="store_true", help="query and match only; download nothing")
    parser.add_argument("--limit", type=int, help="maximum downloads this invocation")
    parser.add_argument("--delay", type=float, default=0.5, help="seconds between image downloads")
    parser.add_argument("--min-confidence", choices=("HIGH", "MEDIUM", "LOW"), default="LOW")
    args = parser.parse_args()
    env = {**dotenv_values(Path(__file__).resolve().parent / ".env"), **os.environ}
    username, password = env.get("LEAGUEPEDIA_BOT_USERNAME", "").strip(), env.get("LEAGUEPEDIA_BOT_PASSWORD", "").strip()
    if not username or not password:
        parser.error("LEAGUEPEDIA_BOT_USERNAME and LEAGUEPEDIA_BOT_PASSWORD must be set in .env")
    client = Leaguepedia(username, password)
    summary = run(args.db, client=client, dry_run=args.dry_run, limit=args.limit, delay=args.delay,
                  min_confidence=args.min_confidence)
    summary["api_requests"] = client.requests
    print(json.dumps(summary, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
