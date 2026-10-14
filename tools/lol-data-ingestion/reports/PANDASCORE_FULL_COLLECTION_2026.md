# PandaScore Free API — full LoL collection, 2026

**Question:** can we hold every LoL record the Free plan lists (players, teams, leagues) and everything that touches
2026 (series, stages, matches, stage rosters, image URLs), complete and reconciled?

**Answer:** yes, for the scope defined in §2. All 19 queries paged to their end, and every query's unique ids equal its
`X-Total`. No total moved during the crawl and no id repeated. Eight cross-checks passed. What the Free plan does not
supply is listed in §8 and is **not** collected: individual match participation, academy or reserve status,
birthdays on the player endpoint, and cached logos (a licence decision is pending).

| | |
|---|---|
| Collection run | `data/raw/pandascore/20261013T035908Z/` (git-ignored): 181 raw pages and a `manifest.json` with SHA-256 per page |
| Probes | `data/raw/discovery/20261013T034655Z/` (19 scope probes), `20261013T041452Z/` (5 roster-endpoint checks) |
| Observed | 2026-10-13, 03:46–04:15 UTC |
| Requests | **205** against a budget of 300: 19 probes, 181 for the collection, 5 roster checks. No retry and no HTTP 429 |
| Aggregates | [`pandascore_full_collection_2026_stats.json`](pandascore_full_collection_2026_stats.json): counts and status words only, with no names or person ids |
| Reproduce | See the README section "Full 2026 collection". `normalize_pandascore.py`, `collection_report.py` and `cache_logos.py` (without approval) run offline |

Everything below is aggregate. The data itself stays in git-ignored `data/` and is not redistributed (PandaScore terms
6.3–6.4). Cite it as "Source: PandaScore".

---

## 1. Collection result

| Collection | Queries | Provider total (`X-Total`) | Collected (unique ids) | Requests | Status |
|---|---|---:|---:|---:|---|
| Players (catalogue) | `/lol/players?sort=id` | 9,915 | 9,915 | 100 | **COMPLETE** |
| Teams (catalogue) | `/lol/teams?sort=id` | 1,948 | 1,948 | 20 | **COMPLETE** |
| Leagues (catalogue) | `/lol/leagues?sort=id` | 136 | 136 | 2 | **COMPLETE** |
| Series touching 2026 | begins in 2026 (108) + began 2025, ends 2026 (1) + spans 2026 (0) | 109 | 109 | 4 | **COMPLETE** |
| Series labelled `year=2026` | `filter[year]=2026` | 106 | 106 | 2 | **COMPLETE** |
| Stages (tournaments) touching 2026 | begins in 2026 (278) + began 2025 (1) + spans (0) | 279 | 279 | 5 | **COMPLETE** |
| Matches 2026, every status | `/lol/matches?range[scheduled_at]=2026` | 3,946 | 3,946 | 40 | **COMPLETE** |
| Checks (§2) | 2 empty-window checks, 6 running/upcoming listings | — | 8/8 pass | 8 | — |
| **Total** | 19 queries | | | **181** | |

The 2026 series set used downstream is the union of the two series rows, 113 series:
- 102 are both in calendar 2026 and labelled 2026;
- 7 run in calendar 2026 under another label (6 promotion series labelled 2027, 1 labelled 2025);
- 4 are labelled 2026 but ran in Sep–Oct 2025 (qualifiers for the 2026 season).

## 2. Scope and method

- **Windows.** The 2026 window is `2026-01-01T00:00:00Z` to `2026-12-31T23:59:59Z`. A series or stage touches 2026 if
  it begins in the window, began earlier and ends in it, or began earlier and ends later. The scope probe showed that
  two `range[...]` filters combine as **AND**: "begins before 2026 AND ends in 2026" returned 1 stage, not the union.
  So the three windows are disjoint, and their `X-Total`s add up to the scope total.
- **Matches.** `/lol/matches` (all statuses) holds past + upcoming: 3,850 + 58 = 3,908 with `begin_at` in 2026.
  - `begin_at` is null on 38 cancelled matches, so the crawl filters on `scheduled_at`, which every match has
    (3,946).
  - Two checks, "begin_at in 2026 AND scheduled_at before 2026" and "… after 2026", both returned 0. So the scheduled
    window covers the begin window as well.
  - The 2026-10-09 run had held 3,829 past matches. Every one of them is in the new set.
