# PHASE 4 — TECHNICAL FOUNDATION

Status: **AUTHORIZED — PLAN / SPEC**

Authority:
- `docs/01-governance/DECISION_LOG.md` through DEC-021
- `docs/00-project/PROJECT_STATE.md` v0.6

## Objective

Choose and specify the smallest maintainable production foundation needed to build the Vertical Slice without losing the validated simulation properties from Phase 3.

## Required decisions

Phase 4 must produce explicit, reviewable decisions for:

1. engine / framework selection;
2. production architecture and module boundaries;
3. simulation / presentation separation;
4. authoritative state ownership;
5. persistence / save-load foundation and versioning;
6. data-driven content format and validation;
7. deterministic testing / debugging strategy;
8. UI technical approach for management-heavy screens;
9. repository / build / tooling structure;
10. disposition of the Phase 3 Python prototype: retain as reference/test oracle, partially port, or retire.

## Evaluation criteria

Compare alternatives against the actual project needs:
- PC deployment;
- dense management UI;
- simulation and time progression;
- data volume and iteration speed;
- AI-assisted coding workflow;
- automated testing and deterministic debugging;
- save/load;
- maintainability;
- performance;
- content growth;
- possible future modding without implementing modding now.

## Scope guard

Do not use Phase 4 to add gameplay breadth.

Not authorized yet:
- production Vertical Slice feature implementation;
- additional disciplines / teams / industries;
- large staff or relationship systems;
- facilities;
- media / merchandising / M&A;
- final balance;
- large content production.

## Prototype carry-forward rule

Prototype evidence informs production design, but prototype code and calibration are not canonical production architecture.

Preserve validated properties where practical:
- explicit state ownership;
- stored-vs-derived discipline;
- cross-system outcome boundaries;
- deterministic seeded testing;
- causal observability;
- manual / delegated parity;
- lower-fidelity bounded rivals.

Any intentional departure should be documented with its rationale and regression risk.

## Phase 4 exit gate

Project Director must approve:
- engine/framework ADR;
- architecture ADR;
- persistence approach;
- data format strategy;
- testing/debugging strategy;
- UI technical foundation;
- repository/tooling structure;
- prototype migration/disposition plan.

Only then may Phase 5 — Vertical Slice BUILD begin.
