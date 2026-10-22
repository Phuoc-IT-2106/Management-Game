# 4,000-NPC generation report

Date: 2026-10-15. Status: **EXPERIMENT**. The 4,000 generated players exist only in git-ignored research storage:
- `tools/lol-data-ingestion/data/world/experiment/world_snapshot_s0.json`, 12.4 MB;
- the report `world_report.json`.

No production content, generator or save changed. Generator code: `tools/WorldExperiment/Generation.cs`,
`WorldBuilder.cs`. Configuration: `config/world-experiment.v1.json`. Seed 20261015.

Labels: **FACT**, **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. Pipeline

`Population generation → career profiles → organizations/teams → free-agent cohort → employment allocation → squad
registration → starting five → backstories → validation`. Steps 3–9 are in [NPC_EMPLOYMENT_DISTRIBUTION](NPC_EMPLOYMENT_DISTRIBUTION.md).

1. **Slots and roles.**
   - Exactly 4,000 slots. Every role gets exactly 800, assigned by a keyed shuffle.
   - The career profile is drawn by quota: PRO 650, SEMI 800, AMATEUR 1,950, PROSPECT 600.
   - A profile only shapes the draw of ability and age. Employment is decided later, by allocation.
2. **Identity.**
   - **Nationality and legal name** come from the production pools (`Generator.Identity`, the content's nationality
     weights).
   - **Handle**: a larger syllable grammar, plus 20% in the production prefix+suffix style. It is unique among all NPCs
     and different from all 15,145 real handles in the staging data (normalized lower-case letters and digits), with a
     keyed retry and a small blocklist of offensive substrings.
   - **ID**: `player:npc:w1:c0000:{slot6}`. The initial cohort is `c0000`; the intake of season *s* is `c{s:D4}`. Real
     players are `player:real:ps:{id}`, so the two can never collide.
3. **Age.** PROSPECT is 16–19 (18/30/30/22%). The other profiles use the real age histogram of their calibration tier.
4. **Overall.**
   - A keyed draw from the calibrated quantile function of the profile's tier mixture (§2), shifted down for ages 16–20
     (−9, −7, −5, −3, −1).
   - It is **truncated to 30–82 by rescaling the uniform**, not by clamping, so there is no spike at the cap.
5. **Skills.**
   - The level plus the role's archetype offsets and ±7 jitter, **centred by the role weights**. The profile does not move
     the rating; this is model B of [GENERATOR_V2_PLAN](../GENERATOR_V2_PLAN.md).
   - The Overall is then recomputed with the production formula (`Ratings.InRole`). Above 82, the highest-weighted
     skill is lowered one point at a time.
6. **Mind.** The production bell draws over the content ranges (mind 35–85, personality 20–90), plus the archetype's
   communication offset.
