# LoL Esports Data Starter (research only)

This is an **independent staging utility**, not a production game importer, all-2026 exhaustive scraper, legal permission, or validated current-roster dataset. It was not tested against a live account. There is no PandaScore API key included.

## Windows setup

```powershell
cd "C:\Management Game"
mkdir tools\lol-data-ingestion
# Extract this starter's contents into tools\lol-data-ingestion, then:
cd tools\lol-data-ingestion
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
Copy-Item .env.example .env
notepad .env
```

Enter your personal PandaScore REST API token in `.env`. Never commit that file, distribute it, or put it in your game binary.

## First smoke run

```powershell
.\.venv\Scripts\python.exe sync_pandascore.py --collections leagues teams players --max-pages 1
```

Outputs:
- `data/raw/pandascore/<UTC-run-id>/*.json`: immutable raw snapshot for that run and SHA256 manifest.
- `data/staging.sqlite3`, table `provider_records`: upserted provider records keyed by `(provider, collection, external_id)`.

Each returned list defaults to 100 entries per page. `--max-pages 1` is **intentionally incomplete**. Increase carefully after inspecting the budget/coverage. The script throttles at >=4 seconds between requests; it handles server quota errors, records failed run manifests and does not bypass restrictions. A page with 100 results may still be the last page: the manifest conservatively marks the cap as reached. Do not call the results a full 2026 list.

Sample broader scan (may take time or hit plan limits):

```powershell
.\run_sync.ps1 -MaxPages 5 -Collections leagues,series,tournaments,teams,players,matches
```

The entire PandaScore catalogue contains historical/inactive records; **do not** use its presence to set `active_as_of_2026_10_08 = true`. The starter deliberately does not infer roster/academy status. Establish those from timestamped roster and match evidence and separate `played_in_2026`, `on_active_roster_as_of`, `academy`, `substitute`, `retired`, and `unknown`.

## Full 2026 collection (catalogue + competitions)

The `full-2026` preset collects every league, team and player record the Free plan lists, and every series, stage
and match that touches 2026 (past, running and upcoming). Each query is sorted by `id`, paged to `X-Total` and
reconciled; the manifest is checkpointed after every page.

```powershell
.\.venv\Scripts\python.exe discover_api.py --probe full-2026                 # optional: 19 scope probes, raw kept
.\.venv\Scripts\python.exe sync_pandascore.py --preset full-2026 --max-pages 200 --max-requests 270
# Stopped (quota floor, budget, page cap: exit code 3)? Continue the same run; saved pages are not fetched again:
.\.venv\Scripts\python.exe sync_pandascore.py --preset full-2026 --max-pages 200 --resume <RUN_ID>
.\.venv\Scripts\python.exe normalize_pandascore.py --export                  # offline: tables + local CSV exports
.\.venv\Scripts\python.exe collection_report.py                              # offline: aggregate completeness stats
.\.venv\Scripts\python.exe cache_logos.py                                    # offline report; see "Portraits and logos" below
```

About 180 requests at 4 s each (about 14 minutes). Results: [`reports/PANDASCORE_FULL_COLLECTION_2026.md`](reports/PANDASCORE_FULL_COLLECTION_2026.md).

**Staging tables** (`data/staging.sqlite3`, all rebuilt offline from the immutable raw runs):

| Kind | Tables | Rule |
|---|---|---|
| Observations | `collection_runs`, `collection_queries`, `record_observations`, `record_payloads`, `tournament_registrations`, `team_membership_observations` | Append-only, keyed by run: a later run never overwrites what an earlier run saw |
| Current | `ps_leagues`, `ps_series`, `ps_tournaments`, `ps_tournament_teams`, `ps_registrations`, `ps_matches`, `ps_match_games`, `ps_teams`, `ps_players`, `ps_image_refs`, `ps_player_index_2026` | Rebuilt each time from the latest copy of each record, with run, query, file and payload hash |
| Starter | `provider_records`, `tournament_roster` | Unchanged |

