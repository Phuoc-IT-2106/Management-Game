# PHASE 2 OPEN QUESTIONS

Status: Phase 2 — Core Loop & Simulation specialist register

This file contains questions that remain unresolved after the Phase 2 simulation model. They must not be silently answered during implementation.

## Priority A — Required before Headless Prototype Specification

### Q2-01 — Competition Calendar

**OPEN QUESTION**

What competition format and calendar does the first prototype use?

Needed to finalize:

- rival count;
- time pacing;
- match/checkpoint cadence;
- registration deadlines;
- season / cycle length.

### Q2-02 — Minimum Match Resolution Contract

**OPEN QUESTION**

What exact inputs and outputs define medium-depth competition resolution?

At minimum likely inputs:

- roster capability;
- staff/coach quality;
- preparation;
- opponent strength;
- availability;
- strategic matchup / risk posture;
- uncertainty.

Must be specified with the Esports Competition specialist.

### Q2-03 — Mandatory Manual Competitive Decisions

**OPEN QUESTION**

Which 1–3 competitive decisions must remain available to the player in the first slice, and which can be delegated entirely?

### Q2-04 — First-Slice Staff Roles

**OPEN QUESTION**

Beyond the coach / competitive lead, does the first prototype require a separate scout/analyst or commercial role for delegation to be meaningful?

### Q2-05 — Financial Cadence and Thresholds

**OPEN QUESTION**

What are the actual payroll cadence, sponsor payment cadence, debt-service cadence, distress window, and grace periods?

### Q2-06 — Company Value Abstraction

**OPEN QUESTION**

How is company value estimated at sufficient depth to support the approved minimum-value failure path without becoming a valuation/accounting simulator?

## Priority B — Important for Balance and Information Design

### Q2-07 — Audience Granularity

**OPEN QUESTION**

Is one aggregate audience/fandom pool sufficient, or are a small number of audience segments required?

### Q2-08 — Reputation Sensitivity

**OPEN QUESTION**

How much should single results matter compared with sustained performance and organizational behavior?

### Q2-09 — Information Representation

**OPEN QUESTION**

Should uncertainty/confidence be numeric, categorical, range-based, or descriptive?

### Q2-10 — Organizational Capacity Granularity

**OPEN QUESTION**

Is one aggregate capacity pool sufficient for the first prototype, or must competitive and commercial workload be separated?

### Q2-11 — Rival Financial Visibility

**OPEN QUESTION**

How much can the player know about rival cash pressure, roster budgets, and sponsor needs?

### Q2-12 — Sponsor Competition

**OPEN QUESTION**

Are sponsor opportunities exclusive, category-exclusive, or independently available to multiple organizations?

## Priority C — Can Remain Deferred

### Q2-13 — Executive Identity

**OPEN QUESTION — inherited**

Founder/owner-CEO, hired CEO, or abstract executive identity?

This does not block the minimum simulation model unless leadership removal becomes a failure condition.

### Q2-14 — Leadership Removal

**OPEN QUESTION — inherited**

Can the player be removed from leadership independently of company failure?

### Q2-15 — Deep Staff Relationships

**OPEN QUESTION**

Do trust, loyalty, ambition, and persistent coach/player relationships belong in the first prototype or later System Depth?

Recommended default: defer unless a specific core decision cannot work without them.

### Q2-16 — Meta Change

**OPEN QUESTION**

Does the first prototype need active meta shifts, or can changing rivals/talent/sponsor conditions provide enough early variation?

## Decisions/Proposals Needing Director Approval

**PROPOSAL**

The following Phase 2 choices should be explicitly accepted or revised before they are treated as canonical:

1. one-day authoritative Simulation Tick;
2. checkpoint-driven player-facing advancement;
3. primary pools: cash, organizational capacity, reputation, audience, information quality;
4. commercial value / competitive capability / strategic flexibility as derived states;
5. minimum rival ecosystem: at least four rivals, preferred five;
6. compressed rival simulation with shared ecosystem constraints;
7. failure ladder: Stable → Warning → Distress → Restructuring → Stabilized or Terminal;
8. authority-envelope model for delegation;
9. daily/weekly/monthly conceptual cadences;
10. immediate / short-delay / medium-delay / long-delay consequence classes.
