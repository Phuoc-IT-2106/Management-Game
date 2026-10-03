# Outcome

**Status: PROPOSAL — Prototype Specification**

A complete specification is defined for a **headless or minimal-presentation simulation prototype** whose purpose is to validate the strategic management model before Technical Architecture.

The prototype preserves the established product direction: company-first management, executive + management authority, esports as the initial validation domain, medium simulation depth, optional delegation, meaningful financial failure, evolving rivals, and narrow early scope. These directions are consistent with the Product Foundation, which places the strategic center at the company rather than at tactical match control.

The prototype is deliberately narrower than a vertical slice. The roadmap defines this phase as a headless/minimal prototype whose exit signal is repeated meaningful decisions without scripted novelty.

### Source-governance note

**FACT**

The currently supplied `PROJECT_STATE.md` still identifies the project as Discovery and states that no major decisions are locked.

**FACT**

The supplied `DECISION_LOG.md` contains only DEC-001 and marks it `Proposed`, not `Accepted`.

**ASSUMPTION — for this specification only**

The directions explicitly supplied in the Minimal Simulation Prototype handoff are treated as inherited working constraints because they represent the latest phase handoff supplied to this specialist.

They are **not** silently written back as canonical decisions.

**BLOCKING GOVERNANCE ACTION**

Before prototype implementation, Project Director should synchronize `DECISION_LOG.md`, `PROJECT_STATE.md`, and any accepted Phase 2 outputs so the implementation team has one unambiguous source of truth.

---

# Prototype validation target

**DECISION — inherited**

The prototype tests one primary question:

> Does the simulation repeatedly create meaningful management decisions where the preferred choice changes with company state, competitive conditions, information, commitments, rivals, and risk, without relying mainly on scripted events?

The accepted management rhythm remains:

> Observe → Prioritize → Decide → Commit → Delegate / Intervene → Advance Time → Resolve → Review → Adapt

This is aligned with the established Product Foundation loop.

### What the prototype must prove

**PROPOSAL**

The prototype must provide evidence that:

1. Competition and company management form one connected decision system.
2. Competitive success creates business leverage but does not automatically create money.
3. Financial commitments can make an otherwise strong competitive decision strategically dangerous.
4. Imperfect information changes what a reasonable decision looks like.
5. Delegation reduces operational burden without becoming a universally superior choice.
6. Rival adaptation can make a previously effective strategy less attractive.
7. Organizational growth can create capacity pressure.
8. Distress creates sacrifice-based recovery decisions before terminal failure.
9. Delayed consequences materially affect later choices.
10. At least several distinct strategic approaches remain viable under different conditions.

### What the prototype is NOT intended to prove

**DECISION — inherited**

It does not validate:

- final balance;
- final campaign pacing;
- final insolvency thresholds;
- production UI/UX;
- engine choice;
- production architecture;
- save/load architecture;
- multiplayer;
- modding;
- multiple esports disciplines;
- multiple player teams;
- full tactical match simulation;
- long-term non-esports expansion;
- detailed facilities;
- realistic accounting;
- deep relationship simulation;
- a final content pipeline.

The Product Foundation already excludes a full tactical simulator, detailed accounting, daily-task micromanagement, global economy simulation and broad early implementation.

---

# Included scope

**PROPOSAL**

### World size

The prototype contains:

- 1 player-controlled company;
- 1 esports discipline;
- 1 primary team;
- 6 contracted players: 5 starters + 1 flex substitute;
- 1 Head Coach;
- 4 rival organizations;
- a tiny talent market;
- one active sponsor relationship;
- a bounded sponsor-opportunity pool;
- one competition season;
- one financing mechanism;
- one information-capability investment mechanism.

Total competitive ecosystem: **5 organizations**.

Four rivals are sufficient to create opponent variation and external pressure while remaining small enough to trace manually.

### Prototype duration

**PROPOSAL**

Authoritative simulation duration:

**84 calendar days / 12 weeks.**

The simulation advances internally one day at a time.

The player normally interacts only when a meaningful checkpoint occurs.

The 84-day horizon is long enough to test:

- repeated competition;
- preparation adaptation;
- several financial settlements;
- sponsor consequences;
- strategy exposure;
- rival adaptation;
- organizational load;
- one possible debt cycle;
- financial distress;
- restructuring;
- delayed reputation/audience effects.

It is not intended to represent final season length.

### Competition structure

