"""Source C: professional per-game player statistics from Leaguepedia Cargo (lol.fandom.com), 2025 onward.

Independent adapter with its own source id ("leaguepedia"). Leaguepedia's scoreboards are compiled by its editors
from official match data; they are not a copy of Oracle's Elixir, so the two can cross-check each other. What the
tables carry (checked 2026-10-14 with action=cargofields):
  ScoreboardPlayers  per player and game: team, opponent, role, side, champion, kills, deaths, assists, gold, CS,
                     damage to champions, vision score, team kills, team gold, win, date, tournament, player page;
  ScoreboardGames    per game: teams, winner, length, patch, team objectives (towers, dragons, barons, heralds, ...);
  Tournaments        per event: league, region, level (Primary/Secondary), qualifier, official, event type;
  PlayerRedirects    renamed or alternative player pages -> the canonical page (the player's Leaguepedia identity).
Not available: gold/XP/CS differences at 10 or 15 minutes, first blood, wards placed or cleared, damage taken.

Collection: one window per calendar month from --since to the as-of cutoff (00:00 UTC today by default, so a day
still being entered is left out). Each window is counted (COUNT(*), no join), paged at the API cap of 500 rows with a
total order, then counted again; a window whose count moved is fetched once more, and a page total that differs from
the count marks the window incomplete. Raw responses are kept as immutable JSON pages under
data/raw/leaguepedia/<run>/ with a SHA-256 manifest, checkpointed after every window (--resume RUN continues).
`--normalize` (offline) rebuilds lp_games and lp_player_games in the staging database from the newest observation of
every game. Values are stored as published; a blank cell stays NULL.

Needs LEAGUEPEDIA_BOT_USERNAME / LEAGUEPEDIA_BOT_PASSWORD (Special:BotPasswords) in .env; never printed or stored.
Research use only: Leaguepedia text content is CC BY-SA; game statistics belong to Riot Games.

  python fetch_leaguepedia_stats.py --since 2025-01 --dry-run    # counts per window only
  python fetch_leaguepedia_stats.py --since 2025-01              # collect
  python fetch_leaguepedia_stats.py --normalize                  # offline: rebuild lp_* tables
"""
import argparse
from datetime import date, datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import sys

DATA_DIR = Path(__file__).resolve().parent / "data"
RAW_ROOT = DATA_DIR / "raw" / "leaguepedia"
PAGE = 500
SOURCE = "leaguepedia"
# alias -> Cargo field. Aliases keep the response keys stable (Cargo turns underscores into spaces otherwise).
PLAYER_FIELDS = {
    "GameId": "SP.GameId", "MatchId": "SP.MatchId", "OverviewPage": "SP.OverviewPage", "Tournament": "SP.Tournament",
    "DateTimeUTC": "SP.DateTime_UTC", "Link": "SP.Link", "CanonicalLink": "PR.OverviewPage", "Name": "SP.Name",
    "Team": "SP.Team", "TeamVs": "SP.TeamVs", "Role": "SP.Role", "RoleNumber": "SP.Role_Number",
    "IngameRole": "SP.IngameRole", "Side": "SP.Side", "Champion": "SP.Champion", "Kills": "SP.Kills",
    "Deaths": "SP.Deaths", "Assists": "SP.Assists", "Gold": "SP.Gold", "CS": "SP.CS",
    "DamageToChampions": "SP.DamageToChampions", "VisionScore": "SP.VisionScore", "TeamKills": "SP.TeamKills",
    "TeamGold": "SP.TeamGold", "PlayerWin": "SP.PlayerWin", "GameRoleIdVs": "SP.GameRoleIdVs"}
