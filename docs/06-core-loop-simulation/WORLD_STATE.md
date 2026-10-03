# WORLD STATE

Status: Phase 2 — Core Loop & Simulation specialist proposal

## 1. Definition

**FACT**

`World State` is the authoritative simulation state outside the player's direct company.

**DECISION — inherited**

Rival organizations must evolve and compete for relevant resources and opportunities rather than remain static background entities.

## 2. Minimum World State for the First Vertical Slice

### 2.1 Calendar and Competition

**PROPOSAL**

Track:

- current date;
- competition schedule;
- event / match results;
- standings or progression state;
- competition importance / stakes;
- registration / roster deadlines required by the chosen discipline.

### 2.2 Rival Organizations

**PROPOSAL**

Each rival needs only enough state to create pressure in the same ecosystem:

- competitive strength;
- roster core / key talent assets;
- coach / staff quality summary;
- financial posture;
- reputation;
- audience / commercial attractiveness;
- current strategic posture;
- major active commitments;
- current roster / sponsor needs.

Rivals do not require the full player-company management interface or full internal detail.

### 2.3 Talent / Labor Market

**PROPOSAL**

Track a limited market of relevant players and key staff with:

- availability;
- estimated ability / potential;
- compensation expectations;
- contract status;
- interest / attractiveness constraints;
- competing organizations pursuing them when relevant.

The market can remain small and curated for the first prototype.

### 2.4 Sponsor Market

**PROPOSAL**

Track:

- a limited set of sponsor opportunities;
- budget / offer range;
- audience / reputation preferences;
- performance or exposure expectations;
- exclusivity / restriction categories;
- availability window;
- rival competition for selected opportunities.

### 2.5 Competitive Environment / Meta

**PROPOSAL**

Represent only the minimum changing conditions necessary to alter preparation or roster value:

- current strategic/meta context at abstract depth;
- whether certain player/strategy strengths are relatively more or less valuable;
- change events infrequent enough to remain understandable.

**ASSUMPTION**

A detailed game-patch simulator is outside the first slice.

### 2.6 Limited Market Conditions

**PROPOSAL**

The first slice may include one simple external commercial climate signal if needed for variation, such as sponsor demand being weak / normal / strong.

This should not become a macroeconomic simulation.

## 3. Shared-Ecosystem Rules

**PROPOSAL**

Player and rivals should share:

- the same competition results;
- the same talent pool;
- the same sponsor opportunity pool where applicable;
- the same calendar;
- the same broad reputation/audience logic;
- the same constraint that resources and commitments have costs.

Symmetry of **rules and scarcity** is required; symmetry of simulation detail is not.

## 4. World Changes That May Trigger Player Decisions

**PROPOSAL**

Examples:

- rival signs a target;
- talent becomes available;
- sponsor window opens/closes;
- rival improves / rebuilds;
- competitive meta shifts;
- standings change future stakes;
- a rival loses key staff / player;
- market demand changes sponsor terms.

Only changes that create material decisions or feedback need player-facing visibility.

## 5. Excluded World Simulation

**DECISION — inherited**

The first slice must not become a large global economy or future-industry simulation.

**PROPOSAL**

Exclude for the first slice:

- multiple industries;
- multiple esports titles;
- full global league ecosystem;
- deep regulation simulation;
- broad technology economy;
- media-company simulation;
- complete fan-by-fan behavior;
- detailed labor-market macroeconomics.

## 6. World-State Acceptance Criteria

**PROPOSAL**

World State is sufficient when:

- rivals can change the value/timing of player decisions;
- the player can lose talent or sponsor opportunities to rivals;
- competition results alter future stakes;
- the market does not feel static across several cycles;
- world change remains explainable without a full global simulation.

## 7. Open Questions

**OPEN QUESTION**

- Final number of rival organizations in the first prototype.
- Exact competition format.
- Size of the talent and sponsor pools.
- Whether a simple meta-change system is required for the first prototype or can wait until the next depth phase.