- **Ordering.** Every query is sorted by `id`. Ids are immutable, and the probe confirmed ascending order on players
  and teams. Records created mid-crawl can therefore only append to the last page; a total that moved would have marked
  the query incomplete.
- **Cross-checks.** What the provider lists as running or upcoming must already be inside the windows:
  - series: 5 running, 1 upcoming;
  - stages: 5 running, 3 upcoming;
  - matches: 0 running, 58 upcoming.

  Nothing was outside them. Stages without dates cannot be windowed, and none appeared in these listings.
- **Not fetched.**
  - Per-player and per-team detail endpoints. The listings already return every field the Free plan gives, so
    thousands of extra requests would add nothing.
  - Per-stage rosters. The embedded `expected_roster` is identical to `/tournaments/{id}/rosters` on 5 of 5 running or
    upcoming stages checked (242 rows, 0 differences).
  - Game or player statistics endpoints. These belong to the Historical plan.

## 3. Players

### A. Provider catalogue — every player listed by `/lol/players` (9,915)

| Field | Present | Share |
|---|---:|---:|
| Handle (`name`) and `slug` | 9,915 | 100% |
| First and last name | 8,843 | 89.2% |
| Nationality | 8,464 | 85.4% |
| Role | 9,654 | 97.4% |
| Provider current team | 2,774 | 28.0% |
| `image_url` | 5,598 | 56.5% |
| All eight tracked fields | 1,297 | 13.1% |

Breakdowns:
- **Roles:** jungle 2,062, support 1,965, mid 1,932, bot 1,852, top 1,843; 261 null.
- **Nationality, top 10:** CN 1,109, KR 745, BR 435, TR 383, FR 354, PL 307, ES 304, US 291, DE 286, VN 284; 1,451 null.
- **Provider `active` flag:** true on 8,512 (85.9%). It is **not** evidence of professional activity.
  - Of the players with a provider current team, 1,114 are on no 2026 stage roster.
  - Presence in the catalogue says nothing about 2026.
- **Not returned by `/lol/players`:** birthday, age, salary, contract, rating, potential. They stay unknown and are not
  normalized. Birthdays do appear inside embedded roster objects, but their licence is unconfirmed (API discovery §3),
  so they stay in the raw payloads only.

### B. 2026-relevant index — players on the expected roster of a stage touching 2026 (2,331)

| Classification | Value | Players | Evidence |
|---|---|---:|---|
| `registered_in_2026` | verified | 2,331 | Listed on ≥ 1 expected roster of a stage touching 2026 |
| `current_roster_verified_as_of` | 2026-10-13, with team | 303 | Listed on a stage that had not ended when observed (running or upcoming); the provider's current team agrees for 270 of them |
| `match_participation_verified_2026` | unknown | 2,331 | Matches name teams, not lineups; no individual source imported |
| `academy_verified` / `reserve_verified` | unknown | 2,331 | No such provider field; roster size does not identify substitutes |

More about the index:
- Every one of the 2,331 is in the catalogue.
- **Stages per player:** 1: 649, 2: 412, 3: 250, 4: 203, 5 or more: 817.
- **Roles at registration:** jungle 496, support 462, mid 460, top 460, bot 443; 10 null.
- **Nationality:**
  - top: KR 230, PL 134, TR 124, FR 119, CN 105, BR 103;
  - 54 null.
- **Gaps in the index:**
  - 671 players have no provider current team;
  - 1,205 have no `image_url`.

**Membership kinds are kept apart.**

| Kind | Table | Count |
|---|---|---|
| Provider-reported current membership, as of the run | `team_membership_observations` | 2,773 `team.players` links and 2,774 `player.current_team` links; 2,773 agree both ways |
| Tournament registration, dated by its stage | `tournament_registrations`, `ps_registrations` | — |
| Historical membership | Repeated observations; the earlier runs are kept | — |
| Unknown | Everything else | — |

Observation dates are never used as transfer dates.

## 4. Teams and competitions

**Teams (1,948).**
- **Rosters:** 736 carry a non-empty provider roster and 1,212 an empty one. Sizes: 1–4: 423; 5: 150; 6–7: 147;
  8+: 16.