GAME_FIELDS = {
    "GameId": "SG.GameId", "MatchId": "SG.MatchId", "OverviewPage": "SG.OverviewPage", "Tournament": "SG.Tournament",
    "DateTimeUTC": "SG.DateTime_UTC", "GameLengthMin": "SG.Gamelength_Number", "Patch": "SG.Patch",
    "Team1": "SG.Team1", "Team2": "SG.Team2", "WinTeam": "SG.WinTeam", "Winner": "SG.Winner",
    "GameInMatch": "SG.N_GameInMatch", "RiotPlatformGameId": "SG.RiotPlatformGameId",
    **{f"Team{s}{k}": f"SG.Team{s}{k}" for s in (1, 2) for k in ("Kills", "Gold", "Towers", "Dragons", "Barons",
                                                                     "RiftHeralds", "VoidGrubs", "Inhibitors", "Atakhans")},
    "League": "T.League", "Region": "T.Region", "TournamentLevel": "T.TournamentLevel", "IsQualifier": "T.IsQualifier",
    "IsPlayoffs": "T.IsPlayoffs", "IsOfficial": "T.IsOfficial", "EventType": "T.EventType", "Split": "T.Split",
    "TournamentYear": "T.Year", "StandardName": "T.StandardName"}
QUERIES = {
    "games": {"tables": "ScoreboardGames=SG,Tournaments=T", "join_on": "SG.OverviewPage=T.OverviewPage",
              "fields": GAME_FIELDS, "alias": "SG", "count_tables": "ScoreboardGames=SG",
              "order_by": "SG.DateTime_UTC,SG.GameId"},
    "players": {"tables": "ScoreboardPlayers=SP,PlayerRedirects=PR", "join_on": "SP.Link=PR.AllName",
                "fields": PLAYER_FIELDS, "alias": "SP", "count_tables": "ScoreboardPlayers=SP",
                "order_by": "SP.DateTime_UTC,SP.GameId,SP.Side,SP.Role_Number,SP.Link"},
}


