# Player potential and training: experiment

Date: 2026-10-15. Status: **EXPERIMENT**. No production code, content, rule or save contract was changed. The
experimental model lives in `tools/WorldExperiment` (not in the solution). Its coefficients are versioned in
`tools/WorldExperiment/config/world-experiment.v1.json` (`Development.Version = dev2-exp-0.1`). Outputs are in git-ignored
`tools/lol-data-ingestion/data/world/experiment/`.

Labels: **FACT**, **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. Where the ceiling near 93 comes from (FACT)

| Source | What it does | Where |
|---|---|---|
| Production code | Skills, Overall and potential are clamped only to 1–99. **No 93 in code** | `Ratings.InRole`, `Careers.Develop`, `SnapshotValidation` (Score 1–99) |
| Production content | The ability-band table ends at 92. With the age shift and the archetype profile, generated ratings top out in the low 90s. Potential = rating + age headroom (≤ 14), clamped to 99 | `content/fixture.json` `Generation.AbilityBands` (52–92), `AgeProfiles` |
| Research rating model v2 | The banded transform T maps raw quality R to the Overall. Its last knot is R 92 → 93, and every R above 92 is **clamped to 93** (`SCALE_MAX`). Potential is also capped at 93. The top segment is compressed by design: R 80 → 89.5 and R 92 → 93, so 12 R points add only 3.5 Overall points (the user's band decision of 2026-10-14: "the best players form a group") | `rating_model.py` `BAND_KNOTS`, `SCALE_MAX`; `rate_players.py:777` |

**INFERENCE.** The practical ceiling has two parts:
- the research model's clamp, which is minor (§2);
- above all, its compressed top segment, a band design rather than a bug.

The production game itself has no 93 limit, only a content table that stops at 92.

## 2. Scale experiment: the same model without the clamp

`rating_scale_experiment.py` reruns the full research pipeline (`rate_players.run`) with two constants changed:
- T keeps every v2 knot, and its last segment continues at the same slope to 99 (an extra knot at R 112.57). T is
  identical for every R ≤ 92, so no player below the old clamp moves;
- the scale maximum is 99 for the Overall and the potential, with a band `EXCEPTIONAL` 94–99 that needs the same elite
  evidence as ELITE.

The v2 outputs are untouched. The experiment writes to `data/ratings/experiment-uncapped/`.

| Measure (9,915 catalogue players) | v2 | Uncapped experiment |
|---|---:|---:|
| Records changed | — | 3 (potential only) |
| Overall max | 93 | 93 |
| Overall 90 / 91 / 92 / 93 / 94+ | 19 / 9 / 3 / 2 / 0 | 19 / 9 / 3 / 2 / 0 |
| Potential 93 / 94+ | 7 / 0 | 4 / 3 (all 94) |
| Players with R > 92 | 1 | 1 |
| Potential below Overall | 0 | 0 |

**FACT.** Removing the clamp alone produces no Overall above 93, because only one player has R above 92, and the
compressed slope turns their R 93.7 into 93.5 before the skills are rounded.

**Sensitivity: the top knot** (post-hoc on R; elite-evidence gate kept; counts of players with measured evidence):

| Top of the transform | 90 | 91 | 92 | 93 | 94 | 95 | 96 | 97–98 | 94+ |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| v2 (92 → 93, clamped) | 21 | 7 | 3 | 2 | 0 | 0 | 0 | 0 | **0** |
| Uncapped (slope kept) | 21 | 7 | 3 | 1 | 1 | 0 | 0 | 0 | **1** |
| Re-knot 92 → 96 (then to 99 at R 104) | 11 | 10 | 2 | 5 | 3 | 0 | 2 | 0 | **5** |
| Re-knot 92 → 98 (then to 99 at R 98) | 10 | 3 | 8 | 4 | 3 | 2 | 1 | 2 | **8** |

**Reading (INFERENCE).**
- Real players above 93 exist on this data only if the elite band is stretched. With the knot at R 92 → 96, the 5
  strongest elite-evidence players reach 94–96.
- That is a **band definition** (how far apart the very best should be), not a measurement. It is the user's decision
  (§10, D1).
- The experiment uses the uncapped scale. It keeps every current Overall, and it allows 94–99 wherever the evidence,
  or later development, supports it.

## 3. Rating categories (PROPOSAL)

The categories are read from existing thresholds, not new ones:

| Category | Rule | Source of the threshold |
|---|---|---|
| Developing | Age ≤ 23 and potential − Overall ≥ 6 (any Overall) | Headroom median at 21–22 in the research data (6 → 4) |
| Ordinary professional | Overall 74–82 | Research band AVERAGE |
| Strong professional | Overall 83–89 | VERY_GOOD |
| Elite | Overall 90–93 | ELITE (needs elite evidence for real players) |
| Exceptional | Overall 94–99 | Above the v2 band; reachable through development or a re-knotted scale |

The NPC cap of 82 is the top of AVERAGE. A generated player starts no better than an ordinary professional and can
only become strong, elite or exceptional by developing.

## 4. The production model today (FACT)

- **`Careers.Change`** (`Lifecycle.cs:16`): one keyed draw from the age band, by age 19 / 24 / 27 / 99:
  - +2..+6;
  - 0..+3;
  - −1..+1;
  - −4..−1.

  Growth is scaled linearly by professionalism, the coach's Development and the share of matches played (each ±25 points
  around 50); decline is scaled by professionalism.