`record_payloads` holds every distinct raw payload once (by SHA-256), so every provider field stays reachable.
`ps_player_index_2026` is dataset B (players on a 2026 stage roster); `ps_players` is dataset A (the whole
catalogue, which says nothing about activity). Classification columns say `verified` only with the named evidence and
`unknown` otherwise; individual match participation, academy and reserve status stay `unknown` (no source for them).

**Portraits and logos.** `cache_logos.py` extends the existing `image_cache` table for player portraits,
regular/dark team logos and league logos. `ps_image_refs` is authoritative; URLs are used exactly as supplied.
The cache accepts PNG/JPEG/GIF/WebP after signature, MIME, bounded decoding and hash validation. Install the
updated `requirements.txt` (includes Pillow). Assets, manifests and ID-bearing exports stay in ignored `data/`.
See [the image collection report](reports/PANDASCORE_IMAGE_ASSET_COLLECTION_2026.md) for rights evidence and coverage.

```powershell
.\.venv\Scripts\python.exe cache_logos.py --dry-run --include-dark
.\.venv\Scripts\python.exe collection_report.py --image-assets
.\.venv\Scripts\python.exe -m unittest discover -s tests
```

Both reporting commands are offline. They migrate `image_cache` additively, verify existing files and rebuild
`data/assets/image_manifest.json`, `image_coverage.json` and `mapping/{player_images,team_images,roster_image_index_2026}.json`.
No REST recrawl or normalization is needed. `image_mappings.py` is an export helper for the same database/cache,
not another asset system. The old `logo_manifest.json` output is superseded by `image_manifest.json`.

The public terms and API access alone do not establish this account's rights to archive photographs and marks.
`--license-approved` must contain an existing responsible approver's decision, scope, date and evidence reference;
the switch is an authorization record, not proof of a licence. Never fabricate the note. Without it, no network
session is opened, including direct calls to `run()`. `--dry-run` always disables networking even with a note.
Research caching, local display, public redistribution and commercial game distribution are separate permissions.
Do not copy these files into the game, content packs, a production build or the repository.

After evidence and approval exist, set `$approvalNote` to the actual recorded decision and run:

```powershell
.\.venv\Scripts\python.exe cache_logos.py --license-approved $approvalNote --pilot
# Inspect the pilot's files, manifest, roster mappings and coverage before continuing.
.\.venv\Scripts\python.exe cache_logos.py --license-approved $approvalNote --include-dark --limit 250 --batch-size 50
```

`--pilot` selects up to 20 pending portraits and 20 regular team logos. The full command includes leagues and
optional dark logos; use `--entities player team` to restrict scope. Repeat bounded commands to advance through
pending assets: valid hits and recorded failures do not consume `--limit`. Each attempt commits its checkpoint.
`--retry-failed` explicitly revisits errors with bounded retries; unchanged failures otherwise stay skipped.
Changed source URLs and damaged cached files are eligible for refetch. `--refresh` rechecks valid cache hits.
Requests, redirects and retries all obey a shared `--delay` (default 1 second, minimum 0.1 seconds); each asset gets at most four attempts
with at most three redirects per attempt. Run metrics are retained in `data/assets/runs/*.json`.
`--workers 1..8` controls concurrent downloads. Each worker owns its HTTP session; all workers share the same
request pacing, and the main thread alone writes the database and files. Requests are queued in bounded batches.
For example, `--workers 8 --delay 0.1 --batch-size 50` permits at most ten request starts per second overall.

On 2026-10-14 the user explicitly authorized bulk local download for a noncommercial game released internally,
superseding the earlier requirement to await provider confirmation before downloading. The actual decision is
recorded in `data/assets/download_authorization.json` and each downloaded asset's licence note. Provider/rightsholder
confirmation remains `NOT_SUPPLIED`; this execution decision does not establish image redistribution rights.

