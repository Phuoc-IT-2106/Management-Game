# DECISION LOG

Use this file for approved project-level decisions.

Do not record brainstorming as a decision.

---

## Decision Template

### DEC-XXX — Title
Date:
Status: Proposed / Accepted / Rejected / Superseded

Context:
Why is this decision needed?

Decision:
What was chosen?

Alternatives:
What other options were considered?

Reasoning:
Why was this option selected?

Consequences:
What becomes easier, harder, required, or impossible?

Affected Systems:
- ...

---

## Current Decisions

### DEC-001 — Esports as Initial Product Domain
Date: 2026-10-03
Status: Accepted

Context:
The long-term game may become a broad strategic-company simulator, but the project needs a narrow initial domain that can validate the management model.

Decision:
Use professional esports as the initial product, first major domain, and vertical-slice validation environment.

Alternatives:
- Begin as a generic company simulator with no specific operating domain.
- Begin with multiple sports / business domains at once.
- Limit the final product permanently to esports.

Reasoning:
Esports is narrow enough for early scope control while still exercising people, contracts, finance, competition, sponsorship, reputation, audience, uncertainty, and organizational growth.

Consequences:
- Early gameplay and prototypes should be validated through one esports discipline.
- Future expansion must not justify implementing many industries early.
- Esports-specific systems may exist, but should not force the entire future company model to become esports-only.

Affected Systems:
- product scope
- competition
- people
- contracts
- finance
- sponsorship
- reputation
- future expansion

---

### DEC-002 — Player Role and Authority
Date: 2026-10-03
Status: Accepted

Context:
The project could become an esports GM game, CEO simulator, tactical coach game, or a mixture of all three unless the player's authority is bounded.

Decision:
The player primarily acts as the organization's CEO / executive while remaining directly involved in important management decisions.

Primary control layers:
- executive
- management

Operational control is secondary and should usually be abstracted, simplified, or delegated when it does not create meaningful strategic value.

Alternatives:
- Pure executive simulation.
- Pure roster / general-manager simulation.
- Operational micromanagement as the primary experience.

Reasoning:
Executive + management control supports the intended company fantasy while preserving meaningful involvement in roster, staff, contracts, sponsors, scouting direction, preparation priorities, and departmental resource allocation.

Consequences:
- UI and future systems should prioritize high-value decisions over daily chores.
- Growth should increase the importance of delegation.
- Operational detail requires explicit gameplay justification before being added.

Affected Systems:
- player role
- UI / information design
- delegation
- people
- competition
- finance
- organization

---

### DEC-003 — Company Success as Strategic Center
Date: 2026-10-03
Status: Accepted

Context:
The project needs to clarify whether winning competitions is the final objective or one major component of a broader company strategy.

Decision:
Long-term company success is the primary strategic objective. Competitive performance remains a major priority because it can generate business leverage and future competitive capacity.

Competitive success may create:
- reputation
- fandom / audience
- sponsor value
- commercial opportunities
- brand value
- talent attraction
- partnership opportunities

Short-term competitive performance may be sacrificed when doing so is strategically justified by stronger long-term company outcomes.

Alternatives:
- Winning as the sole dominant success condition.
- Competition as a minor background activity.

Reasoning:
This preserves esports importance while making competition interact with finance, brand, audience, people, risk, and expansion rather than becoming an isolated match-results loop.

Consequences:
- Competitive results should feed into company systems.
- Business decisions may legitimately conflict with short-term sporting performance.
- The economy must prevent automatic infinite growth from repeated success.

Affected Systems:
- competition
- finance
- reputation
- audience / fandom
- sponsorship
- brand
- progression
- risk

---

### DEC-004 — Scalable Competitive Control
Date: 2026-10-03
Status: Accepted

Context:
Competitive management needs enough depth to matter without forcing every player to micromanage every match, especially as the company grows.

Decision:
Competitive management should support both:
1. direct player involvement in meaningful preparation / tactical decisions; and
2. delegation to coaches and competitive staff operating within assigned authority.

