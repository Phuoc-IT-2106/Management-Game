# COMPANY STATE

Status: Phase 2 — Core Loop & Simulation specialist proposal

## 1. Definition

**FACT**

`Company State` is the authoritative simulation state describing the player company and its owned entities.

**DECISION — inherited**

The first slice contains one player-controlled company, one esports discipline, one primary team, limited staff, contracts, finance, sponsorship, reputation/audience linkage, delegation, time progression, and a recovery path.

## 2. Minimum Authoritative Company State

### 2.1 Company Identity and Strategic Posture

**PROPOSAL**

Store conceptually:

- company identity;
- current strategic priorities;
- current risk posture;
- current executive constraints / policies;
- active restructuring state, if any.

The exact player identity (founder, owner-CEO, hired CEO, abstract executive) remains outside this minimum state until approved.

### 2.2 Financial State

**PROPOSAL**

Minimum financial state:

- current cash;
- scheduled material inflows;
- scheduled material obligations;
- payroll / staff commitment total;
- outstanding debt / financing commitments;
- next repayment deadlines;
- distress status;
- estimated company value / minimum-value status.

Do not require a full balance sheet or accounting ledger.

### 2.3 Resource State

**PROPOSAL**

Track the primary resource pools defined in `RESOURCE_MODEL.md`:

- cash;
- organizational capacity;
- reputation;
- audience;
- information quality by relevant domain.

### 2.4 Team / Roster State

**PROPOSAL**

For each roster member, the minimum decision-relevant state is:

- identity;
- role / position relevant to the discipline;
- competitive ability estimate;
- development / future-value estimate where needed;
- availability / fatigue-equivalent condition at abstract depth;
- current form or short-term performance signal;
- contract terms relevant to decisions;
- salary / financial commitment;
- transfer / exit status where relevant;
- limited morale / satisfaction signal only if it changes meaningful decisions.

**ASSUMPTION**

Deep personality and relationship networks are excluded from the first slice unless later testing proves they are required.

### 2.5 Staff State

**PROPOSAL**

For each key staff member:

- role;
- relevant skill / quality dimensions;
- contract / cost;
- delegated authority scope;
- strategic tendencies;
- current workload / capacity contribution;
- recommendation confidence where applicable.

The exact first-slice staff-role list remains open. The model must at minimum support a competitive lead / coach capable of meaningful delegation.

### 2.6 Competitive Preparation State

**PROPOSAL**

Minimum state:

- next competition / opponent;
- current preparation priority;
- selected strategic posture or staff recommendation;
- manual vs delegated authority status;
- preparation progress / readiness at abstract depth;
- known uncertainty about opponent / meta.

### 2.7 Sponsor / Commercial State

**PROPOSAL**

For each active sponsor relationship:

- contract duration;
- payment schedule;
- obligations / performance clauses at abstract depth;
- exclusivity / strategic restrictions;
- relationship health;
- renewal / termination window.

For live sponsor opportunities:

- offer value;
- fit / requirements;
- deadline;
- uncertainty / recommendation.

### 2.8 Organizational Capacity and Delegation State

**PROPOSAL**

Minimum state:

- total effective capacity;
- committed capacity by major function;
- overload / spare-capacity signal;
- authority assignments;
- escalation rules / boundaries;
- unresolved staff-recommended decisions.

### 2.9 Commitments and Delayed Effects

**PROPOSAL**

The Company State must retain material commitments that have future effects, such as:

- signed contracts not yet effective;
- future payments;
- debt service dates;
- sponsor obligations;
- preparation commitments;
- staff changes in transition;
- recovery / restructuring obligations.

This is required so delayed consequences remain causally traceable.

### 2.10 Risk and Recovery State

**PROPOSAL**

Track:

- liquidity warning status;
- overdue obligations;
- debt-pressure status;
- company-value floor status;
- restructuring stage;
- available recovery options;
- grace periods / deadlines where applicable.

## 3. Derived State — Do Not Duplicate as Authoritative Pools

**PROPOSAL**

Calculate or summarize from authoritative state where possible:

- competitive capability;
- commercial value;
- strategic flexibility;
- financial runway;
- sponsor attractiveness;
- company health summary;
- current executive attention priorities.

## 4. State Ownership Rule

**PROPOSAL**

Every material outcome in the first slice must end as a change to either:

1. authoritative Company State;
2. authoritative World State;
3. a scheduled future commitment connecting the two.

No major consequence should exist only as flavor text.

## 5. Minimum-State Acceptance Criteria

**PROPOSAL**

Company State is sufficient when it can answer:

- Can the company meet its obligations?
- What is the current competitive capability and why?
- Which roster/staff commitments limit future choices?
- What can staff decide without the player?
- What is the current reputation and audience trajectory?
- Which sponsors are active and what do they constrain?
- Where is organizational capacity overloaded?
- Which earlier decisions are waiting to produce delayed consequences?
- Is the company healthy, under warning, in distress, or restructuring?

## 6. Open Questions

**OPEN QUESTION**

- Exact first-slice staff roles.
- Minimum player attributes needed for medium-depth competition.
- Whether morale/satisfaction is required in the first prototype.
- Exact company-value formula / abstraction.
- Whether one aggregate organizational-capacity pool is sufficient or must be split into competitive and commercial capacity.