| Image status | Meaning |
| --- | --- |
| `AVAILABLE` | Non-null trusted reference with a recorded approval; file not yet verified in the cache |
| `CACHED` | Current original URL and ID-specific local file verified by path, size, SHA-256, MIME and image validation |
| `MISSING_URL` | Null, empty, absent URL or absent reference row; no substitute |
| `DOWNLOAD_FAILED` | HTTP/transport/storage error or invalid source identity/URL; error records the reason |
| `INVALID_IMAGE` | Rejected content or failed local cache validation; never exposes a usable path |
| `LICENSE_REVIEW_REQUIRED` | URL exists but no scoped approval has been recorded |

Download status and use-rights status are separate: `APPROVAL_RECORDED_INTERNAL_RESEARCH` records a decision,
never a claim of ownership or permission to distribute. `reference_license_status` preserves the source's review
marker. Null URLs remain missing even when licensing is unresolved. Latest validation times concern checked files;
unattempted references have no download timestamp. Manifest paths are relative to `data/assets/`.

Cached files are named after what they show: `players/<nickname>.png`, `team-logos/<team>.png` (dark variant
`<team>-dark.png`) and `league-logos/<league>.png`. Spaces become `_`, characters Windows forbids are dropped, and a
name shared by several entities of the same kind (about 700 of ~12,000) gets `__<provider id>` on every member, so
no file silently stands for the wrong person and a later duplicate never reassigns an existing file. The provider ID
stays the identity key in the manifest and mappings; renaming is automatic and offline (a rerun, including
`--dry-run`, moves files to match the catalog without any request). Limits: 8 MiB per file, 40 megapixels.

Mappings use `(pandascore, entity type, provider ID, image field, original)`; fictional game IDs are never assigned.
Roster rows use `ps_registrations` for stages touching calendar 2026, and resolve the logo by the registration team.
Current provider team, historical membership observations and historical roster observations are separate fields.
Missing dark paths remain null; `select_team_logo(..., dark=True)` may return that same team's regular path.
Overlapping registrations under different teams and uncertain periods are exported for review, never silently
resolved. No participation, contract, academy or reserve status is inferred. Every export includes Source: PandaScore.

**Calibration note.** `calibrate.py` reads every completed run, so rerunning it now would include the full-2026 run
and change the pinned `reports/calibration_2026.json`. Do not rerun it as part of a collection.

## 2026 player portraits via Leaguepedia

PandaScore serves one portrait per player, and many are older than 2026 (the year is usually in the file name). Leaguepedia
(lol.fandom.com) carries per-tournament player images, so `fetch_leaguepedia_images.py` fetches the 2026 ones.
It needs a Special:BotPasswords login in `.env` as `LEAGUEPEDIA_BOT_USERNAME` / `LEAGUEPEDIA_BOT_PASSWORD`
(never printed or written to an output).

```powershell
.\.venv\Scripts\python.exe fetch_leaguepedia_images.py --dry-run   # query + match only
.\.venv\Scripts\python.exe fetch_leaguepedia_images.py             # download (about 1 image/second)
```

- Queries `PlayerImages` for tournaments in 2026, then picks per player the newest headshot/profile file whose name or
  upload date says 2026. A 2026 tournament page that only links an older photo is rejected (`no_2026_file`).
- Matching is by nickname, never by guessing: unique name + agreeing team is `HIGH`; unique name with another team is
  `MEDIUM`; a name found only in the wider catalog is `LOW`; ambiguous names go to `review`.
- Files land in `data/assets/players-2026/<nickname>.<ext>` (same naming rule as `players/`), PandaScore originals stay
  untouched. `data/assets/leaguepedia_player_images_2026.json` holds source URL, file, team, tournament, upload time and
  confidence; `data/assets/mapping/player_images_2026.json` pairs each PandaScore ID with its 2026 path and the previous path.
- Internal research cache only: the images belong to their rightsholders, and the licence field is recorded when present.

## Historical match stats via Oracle's Elixir