The player may accept, modify, override, or delegate recommendations and decisions.

Alternatives:
- Fully abstract match resolution.
- Mandatory deep manual coaching for every match.
- Fully automated competitive operations with no meaningful player intervention.

Reasoning:
This keeps competitive gameplay available to players who want depth while allowing organizational scale and delegation to become part of management gameplay.

Consequences:
- Coach / manager capability and information quality may become meaningful simulation inputs.
- Different esports disciplines may later expose different domain-specific tactical interfaces.
- Exact tactical granularity and match simulation mechanics remain a later specification problem.

Affected Systems:
- competition
- staff
- delegation
- people AI
- information / analytics
- UI

---

### DEC-005 — Simulation Philosophy
Date: 2026-10-03
Status: Accepted

Context:
A management simulation can become either too shallow to sustain decisions or too detailed to remain readable and maintainable.

Decision:
Use medium simulation depth by default, with selective deeper simulation only where added detail creates meaningful strategic decisions, emergent behavior, useful uncertainty, or important long-term consequences.

Design rule:
**Deep where decisions matter. Simple where detail becomes repetitive work.**

Alternatives:
- Accessible abstraction across nearly all systems.
- Heavy simulation fidelity across most systems.

Reasoning:
This balances depth with clarity, scalability, balance, maintainability, UI readability, and AI-assisted development consistency.

Consequences:
- Real-world detail is not sufficient justification for simulation detail.
- Low-value operational detail should be abstracted, simplified, or delegated.
- Deeper systems require explicit gameplay value and later validation.

Affected Systems:
- all simulation systems
- UI / UX
- balancing
- content
- AI-assisted development

---

### DEC-006 — Evolving Rival Organizations
Date: 2026-10-03
Status: Accepted

Context:
Long campaigns need pressure and change that do not depend only on the player's own growth or scripted events.

Decision:
The world should contain multiple rival organizations that evolve over time and compete with the player across relevant competitive and business dimensions.

Rivals may change teams, leadership, strategy, sponsors, markets, staff, and organizational direction as appropriate to the supported simulation depth.

Alternatives:
- Static background competitors.
- Player-only economy with externally generated match opponents.

Reasoning:
Evolving rivals support adaptation, market pressure, talent competition, sponsor competition, and a world that feels active without requiring every situation to be manually scripted.

Consequences:
- Competitor state must eventually persist and change over time.
- World simulation needs bounded but meaningful rival behavior.
- Exact world size and competitor-AI fidelity remain unspecified.

Affected Systems:
- world simulation
- competition
- talent market
- staff market
- sponsorship
- audience / brand
- commercial opportunities

---

### DEC-007 — Structural Failure and Recovery
Date: 2026-10-03
Status: Accepted

Context:
If poor strategic and financial decisions cannot create serious consequences, debt, risk, and organizational trade-offs lose weight.

Decision:
The company can face genuine structural failure. Financial distress should generally create recovery and restructuring gameplay before terminal game over when practical.

Potential structural failure sources include insolvency, sustained inability to meet obligations, or failure to recover from severe loss of company viability.

Debt is a strategic instrument and must create repayment pressure, risk, and reduced flexibility.

Alternatives:
- No terminal failure; setbacks only slow progression.
- Immediate game over when a financial threshold is crossed.

Reasoning:
Real downside preserves strategic tension while a recovery window creates more gameplay than abrupt failure.

Consequences:
- Finance must eventually model obligations and distress states.
- Downsizing, asset / contract sales, refinancing, sponsor loss, or restructuring may become recovery tools.
- Exact insolvency, debt, valuation, and termination formulas remain unresolved.

Affected Systems:
- finance
- debt
- valuation
- contracts
- staff
- sponsors
- reputation
- progression / campaign state

---

### DEC-008 — Long-Term Company-First Identity
Date: 2026-10-03
Status: Accepted