**PROPOSAL**

Use:

- 5 organizations;
- double round-robin against 4 rivals = 8 regular matches;
- top-four playoff;
- semifinal + final where applicable.

Maximum player-company matches:

**10.**

This is enough repetition to detect whether the same competitive decisions remain dominant.

### Esports discipline archetype

**PROPOSAL — requires Project Director approval**

Use one fictional discipline provisionally called:

**5v5 Role Arena**

This is not a proposed production esports title.

It exists only to provide the minimum properties required by the simulation.

Essential assumptions:

- teams field 5 active players;
- each competitive slot has a distinct role;
- each player has primary/secondary role proficiency;
- teams may use one substitute;
- role coverage matters;
- preparation matters;
- strategic posture matters;
- opponent-specific knowledge matters;
- meta conditions can change which capabilities are valuable;
- match outcomes remain probabilistic.

Role slots may remain abstract as:

`Role A / B / C / D / E`

during the headless prototype.

No draft system, item system, map simulation, action economy, mechanical execution simulator, or domain-specific tactical rules are required.

### Strategic posture

**DECISION — inherited constraint**

Use the minimum three options:

- Conservative;
- Balanced;
- Aggressive.

These influence matchup and variance but are not deterministic rock-paper-scissors counters.

### Initial state

**PROPOSAL — calibration seed, not final balance**

All numerical values below are prototype-scale test values.

Primary normalized attributes use `0–100` where helpful.

#### Player company

Starting Cash: **420 CU**.

`CU` means prototype Currency Unit and has no production-world denomination.

Starting:

- Reputation: 45;
- Audience/Fandom: 35;
- Talent Information Quality: 50;
- Opponent Information Quality: 45;
- Commercial Information Quality: 50;
- organizational capacity input baseline: 100;
- Organizational Load: 74;
- financial distress stage: Stable.

#### Roster

| Player | Primary role | Execution | Adaptability | Consistency | Readiness |
|---|---|---:|---:|---:|---:|
| P1 | A | 67 | 55 | 72 | 92 |
| P2 | B | 64 | 63 | 66 | 90 |
| P3 | C | 69 | 58 | 61 | 88 |
| P4 | D | 63 | 72 | 64 | 93 |
| P5 | E | 66 | 60 | 70 | 91 |
| P6 | Flex B/D | 59 | 76 | 62 | 95 |

These numbers are merely sufficient to create lineup and role-coverage decisions.

#### Head Coach

Coach C1:

- preparation capability: 68;
- opponent analysis: 64;
- strategic judgment: 63;
- lineup judgment: 67;
- adaptability: 61;
- delegation reliability: 70;
- default tendency: Balanced.

#### Active sponsor

One moderate-fit sponsor agreement exists at prototype start.

It contains:

- scheduled guaranteed payments;
- a performance-related condition;
- a reputation/visibility expectation;
- a fixed expiry inside the prototype horizon.

Sponsor money is received only according to contract terms.

Competitive victories themselves do not directly generate sponsor cash.

#### Scheduled finances

Starting schedule should contain approximately:

- three major payroll/operating settlements;
- three sponsor receivables;
- contractual roster obligations;
- optional discretionary investment opportunities.

Exact amounts remain tuning variables.

The calibration should place the starting company in a position where:

- conservative operation is sustainable;
- one aggressive investment is affordable but materially risky;
- two simultaneous aggressive commitments are likely to require financing or cuts.

#### Rival organizations

**R1 — Contender**

- strongest starting roster;
- strong coach;
- high opponent-information capability;
- adapts quickly to exposed strategies.

**R2 — Aggressor**

- upper-middle roster;
- aggressive competitive tendency;
- weaker organizational stability;
- medium adaptation.

**R3 — Developer**

- weaker initial roster;
- improving talent;
- good adaptability;
- lower commercial pressure.

**R4 — Stable**

- middle-strength roster;
- conservative strategy;
- strong financial stability;
- consistent but less explosive performance.

Rivals do not require player-equivalent financial or personnel simulation.

They only require sufficient state to create:

- competitive pressure;
- talent competition;
- sponsor scarcity;
- strategic adaptation.

### Minimal talent market

**PROPOSAL**

Maintain no more than 2–3 meaningful candidates simultaneously.

At prototype start or an early checkpoint:

**Star S**

- clearly stronger execution;
- expensive acquisition;
- expensive recurring commitment;
- relatively known information.

