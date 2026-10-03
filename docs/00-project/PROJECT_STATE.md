# PROJECT STATE

Version: 0.6
Phase: Phase 4 — Technical Foundation
Status: Prototype Gate Passed — Technical Foundation Authorized

## Current Objective

Define and approve the production technical foundation required to turn the validated management simulation into a maintainable PC game.

Phase 4 must determine:
- engine / framework choice;
- production architecture and module boundaries;
- simulation / presentation separation;
- authoritative state and persistence strategy;
- data-driven content format;
- testing and deterministic-debugging strategy;
- repository / tooling structure;
- UI technical approach for a management-heavy game.

Phase 4 does **not** authorize broad feature expansion or production Vertical Slice implementation before its own gate is closed.

## Strategic Center

The project is a **company-first PC management / business strategy simulation that begins with professional esports as its first production domain**.

The player primarily acts as a CEO / executive while remaining directly involved in high-value management decisions. Competitive success matters because it creates strategic and commercial leverage, while long-term company success remains the broader objective.

## Completed Phase Gates

### Phase 0 — Discovery
CLOSED.

### Phase 1 — Product Foundation
CLOSED for current production planning.

### Phase 2 — Core Loop & Simulation
CLOSED.

### Phase 3 — Headless / Minimal Prototype
CLOSED — **PASS PROTOTYPE GATE**.

The approved deterministic prototype implemented the bounded simulation and verified:
- scenarios A–M: PASS;
- acceptance criteria AC-01 through AC-13: PASS;
- 32/32 automated regression tests: PASS;
- 1,664/1,664 repeated seeded scenario pairs with matching final-state hashes;
- 13/13 representative full-trace replays matched;
- 128/128 manual/delegated paired runs preserved gameplay parity while delegation reduced approval burden.

The evidence is stored under `prototypes/reports/`.

Phase 3 validates the bounded management-simulation thesis. It does **not** validate final balance, final content scope, production architecture, or a production esports discipline.

## Accepted Decisions

Canonical accepted decisions are recorded in `docs/01-governance/DECISION_LOG.md`.

Current accepted range:
- DEC-001 through DEC-021.

DEC-021 closes Phase 3 and authorizes Phase 4.

## Prototype Findings Carried Forward

Treat these as validated design evidence:
- context can change the preferred management choice;
- competition and business consequences can interact through explicit outcome boundaries;
- Commercial Value should create opportunities, not direct Cash;
- Information Quality can improve estimates without buffing true asset strength;
- delegation can reduce management burden without hidden performance bonuses;
- rival adaptation and meta state can change preparation choices;
- Organizational Capacity / Load can affect process conversion without becoming generic action points;
- financial commitments and debt can constrain future flexibility;
- distress can support costly recovery rather than only instant failure;
- deterministic seeded simulation and causal traces materially improve verification.

## Prototype Elements That Are NOT Production Canon

Do not silently carry the following forward as final design:
- Python prototype implementation;
- 84-day horizon;
- exact four-rival count;
- fictional 5v5 Role Arena structure;
- exact player / coach ratings;
- Currency Unit values;
- coefficients, thresholds, probability clamps, overload curves and debt costs;
- exact number of matches;
- current sponsor / talent-market calibration;
- current terminal-state thresholds;
- current policy scripts.

These remain disposable validation artifacts unless separately approved.

## Known Prototype Limitations To Respect In Phase 4

- end-date viability is not the same as long-term solvency;
- strong-start / snowball balance is not proven beyond the bounded horizon;
- talent-market pressure is only lightly validated;
- only opponent-information value is strongly demonstrated;
- several coach / information fields remain inactive or weakly justified;
- K/L validate one principal restructuring path more strongly than the full recovery space;
- rival world simulation is intentionally lower fidelity;
- prototype traces are verification instrumentation, not a final production event-sourcing design.

These limitations are inputs to Technical Foundation and later design work, not blockers to starting Phase 4.

## Phase 4 Scope

### Required
- compare engine / framework candidates against this game's requirements;
- select production technology through an explicit ADR;
- define high-level architecture;
- preserve explicit authoritative state ownership;
- preserve deterministic / headless simulation testing where practical;
- define data-driven content boundaries;
- define save/load and versioning approach at foundation level;
- define testing, debugging and simulation tooling;
- define UI technology / composition strategy appropriate for dense management interfaces;
- identify which prototype concepts should be retained, redesigned, or discarded before production.

### Not Authorized Yet
- production Vertical Slice feature build;
- final UI art direction;
- large content production;
- multiple esports disciplines;
- multiple player-controlled teams;
- additional industries;
- deep facilities / relationships / merchandising / M&A;
- final balance;
- full production save migration implementation;
- broad optimization work.

## Phase 4 Decision Rules

For each major technical choice, record:
- problem;
- requirements;
- alternatives;
- trade-offs;
- decision;
- consequences;
- migration / reversal cost;
- affected modules.

Do not select technology because it is fashionable or familiar alone.

Technical choices must be evaluated against:
- PC deployment;
- management-heavy UI;
- simulation and deterministic testing;
- data volume;
- iteration speed;
- AI-assisted coding workflow;
- debugging / observability;
- save/load;
- maintainability;
- performance;
- future content growth;
- possible modding without implementing modding now.

## Phase 4 Exit Gate

Do not begin the production Vertical Slice until Project Director accepts:
- engine / framework ADR;
- production architecture boundaries;
- simulation ownership model;
- persistence / save-load foundation;
- data format strategy;
- testing / deterministic-debugging strategy;
- UI technical foundation;
- repository / build structure;
- migration disposition for the Phase 3 prototype.

## Current Priority

Proceed with **Phase 4 — Technical Foundation PLAN / SPEC**.

First task:
evaluate production engine / framework and architecture alternatives against the validated game requirements.

Do not begin Vertical Slice implementation yet.