Context:
The project needs to determine whether esports is the permanent product boundary or the first business domain of a broader strategic-company simulator.

Decision:
The long-term product is company-first. Esports is the starting business domain, not the permanent boundary.

Future business divisions may include other sports, media, advertising, events, merchandising, talent management, partnerships, investments, or other strategically justified domains.

Future divisions should reuse shared company concepts where appropriate and add only domain-specific rules needed to create new strategic decisions.

Alternatives:
- Esports-first forever.
- Multi-title esports ecosystem only.

Reasoning:
The desired long-term fantasy is to build, operate, adapt, and protect a growing multi-division company while preserving a narrow early implementation path.

Consequences:
- Architecture may later support a shared company core plus domain modules, but this does not authorize premature implementation of future industries.
- New divisions must create new strategic decisions rather than act as passive revenue multipliers.
- The first playable product remains deliberately esports-focused.

Affected Systems:
- product identity
- company core
- expansion
- finance
- organization
- future domain modules

---

### DEC-009 — Time Progression Model
Date: 2026-10-03
Status: Accepted

Context:
The simulation needs a common chronology for competition, contracts, finance, preparation, rivals, and delayed consequences without turning the player experience into daily micromanagement.

Decision:
Use one calendar day as the authoritative smallest simulation tick. Player-facing progression is organized around meaningful checkpoints and material decisions. Multiple days may advance automatically when no executive- or management-relevant interruption is required.

Alternatives:
- Weekly-only simulation ticks.
- Event-only time with no shared daily chronology.
- Mandatory manual advancement and decisions every day.

Reasoning:
A daily chronology is fine-grained enough for obligations and competition timing while checkpoint-driven interaction preserves the executive-management fantasy.

Consequences:
- Daily simulation does not imply daily player micromanagement.
- Time advancement must be interruptible by meaningful checkpoints.
- Final season pacing remains a later balance question.

Affected Systems:
- core loop
- finance
- contracts
- competition
- preparation
- rivals / world simulation
- delayed consequences

---

### DEC-010 — Authoritative Company State and World State Ownership
Date: 2026-10-03
Status: Accepted

Context:
Interacting systems require explicit state ownership to avoid duplicated authority and simulation drift.

Decision:
Company State is authoritative for the player-controlled company and its owned entities. World State is authoritative for external organizations, markets, competition environment, and other external simulation state. Each durable fact should have one authoritative owner.

Alternatives:
- Shared mutable state with no explicit ownership.
- Independent copies of the same facts inside each subsystem.

Reasoning:
Explicit ownership improves explainability, testing, and later technical design while reducing simulation coupling.

Consequences:
- Systems may consume views of state they do not own.
- Derived values must not silently become competing sources of truth.
- No third overlapping top-level authoritative state should be introduced without Director review.

Affected Systems:
- company simulation
- world simulation
- competition
- economy
- people
- contracts
- testing

---

### DEC-011 — Primary Resource Model
Date: 2026-10-03
Status: Accepted

Context:
The first simulation needs a small set of decision-relevant resources that support company strategy without creating a dashboard of redundant currencies.

Decision:
Use five primary resource concepts for the initial simulation model:
- Cash
- Organizational Capacity
- Reputation
- Audience / Fandom
- Information Quality

Commercial Value, Financial Pressure, Strategic Flexibility, Company Value, Competitive Capability, and Preparation Capacity are derived concepts rather than additional primary spendable resource pools.

Alternatives:
- Money as the only meaningful resource.
- A larger set of independent meters for every useful concept.

Reasoning:
The five-resource model captures liquidity, organizational limits, external standing, market attention, and uncertainty while keeping the model understandable.

Consequences:
- Derived concepts may still be visible as estimates, warnings, or categories.
- Organizational Capacity must not become a generic mana/action-point currency.
- Information Quality may have bounded domains rather than one universal scalar.

Affected Systems:
- economy
- organization
- reputation
- audience
- information / scouting
- competition
- sponsorship