- **`Careers.Develop`** (`Lifecycle.cs:28`): **every skill moves by the same amount**, so a profile never changes. Growth
  stops at the potential, which is **static** (it never shrinks with age).
- **Missing:** training focus or intensity, readiness, morale, breakthroughs and plateaus, late bloomers, and a potential
  that adapts.

## 5. The experimental model dev2 (EXPERIMENT)

**Per season** (`tools/WorldExperiment/Development.cs`), with every coefficient in config:

1. **Effective age.** `age − (peak age − 24.5)`. Each person has a hidden peak age:
   - NPCs: bell-shaped in 22.5–27;
   - late bloomers (6% of NPCs aged 21–27): 26–29;
   - real players: the neutral 24.5.
2. **Expected growth** = growth(effective age) × headroom factor × environment.
   - The growth curve gives Overall points a season: 16 5.5, 17 5.0, 18 4.4, 19 3.8, 20 3.2, 21 2.6, 22 2.0, 23 1.5,
     24 1.0, 25 0.6, 26 0.3, 27 0. It follows the research model's fitted curve; the late steps are authored.
   - Headroom factor = G / (G + 4), where G = potential − Overall.
3. **Environment** is the product of the factors below, clamped to 0.35–1.8:

   | Factor | Formula |
   |---|---|
   | Coach | 0.55 + 0.009 × coach Development (FA self-training: coach 25) |
   | Intensity | low 0.85, normal 1.0, high 1.15 |
   | Readiness | 0.5..1.0. Average readiness is 88 / 80 / 70 by intensity, minus 0.05 a match |
   | Playing time | 0.8 + 0.4 × share of matches |
   | Morale | 0.85 + 0.003 × morale |
   | Professionalism | 0.8 + 0.004 × professionalism |
   | Trainability | 0.7–1.3, hidden |

4. **Season state** (keyed):
   - **breakthrough** 1–12%, more likely with ambition and a good environment, only before effective age 25 with G ≥ 6:
     growth × 2 plus a 2–5 point jump × headroom;
   - **plateau** 6–30%: growth × 0.15;
   - **setback** 5%: −0.5..−1.5;
   - otherwise **normal**.

   Keyed noise of ±0.8 × volatility × headroom is added.
5. **Decline** after effective age 27: (0.4 + 0.45 × years past 27) × (1.3 − 0.6 × professionalism) × 0.9 for
   regulars. It falls mostly on mechanics and early game (28 / 24 / 20 / 12 / 16%).
