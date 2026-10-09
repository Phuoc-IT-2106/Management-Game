# PandaScore Free API — Discovery & 2026 Coverage (LoL)

**Question:** which parts of a 2026 LoL database can the PandaScore Free plan actually supply, and what is missing?

| | |
|---|---|
| Discovery run | `data/raw/discovery/20261008T162626Z/` (git-ignored; 19 raw responses + `manifest.json` with SHA-256) |
| Aggregates | [`api_discovery_2026_stats.json`](api_discovery_2026_stats.json) (counts and field paths only; no names, no payloads) |
| Reproduce | `.\.venv\Scripts\python.exe discover_api.py` (live, ≤25 requests) or `discover_api.py --analyze <run_dir>` (offline) |
| Observed at | 2026-10-08 16:26 UTC |
| Requests spent | 19 (all HTTP 200, no 429); `X-Rate-Limit-Remaining` 973 after the run |

Everything below is **observed in this sample** unless marked *(docs)*. Two pages per query is **not** a complete dataset. `X-Total` values are provider totals at the time of the request, not records we hold.

---

## 1. Endpoint access matrix

Authentication: `Authorization: Bearer <token>` header for every call; the token is read from `.env` and never logged.

| Endpoint | HTTP | Records (p1/p2) | `X-Total` | Free plan access |
|---|---|---|---|---|
| `GET /lol/leagues` | 200 / 200 | 100 / 36 | 136 | Confirmed |
| `GET /lol/series` | 200 / 200 | 100 / 100 | 867 | Confirmed |
| `GET /lol/tournaments` | 200 / 200 | 100 / 100 | 2,206 | Confirmed |
| `GET /lol/teams` | 200 / 200 | 100 / 100 | 1,947 | Confirmed |
| `GET /lol/players` | 200 / 200 | 100 / 100 | 9,905 | Confirmed |
| `GET /lol/matches/past` | 200 / 200 | 100 / 100 | 47,949 | Confirmed |
| `GET /lol/matches/past?range[begin_at]=2026…` | 200 / 200 | 100 / 100 | **3,828** | Confirmed |
| `GET /lol/tournaments?range[begin_at]=2026…` | 200 | 100 | **277** | Confirmed |
| `GET /lol/series?filter[year]=2026` | 200 | 100 | **105** | Confirmed |
| `GET /lol/players?filter[active]=true` | 200 | 100 | 8,502 | Confirmed |
| `GET /tournaments/{id}/rosters` (id 20315, tier S, 2026) | 200 | 4 rosters × 5 players | — | Confirmed |

Not tested: `/lol/matches/upcoming`, `/lol/matches/running`, `/lol/games/{id}`, any per-game or per-player statistics endpoint, `/lol/champions`/items. *(docs)* Post-match statistics belong to the "Historical & Post-Match Data" plan.

## 2. Pagination verification

| Check | Result |
|---|---|
| Parameters | `page` + `per_page` (starter) and `page[number]` + `page[size]` *(docs)* return **identical ids in identical order** (players page 2) |
| Page size | `X-Per-Page: 100` honoured everywhere; the last page is short (leagues p2 = 36) |
| Headers | `X-Page`, `X-Per-Page`, `X-Total`, `Link` present. Page 1 has `next`/`last`, a middle page has `first`/`prev`/`next`/`last`, and the last page has `first`/`prev` |
| Page 1 vs page 2 | Different for all 7 paged queries; **0 overlapping ids** |
| Null / duplicate ids | 0 null ids, 0 duplicates inside any page |
| Default ordering | **Descending `id`** for leagues, series, tournaments, teams, players. `/lol/matches/past` is not id-ordered: pages 1–2 span begin dates 2026-10-08 → 2026-09-23, i.e. most recent first (inferred, not documented) |
| Explicit sort | `sort=begin_at,id` works on matches and tournaments (2026 matches p1–p2 span 2026-01-01 → 2026-01-22) |
| Stable ids | Page-1 id sets of leagues/teams/players are identical across both sync snapshots (16:10Z, 16:15Z) and discovery (16:26Z) |

**Risk:** with the default newest-first order, records created during a long crawl shift every later page by offset, which can skip or duplicate rows. Crawl with an explicit ascending sort (`sort=id` or `sort=begin_at,id`) within a fixed `range[...]`, and check the collected id count against `X-Total`.