---

### DEC-012 — Stored vs Derived Simulation State Rule
Date: 2026-10-03
Status: Accepted

Context:
The simulation contains many aggregate concepts that can be calculated from more fundamental causes.

Decision:
Store durable causes and authoritative facts. Derive aggregate interpretations whenever practical.

Examples of derived concepts include Commercial Value, Financial Pressure, Strategic Flexibility, Company Value, Competitive Capability, Preparation Capacity, decision-specific confidence, and overload severity.

Alternatives:
- Persist every displayed value independently.
- Recompute every state including durable commitments from transient formulas.

Reasoning:
This minimizes duplicate authority and makes causal behavior easier to test and explain.

Consequences:
- Persistence of a derived concept requires explicit justification.
- Historical snapshots may be stored for reporting without becoming the authoritative current value.

Affected Systems:
- all simulation systems
- debugging
- save/load requirements later
- analytics / UI later

---

### DEC-013 — Canonical Simulation Cycle and Consequence Timing
Date: 2026-10-03
Status: Accepted

Context:
The Product Foundation proposed a management loop that Phase 2 refined into an executable simulation rhythm.

Decision:
Use the canonical management cycle:
Observe → Prioritize → Decide → Commit → Delegate / Intervene → Advance Time → Resolve → Review → Adapt.

The simulation must support both immediate and delayed consequences. Delayed effects should remain traceable to their originating decisions or events.

Alternatives:
- Immediate resolution of all consequences.
- Separate disconnected loops for economy and competition.

Reasoning:
Delayed and cross-system consequences create long-horizon trade-offs while the common cycle keeps the game coherent.

Consequences:
- Review/debug feedback must explain material state changes.
- Systems must share chronology without becoming disconnected mini-games.

Affected Systems:
- core loop
- economy
- competition
- organization
- world simulation
- feedback / observability

---

### DEC-014 — Bounded Delegation Through Authority Envelopes
Date: 2026-10-03
Status: Accepted

Context:
The player is an executive with selective hands-on authority, so operational delegation must reduce burden without removing accountability.

Decision:
Delegated domains use a bounded Authority Envelope. A supported domain may be Player Controlled, Coach/Staff Recommends and Player Approves, or Coach/Staff Autonomous. Staff operate from the same underlying simulation state as manual control, subject to their information, capability, tendencies, and explicit constraints.

Alternatives:
- Binary AI on/off delegation.
- Mandatory manual control.
- Omniscient automation with hidden bonuses.

Reasoning:
Bounded authority makes delegation itself a management decision and supports organizational scale.

Consequences:
- Manual control receives no hidden performance bonus.
- Delegated staff must not receive hidden omniscience.
- The first prototype requires one meaningful Head Coach role; deeper staff hierarchy is deferred.

Affected Systems:
- staff
- competition
- delegation
- information
- player authority

---

### DEC-015 — Bounded Rival Simulation
Date: 2026-10-03
Status: Accepted

Context:
Rivals must create real external pressure, but simulating every rival at player-company fidelity would violate early scope and fidelity goals.

Decision:
World State owns rival organizations. Rivals evolve and compete for relevant scarce opportunities, but may use a compressed lower-fidelity model than the player company.

Alternatives:
- Static rivals used only as match opponents.
- Full player-equivalent company simulation for every rival.

Reasoning:
A bounded model is sufficient to create competitive, talent, sponsor, and opportunity pressure while protecting prototype scope.

Consequences:
- Rival state must materially affect at least some player decisions.
- Rival fidelity should be removed or reduced when variables do not create observable pressure.

Affected Systems:
- world simulation
- competition
- talent market
- sponsorship
- information

---

### DEC-016 — Structural Failure State Progression
Date: 2026-10-03
Status: Accepted

Context:
The project already requires genuine failure with recovery gameplay before terminal failure where practical.

Decision:
Use the conceptual progression:
Stable → Warning → Distress → Restructuring → Stabilized or Terminal.

