# DISCOVERY WORKSHEET — COMPLETED

Project: Strategic Company Simulator  
Phase: Phase 0 — Discovery  
Status: Completed user answers for Director consolidation

This document records the user's current answers to the eight high-impact Discovery questions.

These answers should be reviewed by `00 — Project Director` before being converted into locked project decisions.

---

# Q1 — Primary Player Fantasy

**Answer: B — Run an esports organization as a CEO/executive.**

## Reasoning

The primary fantasy is to lead and develop an esports organization at the company level rather than only managing a competitive roster.

Competitive success remains important, but the player is also responsible for:

- financial health
- people
- sponsors
- organizational structure
- reputation
- long-term strategy
- business development

The player should feel like the person responsible for the direction and growth of the organization, not merely the manager of one team.

---

# Q2 — Primary Decision Level

**Answer: Hybrid — Executive + Management.**

Operational control is secondary.

## Executive Responsibilities

The player should make decisions involving:

- budgets
- company strategy
- expansion
- leadership appointments
- organizational priorities
- major investments
- major partnerships

## Management Responsibilities

The player should also remain involved in important management decisions such as:

- roster strategy
- key staff hiring
- contracts
- sponsors
- scouting direction
- preparation priorities
- department resource allocation

## Operational Responsibilities

Operational-level control is not a major focus.

Detailed daily tasks, schedules, repetitive micro-management, and low-value operational decisions should usually be:

- abstracted,
- simplified,
- or delegated.

As the company grows, delegation should become increasingly important so the player can remain focused on high-value executive and management decisions.

---

# Q3 — Competition vs Company Success

**Answer: Company success is the primary long-term objective, but competitive performance remains a major strategic priority.**

## Reasoning

The company should strongly pursue competitive success because competitive results create business leverage.

Competitive achievements can generate:

- reputation
- fandom
- audience growth
- sponsor value
- merchandise demand
- media attention
- commercial opportunities
- brand positioning
- talent attraction
- partnership opportunities

Winning is therefore not only a sporting objective.

It is also an economic and strategic asset.

The player should normally care about competitive performance, especially while building the organization, but competitive success is ultimately a means of strengthening the company rather than the only definition of success.

The game should allow situations where sacrificing short-term competitive performance is strategically reasonable when it improves:

- long-term company value
- financial stability
- brand growth
- future competitive potential
- business expansion
- risk management

## Strategic Relationship

Competitive Performance  
→ Reputation  
→ Fandom / Audience  
→ Commercial Value  
→ Revenue & Opportunities  
→ Company Growth  
→ Greater Competitive Capacity

This loop must include costs, risks, competition, and diminishing returns so that success does not become automatic infinite growth.

---

# Q4 — Match Simulation Depth

**Answer: Hybrid match-management model with optional manual coaching and AI delegation.**

The game should support both deeper manual competitive control and scalable delegation.

## Manual Coaching Mode

Players who want more control can participate in a combination of medium and deep competitive management.

Possible responsibilities include:

- opponent analysis
- strategic approach
- meta adaptation
- preparation priorities
- drafting or lineup decisions where relevant
- player roles
- tactical instructions
- risk level
- situational adjustments
- selected mid-match decisions

The goal is to provide meaningful competitive depth without forcing every player to micromanage every match.

Different esports disciplines may expose different domain-specific tactical options.

## Delegated Coaching Mode

The organization can hire coaches and competitive staff who act as AI-controlled managers.

A coach should be capable of:

- analyzing opponents
- evaluating available players
- recommending strategies
- proposing lineups
- selecting preparation priorities
- reacting to meta changes
- making tactical decisions
- managing matches within assigned authority

The quality of these decisions may depend on factors such as:

- coaching skill
- tactical knowledge
- game knowledge
- adaptability
- staff support
- available information
- team familiarity
- personality
- strategic tendencies

The player may:

- accept recommendations
- modify recommendations
- override important decisions
- define strategic boundaries
- delegate most competitive operations

## Design Goal

Manual Control when the player wants depth  
+  
Delegation when the player wants scale

Competitive management should be playable, but it should not become a mandatory micromanagement burden.

As the organization grows, delegation should become an important part of organizational gameplay.

---

# Q5 — Simulation Philosophy

**Answer: B+ — Medium simulation with selective deep systems.**

## Reasoning

The default simulation depth should remain moderate to preserve:

- clarity
- scalability
- balance
- maintainability
- UI readability
- AI-agent consistency

Selected systems may use deeper simulation when additional detail creates:

- meaningful strategic decisions
- emergent behavior
- stronger long-term consequences
- useful uncertainty
- better organizational gameplay

Priority areas for deeper simulation may include:

- company strategy
- competitive management
- coach / manager AI
- key people relationships
- organizational growth
- risk
- resource allocation

Low-value operational detail should remain:

