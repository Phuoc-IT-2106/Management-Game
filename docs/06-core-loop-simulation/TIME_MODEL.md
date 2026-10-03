# TIME MODEL

Status: Phase 2 — Core Loop & Simulation specialist proposal

Authority note: Phase 1 `DECISION` items are inherited as approved product constraints. New Phase 2 choices in this file remain `PROPOSAL` until Project Director approval.

## 1. Purpose

**FACT**

The approved management loop requires an explicit `Advance Time` stage, while Phase 1 left the exact time unit unresolved. The first slice must include time progression but should not become a daily-task micromanagement game.

**DECISION — inherited**

The player operates mainly at executive + management level. Low-value operational activity is abstracted or delegated.

## 2. Authoritative Time Unit

**PROPOSAL**

Use **one calendar day as the smallest authoritative Simulation Tick**.

A day is small enough to support:

- competition dates;
- contract deadlines;
- salary / debt / sponsor obligations;
- preparation windows;
- staff and roster availability changes;
- market changes;
- rival actions.

The player is **not required to make a decision every day**.

**ASSUMPTION**

Sub-day simulation is unnecessary for the first vertical slice because no approved core decision requires hour-level timing.

## 3. Player-Facing Advancement

**PROPOSAL**

Separate the Simulation Tick from the **Decision Checkpoint**.

The default player action should be:

> Advance to the next meaningful checkpoint.

A checkpoint occurs when at least one of these is true:

1. a scheduled competition needs preparation, intervention, or review;
2. a material contract or financial obligation is approaching or due;
3. a sponsor decision or obligation requires executive attention;
4. a roster / staff decision reaches a deadline;
5. a critical risk threshold is crossed;
6. a delegated staff member escalates a decision outside assigned authority;
7. a player-selected calendar stop is reached;
8. a periodic operating review is due.

Routine days between checkpoints resolve automatically.

## 4. Review Cadence

**PROPOSAL**

Use three conceptual cadences without forcing three separate screens:

- **Daily tick:** authoritative state progression.
- **Weekly operating review:** trends, upcoming obligations, roster/staff condition, rival movement.
- **Monthly financial review:** cash movement, committed obligations, runway pressure, sponsor income, debt state, company-value trend.

Competition events may create additional checkpoints regardless of cadence.

**OPEN QUESTION**

The final season length, tournament calendar, and whether the weekly/monthly review is mandatory or skippable should be specified with the Esports Competition specialist.

## 5. Action Timing

**PROPOSAL**

Every material action should have, where relevant:

- decision time;
- effective time;
- commitment duration;
- deadline / expiry;
- lead time;
- review point;
- reversal cost or irreversibility.

Examples:

- signing a player may create an immediate cash commitment but competitive value only after joining and integration;
- changing preparation priority affects the next preparation window, not past work;
- a sponsor agreement may pay on scheduled dates while restrictions apply immediately;
- staff replacement can create immediate cost and delayed operational improvement.

## 6. Interruption Rules

**PROPOSAL**

Time advancement should stop only for **material checkpoints**. Routine noise should not interrupt the player.

Mandatory stop candidates:

- decision deadline with irreversible consequence;
- competition requiring a mandatory high-level choice;
- missed or unpayable obligation;
- contract expiry / signing deadline;
- staff escalation beyond delegated authority;
- transition into financial distress / restructuring.

Informational changes may be summarized at the next review instead of stopping time.

## 7. Immediate vs Delayed Time Semantics

**PROPOSAL**

Consequences are classified as:

- **Immediate:** applied at commitment or event resolution on the same tick;
- **Short-delay:** materialize over days / the next match window;
- **Medium-delay:** materialize over several events / a contract or sponsor cycle;
- **Long-delay:** materialize over multiple months / seasons.

A decision may create several consequences with different delays.

## 8. Randomness and Reproducibility

**PROPOSAL**

Uncertainty should be resolved at explicit simulation events rather than through opaque continuous noise.

For validation, the simulation model should support the requirement:

> Same starting state + same decisions + same resolved uncertainty sequence => same resulting state.

This is a simulation testability requirement, not a software architecture decision.

## 9. Time-Model Acceptance Criteria

**PROPOSAL**

The time model is valid for the first prototype when:

- the player can advance through multiple weeks without daily busywork;
- deadlines and obligations cannot be bypassed by skipping time;
- delayed consequences can be traced to earlier commitments;
- competition, finance, contracts, sponsors, staff, and rivals share the same calendar;
- a crisis can interrupt normal advancement;
- repeated cycles create changing decision contexts.

## 10. Open Questions

**OPEN QUESTION**

- What is the exact competition calendar for the chosen discipline?
- How frequently should payroll and sponsor payments occur in the first prototype?
- Which events are mandatory stops versus summary-only notifications?
- Should the player be allowed to advance to an arbitrary date, or only to predefined checkpoints in the first prototype?
