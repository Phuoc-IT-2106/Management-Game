# HIGH-LEVEL ROADMAP

Current phase: **P5 — Vertical Slice**

Completed gates: P0 ✅ · P1 ✅ · P2 ✅ · P3 ✅ · P4 ✅ (with deferred validation obligations)

Authority: [DEC-022](../01-governance/DECISION_LOG.md#dec-022--production-technical-foundation-accepted) closes Phase 4 before authorizing Phase 5 BUILD. [Closure and deferred gates](../09-technical-foundation/PHASE4_CLOSURE.md) record the accepted conditions; no Vertical Slice implementation was started by the closure task.

## P0 — Discovery
Goal:
Define the product, player role, strategic fantasy, constraints, and major risks.

Exit criteria:
- clear product thesis
- initial design pillars
- major open questions identified
- vertical slice scope roughly defined

## P1 — Product Foundation
Goal:
Define what the player does and why the game is interesting.

Outputs:
- player role
- target experience
- core promise
- scope boundaries
- what the game is / is not

## P2 — Core Loop & Simulation
Goal:
Define the management loop and main interacting systems.

Outputs:
- core gameplay loop
- time progression
- company state model
- resource model
- major system interactions

## P3 — Headless / Minimal Prototype
Goal:
Test whether the simulation produces interesting decisions without polished presentation.

Possible content:
- one company
- one esports division
- small roster
- staff
- budget
- contracts
- sponsor
- competition
- reputation

Exit criteria:
The prototype repeatedly creates meaningful decisions without relying on scripted novelty.

## P4 — Technical Foundation
Status: **CLOSED — PASS WITH DEFERRED VALIDATION OBLIGATIONS**.

Goal:
Choose engine / framework and technical architecture based on validated requirements.

Outputs:
- technical ADRs
- module boundaries
- save/load approach
- data format
- testing strategy
- repository structure

Accepted: Godot .NET + C#, ADR-TF-002 modular monolith, engine-independent Domain/Application, Company/World authority, outcome boundaries, deterministic/headless testing, bounded save compatibility and data-driven content. Godot remains revisable through later evidence; Unity remains fallback for disproportionate UI infrastructure cost or major technical failure.

Historical evidence remains QG-01 INCONCLUSIVE, QG-02 FAIL against 16.7 ms, QG-03 bounded PASS and QG-04 local NTFS PASS. DEC-022 accepts the explicit deferrals without rewriting evidence or requiring indefinite Phase 4 optimization.

## P5 — Vertical Slice
Status: **BUILD AUTHORIZED after Phase 4 closure**.

Goal:
Build the smallest production Vertical Slice proving the real management loop through production architecture: one complete playable management cycle.

Target:
- 1 company
- 1 esports discipline
- 1 primary team
- competition cycle
- staff / roster decisions
- finance
- sponsors
- progression
- save/load
- usable management UI

Engineering conditions:
- Use the initial Phase 5 development pin: Godot 4.7.2 stable mono / matching mono templates, SDK 10.0.401, runtime 10.0.12, `net10.0`. This is not a permanent release-support promise; upgrades require targeted requalification.
- Apply the [workflow-specific performance policy](../09-technical-foundation/PHASE4_CLOSURE.md#qg-02--historical-fail-phase-5-engineering-policy); it does not retrospectively pass QG-02.
- Keep Python as behavioral reference/test oracle only; do not port it wholesale.

Required future gates, owned and tracked in [DV-01–DV-10](../09-technical-foundation/PHASE4_CLOSURE.md#risks-carried-forward-and-deferred-validation-gates):
- Developer tooling: external debugger validation during Phase 5; address sooner if development becomes impractical.
- UI quality: physical keyboard/mouse, actual Windows DPI and mixed-monitor checks.
- Vertical Slice performance: approve target/minimum hardware and measurement contract, then assess production workflows; monitor UI regressions at affected changes.
- Determinism: second-machine transition-hash evidence before acceptance or cross-machine claims.
- Campaign scale: approve horizon/data/history envelope during Vertical Slice; validate long-campaign performance and retention before campaign-scale acceptance or broader expansion.
- Save compatibility: approve production supported-version/migration policy before first distribution; test promised migration edges before claiming support.
- Distribution: clean supported Windows execution **before external tester distribution, public demo, release candidate, or a self-contained clean-Windows claim**.
- Release: supported OS/runtime servicing review and relevant requalification on upgrades.

BUILD authorization does not discharge these gates or authorize broader scope. Evidence and Director disposition are required to close obligations.

## P6 — System Depth
Expand:
- scouting
- training
- staff
- facilities
- contracts
- sponsors
- analytics
- relationships
- brand systems

## P7 — Dynamic World
Add:
- competitor strategy
- market change
- meta change
- talent market
- events
- organizational consequences
- more emergent interactions

## P8 — Balance & UX
Focus:
- economy tuning
- dominant strategies
- feedback clarity
- information design
- automation / delegation
- pacing

## P9 — Company Expansion
Only after the core company model works.

Possible new divisions:
- additional esports teams
- traditional sports
- media
- advertising
- events
- merchandising
- talent management

New divisions should connect to the existing company simulation instead of becoming disconnected mini-games.

## P10 — Broader Production
Possible:
- larger world
- more content
- advanced AI competitors
- modding
- long-term replayability
- performance optimization
- accessibility
- localization