## 3. Field availability matrix

Legend: **VALUE** = present with value, **NULL** = key present with `null`, **EMPTY** = present but `[]`/`""`, **ABSENT** = key not in response, **NOT_TESTED**. Counts are over the sampled records (n).

### Players — `/lol/players` (n = 200)

| Requested field | Response field | Status |
|---|---|---|
| ID | `id` | VALUE 200 |
| Name / handle | `name` (handle), `slug` | VALUE 200 |
| Real name | `first_name` / `last_name` | VALUE 130 / 126, NULL 70 / 74 |
| Nationality | `nationality` (ISO alpha-2) | VALUE 190, NULL 10 |
| Role | `role` (`top`/`jun`/`mid`/`adc`/`sup`) | VALUE 200 |
| Current team | `current_team` {id, name, acronym, location, slug, image urls, modified_at} | VALUE 170, NULL 30 |
| Birthday / age | `birthday`, `age` | **ABSENT** (keys not returned) |
| Image URL | `image_url` | **NULL 200** |
| Active flag | `active` | VALUE 200 (provider flag) |
| Update stamp | `modified_at` | VALUE 200 |

The **same player object embedded elsewhere** carries more fields on the Free plan:

| Source | n players | `birthday` VALUE | `age` VALUE | `image_url` VALUE | `nationality` VALUE |
|---|---|---|---|---|---|
| `teams[].players[]` | 759 | 417 | 424 | 239 | 726 |
| `tournaments[].expected_roster[].players[]` | 7,147 | 5,820 | 5,876 | 4,778 | 7,133 |
| `/tournaments/20315/rosters` | 20 | 20 | 20 | 20 | 20 |

*(docs)* `birthday`/`age` are documented as Historical-plan fields. Their presence in embedded objects may be unintended or subject to change. **Do not depend on them without confirming the licence/plan terms.**

### Teams — `/lol/teams` (n = 200)

| Requested field | Response field | Status |
|---|---|---|
| ID | `id` | VALUE 200 |
| Name | `name`, `slug` | VALUE 200 |
| Acronym | `acronym` | VALUE 178, NULL 22 |
| Players / roster | `players[]` (full player objects) | VALUE 158, **EMPTY 42**; sizes: 0 (42), 1–4 (58), 5 (42), 6–7 (48), 8+ (10) |
| League / region reference | — | **ABSENT** (only `location`: country code, EMPTY 37) |
| Academy designation | — | **ABSENT**. Name heuristic ("academy", "youth", …) matched 8/200; unreliable |
| Image | `image_url` / `dark_mode_image_url` | VALUE 187 / 76 |

Consistency: for the 125 sampled players whose `current_team` was also in the sampled teams, `team.players[]` contained the player 125/125 times.

### Leagues (n = 136, complete) / Series (n = 200) / Tournaments (n = 200)

| Requested field | Leagues | Series | Tournaments |
|---|---|---|---|
| ID | `id` VALUE | `id` VALUE | `id` VALUE |
| Name | `name` VALUE | `full_name` VALUE 200; `name` EMPTY 154 | `name` VALUE |
| Dates | ABSENT | `begin_at`/`end_at` VALUE 200 | `begin_at`/`end_at` VALUE 200 |
| Season | ABSENT | `year` VALUE 200; `season` VALUE 124, NULL 76 | ABSENT (via `serie`) |
| Status | ABSENT | ABSENT | ABSENT (no status field; `winner_id` NULL 107) |
| Region | ABSENT | ABSENT | `region` VALUE 200 (ASIA, WEU, EEU, NA, SA, ME, OCE); `country` NULL 73 |
| Tier | ABSENT | ABSENT | `tier` VALUE 200 (s/a/b/c/d) |
| Participants | ABSENT | ABSENT (via `tournaments`) | `teams[]` VALUE 199; `expected_roster[]` {team, players[]} VALUE 199 |
| Parent links | `series[]` VALUE 135 | `league_id`, `tournaments[]` | `league_id`, `serie_id`, `matches[]` |
| Other | `url` NULL 98, `image_url` NULL 18 | `winner_id` NULL 106 | `prizepool` NULL 175, `type` (online/offline), `has_bracket` |

