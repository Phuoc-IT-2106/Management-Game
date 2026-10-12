# HANDOFF — PROTOTYPE BUILD & VERIFY

Status: ACTIVE HANDOFF
Target: 06 — Prototype Build & Verification

## Objective
Build the smallest deterministic headless simulation that implements the approved Phase 3 specification and verify it against the accepted scenario/acceptance suite.

This is prototype engineering, not production engineering.

## Source of Truth
Use, in order:
1. `docs/01-governance/DECISION_LOG.md`
2. `docs/00-project/PROJECT_STATE.md`
3. `docs/08-minimal-prototype/MINIMAL_SIMULATION_PROTOTYPE_SPEC.md`
4. `docs/08-minimal-prototype/DIRECTOR_REVIEW.md`
5. `docs/07-specialist-specs/DIRECTOR_INTEGRATION.md`
6. specialist specifications and Phase 2 detailed documents.

Do not silently override a higher-authority source.

## Required Build
Implement only what is needed to exercise the approved scenarios:
- authoritative Company State and World State needed by the prototype;
- one-day simulation tick with checkpoint interruption;
- deterministic seed control;
- roster, Head Coach and simple contracts;
- Cash / receivables / obligations / one financing mechanism;
- Reputation and Audience/Fandom;
- Organizational Capacity / Load;
- domain-aware Information Quality;
- preparation allocation and bounded competitive resolution;
- Authority Envelope delegation;
- bounded rivals and strategy adaptation;
- sponsor opportunity/agreement flow;
- Stable → Warning → Distress → Restructuring → Stabilized/Terminal candidate flow;
- explicit cross-system outcome signals;
- causal/debug traces.

## Required Verification
Run controlled versions of scenarios A–M from the approved prototype specification and verify AC-01 through AC-13.

For every failed criterion:
1. record the evidence;
2. identify whether the failure is implementation, calibration, or design;
3. make the smallest coherent change;
4. rerun affected scenarios;
5. report regression risk.

## Scope Guard
Do not add production architecture, production UI, extra industries, extra esports disciplines, full tactical match simulation, detailed accounting, advanced debt markets, or deep relationship systems.

## Required Output
Return to Project Director with:
- implementation summary;
- executed scenario matrix;
- AC-01 → AC-13 PASS/FAIL evidence;
- deterministic reproducibility results;
- calibration changes;
- mechanics simplified/removed;
- unresolved design conflicts;
- regression risks;
- recommendation: ITERATE, PASS PROTOTYPE GATE, or STOP/REDESIGN.