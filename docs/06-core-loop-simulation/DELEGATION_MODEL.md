# DELEGATION MODEL

Status: Phase 2 — Core Loop & Simulation specialist proposal

## 1. Foundation

**DECISION — inherited**

Delegation is a core part of the executive fantasy. Operational micromanagement is secondary. Manual competitive control may exist as optional depth.

## 2. Four Resolution Classes

### A. Directly Controlled

**PROPOSAL**

The player directly controls decisions with material strategic consequence.

First-slice examples:

- company spending priorities;
- major roster sign / sell / release decisions;
- key staff hire / replace decisions;
- major contract commitments;
- sponsor acceptance / rejection;
- debt / emergency financing decisions;
- restructuring choices;
- high-level competitive preparation priority;
- override of an escalated staff decision.

### B. Staff-Recommended

**PROPOSAL**

Staff can prepare a recommendation, but the player owns approval when the decision crosses a material threshold.

Examples:

- target shortlist;
- opponent-specific plan;
- lineup / role proposal;
- scouting priority change;
- sponsor opportunity assessment;
- warning that current workload/plan exceeds capacity.

Recommendation quality depends on staff capability, information quality, workload, and strategic tendencies.

### C. Delegated

**PROPOSAL**

The player assigns authority within an explicit boundary.

Examples:

- routine training / preparation allocation;
- detailed opponent analysis;
- lineup adjustments within roster constraints;
- routine scouting work;
- minor competitive decisions;
- sponsor-delivery operations that do not alter the contract.

Delegated actions can create real consequences. Delegation reduces control burden, not accountability.

### D. Automatically Resolved

**PROPOSAL**

No agent chooses between meaningful strategic alternatives; the simulation applies rules/commitments.

Examples:

- calendar progression;
- scheduled salary payment;
- contract countdown;
- due sponsor payment;
- passive audience/reputation drift where applicable;
- expiration of an offer;
- routine low-value administration;
- application of already-committed contract terms.

## 3. Authority Envelope

**PROPOSAL**

Delegation should be defined by an Authority Envelope containing conceptually:

- domain: what may be decided;
- scope: which team / function / opportunity;
- budget / resource ceiling;
- strategic constraints;
- risk tolerance;
- escalation triggers;
- review cadence.

Example:

> Coach controls preparation and lineup decisions, but may not bench a designated core player for a high-stakes match without escalation.

This is a simulation concept, not a UI or class design.

## 4. Recommendation Model

**PROPOSAL**

A recommendation should include:

- recommended action;
- expected upside/downside;
- confidence / uncertainty;
- key assumptions;
- resource cost;
- whether it fits current company priority.

The recommendation can be wrong because of imperfect information or staff limitations, but should not be arbitrary.

## 5. Delegation Quality Drivers

**DECISION — inherited**

Staff quality should matter to recommendation and delegated execution quality.

**PROPOSAL**

For the first slice, use only drivers that materially affect decisions:

- role skill;
- strategic / tactical tendency;
- information quality;
- workload / capacity;
- familiarity with the roster/opponent where relevant;
- authority clarity.

Do not require a deep personality simulator.

## 6. Escalation Rules

**PROPOSAL**

Delegated work escalates to the player when:

- a decision exceeds budget/authority;
- two company objectives conflict materially;
- risk exceeds the authorized threshold;
- a deadline would be missed;
- the situation becomes structurally important;
- the responsible staff member cannot form a recommendation with sufficient confidence.

Escalation is a checkpoint and stops time only when the choice is material.

## 7. Override Consequences

**PROPOSAL**

Overriding staff should not create an arbitrary penalty by default.

Its consequence should come from the decision itself, plus possible future relationship/trust mechanics only if those mechanics are approved later.

For the first slice, repeated overrides may be tracked as feedback but should not require a deep staff-relationship system.

## 8. First-Slice Minimum Delegation

**PROPOSAL**

Minimum viable delegation requires:

1. one competitive staff role (coach / competitive lead);
2. staff recommendations before selected competition decisions;
3. an authority envelope for preparation / lineup-level operations;
4. at least one escalation case;
5. review feedback showing what the coach decided and why.

Commercial or scouting delegation may be added only if needed to validate the loop; it is not required for the first minimal test.

## 9. Open Questions

**OPEN QUESTION**

- Exact mandatory competitive decisions retained by the player.
- Whether the first slice needs a second delegatable role beyond coach.
- Whether staff confidence is numeric, categorical, or purely descriptive.
- Whether persistent trust/relationship effects belong in the first prototype or a later depth phase.