Structural failure is evaluated from company conditions such as liquidity, obligations, leverage, recurring costs, expected inflows, and credible recovery options rather than from a single match result or a single arbitrary negative number.

Alternatives:
- Instant bankruptcy at one threshold.
- No structural failure.

Reasoning:
A staged model creates earlier decisions, costly recovery, and clearer causality.

Consequences:
- Exact insolvency and terminal game-over thresholds remain unresolved until prototype evidence exists.
- Recovery actions must sacrifice another valuable capability or opportunity.

Affected Systems:
- finance
- debt
- contracts
- roster / staff
- sponsorship
- progression

---

### DEC-017 — Cross-System Outcome Boundary
Date: 2026-10-03
Status: Accepted

Context:
Direct cross-system mutation would create unclear ownership and fragile simulation coupling.

Decision:
Systems emit domain outcomes/signals that owning systems interpret. They do not directly mutate authoritative state owned by another domain.

Competition therefore emits a CompetitiveOutcome containing consequence-relevant context. Company/economy/reputation/audience/commercial processes interpret that outcome and apply their own state changes.

Alternatives:
- Competition directly changes Cash, Reputation, Audience, sponsor value, and Company Value.

Reasoning:
Outcome boundaries preserve ownership, explainability, testing, and later architecture flexibility.

Consequences:
- Cross-system interfaces need explicit producer/consumer responsibility.
- Duplicate application of the same causal effect must be prevented.

Affected Systems:
- competition
- economy
- reputation
- audience
- sponsorship
- debugging

---

### DEC-018 — Preparation and Information Integration
Date: 2026-10-03
Status: Accepted

Context:
Competitive preparation and imperfect information need to connect to company resources without introducing redundant primary currencies.

Decision:
Preparation Capacity is a derived short-horizon capability based on factors such as available preparation time, relevant staff capability, information, and organizational pressure. It is allocated among Team Execution, Opponent-Specific Preparation, and Meta Adaptation.

Information capability is domain-aware. Subsystems consume decision-specific estimates and confidence/uncertainty rather than omniscient hidden truth. Better information improves decision quality, not the underlying asset or opponent strength.

Alternatives:
- Preparation Points as a sixth primary resource.
- One global information accuracy buff.

Reasoning:
This preserves scarcity and uncertainty while keeping primary resource count bounded.

Consequences:
- The three preparation priorities must create real opportunity cost or be simplified after testing.
- Information domains that do not change decisions should be merged or removed.

Affected Systems:
- competition
- information / scouting
- staff
- organizational capacity
- preparation

---

### DEC-019 — Bounded Competitive Resolution for the Initial Simulation
Date: 2026-10-03
Status: Accepted

Context:
Competition needs enough depth to affect company decisions without becoming a full tactical esports simulator.

Decision:
Resolve competition using the conceptual dimensions:
- Base Competitive Capability
- Preparation Advantage
- Strategic Matchup
- Adaptation
- bounded competitive variance

The first simulation supports high-leverage preparation, lineup exceptions, strategic posture, and coach delegation. A single Head Coach is the only mandatory deep competitive staff role.

Alternatives:
- Pure roster-rating roll.
- Full action-level or tactical match simulator.

Reasoning:
The model makes roster quality matter while preserving value for preparation, information, coaching, matchup, and adaptation.

Consequences:
- Stronger teams are advantaged but not guaranteed to win.
- Exact coefficients and balance are prototype calibration, not canonical production rules.
- Detailed drafting, action simulation, deep analysts, and repeated mid-match intervention are outside the initial prototype requirement.

Affected Systems:
- competition
- roster
- Head Coach
- information
- preparation
- rivals

---

### DEC-020 — Minimal Simulation Prototype Validation Envelope
Date: 2026-10-03
Status: Accepted

Context:
The project now needs a bounded headless/minimal-presentation prototype to test the management model before Technical Foundation.