- **Images and location:** 1,640 have `image_url`, 174 `dark_mode_image_url` and 1,347 a `location`.
- **In 2026 competitions:** 363 teams (on stage rosters or as match opponents), all in the catalogue; 357 of them have an
  image URL.
- Region, tier, academy and active status are not team fields and are not inferred.

**Leagues (136).**
- 118 have `image_url`.
- 44 leagues have 2026 stages, and 33 of those have an image.

**Stages touching 2026 (279).**

| Measure | Values |
|---|---|
| Tier | S 13, A 50, B 13, C 113, D 90 |
| Region | WEU 102, ASIA 77, EEU 42, SA 24, NA 21, ME 11, OCE 2 |
| Type | online 185, offline 74, both 20 |
| Fields present | winner 130, prize pool 33, country 179 |
| Without teams | 1 S-tier playoff stage starting 2026-11-03, not yet seeded |

There is no status field; completion shows only through dates, `winner_id` and match statuses.

**Registrations.**
- 9,764 (stage, team, player) rows, covering 2,331 players and 363 teams.
- All 1,779 team-stage entries list players. Sizes: 5: 1,247; 6: 339; 7: 124; 8+: 69.
- **Compared with the 2026-10-09 observation:** 277 stages were seen in both runs. 16 rows were added since, and none
  removed.

**Matches 2026 (3,946).** Scheduled 2026-01-01 → 2026-11-14.

| Measure | Values |
|---|---|
| Status | finished 3,850, not started 58, cancelled 38 |
| Format | Bo1 1,092, Bo2 91, Bo3 2,064, Bo5 699 |
| Opponents | Both known on 3,892. Missing on 54: 48 not started (opponents to be decided), 6 cancelled |
| Results | A winner on 3,855, including 25 cancelled forfeit wins; 20 Bo2 draws |
| `detailed_stats` | True on 3,903; this is a flag, not data on this plan |
| Games referenced | 8,850: 8,574 finished, 276 not started |

**Cross-year overlap.**
- 1 series and 1 stage began in 2025 and ended in 2026; none spans the whole year.
- 7 series run in 2026 under a 2027 or 2025 label, and 4 labelled 2026 ran in 2025 (§1).

**Missing references.** All are 0 except one: a player's provider current team is not in the `/lol/teams` listing.
That team was last modified in 2023, so this is a provider inconsistency, not a crawl gap. The checks that came back 0:
- stage → league or series;
- series → league;
- match → stage, series or league;
- match opponent → team;
- registration → player or team;
- team member → player;
- 2026 stages missing a league or series id.

## 5. Logos and images

| Entity, field | URL present | Null | Cached | Failed |
|---|---:|---:|---:|---:|
| Team `image_url` | 1,640 | 308 | 0 | 0 |
| Team `dark_mode_image_url` | 174 | 1,774 | 0 | 0 |
| League `image_url` | 118 | 18 | 0 | 0 |
| Player `image_url` (metadata only) | 5,598 | 4,317 | — | — |

- **What is recorded.** Every URL is kept exactly as supplied. `ps_image_refs` tells a null value (`null`), a missing
  key (`absent`) and an empty string (`empty`) apart, and `image_cache` would add download failures. All 7,530 URLs
  are on `cdn-api.pandascore.co`.
- **Rights.**
  - PandaScore's terms make the database PandaScore's (6.2) and forbid passing on raw data (6.4).
  - They contain no clause about team or league logos, which are third parties' marks and artwork.
  - A public CDN URL is not a licence. Every URL is therefore `LICENSE_REVIEW_REQUIRED`, and **no logo was
    downloaded**.
- **Variants.** PandaScore documents `normal_` (800 px) and `thumb_` (200 px) prefixes for team logos on
  `cdn.pandascore.co`, but the API returns `cdn-api.pandascore.co` URLs. The variants are unverified for that host and
  were not constructed as facts.
- **Formats.** The URL suffixes are `.png` 1,862, no extension 67, `.webp` 2 and 1 other. The real format is known only
  from the bytes.
- **When a licence decision exists.** `cache_logos.py --license-approved "<who, scope, date>"` caches one original
  logo per team and league in `data/assets/{team,league}-logos/<id>.<ext>`. It:
  - accepts only the provider CDN hosts;
  - follows redirects only within them;
  - accepts PNG, JPEG, GIF or WebP by signature, up to 5 MB;
  - records SHA-256 and skips identical files;
  - retries transiently, with bounded backoff;
  - waits 1 s between requests.

  It costs about 1,760 CDN requests and none from the REST quota. Never copy those files into the game, a content pack
  or the repository.

