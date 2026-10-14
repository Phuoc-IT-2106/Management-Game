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
.\.venv\Scripts\python.exe cache_logos.py                                    # offline report; see "Logos" below
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

**Logos.** `ps_image_refs` keeps each image URL exactly as supplied, with `value`/`null`/`empty`/`absent` states, and
marks every URL `LICENSE_REVIEW_REQUIRED`: PandaScore's terms grant no rights in third-party logos. `cache_logos.py`
fetches nothing until run with `--license-approved "<who, scope, date>"`; it then caches one logo per team and
league in git-ignored `data/assets/{team,league}-logos/` (provider CDN hosts only, signature-checked PNG/JPEG/GIF/WebP,
SHA-256, bounded retries). Never copy those files into the game, a content pack or the repository.

**Calibration note.** `calibrate.py` reads every completed run, so rerunning it now would include the full-2026 run
and change the pinned `reports/calibration_2026.json`. Do not rerun it as part of a collection.

## Historical match stats via Oracle's Elixir

Manually download the 2026 match CSV from the publisher's downloads page under terms appropriate to your use:
https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads

Save it as `data/import/oracles-elixir/2026_LoL_esports_match_data_from_OraclesElixir.csv` (git-ignored), then:

```powershell
.\.venv\Scripts\python.exe import_oracles_elixir.py        # validate, import (newest CSV in data/import/oracles-elixir)
.\.venv\Scripts\python.exe reconcile_identities.py         # identity map, participation, reports/oracles_elixir_2026_stats.json
```

The import validates the file before writing anything. It rejects:
- a file that is not UTF-8 CSV or is outside 1 byte to 1 GB;
- missing required columns (`gameid`, `participantid`, `date`, `league`, `position`, `playername`, `teamname`);
- unparseable dates;
- a wrong reporting period (more than 5% of rows outside 2026).

It keeps every row exactly as read (`oe_raw_rows`, content-addressed, so a newer download stores only changed rows)
and one `oe_source_files` row per file version. Current tables come from the newest version:
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
