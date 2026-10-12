# Player Data Foundation v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

## Purpose and authority

Specify an auditable path from public observations or fictional generation to a shared player capability contract. This is a PLAN → SPEC → REVIEW deliverable, not BUILD authorization. **LoL is a working design candidate, not the approved production discipline.** All thresholds, distributions, weights and new field shapes below are proposals.

Labels used throughout:

- **FACT:** observed repository behavior or attributed external documentation.
- **DECISION:** an existing accepted decision, with its source; this package accepts none.
- **ASSUMPTION:** a bounded condition needed to explore the design.
- **PROPOSAL:** a reviewable change or candidate parameter, not production balance.
- **OPEN QUESTION:** an unresolved dependency; see the registers below.

**DECISION — inherited:** company-first management, one initial discipline/primary team, Company/World ownership, engine-independent deterministic resolution, actor-safe observations and versioned persistence remain binding. Authority is [Decision Log](../../../01-governance/DECISION_LOG.md), especially DEC-022/023, read with [Project State](../../../00-project/PROJECT_STATE.md) and [Game Vision](../../../00-project/GAME_VISION.md). Later accepted decisions override historical specialist proposals. DEC-022's foundation closure retains deferred validation obligations; it does not accept every proposal in every foundation document.

## Scope and document map

| Document | Responsibility | Status |
|---|---|---|
| [Data contract](PLAYER_DATA_CONTRACT_V1.md) | Identities, dictionaries, ownership, lifecycle and example envelope | PROPOSED — NOT APPROVED |
| [Attribute catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md) | Four core candidates, four deferred candidates, twenty fields each | PROPOSED — NOT APPROVED |
| [Rating formulas](PLAYER_RATING_FORMULAS_V1.md) | Feature definitions, context, uncertainty and worked arithmetic | PROPOSED — NOT APPROVED |
| [NPC generation](NPC_GENERATION_V1.md) | Conditional population model, existing RNG, knowledge boundary | PROPOSED — NOT APPROVED |
| [Validation and calibration](VALIDATION_CALIBRATION_V1.md) | Planned tests, migration assessment, design review and actual document checks | PROPOSED — NOT APPROVED |
| [Real-path example](examples/real_player.example.json) | Invented identity and synthetic summary; no real person's rating | ILLUSTRATIVE ONLY |
| [NPC example](examples/npc_player.example.json) | Hand-worked fictional definition using the same internal fields | ILLUSTRATIVE ONLY |

Non-goals: importer, live API, player database acquisition, production rights clearance, new match/training/scouting mechanics, UI, a generator implementation, multi-esport abstraction, accepted decision edits, runtime/schema/balance changes or commit/push in this specification task. No photographs, logos or real player ratings are supplied.

## Repository source index and current state

**FACT — inspected baseline:** branch stage5-sponsor-instruments, HEAD 14b9f48134264d8c04f3dfb268b93ee295b825f8, plus pre-existing uncommitted UI/Application work on 2026-10-08. This is an inspection baseline, not a clean-tree claim. Application changes include brand references, expired-employment subject lookup, horizon projection and transfer presentation. They do not add a new Domain player model.