The publisher's downloads page (https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads) links a public Google
Drive folder. `fetch_oracles_elixir.py` reads that link from the page itself, lists the folder and downloads the yearly
files into `data/import/oracles-elixir/snapshots/<UTC stamp>/` with `provenance.json` (URLs, Drive file id, bytes,
SHA-256, the terms shown on the page). A snapshot is never overwritten. When Drive answers "Quota exceeded" (too many
anonymous downloads of a popular file) the script records `BLOCKED` in `attempts.jsonl` and stops; it does not work
around the limit. A manual browser download into the same folder works just as well
(`<year>_LoL_esports_match_data_from_OraclesElixir.csv`).

```powershell
.\.venv\Scripts\python.exe fetch_oracles_elixir.py --years 2025 2026   # one attempt per year; exit 3 when blocked
.\.venv\Scripts\python.exe import_oracles_elixir.py        # validate, import every yearly file found (year from the name)
.\.venv\Scripts\python.exe reconcile_identities.py         # identity map, participation, reports/oracles_elixir_2026_stats.json
```

The import validates the file before writing anything. It rejects:
- a file that is not UTF-8 CSV or is outside 1 byte to 1 GB;
- missing required columns (`gameid`, `participantid`, `date`, `league`, `position`, `playername`, `teamname`);
- unparseable dates;
- a wrong reporting period (more than 5% of rows outside the file's year).

It keeps every row exactly as read (`oe_raw_rows`, content-addressed, so a newer download stores only changed rows)
and one `oe_source_files` row per file version. Each reporting year is its own series of versions: the current tables
union the newest version of every year, so importing 2026 never drops 2025, and a key found in two years' files keeps
the row dated in its own year (`oe_key_collisions`). Current tables:
- `oe_player_games`: source fields;
- `oe_player_game_stats`: statistics as published; NULL where the file has no column;
- `oe_team_games`.

The starter's `oracle_player_game_raw` table is still written for `report_2026.py`.

`reconcile_identities.py` never links on a handle alone. An Oracle's Elixir identity maps to a PandaScore player only
when all four agree:
- the handle;
- a team alias proven by roster overlap;
- a registration window that contains the game date;
- the role.

The result is `xp_identity_map` (`HIGH_CONFIDENCE_MATCH` / `REVIEW_REQUIRED` / `UNMATCHED`, with a reason), with
evidence per row in `xp_row_matches`. Only high-confidence identities write `xp_participation_2026`, which sets
`ps_player_index_2026.match_participation_verified_2026`. The provider's current team is never used, and nothing
assigns abilities, ratings, academy or reserve status. Details:
[`reports/ORACLES_ELIXIR_2026_INTEGRATION.md`](reports/ORACLES_ELIXIR_2026_INTEGRATION.md).

## Player performance and research ratings, 2023-2026

Per-game player statistics come from Leaguepedia's Cargo tables (source C, `fetch_leaguepedia_stats.py`, logged-in bot
from `.env`; about 1,450 paced requests for January 2023 to the as-of day) and, when imported, Oracle's Elixir.
`fetch_leaguepedia_reference.py` adds tournament placements (achievements) and player birthdates (age, potential, aging;
about 100 requests). PandaScore 2023-2025 stage rosters (`registrations-2023-2024` and `registrations-2025` presets,
17 requests) give the identity evidence for the earlier seasons.