**Prospect T**

- lower immediate strength;
- inexpensive;
- more uncertain evaluation;
- good potential role flexibility.

This is sufficient to test star investment versus flexibility and information uncertainty.

---

# Excluded scope

**DECISION — inherited**

The prototype excludes:

- additional industries;
- traditional sports;
- media operations;
- advertising divisions;
- merchandise systems;
- events businesses;
- multiple esports titles;
- additional player-controlled teams;
- academy teams;
- deep facilities;
- large staff hierarchies;
- analyst characters as a separate deep subsystem;
- detailed training schedules;
- full drafting;
- action-level tactical simulation;
- mid-match tactical minigames;
- detailed player relationships;
- complete morale/personality simulation;
- realistic legal systems;
- taxation;
- accounting statements;
- transfer-market simulation at global scale;
- complex lending markets;
- M&A;
- equity financing;
- detailed valuation modeling;
- a global macroeconomy;
- large event libraries;
- scripted narrative campaign structure.

This follows the established early-scope guard of one company, one esports discipline, one primary team and a tightly bounded management system.

**DECISION — inherited**

Long-term company expansion remains outside this prototype. The project explicitly requires validation of the current management model before expansion.

---

# Prototype entities

### Company

**DECISION — inherited concept**

Represents the player-controlled business and owns authoritative Company State.

Responsibilities:

- financial position;
- owned roster/staff/contracts;
- Reputation;
- Audience/Fandom;
- organizational-capacity inputs;
- obligations and receivables;
- sponsor agreements;
- delegation settings;
- distress state;
- strategic commitments.

It does not own rival state.

### Player

**PROPOSAL**

Minimum persistent person representation for a competitive player.

Contains only attributes required for:

- role suitability;
- competitive capability;
- adaptability;
- consistency;
- readiness;
- availability;
- contractual commitment.

No deep life simulation is included.

### Head Coach

**DECISION — inherited requirement**

The single meaningful staff role in the prototype.

Responsible for producing competitive recommendations or autonomous decisions in permitted authority domains.

Relevant capabilities:

- preparation;
- opponent analysis;
- lineup judgment;
- strategic judgment;
- adaptability;
- delegation reliability.

The Product Foundation already establishes that manual competitive depth may exist while operational work is delegatable.

### Player Contract

**PROPOSAL**

Represents durable employment/roster commitment.

Contains only:

- contracted player;
- start/end;
- recurring cost;
- acquisition/release consequence where relevant;
- optional guaranteed commitment.

### Head Coach Contract

**PROPOSAL**

Same commitment concept as player contract but attached to the Head Coach.

### Sponsor Agreement

**PROPOSAL**

Represents an accepted commercial commitment.

Contains:

- guaranteed payment schedule;
- conditional payment where required;
- duration;
- fit/context;
- performance or visibility expectations;
- relevant restriction;
- termination consequence where needed.

### Rival Organization

**PROPOSAL**

Bounded World State entity.

Stores only enough durable state to affect:

- match capability;
- strategy tendency/history;
- coach capability;
- adaptation;
- talent competition;
- sponsor scarcity.

### Competition

**PROPOSAL**

Owns:

- schedule;
- standings;
- match importance;
- completed CompetitiveOutcomes;
- playoff qualification state.

It does not directly modify company finance or reputation.

### Debt Instrument

**PROPOSAL**

One simple bounded financing mechanism.

Represents:

- borrowed principal;
- financing cost;
- repayment dates;
- outstanding obligation;
- delinquency/default state if applicable.

No bond market, credit rating system, or multi-lender simulation is included.

### Sponsor Opportunity

**PROPOSAL**

A temporary World/Commercial opportunity.

It is not cash.

It represents terms that may become a Sponsor Agreement only after player acceptance.

---

# Stored state

The rule is:

> Store durable causes and authoritative facts. Derive aggregate interpretations whenever practical.

### Company-owned stored state

