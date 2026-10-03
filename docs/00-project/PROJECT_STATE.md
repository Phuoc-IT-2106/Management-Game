# PROJECT STATE

Version: 0.5
Phase: Phase 3 — Headless / Minimal Prototype
Status: Prototype Specification Approved — Ready for BUILD / VERIFY

## Current Objective
Implement and verify the smallest deterministic, headless or minimal-presentation simulation needed to test whether the core company-management model repeatedly creates meaningful, explainable, context-dependent decisions.

This phase validates gameplay logic. It does **not** select the production engine or technical architecture and does not authorize production vertical-slice development.

## Strategic Center
The project is a **company-first PC management / business strategy simulation that begins with professional esports as its first validation domain**.

The player primarily acts as a CEO / executive while remaining directly involved in important management decisions. Competitive success matters because it creates strategic and commercial leverage, but long-term company success is the broader objective.

## Completed Phase Gates

### Phase 0 — Discovery
CLOSED.

Discovery established the player fantasy, strategic center, authority model, role of competition, simulation philosophy, structural failure direction, rival-world requirement, and long-term company-first identity.

### Phase 1 — Product Foundation
CLOSED for the purpose of prototype validation.

The foundation defines executive + management authority, bounded manual competitive control, delegation, narrow early scope, the management loop, and the company-first product promise.

### Phase 2 — Core Loop & Simulation
CLOSED.

Director integration accepted:
- daily authoritative time progression with checkpoint-driven player interaction;
- Company State / World State ownership;
- five primary resource concepts;
- stored-vs-derived state discipline;
- immediate and delayed consequences;
- Authority Envelope delegation;
- bounded rival simulation;
- staged structural failure/recovery;
- explicit cross-system outcome boundaries;
- integrated Simulation & Economy × Esports Competition model.

## Accepted Decisions

Canonical accepted decisions are recorded in `DECISION_LOG.md`.

Current accepted range:
- DEC-001 through DEC-020.

Major current decisions include:
- esports as the initial validation domain;
- CEO/executive player role with executive + management authority;
- company success as the strategic center;
- scalable direct/delegated competitive control;
- medium simulation depth with selective deeper systems;
- evolving rivals;
- structural failure with recovery gameplay;
- company-first long-term identity;
- one-day authoritative simulation tick with checkpoint-driven interaction;
- explicit Company State / World State ownership;
- Cash, Organizational Capacity, Reputation, Audience/Fandom, and Information Quality as primary resource concepts;
- store durable causes and derive aggregate interpretations;
- Observe → Prioritize → Decide → Commit → Delegate/Intervene → Advance → Resolve → Review → Adapt;
- bounded Authority Envelope delegation;
- lower-fidelity but meaningful rival simulation;
- Stable → Warning → Distress → Restructuring → Stabilized/Terminal failure progression;
- cross-system outcome boundaries;
- derived Preparation Capacity and domain-aware information;
- bounded medium-depth competitive resolution;
- a narrow headless prototype validation envelope.

## Approved Prototype Validation Envelope

The current prototype may use:
- 1 player-controlled company;
- 1 fictional 5v5 role-based esports test discipline;
- 1 primary team;
- 6 contracted players: 5 starters + 1 flex substitute;
- 1 Head Coach;
- 4 bounded rival organizations;
- a tiny talent market;
- 1 active sponsor relationship plus a bounded opportunity pool;
- 1 simple financing mechanism;
- up to 84 calendar days as the default integrated-run horizon;
- a 5-organization competition cycle, with double round-robin as the baseline and a small playoff layer permitted when it materially improves validation.

The fictional discipline is a **prototype test abstraction**, not a commitment to the production game's final esports discipline.

All numerical starting values, coefficients, Currency Units, exact duration, and exact match counts are **noncanonical calibration parameters**. They may be tuned or reduced during prototype testing.

## Prototype Validation Target

The prototype must answer:

> Does the simulation repeatedly create meaningful management decisions where the preferred choice changes with company state, competitive conditions, information, commitments, rivals, and risk without relying mainly on scripted novelty?

Required systemic evidence includes:
- competition → CompetitiveOutcome → reputation/audience → commercial leverage → future financial/strategic options;
- aggressive investment producing upside and future obligations;
- information improving estimates rather than directly buffing assets;
- growth creating organizational-load pressure;
- delegation reducing burden without becoming universally optimal;
- rival adaptation changing the value of repeated strategies;
- distress creating costly recovery choices;
- success encountering diminishing returns, scarcity, commitments, and organizational pressure rather than automatic snowballing.

## Prototype Build Scope Guard

### Included
- Company State and World State needed by the prototype;
- small roster and Head Coach;
- contracts and financial commitments;
- Cash and scheduled financial flows;
- Organizational Capacity / Load;
- Reputation;
- Audience / Fandom;
- domain-aware Information Quality;
- bounded preparation and competitive resolution;
- Authority Envelope delegation;
- bounded rivals;
- sponsor opportunity / agreement loop;
- one simple financing mechanism;
- failure/restructuring path;
- deterministic seeded scenario execution;
- causal/debug traces.