| Source | Finding and authority |
|---|---|
| [Specialist index](../../../07-specialist-specs/README.md), [competition](../../../07-specialist-specs/ESPORTS_COMPETITION_SPEC.md), [economy](../../../07-specialist-specs/SIMULATION_ECONOMY_SPEC.md), [Director integration](../../../07-specialist-specs/DIRECTOR_INTEGRATION.md) | Detailed proposals and prototype integration; preserve information uncertainty and competition outcomes rather than direct finance mutations. Do not treat unfinished historical sections as completed specifications. |
| [Data-driven content](../../../09-technical-foundation/DATA_DRIVEN_CONTENT.md) | Separate authored content from mutable campaign state; changes require version and validation policy. |
| [Ownership boundary](../../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md) | Company owns its people/contracts/readiness; World owns external market/rivals. Competition consumes inputs and emits outcomes. |
| [Persistence](../../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md) | Explicit save contracts and compatibility checks; historical schema examples do not override current code. |
| [Model.cs](../../../../src/ManagementGame.Domain/Model.cs) | Person has Id, DefinitionId, Name, Role, Execution, Adaptability, Consistency, Readiness. No birth, public identity, champion, measured statistics or potential. Company.People is the owned roster; there is no independent authoritative Team roster. |
| [Competition.cs](../../../../src/ManagementGame.Domain/Competition.cs) | Uses readiness-weighted Execution, Adaptability and Consistency with preparation/posture/rivals. No lane/combat/vision phase or champion draft. New ratings cannot be substituted automatically. |
| [Seasons.cs](../../../../src/ManagementGame.Domain/Seasons.cs), [factory](../../../../src/ManagementGame.Domain/CampaignFactory.cs), [determinism](../../../../src/ManagementGame.Domain/Determinism.cs) | Roles A–E; generated people share template DefinitionId player:generated; stable entity IDs; keyed rng-v1. Salary depends on Execution squared, so a capability adapter also affects economy. |
| [ContentLoader](../../../../src/ManagementGame.Infrastructure/ContentLoader.cs), [fixture](../../../../content/fixture.json) | Strict schema 3, slice-rules-v3, NONCANONICAL fictional Role Arena content; ranges/name pools are not a real-player catalog or final balance. Unknown properties and content hash mismatch fail. |
| [Snapshot DTOs](../../../../src/ManagementGame.Infrastructure/SnapshotDtos.cs), [store](../../../../src/ManagementGame.Infrastructure/SnapshotStore.cs), [validation](../../../../src/ManagementGame.Infrastructure/SnapshotValidation.cs) | Save schema 2 explicitly mirrors Person and checks content/rules/RNG/canonical versions. This package supplies no migration. |
| [Observations and CoachPolicy](../../../../src/ManagementGame.Application/Observations.cs), [contracts](../../../../src/ManagementGame.Application/Contracts.cs) | Owned roster exposes Execution/readiness; market ability is a band. UI and delegated decisions consume approved observations rather than unrestricted truth. |

**PROPOSAL:** keep the ownership and deterministic boundaries, but introduce reviewed definition identity, an offline measurement artifact and a role-aware capability vector in a later bounded task. Neither JSON example is loadable by ContentLoader or SnapshotStore. There is no approved A–E ↔ LoL role mapping, and no lossless mapping to Execution or legacy Adaptability. Detailed compatibility and migration dependencies are in [validation](VALIDATION_CALIBRATION_V1.md).

## External research index

Documentation accessed 2026-10-08; no authenticated API requests or dataset imports were made. These sources establish candidate availability and design limitations, not commercial permission or validation of our coefficients.