6. **Experience**: up to +0.35 a season from matches, into macro and champion pool, at any age.
7. **Skill allocation.** Growth is spent as skill points over the five skills, weighted by:
   - the training **focus**: role-aligned (50% of NPCs), specialist on the two strongest skills (30%), or weakness
     (20%);
   - affinity;
   - the room left under each skill's own ceiling (the person's ceiling plus their profile offset).

   Role-aligned focus turns about 10% more growth into Overall. Fractional progress is kept per skill.
8. **The Overall never exceeds the potential.** The largest gains of the season are taken back first.
9. **Dynamic potential** = min(ceiling, Overall + optimistic remaining growth). The optimistic growth is the curve from
   the next effective age, × 1.7. The potential is clamped to [Overall, 99]. So it shrinks when years pass without growth
   (an unrealized prospect), and it can rise for a late bloomer whose Overall jumps. The hidden ceiling is fixed at
   generation.

**Determinism.**
- Every draw is keyed by (seed, `dev2`, season:person, purpose) on the production rng-v1.
- Doubles use basic IEEE arithmetic and square root only.
- A production port would use the integer helpers (`Numbers.Divide`), because cross-machine determinism is still
  DV-06.

## 6. Results: representative players (1,000 replicates each, 8 seasons)

**Set-up.**
- Each case starts with five equal skills; role mid.
- Environments:
  - poor: coach 30, normal intensity, 30% of matches;
  - average: coach 60, normal, 70%;
  - strong: coach 90, high, 100%.
- Playing-time variants use coach 60: bench 5%, rotation 50%, regular 100%.
- "Neutral" traits: peak 24.5, volatility and trainability 1, professionalism 60, ambition 55, role focus.
- "Population" traits: drawn as for NPCs.
- P0 = the starting potential. Reach = the Overall reached P0 at least once.

### 6.1 Coaching (neutral traits): final Overall after 8 seasons

| Case | Start (age, Overall, ceiling → P0) | Poor p10/p50/p90 | Average | Strong | Reach P0 poor / average / strong |
|---|---|---|---|---|---|
| A prospect | 17, 55, 90 → 90 | 63 / 68 / 73 | 70 / 76 / 84 | 77 / 86 / 90 | 0% / 0.1% / 19.1% |
| B wonderkid | 18, 65, 95 → 95 | 70 / 75 / 79 | 76 / 82 / 88 | 82 / 90 / 95 | 0% / 0.4% / **14.3%** |
| C young pro | 20, 70, 82 → 82 | 71 / 75 / 77 | 75 / 78 / 81 | 78 / 82 / 82 | 0.2% / 9.9% / 55.1% |
| D late window | 22, 75, 88 → 84 | 73 / 75 / 76 | 75 / 77 / 79 | 77 / 79 / 82 | 0% / 2.2% / 8.8% |
| E near peak | 24, 78, 84 → 81 | 71 / 72 / 73 | 73 / 74 / 75 | 73 / 75 / 76 | 0.3% / 0.7% / 3.3% |
| F established | 26, 80, 82 → 81 | 67 / 68 / 68 | 69 / 70 / 71 | 70 / 71 / 71 | 0.2% / 0.9% / 1.0% |
| G veteran | 29, 85, 85 → 85 | 61 / 62 / 62 | 64 / 65 / 65 | 64 / 66 / 66 | 0% |

Median path of case B (Overall at the end of seasons 1–8):

| Environment | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Poor | 67 | 69 | 71 | 73 | 74 | 74 | 74 | 75 |
| Average | 69 | 73 | 76 | 78 | 80 | 81 | 81 | 82 |
| Strong | 72 | 77 | 81 | 84 | 87 | 88 | 89 | 90 |

### 6.2 Playing time (neutral, coach 60)

| Case | Bench (5%) p50 / mean | Rotation (50%) | Regular (100%) |
|---|---|---|---|
| A | 69 / 69.2 | 74 / 74.2 | 79 / 78.9 |
| B | 76 / 76.1 | 80 / 80.0 | 84 / 84.1 |
| C | 75 / 74.8 | 77 / 77.3 | 80 / 79.4 |
| D | 74 / 74.4 | 76 / 76.5 | 78 / 78.3 |

