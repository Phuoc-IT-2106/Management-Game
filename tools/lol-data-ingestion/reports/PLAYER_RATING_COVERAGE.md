# Player ratings 2023–2026 — coverage, results and quality gates

**Status: RESEARCH OUTPUT, aggregates only.** Window: games from 2023-01-01 and before 2026-10-14 (UTC). Model
`lol-research-rating 2.0.0` ([method](PLAYER_RATING_MODEL.md)). Player-level files stay in git-ignored `data/ratings/`;
this document carries counts, league names and distributions, never a person.

## 1. Acquisition (Gate A)

| Source | Role | Status (2026-10-14) |
|---|---|---|
| Leaguepedia Cargo (`fetch_leaguepedia_stats.py`) | per-game player statistics, source C | **Collected**: runs `20261014T113236Z` (2025–2026, 687 requests) and `20261014T154104Z` (2023–2024, 764 requests); 92 of 92 month-table windows complete (count before = count after = unique keys) |
| Leaguepedia Cargo (`fetch_leaguepedia_reference.py`) | tournament placements, birthdates | **Collected**: run `20261014T155543Z`, 101 requests: 34,345 placements (2023: 8,439; 2024: 9,209; 2025: 10,860; 2026: 5,837) and 9,409 birthdates; every query complete |
| Oracle's Elixir (`fetch_oracles_elixir.py`) | per-game statistics with timeline (preferred) | **Blocked**: official page → Drive folder → files found, but Drive returned "Quota exceeded" on all five attempts (11:23–14:28 UTC); not worked around. No OE row is used |
| PandaScore (existing staging) | catalogue, teams, 2026 stage rosters | reused, no recrawl |
| PandaScore `registrations-2025`, `registrations-2023-2024` | earlier stage rosters for identity | **Collected**: 6 + 11 requests: 215 + 109 series, 473 + 283 stages; the 2026 player index's registration columns were unchanged by the re-normalization (same hash) |

**Leaguepedia coverage**

| | 2023 | 2024 | 2025 | 2026 (to 2026-10-13) |
|---|---:|---:|---:|---:|
| Games | 14,772 | 14,577 | 14,117 | 11,877 |
| Player-game rows | 147,720 | 145,770 | 141,170 | 118,770 |
| Complete games | 14,765 | 14,575 | 14,101 | 11,877 |
| Duplicate games removed (same day, same ten champions) | 9 | 2 | 0 | 2 |
| Competitions (source league names) | 99 | 111 | 100 | 78 |
| Games tied to a PandaScore stage | 9,092 | 8,790 | 8,623 | 8,495 |

Missing values: damage to champions blank on 110 rows and vision score on 173 (kept NULL); kills, deaths, assists,
gold and CS are never blank. No timeline statistics, ward counts or first blood: every rated record carries
`EarlyGame:EARLY_TIMELINE_UNAVAILABLE` and `Macro:WARD_COUNTS_UNAVAILABLE`.

