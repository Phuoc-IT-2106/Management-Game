# NPC employment distribution

Date: 2026-10-15. Status: **EXPERIMENT**. World structure and numbers are configuration
(`tools/WorldExperiment/config/world-experiment.v1.json`), not approved canon. The allocation code is
`tools/WorldExperiment/WorldBuilder.cs`. Outputs are git-ignored.

Labels: **FACT**, **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. World structure at creation

| Layer | Teams | Leagues / circuits | Squad target (registered) | Coach Development (bell) | Season revenue (minor units) | Wage factor × production wage curve |
|---|---:|---|---|---|---:|---:|
| Real teams (playable verified) | 210 | 31 real leagues | real squad (5–10) + NPC reserve seats (0–2) | tier a 60–92, b 50–85, c 45–80, d 35–70 (assumed) | a 1.5M, b 650k, c 300k, d 120k (assumed) | a 1.6, b 1.0, c 0.6, d 0.35 |
| Fictional professional | 60 | 6 leagues × 10 | 7–9 | 50–90 | 900k ± 15% | 1.0 |
| Academy (one per fictional pro organization) | 60 | 6 academy circuits × 10 | 5–7 | 45–85 | 120k | 0.3 |
| Semi-professional | 100 | 10 leagues × 10 | 6–8 | 35–75 | 260k | 0.4 |
| Amateur | 334 | 34 circuits of ≤ 10 | 5–7 | 20–60 | 45k | 0.12 |
| **Total** | **764** | | | | | |

- **Organizations.** A fictional professional organization owns a senior team and an academy team. Every other team is
  its own organization.
- **Names.** Fictional team names come from authored word lists. They are unique and never equal any of the 1,940 real
  team names.
- **Strategies.** Teams get keyed strategies (contend, balanced, develop, conserve, weighted by layer), intensity,
  youth focus and reputation.

**How the amateur count is set.** It is not configured: it **absorbs exactly the remaining employed seats**. 3,680
employed NPCs (4,000 − 320 free agents), less 1,677 fixed seats (professional, academy, semi-pro and 123 real-team NPC
seats), leaves 2,003 amateur seats in 334 teams of 5–7.

## 2. Allocation process

1. **Free-agent cohort first:** exactly 8% (320), selected without replacement by person propensity. The age
   weights are 2.2 at 17 and under, falling to 0.6 at 23–26, then rising to 1.6 at 29+. Profile weights favour
   prospects, and players rated 78+ get 0.6. The free-agent cohort is described in
   [FREE_AGENT_MARKET_VALIDATION](FREE_AGENT_MARKET_VALIDATION.md).
2. **Then layer by layer**, in the order fictional professional → real-team NPC reserve seats → academy → semi-pro →
   amateur:
   - **Starters:** one per role, snake order across the layer's teams, the largest budgets first.
   - **Reserves:** to each squad's target.
3. **Scouting.** Each team scores candidates with its layer's scouting noise: σ 2 for professionals, 3 for academy and
   semi-pro, 4 for amateurs; doubled for potential.
   - A **starter** is valued on current ability plus 0.3 × youth focus × headroom (ages ≤ 21, half to 23).
   - A **reserve** is valued with the full youth weight.
   - **Academies** take ages ≤ 20, valued on potential and youth.
   - **Real teams** take NPC reserves aged ≤ 22, valued on potential.
4. **Affordability.** A team takes its best-valued candidate whose wage fits its wage cap (revenue × 0.8 / season
   days). When no candidate fits, it takes the cheapest.
5. **Contracts.** Every employed NPC gets an active contract with an end season:
   - professional and academy: 1–3 seasons;
   - semi-pro: 1–2;
   - amateur: 1.
6. **Squad registration and lineups.** Squad registration and the starting five follow. A fictional lineup is the best
   assignment over each role's top three candidates. A real team's five is the verified five (REAL_TEAM_ROSTER_VALIDATION).