def months(since, until):
    """[(label, start, end)] calendar-month windows; the last one ends at the cutoff."""
    year, month = since
    out = []
    while date(year, month, 1) < until:
        nxt = date(year + month // 12, month % 12 + 1, 1)
        out.append((f"{year:04d}-{month:02d}", date(year, month, 1), min(nxt, until)))
        year, month = nxt.year, nxt.month
    return out


def where(alias, start, end):
    return f"{alias}.DateTime_UTC >= '{start.isoformat()} 00:00:00' AND {alias}.DateTime_UTC < '{end.isoformat()} 00:00:00'"


def count(client, query, start, end):
    body = client.call(action="cargoquery", tables=query["count_tables"], fields="COUNT(*)=n",
                       where=where(query["alias"], start, end))
    return int(body["cargoquery"][0]["title"]["n"])


def fetch_window(client, query, start, end, store):
    """Pages one window; store(page number, rows) persists each page. Returns the row total."""
    fields = ",".join(f"{field}={alias}" for alias, field in query["fields"].items())
    offset, page, total = 0, 0, 0
    while True:
        body = client.call(action="cargoquery", tables=query["tables"], join_on=query["join_on"], fields=fields,
                           where=where(query["alias"], start, end), order_by=query["order_by"], limit=PAGE, offset=offset)
        rows = [item["title"] for item in body.get("cargoquery", [])]
        page += 1
        store(page, rows)
        total += len(rows)
        if len(rows) < PAGE:
            return total
        offset += PAGE


def write_json(path, payload):
    text = json.dumps(payload, ensure_ascii=False, indent=0, sort_keys=True)
    path.write_text(text + "\n", encoding="utf-8", newline="\n")
    return hashlib.sha256((text + "\n").encode("utf-8")).hexdigest()


def collect(client, since, cutoff, root=RAW_ROOT, resume=None, dry_run=False, log=print):
    run_id = resume or datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    run_dir = root / run_id
    run_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = run_dir / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else {
        "source": SOURCE, "api": "https://lol.fandom.com/api.php (action=cargoquery, logged-in bot)", "run_id": run_id,
        "since": f"{since[0]:04d}-{since[1]:02d}", "cutoff": cutoff.isoformat(), "page_size": PAGE, "status": "running",
        "queries": {name: {k: (v if k != "fields" else v) for k, v in q.items()} for name, q in QUERIES.items()},
        "windows": {}}
    cutoff = date.fromisoformat(manifest["cutoff"])
    for label, start, end in months(since, cutoff):
        if manifest["windows"].get(label, {}).get("complete"):
            continue
        entry = {"start": start.isoformat(), "end": end.isoformat(), "tables": {}}
        for name, query in QUERIES.items():
            for attempt in range(2):
                before = count(client, query, start, end)
                pages, keys = [], set()

                def store(number, rows, name=name):
                    if dry_run:
                        return
                    keys.update((r.get("GameId"), r.get("Side"), r.get("RoleNumber")) for r in rows)
                    file_name = f"{label}-{name}-page-{number:04d}.json"
                    pages.append({"page": number, "rows": len(rows), "file": file_name,
                                  "sha256": write_json(run_dir / file_name, rows)})
                fetched = 0 if dry_run else fetch_window(client, query, start, end, store)
                after = before if dry_run else count(client, query, start, end)
                if before == after:
                    break
            unique = before if dry_run else len(keys)
            entry["tables"][name] = {"count_before": before, "count_after": after, "rows": fetched, "unique_keys": unique,
                                     "pages": pages, "stable": before == after,
                                     "complete": dry_run or (before == after == unique)}
            log(f"{label} {name}: count {before}, fetched {fetched}{'' if before == after else ' (count moved)'}")
        entry["complete"] = all(t["complete"] for t in entry["tables"].values())
        manifest["windows"][label] = entry
        manifest["requests"] = getattr(client, "requests", None)
        write_json(manifest_path, manifest)
    manifest["status"] = "dry_run" if dry_run else ("completed" if all(w["complete"] for w in manifest["windows"].values())
                                                   else "incomplete")
    manifest["finished_at"] = datetime.now(timezone.utc).isoformat()
    manifest["requests"] = getattr(client, "requests", None)
    write_json(manifest_path, manifest)
    return manifest


# ---------- offline normalization ----------

GAME_COLUMNS = ["game_id", "match_id", "overview_page", "tournament", "game_ts", "game_length_min", "patch", "team1",
                "team2", "win_team", "winner", "game_in_match", "riot_platform_game_id"] + \
               [f"team{s}_{k.lower()}" for s in (1, 2) for k in ("Kills", "Gold", "Towers", "Dragons", "Barons", "RiftHeralds",
                                                                   "VoidGrubs", "Inhibitors", "Atakhans")] + \
               ["league", "region", "tournament_level", "is_qualifier", "is_playoffs", "is_official", "event_type", "split",
                "tournament_year", "standard_name", "run_id", "source_file", "page_sha256"]
PLAYER_COLUMNS = ["game_id", "side", "role_number", "match_id", "overview_page", "tournament", "game_ts", "link",
                  "canonical_link", "name", "team", "team_vs", "role", "ingame_role", "champion", "kills", "deaths",
                  "assists", "gold", "cs", "damage_to_champions", "vision_score", "team_kills", "team_gold", "player_win",
                  "game_role_id_vs", "duplicate_rows", "run_id", "source_file", "page_sha256"]
NUMERIC = {"game_length_min", "kills", "deaths", "assists", "gold", "cs", "damage_to_champions", "vision_score",
           "team_kills", "team_gold"} | {c for c in GAME_COLUMNS if c.startswith("team") and c[4] in "12" and "_" in c}


def blank(value):
    return None if value is None or str(value).strip() == "" else str(value).strip()


def numeric(value):
    value = blank(value)
    try:
        return None if value is None else float(value)
    except ValueError:
        return None


def completed_runs(root=RAW_ROOT):
    runs = []
    for manifest_path in sorted(root.glob("*/manifest.json")):
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        if manifest.get("status") in ("completed", "incomplete"):
            runs.append((manifest_path.parent, manifest))
    return runs


def normalize(db_path, root=RAW_ROOT):
    """Rebuilds lp_games / lp_player_games / lp_collection_windows. A game seen in several runs keeps the newest run's
    rows; within a run, a duplicated (game, side, role number) is kept once and counted."""
    games, players, windows = {}, {}, []
    for run_dir, manifest in completed_runs(root):
        for label, window in sorted(manifest["windows"].items()):
            for table, info in window["tables"].items():
                windows.append((manifest["run_id"], label, table, info["count_before"], info["count_after"], info["rows"],
                                int(info["complete"])))
                run_games, run_players = {}, {}
                for page in info["pages"]:
                    path = run_dir / page["file"]
                    text = path.read_text(encoding="utf-8")
                    if hashlib.sha256(text.encode("utf-8")).hexdigest() != page["sha256"]:
                        raise RuntimeError(f"{path} does not match its manifest hash")
                    for row in json.loads(text):
                        provenance = [manifest["run_id"], page["file"], page["sha256"]]
                        if table == "games":
                            values = [blank(row.get(k)) for k in GAME_FIELDS]
                            values = [numeric(v) if col in NUMERIC else v for col, v in zip(GAME_COLUMNS, values)]
                            run_games[values[0]] = values + provenance
                        else:
                            key = (blank(row.get("GameId")), blank(row.get("Side")), blank(row.get("RoleNumber")))
                            values = [blank(row.get(k)) for k in PLAYER_FIELDS]
                            named = dict(zip(PLAYER_FIELDS, values))
                            record = [key[0], key[1], key[2]] + [named[k] for k in PLAYER_FIELDS if k not in ("GameId", "Side", "RoleNumber")]
                            cols = PLAYER_COLUMNS[:-4]
                            record = [numeric(v) if c in NUMERIC else v for c, v in zip(cols, record)]
                            if key in run_players:
                                run_players[key][-4] += 1
                            else:
                                run_players[key] = record + [0] + provenance
                games.update(run_games)
                for key, record in run_players.items():
                    players[key] = record
    db = sqlite3.connect(db_path)
    try:
        with db:
            for table, columns, rows, key in (
                    ("lp_games", GAME_COLUMNS, list(games.values()), "PRIMARY KEY (game_id)"),
                    ("lp_player_games", PLAYER_COLUMNS, list(players.values()), "PRIMARY KEY (game_id, side, role_number)")):
                db.execute(f"DROP TABLE IF EXISTS {table}")
                db.execute(f"CREATE TABLE {table} ({', '.join(c + (' REAL' if c in NUMERIC else ' TEXT') for c in columns)}, {key})")
                if rows:
                    db.executemany(f"INSERT INTO {table} VALUES ({','.join('?' * len(columns))})", rows)
            db.execute("DROP TABLE IF EXISTS lp_collection_windows")
            db.execute("CREATE TABLE lp_collection_windows (run_id TEXT, window TEXT, query TEXT, count_before INTEGER, "
                       "count_after INTEGER, rows INTEGER, complete INTEGER, PRIMARY KEY (run_id, window, query))")
            db.executemany("INSERT INTO lp_collection_windows VALUES (?,?,?,?,?,?,?)", windows)
        return {"games": len(games), "player_rows": len(players), "windows": len(windows)}
    finally:
        db.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--since", default="2025-01", help="first month, YYYY-MM")
    parser.add_argument("--cutoff", help="as-of date (exclusive), YYYY-MM-DD; default today 00:00 UTC")
    parser.add_argument("--resume", help="continue a run id")
    parser.add_argument("--dry-run", action="store_true", help="counts only")
    parser.add_argument("--delay", type=float, default=1.0)
    parser.add_argument("--normalize", action="store_true", help="offline: rebuild lp_* tables from raw runs")
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    args = parser.parse_args()
    if args.normalize:
        print(json.dumps(normalize(args.db)))
        return 0
    from dotenv import dotenv_values
    from fetch_leaguepedia_images import Leaguepedia
    env = {**dotenv_values(Path(__file__).resolve().parent / ".env"), **os.environ}
    username, password = env.get("LEAGUEPEDIA_BOT_USERNAME", "").strip(), env.get("LEAGUEPEDIA_BOT_PASSWORD", "").strip()
    if not username or not password:
        parser.error("LEAGUEPEDIA_BOT_USERNAME and LEAGUEPEDIA_BOT_PASSWORD must be set in .env")
    client = Leaguepedia(username, password, delay=args.delay)
    since = tuple(int(x) for x in args.since.split("-"))
    cutoff = date.fromisoformat(args.cutoff) if args.cutoff else datetime.now(timezone.utc).date()
    manifest = collect(client, since, cutoff, resume=args.resume, dry_run=args.dry_run,
                       log=lambda text: print(text, flush=True))
    print(json.dumps({"run_id": manifest["run_id"], "status": manifest["status"], "requests": client.requests}))
    return 0 if manifest["status"] in ("completed", "dry_run") else 3


if __name__ == "__main__":
    sys.exit(main())
