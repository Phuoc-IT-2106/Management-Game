# Free-agent market validation

Date: 2026-10-15. Status: **EXPERIMENT**. Percentages and rules are proposed configuration
(`tools/WorldExperiment/config/world-experiment.v1.json`), not approved canon.

Labels: **FACT**, **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. Initialization constraint (FACT)

| Requirement | Configured | Measured |
|---|---|---|
| Free agents strictly below 10% of the 4,000 NPCs (≤ 399) | Target 8% (`FreeAgentTargetPercent`). Hard limit `< 10%` enforced at build: the builder refuses a configuration whose target is not below it | **320 (8.00%)** |
| Every NPC accounted for | Census mutually exclusive | 4,000 = 300 + 178 + 710 + 2,003 + 366 + 123 + 320 |
| No hidden or undefined state | Three states only: employed / free agent / retired | 0 invariant failures |
| Real free agents counted separately | — | 0 real free agents at creation (not verifiable from sources). Combined NPC + real: 320 / 5,135 = 6.23% |

**How the 320 are chosen.**
- **Method:** weighted sampling without replacement, keyed by person. The propensity weight comes from the person, not
  from a quota:
  - age: 2.2 at ≤ 17, 1.8 at 18, 1.3 at 19, 0.8 at 20–22, 0.6 at 23–26, 0.9 at 27–28, 1.6 at 29+;
  - career profile: prospect 1.2, amateur 1.0, semi 0.9, pro 0.8;
  - × 0.6 when the player is rated 78 or more.
