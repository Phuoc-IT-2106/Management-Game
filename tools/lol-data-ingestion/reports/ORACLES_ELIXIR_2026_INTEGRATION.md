# Oracle's Elixir 2026 — player participation integration

**Question:** which players registered by PandaScore for 2026 actually played professional games in 2026?

**Answer: not measured yet.** No Oracle's Elixir CSV is on this machine, so **no real import has happened** and
`match_participation_verified_2026` is `unknown` for all 2,331 registered players. The importer and the identity
reconciliation are built and tested on synthetic fixtures only. Every Oracle's Elixir figure below is "not measured"
until a real file is imported. Synthetic data was never used for real players.

| | |
|---|---|
| Status | `NO_SOURCE_FILE` (see [`oracles_elixir_2026_stats.json`](oracles_elixir_2026_stats.json)) |
| Checked | 2026-10-13 and 2026-10-14: `data/import/oracles-elixir/` is empty, and the user's Downloads folder has no Oracle's Elixir file (names only; no file was opened) |
| Official source | https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads (a JavaScript page: its links and terms could not be read automatically) |
| Staging DB | `data/staging.sqlite3`: no `oe_*` or `xp_*` tables; `PRAGMA quick_check` ok; PandaScore tables and raw snapshots untouched |
| Calibration | `calibrate.py` was not rerun; `calibration_2026.json` (pinned by content SHA-256) is unchanged |

## 1. How to run the real import

1. Download `2026_LoL_esports_match_data_from_OraclesElixir.csv` from the official source, under terms that fit the
   intended use.
2. Save it in `tools/lol-data-ingestion/data/import/oracles-elixir/` (git-ignored).
3. Run, from `tools/lol-data-ingestion`:

```powershell
.\.venv\Scripts\python.exe import_oracles_elixir.py     # validates, then imports the newest CSV in that folder
.\.venv\Scripts\python.exe reconcile_identities.py      # identity map, participation, stats JSON
.\.venv\Scripts\python.exe -m unittest discover -s tests
```

Then fill §3–§5 of this report from `reports/oracles_elixir_2026_stats.json`. Both scripts are offline: no API call.

## 2. What the pipeline does