7. **Hidden development traits.** Peak age, volatility, trainability, a per-skill affinity, training focus, and the
   late-bloomer flag. All are bell or keyed draws; see [PLAYER_POTENTIAL_TRAINING_EXPERIMENT §5](PLAYER_POTENTIAL_TRAINING_EXPERIMENT.md#5-the-experimental-model-dev2-experiment).
8. **Potential.**
   - Ceiling = Overall + headroom(age) × talent. A late bloomer adds 3–9.
   - Headroom by age: 16 22, 17 19, 18 16, 19 13, 20 10, 21 7, 22 5, 23 3, 24 2, 25 1, 26 0.5; it reads the research
     model's median headroom by age.
   - Talent comes from a keyed quantile table: median 1.0, p95 2.0, p99 2.6, max 3.2.
   - Above 88, the ceiling is compressed as 88 + 11·x/(x + 11), so it nears 99 without a spike of clamped values.
   - Potential = min(ceiling, Overall + optimistic remaining growth). It is never below the Overall.

**Nationality never enters** an ability, trait or potential draw: the keys are independent. The check is in §5.

## 2. Calibration (aggregate, from the research ratings)

`world_calibration.py` writes `data/world/npc_calibration_2026.json`. The population is rated players with 10 or more
verified 2026 games (MEASURED or LIMITED evidence; 1,908 players). Every value is an aggregate over at least 20 players.

| PandaScore tier | Players | Mean | SD | p5 | p25 | p50 | p75 | p95 | Used by profile |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| a | 226 | 81.9 | 6.5 | 71 | 77 | 82 | 87 | 91 | — (above the NPC cap) |
| b | 101 | 67.0 | 10.7 | 50 | 58 | 69 | 75 | 82 | PRO |
| c | 521 | 59.0 | 12.1 | 42 | 49 | 57 | 69 | 79 | SEMI; 40% of PROSPECT |
| d | 669 | 51.1 | 8.2 | 39 | 46 | 50 | 54 | 68 | AMATEUR 60%; 60% of PROSPECT |
| unknown | 391 | 47.7 | 9.2 | 36 | 41 | 46 | 52 | 66 | AMATEUR 40% |

**INFERENCE.** The NPC cap of 82 equals the tier-b p95. Fictional professional teams are therefore built like tier-b,
lower tier-a squads: below the real elite, as the cap intends.

## 3. Distributions (FACT, the 4,000 at creation)

| Measure | Value |
|---|---|
| NPCs / unique IDs / unique handles | 4,000 / 4,000 / 4,000 |
| Handles equal to a real handle | 0 of 15,145 |
| IDs in the real-player namespace | 0 |
| Overall mean, p10 / p25 / p50 / p75 / p90 / p99, max | 53.6 — 40 / 45 / 51 / 62 / 72 / 80, **82** |
| Overall above 82 | **0** |
| Potential mean, p10 / p50 / p90 / p99, max | 59.3 — 45 / 57 / 77 / 91, **96** |
| Potential ≥ 83 / ≥ 90 / ≥ 95 / = 99 | **150 (3.75%)** / 49 (1.2%) / 7 (0.18%) / 0 |
| Potential below Overall | 0 |
| Age mean, p10 / p50 / p90, range | 22.9 — 18 / 23 / 27, 16–31 |
| Secondary role | 25.3% |
| Late bloomers | 180 |
| Training focus: role / specialist / weakness | 1,985 / 1,200 / 815 |
| Mind means: consistency, composure, communication | 59.8, 60.2, 64.0 |
| Personality means: professionalism, ambition, loyalty | 55.3, 54.9, 54.9 |
| Career events per NPC at creation | 2.8. **Every NPC has a history** |

Overall histogram (5-point bins): 30–34 62, 35–39 283, 40–44 588, 45–49 818, 50–54 777, 55–59 342, 60–64 296,
65–69 283, 70–74 294, 75–79 210, 80–82 47.

Potential histogram: 30–34 10, 35–39 95, 40–44 277, 45–49 572, 50–54 732, 55–59 596, 60–64 442, 65–69 368, 70–74 334,
75–79 294, 80–84 173, 85–89 58, 90–94 42, 95–99 7.

By profile (Overall mean / p50 / p90 / max; median age):

| Profile | NPCs | Mean | p50 | p90 | Max | Median age |
|---|---:|---:|---:|---:|---:|---:|
| PRO | 650 | 65.6 | 68 | 78 | 82 | 25 |
| SEMI | 800 | 57.7 | 54 | 75 | 82 | 24 |
| AMATEUR | 1,950 | 49.5 | 49 | 61 | 76 | 23 |
| PROSPECT | 600 | 48.2 | 46 | 65 | 80 | 18 |

**Headroom by age** (potential − Overall, at creation):

| Age | NPCs | Overall p50 | Headroom p10 / p50 / p90 / max | Potential ≥ 83 | Research headroom p50 |
|---:|---:|---:|---|---:|---:|
| 16 | 111 | 42 | 8 / 19 / 35 / 57 | 20 | — |
| 17 | 192 | 45 | 7 / 19 / 29 / 45 | 23 | 18 |
| 18 | 209 | 46 | 6 / 15 / 27 / 42 | 26 | 18 |
| 19 | 258 | 49 | 5 / 13 / 20 / 40 | 20 | 13.5 |
| 20 | 219 | 50 | 4 / 10 / 17 / 28 | 10 | 9 |
| 21 | 332 | 51 | 3 / 7 / 13 / 20 | 22 | 6 |
| 22 | 448 | 52 | 2 / 5 / 9 / 19 | 10 | 4 |
| 23 | 417 | 53 | 1 / 3 / 6 / 16 | 6 | 2 |
| 24 | 422 | 54 | 1 / 2 / 4 / 11 | 7 | 1 |
| 25 | 440 | 52 | 0 / 1 / 2 / 11 | 4 | 0 |
| 26–31 | 952 | 53 | 0 / 0 / 0–1 / 8 | 2 | 0 |

**INFERENCE.**
- Median headroom tracks the research medians. The research model gives none from 25 by rule; dev2 allows a little,
  through late bloomers.
- The tail is wider by design: potential is a possible ceiling, not a forecast.
- **High potential is uncommon:** 150 NPCs (3.75%) can become strong professionals or better. Most of them are young;
  7 can become exceptional.

## 4. By employment category (at creation)

| Category | NPCs | Overall mean (p10 / p50 / p90, max) | Potential mean (p90, max) | Median age | Potential ≥ 83 |
|---|---:|---|---|---:|---:|
| Fictional professional starters | 300 | 76.3 (72 / 76 / 81, 82) | 80.4 (88, 96) | 24 | 79 |
| Fictional professional reserves | 178 | 66.1 (54 / 69 / 74, 77) | 79.4 (91, 96) | 20 | 50 |
| Semi-professional | 710 | 63.2 (54 / 64 / 72, 77) | 66.4 (74, 86) | 24 | 1 |
| Amateur | 2,003 | 47.4 (39 / 48 / 54, 66) | 50.6 (58, 70) | 24 | 0 |
| Academy / development | 366 | 47.9 (40 / 47 / 58, 68) | 62.7 (70, 76) | 18 | 0 |
| Real-team NPC reserves | 123 | 47.5 (33 / 46 / 67, 75) | 64.8 (80, 85) | 18 | 2 |
| Free agents | 320 | 51.1 (39 / 49 / 69, 81) | 60.9 (78, 95) | 21 | 18 |

**INFERENCE.**
- **Where the high-potential players are.** Professional reserves are mostly young development players (median 20,
  potential p90 91). Fictional professional teams pick high-potential prospects as reserves, while their starters are
  valued mainly on current ability.
- **Academies and the scouting order.** Academies select by age and potential, but they pick after the professional
  teams. So they hold steady prospects (potential p90 70) rather than the rare top ones.
  - If academies should hold the best prospects, the allocation order is a design choice. **OPEN QUESTION.**
- **High potential outside the professional teams.** 18 free agents and 2 real-team NPC reserves have potential 83+:
  rare finds for a scout.

## 5. Checks (FACT)

| Check | Result |
|---|---|
| Exactly 4,000 unique NPCs | Pass (N01) |
| Every NPC starts at 82 or less: 4,000 initial and 4,810 intake over the test run's 11 simulated seasons | Pass (R04: 8,810, max 82) |
| Potential may exceed 82; ≤ 99; never below the Overall | Pass (R05, R02) |
| Roles 800 each; ages 16–31; nationalities from the content | Pass (N10) |
| Generated handles never collide with real ones | Pass (N09) |
| Nationality has no effect on ability | KR 53.6 / CN 54.1 / VN 52.9 / JP 53.2 / TW 54.4 / FR 54.8 / DE 54.2 / US 53.4 / PH 53.4 / BR 51.6 |
| Role balance | Overall means by role 53.1–54.3 (bot highest by 0.7–1.2) |
| Legal names: (name, nationality) groups with more than one NPC | **926**. The content pools hold 16 given × 16 family names per nationality (256), and 1,342 NPCs are KR |

The nationality spread is sampling noise: BR is 85 NPCs, standard error about 1.0.

## 6. Performance (FACT; this machine, Release, ~8 GB RAM)

| Measure | Value |
|---|---|
| Generation and allocation (4,000 NPCs, 854 teams, backstories), including loading 1,135 real players | 3.0 s |
| Managed memory of the world at creation | 12.6 MB |
| Snapshot at creation (JSON, all people, teams, contracts, history) | 12.38 MB; serialize 0.17 s, load 0.32 s |
| Free-agent query (role and level filter over every person) | 0.24 ms |
| Scouting query (young mids with potential 80+, top 20 over ~5,100 active people) | 0.32 ms |

## 7. Limits and open questions

- **Legal names repeat** (926 groups). Handles, which identify people in play, are unique. Unique legal names need larger
  pools per nationality: a content task (audit P1).
- **Nationality weights are the content's ASIA-region shares.** A multi-region fictional world would need region weights
  per league. **OPEN QUESTION.**
- **The Overall comes from real tier aggregates** (DEC-031 proposes fitting generated players to the real pack). Skill
  covariance and archetypes are still authored, so the profiles are not yet calibrated: GENERATOR_V2_PLAN §7 needs the
  Oracle's Elixir file.
- **The age range ends at 31**, because calibration cells under 3 players are dropped. Older NPCs appear only through
  ageing.