| Stored state | Classification | Owner | Why stored | Changed by |
|---|---|---|---|---|
| Cash | DECISION — inherited | Company | authoritative current liquidity | financial settlement |
| Scheduled receivables | DECISION — inherited | Company | future committed inflows | contracts/settlement |
| Scheduled obligations | DECISION — inherited | Company | future committed outflows | contracts/debt/settlement |
| Debt balance and repayment schedule | PROPOSAL | Company | durable financing commitment | borrowing/repayment |
| Player roster membership | DECISION — inherited | Company | authoritative ownership | roster decisions |
| Player attributes | PROPOSAL | Company-owned Player | durable competitive causes | bounded development/state changes |
| Player role proficiency | PROPOSAL | Player | lineup suitability | prototype state updates |
| Readiness | DECISION — inherited concept | Player | single authoritative source | time/state effects |
| Availability | PROPOSAL | Player | eligibility | state resolution |
| Contracts | DECISION — inherited | Company | obligations/flexibility | negotiation/sign/release |
| Head Coach | DECISION — inherited | Company | delegation/competitive capability | staffing decision |
| Coach attributes | PROPOSAL | Head Coach | recommendation causes | normally fixed during prototype |
| Reputation | DECISION — inherited | Company | persistent professional standing | consequence resolution |
| Audience/Fandom | DECISION — inherited | Company | persistent engaged attention | consequence resolution |
| Talent Information capability | PROPOSAL | Company | durable information capability | investment |
| Opponent Information capability | PROPOSAL | Company | durable information capability | investment |
| Commercial Information capability | PROPOSAL | Company | durable information capability | investment |
| Capacity-contributing inputs | PROPOSAL | Company | durable organizational ability | staffing/investment |
| Organizational Load commitments | PROPOSAL | Company | persistent workload causes | signings/programs/contracts |
| Active Sponsor Agreement | PROPOSAL | Company | durable commitment | sponsor decision |
| Financial distress stage | DECISION — inherited concept | Company | persistent structural status | distress resolution |
| Authority Envelope | DECISION — inherited | Company | defines delegated authority | player decision |
| Current lineup | PROPOSAL | Company/team | authoritative competition selection | player/coach |
| Preparation allocation | PROPOSAL | Company/team | active commitment until match | player/coach |
| Strategy history | PROPOSAL | Company/team | durable cause of exposure | completed matches |
| Company strategic posture/context | PROPOSAL | Company | explains linked decisions | executive choice where exposed |

### World-owned stored state

| Stored state | Classification | Owner | Why stored | Changed by |
|---|---|---|---|---|
| Rival roster profile | PROPOSAL | World State | external competitive capability | bounded rival evolution |
| Rival coach capability | PROPOSAL | World State | adaptation/delegation quality | world evolution |
| Rival strategy history | PROPOSAL | World State | opponent tendencies/exposure | rival matches |
| Rival recent form | PROPOSAL | World State | contextual competitive cause | competition |
| Rival standing | PROPOSAL | Competition/World | current competition fact | match outcomes |
| Rival information/adaptation capability | PROPOSAL | World State | determines response to exposure | world evolution |
| Competition schedule | PROPOSAL | Competition | authoritative event timing | fixed at initialization |
| Competition results | PROPOSAL | Competition | durable world facts | match resolution |
| Discipline meta state | PROPOSAL | World State | changing strategic environment | bounded meta update |
| Talent-market candidates | PROPOSAL | World State | opportunity scarcity | market resolution |
| Sponsor opportunities | PROPOSAL | World State | commercial opportunity scarcity | commercial resolution |
| External market modifier | ASSUMPTION | World State | minimum shared commercial context | bounded world update |

### State explicitly NOT stored

**PROPOSAL**

Do not persist independent authoritative versions of:

- Commercial Value;
- Financial Pressure;
- Strategic Flexibility;
- Company Value;
- Competitive Capability;
- Preparation Capacity;
- exact decision confidence;
- strategy exposure score;
- organizational overload severity.

These should be derived from authoritative causes unless prototype evidence demonstrates that persistence is necessary.

---

# Derived state