Decision:
Authorize a prototype validation envelope consisting of:
- 1 player-controlled company;
- 1 fictional 5v5 role-based esports test discipline;
- 1 primary team with 5 starters + 1 flex substitute;
- 1 Head Coach;
- 4 bounded rival organizations;
- a tiny talent market;
- 1 active sponsor plus a bounded sponsor-opportunity pool;
- 1 simple financing mechanism;
- a default integrated-run horizon of up to 84 calendar days;
- a 5-organization competition cycle using a double round-robin, with the proposed small playoff layer permitted if needed for validation.

All numeric starting values, coefficient values, Currency Units, exact match counts, and exact duration are prototype calibration parameters, not production balance decisions. They may be reduced or tuned when a smaller test produces equivalent evidence.

Alternatives:
- Begin production vertical-slice implementation immediately.
- Use a broad multi-discipline or multi-team prototype.
- Delay all simulation validation until after production architecture is chosen.

Reasoning:
The envelope is large enough to test competition-business feedback, delegation, information uncertainty, rival adaptation, financial commitments, overload, debt, and recovery while remaining intentionally disposable and bounded.

Consequences:
- The fictional discipline is a test abstraction, not a production content commitment.
- Prototype implementation must remain headless or minimal-presentation and deterministic/testable.
- Passing the prototype gate validates the management model, not final balance or production architecture.
- Technical Foundation begins only after prototype BUILD/VERIFY evidence is reviewed by Project Director.

Affected Systems:
- prototype scope
- simulation
- competition
- economy
- testing
- project roadmap

---

### DEC-021 — Minimal Simulation Prototype Gate Passed
Date: 2026-10-03
Status: Accepted

Context:
Phase 3 implemented the approved deterministic headless prototype and returned measured BUILD / VERIFY evidence for scenarios A–M and acceptance criteria AC-01 through AC-13. Project Director reviewed the implementation, verification reports, regression tests, representative traces, reproducibility evidence, and stated limitations.

Decision:
Close Phase 3 — Headless / Minimal Prototype with **PASS PROTOTYPE GATE** and authorize **Phase 4 — Technical Foundation**.

The pass means the bounded prototype provides sufficient evidence that the core company-management thesis can create meaningful, context-dependent, explainable decisions through interacting competition, economy, information, delegation, rival, organizational-pressure, and recovery systems.

The pass does **not** approve the prototype implementation, numerical calibration, fictional discipline, or balance values as production canon.

Alternatives:
- ITERATE Phase 3 before opening Technical Foundation.
- STOP / REDESIGN the core management model.
- Begin the production Vertical Slice immediately without a Technical Foundation phase.

Reasoning:
The final Phase 3 evidence reports:
- scenarios A–M PASS;
- AC-01 through AC-13 PASS;
- 32/32 automated regression tests PASS;
- 1,664/1,664 repeated seeded scenario pairs with matching final-state hashes;
- 13/13 representative full-trace replays matched;
- manual/delegated parity across 128/128 paired verification runs while delegation reduced approval burden.

The remaining limitations concern bounded-horizon balance, market breadth, underused information/coach fields, recovery breadth, and production engineering. They do not invalidate the core prototype thesis and are more appropriately handled during Technical Foundation, Vertical Slice design, or later validation.

Consequences:
- Phase 3 is closed.
- Phase 4 PLAN / SPEC is authorized.
- Engine / framework and production architecture may now be evaluated.
- Production Vertical Slice BUILD is still blocked until the Phase 4 exit gate passes.
- Prototype code under `prototypes/` remains a validation reference and test asset, not automatic production architecture.
- Prototype coefficients, Currency Units, exact 84-day horizon, exact rival count, exact match count, and fictional 5v5 test discipline remain noncanonical.
- Validated concepts such as explicit state ownership, deterministic testing, cross-system outcome boundaries, and causal observability should inform Phase 4 unless a documented technical/design reason justifies a change.