### 6.3 Population traits (the spread of peak ages, focus and personality)

| Case | Average p10 / p50 / p90 | Strong p10 / p50 / p90 | Reach P0 average / strong | Reach 95 strong |
|---|---|---|---|---|
| A | 68 / 76 / 86 | 75 / 85 / 90 | 2.6% / 26.2% | — |
| B | 74 / 82 / 91 | 79 / 89 / 95 | 1.6% / 19.7% | **17.5%** |
| C | 73 / 78 / 82 | 76 / 81 / 82 | 16.3% / 50.2% | — |
| F (26, 80) | 67 / 71 / 75 | 67 / 71 / 76 | **28.4% / 29.6%** | — |
| G (29, 85) | 61 / 65 / 71 | 60 / 66 / 71 | 2.7% / 3.0% | — |

### 6.4 Against the production model (same cases, same environments)

| Case, environment | dev2 mean | Production mean | dev2 reach P0 | Production reach ceiling |
|---|---:|---:|---:|---:|
| B strong | 88.9 | 88.5 | 14.3% | 11.0% |
| B average | 81.7 | 83.4 | 0.4% | 0.2% |
| B poor | 74.8 | 79.2 | 0% | 0% |
| C strong | 80.7 | 80.2 | 55.1% | 65.0% |
| E average | 74.0 | 69.9 | 0.7% | 5.2% |
| F strong | 70.6 | 65.4 | 1.0% | 44.2% |
| G average | 64.7 | 64.9 | 0% | 0% |

**Reading (INFERENCE).**
- **Environment matters more in dev2** than in the production model.
  - For case B, a poor environment costs 7 points against average in dev2, and 4 points in production.
  - Strong against average adds 7 points in dev2 and 5 in production.
  - Production reaches its static ceiling more often for older players, because its ceiling never shrinks.
- **The required behaviours (FACT, from the tables above).**

  | Behaviour | Evidence |
  |---|---|
  | **A 65 with potential 95 can reach 95, but not automatically** | 14–18% under strong conditions; 0.4–1.6% average; 0% poor |
  | **Better training raises expected growth and realization** | In every young case: strong > average > poor, in both the mean and the reach rate |
  | **Weak training leaves potential unrealized** | Case A poor: 0% reach P0; median 68 against a ceiling of 90 |
  | **Breakthroughs** | 15–38% of young players (cases A–C) have at least one in 8 seasons |
  | **Temporary stagnation** | Plateau seasons happen to 6–30% of players a season. Three non-improving seasons in a row before 24: 0–5% |
  | **Late bloomers** | Case F with population traits reaches its potential 24–31% of the time (neutral traits: about 1%) |
  | **Age decline** | Case G loses 19–23 points over ages 29–36, about 2.7 a season. That matches the research model's veterans' limit (2 a season at 29–30, then 3) |
  | **Skills diverge** | Five equal skills reach an SD of 0.5–2.8 (young) and 6–8 (veterans: mechanics falls, macro holds). Population traits: 1.5–5 |
  | **The potential is never exceeded** | Test R02 |
  | **The potential adapts** | Case D's potential falls from 84 to a mean of 74.7 (poor) – 79.3 (strong) by age 30 |

### 6.5 In the world (the initial NPC cohort over 11 simulated seasons)

The test run simulates 10 seasons, plus one more to check that a reloaded save continues identically.