## 6. Data quality

| Check | Result |
|---|---|
| Pagination reconciliation | 19/19 queries: unique ids equal `X-Total`; 0 duplicate ids; no total changed mid-crawl |
| Raw SHA-256 | 181/181 pages of the run re-verified. The normalizer verifies all 236 pages of the 4 completed runs before writing anything, and refuses an edited page (tested) |
| SQLite | `integrity_check = ok`; 0 duplicate observations; 0 observations without a payload |
| Idempotency | Two consecutive rebuilds gave an identical digest over 127,294 rows. `--resume` on the completed run fetched nothing |
| Interrupted-run recovery | Tested offline with synthetic APIs (request budget every 4 requests, quota floor, CLI partial → resume): every page fetched exactly once, and the manifest checkpointed after each page. The live run needed no resume |
| Token | Read from `.env` only. A scan of 1,289 files (tool, `data/`, `content/`, `docs/`) found it nowhere else; manifests and logs carry no token |
| Quota | 205 requests. `X-Rate-Limit-Remaining` went 980 → 958, reset to 997 at the hour, and ended at 837 |
| Tests | `python -m unittest discover -s tests`: 36 pass (17 existing, 19 new) |

## 7. Staging schema (`data/staging.sqlite3`, about 100 MB)

| Kind | Tables | Rule |
|---|---|---|
| Observations | `collection_runs`, `collection_queries`, `record_observations` (21,736), `record_payloads` (16,765 distinct payloads), `tournament_registrations`, `team_membership_observations` | Append-only, keyed by run |
| Current | `ps_players`, `ps_teams`, `ps_leagues`, `ps_series`, `ps_tournaments`, `ps_tournament_teams`, `ps_registrations`, `ps_matches`, `ps_match_games`, `ps_image_refs`, `ps_player_index_2026` | Rebuilt from the latest copy of each record (latest `modified_at`, ties to the later run). Each row names its run, query, raw file, observation time and payload SHA-256 |
| Starter | `provider_records` (UPSERT), `tournament_roster` | Unchanged |

Observations are append-only, so a later run can never erase an earlier one. Every provider field stays reachable:
`record_payloads` stores each distinct payload once, keyed by its SHA-256.

**Local CSV exports** (`data/reports/exports/`, git-ignored, 9 MB):
- the player catalogue and the 2026 index;
- teams and leagues;
- 2026 series, stages, stage teams, registrations, matches and games;
- image references.

## 8. Gaps and limits

1. **Individual participation in 2026 matches is unknown.** Free-plan matches name teams, not lineups. Team results
   never establish that a player played.
2. **Academy, reserve and substitute status are unknown.** There is no provider field for them.
3. **`current_roster_verified_as_of` covers 303 players only.** They are the players on stages that had not ended at
   observation. For everyone else, the provider's `current_team` is a mutable snapshot, not a verified roster.
4. **Logos are not cached** until a licence decision is recorded (§5).
5. **Ages and birthdays stay unknown in the normalized tables.** They are in raw embedded objects, with their licence
   unconfirmed.
6. **One provider inconsistency** (§4) is kept as reported.
7. **Do not rerun `calibrate.py` as part of a collection.** It reads every completed run, so a rerun would now include
   this run and change the pinned `calibration_2026.json`.
8. **The data is mutable.** The counts are as of 2026-10-13 04:15 UTC. Worlds 2026 stages, upcoming matches and
   rosters will change; a later run adds a new observation rather than overwriting this one.

## 9. Next step

Bring individual 2026 player-game evidence in from Oracle's Elixir, as research staging only.
1. Download the 2026 match CSV manually under its terms.
2. Import it with the existing `import_oracles_elixir.py` (`oracle_player_game_raw`).
3. Build a separate, reviewed identity crosswalk from Oracle player and team names to PandaScore ids. Match on handle
   **and** team **and** date within a registered stage window; mark unresolved and ambiguous rows `unknown`.

Only crosswalked rows may set `match_participation_verified_2026`. Nothing goes into `content/`, saves or Domain
models.