### Explicitly excluded
- multiple esports disciplines;
- multiple player-controlled teams;
- additional industries;
- full tactical/action-level match simulation;
- deep drafting;
- detailed training schedules;
- large staff hierarchies;
- deep relationship simulation;
- detailed facilities;
- merchandising/media/M&A systems;
- global macroeconomy;
- detailed accounting/taxation;
- advanced debt markets;
- large authored event libraries;
- final UI/UX;
- production engine selection;
- production architecture;
- final save/load design.

## Prototype Calibration Rules

The following are not product decisions:
- exact 0–100 attribute scales;
- starting Cash values;
- exact player/coach ratings;
- exact Reputation/Audience values;
- exact preparation coefficients;
- exact match probability clamps;
- exact strategy-exposure rates;
- exact overload curves;
- exact debt cost;
- exact reputation/audience persistence;
- exact 84-day duration if fewer days produce equivalent evidence;
- exact 4-rival count if fewer rivals preserve the same validation value.

Prototype parameters must remain easy to tune and must not be treated as final balance.

## Resolved Prototype Blockers

### BQ-01 — Source-of-truth synchronization
RESOLVED.

`DECISION_LOG.md` and `PROJECT_STATE.md` have been synchronized through the accepted Phase 2 integration and prototype Director gate.

### BQ-02 — Fictional discipline archetype
RESOLVED FOR PROTOTYPE.

Use the proposed fictional **5v5 Role Arena** abstraction only as a test discipline with:
- 5 active roles;
- 1 flex substitute;
- role proficiency;
- lineup decisions;
- preparation;
- strategic posture;
- matchup;
- bounded meta change;
- probabilistic competitive resolution.

No production discipline choice is implied.

### BQ-03 — Prototype calibration authority
RESOLVED.

All numeric starting values and coefficients are test calibration only and are noncanonical.

## Open Questions To Test During Prototype
- whether four rivals are necessary or fewer create equivalent pressure;
- whether 84 days are necessary or a shorter run provides equivalent evidence;
- whether 8–10 matches are necessary;
- whether all proposed information domains create distinct decisions;
- whether Reputation and Audience remain mechanically distinct;
- whether Commercial Value adds useful explanatory/decision value;
- whether Company Value is useful enough to retain in the prototype;
- whether three preparation priorities create genuine opportunity cost;
- whether Organizational Capacity changes decisions rather than becoming a penalty meter;
- whether strategy exposure and rival adaptation should remain separate concepts;
- whether the flex substitute produces meaningful lineup choices;
- whether sponsor restrictions add enough strategic value;
- exact prototype tuning for variance, adaptation, overload, debt, and delayed consequences.

## Deferred Beyond Prototype
- final insolvency / game-over thresholds;
- final victory conditions;
- founder/owner-CEO versus hired CEO identity;
- final season/campaign pacing;
- production esports discipline selection;
- deep drafting and mid-match control;
- additional staff roles;
- deep people relationships;
- facilities;
- additional teams/disciplines;
- non-esports industries;
- advanced finance;
- final UI;
- engine/framework;
- production technical architecture;
- save/load architecture;
- multiplayer;
- modding.

## Active Risks
- prototype growing into a mini production game;
- many changing numbers without changing decisions;
- cross-system double counting;
- duplicate authoritative state;
- opaque coach/delegation logic;
- Information Quality becoming a hidden buff;
- Organizational Capacity becoming generic mana;
- rival adaptation becoming rubber-banding;
- economy drifting into bookkeeping;
- success snowballing too quickly;
- provisional prototype numbers becoming accidental canon;
- AI-assisted implementation drifting from accepted specifications.

## Phase 3 Exit Gate

Do **not** advance to Technical Foundation merely because the prototype runs.

Project Director should close Phase 3 only when prototype evidence shows that the critical acceptance criteria pass, including:
- multiple management approaches are viable in different contexts;
- changed state changes rational decisions;
- competition and business interact materially;
- commercial leverage does not directly print money;
- information changes confidence/decision quality rather than asset strength;
- delegation is useful but not universally optimal;
- rivals materially affect decisions;
- financial commitments constrain future options;
- distress is recoverable only through meaningful sacrifice;
- success does not create uncontrolled snowballing;
- variation emerges from system state rather than scripted novelty;
- important outcomes are causally explainable;
- fixed-seed scenario runs are reproducible.

## Current Priority

Proceed to **BUILD / VERIFY of the minimal headless simulation prototype** using the approved prototype specification and acceptance scenarios.

Do not choose the production engine or production architecture yet.

After prototype evidence is reviewed and the Phase 3 exit gate passes, proceed to **Phase 4 — Technical Foundation**.