# HANDOFF TO PROJECT DIRECTOR — PHASE 2 CORE LOOP & SIMULATION

Status: Phase 2 specialist handoff

## Outcome

A concrete, implementation-independent simulation model has been drafted for the approved Product Foundation.

Created:

- `TIME_MODEL.md`
- `RESOURCE_MODEL.md`
- `COMPANY_STATE.md`
- `WORLD_STATE.md`
- `SIMULATION_CYCLE.md`
- `CONSEQUENCE_MAP.md`
- `DELEGATION_MODEL.md`
- `RIVAL_SIMULATION.md`
- `FAILURE_RECOVERY_MODEL.md`
- `PHASE2_OPEN_QUESTIONS.md`

Targeted clarification proposals:

- `CORE_LOOP_UPDATE_PROPOSED.md`
- `SYSTEM_LANDSCAPE_UPDATE_PROPOSED.md`

No engine, software architecture, production code, additional industry, or broader vertical-slice feature was selected.

## Decisions Confirmed

**DECISION — inherited from approved Product Foundation**

- player primarily acts as CEO / executive;
- executive + management are the main control levels;
- operational micromanagement is secondary;
- delegation is important;
- competitive performance creates business leverage;
- simulation is medium depth by default with selective deeper systems;
- rival organizations evolve and compete in the same ecosystem;
- structural failure is real;
- recovery/restructuring normally precedes terminal game over;
- the first vertical slice remains narrow;
- current management loop remains `Observe → Prioritize → Decide → Commit → Delegate / Intervene → Advance Time → Resolve → Review → Adapt`.

## Proposals

**PROPOSAL**

1. **Time:** one calendar day is the authoritative Simulation Tick; player-facing advancement is checkpoint-driven.
2. **Core resources:** cash, organizational capacity, reputation, audience/fandom, and domain-specific information quality.
3. **Derived values:** commercial value, competitive capability, strategic flexibility, runway, and company value are derived rather than independent spendable pools.
4. **Company State:** minimum authoritative state covers finance, resources, roster, key staff, contracts, preparation, sponsors, delegation, commitments, and recovery status.
5. **World State:** minimum state covers calendar/competition, rivals, limited talent market, limited sponsor market, and optionally a lightweight meta/market signal.
6. **Delegation:** use Authority Envelopes with scope, constraints, budget/risk limits, escalation triggers, and review points.
7. **Rivals:** compressed simulation with shared ecosystem scarcity; at least four evolving rivals, with five preferred for the prototype.
8. **Failure:** Stable → Warning → Distress → Restructuring → Stabilized or Terminal Insolvency.
9. **Consequences:** classify as immediate, short-delay, medium-delay, or long-delay; major changes must be causally traceable.
10. **Core cycle:** convert decisions into authoritative state changes / future commitments, advance through ticks, resolve world/company interactions, stop only at material checkpoints.

## Dependencies

**FACT**

Before a headless prototype specification can be complete, specialist input is required for:

- esports competition calendar and match-resolution contract;
- exact mandatory manual competitive decisions;
- first-slice staff-role set;
- financial cadence and balancing thresholds;
- company-value abstraction.

**FACT**

The currently supplied `PROJECT_STATE.md` and `DECISION_LOG.md` still reflect an earlier Discovery state, while the Phase 1 handoff and current Director instruction state that Product Foundation is approved and Phase 2 is active. Project Director should synchronize canonical status/decision records without rewriting the Phase 2 proposals as accepted decisions automatically.

## Open Questions

**OPEN QUESTION**

Highest priority:

1. exact competition format/calendar;
2. match-resolution inputs/outputs;
3. mandatory manual competitive decisions;
4. exact first-slice staff roles;
5. payroll/sponsor/debt cadence;
6. distress and grace thresholds;
7. company-value abstraction;
8. audience and organizational-capacity granularity.

See `PHASE2_OPEN_QUESTIONS.md` for the complete register.

## Risks

**FACT**

- **Resource duplication:** reputation, audience, commercial value, and company value can become redundant if not kept distinct.
- **Capacity becoming a generic mana bar:** organizational capacity must represent real workload/coordination trade-offs rather than an arbitrary limiter.
- **Rival false symmetry:** fully simulating every rival like the player would create unnecessary scope; too little simulation would make rivals decorative.
- **Positive feedback runaway:** competition → reputation → money → stronger roster can become self-reinforcing without rising costs, expectations, rival pressure, and diminishing returns.
- **Failure death spiral:** distress needs credible recovery counterweights, not only compounding penalties.
- **Checkpoint overload:** too many interruptions would recreate operational micromanagement.
- **Untraceable delay:** delayed effects must retain enough causal context for Review to explain outcomes.

## Recommended Next Action

**PROPOSAL**

Project Director should:

1. approve/revise the Phase 2 proposals listed above;
2. synchronize stale canonical phase/decision documents;
3. send `SIMULATION_CYCLE.md`, `TIME_MODEL.md`, `WORLD_STATE.md`, and `RIVAL_SIMULATION.md` to the Esports Competition specialist to specify the minimum competition-resolution contract;
4. send `RESOURCE_MODEL.md` and `FAILURE_RECOVERY_MODEL.md` to the Simulation & Economy specialist for numeric/economic specification;
5. integrate those specialist results back into a **Minimal Simulation Prototype Specification**;
6. only after that specification passes review, proceed toward Technical Architecture.

Do not begin engine selection or production implementation at this handoff.