Affected Systems:
- project roadmap
- technical architecture
- engine / framework selection
- simulation architecture
- persistence / save-load
- data model
- testing / tooling
- UI technical foundation
- prototype disposition

---

### DEC-022 — Production Technical Foundation Accepted
Date: 2026-10-04
Status: Accepted
Approval authority: Project Director/user's explicit final Phase 4 closure instruction.

Context:
Phase 4 produced architecture specifications and bounded qualification evidence, reviewed at `258be419a0731a71cc7141ab1907c79b444424b5`. QG-03 and QG-04 passed within their documented local fixture scope. QG-01 lacks clean-machine and external-debugger evidence; QG-02 missed its original uniform 16.7 ms frame budget. Neither exposed a fundamental architecture or engine blocker. Native Godot Tree and bounded row reuse provide a tractable implementation path without a large custom table framework.

Decision:
Accept the Phase 4 production technical foundation with **PASS WITH DEFERRED VALIDATION OBLIGATIONS**. Close **Phase 4 — Technical Foundation**, then authorize **Phase 5 — Vertical Slice BUILD**.

Adopt:
- Godot .NET + C# as the production technical foundation; accept ADR-TF-001 under the conditions in [Phase 4 closure](../09-technical-foundation/PHASE4_CLOSURE.md).
- ADR-TF-002: modular monolith, engine-independent C# Domain/Application, and Godot presentation/runtime host.
- Company/World gameplay authority and explicit outcome boundaries. The technical execution envelope is not a third gameplay authority.
- Versioned bounded save compatibility, deterministic/headless testing and data-driven content foundation.
- Godot 4.7.2 stable mono, matching 4.7.2 mono export templates, .NET SDK 10.0.401, runtime 10.0.12 and `net10.0` as the initial **Phase 5 development pin**, for reproducibility rather than a permanent release-support promise. Future engine/runtime upgrades require targeted requalification.
- Workflow-specific Phase 5 engineering budgets: approximately 60 Hz / 16.7 ms for continuous scrolling/direct navigation where practical; provisional p95 frame work around 33.3 ms for discrete rebind/page/sort/filter/layout transitions; <=100 ms p95 ordinary-action acknowledgement without deliberate debounce; query p95 <=250 ms at 10k and <=1 s at 100k. Measure intentional search debounce separately from processing latency. These are future engineering budgets, not retrospective qualification passes.
- Phase 3 Python prototype as behavioral reference/test oracle only, without wholesale porting or promotion of its calibration to production canon.

