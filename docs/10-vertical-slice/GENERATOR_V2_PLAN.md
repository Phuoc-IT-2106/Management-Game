# Player generation v2 — assessment, experiment and plan

Date: 2026-10-14. Status: **PROPOSAL**. No production code, content, rule or save contract was changed. The experiment
ran in a scratch copy of `src/` and `tools/Headless`, outside the repository. Its kit (patch, probe, analysis scripts)
is kept in `artifacts/generator-v2-experiment/`, which git does not track.

Labels: **FACT** (read in code or data, or measured here), **INFERENCE** (reasoned from facts), **PROPOSAL**,
**OPEN QUESTION**.

## 0. What was assessed

- **FACT.** HEAD is `587b910`. The working tree holds the uncommitted rating-distribution changes of
  [DATA_STANDARD_AUDIT §6](DATA_STANDARD_AUDIT.md#6-changes-made-on-2026-10-14-the-fixes-that-need-no-per-player-data)
  (R1–R3, R5–R7). Both were measured:
  - HEAD's generator, as a reference;
  - the working tree, as the baseline **A**. Its gates pass with 16 seeds, and it is what the next commit holds.
- **FACT.** Model A in the scratch copy reproduces the working tree's headless hash `a789c421…`. The comparisons run on
  the real rules.
- **FACT.** There is no Oracle's Elixir 2026 CSV on this machine:
  - `tools/lol-data-ingestion/data/import/oracles-elixir/` is empty;
  - Downloads, Desktop and Documents hold no such file (file names only were checked).

  The official download page is a JavaScript app, so its terms could not be read automatically (checked 2026-10-14).
  Nothing was downloaded. Every real per-player target below is **not measured**.

**Method.**
- Static sample: 10,000 generated players (seed 2026, ages from the table, no pool shift).
- Worlds: 16 opening worlds.
- Campaigns: 8 campaigns of 5 seasons with a delegating owner (the coach recruits within limits, as in the gate
  measurement).
- Gates: `Headless --measure` at gate 2, 16 seeds for every model and 48 for A and B.

## 1. Current state (baseline A)

| # | Finding | Label | Evidence |
|---|---|---|---|
| F1 | **Level and profile are drawn together.** A player is band + age shift + pool shift, plus archetype offsets, plus jitter ±7 per skill. Mind and personality are bell-shaped draws. Potential is rating + the age's headroom. Popularity is 1.5 × (rating − 50) + 5 in prime age ± 8. | FACT | `Generator.cs:70–85` |
| F2 | **Archetypes move the rating.** Their offsets add +2.0 on average: +1.1 for top, +2.7 for support. Inside a role, archetypes differ by 2.2 points on average and by 3.6 in the worst role; a carry jungler is 2.5 below the jungle mean. HEAD: 1.4 and 1.9. | FACT (measured) | static sample |
| F2a | So any edit to archetype content silently moves the world's ratings. R1 kept the bands' mean (69.7), but R2's ×1.5 offsets raised the archetypes' share from about +1.3 to +2.0, so the generated mean moved from 70.2 to 70.9. Style and quality are confounded by accident, and a future calibration cannot separate them. | INFERENCE | F2 |
| F3 | **The rating histogram is now smooth.** Against a 5-point moving average, χ²/dof is 27 at HEAD (lumpy) and 1.7 in A. R1 worked. | FACT | static sample |
| F4 | **The skills are no longer one number, but archetypes are discrete.** In A, the within-role correlation between skills averages 0.42 (0.04–0.81); HEAD 0.65. A player's skill SD is 7.1 (HEAD 4.6). The role rating still correlates 0.977 with the plain mean of the skills. A nearest-centroid rule recovers the archetype from the skills 98% of the time: each role has three tight types. | FACT | static sample |
| F5 | **Profiles matter at the margin.** In a lineup, one player's EarlyGame − Teamfight contributes about 1/20 of a match point, and a rating point 1/5. Among same-role pairs within 3 rating points, 61% change order between an aggressive and a conservative meta (HEAD 48%). This is a linear upper bound: it ignores truncation, the ±3 clamp and balanced metas. | FACT (approximate) | static sample |
| F6 | **Nobody but the human sees or uses a profile.** The coach's lineup, rival lineups, rival recruitment and the coach's recruiting all choose by rating alone. Free agents and rival players are observed as a rating band, with no skills or style. | FACT | `Observations.cs:228` (`BestLineup`), `Rivals.cs:15,211`, `Contracts.cs:41,68` |
| F6a | More profile diversity therefore adds hidden variance to signings: a band is bought and the profile is found afterwards. Diversity becomes a decision only where it is observed. | INFERENCE | F6 |
| F7 | **The open market is not a fringe.** At the start, free agents average 68.9, rival players 68.1 and the company 67.4. 70% of free agents rate at or above the median rival's weakest starter. Players aged 20 or less are 18% of the market at the start and 55–60% by seasons 3–6, through the youth intake of 15 a season aged 17–21. | FACT | 16 worlds, 8 campaigns |
| F7a | In the real data, 46% of players new to a team's roster are new to the observed rosters. Their median age is 24.1 (p10 20.2, p90 27.6). The game's intake ages (17–21) come from the age histogram's growth, not from entrants. | FACT | `calibration_2026.json` `careers` |
| F7b | The game's market stands in for everything below the league. Its inflow is all prospects, where the real inflow is mostly adults from lower tiers, so the market acts as a strong talent reservoir. | INFERENCE | F7, F7a |
| F8 | **Development has a fixed shape and stops early.** Mean change a season: +3.6 at 18, +1.0 at 20, +0.5 at 22, +0.4 at 24, −0.2 at 26, −2.5 at 28 and over. 54% of 22-year-olds and 70% of 24-year-olds are at their potential ceiling. Potential correlates 0.99 with rating from age 21. Development moves all five skills by the same amount, so a profile never changes during a career. | FACT | campaigns; `Lifecycle.cs:28–36` |
| F9 | **Popularity is rating.** It correlates 0.90 with rating; no generated player reaches the star threshold of 80. | FACT | static sample (audit R8) |
| F10 | **Two listings ignore the fee formula.** A company player whose contract ends is listed at 5 days' wage, and a released player at the release cost, instead of `Pay.Fee`. Measured: 3 of about 1,000 listings in the campaigns, at 0.31–0.38 of the formula fee. That owner renews most players; one who lets contracts run out would see more. | FACT | `Market.cs:223`, `Simulation.cs:74` (audit P2) |
| F11 | **The rival squad shift is anchored on a literal 71**, the expected rating of the shared distribution. Redraws correct a miss, but a change of the generated mean changes first-draw accuracy silently. | FACT | `Rivals.cs:75` |
| F12 | **Ages do not depend on role.** Real supports are older: mean 24.8, against 23.7–23.9 for the other roles. | FACT | `calibration_2026.json` `demographics.age` |

## 2. Measured versus authored

| Parameter (content) | Source | How | Quality |
|---|---|---|---|
| `Generation.Ages` | Measured | 2026 age histogram, ends pooled into 17 and 31 | 66% coverage, biased (W3); licence unconfirmed |
| `Nationalities[].Weight` | Measured | ASIA region shares | Good for one region |
| `Rivals.RosterSizes` | Measured, shifted | Real 5/6/7 at 76/19/3%; the game uses 6/7/8 (minimum roster 6) | Shape only |
| `Generation.Rival` 68–82, `Balance.LogitScale` | Derived | Within-league Elo spread (351, top beats bottom 88%) through the logistic | Within-league only (W1) |
| `Balance.Variance`, `SeriesForm` | Fitted | Replay tuned to the real score mix and the favourite's win rate | Good |
| `Rivals.BuyPercent` 54 | Measured | 53.9% of newcomers come from another team | Good |
| `Lifecycle.RetirementByAge` | Derived | Stationary life table from the age histogram | Proxy (W2) |
| `Lifecycle.IntakeAges` | Derived | The histogram's growth from 17 to 21 | Conflicts with entrant ages (F7a) |
| `AbilityBands` (normal 52–92) | **Authored** | Old bands' mean and SD kept | No real data |
| `SkillJitter`, `Archetypes`, `RoleWeights` | **Authored** | — | No real data |
| `AgeProfiles` (shift, headroom), `Lifecycle.Development` | **Authored** | Tuned for a stable world (rules v4 phase 6) | No real data |
| `Mind`, `Personality`, `SecondaryRoleChance`, popularity formula | **Authored** | — | No real data |
| `StartingShift`, `MarketShift`, `Rivals.Edge`, funds, wage caps, `YouthIntake`, pool size, cap and floor | **Authored** | Balance and design decisions | — |

What would calibrate the authored rows, if the Oracle's Elixir file is obtained and its terms allow this use:

| Target | Oracle's Elixir fields (all captured by `import_oracles_elixir.py`) |
|---|---|
| Rating distribution by role (`AbilityBands`) | A composite per player: role- and league-normalized, shrunk by games played (audit §5) |
| Skill covariance and archetypes | `golddiffat10/15`, `xpdiffat10/15`, `csdiffat10/15`, `dpm`, `damageshare`, `earned gpm`, kills, assists and deaths shares, `vspm`, `wpm`, `wcpm`, first-blood columns, distinct champions |
| `RoleWeights` | Which role metrics separate wins from losses. Post-game statistics are partly outcome, so early-game columns only |
| Consistency | A player's game-to-game SD of the composite |
| Composure | `playoffs` versus regular games |
| Headroom and development by age | Needs 2025 and 2026 files, and ages through the identity reconciliation |
| Not in the data | Popularity, communication, personality, wages, budgets |

## 3. Measurement weaknesses

| # | Weakness | Label | Effect |
|---|---|---|---|
| W1 | **Team Elo is one pool, but leagues rarely meet.** Each league's ratings centre on 1500: tier medians are A 1505, B 1476, C 1502, D 1503. K 32 with at least 8 matches adds noise to the spread, and the spread pools a year's splits. The Brier score is 0.2196 against 0.25 for a coin. | FACT | Only within-league spreads are usable. Tiers cannot share one scale (audit R9) |
| W2 | **The retirement table is a proxy.** 1 − n(age + 1)/n(age) on one year's histogram assumes equal cohorts and no exits to lower tiers. It gives 27.7% a season at 25–27, where the direct measure is 10.4% exits per split. The two agree only if a season is about three splits (1 − 0.896³ = 28%). | FACT; INFERENCE | Unverified scale. "Retirement" at 25–27 is really leaving the observed tiers |
| W3 | **Age coverage is 66% and biased.** Players without a birthday leave the rosters at 30% per split, against 9–16% for the others. | FACT | The age table leans to stable, higher-tier players |
| W4 | **Selection.** Registered rosters are not the players who played. A league's tier is the tier of its first match. Oracle's Elixir rows carry a `datacompleteness` column (the importer keeps it); how many 2026 rows are partial, and in which leagues, is not measured. | FACT; OPEN QUESTION (OE coverage) | Per-player targets need participation from the reconciliation, and per-league completeness checks |
| W5 | **No player-game data.** Every ability, profile, mind and development value is authored. | FACT | §2 |
| W6 | **The game's own gates are noisy for this question.** At 16 seeds, multi-year progress has an SE of 0.4–0.5 around its threshold of 1, and outcome diversity turns on a few outlier seeds (§4.3). | FACT | A generator change can pass or fail by seed luck. Judge generators on 48 seeds, paired by seed |

## 4. Experiment: one baseline, two alternatives

Only `Generator.Player` differs between the models: same seeds, same content. B and C add five keyed draws for the
level noise.

| Model | Rule |
|---|---|
| **A** (baseline) | skill = level + archetype offset + jitter |
| **B** anchored | The level is drawn first. The profile (offset + jitter) is centred by the role's weights, so the role rating equals the level. A level noise of the size of A's jitter share, and a constant +2 (A's mean archetype bias, computed from content), hold the rating distribution at A's. Only the archetype-to-rating link changes |
| **C150** wider profile | B with the profile ×1.5: a sensitivity test of profile diversity |

### 4.1 Population (10,000 players)

| Metric | HEAD | A | B | C150 |
|---|---|---|---|---|
| Rating mean / SD | 70.2 / 7.1 | 70.9 / 7.3 | 70.9 / 7.2 | 70.8 / 7.1 |
| Rating p5 / p50 / p95 | 59 / 71 / 81 | 59 / 71 / 83 | 59 / 71 / 83 | 59 / 71 / 83 |
| Role means, lowest–highest | 69.5–70.8 | 70.0–71.7 | 70.7–71.0 | 70.7–70.9 |
| Histogram χ²/dof against a smooth curve | 27 | 1.7 | 0.9 | 0.9 |
| Archetype rating gap inside a role, mean / worst | 1.4 / 1.9 | 2.2 / 3.6 | **0.6 / 1.1** (sampling noise) | 0.6 / 1.0 |
| Within-role skill correlation, mean | 0.65 | 0.42 | 0.43 | **0.19** |
| A player's skill SD | 4.6 | 7.1 | 7.1 | 10.5 |
| Role rating against the plain mean of skills (r) | 0.989 | 0.977 | 0.977 | 0.952 |
| Skills clamped at 1 or 99 | 0.04% | 0.26% | 0.29% | **1.66%** |
| Archetype recovered from skills | 99% | 98% | 98% | 98% |
| EarlyGame − Teamfight SD at equal rating | 6.9 | 11.3 | 11.4 | 16.8 |
| Close pairs that change order with the meta | 48% | 61% | 61% | 73% |
| Potential against rating, age 21+ (r) | 0.99 | 0.99 | 0.99 | 0.99 |

### 4.2 World and careers (16 worlds; 8 campaigns × 5 seasons)

| Metric | A | B | C150 |
|---|---|---|---|
| Start: company / rival players / market | 67.4 / 68.1 / 68.9 | 67.7 / 68.1 / 68.8 | 67.6 / 68.1 / 68.8 |
| Start: market at or above the median rival's weakest starter | 70% | 73% | 73% |
| Start: rival strength mean, spread | 74.9, 13.8 | 75.0, 14.1 | 75.0, 14.1 |
| Start: rival lineup style SD; lineups with a style term of 1+ | 5.4; 52% | 5.4; 54% | **8.4; 71%** |
| Season 6: company / rival players / market | 73.7 / 69.9 / 70.8 | 73.8 / 70.1 / 71.4 | 73.6 / 70.4 / 71.0 |
| Season 6: rival strength mean, spread; mean age | 76.6, 14.4; 22.5 | 76.6, 12.8; 22.5 | 76.9, 12.4; 22.5 |
| Development at 18 / 22 / 28 | +3.6 / +0.5 / −2.5 | +3.5 / +0.5 / −2.2 | +3.5 / +0.5 / −2.2 |
| Season 6: rival players' skill correlation (roles pooled) | 0.39 | 0.38 | 0.17 |

Market people differ between models after the first rival redraw (IDs shift), so these are distribution comparisons,
not paired ones.

### 4.3 Rules gates (16 and 48 seeds × 5 seasons, gate 2)

Each model ran the gate measurement on seeds 20261004 and 1–15. A and B also ran seeds 16–47, through a seed-offset
option added to the scratch copy's Headless, so they have 48 seeds each. "Paired" is B − A, seed by seed, with its
standard error. Both alternatives pass the two-cycle determinism check (B `7c6f52fd…`, C150 `01c7417b…`).

| Measure | Threshold | A, 16 | B, 16 | C150, 16 | A, 48 | B, 48 | Paired B − A, 48 |
|---|---|---|---|---|---|---|---|
| Gate 1: losing is not profitable | ≥ 75% of seeds | 15/16 | 12/16 | 14/16 | 39/48 | 39/48 | — |
| Gate 1: reputation follows results (partial r) | ≥ 0.6 | 0.89 | 0.88 | 0.86 | 0.86–0.91 (three blocks) | 0.87–0.90 | — |
| Gate 2: empty stretches | ≤ 10 days | 9 | 9 | 9 | 9 | 9 | — |
| Gate 2: multi-year progress | ≥ 1 place | **1.38** | **0.81 ✗** | **0.88 ✗** | 1.35 (SE 0.30) | 1.19 (SE 0.26) | −0.17 ± 0.22 |
| Player value (balanced − coach win rate) | 5–15 | 13.1 | 16.2 | 15.5 | 13.4 (SE 1.4) | **16.6** (SE 1.5) | **+3.2 ± 1.6** |
| Balanced owner's win rate | — | 84.1% | 85.6% | 85.1% | 81.6% | 83.9% | +2.3 ± 1.2 |
| Delegating coach's win rate | — | 71.1% | 69.5% | 69.7% | 68.2% | 67.2% | −0.9 ± 1.3 |
| Balanced lineup base, season 5 | — | — | — | — | 81.3 | 82.3 | — |
| Outcome diversity | ≥ 1 | 0.66 | 0.87 | 0.86 | 1.17 | 0.64 | bootstrap 90% [−0.91, −0.08] |
| Financial pressure | 20–60% | 5.6% | 3.4% | 4.1% | 3.8–6.9% (blocks) | 2.5–3.8% | — |
| Market depth; rival churn; rival spread | ≥ 90%; 15–35%; 10–20 | 100; 16.4; 13.9 | 100; 16.4; 14.1 | 100; 16.5; 13.8 | — | — | — |
| Runs that stopped insolvent | — | 1 of 64 (thrift) | 1 of 64 (thrift) | 1 of 64 (thrift) | 2 of 192 (thrift) | 2 of 192 (thrift; **balanced**, seed 31) | — |

By seed block (0–15, 16–31, 32–47), multi-year progress is 1.38, 1.19, 1.50 for A and 0.81, 1.40, 1.38 for B.

### 4.4 Reading

- **B does what it was built for.** The population is A's: the same rating distribution, world level, drift,
  development and market. The archetype-to-rating link is gone (F2). **FACT.**
- **B's gate-2 failure at 16 seeds is seed noise.** On 48 seeds B passes (1.19). The paired difference, −0.17 ± 0.22,
  is not distinguishable from zero. On the first 16 seeds alone, B scores 0.81; on the next two blocks, 1.40 and 1.38.
  **FACT.**
- **The gate statistics cannot judge a generator at 16 seeds.** Multi-year progress has a standard error of 0.4–0.5
  around its threshold of 1. A's own pass rests on two seeds (+6 and +5 places). Outcome diversity is the SD of a few
  outlier finishes: A scores 0.66 on 16 seeds and 1.17 on 48. **FACT** (W6 below).
- **One effect survives 48 seeds.** The balanced owner gains: +2.3 ± 1.2 win rate, and its lineup base is 1.0 higher by
  season 5. The delegating coach does not gain. So player value rises from 13.4 to 16.6, above its 5–15 range, and
  outcome diversity falls. One balanced run stopped insolvent (seed 31), against none in A. **FACT.** The mechanism is
  not identified. Rivals and both owners choose by rating in both models. The candidates are the match terms that a
  rating does not carry (leadership, style, composure) and the coach's recruitment limits, which are set from the
  opening squad's wages.
  **OPEN QUESTION.**
- **C150** behaves like B on every gate at 16 seeds. Its own costs: 1.66% of skills clamped, and the rating explains
  less of the skills (r 0.95). Its benefit, team style SD 5.4 → 8.4, is invisible when recruiting (F6). **FACT;
  INFERENCE.**
- **So B is neutral for the population but not for balance on one reference.** Adopting it is a balance decision for
  the user, after the cause is known. **INFERENCE.**

## 5. Generator v2 (PROPOSAL)

### 5.1 Current and proposed population model

| Part | Now (A) | Proposed v2 |
|---|---|---|
| Level | Band + age shift + pool shift; the archetype and jitter add to it | **The level is the rating.** `AbilityBands` becomes the role-rating table itself (per role once data shows roles differ). Pool shifts as now |
| Profile | Archetype offsets + jitter, not centred | Archetype offsets + jitter, **centred by the role's weights**. An optional `ProfileScale` (percent, default 100) is the one diversity knob |
| Archetypes | 3 discrete types a role | Unchanged now. Later: clusters fitted to real profiles |
| Mind, personality | Bell-shaped, independent | Unchanged |
| Potential, development | Age headroom; every skill moves alike | Unchanged in v2. Later, after two years of data: headroom with individual spread, profile-aware ageing |
| Popularity | Follows rating | Unchanged (no data) |
| Market inflow | Youth 17–21 at full level; opening pool −2 | Decision (§5.3) |
| Rival squads | Shift anchored on literal 71 | Anchor on the content's expected level |

### 5.2 Minimal change (step G1)

- **Code** (`Generator.Player`, about 15 lines): draw the level; draw the profile; centre it; add. The experiment's
  version is `artifacts/generator-v2-experiment/Generator.experiment.diff`. Production drops the experiment's level
  noise and its +2 constant, and moves both into the table (below).
- **Content:** `AbilityBands` is replaced by the distribution model B produces, so the world's level does not move. A
  script builds it offline: the bands convolved with the level noise, +2. `Rivals.cs:75` reads the table's mean.
- **No save change.** `Person` keeps its fields. The content hash changes, so older saves are rejected, as DV-08 allows.
- A generator change must **never ship as code alone**. Saves bind the content hash and rules version
  (`SnapshotStore.cs:74–75`), so a code-only change would let an old save continue with a different generator for new
  people.

### 5.3 Decisions this plan needs from the user

1. **G1, the anchored generator.** Options:
   - (a) explain the player-value shift first, with one diagnostic run, then decide (recommended);
   - (b) adopt it now, and accept player value at about 16.6 or retune the owner side;
   - (c) keep A, and fix only F10 and F11.
2. **Profile diversity** (`ProfileScale`): keep 100 until real data gives a target (recommended), or raise it now.
   Raising it only pays off if scouting shows a style (F6a).
3. **Market inflow** (F7b): keep the intake as prospects; or apply `MarketShift` to the intake and the floor; or add
   adult entrants at the measured entrant ages. Each changes balance (player value, coach recruiting) and needs a
   gate run.
4. **Scouting shows style:** an estimated "early game ↔ teamfight" marker for free agents and rival players. This is a
   UI and information decision (DEC-005), and the condition for (2).

## 6. How a generator is judged

Every candidate runs the same three probes as §4:
1. the static sample of 10,000;
2. the 16 opening worlds and the 8 five-season campaigns;
3. the gate measurement on 48 seeds, paired by seed with the baseline (16 seeds cannot tell these models apart, W6).

Acceptance gates for G1. A miss is reported, never tuned silently.

| Gate | Measure | Threshold |
|---|---|---|
| V0 Determinism and contracts | Integration (369 + new checks); two-cycle headless; culture replay; shuffled content gives the same campaign; snapshot validation; save schema unchanged | All pass; new hashes recorded |
| V1 Rating distribution | Static sample against A (later: against the real table) | Mean ±0.3, SD ±0.3, p5/p50/p95 ±1; χ²/dof ≤ 2; role means within 1 point of each other |
| V2 Structure | Archetype gap inside a role; clamped skills; within-role skill correlation; a player's skill SD | Gap ≤ 1.0 (mean); clamped ≤ 0.5%; correlation 0.25–0.55 (authored band until real data); skill SD ≥ 6 |
| V3 Differentiation | Close pairs that change with the meta; rival lineup style SD | 50–75%; ≥ 5 |
| V4 World | Gates 1–2 at 48 seeds; references: player value 5–15, market depth ≥ 90%, rival churn 15–35%, rival spread 10–20; rival strength drift over 5 seasons ≤ +2; mean age 22–24.5 | All gates pass; no paired difference from A beyond 2 SE on a reference; references inside their ranges |
| V5 Real data (after the file) | Generated against real, per role: quantile difference at p10, p50 and p90; correlation matrix; team strength from five ratings predicting series | ≤ 2 points; every pair inside the real 90% interval; Brier ≤ 0.23 (Elo 0.2196) |

## 7. Controlled A/B plan for when real data arrives

1. **Terms first.** Read the Oracle's Elixir terms on the download page. Record whether derived, aggregate use in a
   generator is allowed. If not, stop the real-data track (§9).
2. **Import and reconcile** (built, `import_oracles_elixir.py`, `reconcile_identities.py`). Count per role and league
   the players with 10 or more games and complete data.
3. **Compute targets** in the research tool, aggregate only: role-rating quantiles, the 5×5 covariance of the mapped
   skills, cluster centres, and consistency and composure proxies, with 90% bootstrap intervals. They go into a new
   versioned report beside the pinned `calibration_2026.json`, which stays as it is.
4. **Arms**, from one content file each and the same seeds:
   - A: the current content;
   - G1: anchored, with today's authored profile;
   - G1 + real: anchored, with the real quantile table and profile scale.

   Then run the three probes for each arm.
5. **Decide** by gates V0–V5. A real-data arm that fails V4 does not ship. Report which reference moved, and ask.

## 8. Dependencies and regression risks

| Risk | Where | Guard |
|---|---|---|
| Hashes change: headless, Integration, every test world | All generation | One measured round; record new hashes in the handoff |
| Tests that assume today's generator: market versus roster shift, archetype variety, young headroom, fee curve | `tests/Integration/Program.cs:133–157, 790–840` | Keep their intent; update thresholds only with the evidence |
| World level moves with the table (company, market, rivals) | `AbilityBands`, `Rivals.cs:75` | V1 and V4 |
| Balance references near their edges: player value 13.1 (5–15), outcome diversity 0.66, financial pressure 5.6% | Measure-Rules | Report each; balance is the user's call |
| Uncommitted R1–R7 still pending | Working tree | Commit them first, so G1 is measured against a committed baseline |
| Gate noise mistaken for an effect, or an effect hidden by it | `Measure-Rules` at 16 seeds | 48 seeds, paired by seed; report the standard error with every difference |
| B moves balance: player value 13.4 → 16.6, one balanced insolvency | §4.3 | Step 2a before adoption |
| Content order independence | `Generator.Weighted`, sorted pools | The shuffled-content test |
| Real data leaking into content or saves | Research tool | Aggregates only; the existing no-names test; DEC-031 boundary |

## 9. Sequence and stop conditions

| Step | Work | Needs | Stop if |
|---|---|---|---|
| 0 | Commit the R1–R7 working tree as the baseline | User approval | — |
| 1 | Fix F10 (both listings use `Pay.Fee`) and F11 (anchor on the table) | Nothing | Integration fails |
| 2a | Diagnostic: why the balanced owner gains under B. Freeze one match term at a time (leadership, style, composure), and hold the coach's recruitment limits fixed; 48 seeds | Nothing (scratch copy) | The cause is a defect of B: stop G1 |
| 2 | **G1**: anchored generator and the rebuilt `AbilityBands`; gates V0–V4 | Decision 1 | Any V0 failure; V4 fails at 48 seeds (report, ask; no balance tuning inside this step) |
| 3 | Market inflow | Decision 3 | Player value leaves 5–15, or market depth < 90%, after two rounds |
| 4 | Scouting shows style; only then `ProfileScale` | Decisions 2 and 4 | Clamped skills > 0.5% |
| 5 | Real-data A/B (§7) | The file, its terms, DEC-031 | Terms forbid it; fewer than 150 players with 10+ complete games in a role (then pool roles, or stay authored); V5 fails |
| 6 | Potential and development from two years of data | 2025 + 2026 files | Too few players followed across both years |

## 10. Open questions

- What are Oracle's Elixir's terms for derived, aggregate use? This is the user's to read on the download page.
- What should the skill correlation be? Today's band (0.25–0.55) is authored. Real role-normalized metrics will set it.
- Should roles differ in their rating distribution? This is unknown until per-role real data exists.
- Is the game's season one real split or one real year? It decides how the per-split exit rate (10.4%) maps to the
  retirement table (W2).
- Should rivals and the coach ever choose by style? They are profile-blind today (F6).
- Why does the balanced owner gain under B (§4.4)? Step 2a answers it.
- Should the gate standard for generator changes be 48 paired seeds? The audit already asks whether 16 seeds should be
  the standard; this experiment says 16 is too few for generator decisions.