```powershell
.\.venv\Scripts\python.exe sync_pandascore.py --preset registrations-2023-2024 --max-pages 20 --max-requests 40
.\.venv\Scripts\python.exe sync_pandascore.py --preset registrations-2025 --max-pages 20 --max-requests 30
.\.venv\Scripts\python.exe normalize_pandascore.py
.\.venv\Scripts\python.exe fetch_leaguepedia_stats.py --since 2023-01 --cutoff 2026-10-14   # raw pages in data/raw/leaguepedia
.\.venv\Scripts\python.exe fetch_leaguepedia_stats.py --normalize                            # offline: lp_games, lp_player_games
.\.venv\Scripts\python.exe fetch_leaguepedia_reference.py --since 2023 --cutoff 2026-10-14  # results and birthdates
.\.venv\Scripts\python.exe fetch_leaguepedia_reference.py --normalize
.\.venv\Scripts\python.exe reconcile_identities.py                                           # xp_* (OE) and lpx_* (Leaguepedia)
.\.venv\Scripts\python.exe build_performance.py                                              # pf_* normalized player-games
.\.venv\Scripts\python.exe rate_players.py                                                   # data/ratings/* (all catalogue players)
.\.venv\Scripts\python.exe validate_ratings.py                                               # temporal validation (about 1-2 hours)
.\.venv\Scripts\python.exe ratings_report.py                                                 # data/ratings/top100_report.html
```

Overall bands (model v2): ELITE 90-93 (needs an international final or two tier-a titles in the window), VERY_GOOD
83-89, AVERAGE 74-82, INEXPERIENCED 55-73, AMATEUR_ACADEMY 30-54. Potential exists up to age 25; from 29 the Overall
falls at most 2 points a year (3 from 31).

Everything after the fetches is offline and repeatable. Outputs stay in git-ignored `data/ratings/`: the catalogue
ratings (one row per PandaScore player), evidence, unresolved source identities, six Top 100 lists, the manifest (model
version, source hashes, content hash, as-of day) and the local HTML report (portraits and logos by relative path, never
embedded; do not publish it). Method and aggregate results:
[`reports/PLAYER_RATING_MODEL.md`](reports/PLAYER_RATING_MODEL.md) and
[`reports/PLAYER_RATING_COVERAGE.md`](reports/PLAYER_RATING_COVERAGE.md). This is a research
model: it changes nothing in `content/`, saves or the game's generator.

## Automation with Windows Task Scheduler

Only after smoke-running manually and confirming provider permission:

1. Open `taskschd.msc` > Create Basic Task > Daily.
2. Action: Start a program.
3. Program: `powershell.exe`.
4. Arguments: `-NoProfile -ExecutionPolicy Bypass -File "C:\Management Game\tools\lol-data-ingestion\run_sync.ps1" -MaxPages 2`.
5. Start in: `C:\Management Game\tools\lol-data-ingestion`.
6. Confirm a task run, UTC snapshot, manifest status, data quality, and token quota.

The scheduler invokes only the PandaScore fixtures/catalog connector; Oracle's Elixir, Liquipedia or GRID automatic access requires separate documented rights/authorized adapters.

## Next stage before feeding the game

Implement a **separate** normalization/export pipeline: identity crosswalk, duplicate handling, roster-membership timestamps, hierarchy `Organization → Team → Person`, historical competition/game rows, per-field provenance, confidence/unknown, as-of snapshot, validity/license review, then versioned/exported definitions. Do not change live `Person`/save DTOs or automatically distribute real LoL entities. Keep unlicensed images/logos out.

## Compliance

Use only source/API access allowed by the provider. PandaScore endpoint access and rate limits depend on plan. Liquipedia prohibits automated scraping of ordinary HTML pages (use authorized APIs); Leaguepedia/Fandom requires its own access/rights review. Riot currently has restrictions against games using Riot IP; a data subscription does not confer game IP rights. This starter is for controlled research only.

## Evidence of participation during 2026 (not present-day active roster)

After importing the Oracle's Elixir CSV:

```powershell
.\.venv\Scripts\python.exe report_2026.py --year 2026
```

Outputs `data/reports/participation_2026.csv` with observed source names, associated team/league, distinct game count, and latest observed game date. **Rows are handle-team-league groups, not deduplicated people.** A player who played in January and has since retired will still appear; a signed reserve/academy member without recorded matches may be absent. Resolve identities and current-roster membership separately using time-stamped reliable roster evidence, and retain `unknown` for unresolved cases.