- abstracted
- simplified
- or delegatable

## Design Principle

**Deep where decisions matter.  
Simple where detail becomes repetitive work.**

---

# Q6 — Long-Term Campaign Motivation

**Answer: Long-term motivation should come from building and evolving the company inside a competitive world populated by multiple rival organizations.**

## Reasoning

The player should not operate in an isolated economy.

Other organizations and companies should actively compete within the same ecosystem.

Competition may occur across:

- esports performance
- talent recruitment
- staff hiring
- sponsorship
- audience
- fandom
- brand reputation
- media presence
- commercial partnerships
- merchandise
- market expansion
- new business opportunities

Rival organizations should evolve over time rather than remain static background entities.

They may:

- improve
- decline
- change leadership
- rebuild teams
- change strategy
- enter new markets
- lose key people
- gain sponsors
- expand into new divisions
- respond to market trends
- compete directly with the player's company

The long-term campaign motivation should combine:

- organizational growth
- adapting to an evolving world
- competitive rivalry
- business competition
- developing people
- building a larger corporate organization

The player should feel that the world continues to develop even without direct player involvement.

Long-term success should come not only from accumulating more resources, but from making better strategic decisions than competing organizations in a changing environment.

---

# Q7 — Failure Model

**Answer: The company should face genuine structural failure conditions.**

Primary failure conditions may include:

- bankruptcy
- taking on debt and failing to meet repayment obligations
- falling below a required minimum company valuation
- losing financial viability for a sustained period

## Reasoning

Failure should have real consequences so that financial and strategic decisions retain weight.

Debt should be usable as a strategic tool, but it must create:

- repayment pressure
- risk
- reduced flexibility
- possible restructuring consequences

If the company cannot meet its obligations, possible consequences may include:

- emergency asset sales
- forced downsizing
- closing divisions
- selling player contracts
- losing staff
- sponsor withdrawal
- reduced reputation
- refinancing under worse terms
- loss of strategic flexibility

The game should generally allow a recovery window before immediate campaign termination.

However, if the company reaches true insolvency, repeatedly fails required obligations, or falls below a defined minimum company-value threshold without recovery options, the campaign may end.

## Design Principle

**Failure should create recovery gameplay before it creates game over.**

Debt should be a strategic instrument, not free money.

---

# Q8 — Long-Term Identity

**Answer: C — General strategic company simulator beginning with esports.**

## Reasoning

Esports is the starting business domain, not the permanent boundary of the game.

The long-term product should allow the company to expand into additional industries such as:

- traditional sports
- media
- advertising
- events
- merchandising
- talent management
- partnerships
- investments
- other strategic business divisions

The game should eventually separate shared company systems from domain-specific modules.

## Shared Company Systems May Include

- finance
- debt
- valuation
- brand
- reputation
- people
- contracts
- organizational capacity
- risk
- executive strategy

## Domain Modules May Include

- esports
- traditional sports
- media
- advertising
- events
- merchandising
- talent management
- future business sectors

Esports should be treated as the first major domain module used to validate the company-management model.

Future divisions should reuse the shared company core while introducing only the systems specific to their domain.

The project should not implement these future industries early.

## Design Principle

**Broad long-term vision, narrow early implementation.**

---

# DISCOVERY SUMMARY

The current direction can be summarized as:

- The player primarily acts as a CEO / executive.
- The player works mainly at executive and management levels.
- Operational micromanagement is secondary and increasingly delegatable.
- Competitive success is important because it creates business value.
- Competitive management can be manual or delegated to AI coaches.
- Simulation uses medium depth by default, with selective deep systems.
- Rival organizations actively compete and evolve in the same world.
- Financial failure is real and may lead to restructuring or game over.
- The long-term product is a general strategic-company simulator beginning with esports.

---

# CURRENT PRODUCT DIRECTION

The project is moving toward:

> A PC business strategy and management simulation in which the player begins by operating a professional esports organization, uses competitive success to build reputation, fandom, commercial value, and organizational capability, competes against evolving rival organizations, and may eventually expand the company into multiple sports and business sectors.

The intended long-term fantasy is not merely to manage winning teams.

It is to build, operate, adapt, and protect a growing company inside a competitive and changing world.

---

# RECOMMENDED DIRECTOR ACTION

`00 — Project Director` should now:

1. review all eight answers for contradictions;
2. reconcile them with existing project sources;
3. decide which answers are ready to become accepted project decisions;
4. update `PROJECT_STATE.md`;
5. update `DECISION_LOG.md`;
6. revise `GAME_VISION.md` if necessary;
7. define the strategic center of the product;
8. define the first vertical-slice boundaries;
9. determine whether Phase 0 exit criteria are satisfied;
10. activate `01 — Product & Game Design` only after Phase 0 is formally closed.

No technical architecture or production implementation should be started from this worksheet alone.
