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

## Historical match stats via Oracle's Elixir

Manually download the 2026 match CSV from the publisher's downloads page under terms appropriate to your use:
https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads

Then:

```powershell
.\.venv\Scripts\python.exe import_oracles_elixir.py "C:\path\to\2026_LoL_esports_match_data_from_OraclesElixir.csv"
```

Stores one raw `oracle_player_game_raw` row per `(gameid, participantid)` with exact CSV record JSON and CSV SHA256. It **does not** merge with a PandaScore player from shared handle, assign abilities, infer coach performance, or promise academy completeness.

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