| Age at start | Initial potential | NPCs | Initial Overall | Initial potential | Best Overall (mean) | Best (max) | Reached potential −1 | Breakthrough | Retired |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 16–18 | 90+ | 37 | 63.1 | 92.4 | 78.6 | 94 | 13.5% | 43.2% | 35.1% |
| 16–18 | 83–89 | 32 | 61.2 | 85.9 | 73.2 | 89 | 25.0% | 21.9% | 50.0% |
| 16–18 | 75–82 | 55 | 55.0 | 78.3 | 65.6 | 82 | 14.5% | 10.9% | 60.0% |
| 16–18 | < 75 | 388 | 43.5 | 59.1 | 52.9 | 74 | 33.5% | 19.1% | 71.1% |
| 19–21 | 90+ | 12 | 74.2 | 92.2 | 84.9 | 92 | 8.3% | 33.3% | 75.0% |
| 19–21 | 83–89 | 40 | 73.4 | 85.6 | 79.1 | 88 | 25.0% | 7.5% | 62.5% |
| 22–24 | 83–89 | 23 | 79.0 | 85.0 | 81.4 | 86 | 30.4% | 8.7% | 82.6% |
| 25+ | 75–82 | 138 | 76.9 | 77.5 | 77.0 | 82 | 92.8% | 6.5% | 99.3% |

**FACT.**
- 85 NPCs passed 82 during the 11 seasons. The best reached 94; the cohort it belongs to (aged 16–18, potential 90+)
  started at a mean of 63.
- No NPC reached 95–99. Among the 89 young prospects with potential 85+ and a gap of 10+, 76 never reached it.
- The population does not inflate. The NPC mean Overall is 53.6 at the start and 52.1 after 10 seasons. The NPC top 50
  by role moves by −0.4 to +1.1 points.

**INFERENCE.** High-potential prospects mostly fall short. They quit early when they play in weak environments: the
retirement rate of amateurs is high. Realization is a balance question, tied to where talented NPCs are employed (§10).

## 7. Determinism and versioning

- **Replay.** The same seed and the same decisions give the same careers: the world replay hash `0c147a30…` equals the
  live run after 11 seasons (test R10).
- **Versioning.** Coefficients sit in one versioned file. Any change is a new version string, not a silent edit.
- **Pinning.** A production version would pin it like content: by hash, and bound to the rules version.

## 8. Tests executed (2026-10-15)

From the world experiment's suite (`dotnet run --project tools/WorldExperiment -c Release -- test --seasons 10`), all
pass:

| Test | Checks |
|---|---|
| R01 | Overall stays within 1–99 |
| R02 | Potential stays within 1–99 and never below the Overall |
| R03 | Ratings above 93 are reachable |
| R04 | Every NPC starts at 82 or less: 8,800 created, including every intake |
| R05 | NPC potential can exceed 82 |
| R06 | NPCs develop past 82 |
| R07 | Nobody is guaranteed their potential |
| R08 | Coaching and playing time change development |
| R09 | Skills diverge |
| R10 | Determinism |

## 9. Limits

- **Coefficients are authored.** The age curve follows the research fit. Its spread is not measurable in the data
  (residual SD 9.19 under 25, against 9.41 over 25), so volatility, breakthroughs and plateaus are authored.
- **Season granularity.** One tick a season, with readiness and morale as season averages. A daily game would need the
  model split into training ticks.
- **Real players** use neutral traits, so they have no late bloomers and no specialists. That is a choice: no inferred
  trait is treated as a fact about a person.
- **No injuries and no facilities** (not approved).

## 10. Decisions needed (Director)

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| D1 | The top of the rating scale | (a) keep the v2 compression (max 93; 94+ only through development); (b) uncapped slope (a 94 at most today); (c) re-knot the elite band to 92 → 96 (5 real players at 94–96) | (b) now. It changes nobody below the old clamp. Decide (c) with the band owner |
| D2 | "No growth after 25" (the research rule) or late development | (a) keep it for real players; (b) the dev2 late-bloomer and experience rules for NPCs and real players alike | (b) for NPCs, (a) for real players, until data shows late growth |
| D3 | Adopt dev2 in production | (a) port it as integer code with a content block and a save schema bump; (b) keep the production model | (a), after a gate run at 48 seeds (GENERATOR_V2_PLAN §6). dev2 moves balance: environment matters more |
| D4 | Training focus and intensity as player-facing decisions | (a) expose them (a UI and Application change); (b) keep them as team AI policy | (a) is a new decision surface; it needs the UI rules and a DEC entry |