| Source | FACT from source | Design consequence / remaining unknown |
|---|---|---|
| [Riot LoL developer documentation](https://developer.riotgames.com/docs/lol) | Riot ID display names can change; PUUID-based account APIs exist. Data Dragon supplies versioned static content and may lag patch releases. Policy restricts alternative official skill-ranking products. | Keep account identity distinct from a human; pin champion/patch reference versions. Do not infer professional coverage or permitted game-data redistribution. Intended simulation use requires a specific rights/policy review. |
| [PandaScore plan reference](https://developers.pandascore.co/docs/plan-reference), [match player statistics](https://developers.pandascore.co/ws/reference/get_lol_matches_matchidorslug_players_stats) | Identity/context, historical statistics and live frames/events have different access requirements. Match player statistics have a documented endpoint. | Obtain the applicable per-game schema and metric definitions before mapping fields. A match aggregate is not a player-game row. Coverage, commercial use and redistribution remain unverified. |
| [GRID LoL access](https://grid.gg/get-league-of-legends/), [LoL data description](https://grid.gg/data/games/league-of-legends/) | GRID describes official LoL data and different access routes. | Rich telemetry is a candidate, not a verified schema or a blanket open license. Snapshot timing, circuit coverage and terms must be checked against the actual agreement. |
| [PandaSkill, authors' paper v2](https://arxiv.org/abs/2501.10049v2) | Proposes role-specific performance estimation and addresses disconnected regional rating pools. | Supports investigating separate roles and cross-region calibration. Our transparent candidate formulas are independently proposed; the paper does not validate their weights, causal interpretation or game balance. |

Oracle's Elixir was considered, but its downloads/definitions pages could not be retrieved during this review. No field availability or license claim from that provider is assumed. Provider catalogs, team win rates, highlights and fan rankings do not establish mechanics, intelligence, personality or potential.

## PROPOSAL REGISTER

| ID | Proposal | Gate / dependency |
|---|---|---|
| PD-P01 | LoL-specific candidate contract; shared real/NPC runtime input | PD-D01 discipline decision |
| PD-P02 | Core candidates: Laning, Combat, Vision, Consistency; four deferred | PD-D02 gameplay consumers and ablation |
| PD-P03 | Role/context-standardized, series-weighted, shrunk ratings | PD-D03 licensed data, benchmark and model calibration |
| PD-P04 | Correlated NPC cohort generation on existing rng-v1 | PD-D04 bounded population and recruitment lifecycle |
| PD-P05 | Frozen campaign initialization; separately versioned measurements | PD-D05 ownership, save migration and observation contract |

## OPEN QUESTIONS

| ID | Question | Consequence until answered |
|---|---|---|
| PD-Q01 | Which circuits, seasons and provider fields are actually licensed and complete? | No production ingestion or measured player definitions. |
| PD-Q02 | Can cross-tier/region comparisons be anchored with enough connected competition? | Ratings retain within-context comparability only. |
| PD-Q03 | Which bounded decision uses Vision independently of Combat? | Keep it a data-design candidate; omit from runtime if no useful consumer is approved. |
| PD-Q04 | Does a phase-aware capability consumer create value beyond current Execution? | No proposed vector-to-Execution adapter is silently installed. |
| PD-Q05 | How will role changes, development, decline and retiring NPCs work? | Reserve runtime fields; no automatic growth/decline or replenishment mechanic. |
| PD-Q06 | How much player knowledge may owned-roster and staff views reveal? | Existing information policy remains; new hidden fields are never exposed by default. |

## REQUIRED DIRECTOR DECISIONS

| Request | Concrete decision needed before dependent BUILD |
|---|---|
| PD-D01 | Accept/reject LoL as production discipline, or keep this an isolated design exercise. |
| PD-D02 | Select capability set and bounded competitive/management consumers; decide legacy Execution/Adaptability treatment and role mapping. |
| PD-D03 | Approve data source/use rights, supported coverage, benchmark population, uncertainty display and calibration acceptance criteria. |
| PD-D04 | Approve NPC cohort size/role/rarity budgets and lifecycle scope; approve or defer potential and career mechanics. |
| PD-D05 | Approve new definition/runtime DTOs, version boundaries, old-save disposition and actor-safe projections. |

These are requests, not DEC-XXX entries or Accepted decisions. This specification task itself needs no additional approval to finish.

## IMPLEMENTATION BLOCKERS

1. Discipline and simulation consumer semantics are undecided (D01/D02).
2. No licensed, frozen, validated measurement dataset or cross-context calibration exists (D03).
3. Current Person, A–E eligibility, strict content and save contracts cannot load these examples (D05).
4. Generator configuration, golden vectors and population calibration are proposed, not executed (D04).
5. Existing deferred foundation obligations, including cross-machine determinism, long-campaign scaling and migration evidence, remain open at their applicable gates.

## Handoff and version history

Future implementers: read this entry point and the contract first, then formulas/catalog, NPC and validation. Resolve the relevant Director requests; pin dataset/model/generator/rules versions; implement one reviewed boundary at a time. Keep unknown evidence explicit. Never copy illustrative values into production balance or assign them to a real person. Do not load these envelopes as development content, overwrite a running campaign from refreshed public data, or change RNG to make an example fit.

| Version | Date | Change |
|---|---|---|
| v0.1 | 2026-10-08 | Initial specification, two synthetic examples, current-code assessment and document review; no gameplay changes. |