- **Exact count:** exactly 320 are taken, by the order of the keyed draws.
- **Origins come afterwards:** each free agent's origin comes from their own generated history
  ([NPC_EMPLOYMENT_DISTRIBUTION §5](NPC_EMPLOYMENT_DISTRIBUTION.md#5-career-history)), never from a target.

## 2. Free-agent breakdown at creation (FACT)

| Free-Agent Origin | Count |
|---|---:|
| Never contracted | 142 |
| Contract expired | 41 |
| Released/dismissed | 53 (52 released, 1 dismissed) |
| Voluntary departure | 21 |
| Former academy/amateur player | 63 (54 whose academy or amateur contract expired, 9 not retained by an academy) |
| Other verified simulation origins (team dissolved) | 0 |
| **Total free agents** | **320** |

**Classification rule** (mutually exclusive). The last exit in the person's history decides:
1. none → never contracted;
2. released or dismissed;
3. voluntary departure;
4. academy not retained, or an expired contract with an academy or amateur team → former academy/amateur;
5. any other expired contract → contract expired;
6. team dissolved → other.

**Every free agent has an individual history:** 320 of 320, at 2.8 events each on average. Nobody who never signed has
an exit event.

**Mix and quality at creation:**

| Age | Free agents | Overall mean / p90 / max | Potential 85+ | Origins |
|---|---:|---|---:|---|
| 16–19 | 141 | 46.5 / 62 / 76 | 11 | never contracted 124, former academy/amateur 7, released 4, voluntary 4, expired 2 |
| 20–22 | 72 | 52.7 / 70 / 77 | 1 | former academy/amateur 20, never 17, released 17, expired 11, voluntary 7 |
| 23–26 | 57 | 56.9 / 72 / 76 | 0 | released 18, former academy/amateur 17, expired 15, voluntary 7 |
| 27–29 | 42 | 54.6 / 68 / 81 | 0 | former academy/amateur 16, released 13, expired 9, voluntary 3, never 1 |
| 30+ | 8 | 57.3 / 78 / 79 | 0 | expired 4, former academy/amateur 3, released 1 |

**Quality compared with employed NPCs.**

| | Mean | p50 | p90 | Max |
|---|---:|---:|---:|---:|
| Free agents | 51.1 | 49 | 69 | 81 |
| Employed NPCs | 53.8 | 51 | 73 | 82 |

**Who the free agents are.**
- 13 free agents have held a professional contract.
- 12 have potential 85+.
- **12 are rated 75 or more, and each has an explanation:** 9 contracts expired, 2 left voluntarily, 1 was released;
  their ages run from 18 to 30.
- 50 had been unemployed for one or two seasons at the start.

**INFERENCE.**
- **The pool is a mix**, not only weak or young players: 107 free agents are 23 or older, and 74 were released or left.
- **High-rated free agents are rare and explained.**

## 3. The market over 10 seasons (baseline, seed 20261015)

The market is read at two moments of each season end. **Pre-window** is after retirements, contract ends, releases,
dismissals, academy graduation, dissolutions and the intake: the supply a recruiter sees. **Post-window** is after
every team has recruited: who stayed unsigned.

### 3.1 After the window (the authoritative free-agent state)

| Season | NPC free agents | % of active NPCs | Never | Expired | Released/dismissed | Voluntary | Former academy/amateur | Dissolved | Unemployed 0 / 1 seasons | Overall mean / p90 / max |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| 0 | 320 | 8.00 | 142 | 41 | 53 | 21 | 63 | 0 | 271 / 35 (+14 two) | 51.1 / 69 / 81 |
| 1 | 269 | 6.72 | 12 | 76 | 0 | 12 | 164 | 5 | 267 / 1 | 50.3 / 61 / 74 |
| 2 | 347 | 8.68 | 0 | 125 | 2 | 15 | 202 | 3 | 166 / 181 | 52.0 / 61 / 73 |
| 3 | 311 | 7.78 | 0 | 112 | 3 | 18 | 178 | 0 | 188 / 123 | 52.7 / 65 / 74 |
| 4 | 298 | 7.45 | 0 | 81 | 6 | 10 | 201 | 0 | 159 / 139 | 53.4 / 64 / 73 |
| 5 | 278 | 6.95 | 0 | 55 | 6 | 16 | 195 | 6 | 166 / 112 | 53.3 / 62 / 72 |
| 6 | 273 | 6.82 | 0 | 51 | 4 | 12 | 202 | 4 | 156 / 117 | 53.4 / 62 / 75 |
| 7 | 281 | 7.02 | 0 | 51 | 9 | 9 | 212 | 0 | 159 / 122 | 52.9 / 60 / 75 |
| 8 | 291 | 7.28 | 0 | 53 | 13 | 9 | 216 | 0 | 158 / 133 | 52.4 / 59 / 78 |
| 9 | 255 | 6.38 | 0 | 36 | 8 | 9 | 199 | 3 | 140 / 115 | 52.3 / 58 / 66 |
| 10 | 251 | 6.28 | 0 | 42 | 11 | 18 | 177 | 3 | 142 / 109 | 52.6 / 60 / 78 |

After the window, the NPC free-agent share stays at **6.3–8.7%**. That is inside the proposed 6–9% campaign range, with
no unemployment warning (threshold 12%). Free agents average 50–53 against 52–54 for employed NPCs.

### 3.2 As the window opens (the supply)

| Season end | NPC free agents | % | Never contracted (new intake) | Expired | Released/dismissed | Voluntary | Former academy/amateur | Dissolved | Overall mean / p90 / max | 70+ | Potential 85+ | Former professionals |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|
| 1 | 1,146 | 28.7 | 634 | 109 | 38 | 40 | 292 | 33 | 47.5 / 63 / 79 | 54 | 40 | 36 |
| 2 | 1,023 | 25.6 | 483 | 145 | 3 | 53 | 291 | 48 | 47.5 / 61 / 76 | 41 | 38 | 64 |
| 5 | 1,027 | 25.7 | 460 | 97 | 6 | 65 | 329 | 70 | 48.9 / 62 / 83 | 36 | 46 | 87 |
| 10 | 857 | 21.4 | 375 | 79 | 14 | 40 | 314 | 35 | 48.5 / 60 / 79 | 14 | 25 | 62 |

The pre-window count of season 1 includes the 269 left unsigned from creation plus the season's exits and intake.

**INFERENCE.**
- **The open window is where choice exists.** Every season it offers 14–54 players rated 70+, 25–46 with potential 85+,
  and 36–94 former professionals. Real windows behave the same way: most are signed within it.
- **What stays unsigned** is mostly former amateur and academy players and experienced players who wait for their
  reservation wage (§4).

### 3.3 Flows

| Season | NPC retirements | Intake | Signings from the market | Moves to a higher tier | Same tier | Amateur/semi-pro to professional | Academy promotions | Academy not retained | Contracts expired | Released | Dismissed | Voluntary | Dissolution exits |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 496 | 496 | 882 | 153 | 65 | 9 | 17 | 162 | 170 | — | 1 | 22 | 33 |
| 2 | 473 | 473 | 686 | 205 | 71 | 11 | 5 | 91 | 155 | 9 | 3 | 44 | 43 |
| 3 | 522 | 522 | 829 | 215 | 51 | 13 | 7 | 124 | 123 | 8 | 3 | 44 | 28 |
| 5 | 460 | 460 | 763 | 232 | 59 | 19 | 11 | 129 | 62 | 4 | 2 | 58 | 70 |
| 10 | 404 | 404 | 627 | 129 | 43 | 19 | 11 | 113 | 58 | 9 | 1 | 43 | 29 |

**Over the 10 seasons** (snapshot after season 10):
- **Market signings:** 7,165. By buyer: amateur 4,402, academy 1,045, semi-pro 949, fictional professional 417, real
  teams 352 (of which 292 NPC reserve signings).
- **Team-to-team moves:** 2,680. Every transfer, 2,573 of them, has a recorded fee; the 107 academy promotions have
  none. The median fee is 2,186 minor units, p90 10,101, maximum 138,720.
  - Most common seller → buyer pairs: amateur → academy 641, amateur → amateur 327, real d → real c 308, academy →
    semi-pro 253, semi-pro → fictional pro 169, real c → real b 155, real b → real a 137.
- **Unemployment spells:** 5,803 ended in the window they began, 213 after one season and 882 after two. The two-season
  peak is the reservation-wage patience. A further 385 spells ended in retirement.

## 4. Market rules in the experiment

| Rule | Behaviour |
|---|---|
| **Contract end** | A team renews a starter, someone within 2 points of its weakest starter, or a young player (≤ 22) with 6+ of headroom, while within its wage cap. A develop or conserve team lets non-starters of 28+ go |
| **Voluntary departure** | The player declines to renew in two cases: ambition ≥ 65 and rated at or above their layer's ceiling (amateur 62, semi-pro 72), seeking a higher level; or ambition ≥ 70 and played under 10% of matches, wanting playing time. 398 in the simulated seasons (42 seeking a level, 356 wanting time) |
| **Release** | A squad more than one over its target releases its weakest non-starter and pays half of the remaining wages (64 releases) |
| **Dismissal** | A conduct breach dismisses a player with professionalism under 30 (0.4% a season per point below) (23) |
| **Team dissolution** | Amateur 2% and semi-pro 0.8% a season; 50% when funds fall below −20% of revenue. Players leave with a `TeamDissolved` event, and a new team is founded in the same league |
| **Academy graduation** | At 21: promoted when rated 60+, there is room and wage, and the player is better than a senior reserve. Otherwise not retained (107 promoted, 1,269 not retained in 10 seasons) |
| **Willingness** | A free agent rated 76+ joins only professional teams, and one rated 65+ only semi-pro or better. After two seasons unemployed they accept anything |
| **Reservation wage** | A player who lost a contract refuses an offer below 80% of their last wage. The floor falls by 35% for each season unemployed |
| **Real players** | Real players join real teams only. NPCs join real teams only into their reserve seats |
| **Scouting** | A buyer sees Overall and potential with its layer's noise (σ 1.5–4; double for potential) |
| **Fees** | The production fee (`Pay.Fee`) × the seller's layer factor × the remaining contract (50–100%) |

**Trial history (reported, not hidden).** The first two runs failed the market requirements, and the configuration was
changed:
1. **Intake.** A fixed intake of 330 a season shrank the NPC population by about 4% a season. Retirements ran at about
   12%, so the free-agent pool drained to 0.2% by season 2. The intake was then calibrated to replacement (§5).
2. **Free-agent quality.** After season 1, free agents were only weak prospects (mean 36, none above 60), because every
   decent player was re-signed in the same window. Lowering the willingness thresholds from 75/82 to 65/76 changed
   nothing: semi-pro teams pick before amateur teams, so they took everyone first. Adding the reservation wage produced
   the diverse pool of §3.

## 5. Dynamic free-agent ratio: sensitivity (10 seasons each)

| Run | NPC free-agent % by season (1–10) | Range | Active NPCs at 10 | Warnings |
|---|---|---|---:|---|
| Baseline, replacement intake | 6.72 8.68 7.78 7.45 6.95 6.82 7.02 7.28 6.38 6.28 | 6.3–8.7 | 4,000 | none |
| Seed 1 | 6.65 8.48 6.85 7.10 6.88 6.92 7.32 7.58 6.65 6.88 | 6.7–8.5 | 4,000 | none |
| Seed 2 | 7.10 8.25 7.15 6.65 6.65 6.42 6.60 6.30 6.68 6.98 | 6.3–8.3 | 4,000 | none |
| Seed 3 | 6.65 8.30 7.18 7.30 7.45 6.80 6.52 6.85 6.78 7.18 | 6.5–8.3 | 4,000 | none |
| Fixed intake of 480 | 6.40 8.79 7.75 7.61 7.17 7.33 9.16 10.48 12.26 13.61 | 6.4–13.6 | 4,351 | seasons 9 and 10 above 12% |
| Real teams may start NPCs (not the rule) | 5.62 7.68 6.90 6.30 5.80 5.42 5.08 5.00 4.97 4.95 | 5.0–7.7 | 4,000 | none |

**FACT.**
- **Fixed intake.** Exits fall from about 500 to about 400 a season as the opening population ages through. A fixed
  intake of 480 therefore grows the population, and unemployment rises past the warning level.
- **Replacement intake** keeps the population and the free-agent share stable on four seeds.
- **The NPC fallback variant** pulls NPCs into 896 real starting slots by season 10, which lowers NPC unemployment.

**Nothing forces the ratio.** No contract is invented, nobody is deleted or retired early, and no team assignment is
arbitrary. The ratio moves with exits, the intake, team needs and players' willingness. The warning is logged when it
exceeds 12%.

## 6. Market depth by role (NPC free agents after the window: count / rated 70+ / potential 85+)

| Season | Top | Jungle | Mid | Bot | Support |
|---:|---|---|---|---|---|
| 0 | 68 / 3 / 1 | 57 / 6 / 2 | 68 / 8 / 2 | 62 / 11 / 3 | 65 / 4 / 4 |
| 5 | 61 / 1 / 1 | 47 / 0 / 0 | 51 / 1 / 0 | 58 / 3 / 0 | 61 / 1 / 0 |
| 10 | 49 / 1 / 0 | 52 / 1 / 0 | 56 / 1 / 0 | 52 / 1 / 0 | 42 / 0 / 0 |

Every role keeps 40+ unsigned players. Strong unsigned players are rare after the window, by design; see §3.2 for what
the window offers.

## 7. Recruitment scenarios (evidence from the simulated seasons)

| Scenario | Evidence |
|---|---|
| A low-rated prospect with high potential | Every season 25–46 free agents with potential 85+ are available as the window opens. Professional reserves at creation have a median age of 20 and potential p90 91 |
| A veteran for immediate performance | The upgrade rule: a contend or balanced team buys a player at least 4 points better than its weakest starter (115–195 upgrades a season) |
| Replacing an expensive substitute | A squad over target releases its weakest non-starter and pays half of the remaining wages (64 releases) |
| Discovering an amateur prospect | 186 moves from amateur or semi-pro teams to professional teams; 641 amateur → academy moves |
| Signing a recently released professional | Former professionals are 36–94 of the pre-window free agents each season |
| Developing an academy player instead of buying | 107 senior promotions, part of them to fill a starting vacancy before the market was tried |
| Keeping a reserve seat empty to save wages | A reserve seat is left empty when no free agent is affordable or willing: `reserve_unfilled` 57–205 a season |
| A free agent is not willing to join every team | Willingness (level and reservation wage) blocks offers. 882 spells ended only after two seasons, when patience ran out |

**Dependencies (not built; flagged rather than invented):**
- **Negotiation.** The production game has no player negotiation, only fixed terms. Willingness here is a rule, not a
  negotiation.
- **Scouting confidence as a visible number.** Scouting noise exists, but no confidence value is shown.
- **Adaptation and familiarity cost for new signings.** Familiarity exists in production for the company only.

## 8. Tests executed (FACT)

Market lifecycle checks from `dotnet run --project tools/WorldExperiment -c Release -- test --seasons 10`, all pass. The
tested world had run 11 simulated seasons: 10, plus one season to compare continuing after a reload.

| Check | Result |
|---|---|
| M01 never-contracted prospects enter and sign | 4,810 prospects |
| M02 contract expiry | 985 expiries |
| M03 release and dismissal | 71 released, 26 dismissed |
| M04 signings | 2,703 signings, 3,575 first amateur contracts, 827 first professional contracts |
| M05 team changes without duplicate ownership | 2,288 transfers; 0 history violations |
| M06 amateur → professional | 186 |
| M07 academy promotion | 115 |
| M08 retirement | 5,841 |
| M09 vacancies filled | 5,293 |
| M10 the free-agent share changes naturally | — |
| M11 history immutable | — |
| N02 initial free agents < 400 | 320 |

## 9. Decisions needed

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| F1 | A hard free-agent maximum during play | (a) no hard maximum: monitor, warn at 12% (now); (b) a hard maximum, which needs an explicit mechanism such as an elastic amateur layer that founds teams when unemployment is high | (a). (b) only with an explicit, visible mechanism |
| F2 | Reservation wage and level willingness | (a) keep them (they create the mixed pool); (b) remove them (the pool empties to weak prospects) | (a); the values (80%, −35% a season, 65/76) are authored |
| F3 | The intake rule | See NPC_EMPLOYMENT_DISTRIBUTION E2 | Replacement intake |
| F4 | What the player sees | (a) the whole pre-window supply, filtered by scouting; (b) only the post-window remainder | (a) |