7. **Overlap.** 43 academy players are also registered with their senior squad: 15% chance, at most 2 per team. They stay
   employed by the academy.
8. **Backstories.** Every NPC gets a fictional history consistent with their age and state (§5).
9. **Validation.** The invariants of §6.

## 3. Population census at creation (FACT)

The census is mutually exclusive: each NPC is counted once, by authoritative employment, and by lineup for the
professional layer.

| Category | NPC Count | % |
|---|---:|---:|
| Fictional professional starters | 300 | 7.50% |
| Fictional professional reserves | 178 | 4.45% |
| Semi-professional players | 710 | 17.75% |
| Amateur players | 2,003 | 50.08% |
| Academy/development players | 366 | 9.15% |
| Real-team NPC reserves | 123 | 3.08% |
| Free agents | 320 | 8.00% |
| **Total** | **4,000** | **100%** |

**Overlapping affiliations (not added to the census):**

| Overlap | Count |
|---|---:|
| Academy players also registered with the senior squad | 43 |
| Of them, in the senior starting five at creation | 0 |
| NPCs employed by one team and registered as a starter elsewhere | 0 (impossible by construction; invariant) |

**Semi-professional detail:** 500 starters (100 teams × 5) and 210 reserves.

**Real people in the same world (counted separately, never in the NPC census):**
- 1,135 real players: 1,050 verified starters and 85 substitutes;
- 0 real free agents at creation. The sources cannot verify that a real player is unemployed, so none is assumed.
- Combined free-agent share (NPC + real): 320 / 5,135 = **6.23%**.

## 4. Census over 10 simulated seasons (baseline, seed 20261015)

| Season | Fictional pro starters | Fictional pro reserves | Semi-pro | Amateur | Academy | Real-team NPC reserves | Free agents | Total active NPCs | NPCs retired so far |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 300 | 178 | 710 | 2,003 | 366 | 123 | 320 | 4,000 | 0 |
| 1 | 300 | 191 | 719 | 2,036 | 370 | 115 | 269 | 4,000 | 496 |
| 2 | 300 | 185 | 716 | 1,965 | 371 | 116 | 347 | 4,000 | 969 |
| 3 | 300 | 181 | 714 | 2,008 | 371 | 115 | 311 | 4,000 | 1,491 |
| 4 | 300 | 181 | 717 | 2,011 | 372 | 121 | 298 | 4,000 | 1,953 |
| 5 | 300 | 183 | 724 | 2,024 | 370 | 121 | 278 | 4,000 | 2,413 |
| 6 | 300 | 186 | 721 | 2,028 | 369 | 123 | 273 | 4,000 | 2,831 |
| 7 | 300 | 184 | 721 | 2,021 | 370 | 123 | 281 | 4,000 | 3,223 |
| 8 | 300 | 182 | 724 | 2,009 | 371 | 123 | 291 | 4,000 | 3,637 |
| 9 | 300 | 184 | 718 | 2,048 | 372 | 123 | 255 | 4,000 | 4,031 |
| 10 | 300 | 184 | 726 | 2,046 | 370 | 123 | 251 | 4,000 | 4,435 |

**Why the active population stays at exactly 4,000.** The replacement intake adds one new prospect for each NPC who
retires in the season (bounded 300–600). It is a demographic rule, not a market control, and it is a decision (§8).
With a fixed intake of 480 the population grows. See FREE_AGENT_MARKET_VALIDATION §5.

## 5. Career history

**Events.** A minimal model of `CareerEvent` records:
- the fields: id, person, type, source team, destination team, season, reason, contract reference, era;
- the types: `GeneratedProspect`, `FirstAmateurContract`, `FirstProfessionalContract`, `AcademyRecruitment`, `Signed`,
  `Transfer`, `ContractExtension`, `ContractExpired`, `Released`, `Dismissed`, `VoluntaryDeparture`, `TeamDissolved`,
  `SeniorPromotion`, `AcademyNotRetained`, `Retired`, `RealRegistrationObserved`, `SnapshotEmployment`.