In all 200 sampled tournaments, `teams[]` ids equal `expected_roster[].team` ids. Players per expected-roster team: 5 (896), 6 (252), 7 (98), 8–12 (52).

### Matches — `/lol/matches/past` (n = 200)

| Requested field | Response field | Status |
|---|---|---|
| ID | `id` | VALUE 200 |
| Date | `begin_at` / `end_at` / `scheduled_at` / `original_scheduled_at` | VALUE 195 / 195 / 200 / 200 (NULL 5 = cancelled) |
| Opponents | `opponents[]` {type: `Team`, opponent: team object} | VALUE 199 (2 each) |
| Tournament | `tournament` (+ `league`, `serie`, ids) | VALUE 200 |
| Results | `results[]` {team_id, score}; `winner`, `winner_id` | VALUE 199 |
| Status | `status` | VALUE 200 (`finished` 195, `canceled` 5) |
| Game references | `games[]` {id, position, status, winner{id,type}, length, begin_at, end_at, …} | VALUE 200 (391 games) |
| Format | `match_type` (`best_of`), `number_of_games` (1/3/5) | VALUE 200 |
| Player lineups / stats | — | **ABSENT** in match/game objects; stats endpoints NOT_TESTED |

## 4. Sample counts (records actually stored in raw)

| Dataset | Stored this run | Provider total (`X-Total`) |
|---|---|---|
| Leagues | 136 | 136 (complete) |
| Series (default / 2026) | 200 / 100 | 867 / 105 |
| Tournaments (default / 2026) | 200 / 100 | 2,206 / 277 |
| Teams | 200 | 1,947 |
| Players (default / active) | 200 / 100 | 9,905 / 8,502 |
| Past matches (default / 2026) | 200 / 200 | 47,949 / 3,828 |
| Tournament roster sample | 4 teams, 20 players | — |
| Staging SQLite (from sync) | leagues 100, teams 100, players 100 | — |

## 5. 2026 filtering feasibility

| Filter | Works | Observation |
|---|---|---|
| `range[begin_at]=2026-01-01T00:00:00Z,2026-12-31T23:59:59Z` on `/lol/matches/past` | Yes | 3,828 finished/cancelled matches dated 2026 (as of 2026-10-08) |
| Same range on `/lol/tournaments` | Yes | 277 tournaments starting in 2026; the sorted first page spans 2026-01-08 → 2026-03-31 |
| `filter[year]=2026` on `/lol/series` | Yes | 105 series. In the default (unfiltered) listing, 6 series with `year=2027` start in 2026, so `year` ≠ calendar year |
| `filter[active]=true` on `/lol/players` | Yes | 8,502 of 9,905 (86%) flagged active. **Too broad to mean "on a 2026 roster"** |
| `range[modified_at]` | *(docs)* yes on matches/players | Not tested; useful for incremental syncs |
| Teams / leagues | No date filter used | No date fields on teams or leagues |

Caveat: `range[begin_at]` excludes tournaments/series that started in late 2025 and ran into 2026. Add a second query on `range[end_at]` for full overlap.

## 6. Academy / roster coverage limitations

| Status to distinguish | What PandaScore Free gives | Verdict |
|---|---|---|
| **Played a match in 2026** | Matches name **teams** only (`opponents`), with no per-game lineups | Team-level: **feasible**. Player-level: **not available** here. Use Oracle's Elixir CSV (`import_oracles_elixir.py`) |
| **Roster published in 2026** | `tournaments[].expected_roster` (and `/tournaments/{id}/rosters`, identical for the sample) for 2026 tournaments | **Feasible** as "registered for tournament X", bounded by the tournament's `begin_at`/`end_at`. No per-player join/leave dates |
| **On a roster at date D** | `players.current_team` / `teams.players` = provider snapshot at fetch time; tournament rosters covering D | **Approximate only.** Needs snapshots over time or roster evidence covering D; not a contract record |
| **Academy / reserve / substitute** | No academy field, no starter/sub flag. Rosters of 6–12 imply subs but do not identify them. League/tier may hint at academy competitions | **Not determinable** from fields. Requires a curated mapping |
| **Unknown** | `current_team` NULL (30/200), empty `teams.players` (42/200), `active` flag over-broad | Must stay `unknown`; never infer from `/players` membership or `active` |