**Validation before anything is written** (`import_oracles_elixir.py`). A file is rejected, with nothing written, when:
- it is not a UTF-8 `.csv`, or is outside 1 byte to 1 GB;
- the header is missing or repeats a column;
- a required column is missing: `gameid`, `participantid`, `date`, `league`, `position`, `playername`, `teamname`;
- a row has the wrong field count, a non-numeric `participantid`, an empty `gameid` or an unparseable date;
- more than 5% of rows fall outside 2026 (for example last year's file).

**Raw observations are kept separate from derived features.**

| Table | Holds |
|---|---|
| `oe_source_files` | one row per file version: SHA-256, size, header, missing expected columns, period, duplicate and invalid-position counts |
| `oe_raw_rows` | every CSV row exactly as read, content-addressed, so a newer download stores only the rows that changed |
| `oe_row_observations` | file, row number and duplicate marker for every row |
| `oe_player_games` | source fields of player rows: game, player, team, league, date, position, champion, result, patch, length |
| `oe_player_game_stats` | statistics as published: K/D/A, CS, gold, damage, vision, early-game values at 10 and 15 minutes. A column the file lacks stays NULL and is listed, never filled |
| `oe_team_games` | team rows (participant 100 and 200) |

Ids stay provider-scoped as given (`gameid`, `oe:player:…`, `oe:team:…`). No internal person id is created.

**Identity reconciliation** (`reconcile_identities.py`). A handle alone never establishes identity. An Oracle's Elixir
identity maps to one PandaScore player only when four kinds of evidence agree:
- **handle:** the normalized handle (NFKC, casefold, letters and digits) equals a name PandaScore reported for that
  player, in the catalogue or in any observed stage roster;
- **team:** the OE team maps to the PandaScore team through a high-confidence alias. Aliases come from roster overlap
  only (at least 3 overlapping players, at least twice the runner-up, and a compatible name unless 4 or more overlap).
  Names alone never link teams;
- **window:** the player is registered for that team on a 2026 stage whose dates (±2 days) contain the game date. The
  provider's current team is never used as evidence about earlier months;
- **role:** the OE position equals the registered role.

| Status | Meaning |
|---|---|
| `HIGH_CONFIDENCE_MATCH` | exactly one PandaScore player meets all four on at least one row, no row is ambiguous, and no other OE playerid claims that player |
| `REVIEW_REQUIRED` | a handle matches but evidence is missing, conflicting or shared. The reason is recorded |
| `UNMATCHED` | no PandaScore name matches, or the rows carry no handle |

Only high-confidence identities with games dated in 2026 write `xp_participation_2026`, and only that table sets
`ps_player_index_2026.match_participation_verified_2026 = verified`. Matched and unresolved rows are both kept
(`xp_row_matches`, `xp_identity_map`, `xp_team_aliases`).

## 3. Coverage

| Measure | Value |
|---|---|
| CSV source and reporting period | not measured: no file |
| Player-game rows | not measured |
| Unique players / teams / competitions | not measured |
| High-confidence identity matches | not measured |
| Ambiguous (review) identities | not measured |
| Unmatched observations | not measured |
| Coverage by competition | not measured |
| Missing statistical fields | not measured |
| Registered players with verified 2026 participation | **0 of 2,331** (none can be verified without the file) |

**PandaScore side, ready for the join** (measured 2026-10-13 on the full-2026 snapshot):

| Measure | Value |
|---|---:|
| Registered players with stage windows | 2,331 |
| … with a provider-reported name | 2,331 |
| … with several provider-reported names (aliases) | 0 |
| Normalized handles shared by several registered players | 23 (47 players) |
| Catalogue handles shared by several players | 333 |
| Teams on 2026 registrations | 363 |
| Stage windows | 278 |

The 47 registered players on shared handles are the main collision risk. The team and window evidence must separate
them. Where it cannot, they stay `REVIEW_REQUIRED`.

## 4. Academy and reserve

Neither provider marks academy or reserve status:
- PandaScore has no field for it.
- Oracle's Elixir lists leagues, not roster tiers.

Status is not inferred from a league name. After a real import, "registered without confirmed participation" will
**not** mean reserve or never fielded. Oracle's Elixir may simply lack those games, or the identity may be under review.
The stats file counts these players separately, with that caveat. `academy_verified` and `reserve_verified` stay
`unknown` for everyone.

## 5. Data-use limits

- Research only. Nothing enters `content/`, saves, Domain models or ratings, and no real person enters the game.
- The CSV, the database and the exports stay in git-ignored `data/`. Reports hold aggregates only: league codes and
  counts, with no person names, handles, person ids or team names. A test enforces this for the stats file.
- The download terms of Oracle's Elixir could not be read automatically. The user should confirm the terms when
  downloading. PandaScore data remains under its terms (6.3–6.4, no redistribution).

## 6. Tests actually executed

`.venv/Scripts/python.exe -m unittest discover -s tests`: **47 tests pass** (2026-10-14). Of these, 11 cover this
integration (`tests/test_oracles_elixir.py`, synthetic fixtures only):

| Required case | Test |
|---|---|
| Repeatable import; duplicate game/player rows; reporting period | `test_validation_and_repeatable_import` |
| Newer file version stores only changed rows | `test_a_newer_version_stores_only_changed_rows` |
| Rejections write nothing (missing column, last year's file, not CSV, not UTF-8) | `test_rejections_write_nothing` |
| Missing statistical columns stay NULL | `test_missing_statistics_columns_stay_null` |
| Team alias ambiguity; names alone never link | `test_team_aliases_need_roster_overlap_not_names` |
| Missing identities, date mismatch, role mismatch, invalid position, current team without registration, changed handle | `test_identity_outcomes` |
| Transfers during the year; participation only from high-confidence identities; no academy, reserve or roster inference | `test_participation_only_from_high_identities` |
| Handle collisions: two OE ids for one player, two players for one handle | `test_two_oe_ids_claiming_one_player_are_held_for_review`, `test_same_handle_on_the_mapped_team_is_not_forced` |
| Idempotent rebuild, SQLite integrity, PandaScore raw snapshots unchanged | `test_rebuild_is_idempotent_intact_and_leaves_raw_snapshots_alone` |
| Stats file has counts but no names | `test_statistics_report_counts_without_names` |

Scale check (2026-10-13, on a copy of the staging DB, synthetic rows): 100,000 player rows and 20,000 team rows
(30.7 MB) imported in 28 seconds, and reconciliation ran end to end. It verified no participation, as expected: the
synthetic handles match no real name.