| Derived value | Inputs | Purpose | Player visibility |
|---|---|---|---|
| Effective Capacity | capacity inputs, active pressure modifiers | usable organizational capability | category + warning |
| Capacity Pressure | Effective Capacity vs Organizational Load | detect overload | category + trend |
| Commercial Value | Reputation, Audience, competitive relevance, sponsor fit, market conditions | sponsor quality/leverage | category or estimate |
| Financial Pressure | Cash, upcoming obligations, receivables, debt | liquidity danger | warning + short forecast |
| Strategic Flexibility | liquidity, contract commitments, debt, roster options | show future option constraint | category |
| Company Value | assets/commitments, Cash, commercial standing, risk | health/stress metric | estimate/category |
| Base Competitive Capability | lineup/player attributes, role coverage, readiness | match input | estimate |
| Preparation Capacity | available time, Coach, support/information, capacity pressure | preparation budget | category + approximate amount |
| Team Execution Preparation | preparation allocation + capability | match input | estimate |
| Opponent-Specific Preparation | allocation + opponent information + Coach | match input | confidence range |
| Meta Adaptation Preparation | allocation + meta information/Coach | match input | estimate |
| Strategic Matchup | lineup profile, posture, opponent profile, meta | match modifier | partial estimate |
| Strategy Exposure | own strategy history, repetition, visibility | anti-repetition input | warning/category |
| Rival Adaptation Effect | exposure + rival analysis/adaptation | opponent counter-preparation | uncertain estimate |
| Decision Information Confidence | relevant information capability + uncertainty | controls estimate precision | explicit confidence |
| Sponsor Opportunity Quality | Commercial Value + fit + scarcity + market conditions | determine offer quality | estimated range before offer |
| Reputation Outcome Modifier | opponent/event significance, expectation, previous reputation | diminishing returns | explanation after resolution |
| Audience Response | visibility, relevance, performance surprise, existing audience inertia | slow fan change | trend |
| Distress Risk | financial pressure, debt timing, flexibility | warning generation | warning/category |

### Information rule

**DECISION — inherited**

Information Quality affects:

- estimate width;
- confidence;
- recommendation quality;
- ability to identify matchup/market risk.

It does **not** modify the underlying execution, roster strength, opponent strength or asset value merely because the player knows more about it.

### Organizational Capacity rule

**DECISION — inherited**

Capacity is not action points.

Overload should progressively reduce process effectiveness.

Example consequences may include:

- slower information refresh;
- reduced recommendation reliability;
- reduced preparation conversion;
- weaker commercial execution;
- increased probability that secondary opportunities cannot be pursued simultaneously.

These are pressure effects from organization state, not “spend 5 Capacity to perform action.”

---

# Simulation sequence

**PROPOSAL**

One calendar day is the smallest authoritative tick.

The player does not manually process each day.

When no relevant checkpoint exists, multiple days may advance automatically.

### Daily resolution order

1. **Begin day / advance date.**
2. **Settle due financial items.**
3. **Update contract timing and availability facts.**
4. **Apply preparation progress for active competition preparation.**
5. **Apply organizational-load pressure effects.**
6. **Update bounded rival/world processes.**
7. **Update meta when a scheduled meta checkpoint occurs.**
8. **Generate decision checkpoints for unresolved material issues.**
9. **Pause advancement if executive/management input is required.**
10. **If competition is scheduled and all required decisions are resolved, resolve match.**
11. **Emit `CompetitiveOutcome`.**
12. **Translate the outcome into company/economy consequences.**
13. **Queue immediate and delayed consequences separately.**
14. **Resolve any delayed consequence whose due condition is now reached.**
15. **Recalculate derived warning/estimate state.**
16. **Evaluate distress stage.**
17. **Generate review information where meaningful.**
18. **Continue advancing until the next checkpoint.**

### Ordering rationale

Financial obligations settle before same-day competition rewards.

This prevents a team from retrospectively covering an already-due obligation with a reward that did not exist when the obligation became due.

The player must receive warnings before important due dates so this ordering is understandable rather than punitive.

### Immediate consequences

Typical immediate consequences:

- Cash payment;
- Cash receipt already contractually due;
- contract creation;
- debt creation;
- debt repayment;
- roster membership change;
- lineup change;
- Authority Envelope change;
- preparation allocation change;
- new obligation schedule;
- sponsor agreement creation.

### Delayed consequences

Typical delayed consequences:

- Reputation accumulation/decay;
- Audience/Fandom response;
- Commercial Value change through updated inputs;
- sponsor opportunities;
- rival adaptation;
- strategy exposure;
- organizational overload degradation;
- information-capability improvement after investment;
- increased/decreased future talent interest;
- distress escalation after repeated unresolved pressure.

### Competition resolution

**PROPOSAL**

Conceptually:

`Base Competitive Capability`
`+ Preparation Advantage`
`+ Strategic Matchup`
`+ Adaptation Effect`
`+ bounded execution variance`
`→ Competitive Result`

#### Base Competitive Capability

Derived from:

- player execution;
- role proficiency;
- readiness;
- availability;
- lineup familiarity/coverage;
- consistency.