Games per competition tier (PandaScore tier of the stage, else the competition's most common tier):

| Tier | 2023 | 2024 | 2025 | 2026 |
|---|---:|---:|---:|---:|
| s (international) | 174 | 184 | 232 | 165 |
| a | 1,819 | 1,730 | 2,145 | 2,232 |
| b | 2,520 | 1,348 | 457 | 539 |
| c | 2,733 | 3,100 | 3,153 | 3,095 |
| d | 3,294 | 3,869 | 4,152 | 3,062 |
| unknown (PandaScore does not list the league) | 4,232 | 4,346 | 3,978 | 2,784 |

**Use limits.** Leaguepedia text is CC BY-SA; game statistics belong to Riot Games (the Oracle's Elixir page states the
same for its files). Birthdates are personal data: used locally for age only, never reported per person; the licence of
the PandaScore roster birthdays is unconfirmed. PandaScore terms 6.3–6.4 (no redistribution) apply to the catalogue.

## 2. Identity reconciliation (Gate B)

Leaguepedia person ids (canonical player pages, renames resolved by redirects) mapped to PandaScore players with the
four-evidence rule (handle + per-year team alias by roster overlap + registration window + role), 2023–2026:

| Identities (person pages) | Count | Player-game rows |
|---|---:|---:|
| HIGH_CONFIDENCE_MATCH | 4,567 (7 of them by dominant evidence, ≥ 95% of ≥ 20 rows, real names agreeing) | 460,369 (83.2%) |
| REVIEW_REQUIRED | 1,333 | 36,119 |
| UNMATCHED (no PandaScore name) | 4,485 | 56,942 |

Review reasons: team alias not high-confidence 1,029 (mostly leagues PandaScore does not list), not registered for the
mapped team 107, evidence pointing to several provider players 98, role differs 73, date outside every window 18, one
provider player claimed by two person ids 8. None was forced.

| | 2023 | 2024 | 2025 | 2026 |
|---|---:|---:|---:|---:|
| PandaScore players with verified games | 3,061 | 3,049 | 2,756 | 2,432 |
| … Top / Jungle / Mid / Bot / Support | 640 / 665 / 640 / 656 / 677 | 650 / 666 / 647 / 661 / 651 | 580 / 610 / 578 / 599 / 604 | 523 / 528 / 503 / 520 / 539 |

Of 5,272 PandaScore players registered on a 2023–2026 stage, 4,567 have verified games. Share of rows with a HIGH
identity by tier: a 100%, s 99.8%, b 97.5%, c 94.9%, d 89.4%, unknown 54.1%. In `ps_player_index_2026`
`match_participation_verified_2026` is `verified` for 2,123 of 2,331 registered players.

## 3. Rating records (Gate C)

Every PandaScore catalogue player has exactly one record: **9,915 rows, 9,915 unique ids** (CSV, JSON and
`rt_player_ratings`). Source identities not reconciled to a PandaScore player (5,790) are rated too but kept apart in
`unresolved_identities.csv` / `rt_unresolved_ratings`.

| Class | Players | Overall mean (SD) | p5 / p50 / p95 | Range | Bands |
|---|---:|---:|---|---|---|
| MEASURED_ESTIMATE | 2,850 | 56.5 (13.2) | 40 / 53 / 82 | 31–93 | ELITE 33, VERY_GOOD 108, AVERAGE 290, INEXPERIENCED 771, AMATEUR_ACADEMY 1,648 |
| LIMITED_EVIDENCE | 1,717 | 46.3 (6.8) | 38 / 45 / 59 | 30–85 | VERY_GOOD 1, AVERAGE 13, INEXPERIENCED 139, AMATEUR_ACADEMY 1,564 |
| PRIOR_ONLY | 5,348 | 44.1 (1.0) | 44 / 44 / 44 | 44–73 | INEXPERIENCED 2, AMATEUR_ACADEMY 5,346 |

Whole catalogue: ELITE 33, VERY_GOOD 109, AVERAGE 303, INEXPERIENCED 912, AMATEUR_ACADEMY 8,558. PRIOR_ONLY placeholders:
4,640 use the declared global prior (raw quality 31.4 → 44; no 2023–2026 stage registration, mostly historical catalogue
entries) and 708 a tier reference (raw 25th percentiles a 63.9, b 44.4, c 34.2, d 31.0; a PRIOR_ONLY value never exceeds
73). 261 PRIOR_ONLY players have no known role.

Measured players by primary tier (mean Overall; bands):

| Tier | Players | Mean | ELITE | VERY_GOOD | AVERAGE | INEXPERIENCED | AMATEUR_ACADEMY |
|---|---:|---:|---:|---:|---:|---:|---:|
| a | 311 | 80.0 | 32 | 93 | 140 | 44 | 2 |
| b | 290 | 61.2 | 0 | 4 | 38 | 151 | 97 |
| c | 720 | 58.6 | 1 | 10 | 89 | 272 | 348 |
| d | 872 | 51.8 | 0 | 0 | 15 | 210 | 647 |
| unknown | 657 | 47.4 | 0 | 1 | 8 | 94 | 554 |

By primary role (measured): Top 56.6, Jungle 56.5, Mid 56.5, Bot 56.5, Support 56.5 (n 560–579 each).

Skills of rated players (mean / p5 / p50 / p95): Mechanics 52.6 / 38 / 50 / 79; EarlyGame 52.7 / 39 / 49 / 79;
Teamfight 52.8 / 38 / 50 / 79; Macro 52.2 / 36 / 49 / 81; ChampionPool 53.4 / 39 / 50 / 79. Six of the 33 ELITE players
have one skill at the 99 ceiling (mostly ChampionPool); their Overall stays inside the band.

**Achievements:** 1,320 rated players have at least one title, 88 an international final, 53 at least two tier-a titles;
100 meet the ELITE evidence rule, 33 are strong enough to reach 90. 7,075 placements were credited (5 standings pages
aggregated from their sub-pages); 15,942 placements belong to events without Leaguepedia scoreboards and award nothing.

**Ages:** known for 2,994 of 4,567 rated players (PandaScore 3,031 and Leaguepedia 250 across the catalogue; 10 conflicts
flagged). Stages of rated players: DEVELOPING 1,842, PEAK 819, VETERAN 333, UNKNOWN 1,573. Mean potential above the
Overall by age: 17: +20.7, 18: +16.5, 19: +13.6, 20: +9.5, 21: +6.3, 22: +4.2, 23: +1.9, 24: +1.0, 25: 0. The veterans'
floor held 36 of 333 veterans.

**Confidence:** HIGH 2,739, MEDIUM 1,119, LOW 709, NONE 5,348 (every PRIOR_ONLY).

**Form:** measured for 1,147 players; 256 have recent games but too few; 8,512 `NO_RECENT_EVIDENCE` (including all
PRIOR_ONLY). Trend: IMPROVING 105, STABLE 934, DECLINING 108.

**Years:** 1,191 rated players have games in all four years; 2,432 played in 2026.

**Images:** portrait path for 5,774 players (716 Leaguepedia 2026 headshots of HIGH confidence, 5,058 PandaScore),
none for 4,141; team logo for 2,196. Local paths only.

## 4. Rankings (Gate D)

| List | Eligible | Published | Duplicates | Fewest 2026 games in role | Evidence |
|---|---:|---:|---:|---:|---|
| Top | 256 | 100 | 0 | 22 | all MEASURED |
| Jungle | 260 | 100 | 0 | 21 | all MEASURED |
| Mid | 270 | 100 | 0 | 21 | all MEASURED |
| Bot | 266 | 100 | 0 | 25 | all MEASURED |
| Support | 250 | 100 | 0 | 25 | all MEASURED |
| Overall (EXPERIMENTAL) | 1,292 | 100 | 0 | 27 | all MEASURED |

No list has a deficit. Top 100 overall: 32 ELITE and 68 VERY_GOOD; leagues LCK 36, LPL 31, LEC 19, LCS 8, and one each
from LFL, TCL, CBLOL and Circuito Desafiante (players who left major-league teams for those leagues). Each role list
holds 5–9 ELITE players, then VERY_GOOD, AVERAGE and INEXPERIENCED players from the strongest leagues downward.

## 5. Statistical quality (Gate E)

See [model §9](PLAYER_RATING_MODEL.md#9-validation). In short: the selected model predicts later games better than team
Elo (log loss 0.617 against 0.651 even when Elo updates after every game) and much better across leagues (international
0.574 against 0.693); rankings are stable under parameter changes (Spearman ≥ 0.979, ≥ 0.995 except for halving the
half-life); the v2 rules hold on every record. Weaknesses: the size of the ELITE group depends on the AUTHORED achievement
weight; 87% of Level variance lies between teams; no timeline features; mild over-confidence at the extremes.

## 6. Quality gates

| Gate | Status | Evidence |
|---|---|---|
| A — Acquisition | **CONDITIONAL** | 2023–2026 collected from Leaguepedia with provenance (raw pages + SHA-256 manifests, run ids, cutoff) and per-window completeness; placements and birthdates likewise; importer handles yearly Oracle's Elixir files without loss (tested on synthetic files); **Oracle's Elixir itself not obtained** (Drive quota), so no timeline features |
| B — Identity | PASS | four-evidence rule, per-year aliases, ambiguous cases held (1,333 REVIEW), transfers and multi-role kept per game; tests in `test_oracles_elixir.py` and `test_player_ratings.py` |
| C — Rating coverage | PASS | 9,915 / 9,915; five skills and Overall in bounds; class on every record; 0 zero-game rated records; 0 formula mismatches; band, gate, cap and age rules checked on every record |
| D — Ranking | PASS | six lists, declared eligibility, 0 duplicates, all ranked players measured with ≥ 21 games in role; global list marked EXPERIMENTAL with per-competition comparisons in the report |
| E — Statistical quality | PASS with limits | temporal validation, role balance, small-sample check, bootstrap, sensitivity (incl. the v2 weights), reproducibility; limits in model §11 |
| F — Repository safety | PASS | `python -m unittest discover -s tests`: 105 tests OK (27 in `test_player_ratings.py`); `PRAGMA integrity_check` ok; no `.env` value in 1,410 changed, new or generated files; `data/` (ratings, raw pages, imports, database) git-ignored; no file under `content/`, `src/`, `game/` or `tests/Integration` changed by this work (`content/fixture.json` only read); nothing committed or pushed |
