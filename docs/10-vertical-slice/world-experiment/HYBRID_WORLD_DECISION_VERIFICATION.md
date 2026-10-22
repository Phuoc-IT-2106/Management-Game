# Hybrid world: four-decision verification

Date: 2026-10-15. Status: **EXPERIMENT COMPLETE — DIRECTOR APPROVAL GATE**. Four decision proposals (D-H1, D1, E2/F1,
D-H3), each with measured evidence. **None of them is an accepted decision, and none is integrated:** production code,
content, rules, the save schema and the decision log are unchanged. Nothing was committed or pushed.

Labels: **FACT** (measured or read in code), **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**. Results are PASS, FAIL,
NOT RUN or INCONCLUSIVE.

This report continues [HYBRID_WORLD_INTEGRATION_REVIEW §8](HYBRID_WORLD_INTEGRATION_REVIEW.md#8-decisions-required-before-production-integration).

## 0. Inspection record (FACT)

| Item | Value |
|---|---|
| Branch / HEAD | `stage5-sponsor-instruments` / `587b910` |
| Working tree before this task | 24 modified tracked files (the 2026-10-14 rating fixes, the data-standard audit, data tooling) and the untracked world experiment. All preserved; none was edited by this task |
| Production content | `content/fixture.json` SHA-256 `ea764b69…` (matches `content/fixture.sha256`) |
| Real roster pack | `real_roster_snapshot_2026-10-13.json` `3674c6f0…` (git-ignored) |
| NPC calibration / exclusions | `f28db406…` / `a92ef3e9…` (git-ignored) |
| Research ratings used by the pack | uncapped experiment `c64684f9…`; v2 `ca2d04bb…` (git-ignored) |
| World experiment config | `world-experiment.v1.json` `ba778fdf…` (unchanged); new `decisions.v1.json` for the variants of this report |

**Artifact identity was verified before any new run.** `WorldExperiment test --seasons 10` was rerun into a separate
directory and reproduced the existing artifacts bit for bit:
- the snapshot hashes at creation and after 10 seasons (`8d6d8837…`, `aeaa897e…`);
- the replay hash (`0c147a30…`);
- every season state, every development row, and the 40/40 test results.

After this task's changes to the experiment tool, the same run was repeated and still reproduced those three hashes
(40/40). So the v1 experiment remains reproducible, and every new behaviour sits behind a new option.

**What this task added** (all outside production, all uncommitted):

| File | Purpose |
|---|---|
| `tools/WorldExperiment/Decisions.cs`, `config/decisions.v1.json` | The `decide` command: a variant over a seed list, metrics at seasons 0, 1, 3, 5, 10 and 20 |
| `tools/WorldExperiment/Season.cs`, `Model.cs`, `Checks.cs`, `RealPack.cs`, `Generation.cs`, `Config.cs` (edited) | Policies NPC_OPEN and EXIT; intake modes OneToOne, OneToOneAll and Pathways; the D1a ceiling tail; the D1b pack adjustment; diagnostics. Each is off in the v1 configuration |
| `tools/WorldExperiment/PersistenceLab.cs`, `PersistenceRuns.cs`, `Sqlite.cs` | S1/S2/S3 stores, the benchmark, fault injection and process-kill trials |
| `tools/WorldExperiment/decision_summary.py` | Means, standard errors, paired differences, integrity and determinism checks |
| `tools/lol-data-ingestion/elite_scale_experiment.py`, `tests/test_elite_scale.py` | D1b: the elite tail, transform slopes, a calibration on 2026 games, the K96 sidecar |

Outputs stay in git-ignored `tools/lol-data-ingestion/data/world/decisions/`, because runs read the real pack.

## 1. Executive summary

**What was investigated.** Four decisions on the hybrid world, in 20-season simulations of the full 4,000-NPC world with
the 210 verified real teams:
- **D-H1:** what happens to real teams when their real players leave;
- **D1:** where the practical rating ceiling comes from, and whether to raise it;
- **E2/F1:** a one-to-one replacement intake, and no free-agent cap during play;
- **D-H3:** how to keep a full career history across saves.

**What was implemented experimentally:**
- three real-starter policies, plus one sensitivity policy;
- four intake rules;
- two ceiling variants and one elite re-scaling;
- three persistence stores, with fault injection and real process kills.

**What was verified.**

| Decision | Finding |
|---|---|
| D-H1 | Under the strict rule (A), real teams lose their complete lineups within a few seasons: 49% have had a hole by season 5, 97% by season 10, all by season 20. The transparent fallback (B) keeps 99.7% of them fielding five through season 20, through market signings only. Withdrawal (C) removes every real organization by season 20. **But under every policy, real players are gone by season 20**: B preserves organizations, not authentic rosters |
| D1 | The ceiling near 93 comes from the research scale's compressed top segment, not from any clamp. The one practical clamp in the world experiment is the NPC ceiling's asymptote, which made 99 unattainable. The minimal fix (D1a) leaves every ceiling below 96 unchanged and makes 97–99 attainable but rare. Stretching the elite (D1b) is consistent with game results, but the evidence is in-sample and within one standard error |
| E2/F1 | One-to-one replacement holds the NPC population at exactly 4,000 in every season and seed, with no cap; the free-agent share moves by itself, between 3.7% and 9.4% in A and B. **Structural problem:** it counts NPC retirements only. When real teams recruit NPCs (B), the 1,135 real retirees are never replaced, so the lower fictional layers thin out (about 830 empty squad seats by season 20). Counting every retirement removes the gap |
| D-H3 | All three stores pass the critical crash scenario and 12 real process kills each. At season 20 the simplest, a single compressed snapshot (S1-gzip), is 9.3 MB, saves in 0.6 s and loads in 1.3 s. S2 (snapshot + journal) prevents by protocol the inconsistency a naive loader shows, but costs about the same, because its state snapshot still holds every retired person and old contract. S3 (SQLite) does not detect a silently changed event row |

**What remains uncertain:**
- whether the authored coefficients (retirement, wages, willingness) match reality;
- the out-of-sample calibration of the elite scale;
- the effect on the player's own league, which this experiment does not model (no company);
- second-machine determinism (DV-06);
- persistence durability against power loss (only process kills and simulated torn writes were tested).

## 2. Decision matrix

| Decision | Options tested | Recommended choice | Evidence | Risks | Approval required |
|---|---|---|---|---|---|
| **D-H1** | A strict; B transparent NPC fallback; B-open (merit, sensitivity); C withdrawal | **B**, with C kept only for genuine dissolution (insolvency, a deliberate exit), never as the answer to a roster gap | §3: A 0 of 210 complete at S20; B 209.4; C 0 active | Authenticity erodes under every policy; B keeps weaker real players over better NPCs; real teams drain the fictional pyramid | **Yes**: B relaxes the earlier rule that no NPC may start for a real team (a requirement of the world-experiment task, not a DEC entry), after the snapshot only |
| **D1** | D1a: research clamp removed (done), NPC ceiling tail; sensitivity: wider soft room; D1b: elite re-knot K96 (also K98, L1 static) | **D1a** (tail 96 → 99). D1b as a separate band decision after an out-of-sample check | §4: D1a 0.00 change in 95+ potential; 99 attainable; D1b slope 1.28 → 1.11 in elite games | D1a negligible; D1b raises top wages and elite separation | Yes (D1a: generator rule; D1b: the user's band decision of 2026-10-14) |
| **E2/F1** | One-to-one (NPC); bounded (v1); fixed 480; pathways (mixed ages); one-to-one over all retirements | **One-to-one with no campaign cap**; with B, count every retirement (OneToOneAll) | §5: exactly 4,000 active NPCs; free agents 3.7–10.8%, no warning season | Younger population (16–21 share 33% → about 50%); a lower-layer talent decline of about 5 points | Yes |
| **D-H3** | S1 snapshot (+ gzip); S2 snapshot + journal; S3 SQLite | **Approve the requirement** (durable career history). Preferred design: **S1 with compression**; S2 is the verified scale-up path | §6: S1-gzip at S20: 9.3 MB, save 0.6 s, load 1.3 s; every crash scenario and kill passes | Save cost grows with all history (linear); the save limits must be raised; a corrupted save rolls back one save | Yes for the requirement; the architecture after the conditions of §9 |

## 3. Real-team longevity (D-H1)

### 3.1 Variants (EXPERIMENT)

All variants start from the same world for a given seed. The season-0 state digest is identical across A, B, B-open, C
and the market variants on every shared seed (§7).

| Variant | Rule |
|---|---|
| **A STRICT** | Real teams start real players only, through every season. A real team may hold NPC reserves in development seats; they never play |
| **B NPC_FALLBACK** | At creation, the verified real five; no NPC starts. After season 0, a real team with a vacancy first looks for a real player. Failing that, it may sign an NPC **through the ordinary market**: the fee from its funds, its wage cap, the player's willingness and reservation wage, and the 16-player limit. The NPC stays fictional, and the imported roster stays in Snapshot-era history. Real players start first; an NPC starts only in a role no real squad member can fill |
| **B-open** (sensitivity; BO in the tables) | As B, but real teams pick and recruit on merit, whatever the origin |
| **C EXIT** | As A. A real organization that still cannot field five after the window withdraws: it is marked inactive, keeps its identity (never renamed), releases its people with a recorded exit, and its league slot stays empty |

**Nothing spawns a player in any variant.** The only player creation is the NPC intake (§5), and it never joins a team
directly.

### 3.2 Survival (48 seeds for A, B and C; 12 for B-open; FACT)

*Real teams of 210 (mean ± SE across seeds).*

| Variant / measure | S0 | S1 | S3 | S5 | S10 | S20 |
|---|---:|---:|---:|---:|---:|---:|
| A (48 seeds): fielding five starters | 210.0 ± 0.0 | 165.6 ± 0.5 | 128.1 ± 0.3 | 107.9 ± 1.3 | 6.6 ± 0.4 | 0.0 ± 0.0 |
| B (48 seeds): fielding five starters | 210.0 ± 0.0 | 209.0 ± 0.1 | 209.5 ± 0.1 | 209.5 ± 0.1 | 209.6 ± 0.1 | 209.4 ± 0.1 |
| BO (12 seeds): fielding five starters | 210.0 ± 0.0 | 206.2 ± 0.5 | 208.6 ± 0.3 | 209.9 ± 0.1 | 210.0 ± 0.0 | 210.0 ± 0.0 |
| C (48 seeds): fielding five starters | 210.0 ± 0.0 | 165.6 ± 0.5 | 134.8 ± 0.5 | 86.2 ± 0.9 | 10.7 ± 0.4 | 0.0 ± 0.0 |
| A (48 seeds): fielding five real starters | 210.0 ± 0.0 | 165.6 ± 0.5 | 128.1 ± 0.3 | 107.9 ± 1.3 | 6.6 ± 0.4 | 0.0 ± 0.0 |
| B (48 seeds): fielding five real starters | 210.0 ± 0.0 | 162.2 ± 0.6 | 120.0 ± 0.5 | 96.9 ± 1.0 | 7.0 ± 0.4 | 0.0 ± 0.0 |
| BO (12 seeds): fielding five real starters | 210.0 ± 0.0 | 78.7 ± 1.2 | 25.6 ± 1.4 | 7.7 ± 0.6 | 0.1 ± 0.1 | 0.0 ± 0.0 |
| C (48 seeds): fielding five real starters | 210.0 ± 0.0 | 165.6 ± 0.5 | 134.8 ± 0.5 | 86.2 ± 0.9 | 10.7 ± 0.4 | 0.0 ± 0.0 |
| A (48 seeds): still competing | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 |
| B (48 seeds): still competing | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 |
| BO (12 seeds): still competing | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 | 210.0 ± 0.0 |
| C (48 seeds): still competing | 210.0 ± 0.0 | 165.6 ± 0.5 | 134.8 ± 0.5 | 86.2 ± 0.9 | 10.7 ± 0.4 | 0.0 ± 0.0 |

*Real teams that have had an incomplete lineup (% of team-runs, cumulative).*

| Variant | S1 | S3 | S5 | S10 | S20 | Never in 20 seasons | Median first season |
|---|---:|---:|---:|---:|---:|---:|---:|
| A | 21.1 | 39.5 | 49.0 | 96.9 | 100.0 | 0.0 | 6.0 |
| B | 0.5 | 0.9 | 1.3 | 2.5 | 5.0 | 95.0 | > 20 |
| BO | 1.8 | 2.3 | 2.3 | 2.3 | 2.3 | 97.7 | > 20 |
| C | 21.1 | 35.8 | 58.9 | 94.9 | 100.0 | 0.0 | 5.0 |

*Ever incomplete by tier (%; tier a 47, b 7, c 70, d 86 teams).*

| Variant | Tier | S1 | S3 | S5 | S10 | S20 |
|---|---:|---:|---:|---:|---:|---:|
| A | a | 0.0 | 0.0 | 0.0 | 86.0 | 100.0 |
| A | b | 0.0 | 0.0 | 0.0 | 100.0 | 100.0 |
| A | c | 1.3 | 1.3 | 24.2 | 100.0 | 100.0 |
| A | d | 50.5 | 95.3 | 100.0 | 100.0 | 100.0 |
| B | a | 0.0 | 0.0 | 0.0 | 0.0 | 0.8 |
| B | b | 0.0 | 0.0 | 0.0 | 4.2 | 8.9 |
| B | c | 0.7 | 0.7 | 0.7 | 2.1 | 4.9 |
| B | d | 0.6 | 1.6 | 2.6 | 4.0 | 7.0 |
| BO | a | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| BO | b | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| BO | c | 1.4 | 2.0 | 2.0 | 2.0 | 2.0 |
| BO | d | 3.2 | 4.1 | 4.1 | 4.1 | 4.1 |
| C | a | 0.0 | 0.0 | 0.0 | 77.3 | 100.0 |
| C | b | 0.0 | 0.0 | 0.0 | 100.0 | 100.0 |
| C | c | 1.3 | 1.3 | 53.9 | 100.0 | 100.0 |
| C | d | 50.5 | 86.4 | 100.0 | 100.0 | 100.0 |

**Reading (INFERENCE).**
- **A decays from the bottom up.** Half of the tier-d teams have a hole after one season; tier a and b stay complete
  until about season 5, then collapse between seasons 5 and 10. The median real team first fails in season 6.
- **B keeps the organizations.** About 0.5 real teams per checkpoint end a season with a hole, but only about one
  vacancy per run (in 20 seasons) found no candidate: it was over the wage cap. The rest most likely come from a gap in
  the experiment's recruitment loop (INFERENCE): a signing can change the best assignment so that a role already
  handled in that window is empty again, and the loop does not revisit it. A C-style exit would remove at most those
  teams.
- **C is worse than A for the league.** Exits free real players for the survivors, so C keeps slightly more complete
  teams at season 3. After that it empties the real leagues: about 10 of 31 leagues have 4+ teams at season 5, none at
  season 20.
- **Authenticity has the same horizon under every policy.** "Fielding five real starters" follows A's curve in B too:
  no source adds real players, and all 1,135 retire within 20 seasons. B extends the life of the organizations, not of
  the real rosters.
- **Merit recruitment (B-open) removes authenticity almost at once.** After one season only 79 of 210 real teams field
  five real starters (B 162); after three, 26. Real tier-a teams get stronger (83 against 80 at season 5), and fictional
  professionals weaker. B's real-first rule is what keeps real rosters on screen while real players exist.

### 3.3 Recruitment, shortages and consequences (FACT)

*Recruitment and consequences over 20 seasons (per run).*

| Measure | A | B | BO | C |
|---|---:|---:|---:|---:|
| Real vacancies filled with a real player | 740 ± 3 | 663 ± 3 | 72 ± 2 | 727 ± 3 |
| Real vacancies filled with an NPC (market signing) | 0 ± 0 | 4553 ± 9 | 3441 ± 23 | 0 ± 0 |
| Real vacancies left unfilled (team-role-windows) | 15186 ± 14 | 1 ± 0 | 9 ± 1 | 397 ± 2 |
|   no allowed candidate exists | 3933 ± 95 | — | — | 2 ± 0 |
|   candidates held by teams it may not buy from | 10540 ± 98 | — | — | 290 ± 3 |
|   candidates unwilling (level or reservation wage) | 712 ± 26 | — | — | 105 ± 3 |
|   over the wage cap / fee above funds | 1 ± 0 / — | 1 ± 0 / — | 9 ± 1 / — | 1 ± 0 / — |
| Upgrades (real / NPC) | 98 ± 1 / 0 ± 0 | 127 ± 2 / 0 ± 0 | 40 ± 1 / 1268 ± 11 | 120 ± 2 / 0 ± 0 |
| Real-to-real transfers | 827 ± 3 | 785 ± 3 | 94 ± 2 | 607 ± 3 |
| Transfer fees paid by real teams (x100 minor units) | 78321 ± 389 | 355625 ± 1444 | 372174 ± 3442 | 71214 ± 441 |
| Organizations withdrawn | 0.0 ± 0.0 | 0.0 ± 0.0 | 0.0 ± 0.0 | 210.0 ± 0.0 |
| Real players retired | 1135 ± 0 | 1135 ± 0 | 1135 ± 0 | 1135 ± 0 |
| Series won by the stronger real team (%) | 72.3 ± 0.1 | 66.5 ± 0.0 | 65.6 ± 0.1 | 67.3 ± 0.1 |

*State at S5 / S10 / S20.*

| Measure | A | B | BO | C |
|---|---:|---:|---:|---:|
| Real players active | 640 ± 3 / 155 ± 2 / 0 ± 0 | 642 ± 3 / 155 ± 2 / 0 ± 0 | 592 ± 4 / 131 ± 2 / 0 ± 0 | 589 ± 3 / 88 ± 2 / 0 ± 0 |
| NPC starters on real teams | 0 ± 0 / 0 ± 0 / 0 ± 0 | 440 ± 2 / 895 ± 2 / 1049 ± 0 | 622 ± 4 / 942 ± 2 / 1050 ± 0 | 0 ± 0 / 0 ± 0 / 0 ± 0 |
| Mean strength, real tier a (an empty slot counts 25) | 80.1 ± 0.1 / 51.2 ± 0.3 / 25.0 ± 0.0 | 80.1 ± 0.1 / 70.7 ± 0.2 / 77.2 ± 0.1 | 83.3 ± 0.1 / 80.4 ± 0.1 / 77.0 ± 0.1 | 80.1 ± 0.1 / 73.7 ± 0.3 / — |
| Mean strength, all real teams | 47.1 ± 0.1 / 30.9 ± 0.1 / 25.0 ± 0.0 | 59.8 ± 0.0 / 61.4 ± 0.0 / 61.2 ± 0.0 | 65.2 ± 0.1 / 62.8 ± 0.0 / 60.7 ± 0.1 | 68.4 ± 0.1 / 73.7 ± 0.3 / — |
| Mean strength, fictional professional teams | 76.9 ± 0.1 / 75.2 ± 0.1 / 73.8 ± 0.1 | 77.0 ± 0.0 / 71.5 ± 0.1 / 67.3 ± 0.1 | 73.1 ± 0.1 / 68.3 ± 0.2 / 65.6 ± 0.1 | 76.9 ± 0.1 / 75.6 ± 0.1 / 75.1 ± 0.1 |
| Real leagues (of 31) with 4+ active teams | 26.0 ± 0.0 / 26.0 ± 0.0 / 26.0 ± 0.0 | 26.0 ± 0.0 / 26.0 ± 0.0 / 26.0 ± 0.0 | 26.0 ± 0.0 / 26.0 ± 0.0 / 26.0 ± 0.0 | 9.9 ± 0.2 / 0.7 ± 0.1 / 0.0 ± 0.0 |
| Real teams with negative funds | 0.0 ± 0.0 / 0.0 ± 0.0 / 0.0 ± 0.0 | 0.0 ± 0.0 / 0.0 ± 0.0 / 0.0 ± 0.0 | 0.0 ± 0.0 / 0.0 ± 0.0 / 0.0 ± 0.0 | 0.0 ± 0.0 / 0.0 ± 0.0 / 0.0 ± 0.0 |
| Real teams: season wages / revenue | 0.34 ± 0.00 / 0.12 ± 0.00 / 0.06 ± 0.00 | 0.54 ± 0.00 / 0.50 ± 0.00 / 0.47 ± 0.00 | 0.69 ± 0.00 / 0.59 ± 0.00 / 0.55 ± 0.00 | 0.56 ± 0.00 / 0.56 ± 0.01 / — |
| NPCs rated 75+ employed by real teams | 12 ± 0 / 22 ± 1 / 21 ± 1 | 13 ± 1 / 112 ± 2 / 180 ± 2 | 118 ± 2 / 190 ± 2 / 165 ± 3 | 12 ± 0 / 5 ± 0 / 0 ± 0 |
| NPC free agents after the window (%) | 6.9 ± 0.0 / 6.9 ± 0.0 / 6.7 ± 0.0 | 5.4 ± 0.0 / 4.6 ± 0.0 / 4.9 ± 0.0 | 5.0 ± 0.1 / 5.2 ± 0.1 / 5.5 ± 0.1 | 8.4 ± 0.0 / 9.5 ± 0.1 / 9.7 ± 0.1 |
| Pre-window free agents rated 70+ | 28 ± 1 / 16 ± 1 / 15 ± 1 | 27 ± 1 / 9 ± 0 / 3 ± 0 | 16 ± 1 / 7 ± 1 / 7 ± 1 | 28 ± 1 / 25 ± 1 / 14 ± 0 |

*Real vacancies by role under A (S5 / S10).*

| Role | Vacancies |
|---|---:|
| top | 83 ± 1 / 175 ± 1 |
| jungle | 82 ± 1 / 176 ± 1 |
| mid | 87 ± 1 / 180 ± 1 |
| bot | 89 ± 1 / 179 ± 1 |
| support | 96 ± 1 / 185 ± 1 |

**Reading (INFERENCE).**
- **Why A's vacancies stay open.** Most of A's open vacancies have real candidates, but those candidates are held by
  teams the buyer may not buy from: the experiment's market lets a team poach only from a lower layer, or, for a
  contender, a non-starter of a lower-reputation team in its own layer.
  - So part of A's decay is a market-rule effect.
  - A looser real-to-real market would delay it. It cannot prevent it, because the real population only shrinks.
- **No role is scarcer than another.** Vacancies spread evenly across roles; support is slightly ahead.
- **Finance.** No variant drives real teams into debt.
  - Under A, real teams with empty slots pay almost no wages (0.06 of revenue at season 20), so they accumulate cash.
    They are economically inert organizations.
  - Under B, wages stay at about half of revenue (0.47–0.54), and transfer fees are about 4.5 times A's: the fallback
    is paid for through the market.
- **Competitiveness.** In A the stronger team wins 72% of real series, because complete teams play hollow ones. In B
  it wins 67%, the same as C's surviving teams.
- **Market pressure (B).** Real teams buy the best NPCs:
  - by season 20, 180 NPCs rated 75+ play for real teams;
  - fictional professional teams fall from strength 77 to 67;
  - the pre-window market offers 3 players rated 70+, against 15 under A (§5.3).

### 3.4 Simulation validity against gameplay quality

| Aspect | Result |
|---|---|
| **Mechanical validity of B** | **PASS**: 0 invariant failures and 0 history violations in every checkpoint of every run; squads ≤ 16 (max registered 12); no NPC starts at creation; every NPC on a real team arrived by a recorded market signing with a fee or a free-agent contract |
| **Credibility of B's recruitment** | **INCONCLUSIVE.** Real players start first even when a signed NPC is better. An NPC who fills a role is replaced only by a real player at least 4 points better (an upgrade); an NPC is never upgraded to a better NPC. B-open (merit) measures the alternative (§3.2 tables) |
| **Player-facing effect** | **NOT RUN.** The experiment has no company. Proxies: the authenticity of rival lineups (the real-starter share falls under every policy), and the talent the market offers (B consumes it, §5.3) |

**Recommendation (PROPOSAL).**
- **B**, when the Director accepts that an NPC may start for a real team after the snapshot.
- **C** stays a valid mechanism only for a genuine exit (insolvency, a decision to withdraw), defined separately.
- Decide with B:
  1. the intake rule of §5.3 (OneToOneAll);
  2. whether real teams may recruit on merit (B-open);
  3. a cap on how many NPCs a real team may start, if authenticity should be protected for a few seasons (not measured).

## 4. Rating distribution (D1)

### 4.1 Where the ceiling comes from (FACT)

| Mechanism | Where | Effect on the top |
|---|---|---|
| Production rating formula | `Ratings.InRole` (role-weighted mean of five 1–99 skills, round half to even) | None. A 99 is possible |
| Production generation | `content/fixture.json` `Generation.AbilityBands` 52–92; `Generator.cs:70` adds shifts ≤ 0 | Generated production players top out in the low 90s. Potential = rating + headroom ≤ 14, clamped to 99 |
| Production development | `Careers.Develop` (`Lifecycle.cs:28–34`): growth stops at a static potential | None beyond the potential |
| Research scale: the clamp | `rating_model.py:434` `SCALE_MAX` 93; `rate_players.py:777` potential ≤ 93 | Removed in the uncapped experiment: 0 Overall changes, 3 potentials 93 → 94 |
| **Research scale: the top segment** | `rating_model.py:433` knots R 80 → 89.5 and R 92 → 93: a slope of 0.29 against 0.875 just below | **The binding cause.** 12 raw-quality points become 3.5 Overall points |
| Research elite gate | `rating_model.py:436`, `rate_players.py:611`: no elite evidence → at most 89 | Intended (the band rule) |
| Research potential after 25 | `GROWTH_END_AGE` 25: potential = Overall | Real players aged 25+ never grow (decision D2) |
| World: real players | `RealPack.cs:111` ceiling = research potential | A real player never exceeds their potential estimate (at most 94) |
| **World: NPC ceilings** | `Generation.cs:201` `88 + 11·x/(x + 11)`: an asymptote at 99 that is never reached | **A practical clamp:** at creation the potential stops at 96–97, and 99 is unattainable |
| World: development | `Development.cs:133` (Overall ≤ potential); growth × G/(G + 4); skill room | Intended: no guarantee, slow near the ceiling |
| NPC start cap | `InitialOverallCap` 82 | Intended; kept |

**INFERENCE.**
- There is no single constant.
- The research model's top segment compresses the elite **by design** (the user's band decision of 2026-10-14: "the
  best players form a group").
- The NPC asymptote is the only mechanism that contradicts the stated requirement that potential can reach 99.

### 4.2 D1a: removing the unintended ceiling

**Two changes, in experiments only:**
1. **The research clamp at 93 is removed.** This is already the uncapped experiment, and it is the scale of the world
   pack.
2. **The NPC ceiling tail.** Above a ceiling of 96, the asymptote is replaced by a straight line that reaches 99 at a raw
   room of 45 (`decisions.v1.json` `D1aTail` [96, 45]). Every ceiling below 96 is unchanged.

The sensitivity variant (a wider soft room, 11 → 14) shows what a careless fix does.

*Ratings in the world (mean per run).*

| Measure | B baseline (48) | D1a: tail (32) | D1a tail + D1b K96 (36) | Sensitivity: room 14 (48) | Room 14 + D1b K96 (48) |
|---|---:|---:|---:|---:|---:|
| NPC potential 95+ / 97+ / 99 at creation | 7.19 ± 0.40 / 0.29 ± 0.08 / 0.00 ± 0.00 | 7.50 ± 0.52 / 0.66 ± 0.15 / 0.06 ± 0.04 | 7.28 ± 0.50 / 0.72 ± 0.15 / 0.06 ± 0.04 | 12.27 ± 0.44 / 3.08 ± 0.28 / 0.10 ± 0.04 | 12.27 ± 0.44 / 3.08 ± 0.28 / 0.10 ± 0.04 |
| Every NPC created by S20 (cohort + entrants): potential 97+ / 99 | 1.58 ± 0.16 / 0.00 ± 0.00 | 4.25 ± 0.34 / 0.47 ± 0.10 | 4.14 ± 0.32 / 0.47 ± 0.09 | 22.17 ± 0.67 / 0.56 ± 0.08 | 22.12 ± 0.67 / 0.56 ± 0.08 |
| Max potential, S0 / S20 | 96.2 ± 0.1 / 96.2 ± 0.1 | 96.7 ± 0.2 / 96.9 ± 0.2 | 97.3 ± 0.1 / 97.1 ± 0.2 | 97.7 ± 0.1 / 97.6 ± 0.1 | 97.7 ± 0.1 / 97.6 ± 0.1 |
| Players 90+, S0 / S5 / S20 | 32.0 ± 0.0 / 28.9 ± 0.6 / 9.8 ± 0.5 | 32.0 ± 0.0 / 29.4 ± 0.8 / 9.4 ± 0.5 | 32.0 ± 0.0 / 31.5 ± 0.7 / 9.7 ± 0.5 | 32.0 ± 0.0 / 29.2 ± 0.6 / 10.5 ± 0.5 | 32.0 ± 0.0 / 31.5 ± 0.6 / 10.4 ± 0.5 |
| Players 94+, S0 / S5 / S10 / S20 | 0.00 ± 0.00 / 1.98 ± 0.14 / 1.69 ± 0.20 / 0.94 ± 0.14 | 0.00 ± 0.00 / 1.94 ± 0.18 / 1.91 ± 0.26 / 0.97 ± 0.17 | 5.00 ± 0.00 / 6.11 ± 0.20 / 2.31 ± 0.25 / 1.14 ± 0.14 | 0.00 ± 0.00 / 2.33 ± 0.16 / 2.31 ± 0.24 / 1.79 ± 0.18 | 5.00 ± 0.00 / 6.58 ± 0.21 / 2.83 ± 0.22 / 1.56 ± 0.18 |
| Players 97+, S5 / S20 | 0.00 ± 0.00 / 0.00 ± 0.00 | 0.06 ± 0.04 / 0.00 ± 0.00 | 0.58 ± 0.10 / 0.03 ± 0.03 | 0.02 ± 0.02 / 0.10 ± 0.04 | 0.54 ± 0.08 / 0.12 ± 0.05 |
| Real players 94+, S0 / S5 / S10 | 0.00 ± 0.00 / 1.38 ± 0.08 / 0.48 ± 0.07 | 0.00 ± 0.00 / 1.28 ± 0.10 / 0.47 ± 0.09 | 5.00 ± 0.00 / 5.47 ± 0.19 / 0.92 ± 0.12 | 0.00 ± 0.00 / 1.38 ± 0.08 / 0.46 ± 0.07 | 5.00 ± 0.00 / 5.65 ± 0.18 / 1.02 ± 0.10 |
| Ever 94+ by S20: anyone / NPCs | 5.33 ± 0.28 / 3.65 ± 0.27 | 5.56 ± 0.37 / 3.91 ± 0.33 | 14.50 ± 0.31 / 3.92 ± 0.29 | 7.00 ± 0.37 / 5.31 ± 0.35 | 15.81 ± 0.36 / 5.04 ± 0.34 |
| Ever 97+ by S20 | 0.00 ± 0.00 | 0.12 ± 0.06 | 0.81 ± 0.10 | 0.38 ± 0.08 | 1.02 ± 0.11 |
| Max Overall, S5 / S20 | 94.2 ± 0.1 / 93.9 ± 0.2 | 94.3 ± 0.1 / 93.8 ± 0.2 | 96.4 ± 0.1 / 94.2 ± 0.2 | 94.4 ± 0.1 / 94.8 ± 0.2 | 96.4 ± 0.1 / 94.6 ± 0.2 |
| NPCs that ever passed 82, by S20 | 156 ± 2 | 157 ± 2 | 155 ± 2 | 158 ± 2 | 157 ± 2 |
| NPCs created above 82 (cohort + every entrant) | 0 ± 0 | 0 ± 0 | 0 ± 0 | 0 ± 0 | 0 ± 0 |
| Young prospects (<= 21, potential 85+) that reached potential - 1 by S20 (%) | 14.1 ± 0.5 | 14.6 ± 0.6 | 14.0 ± 0.6 | 13.9 ± 0.5 | 13.9 ± 0.5 |
| Overall p99 of active players, S0 / S20 | 87 ± 0 / 84 ± 0 | 87 ± 0 / 84 ± 0 | 87 ± 0 / 84 ± 0 | 87 ± 0 / 84 ± 0 | 87 ± 0 / 84 ± 0 |
| Top 50 by role, all players, S0 / S20 | 83.41 ± 0.01 / 78.39 ± 0.07 | 83.40 ± 0.01 / 78.34 ± 0.08 | 83.54 ± 0.01 / 78.36 ± 0.07 | 83.41 ± 0.01 / 78.45 ± 0.07 | 83.54 ± 0.01 / 78.42 ± 0.08 |
| Strongest real tier-a team, S0 / S5 | 91.2 ± 0.0 / 89.2 ± 0.2 | 91.2 ± 0.0 / 89.3 ± 0.3 | 93.0 ± 0.0 / 90.5 ± 0.3 | 91.2 ± 0.0 / 89.3 ± 0.2 | 93.0 ± 0.0 / 90.5 ± 0.3 |
| Mean contract wage of 90+ players, S1 (per day) | 2627 ± 3 | 2624 ± 4 | 2687 ± 3 | 2626 ± 3 | 2688 ± 3 |
| Mean fee of a transfer of an 88+ player (20 seasons) | 78226 ± 461 | 78200 ± 501 | 79310 ± 533 | 79122 ± 466 | 78923 ± 448 |
| Series won by the stronger real team (%) | 66.51 ± 0.04 | 66.55 ± 0.05 | 66.61 ± 0.06 | 66.48 ± 0.05 | 66.50 ± 0.04 |

*Paired differences on shared seeds (mean ± SE).*

| Measure (season) | D1a tail − B | D1b K96 (on the tail) | Room 14 − B | D1b K96 (on room 14) |
|---|---:|---:|---:|---:|
| NPCs with potential 95+ (S0) | 0.00 ± 0.00 | 0.00 ± 0.00 | 5.08 ± 0.31 | 0.00 ± 0.00 |
| NPCs with potential 97+ (S0) | 0.34 ± 0.11 | 0.00 ± 0.00 | 2.79 ± 0.26 | 0.00 ± 0.00 |
| Players 90+ (S5) | -0.03 ± 0.03 | 2.41 ± 0.24 | 0.29 ± 0.14 | 2.35 ± 0.17 |
| Players 94+ (S5) | 0.00 ± 0.04 | 4.12 ± 0.21 | 0.35 ± 0.09 | 4.25 ± 0.17 |
| Players 94+ (S20) | 0.12 ± 0.07 | 0.19 ± 0.12 | 0.85 ± 0.14 | -0.23 ± 0.13 |
| Players ever 94+ (S20) | 0.16 ± 0.09 | 8.97 ± 0.28 | 1.67 ± 0.25 | 8.81 ± 0.25 |
| NPCs ever 94+ (S20) | 0.16 ± 0.09 | 0.09 ± 0.16 | 1.67 ± 0.25 | -0.27 ± 0.16 |
| Real players 94+ (S5) | 0.00 ± 0.00 | 4.12 ± 0.21 | 0.00 ± 0.00 | 4.27 ± 0.17 |
| Young prospects reaching potential (%) (S20) | 0.16 ± 0.08 | -0.12 ± 0.08 | -0.19 ± 0.20 | -0.04 ± 0.07 |
| Mean wage of 90+ players (S1) | 0.00 ± 0.00 | 61.98 ± 0.75 | -1.39 ± 1.00 | 62.15 ± 0.58 |

Variants (run labels in §7.2): **B baseline** (the B policy, one-to-one intake); **D1a: tail** (`D1t`, the candidate);
**D1a tail + D1b K96** (`D1bt`); **Sensitivity: room 14** (`D1a`); **Room 14 + D1b K96** (`D1b`).

**Reading (FACT, then INFERENCE).**
- **D1t changes what it should, and nothing else.**
  - The number of NPCs with potential 95+ is identical on every seed (paired difference 0.00 ± 0.00). On half the seeds
    the whole initial world is identical to B's, because no NPC there had a ceiling above 96.
  - Potential 97+ at creation rises by a third of a player per world (+0.34 ± 0.11).
  - 99 now occurs: 0.47 per run among the about 12,200 NPCs created in 20 seasons, roughly one in 26,000.
  - In the world, exceptional players stay as rare as before: +0.12 ± 0.07 players at 94+ at season 20, not
    significant.
- **The wider room is not a "removal".** It raises every compressed ceiling:
  - potential 95+ at creation from 7.2 to 12.3 (+71%);
  - 97+ about ten times as often;
  - +1.7 ± 0.3 players reaching 94+ in 20 seasons.

  That is a recalibration, not the removal of a cap. **Not recommended.**
- **The top of the world thins over time, in every variant.** Players rated 90+ fall from 32 at creation to about 10 at
  season 20, and the top 50 by role from 83.4 to 78.4: the real elite retires, and NPCs rarely develop past 90.
  - Neither D1a nor D1b changes that.
  - It is a development and potential calibration question (D2, D3 of PLAYER_POTENTIAL_TRAINING_EXPERIMENT §10), not a
    ceiling.
- **The rating requirements hold in every variant:**
  - skills, Overall and potential within 1–99;
  - potential never below the Overall (0 cases in every checkpoint);
  - no NPC created above 82, among the cohort and every entrant;
  - NPCs pass 82 only by developing (about 156 per world by season 20);
  - no player is guaranteed their potential: about 14% of the young high-potential prospects (21 or younger, potential
    85+) reach it.

### 4.3 D1b: does the evidence support a stretched elite? (INFERENCE, in-sample)

**The research tail** (4,567 rated players with games; aggregates only):

| Measure | Value |
|---|---|
| R of the top 10 | 93.7, 91.9, 88.7, 88.1, 87.7, 86.3, 86.1, 86.0, 85.6, 85.6 |
| Players with R ≥ 80 / 84 / 88 / 90 / 92 | 35 / 12 / 4 / 2 / 1 |
| R gap: rank 1 → 10; rank 10 → 50; rank 50 → 200 | 8.2; 7.2; 9.1 |
| Players with elite evidence | 100 |

**The candidate transforms** (post hoc on R, elite gate kept; Overall counts of those players):

| Transform | Slope of the segment above R 80 | 90–93 | 94–96 | 97–99 | Max |
|---|---:|---:|---:|---:|---:|
| V2U (current, uncapped) | 0.29 | 32 | 1 | 0 | 94 |
| K96 (92 → 96, then 99 at R 104) | 0.54 | 28 | 5 | 0 | 96 |
| K98 (92 → 98) | 0.71 | 25 | 6 | 2 | 98 |
| L1 (slope 1 above R 80) | 1.00 | 21 | 7 | 5 | 99 |

**Competitive calibration.**
- **Data:** 2,816 of the 3,036 primary 2026 games whose ten seats are all rated players. The other 220 games have an
  unrated seat.
- **Model:** a logistic fit of the blue side's result on the lineup-average difference ÷ 7 (the game's points per
  logit), with an intercept for the side.
- **Reading:** a slope of 1 means the scale's differences predict results as the game reads them.

| Games | V2U | K96 | K98 | L1 | Linear Level |
|---|---:|---:|---:|---:|---:|
| All (2,816) | 1.18 ± 0.06 | 1.13 ± 0.06 | 1.10 ± 0.06 | 1.05 ± 0.05 | 1.18 ± 0.06 |
| Both lineups average 85+ (832) | **1.28 ± 0.13** | **1.11 ± 0.11** | 1.02 ± 0.10 | 0.91 ± 0.09 | 1.01 ± 0.10 |
| 78–85 (1,177) | 1.15 ± 0.09 | 1.13 ± 0.09 | 1.12 ± 0.09 | 1.11 ± 0.09 | 1.21 ± 0.10 |
| Below 78 (807) | 1.15 ± 0.10 | 1.15 ± 0.10 | 1.15 ± 0.10 | 1.15 ± 0.10 | 1.34 ± 0.11 |

**Reading.**
- **Every scale has slopes above 1.** Read with 7 points per logit, single-game differences are about 15% larger than
  the scale shows. That is the game's LogitScale, not the top end.
- **The current scale is flatter in elite games** (1.28 against 1.15 elsewhere), consistent with the compressed segment.
  - K96 brings the elite games in line with the rest (1.11 against 1.13–1.15).
  - K98 and L1 overshoot: they stretch the elite beyond the others.
- **Not established.** The difference, 0.13, is within one combined standard error (about 0.16). The log loss moves by at
  most 0.002, and the ratings were fitted on these games.

**D1b in the world (K96).** 58 pack players change:
- 22 by +1 to +3 Overall;
- 36 by potential only.

Five real players start at 94–96. The world consequences, D1bt against D1t, paired:
- **More players at 94+:** 4.1 more at season 5, and 9.0 more who ever reach 94+ in 20 seasons. They are the real elite;
  NPCs are unaffected.
- **A stronger top team:** the strongest real tier-a team goes from 91.2 to 93.0.
- **Wages:** +62 a day (+2.4%) on the mean wage of 90+ players.
- **Fees:** transfers of 88+ players +1.4%, within noise.
- **Competition:** the share of series won by the stronger real team is unchanged (66.6%).

Real elite players still decline with age, so the effect fades by season 10 and is gone by season 20.

**Recommendation (PROPOSAL).**
- **D1a now.** It changes nobody below the new tail.
- **D1b only as a band decision, after an out-of-sample check:** ratings frozen before a test window, as the rating
  validation pipeline already does (`validation.json`, inner and outer cutoffs).
- K96 is the candidate the in-sample evidence favours. No quota of 95+ players is set by any option.

## 5. Population and free-agent market (E2/F1)

### 5.1 The model (FACT)

**Population.**
- **At creation:** exactly 4,000 NPCs, with 320 free agents (8%; the builder refuses any target that is not below 10%).
- **States:** an NPC is employed, a free agent or retired. A retired person keeps their history and leaves the active
  population.
- **Entrants:** each has a new identity `player:npc:w1:c{season}:{n}`, never a reused ID. They enter the open market
  unattached, and the market decides where they go. **No employment is created to hold a ratio.**

**Intake modes:**

| Mode | Rule |
|---|---|
| **OneToOne** (E2 candidate) | Entrants = the season's NPC retirements, exactly |
| Replacement (v1) | The same, bounded to 300–600 |
| Fixed | 480 a season |
| Pathways | One-to-one. Entrants are drawn 50% unattached prospects (16–18), 35% amateur-scene players (18–23, tier d/unknown ages) and 15% semi-pro-scene players (19–24, tier c). The weights are AUTHORED |
| **OneToOneAll** (alternative) | Entrants = NPC retirements + real retirements. Still no real person is created |

**No hard free-agent cap after creation.** Nothing deletes, retires or assigns a player to hold a ratio. The share is
reported and warned above 12%, never enforced.

**Conservation.** Active NPCs at the end = active at the start − NPC retirements + entrants. It is checked every season
of every run.

### 5.2 Stability over 20 seasons (FACT)

*Population and market (S1 / S5 / S10 / S20).*

| Measure | A (48) | B (48) | B-all (16) | B-bounded (3) | B-fixed (8) | B-path (8) | C (48) |
|---|---:|---:|---:|---:|---:|---:|---:|
| Active NPCs | 4000 ± 0 / 4000 ± 0 / 4000 ± 0 / 4000 ± 0 | 4000 ± 0 / 4000 ± 0 / 4000 ± 0 / 4000 ± 0 | 4078 ± 2 / 4497 ± 4 / 4982 ± 2 / 5135 ± 0 | 4000 ± 0 / 4000 ± 0 / 4000 ± 0 / 4000 ± 0 | 3986 ± 9 / 4030 ± 10 / 4498 ± 9 / 4834 ± 11 | 4000 ± 0 / 4000 ± 0 / 4000 ± 0 / 4000 ± 0 | 4000 ± 0 / 4000 ± 0 / 4000 ± 0 / 4000 ± 0 |
| Entrants = NPC retirements, every season | True | True | False | True | False | True | True |
| NPC retirements per season | 499 ± 3 / 449 ± 3 / 380 ± 3 / 414 ± 2 | 499 ± 3 / 436 ± 3 / 356 ± 2 / 391 ± 3 | 496 ± 6 / 451 ± 3 / 420 ± 4 / 515 ± 5 | 497 ± 12 / 439 ± 16 / 351 ± 9 / 407 ± 2 | 494 ± 9 / 432 ± 6 / 372 ± 4 / 481 ± 8 | 494 ± 9 / 502 ± 6 / 474 ± 8 / 474 ± 6 | 499 ± 3 / 452 ± 3 / 386 ± 3 / 416 ± 3 |
| Entrants per season | 499 ± 3 / 449 ± 3 / 380 ± 3 / 414 ± 2 | 499 ± 3 / 436 ± 3 / 356 ± 2 / 391 ± 3 | 574 ± 7 / 560 ± 4 / 491 ± 4 / 516 ± 5 | 497 ± 12 / 439 ± 16 / 351 ± 9 / 407 ± 2 | 480 ± 0 / 480 ± 0 / 480 ± 0 / 480 ± 0 | 494 ± 9 / 502 ± 6 / 474 ± 8 / 474 ± 6 | 499 ± 3 / 452 ± 3 / 386 ± 3 / 416 ± 3 |
| NPC free agents after the window (%) | 6.8 ± 0.0 / 6.9 ± 0.0 / 6.9 ± 0.0 / 6.7 ± 0.0 | 5.3 ± 0.0 / 5.4 ± 0.0 / 4.6 ± 0.0 / 4.9 ± 0.0 | 6.9 ± 0.1 / 8.6 ± 0.1 / 9.3 ± 0.1 / 9.1 ± 0.1 | 5.4 ± 0.1 / 5.3 ± 0.1 / 4.9 ± 0.1 / 4.8 ± 0.1 | 5.2 ± 0.1 / 5.4 ± 0.1 / 4.3 ± 0.1 / 4.5 ± 0.1 | 5.3 ± 0.1 / 6.4 ± 0.1 / 5.1 ± 0.0 / 5.4 ± 0.1 | 7.5 ± 0.0 / 8.4 ± 0.0 / 9.5 ± 0.1 / 9.7 ± 0.1 |
|   range over every season and seed (%) | 6.1–9.38 | 3.7–8.55 | 6.47–10.14 | 4.03–7.62 | 3.73–7.72 | 4.62–8.62 | 6.92–10.78 |
|   seasons above the 12% warning (per run) | 0.00 ± 0.00 | 0.00 ± 0.00 | 0.00 ± 0.00 | 0.00 ± 0.00 | 0.00 ± 0.00 | 0.00 ± 0.00 | 0.00 ± 0.00 |
| NPC free agents as the window opens (%) | 28.5 ± 0.1 / 24.2 ± 0.1 / 22.4 ± 0.1 / 22.7 ± 0.1 | 28.5 ± 0.1 / 22.2 ± 0.1 / 18.6 ± 0.1 / 19.1 ± 0.1 | 29.8 ± 0.2 / 25.5 ± 0.1 / 23.3 ± 0.2 / 22.9 ± 0.1 | 28.6 ± 0.3 / 22.6 ± 0.5 / 18.2 ± 0.2 / 19.1 ± 0.0 | 28.0 ± 0.1 / 23.0 ± 0.2 / 19.7 ± 0.2 / 18.8 ± 0.2 | 28.3 ± 0.2 / 25.3 ± 0.3 / 21.9 ± 0.3 / 21.7 ± 0.4 | 28.5 ± 0.1 / 25.1 ± 0.1 / 24.8 ± 0.1 / 25.3 ± 0.1 |
| Pre-window free agents rated 70+ | 50 ± 1 / 28 ± 1 / 16 ± 1 / 15 ± 1 | 50 ± 1 / 27 ± 1 / 9 ± 0 / 3 ± 0 | 50 ± 1 / 27 ± 1 / 11 ± 1 / 6 ± 0 | 46 ± 2 / 21 ± 1 / 10 ± 2 / 2 ± 1 | 49 ± 2 / 25 ± 2 / 9 ± 1 / 5 ± 0 | 71 ± 3 / 59 ± 1 / 37 ± 2 / 25 ± 2 | 50 ± 1 / 28 ± 1 / 25 ± 1 / 14 ± 0 |
| Pre-window free agents with potential 85+ | 45 ± 1 / 34 ± 1 / 30 ± 1 / 32 ± 1 | 45 ± 1 / 34 ± 1 / 27 ± 1 / 27 ± 1 | 51 ± 2 / 40 ± 1 / 38 ± 1 / 38 ± 2 | 47 ± 2 / 31 ± 3 / 28 ± 2 / 30 ± 5 | 44 ± 2 / 34 ± 1 / 36 ± 2 / 35 ± 2 | 36 ± 2 / 22 ± 2 / 25 ± 2 / 23 ± 2 | 45 ± 1 / 35 ± 1 / 35 ± 1 / 31 ± 1 |
| Smallest role pool after the window | 47 ± 1 / 48 ± 1 / 47 ± 1 / 46 ± 1 | 35 ± 1 / 37 ± 0 / 31 ± 0 / 33 ± 0 | 48 ± 1 / 65 ± 1 / 78 ± 2 / 80 ± 2 | 34 ± 2 / 36 ± 3 / 35 ± 2 / 32 ± 2 | 33 ± 1 / 36 ± 1 / 33 ± 1 / 37 ± 1 | 35 ± 1 / 44 ± 1 / 35 ± 1 / 38 ± 1 | 52 ± 0 / 59 ± 1 / 66 ± 1 / 67 ± 1 |
| Free agents unemployed 2+ seasons | 1 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 0 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 1 ± 0 / 24 ± 2 / 83 ± 2 / 83 ± 3 | 0 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 0 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 0 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 1 ± 0 / 2 ± 0 / 16 ± 1 / 30 ± 1 |
| Mean asking wage of free agents (per day) | 418 ± 2 / 467 ± 1 / 452 ± 1 / 447 ± 1 | 448 ± 3 / 434 ± 1 / 378 ± 1 / 372 ± 1 | 391 ± 4 / 357 ± 3 / 305 ± 2 / 308 ± 2 | 442 ± 12 / 437 ± 3 / 384 ± 4 / 373 ± 4 | 451 ± 7 / 441 ± 2 / 400 ± 3 / 406 ± 4 | 467 ± 8 / 438 ± 3 / 367 ± 1 / 367 ± 3 | 415 ± 2 / 461 ± 2 / 413 ± 2 / 384 ± 2 |
| Vacant squad seats, fictional layers | 1 ± 0 / 17 ± 2 / 16 ± 2 / 12 ± 2 | 4 ± 1 / 308 ± 3 / 669 ± 3 / 831 ± 3 | 1 ± 0 / 1 ± 0 / 0 ± 0 / 0 ± 0 | 4 ± 3 / 298 ± 5 / 682 ± 5 / 827 ± 7 | 12 ± 6 / 276 ± 12 / 214 ± 10 / 38 ± 12 | 11 ± 4 / 341 ± 7 / 690 ± 5 / 859 ± 8 | 1 ± 0 / 1 ± 0 / 0 ± 0 / 0 ± 0 |
| Fictional teams without five starters | 0 ± 0 / 10 ± 1 / 10 ± 1 / 9 ± 1 | 2 ± 0 / 85 ± 1 / 153 ± 1 / 148 ± 1 | 0 ± 0 / 0 ± 0 / 0 ± 0 / 0 ± 0 | 2 ± 2 / 79 ± 5 / 155 ± 4 / 149 ± 2 | 4 ± 2 / 68 ± 2 / 46 ± 3 / 18 ± 4 | 5 ± 2 / 84 ± 3 / 148 ± 3 / 153 ± 1 | 0 ± 0 / 1 ± 0 / 0 ± 0 / 0 ± 0 |
|   of which fictional professional | 0.02 ± 0.02 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.02 ± 0.02 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 | 0.02 ± 0.02 / 0.00 ± 0.00 / 0.00 ± 0.00 / 0.00 ± 0.00 |
| Starter vacancies filled (% cumulative) | 86.8 ± 0.1 / 67.9 ± 0.1 / 51.5 ± 0.1 / 38.8 ± 0.0 | 99.5 ± 0.1 / 85.0 ± 0.1 / 73.6 ± 0.1 / 60.3 ± 0.1 | 99.9 ± 0.0 / 99.9 ± 0.0 / 100.0 ± 0.0 / 100.0 ± 0.0 | 99.6 ± 0.4 / 84.9 ± 0.3 / 73.7 ± 0.5 / 60.5 ± 0.2 | 99.0 ± 0.5 / 84.3 ± 0.7 / 81.6 ± 0.7 / 86.5 ± 0.7 | 98.9 ± 0.3 / 81.6 ± 0.2 / 71.0 ± 0.1 / 58.6 ± 0.2 | 86.8 ± 0.1 / 92.0 ± 0.1 / 93.0 ± 0.0 / 95.9 ± 0.0 |
| NPC mean age | 22.64 ± 0.01 / 21.80 ± 0.01 / 21.89 ± 0.01 / 21.87 ± 0.01 | 22.64 ± 0.01 / 21.90 ± 0.01 / 22.11 ± 0.01 / 22.23 ± 0.01 | 22.53 ± 0.01 / 21.51 ± 0.01 / 21.74 ± 0.01 / 22.10 ± 0.01 | 22.63 ± 0.04 / 21.90 ± 0.04 / 22.11 ± 0.03 / 22.18 ± 0.04 | 22.66 ± 0.01 / 21.83 ± 0.01 / 21.71 ± 0.01 / 22.18 ± 0.01 | 22.92 ± 0.02 / 22.98 ± 0.01 / 23.16 ± 0.02 / 23.28 ± 0.03 | 22.64 ± 0.01 / 21.79 ± 0.01 / 21.84 ± 0.01 / 21.82 ± 0.01 |
| NPC aged 16-21 (%) | 36.7 ± 0.1 / 57.6 ± 0.1 / 49.2 ± 0.1 / 51.4 ± 0.1 | 36.7 ± 0.1 / 56.6 ± 0.1 / 47.0 ± 0.1 / 48.9 ± 0.1 | 37.7 ± 0.2 / 61.2 ± 0.1 / 51.0 ± 0.1 / 50.0 ± 0.2 | 36.5 ± 0.6 / 56.1 ± 0.2 / 47.1 ± 0.4 / 49.1 ± 0.5 | 36.3 ± 0.2 / 56.9 ± 0.1 / 52.3 ± 0.1 / 48.7 ± 0.2 | 33.0 ± 0.2 / 36.7 ± 0.2 / 34.5 ± 0.2 / 34.4 ± 0.4 | 36.7 ± 0.1 / 57.8 ± 0.1 / 49.7 ± 0.1 / 51.8 ± 0.1 |
| NPC aged 28+ (%) | 9.4 ± 0.1 / 12.7 ± 0.1 / 6.6 ± 0.0 / 8.9 ± 0.1 | 9.4 ± 0.1 / 13.2 ± 0.1 / 7.9 ± 0.0 / 12.0 ± 0.1 | 9.2 ± 0.1 / 11.6 ± 0.1 / 6.0 ± 0.1 / 11.2 ± 0.1 | 9.2 ± 0.3 / 13.1 ± 0.4 / 7.9 ± 0.2 / 11.9 ± 0.3 | 9.3 ± 0.2 / 12.9 ± 0.1 / 6.9 ± 0.1 / 11.3 ± 0.2 | 9.3 ± 0.2 / 13.0 ± 0.1 / 13.2 ± 0.2 / 15.2 ± 0.2 | 9.4 ± 0.1 / 12.6 ± 0.1 / 6.4 ± 0.0 / 8.6 ± 0.1 |
| NPC mean Overall | 52.97 ± 0.02 / 52.32 ± 0.02 / 52.41 ± 0.03 / 51.99 ± 0.02 | 52.98 ± 0.02 / 52.58 ± 0.02 / 53.08 ± 0.03 / 52.90 ± 0.03 | 52.81 ± 0.04 / 52.00 ± 0.03 / 52.45 ± 0.05 / 52.53 ± 0.03 | 52.99 ± 0.06 / 52.67 ± 0.06 / 53.18 ± 0.01 / 52.83 ± 0.07 | 53.03 ± 0.07 / 52.55 ± 0.06 / 52.59 ± 0.06 / 52.74 ± 0.04 | 53.52 ± 0.07 / 53.90 ± 0.07 / 53.99 ± 0.05 / 53.77 ± 0.05 | 52.97 ± 0.02 / 52.31 ± 0.02 / 52.35 ± 0.03 / 51.96 ± 0.03 |
| NPC top 50 by role | 77.67 ± 0.03 / 78.92 ± 0.05 / 78.26 ± 0.07 / 76.93 ± 0.08 | 77.67 ± 0.03 / 78.97 ± 0.05 / 78.56 ± 0.07 / 78.39 ± 0.07 | 77.66 ± 0.06 / 79.11 ± 0.08 / 79.51 ± 0.10 / 79.89 ± 0.10 | 77.65 ± 0.17 / 79.01 ± 0.13 / 78.51 ± 0.07 / 78.08 ± 0.25 | 77.62 ± 0.11 / 78.97 ± 0.14 / 78.86 ± 0.11 / 79.46 ± 0.16 | 77.73 ± 0.11 / 79.66 ± 0.15 / 79.57 ± 0.11 / 78.94 ± 0.18 | 77.67 ± 0.03 / 78.92 ± 0.05 / 78.23 ± 0.07 / 77.46 ± 0.07 |
| Fictional professional starters, mean Overall | 76.65 ± 0.03 / 77.11 ± 0.05 / 75.48 ± 0.07 / 74.07 ± 0.07 | 76.65 ± 0.03 / 77.16 ± 0.05 / 71.75 ± 0.08 / 67.56 ± 0.06 | 76.63 ± 0.07 / 77.31 ± 0.08 / 73.00 ± 0.10 / 69.92 ± 0.10 | 76.59 ± 0.20 / 77.35 ± 0.26 / 71.63 ± 0.10 / 67.46 ± 0.19 | 76.58 ± 0.10 / 77.23 ± 0.14 / 72.27 ± 0.11 / 69.36 ± 0.18 | 76.66 ± 0.10 / 77.75 ± 0.14 / 73.42 ± 0.10 / 68.95 ± 0.11 | 76.65 ± 0.03 / 77.11 ± 0.05 / 75.84 ± 0.07 / 75.36 ± 0.07 |
| Semi-professional starters, mean Overall | 66.21 ± 0.06 / 63.37 ± 0.06 / 62.40 ± 0.05 / 61.67 ± 0.04 | 66.22 ± 0.06 / 63.39 ± 0.05 / 58.34 ± 0.04 / 56.75 ± 0.04 | 66.24 ± 0.09 / 64.02 ± 0.11 / 60.19 ± 0.07 / 59.36 ± 0.07 | 66.28 ± 0.15 / 63.76 ± 0.17 / 58.60 ± 0.10 / 56.65 ± 0.08 | 66.28 ± 0.13 / 63.63 ± 0.18 / 59.07 ± 0.09 / 58.70 ± 0.09 | 66.67 ± 0.15 / 64.84 ± 0.17 / 58.86 ± 0.07 / 57.07 ± 0.10 | 66.21 ± 0.06 / 63.35 ± 0.06 / 62.85 ± 0.06 / 62.70 ± 0.05 |

### 5.3 Is one-to-one structurally sound? (INFERENCE)

| Concern | Finding |
|---|---|
| **Population conservation** | **PASS.** Exactly 4,000 active NPCs at every checkpoint of every one-to-one run, and 0 conservation violations. Entrants equal NPC retirements in every season. Real careers and NPC replacement are not conflated: a real retirement never creates an NPC under OneToOne |
| **Free-agent accumulation** | **None.** No cap is needed: the share after the window stays inside 3.7–10.8% across every run and season. The open market as the window opens holds about 20–28%: the supply recruiters see |
| **Professional-ready replacements** | **Too few.** Every entrant is 16–18 and weak, so under A the fictional pyramid loses about 2.6 points at the professional level (starters 76.7 → 74.1 from season 1) and 4.5 at semi-pro (66.2 → 61.7). **Pathways** (mixed entry ages) raise the pre-window supply of 70+ players at season 20 from 3 to 25 (under B) and keep the 16–21 share at 34%. They do not restore fictional professional strength under B (69.0 against 67.6), because that loss comes from real teams buying NPCs |
| **Concentration of inexperienced players** | **Yes, with young-only entrants.** The 16–21 share rises from 33% to about 50%, and the mean age falls from 22.8 to about 22. No role is short: the smallest role pool is 31+ unsigned players in every variant |
| **Long-term inflation or decline** | **No inflation.** Under A the NPC mean Overall drifts from 53.7 to 52.0. The NPC top 50 by role peaks at season 5 (78.9) and ends at 76.9 (A) or 78.4 (B) |
| **One-to-one with real teams recruiting NPCs (B)** | **Fails.** The 1,135 real retirees are not replaced, and real teams hire about 4,550 NPCs over 20 seasons. At season 20 the lower layers carry about 830 vacant seats and 148 teams without five (none professional); fictional professional starters fall to 67.6 (A 74.1). **OneToOneAll** (an entrant for every retirement, real included) leaves 0 vacant seats and 0 teams without five. The NPC population then grows to 5,135, the size of the original NPC + real population, and fictional professionals recover partly (69.9) |
| **Fixed intake (480)** | **Drifts.** The NPC population grows to about 4,830 by season 20 (+21%), because retirements fall below the intake as the opening cohort ages |
| **Bounded replacement (v1)** | **Identical to one-to-one in these runs.** NPC retirements stayed between 312 and 550, inside the 300–600 bounds, and the final digests of B-bounded equal B's on all 3 seeds |

**Recommendation (PROPOSAL).**
- Approve **one-to-one replacement as the first controlled rule**, and **no hard free-agent cap after creation**. Keep
  the creation invariant (fewer than 400 free agents).
- If D-H1 B is approved, **count every retirement** (OneToOneAll), so that real retirees' seats are matched. That keeps
  the total active population (NPC + real) at its starting 5,135. It is still demographic, not a market correction.
- Treat the **entrant age mix** (pathways) as a separate calibration decision.

## 6. Save architecture (D-H3)

### 6.1 The current SnapshotStore (FACT, verified in the working tree)

| Property | Where |
|---|---|
| Versioned envelope: format, schema 6, rules, rng, canonical version, content hash, gameplay hash, checksum | `SnapshotStore.cs:8, 16, 29–31` |
| A flushed temporary file, verified, then `File.Replace` with `.bak1`; `.bak1` copied to `.bak2` when valid; load falls back to `.bak1`, `.bak2` | `SnapshotStore.cs:33–52, 61` |
| Exact identity required; older saves rejected | `SnapshotStore.cs:74–76` |
| 8,000,000-byte limit | `SnapshotStore.cs:72`, `ContentLoader.cs:19` |
| JSON arrays ≤ 20,000 elements; strings ≤ 4,096 characters | `ContentLoader.cs:38, 41` |
| Transfer history bounded: `World.Moves` pruned each season | `Model.cs:111`, `Simulation.cs:413` |

All six properties hold today. The hybrid world cannot fit this contract even before any history accumulates: its
snapshot exceeds the 8 MB limit at creation (12.4 MB), and its event list exceeds the 20,000-element array limit in
season 1 (18,906 events at creation, 23,938 after one season).

### 6.2 The three stores (EXPERIMENT)

| Store | Layout | Commit protocol | Recovery |
|---|---|---|---|
| **S1** | One file: the full world with every career event (JSON; also gzip) | Production protocol: temporary file flushed, `File.Replace`, two backups | The newest file whose SHA-256 frame checks; temporary files deleted |
| **S2** | `state.s2`: the state without events, naming its journal checkpoint (sequence number and chain head). `journal.log`: one record per career event, a sequence number and a SHA-256 chain | **The journal first** (appended and flushed), **then** the snapshot (the S1 protocol) | **The snapshot is the authority.** Records after its checkpoint are an unfinished save and are truncated. A snapshot whose checkpoint the journal does not reach, or whose chain head differs, is rejected, and an older backup is tried. Otherwise the load refuses |
| **S3** | One SQLite database (the Windows system library `winsqlite3.dll`, WAL, synchronous FULL): the state blob with its checkpoint, an indexed event table | One transaction per save; WAL checkpoint after it | SQLite's own recovery; `quick_check`; the state blob's SHA-256; the event count equal to the checkpoint |

**One authority in every store.** The current state is the snapshot (S1/S2) or the state row (S3). History never
overrides it.

### 6.3 Benchmark (FACT; one 20-season world, seed 20261015, policy B, one-to-one intake; this machine)

| Season | People / contracts / events | S1 | S1-gzip | S2 (total) | S3 | S2 snapshot + journal (MB) |
|---|---:|---:|---:|---:|---:|---:|
| S0 | 5,135 / 4,815 / 18,906 | 12.4 MB | 1.9 MB | 11.4 MB | 12.6 MB | 8.1 + 3.2 |
| S1 | 5,631 / 8,403 / 23,938 | 15.2 MB | 2.7 MB | 13.9 MB | 23.6 MB | 9.8 + 4.1 |
| S5 | 7,501 / 23,548 / 44,522 | 24.7 MB | 4.4 MB | 22.3 MB | 38.8 MB | 14.8 + 7.5 |
| S10 | 9,434 / 41,378 / 67,926 | 35.3 MB | 6.1 MB | 31.7 MB | 55.6 MB | 20.4 + 11.4 |
| S20 | 13,310 / 72,709 / 109,268 | 54.9 MB | 9.3 MB | 49.2 MB | 86.5 MB | 30.9 + 18.3 |

| Season | S1 save / load (ms) | S1-gzip | S2 | S3 | State-only load S2 / S3 (ms) |
|---|---:|---:|---:|---:|---:|
| S0 | 254 / 319 | 148 / 441 | 187 / 573 | 330 / 319 | 649 / 207 |
| S1 | 328 / 446 | 163 / 843 | 159 / 969 | 254 / 466 | 734 / 237 |
| S5 | 313 / 536 | 320 / 507 | 267 / 882 | 420 / 594 | 887 / 274 |
| S10 | 618 / 715 | 432 / 775 | 366 / 1620 | 608 / 981 | 1309 / 447 |
| S20 | 776 / 936 | 595 / 1294 | 524 / 1354 | 989 / 1382 | 1204 / 612 |

| Season | Memory after a full load: S1 (MB) | S2 | S3 | State only, S2 / S3 (MB) | In memory: index build + 200 careers (ms) | In memory: one season's transfers / one team's history, scan (ms) | S2: one career from the journal file (ms) | S3 SQL: 200 careers / season transfers / team history (ms) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| S0 | 17.2 | 7.6 | 7.6 | 9.7 / 9.7 | 7 + 0.4 | 0.6 / 1.1 | 336 | 3.6 / 0.3 / 4.4 |
| S1 | 11.2 | 9.6 | 9.6 | 1.7 / 1.7 | 58 + 0.1 | 0.7 / 1.1 | 398 | 3.9 / 0.1 / 7.0 |
| S5 | 24.7 | 18.1 | 18.1 | 6.7 / 6.7 | 8 + 0.1 | 1.2 / 1.8 | 618 | 9.5 / 0.1 / 11.4 |
| S10 | 35.4 | 28.1 | 54 | 7.8 / 7.8 | 9 + 0.1 | 1.6 / 2.6 | 826 | 5.0 / 0.1 / 20.4 |
| S20 | 84.5 | 44.8 | 44.8 | 14 / 14 | 41 + 0.1 | 2.0 / 4.3 | 437 | 4.7 / 0.1 / 33.3 |

- **Every load reproduced the live state** (the snapshot hash of the loaded world equals the live world's) at every checkpoint, for every store: **PASS**. Event IDs unique and contiguous: **PASS**; history replay violations at seasons 10 and 20: [0].
- **Continuing after a load:** a world loaded from S2 and from S3 at season 10 and played to season 11 equals the live world: S2 PASS, S3-sqlite PASS.
- **Every-season saves** (21 per store): mean S1 585 ms, S1-gzip 581 ms, S2 458 ms, S3-sqlite 687 ms; the last save: S1 776 ms, S1-gzip 595 ms, S2 524 ms, S3-sqlite 989 ms.
- **Memory** is the managed heap after a forced collection. S1's figures (and S3's at season 10) include the serializer's pooled read buffers; the loaded world itself is about the S2 figure (45 MB at season 20).
- SQLite library: `winsqlite3.dll` 3.29.0. S1 save time includes serialization, the flushed temporary file, the `.bak1` → `.bak2` copy and the replace; it does not include production's re-read validation (SnapshotStore reads the temporary file and the result again).

### 6.4 Crash consistency: fault injection (FACT; a 10-season world, a transfer in the interrupted season)

**Each scenario:**
1. a clean save at season 10;
2. season 11 played, in which player P transfers from their former team to a new one;
3. a save interrupted at a chosen point, or a file corrupted after it;
4. a reload as a new process would do it (no in-memory writer state);
5. the checks: P's team, the history replay, unique and contiguous event IDs, and equality with the acknowledged save;
6. a retry: season 11 replayed from what was loaded, saved, and loaded again.

| ID | Store | Scenario | Result | Evidence |
|---|---|---|---|---|
| S1-T1 | S1 | crash while writing the temporary snapshot | PASS | loaded world.s1 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: removed an unfinished temporary file ; retry (season replayed and saved): equals the uninterrupted se… |
| S1-T2 | S1 | crash after the temporary snapshot, before the replace | PASS | loaded world.s1 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: removed an unfinished temporary file ; retry (season replayed and saved): equals the uninterrupted se… |
| S1-T3 | S1 | primary snapshot corrupted after a completed save (rolls back one save) | PASS | loaded world.s1.bak1 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: rejected world.s1 (checksum or frame); rolled back to world.s1.bak1 ; retry (season replayed and… |
| S1-T4 | S1 | completed save (control) | PASS | loaded world.s1 season 11 ; P at new team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True |
| S2-T1 | S2 | transfer written to the journal, crash before the snapshot commit (naive loader shown first) | PASS | naive loader: state season 10, P at former team, history ends at new team, 50 history violations ; loaded state.s2 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: tr… |
| S2-T2 | S2 | torn journal append (half the season's records, last one partial) | PASS | loaded state.s2 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: journal valid prefix ends: torn record at the end; truncated the journal after checkpoint 67926: 3507… |
| S2-T3 | S2 | journal appended, crash while writing the temporary snapshot | PASS | loaded state.s2 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: removed an unfinished temporary file; truncated the journal after checkpoint 67926: 701475 bytes (416… |
| S2-T4 | S2 | journal appended, crash after the temporary snapshot, before the replace | PASS | loaded state.s2 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: removed an unfinished temporary file; truncated the journal after checkpoint 67926: 701475 bytes (416… |
| S2-T5 | S2 | completed save (control) | PASS | loaded state.s2 season 11 ; P at new team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True |
| S2-T6 | S2 | primary snapshot corrupted after a completed save (rolls back one save, journal truncated to match) | PASS | loaded state.s2.bak1 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recovery: rejected state.s2 (checksum or frame); truncated the journal after checkpoint 67926: 701475 byte… |
| S2-T7 | S2 | a committed journal record corrupted (history before every checkpoint): detection, fail closed | PASS | load refused: S2: no snapshot agrees with the journal. journal valid prefix ends: chain mismatch at sequence 21367; rejected state.s2: its checkpoint 72093 is beyond the journal's valid prefix; rejected state.s2.bak1: its checkpoint 67926 is beyond the journal… |
| S2R-T1 | S2-reverse-order | snapshot committed, crash before the journal append (naive loader shown first) | PASS | naive loader: state season 11, P at new team, history ends at former team, 50 history violations ; loaded state.s2.bak1 season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; recover… |
| S3-T1 | S3-sqlite | failure inside the transaction before COMMIT (rollback) | PASS | loaded world.db season 10 ; P at former team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True ; retry (season replayed and saved): equals the uninterrupted season True, no duplicate events True |
| S3-T2 | S3-sqlite | completed save (control) | PASS | loaded world.db season 11 ; P at new team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save True |
| S3-T3 | S3-sqlite | a byte flipped inside the stored state blob: detection, fail closed | PASS | load refused: S3: state checksum mismatch |
| S3-T3b | S3-sqlite | B-tree page headers damaged (pages 2-5): detection, fail closed | PASS | load refused: S3: quick_check *** in database main *** Page 3: btreeInitPage() returns error code 11 Page 5: btreeInitPage() returns error code 11 Page 4: btreeInitPage() returns error code 11 Page 2: btreeInitPage() returns error code 11 Page 7 is never used … |
| S3-T4 | S3-sqlite | silent content change inside one event row (structurally valid; simulated by a direct UPDATE): detection, fail closed | **FAIL** | loaded world.db season 11 ; P at new team; history replay violations 0; ids unique and contiguous True; state equals the acknowledged save False |
| DUP-S1 | S1 | an acknowledged save retried (saved twice) | PASS | events 72093 = counter 72093; unique and contiguous True; state equals the season True |
| DUP-S2 | S2 | an acknowledged save retried (saved twice) | PASS | events 72093 = counter 72093; unique and contiguous True; state equals the season True |
| DUP-S3-sqlite | S3-sqlite | an acknowledged save retried (saved twice) | PASS | events 72093 = counter 72093; unique and contiguous True; state equals the season True |

19/20 scenarios pass.

**The critical scenario of the task (S2-T1).**
- **The failure, shown first.** A naive loader that reads the snapshot plus every journal record produces exactly the
  failure of the task: the state says P is at their former team, the history says they moved (50 history violations).
- **The recovery.** The S2 loader truncates the unfinished records. P is at their former team in both the state and the
  history, and the retried season reproduces the uninterrupted one.
- **The reverse order.** Snapshot first, then the journal (S2R-T1): the naive loader shows the opposite failure. The
  recovery loader detects that the checkpoint is beyond the journal and rolls back to `.bak1`. **Journal-first is
  required.**

**The one FAIL (S3-T4) is a finding, not a bug in the test.**
- **What happened:** an event row was changed in place (a structurally valid change, made with a direct `UPDATE`). It
  passed `quick_check` and loaded silently. SQLite keeps no checksum of row content.
- **S1 and S2 do detect it:** S1 by its file hash, S2 by the journal chain (S2-T7).
- **What S3 would need:** a per-row hash chain of its own.

### 6.5 Process kills (FACT; 12 kills per store, each during or just after a save, on a 10-season world)

| Store | Trials | Killed | Kill delay after the save began (ms) | Loaded = acknowledged | Loaded = acknowledged + 1 (save done, ack lost) | Recovery action taken | PASS |
|---|---:|---:|---:|---:|---:|---:|---:|
| S1 | 12 | 12 | 0–996 | 12 | 0 | 1 | 12 |
| S2 | 12 | 12 | 0–662 | 12 | 0 | 10 | 12 |
| S3-sqlite | 12 | 12 | 0–2525 | 12 | 0 | 0 | 12 |

- **Every killed process left a store that loaded the acknowledged state exactly.** No history was inconsistent, and
  no acknowledged event was lost.
- **Most S2 kills landed inside the save.** The recovery truncated the half-written journal tail.
- **Limit.** A process kill does not lose the operating system's write cache, so power loss is not covered. Simulated
  torn writes (S2-T2, S1-T1) stand in for it. Beyond these, the only durability evidence remains QG-04's tested local NTFS scenarios (Phase 4), which do not establish power-loss durability either.

### 6.6 Comparison (INFERENCE)

| Criterion | S1 | S2 | S3 |
|---|---|---|---|
| Crash consistency | Atomic by construction | Protocol: journal first, snapshot authority, truncation | Transactions |
| Corruption detection | Whole-file hash | File hash + chain | Structure and state hash; **not row content** |
| Corruption recovery | Roll back one save | Snapshot: roll back one save. A committed journal record: **refused** (no copy) | Needs a backup copy |
| Save cost growth | Rewrites all history every save (and copies `.bak1`) | Appends the season; rewrites the state | Inserts the season; rewrites the state blob |
| History query without loading | No | One sequential journal scan | Indexed SQL |
| Dependency | None | None | A native library outside the package feed (`winsqlite3.dll` here; Microsoft.Data.Sqlite needs nuget.org) |
| Determinism and replay | Unaffected: loads reproduce the live state hash | Unaffected (same check) | Unaffected (same check) |
| Implementation and maintenance | Smallest | Moderate: two files per slot, a recovery rule to keep exact | Largest: a schema, a native library, an export dependency in Godot |

**Reading of the benchmark (INFERENCE).**
- **S1-gzip meets the 20-season scale on this machine.** 9.3 MB, a 0.6 s save (with the `.bak1` → `.bak2` copy) and a
  1.3 s load at season 20.
  - Its cost grows linearly with history: from season 10 to 20, about 0.3 MB, 16 ms of save and 50 ms of load per season.
  - Production's verify-after-write (SnapshotStore reads the temporary file and the result back) would add about two
    loads per save.
- **S2 does not save much yet.** Its journal holds the career events, but its state snapshot still carries every
  retired person and every inactive contract (31 MB of 49 at season 20). Its loads are among the slowest (1.35 s at
  season 20, 1.6 s at season 10), because the whole journal chain is verified.
  - S2's per-save advantage appears only with an archive rule that moves retired people and ended contracts out of
    the state (E3 of NPC_EMPLOYMENT_DISTRIBUTION §8).
- **S3 is the largest** (87 MB: rewritten state blobs leave free pages), and the slowest to save. It is the fastest for
  history queries and state-only loads.

**Recommendation (PROPOSAL).**
- **Approve the requirement:** a durable, append-only career history that is never pruned to keep the save small.
- **Prefer S1 with compression,** the simpler option. It keeps the current protocol and its single authority. It is
  atomic by construction, passes every scenario and kill, and is inside a season-end save budget at 20 seasons.
  - Conditions: raise the save byte and array limits to measured values, and keep exact identity.
  - Decide the save frequency: a daily autosave at 0.6–1.3 s would be felt.
- **Keep S2 as the verified scale-up path.** Switch to it if the approved campaign length, save frequency or archive
  rule makes whole-history rewrites too slow. Its recovery protocol (journal first, snapshot authority) is the part to
  keep.
- **Do not adopt S3 now:** a new native dependency, the largest files, and no row-content integrity without extra work.

## 7. Regression and performance

### 7.1 Tests executed (2026-10-15)

| Suite | Command | Result |
|---|---|---|
| Production Domain | `dotnet tests/Domain/bin/Release/net10.0/Domain.dll` | **15/15 PASS** |
| Production Integration | `dotnet tests/Integration/bin/Release/net10.0/Integration.dll` | **369/369 PASS**, hash `e196d247…` (unchanged from HYBRID_WORLD_INTEGRATION_REVIEW §10) |
| Data tool | `.venv\Scripts\python.exe -m unittest discover -s tests` | **120/120 PASS** (113 before, plus 7 new elite-scale tests) |
| World experiment v1 | `WorldExperiment test --seasons 10` | **40/40 PASS**, before and after this task's changes; hashes identical (§0) |
| Decision runs | `WorldExperiment decide …` (§10) | Integrity table below |
| Fault injection | `WorldExperiment persist-crash --base-seasons 10` | **19/20 PASS**; S3-T4 FAIL (finding, §6.4) |
| Process kills | `WorldExperiment persist-kill --base-seasons 10 --trials 12` | **36/36 PASS**, 36 killed |
| Not run | `build/Verify.ps1` (UI, Godot headless, captures), rules gates (`Measure-Rules`) | NOT RUN: no production code changed |

### 7.2 Decision runs: seeds, integrity, determinism and cost (FACT)

20 seasons each. Seeds 1–48 for A, B, C, D1a and D1b; 1–32 for D1t, and 1–32 plus 45–48 for D1bt; 1–16 for B-all;
1–12 for B-open; 1–8 for B-fixed and B-path; 1–3 for B-bounded and for the B replay. The seed counts per variant are in
the table.

| Variant | Seeds | Invariant failures (start / checkpoints) | History violations (end) | Conservation violations | Max registered | NPC start > 82 | Max initial NPC | Creation s (median) | Season s (median / p95) | Seasons > 60 s (sleep) | Peak MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A | 48 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.06 | 2.23 / 3.343 | 1 | 181.4 |
| B | 48 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.07 | 3.04 / 4.759 | 3 | 181.3 |
| B-all | 16 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.31 | 3.94 / 8.484 | 4 | 176.2 |
| B-bounded | 3 | 0 / 0 | 0 | 0 | 11 | 0 | 82 | 4.17 | 3.47 / 5.08 | 0 | 173.1 |
| B-fixed | 8 | 0 / 0 | 0 | 0 | 11 | 0 | 82 | 3.9 | 3.64 / 7.879 | 0 | 189.3 |
| B-path | 8 | 0 / 0 | 0 | 0 | 11 | 0 | 82 | 3.45 | 3.44 / 5.324 | 0 | 184.7 |
| B-replay | 3 | 0 / 0 | 0 | 0 | 11 | 0 | 82 | 3.71 | 3.54 / 5.444 | 0 | 178.8 |
| BO | 12 | 0 / 0 | 0 | 0 | 11 | 0 | 82 | 8.18 | 6.83 / 8.25 | 0 | 165.9 |
| C | 48 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.33 | 1.95 / 4.591 | 3 | 180.7 |
| D1a | 48 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.38 | 3.21 / 7.897 | 4 | 181.6 |
| D1b | 48 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.45 | 3.2 / 7.729 | 2 | 180.9 |
| D1bt | 36 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.15 | 3.23 / 7.948 | 3 | 183.7 |
| D1t | 32 | 0 / 0 | 0 | 0 | 12 | 0 | 82 | 3.89 | 3.71 / 7.924 | 1 | 178.9 |

**Determinism (FACT).**
- **Replay:** seeds 1, 2, 3 of B rerun in a separate process give the identical final digest after 20 seasons: **PASS**.
- **Season-0 identity with B on shared seeds** (paired comparability): A 48/48; B-all 16/16; B-bounded 3/3; B-fixed 8/8; B-path 8/8; B-replay 3/3; BO 12/12; C 48/48; D1a 0/48; D1b 0/48; D1bt 0/36; D1t 16/32.
  The policy and market variants start from B's exact world; the D1 variants differ by design (their generation changes ceilings or the pack).

**Comparability.** Paired seeds share the identical initial world, except D1 variants, whose generation differs by
design: only the NPC ceilings, or the pack's elite players, change at creation.

**Cost.** The runs were measured with 3–5 in parallel on a 4-core laptop (8 GB). Medians and p95 are reported because
the laptop slept for about two hours mid-run: the few seasons over 60 s are that pause, not simulation cost. An isolated
season costs about 1.2–2 s (1.24 s mean in this task's rerun of the v1 test; HYBRID_WORLD_INTEGRATION_REVIEW §3).

**Sample size.**
- **48 paired seeds** for the policy comparison and the D1 sensitivity pair.
- **32 for the D1 candidates.** Their paired differences are either exactly zero (potential below 96) or large (the
  elite re-knot). The 4-core machine runs about two 20-season runs a minute, so the full 48 would have added about
  15 minutes.
- **8–16 for the sensitivity variants,** whose effects are large or, for B-bounded, identical to B by construction.
- **Every mean is printed with its standard error.**

## 8. Production change impact

Every row below is a proposal. None of it is implemented in `src/`, `content/` or the save schema.

### 8.1 D-H1 B — transparent NPC fallback for real organizations

| Aspect | Impact |
|---|---|
| Affected modules | Domain: a population model with person origin (real / fictional), team origin and a lineup rule per origin (today `Rivals.Lineup` has no origin). Application: AI recruitment of off-screen and rival teams. Infrastructure: save validation of origin, imported-roster history and the no-NPC-starter-at-creation rule. Content: the local real pack (DEC-031). UI: rival squads and history views |
| Domain / state ownership | World State (DEC-010) owns every non-company team. Origin is an immutable person attribute. The imported roster is Snapshot-era history and is never rewritten by a simulated move |
| Data schema | Person: origin, provider id (real only). Team: real flag, provider id. Career event: era (PreWorld / Snapshot / Simulation) |
| Content hash | The policy is a rules value (a new rules version). The real pack is pinned by its own hash (DEC-031); `content/fixture.json` does not change for the policy |
| Save compatibility | A new save schema and rules version; older saves are rejected, as today (DV-08) |
| UI | A visible "fictional" marker on NPCs in real teams; an "imported roster as of 2026-10-13" history view per real team |
| Testing | Invariants: no NPC starter at creation; NPCs join real teams only through the market (fee, wage cap, willingness, squad ≤ 16); no spawning; origin never changes. Rules gates at 48 paired seeds (GENERATOR_V2_PLAN §6) |
| Risks | Authenticity erodes anyway: real players retire, and by season 10 almost no real team fields five real players (§3). The fallback rule keeps weaker real players over stronger NPCs. Real teams pull the best NPCs out of the fictional pyramid (§5.4) |
| Rollback | The policy is one rules value. Returning to STRICT is a new rules version; saves made under B do not load under it |

### 8.2 D1a — the attainable top of the scale

| Aspect | Impact |
|---|---|
| Affected modules | Research tooling: `rating_model.py` `SCALE_MAX` 93 → 99 and the extra knot (the uncapped experiment). Generator (when NPC generation reaches production, GENERATOR_V2_PLAN): the ceiling tail rule |
| Ownership | The rating scale is content/tooling. Skills, Overall and potential stay derived as today (`Ratings.InRole`) |
| Data schema | None. Values stay 1–99 |
| Content hash | Changes when the generator content or the real pack changes (new pack hash) |
| Save compatibility | New pack or content hash → saves bound to the old hash are rejected (exact identity) |
| UI | None required; 94–99 already render |
| Testing | R01–R07 of the world tests; the NPC start cap (no NPC above 82 at creation, every intake included); potential never below Overall; the 48-seed gates |
| Risks | Low. The research clamp changes 3 potentials and no Overall; the tail rule leaves every ceiling below 96 unchanged |
| Rollback | Restore the constants; regenerate the pack |

### 8.3 D1b — re-knotting the elite segment

| Aspect | Impact |
|---|---|
| Affected modules | `rating_model.py` `BAND_KNOTS` (the user's band decision of 2026-10-14); the real pack; every downstream rating report |
| Ownership | A band-definition decision (the user's) |
| Data schema / save | None / new pack hash |
| UI | Bands and labels (ELITE 90–93 would no longer be the top band) |
| Testing | Out-of-sample calibration (the validation pipeline's frozen-cutoff design), the 48-seed gates, wage and fee curves above 90 |
| Risks | Stronger top teams; higher wages at the top; the evidence is in-sample and within one standard error (§4.4) |
| Rollback | Restore the knots; regenerate the pack |

### 8.4 E2 / F1 — replacement intake and no campaign cap

| Aspect | Impact |
|---|---|
| Affected modules | Domain `Lifecycle` (today a fixed `YouthIntake` 15, a pool cap `PoolCap` 30 with the oldest retired above it, `Lifecycle.cs:102`); `SnapshotValidation.cs:77` (a hard bound of 114 candidates); content `Lifecycle` block |
| Ownership | World State: the population and the open market |
| Data schema | Free-agent pool unbounded by schema; per-person career events; entrant pathway label |
| Content hash | New `Lifecycle` values → new content hash |
| Save compatibility | New save schema (the validator bound changes) |
| UI | Market and scouting over hundreds of free agents (DEC-005 estimates; query budget ≤ 250 ms at 10k, met in the experiment) |
| Testing | Population conservation every season; intake = retirements; no forced retirement or assignment; free-agent share reported, never enforced |
| Risks | The one-to-one rule counts NPC retirements only; with real teams recruiting NPCs (B) the fictional pyramid thins (§5.4). Entrants are all 16–18, so the population grows younger |
| Rollback | Restore the fixed intake and the cap (a rules version) |

### 8.5 D-H3 — durable career history

| Aspect | Impact |
|---|---|
| Affected modules | Infrastructure `SnapshotStore`, `SnapshotDtos`, `SnapshotValidation`, `StrictJson` limits; Domain history model (today `World.Moves`, pruned each season, `Simulation.cs:413`) |
| Ownership | One authority for current state (the snapshot). History is append-only and never a second authority |
| Data schema | Career events with stable sequence numbers inside the snapshot (S1). With S2: a journal checkpoint in the snapshot and a second file |
| Content hash | None |
| Save compatibility | New save schema; gzip framing; raised byte and array limits; older saves rejected (DV-08) |
| UI | History views (player career, team history) read the loaded history through an index |
| Testing | The fault-injection scenarios and kill trials of §6, at the approved campaign scale; save/load budgets (DV-07) |
| Risks | The save cost grows with total history. A corrupted save rolls back one save (two backups, as today). With S2, two files per slot must be backed up together, and a corrupted committed journal record is detected but not repairable without a copy |
| Rollback | Schema version; old builds keep their own saves |

## 9. Director approval checklist

**READY TO APPROVE**
- [ ] **D-H3 requirement:** career history is durable and append-only. It is never pruned to keep the save small.
- [ ] **D1a:** the research scale's clamp at 93 goes (no Overall changes), and the NPC ceiling tail reaches 99 above 96.
- [ ] **F1:** no hard free-agent maximum after creation. The share is monitored and warned at 12%, never enforced. The
  creation invariant (fewer than 400 of 4,000) stays.

**APPROVE WITH CONDITIONS**
- [ ] **D-H1 B, transparent NPC fallback.** Conditions:
  1. the Director explicitly accepts the change to the "no NPC starter on a real team" rule after the snapshot;
  2. a visible fictional marker;
  3. the imported roster kept as history;
  4. OneToOneAll intake (below).
- [ ] **E2, one-to-one replacement intake.** Condition: with B, the intake counts every retirement (OneToOneAll).
- [ ] **D-H3 architecture S1 with compression.** Conditions:
  1. the crash scenarios and kill trials rerun on the production implementation at the approved scale;
  2. save and load inside the DV-07 budget at the approved save frequency;
  3. the byte and array limits raised to measured values.

  S2 is the documented alternative if a condition fails.

**REQUIRES MORE EVIDENCE**
- [ ] **D1b, re-knotting the elite band.** Needs an out-of-sample calibration, and the user's band decision.
- [ ] **B-open**, merit recruitment by real teams. The credibility trade-off (authenticity against strength) is a design
  call.
- [ ] **Entrant pathways**, the entrant age mix. The weights are authored.
- [ ] **Campaign scale, save frequency and the archive rule (DV-07, E3).** They decide whether S1 stays sufficient or S2
  is needed.

**NOT RECOMMENDED**
- [ ] A STRICT for campaigns longer than about 3 seasons.
- [ ] C as the answer to roster shortages.
- [ ] The wider ceiling room (a recalibration, not a removal).
- [ ] Fixed intake.
- [ ] S3 now.

## 10. Reproduce

```powershell
cd tools/lol-data-ingestion
.\.venv\Scripts\python.exe elite_scale_experiment.py --sidecar K96      # D1b analysis + sidecar -> data/world/decisions/
.\.venv\Scripts\python.exe -m unittest discover -s tests -p "test_*.py"
cd ../..
dotnet build tools/WorldExperiment/WorldExperiment.csproj -c Release
$x = "tools/WorldExperiment/bin/Release/net10.0/WorldExperiment.exe"; $o = "tools/lol-data-ingestion/data/world/decisions/runs"
& $x decide --label A  --policy STRICT       --intake OneToOne --seeds 1-48 --seasons 20 --out $o
& $x decide --label B  --policy NPC_FALLBACK --intake OneToOne --seeds 1-48 --seasons 20 --out $o
& $x decide --label C  --policy EXIT         --intake OneToOne --seeds 1-48 --seasons 20 --out $o
& $x decide --label BO --policy NPC_OPEN     --intake OneToOne --seeds 1-12 --seasons 20 --out $o
& $x decide --label B-all --policy NPC_FALLBACK --intake OneToOneAll --seeds 1-16 --seasons 20 --out $o
& $x decide --label B-fixed --policy NPC_FALLBACK --intake Fixed --seeds 1-8 --seasons 20 --out $o        # also Pathways 1-8, Replacement 1-3
& $x decide --label D1a --policy NPC_FALLBACK --ceiling d1a --seeds 1-48 --seasons 20 --out $o           # sensitivity; D1b adds --elite
& $x decide --label D1t  --policy NPC_FALLBACK --ceiling d1a-tail --seeds 1-32 --seasons 20 --out $o
& $x decide --label D1bt --policy NPC_FALLBACK --ceiling d1a-tail --elite tools/lol-data-ingestion/data/world/decisions/elite_reknot_k96.json --seeds 1-32 --seasons 20 --out $o
py -3.11 tools/WorldExperiment/decision_summary.py                        # -> decision_summary.json, decision_tables.md, report_tables.md
& $x persist --seasons 20 --out tools/lol-data-ingestion/data/world/decisions/persistence
& $x persist-crash --base-seasons 10 --out tools/lol-data-ingestion/data/world/decisions/persistence
& $x persist-kill --base-seasons 10 --trials 12 --out tools/lol-data-ingestion/data/world/decisions/persistence
```

Prerequisites are the same as the [world experiment](README.md#reproduce). `--resume` skips seeds already written.