#### Preparation Advantage

Preparation Capacity is allocated among:

- Team Execution;
- Opponent-Specific Preparation;
- Meta Adaptation.

The total available preparation cannot maximize all three.

#### Strategic Matchup

Depends on:

- chosen Strategic Posture;
- opponent tendencies;
- lineup profile;
- current meta.

No posture is a universal counter.

#### Adaptation

Repeated posture or lineup patterns increase exposure.

An informed/adaptive rival can convert exposure into an advantage.

Low-information rivals may fail to capitalize on the same exposure.

#### Variance

Variance is bounded.

A stronger roster can lose.

A clearly weaker roster should not receive an arbitrary near-50/50 chance merely because variance exists.

Exact probability transformation is a prototype tuning parameter.

**PROPOSAL**

Use a bounded probability range so:

- decisions change probability;
- outcomes remain uncertain;
- extreme state differences still matter.

The final clamp and coefficient values are explicitly noncanonical.

### Economy resolution

**PROPOSAL**

#### Income

Cash increases only through resolved sources such as:

- scheduled sponsor payment;
- competition prize/reward defined by competition rules;
- accepted commercial contract payment;
- financing proceeds;
- other explicitly represented receivables.

Commercial Value alone never creates Cash.

#### Obligations

Obligations include:

- player compensation;
- coach compensation;
- contract commitments;
- operating cost abstraction;
- debt service;
- accepted investment commitments.

#### Sponsor resolution

CompetitiveOutcome can modify:

- Reputation;
- Audience;
- visibility;
- competitive relevance.

Those modify Commercial Value.

Commercial Value may alter:

- sponsor opportunity quality;
- negotiation leverage;
- available sponsor options.

Only an accepted Sponsor Agreement creates future receivables.

#### Investment

An investment may consume Cash immediately and may increase Load before its benefit is realized.

Prototype investment categories should remain minimal:

- roster;
- information capability;
- limited organizational support.

#### Debt

Use one instrument:

**Bridge Financing**

Borrowing:

- increases Cash immediately;
- creates repayment obligations;
- adds financing cost;
- lowers Strategic Flexibility;
- increases future Financial Pressure.

A company already in distress may receive materially worse terms from the same mechanism.

No separate debt products are required.

#### Distress

The stage ladder is:

Stable  
→ Warning  
→ Distress  
→ Restructuring  
→ Stabilized / Terminal.

No final canonical insolvency numeric threshold is defined.

Escalation should depend on persistent structural inability to satisfy obligations rather than one arbitrary negative number.

#### Recovery

At least one recovery route must remain credible.

Available restructuring actions may include:

- release/sell an expensive player commitment;
- reduce recurring staff cost;
- stop discretionary investments;
- accept worse financing;
- accept an unfavorable sponsor arrangement.

Every recovery action must impose an opportunity cost.

---

# Decision checkpoints

A checkpoint appears only when a material decision exists.

No “continue?” prompt should be generated merely because a day passed.

### 1. Roster Commitment

**PROPOSAL**

**Trigger:** meaningful signing, release, renewal or talent-market opportunity.

**Shown:**

- current roster capability estimate;
- role need;
- contract cost;
- Cash impact;
- future obligations;
- information confidence;
- flexibility consequence.

**Choices:**

- sign expensive option;
- sign cheaper option;
- retain roster;
- release/sell where available.

**Trade-off:** current capability versus liquidity/flexibility/uncertainty.

**Affected:** roster, contracts, Cash, obligations, Load, future capability.

**Delegation:** recommendation allowed; final major commitment remains player-controlled.

### 2. Preparation Priority

**DECISION — inherited**

**Trigger:** upcoming match preparation window.

**Shown:**

- opponent estimate;
- meta signal;
- own execution weaknesses;
- Preparation Capacity;
- information confidence;
- coach recommendation.

**Choices:** allocate preparation among Team Execution, Opponent-Specific Preparation and Meta Adaptation.

**Trade-off:** every allocation sacrifices another preparation dimension.

**Affected:** next competitive resolution.

**Delegation:** yes, according to Authority Envelope.

### 3. Strategic Posture

**DECISION — inherited**

**Trigger:** pre-match strategy checkpoint.

**Shown:**

- own estimated capability;
- opponent profile;
- matchup confidence;
- recent strategy exposure;
- coach recommendation.
