# SIMULATION CYCLE

Status: Phase 2 — Core Loop & Simulation specialist proposal

## 1. Inherited Management Loop

**DECISION — inherited**

Do not replace the approved management loop:

> Observe → Prioritize → Decide → Commit → Delegate / Intervene → Advance Time → Resolve → Review → Adapt

Phase 2 converts this loop into testable state transitions.

## 2. Simulation-Cycle Contract

### Stage 1 — Observe

**PROPOSAL**

Input:

- current Company State;
- current World State;
- unresolved commitments;
- staff recommendations;
- material uncertainty.

Output:

- decision-relevant signals, not a dump of every state variable.

Minimum questions surfaced:

- What changed?
- What is due soon?
- What is at risk?
- What opportunity may expire?
- What does staff recommend?

### Stage 2 — Prioritize

**PROPOSAL**

The player chooses which active situations deserve executive attention.

Not every problem must be solved before time can advance unless it is a mandatory checkpoint.

### Stage 3 — Decide

**DECISION — inherited**

The player makes executive / management decisions such as roster, key staff, contracts, sponsors, budgets, risk posture, and high-level preparation.

**PROPOSAL**

A material decision should specify conceptually:

- target;
- selected option;
- committed resources;
- constraints / authority;
- effective timing;
- intended horizon.

### Stage 4 — Commit

**PROPOSAL**

Commit converts intention into authoritative state changes and/or future obligations.

Examples:

- cash reserved/spent;
- contract created;
- roster slot committed;
- staff workload allocated;
- sponsor restriction activated;
- debt payment scheduled;
- preparation focus locked for a window.

Downstream outcomes are not all resolved immediately.

### Stage 5 — Delegate / Intervene

**DECISION — inherited**

The player may accept, modify, constrain, override, or fully delegate appropriate operational decisions.

**PROPOSAL**

Delegation produces an **Authority Envelope** defining:

- responsible staff role;
- allowed decision domain;
- constraints;
- escalation conditions;
- review point.

### Stage 6 — Advance Time

**PROPOSAL**

Advance by daily Simulation Ticks until the next meaningful Decision Checkpoint.

For each elapsed tick, the model conceptually resolves:

1. scheduled obligations becoming due;
2. ongoing staff / delegated work;
3. roster and contract progression;
4. rival / market activity due on that date;
5. competition events due on that date;
6. cross-system consequences;
7. risk / failure thresholds;
8. checkpoint-generation conditions.

This ordering defines simulation semantics, not software module architecture.

### Stage 7 — Resolve

**PROPOSAL**

Material events produce immediate and scheduled consequences across Company State and World State.

Competition resolution should consume the current roster/staff/preparation/opponent context and produce an outcome plus performance evidence, without requiring a full tactical simulator.

### Stage 8 — Review

**DECISION — inherited**

The player should understand what changed, why it changed, which prior decisions contributed, what remains uncertain, and what new risks/opportunities appeared.

**PROPOSAL**

Review should distinguish:

- direct consequence;
- delayed consequence;
- external world change;
- staff/delegated action;
- unresolved uncertainty.

### Stage 9 — Adapt

**PROPOSAL**

The cycle closes when the new state produces a changed priority set. The player may alter strategy, resources, delegation, or risk posture before the next advance.

## 3. Decision Trigger Classes

**PROPOSAL**

A player-facing decision should normally be triggered by at least one of:

- deadline;
- threshold crossing;
- scarce opportunity;
- conflict between objectives;
- material new information;
- rival action;
- staff escalation;
- failure/recovery state change.

Routine state changes with no meaningful choice should resolve automatically.

## 4. Testable Cycle Scenarios

**PROPOSAL**

### Scenario A — Competitive investment

Given:
- adequate cash;
- upcoming important competition;
- star player available;
- expensive multi-period contract.

When:
- player signs the star.

Then:
- cash / commitments worsen immediately;
- roster capability can improve after joining/integration;
- reputation/audience may react with delay;
- future flexibility decreases;
- failure risk increases if revenue/results disappoint.

### Scenario B — Delegation

Given:
- coach has authority over preparation;
- player sets a risk boundary.

When:
- time advances to competition.

Then:
- coach resolves preparation choices inside the boundary;
- out-of-bounds decision escalates;
- player can review staff choice and outcome;
- player remains accountable for coach selection and authority design.

### Scenario C — Rival pressure

Given:
- player is evaluating a free agent;
- rival has need, funds, and interest.

When:
- player delays.

Then:
- rival may sign the target;
- market availability changes;
- player must adapt instead of seeing a static option.

### Scenario D — Financial distress

Given:
- cash is insufficient for an approaching mandatory obligation.

When:
- checkpoint is reached.

Then:
- time stops;
- recovery options are generated from actual assets/commitments;
- chosen restructuring action creates short-term sacrifice and future consequences;
- failure is terminal only if recovery conditions are exhausted / violated.

## 5. Cycle Acceptance Criteria

**PROPOSAL**

The cycle is ready for a headless prototype specification when:

- every material decision changes authoritative state or a future commitment;
- delegated actions can resolve without player input inside defined authority;
- automatic resolution never makes a high-impact executive choice for the player;
- immediate and delayed effects are distinguishable;
- rival actions can remove or alter opportunities;
- financial failure can enter and exit a recovery state;
- the player can trace company improvement/deterioration to decisions and world changes;
- at least two viable strategies can survive the same starting scenario.

## 6. Open Questions

**OPEN QUESTION**

- Minimum match-resolution inputs and outputs.
- Mandatory preparation decisions for manual mode.
- How many material decisions per in-game week create the desired pacing.
- Exact checkpoint interruption policy.
