# Hybrid world: integration review and approval gate

Date: 2026-10-15. Status: **EXPERIMENT COMPLETE — APPROVAL GATE**. Nothing was merged into production. **Verdict:**
1. The experimental world meets every requested constraint in isolation (40/40 checks).
2. **Integrating it into production needs a substantial rewrite** of world state, persistence and the player lifecycle.
3. As the task requires, work stops here, with a minimal proposal and the decisions it needs.

Labels: **FACT**, **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. What exists now (all outside production)

| Part | Where | State |
|---|---|---|
| Uncapped research rating scale | `tools/lol-data-ingestion/rating_scale_experiment.py` → `data/ratings/experiment-uncapped/` | Built, run |
| Real-team roster pack and validation | `build_real_rosters.py` (+ 8 tests) → `data/world/` | Built, run, tested |
| Aggregate NPC calibration | `world_calibration.py` → `data/world/npc_calibration_2026.json` | Built, run |
| dev2 development model, NPC generator, world builder, off-screen seasons, market, experimental snapshot, 40 checks | `tools/WorldExperiment/` (C# console project, **not** in `ManagementGame.slnx`; references production Domain/Infrastructure read-only) | Built, run, tested |
| Datasets: 4,000 NPCs, hybrid snapshots, reports | `tools/lol-data-ingestion/data/world/experiment/` (git-ignored) | Local only |

## 2. Conflicts with the production architecture (FACT, with code references)

| # | Area | Production today | The experiment needs | Consequence |
|---|---|---|---|---|
| A1 | World size | One company + 7 rivals (`Balance.RivalCount`); rival squads 6–8 (`RivalRules.RosterSizes`); company `MaxRoster` 8 | 764 teams in 5 layers; squads up to 16; ~5,100 active people | A new population layer. The rival model cannot absorb thousands of players |
| A2 | Free-agent pool | 20–30 (`Lifecycle.PoolSize/PoolCap`). Above the cap the oldest retire (`Lifecycle.cs:102`). A save is rejected above PoolCap + YouthIntake + 5 + (RivalCount + 1) × MaxRoster = 114 candidates (`SnapshotValidation.cs:77`) | 250–350 NPC free agents after the window; ~900–1,150 as it opens | The pool cap and the validator bound must go |
| A3 | Save size | `SnapshotStore` rejects files over 8,000,000 bytes (`SnapshotStore.cs:72`). `StrictJson` limits arrays to 20,000 elements and strings to 4,096 characters (`ContentLoader.cs:19, 38, 41`) | 12.4 MB at creation; 35.4 MB after 10 seasons; largest array 67,928 events | The save contract (schema 6) cannot hold the world |
| A4 | History | `World.Moves` is pruned to the current and previous season (`Model.cs:111`, `Simulation.cs:413`) | Persistent careers for every person | A retention policy and a history store are needed |
| A5 | Ownership | A person lives inside `Company.People`, `Rival.People` or `Candidate` (ownership by containment); validated by uniqueness (`SnapshotValidation.cs:33`) | Organization / team / contract / registration / lineup / history as separate records | A new entity model. The company and the player's league become views of it |
| A6 | Identity | `person:gNNNNN`. Handles are drawn from 525 combinations with no uniqueness (audit P1) | Unique handles over ~9,000 NPCs plus every real handle | The generator's handle grammar and a uniqueness rule |
| A7 | Development | One age-band draw; every skill moves alike; potential static (`Lifecycle.cs:16–36`) | dev2: per skill, environment, states, dynamic potential | Content blocks and a rules change. Moves balance (§6) |
| A8 | Generation | Ability bands to 92; potential = rating + headroom ≤ 14 | NPC cap 82, potential to 99, calibrated tiers | A content and generator change (relates to GENERATOR_V2_PLAN) |
| A9 | Determinism | Integer arithmetic (`Numbers.Divide`); rng-v1 | The experiment uses IEEE doubles (basic operations, square root) on rng-v1 | A port to integers for cross-machine replay (DV-06) |
| A10 | Save identity | A save binds the rules version and the content hash (`SnapshotStore.cs:74`) | New rules and content | Old saves are rejected, as DV-08 allows. No migration is promised |
| A11 | Content limits | 20 organization names; nationality pools of 16 × 16 | ~800 team names; ~9,000 people | Larger pools, or generated names (the experiment generates them) |

**INFERENCE.**
- **The parts that fit as they are:**
  - the rating formula (`Ratings.InRole`, used unchanged: 0 mismatches on 1,135 real players);
  - the series resolver (`Competition.Play`, used for every off-screen league series);
  - rng-v1, the wage and fee curves (`Pay`);
  - the five roles.
- **The rival lineup search is not a bottleneck at 16 players:** 0.12 ms against 0.024 ms at 8 (§3).
- **The blockers are structural:** A1–A5.

## 3. Performance (FACT; this machine, ~8 GB RAM, Release, .NET 10; three seeds and the baseline)

| Measure | Value |
|---|---|
| Creating the world: 4,000 NPCs, 764 teams, allocation, backstories, 1,135 real players | 3.0–4.3 s |
| Managed memory of the world at creation | 12.6 MB |
| Peak working set of the test process (creation, 11 seasons, a replay world, a shuffled-pack world, snapshots) | 530 MB |
| One off-screen season (about 4,700 series, development of 4,150–5,135 active people, the market) | mean 1.5–1.7 s, max 2.1 s (other runs on a loaded machine: 0.85–2.3 s) |
| Snapshot at creation / after 10 seasons | 12.38 MB / 35.36 MB (+2.3 MB a season) |
| Serialize, then load, at creation | 0.17–0.25 s, then 0.32–0.49 s |
| Serialize, then load, after 10 seasons | 0.54–0.76 s, then 0.90–1.34 s |
| Records in the season-10 snapshot | 9,570 people (4,153 active), 848 teams (84 dissolved and refounded), 40,886 contracts, 67,928 events |
| Free-agent query (role and level) | 0.24–0.35 ms |
| Scouting query (young mids with potential 80+, top 20) | 0.32–0.72 ms |
| Production `Rivals.Lineup` | squad 6: 0.014 ms; 8: 0.024 ms; 12: 0.051 ms; 16: 0.121 ms |
| Replay: creation + 11 seasons, a second time | 17–27 s |

**Determinism (FACT).**
- The same seed gives the same world hash after 11 seasons (`0c147a30…`).
- A world saved after 10 seasons and reloaded continues to the same state as the live world.
- Shuffling the real pack's team order gives the same world.
- On four seeds: 0 invariant failures and 0 history violations.

**INFERENCE.**
- **Throughput is not the problem.** A season-level off-screen world costs about 2 seconds a year, and the queries are
  far inside the Phase 5 budget (≤ 250 ms at 10k).
- **Size is the problem.** Without a retention or compaction rule, history grows by about 2.3 MB a season. Of the
  growth, about a third is one-season amateur contract extensions, their events and contracts together
  (NPC_EMPLOYMENT_DISTRIBUTION E3). About another quarter is retired people kept for their history.

## 4. The real-team rule over time

**The rule.** NPCs never take a real team's starting slot (STRICT, through every season).

**What happens (baseline, FACT):**

| Season | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Real players active | 1,135 | 1,069 | 973 | 866 | 756 | 636 | 513 | 387 | 293 | 219 | 153 |
| Real players retired in the season | — | 66 | 96 | 107 | 110 | 120 | 123 | 126 | 94 | 74 | 66 |
| Real-to-real transfers | — | 123 | 116 | 93 | 106 | 91 | 81 | 71 | 64 | 36 | 2 |
| Real starting slots left empty after the window | — | 73 | 168 | 263 | 348 | 439 | 549 | 670 | 759 | 831 | 897 |
| Real teams (of 210) with an empty starting slot | 0 | 39 | 67 | 84 | 86 | 112 | 146 | 156 | 164 | 177 | 203 |
| Mean real team strength (an empty slot counts 25) | 61.5 | 59.3 | 56.6 | 53.8 | 50.8 | 47.4 | 43.3 | 39.1 | 36.0 | 33.3 | 31.0 |

**FACT.** No real player can be created: that would invent a real person. Real players retire, so real squads empty.
- By season 3, 40% of real teams cannot field five real players.
- By season 10, 97% cannot.

The same happens on all four seeds: 203–206 teams at season 10. The rule held (0 NPC starters on real teams), and the
real teams collapsed.

**The alternative, measured but not adopted (`--variant npc-fallback`).** A real team may start an NPC in a role no real
player can fill:
- NPC starters on real teams rise to 74 (season 1), 437 (season 5) and 896 (season 10);
- every real team fields five, with a mean strength of 61.0 at season 10;
- the NPC free-agent share falls to 5.0–7.7%.

**INFERENCE.** With strict real-only lineups a hybrid world lasts about 2–3 seasons before real leagues degrade. Any
long campaign needs a decision (D-H1).

## 5. Fidelity layers

| Layer | In the experiment | Proposed for production |
|---|---|---|
| The player's league | Not modelled (no company) | The existing daily simulation (rules v4), its teams taken from the population |
| Other real and fictional professional leagues | Season-level: double round robin of best-of-3 from lineup strength (`Competition.Play`), playing time from the lineup and rotation, season budget | The same, run once a season, or once a split |
| Semi-professional | The same as professional | The same |
| Academies and amateurs | Single round robin of best-of-1 | The same; or no matches, with playing time from squad position |
| Free-agent market | A persistent pool; one window a season; scouting noise by layer; willingness and reservation wage | The same, with the player's window inside the daily calendar |
| Development | dev2 once a season for every active person | dev2 for the player's league, possibly per split. Off-screen once a season |

No off-screen team runs daily matches, finances or coaching. Development uses each team's coach number and playing time
only.

## 6. Effects on the existing game (not measured: production untouched)

| Effect | Status |
|---|---|
| **Gameplay gates** (DEC-029, rules v4) | Not run, because no production rule changed. Adopting dev2 or the NPC generator would move them. GENERATOR_V2_PLAN §6 requires 48 paired seeds for generator changes |
| **dev2 versus production development** | dev2 is more sensitive to the environment: case B poor 74.8 / average 81.7 / strong 88.9, against 79.2 / 83.4 / 88.5. So coaching quality and playing time become stronger levers for the owner. That is a balance decision |
| **NPC cap of 82** | Below the current rival levels (the rival range is 68–82 plus Edge 6). The player's league would need recalibrating if it is drawn from the population |

## 7. Minimal production integration proposal (PROPOSAL; nothing done)

Each step is gated, in order, and stops on its condition.

| Step | Work | Needs | Stop if |
|---|---|---|---|
| I-0 | The decisions of §8 | Director | — |
| I-1 | **Data contracts:** a population module in Domain with ID-keyed records (organization, team, person, contract, registration, career event). The company and the player's league become views of it. A history retention rule (keep career-changing events; fold yearly amateur extensions). A content block for world structure | D-H2, D-H3, E3 | The company's existing behaviour changes (Integration 369 must hold unchanged) |
| I-2 | **Persistence:** save schema 7. Either a split world file with measured byte and array budgets, or an append-only history file beside the snapshot; integrity checks; old saves rejected (DV-08) | D-H3 | Load or save exceeds the Phase 5 budget measured at the approved scale (DV-07) |
| I-3 | **Generator and development:** port the NPC generator and dev2 to integer arithmetic with golden tables (audit C7); content blocks `Development` and `WorldPopulation`; 48-seed gate measurement | D1–D4, GENERATOR_V2_PLAN G1 | Any gate fails at 48 seeds (report and ask; no tuning inside the step) |
| I-4 | **The off-screen world:** season-level leagues and the market as in the experiment, the player's league daily | E1, E2, F1–F4 | Determinism or history invariants fail |
| I-5 | **The real pack** (DEC-031): a local content-pack variant validated like content and pinned by hash; never committed | DEC-031 accepted, D-H1 | Terms forbid the use; verification coverage is below the agreed set |
| I-6 | **UI:** market and scouting search over thousands of people, with estimates by scouting confidence (DEC-005) | UI rules; F4 | Query p95 above 250 ms at 10k |

## 8. Decisions required before production integration

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| **D-H0** | Proceed to integration at all | (a) yes, by the gated steps of §7; (b) keep the vertical slice small and use the population only to generate the market and rivals | Decide after D-H1. Without a real-team policy the hybrid world does not last a campaign |
| **D-H1** | Real starters after the snapshot | (a) STRICT, accepting decay within about 3 seasons; (b) NPC fallback after the snapshot, with the starter visibly marked fictional; (c) real teams become fictional organizations, or leave the playable set, when they cannot field five real players; (d) real players only for a one-season scenario | (b) or (c); the measured effects are in §4 |
| D-H2 | Scope of real teams | (a) all 210 playable verified teams in 31 leagues; (b) tier a/b only (54 teams); (c) none (fictional default) | (b) for a first pack: it has the highest verification coverage |
| D-H3 | Save and history budget | (a) a single JSON snapshot with raised limits; (b) a snapshot plus an append-only history file; (c) a binary or SQLite store | (b): it keeps the snapshot small and the history immutable |
| D1–D4 | Rating scale top, late growth, dev2 adoption, training focus as a player decision | See PLAYER_POTENTIAL_TRAINING_EXPERIMENT §10 | — |
| E1–E4 | World size, intake rule, amateur contracts, academy overlap | See NPC_EMPLOYMENT_DISTRIBUTION §8 | — |
| F1–F4 | Free-agent maximum, reservation wage, intake, what the player sees | See FREE_AGENT_MARKET_VALIDATION §9 | — |
| D-H4 | Real-data licensing | PandaScore forbids redistribution; Oracle's Elixir's terms are unread; image and likeness rights are unresolved | Keep everything local (DEC-031); no build ever contains the pack |

## 9. Risks

| Risk | Where | Guard |
|---|---|---|
| Real-team collapse in long campaigns | §4 | D-H1 |
| Save and history growth (35 MB after 10 seasons) | §3 | D-H3, retention (E3) |
| A balance shift from dev2 | §6 | 48-seed gates (I-3) |
| Cross-machine determinism of a double-based model | A9 | An integer port before acceptance (DV-06) |
| Authored coefficients mistaken for calibration (spread of growth, retirement by layer, wages by layer) | All reports | Labels; a versioned config; real targets after the Oracle's Elixir file |
| Real-data rights | Pack | Git-ignored storage; DEC-031 |

## 10. Tests actually executed (2026-10-15)

| Suite | Result |
|---|---|
| Production Domain tests (`tests/Domain`, Release) | **15/15 PASS** |
| Production Integration tests (`tests/Integration`, Release) | **369/369 PASS** (hash `e196d247…`) |
| Data tool tests (`python -m unittest discover -s tests`) | **113/113 PASS**, including the 8 new roster tests |
| World experiment checks (`WorldExperiment test --seasons 10`) | **40/40 PASS**: ratings R01–R10, real teams T01–T07, population N01–N10, market M01–M11, persistence P01–P02 |
| Determinism | Replay equal; reload-continue equal; pack-order independent; 4 seeds with 0 invariant failures |
| Not run | `build/Verify.ps1` (UI, Godot headless, captures), and the rules gate measurement: no production code changed |

## 11. Not done (on purpose)

- **No production change:** code, content, rules, save schema and decision log are untouched. Nothing was committed or
  pushed.
- **No new data was fetched.** Oracle's Elixir is still absent, and academy relations were not fetched.
- **Out of scope:** injuries, facilities, negotiation, loans, visible scouting confidence, UI, and daily development
  ticks were not invented.
- **Real players** keep neutral personality and development traits. Nothing about them is inferred.