**Eras.** `PreWorld` is a generated NPC's fictional backstory. `Snapshot` is the state at the as-of day: real
observations, and employment at creation. `Simulation` is what simulated seasons did.

**Backstories** (PreWorld; deterministic; consistent with age and state):
- **An employed NPC** gets a debut age of 16–19 and seasons of experience. Their first contract is with the current
  team, or with an earlier existing fictional team followed by a transfer. Academy players get an academy recruitment.
  "First professional contract" is used only when the earlier team was not professional.
- **A free agent** either never signed (most likely for the youngest: 90% at ≤ 17, 4% from 24), or has a last team,
  chosen by level, and an exit.
  - The exit is drawn from the person's own plausibility: released more often when weak for that level, a voluntary
    departure more often when ambitious, a dismissal only with professionalism under 35.
  - It is never chosen to fill a category quota, and nobody who never signed has an exit.

**Integrity (FACT).**
- 2.8 events per NPC at creation; every NPC has a history.
- After 10 seasons there are 67,928 events. A **replay of every person's history** reproduces their current team and
  status, with 0 violations (test M05). A signing comes only from the market, a move or exit only from the team the
  person is with, and nothing follows a retirement.
- **History is never overwritten.** The snapshot-era hash is unchanged after every season (M11).

## 6. Integrity checks (FACT; at creation and after each of 10 seasons)

| Invariant | Result |
|---|---|
| Exactly one authoritative state per person: employed (one active contract, one squad), free agent or retired | 0 failures |
| No person in two squads; no active contract without its employee | 0 |
| Registered players per team ≤ 16 (squad + academy players on the senior list) | 0 over 16. Max registered at creation 11 (fictional pro), after 10 seasons 10 |
| Every fictional team fields five distinct players covering all roles at creation | 0 failures. During the seasons, 0 fictional professional, semi-pro or academy teams had a vacant role. Amateur teams with a vacant role: 6–28 of 334 a season (most in season 2) |
| No NPC in a real team's starting five | 0 in 11 snapshots |
| Academy employment distinct from senior registration | 0 failures |
| NPC handles unique and not real | 0 failures |

## 7. Observations (INFERENCE)

- **The layers hold their shape.** Every layer keeps its size over 10 seasons (fictional pro reserves 178–191,
  semi-pro 710–726, academy 366–372), so the allocation and the yearly market reach a steady state.
- **Real-team NPC reserves** stay at 115–123. They are development seats and never start (STRICT rule).
- **Amateur teams are the elastic layer.** They take most of the intake and release the most players.
- **Gaps.**
  - **Senior registrations of academy players** are made at creation only. After the first season none remain, because
    the simulation does not re-register academy players (T05: 0 at season 10). A gap of the experiment.
  - **Amateur contracts run one season.** 26,226 contract extensions were recorded in 10 seasons, 16,975 of them
    amateur. History grows by about 2.3 MB a season, and about a third of the growth is these amateur extensions
    (HYBRID_WORLD_INTEGRATION_REVIEW §3).

## 8. Decisions needed

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| E1 | The fictional world's size and layers | (a) as configured: 60 pro, 60 academy, 100 semi-pro, ~334 amateur teams; (b) fewer, deeper layers | (a) for the experiment. The player-facing scope is a separate decision (HYBRID_WORLD_INTEGRATION_REVIEW D-H1) |
| E2 | Intake rule | (a) replacement intake (one prospect per retirement, bounded); (b) a fixed yearly intake; (c) intake tied to market signals | (a): it holds the population at 4,000 without touching the free-agent ratio directly |
| E3 | Amateur contracts | (a) one-season contracts as events (now); (b) rolling amateur registrations without a contract event each year | (b): it cuts history growth by about a third |
| E4 | Academy players on the senior list | (a) only at creation (now); (b) a yearly re-registration rule; (c) no overlap at all | (b), if overlap is wanted in play |
