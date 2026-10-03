# HANDOFF — PHASE 4 TECHNICAL FOUNDATION

Status: ACTIVE HANDOFF
Target: 07 — Technical Foundation
Mode: PLAN / SPEC
Implementation authority: NOT YET AUTHORIZED

## Objective

Define the production technical foundation for the validated company-management simulation before any Vertical Slice build begins.

The first assignment is to evaluate engine/framework alternatives and propose a production architecture that preserves the validated simulation properties while supporting a management-heavy PC game.

## Source of Truth

Use, in order:

1. `docs/01-governance/DECISION_LOG.md`
2. `docs/00-project/PROJECT_STATE.md`
3. `docs/09-technical-foundation/README.md`
4. `prototypes/reports/PROJECT_DIRECTOR_HANDOFF.md`
5. `prototypes/reports/verification/VERIFICATION.md`
6. `docs/08-minimal-prototype/*`
7. `docs/07-specialist-specs/*`
8. `docs/06-core-loop-simulation/*`

If sources conflict, stop and report the conflict instead of silently selecting one.

## Current Product Requirements

The production foundation must support:

- PC deployment;
- management-heavy UI with dense tables, dashboards, forms, filters, comparisons and contextual detail;
- deterministic or reproducible simulation testing where practical;
- explicit Company State / World State ownership;
- stored-vs-derived state discipline;
- cross-system outcome boundaries;
- time progression with meaningful checkpoints rather than daily micromanagement;
- data-driven content expected to grow;
- save/load and versioning;
- debugging and causal observability;
- automated tests and headless simulation execution where practical;
- fast design iteration and AI-assisted coding;
- long-term maintainability;
- bounded rival simulation;
- future expansion without implementing future scope now.

## Engine / Framework Evaluation

Compare credible production candidates rather than selecting by familiarity.

At minimum evaluate:

- Godot;
- Unity;
- Unreal Engine.

Additional candidates may be included only if they are realistically competitive for this game's requirements.

For each candidate evaluate:

- desktop / PC deployment;
- management UI capability;
- large data-table and form workflows;
- UI iteration speed;
- simulation separation from presentation;
- headless / automated testing;
- deterministic simulation support;
- serialization and save/load;
- data-driven workflow;
- debugging / profiling;
- editor tooling;
- AI-assisted coding workflow;
- C# / C++ / GDScript or other relevant language implications;
- plugin / ecosystem maturity;
- build complexity;
- maintainability for a small AI-assisted team;
- performance fit for this game;
- licensing / commercial constraints;
- future modding implications;
- migration / lock-in cost.

Use current official documentation and current licensing information for time-sensitive claims.

Do not score or select an engine until the evidence and trade-offs are explicit.

## Production Architecture Evaluation

Propose a high-level architecture, not implementation code.

At minimum define boundaries for:

- Presentation / UI;
- Application / game-flow orchestration;
- Simulation Domain;
- Data / Content;
- Persistence;
- Testing / Debugging / Tooling.

Clarify:

- authoritative state ownership;
- derived state;
- commands / decisions;
- domain outcomes / events;
- time advancement;
- deterministic random streams;
- save boundaries;
- content definitions versus runtime state;
- how UI reads and changes simulation state;
- how delegated AI / staff decisions interact with the same simulation model;
- how rival simulation remains lower fidelity;
- how simulation can be tested without rendering the full game.

Do not copy the Python prototype structure mechanically.

## Prototype Disposition

Explicitly decide, as a proposal, which prototype assets should be:

- retained as behavioral reference;
- retained as test oracle;
- ported conceptually;
- rewritten;
- archived.

Prototype Python code and calibration are not production canon.

## Save / Load Foundation

Specify only the foundation needed before Vertical Slice:

- authoritative persisted state;
- IDs / references;
- schema or save version;
- migration strategy;
- deterministic seed/state requirements;
- derived-state rebuild rules;
- validation / corruption handling;
- separation between static content data and runtime save state.

Do not build a full production migration system yet.

## Data-Driven Foundation

Define a bounded initial strategy for growing content such as:

- players;
- staff;
- organizations;
- competitions;
- sponsors;
- contracts;
- traits;
- balance values.

Do not design one universal schema for all future systems.

## UI Technical Foundation

Focus on technical feasibility, not final visual art direction.

Address:

- screen / panel composition;
- reusable management controls;
- tables / lists / sorting / filtering;
- data binding / view models or equivalent;
- navigation;
- resolution / scaling;
- keyboard and mouse workflow;
- responsiveness for dense data;
- separation of presentation from simulation state.

## Required Deliverables

Produce proposals, not implementation, for:

1. Engine / Framework Evaluation
2. Engine / Framework ADR proposal
3. Production Architecture proposal
4. State Ownership and Simulation Boundary specification
5. Persistence / Save-Load foundation
6. Data-driven content foundation
7. Testing / Determinism / Debugging strategy
8. UI technical foundation
9. Repository / build / tooling proposal
10. Prototype migration / disposition plan
11. Risk register update
12. Phase 4 open questions

## Required Decision Labels

Clearly distinguish:

- FACT
- DECISION
- ASSUMPTION
- PROPOSAL
- OPEN QUESTION

Do not present a proposal as an accepted project decision.

## Scope Guard

Do NOT:

- build the Vertical Slice;
- implement production gameplay systems;
- create final UI;
- broaden gameplay scope;
- add multiple disciplines, teams or industries;
- finalize game balance;
- port the prototype wholesale;
- select technology without comparison;
- invent engine APIs without verification.

## Exit Criteria for This Assignment

The assignment is ready for Project Director review when:

- the engine/framework comparison is evidence-based;
- a preferred candidate is proposed with explicit trade-offs and reversal cost;
- architecture boundaries are understandable and testable;
- state ownership remains explicit;
- save/load and data foundations are bounded;
- testing and headless verification remain practical;
- UI feasibility for a management-heavy game is addressed;
- prototype migration is explicitly handled;
- unresolved decisions are listed;
- no production implementation has begun.

## Final Handoff

Return to Project Director with:

- Outcome
- Facts verified
- Proposals
- Recommended engine/framework candidate
- Architecture summary
- Prototype disposition
- Risks
- Open questions
- Decisions requiring Director approval
- Recommended next action

Do not proceed to BUILD until Project Director approves the Phase 4 specification.