## 7. Database integrity results

| Check | Result |
|---|---|
| Sync snapshots `20261008T161016Z`, `20261008T161546Z` | `completed`; SHA-256 re-verified for every page |
| Discovery snapshot `20261008T162626Z` | `completed`; SHA-256 re-verified for all 19 responses |
| `staging.sqlite3` | `integrity_check = ok`; leagues/teams/players 100 each; 0 duplicate `(provider, collection, external_id)` |
| Rerun idempotency | Two identical sync runs produced the same 300 rows: `first_seen_at` from run 1, `last_seen_at` from run 2 |
| Discovery data in SQLite | Not stored. Discovery stays raw-only, so staging is unaffected |
| **UPSERT and roster history** | **History is lost in SQLite.** Offline demo on a DB copy: re-storing a player with a different `current_team` leaves one row whose payload is overwritten; only `first_seen_at`/`last_seen_at` survive. The previous `current_team` was recoverable **only** from the immutable raw snapshots |

Reconstructing history therefore depends on keeping every raw snapshot (never delete `data/raw/`) and on re-fetching the same records over time. Tournament `expected_roster` blocks are inherently dated by their tournament and are the best free source for roster history. A temporal roster model was **not** implemented, as instructed.

## 8. Rate-limit observations

- `X-Rate-Limit-Remaining` is present on every response and decreased by exactly 1 per request (991 → 973 over 19 requests). The hourly allowance appears to be 1,000 *(docs: 1,000/h for the Schedules, Results & Context plan)*.
- No 429 occurred. The script stops on 429 or when fewer than 50 requests remain. The quota reset time was not observed.
- At ≥4 s per request, the full 2026 core scope below (~46 requests) takes about 3–4 minutes.

## 9. Missing data and risks

1. **No player-level match participation** (lineups and stats) on the tested Free endpoints. "Played in 2026" for individuals needs Oracle's Elixir or a paid plan.
2. **`birthday`/`age`/`image_url`** are missing from `/lol/players` but present in embedded rosters; the plan/licence status is unclear (see §3).
3. **No academy/reserve/substitute designation** and no league/region on teams.
4. **No tournament or league status field**; completion must be inferred from dates, `winner_id` and match statuses.
5. **`active` flag is over-broad** (86% of all players).
6. **Offset-pagination drift** with default newest-first ordering during long crawls.
7. **SQLite UPSERT keeps only the latest payload**; history lives only in raw snapshots.
8. `series.year` is a season label, not the calendar year; `range[begin_at]` misses events that started in 2025.
9. Images and logos (`image_url`) carry no licence information; keep them out of the game.
10. The sample covers ≤200 records per collection. The coverage percentages above are **sample statistics**, not global ones.

## 10. Recommended next collection scope

A bounded "2026 core" crawl. Each query is sorted ascending, run inside a fixed date window, and checked against `X-Total`:

| Query | Pages (100/page) | Purpose |
|---|---|---|
| `/lol/leagues` (all) | 2 | League catalogue (complete at 136) |
| `/lol/series?filter[year]=2026` + `range[end_at]` overlap | ~2–3 | Seasons/splits |
| `/lol/tournaments?range[begin_at]=2026…&sort=begin_at,id` + `range[end_at]` overlap | ~3–4 | Tournaments **with `expected_roster`**, the main 2026 roster evidence |
| `/lol/matches/past?range[begin_at]=2026…&sort=begin_at,id` | ~39 | Team-level 2026 results |
| **Total** | **~46–48 requests** | About 5% of one hour's quota |

Defer: the global `/lol/players` list (~100 requests) and `/lol/teams` list (~20). Fetch only the teams and players referenced by 2026 tournaments if the embedded objects prove insufficient.

Before that crawl, `sync_pandascore.py` needs small, reviewed additions. Today it supports neither query filters/sort nor a check against `X-Total`. Separately, store `expected_roster` as dated tournament-roster rows instead of relying on the UPSERT payload. Join player-level 2026 participation from Oracle's Elixir afterwards, through a separate identity crosswalk.