Deferred validation:
- QG-01 clean Windows execution becomes a **DEFERRED DISTRIBUTION GATE**, required before external tester distribution, public demo, release candidate or a self-contained supported-clean-Windows claim.
- External debugger workflow becomes **DEFERRED DEVELOPER-TOOLING VALIDATION**, not a Phase 4 exit blocker unless development becomes impractical.
- Physical keyboard/mouse and Windows DPI/mixed-monitor validation must pass before the Vertical Slice UI quality gate.
- Target/minimum hardware definition and workflow performance validation occur during Vertical Slice; second-machine determinism, long-campaign scale, production save migration policy and UI regression monitoring remain explicit obligations.
- The [closure register DV-01 through DV-10](../09-technical-foundation/PHASE4_CLOSURE.md#risks-carried-forward-and-deferred-validation-gates) assigns owners, evidence and future gates. Obligations remain visible until evidence and Director disposition close them.

Alternatives:
- Keep Phase 4 open until every deployment, tooling and performance uncertainty is eliminated.
- Switch engine despite no demonstrated architectural blocker or disproportionate UI infrastructure requirement.
- Treat local qualification as proof of clean-machine support or rewrite historical failures as passes.

Reasoning:
The selected architecture is coherent, no unresolved architectural blocker is demonstrated, implementation risk is bounded and understood, and remaining evidence can be required at more appropriate gates. The project can safely begin bounded production work without claiming that unresolved validation is complete. Unavailable ideal hardware alone does not justify indefinite foundation optimization.

Consequences:
- Phase 4 closes; Phase 5 opens only after this accepted closure, satisfying DEC-021's exit dependency.
- QG-01 remains historically INCONCLUSIVE; QG-02 remains historically FAIL against 16.7 ms; QG-03/QG-04 retain bounded PASS results. No qualification evidence is deleted or rewritten.
- This decision explicitly supersedes earlier review-time recommendations to keep Phase 4 open, Phase 5 unauthorized and the runtime pin qualification-only. The development pin does not settle supported release OS/runtime or distribution prerequisites.
- Godot remains revisable if later evidence demonstrates disproportionate UI infrastructure cost or another major technical failure; Unity remains the fallback requiring a documented Director decision.
- Development authorization does not waive distribution/UI/performance/save gates or broaden gameplay scope. No clean-machine support, universal 60 FPS or solved production migration policy is claimed.
- This closure task commits documentation/governance only; no Vertical Slice implementation starts in this task.

Affected Systems:
- project state and roadmap
- engine / framework and development toolchain
- architecture, state ownership and outcome boundaries
- UI performance and validation
- deterministic testing, persistence and content
- distribution and release gates
- prototype disposition

---

### DEC-023 — Company-First UX Architecture
Date: 2026-10-04
Status: Proposed
Approval owner: Project Director.
Authority: Director's bounded Phase 5 UX foundation brief authorizes PLAN / SPEC
and a draft decision. Detailed architecture acceptance is not inferred.

Context:
DEC-003/008 already establish company success and company-first long-term identity.
The current Phase 5 client presents dashboard/team/function tabs and lacks a full
player-defined organization identity contract. Future presentation needs durable
company context without expanding the current gameplay envelope.

Proposed decision:
- The player-controlled company is the highest presentation context; competitive
  teams are owned operating units.
- Organization name, abbreviation, emblem reference and primary/secondary identity
  colors are configurable campaign data exposed through approved read models.
  The game prescribes no canonical player-company name, logo or color scheme.
- Corporate functions and operating portfolios are distinct, connected axes.
  Esports is the first operating domain, not the permanent presentation root.
- Primary UX uses company context, situations, contextual workspaces,
  entities/documents, affairs/time and meaningful commitments. Tables and charts
  remain tools; dashboards/card grids do not define every domain.
- Shared semantic tokens, components and reviewed screen blueprints govern UI.
  Player branding cannot replace system status, focus or readability semantics.
- Production workflows work with text/symbols and do not require bespoke character
  art or AI-generated portraits. Final aesthetic choices require separate review.
- Existing Domain/Application, gameplay, deterministic execution, ownership,
  outcomes and save/load remain valid unless separately superseded. Current UI
  remains internal until each replacement is verified.

Alternatives:
- Retain dashboard/tab navigation as the permanent product identity.
- Require an illustrated or 3D headquarters.
- Build all future corporate modules or a generic UI/graph framework immediately.

Reasoning:
The proposed model expresses accepted CEO/company-first direction while preserving
one-company/one-development-discipline/one-primary-team Phase 5 scope. Reusable
semantics and optional art are practical for one developer and AI-assisted work.

Consequences:
If accepted, the [UX foundation package](../10-vertical-slice/ui-ux-foundation/README.md)
governs subsequent presentation blueprints. Identity/read-model gaps, fixture
renaming and versioned persistence effects require bounded follow-up implementation.
No future division, final balance, full branding editor, UI demo or production
redesign is implemented by this documentation task. This proposal supersedes no
accepted decision, reopens no Phase 4 gate and changes no QG result or DV obligation.
Migration/reversal cost is contained by retaining the internal client and unchanged
Application commands until presentation replacements are verified.

Affected Systems:
- product information architecture and navigation
- company identity presentation and future campaign initialization/DTO extension
- UI design tokens, component contracts and AI implementation workflow
- affairs, documents, decision workspaces and visual verification
- incremental Phase 5 presentation migration

