# 03 — Simulation & Economy: Vertical-Slice Economy Specification

**Status: SPECIALIST PROPOSAL — pending Project Director approval.**

This specification preserves the established product center: executive + management leadership, competitive performance as business leverage, medium-depth simulation, real financial failure with a recovery window, and a deliberately narrow first vertical slice. PLAYER_ROLE PRODUCT_BOUNDARIES

The first slice already requires basic finance/cash pressure, sponsorship, reputation/audience/commercial linkage, and at least one credible distress/restructuring path. PRODUCT_BOUNDARIES

I also preserve the Phase 2 time/state model rather than redefining it: authoritative simulation advances on the established daily tick with player-facing checkpoint progression, while Company State and World State retain their existing ownership boundaries. 

---

# 1. Minimum Economy Model

The smallest useful vertical-slice economy should revolve around **five decision-relevant resources**:

| Resource | Meaning | Role |
|---|---|---|
| **Cash** | Immediately spendable liquidity | Hard financial constraint |
| **Organizational Capacity** | How much operational/managerial load the current organization can absorb effectively | Growth/complexity constraint |
| **Reputation** | How credible, prestigious, and attractive the organization is perceived to be | Opportunity/access modifier |
| **Audience / Fandom** | The size and engagement of people who actively care about the organization | Commercial demand base |
| **Information Quality** | Reliability and precision of information used for decisions | Uncertainty reducer |

These fit the project's existing definition of resources as limited factors including money, capacity, reputation, audience, and information. GLOSSARY

**Commercial Value, Strategic Flexibility, Financial Pressure and Company Value should not become additional primary resource pools.** They are better treated as derived concepts.

This keeps the simulation small enough to reason about while still supporting the intended interconnected loop:

**Competition → Reputation → Audience → Commercial Opportunity → Cash/Capability → Future Competition**

without allowing it to become automatic exponential growth. The existing core loop explicitly requires negative feedback and diminishing returns around this chain. CORE_LOOP

---

# 2. Stored vs Derived State

## 2.1 Cash

**PROPOSAL — stored Company State**

`Cash` represents money currently available to satisfy obligations or fund decisions.

It is deliberately **liquidity**, not accounting profit and not company valuation.

Examples:

- signing fee;
- salary payment;
- staff hiring;
- scouting/analytics investment;
- sponsor receipts;
- financing proceeds;
- emergency spending.

### Derived from Cash

Do not store these independently unless required for history/UI:

- projected runway;
- upcoming net cash flow;
- liquidity coverage;
- financial pressure;
- discretionary spending headroom.

Two companies with identical annual revenue can therefore occupy very different strategic positions because their current liquidity and obligations differ.

---

# 3. Organizational Capacity

## 3.1 Semantics

**PROPOSAL — hybrid stored/derived**

Organizational Capacity measures the company's practical ability to manage its current workload **without coordination quality deteriorating**.

It is not merely "staff count."

It represents the combined effect of:

- relevant staff;
- staff quality;
- delegation;
- organizational support;
- current responsibilities;
- operational complexity.

This directly supports the project principle that growth should create coordination requirements and management overhead rather than simply increase numbers. EXPANSION_PRINCIPLES

### Stored state

Store the inputs that create capacity:

- capacity contribution of relevant staff/functions;
- committed organizational load from persistent activities;
- temporary capacity modifiers where justified.

### Derived state

```text
Effective Capacity
Current Organizational Load
Capacity Headroom = Effective Capacity - Load
Capacity Pressure = function(Load / Effective Capacity)
```

Do **not** require every employee to own a detailed task queue.

The early product explicitly avoids complete organization-chart and employee-life simulation. PRODUCT_BOUNDARIES

---

# 4. Reputation

## 4.1 Semantics

**PROPOSAL — stored Company State**

Reputation represents how strongly external stakeholders perceive the organization as:

- credible;
- prestigious;
- successful;
- professionally desirable.

It should answer:

> "How much weight does this organization's name carry?"

Reputation is **not audience size**.

A highly respected organization may have a smaller fanbase than a mass-market organization, and a large audience does not automatically imply strong professional reputation.

### Primary faucets

- significant competitive achievement;
- sustained credible performance;
- fulfilling major commercial commitments;
- attracting respected players/staff;
- notable organizational successes.

### Primary sinks

- sustained poor performance relative to expectations;
- visible financial distress;
- breaking important commercial commitments;
- badly handled restructuring;
- major organizational instability.

Routine wins should produce little or no lasting reputation at high reputation levels.

That gives natural diminishing returns.

---

# 5. Audience / Fandom

## 5.1 Semantics

**PROPOSAL — stored Company State**

Audience/Fandom represents people with meaningful attention or attachment to the organization.

For the vertical slice, **do not simulate individual fans**. The product explicitly excludes fan-by-fan behavior. PRODUCT_BOUNDARIES

The pool answers:

> "How much engaged market attention does this organization currently command?"

Audience is affected by reputation, but is not identical to it.

### Example divergence

A team can have:

- **high reputation + medium audience** — respected competitive organization;
- **medium reputation + high audience** — popular organization with broad reach;
- **high reputation + high audience** — commercially powerful brand;
- **low reputation + high audience** temporarily — attention without durable credibility.

### Faucets

- meaningful competitive success;
- sustained visibility;
- star acquisition;
- successful sponsor/commercial exposure where appropriate;
- accumulated reputation.

### Sinks

- prolonged irrelevance;
- repeated underperformance;
- loss of recognizable talent;
- organizational instability;
- time without attention-generating activity.

Audience should generally change more gradually than individual match results.

---

# 6. Information Quality

## 6.1 Semantics

**PROPOSAL — stored capability with decision-specific derived output**

Information Quality represents how trustworthy the organization's knowledge is when making a decision.

The product explicitly requires imperfect information and staff recommendations rather than omniscience. PLAYER_ROLE

It does **not** mean a global "+accuracy %" stat applied blindly to everything.

For the slice, maintain limited information domains such as:

- player/talent assessment;
- opponent/competitive assessment;
- commercial/sponsor assessment.

### Stored inputs

- scouting capability;
- analytics capability;
- relevant staff quality;
- accumulated observation where necessary.

### Derived outputs

For a specific decision:

```text
Observed Estimate
Confidence / Uncertainty Band
```

Example:

```text
True player potential: hidden
Observed potential: 72–84 confidence range
```

Higher Information Quality should:

- narrow uncertainty;
- improve forecast reliability;
- improve delegated staff recommendations.

It should **not** automatically make the underlying asset better.

This separates:

**knowing more** from **having more**.

---

# 7. Commercial Value

**PROPOSAL — derived, not stored as a resource pool**

Commercial Value represents the organization's current ability to convert its competitive and brand position into sponsor/commercial opportunities.

Minimal relationship:

```text
Commercial Value
≈ f(
    Reputation,
    Audience/Fandom,
    Competitive Relevance,
    Sponsor Fit,
    Market Conditions
)
```

This preserves the accepted idea that competitive success generates business leverage rather than directly printing money. Competitive success was established as an asset that can strengthen reputation, fandom and commercial opportunity. DISCOVERY_WORKSHEET_COMPLETED

### Critical rule

**Commercial Value does not automatically create Cash.**

It instead influences:

- quality of sponsor offers;
- negotiation leverage;
- number/quality of opportunities;
- achievable contract value.

The player must still obtain and maintain the commercial relationship.

This prevents:

```text
Win match
→ receive automatic money
```

from becoming the dominant loop.

---

# 8. Faucet → Pool → Sink Structure

| Pool | Faucets | Sinks / Pressure |
|---|---|---|
| Cash | sponsor payments, competition rewards, financing, contract/player sale proceeds | salaries, signing costs, operating cost, investments, debt service |
| Organizational Capacity | staff, better delegation, support investment | roster/staff/company complexity, active projects, restructuring workload |
| Reputation | competitive achievement, credible organizational success | failure, broken commitments, prolonged decline, distress |
| Audience/Fandom | results, visibility, stars, reputation | irrelevance, decline, talent loss, weak engagement |
| Information Quality | scouting, analytics, qualified staff, observation | staff loss, underinvestment, environmental change/outdated knowledge |

A faucet should normally require **some condition, investment, success, or opportunity**.

There should be almost no unconditional permanent resource generation.

---

# 9. Cash Flow and Obligations

## 9.1 Financial model

**PROPOSAL**

Avoid full accounting.

The vertical slice only needs:

```text
Cash
Scheduled Receivables
Scheduled Obligations
Conditional Receivables
Debt Obligations
```

This matches the existing boundary that the player should understand material financial consequences without performing bookkeeping. PLAYER_ROLE

## 9.2 Scheduled obligations

Examples:

- player salary;
- staff salary;
- contract installments;
- basic operating cost;
- debt repayment;
- debt interest.

Each obligation needs minimally:

```text
amount
due date / due checkpoint
priority/type
source contract
payment status
```

## 9.3 Cash-flow resolution

At the existing simulation time advance:

```text
Receivables due
→ Cash increases

Obligations due
→ attempt payment

If sufficient Cash:
    pay normally
else:
    create missed/at-risk obligation
    increase Financial Pressure
    trigger decision/restructuring workflow where material
```

The player should **not** click individual invoices.

Routine financial administration is specifically outside core gameplay. CORE_DECISIONS

---

# 10. Revenue Model

For the first slice, keep revenue sources narrow.

### 1. Sponsor income

Primary commercial revenue.

May include:

- guaranteed scheduled amount;
- performance-linked amount;
- optional bonus conditions.

### 2. Competitive rewards

Tournament/prize income.

Important but intentionally volatile.

It should not be sufficient alone to produce predictable sustainable growth.

### 3. Talent/contract transactions

Sale/buyout income where allowed by the player/contract system.

This should be strategically costly because selling talent can reduce competitive strength, reputation momentum, or future capability.

### 4. Financing

Debt proceeds.

This is a **liquidity faucet, not wealth creation**.

Receiving borrowed cash simultaneously creates future obligations.

---

# 11. Obligations vs Discretionary Spending

This distinction is necessary for meaningful cash pressure.

## Obligations

Existing commitments the company is expected to satisfy:

- salary;
- contractual payments;
- debt servicing;
- essential operating expense.

## Discretionary spending

Future commitments the player can choose:

- new player;
- new staff;
- information investment;
- organizational support;
- aggressive competitive investment.

Therefore:

```text
Cash = what can be spent now

Free Cash ≠ Cash

Strategic Flexibility depends on
Cash - near-term obligations - irreversible commitments
```

---

# 12. Strategic Flexibility

**PROPOSAL — derived**

Strategic Flexibility measures how many credible choices remain available to the company.

It should not be another bar the player spends directly.

Approximate drivers:

```text
+ cash headroom
+ organizational capacity headroom
+ strong information
+ manageable commitments

- debt burden
- long contracts
- large payroll
- overloaded staff
- urgent obligations
```

This makes a superficially wealthy company capable of being strategically constrained.

Example:

```text
Cash: high
Payroll commitments: extremely high
Debt maturity: near
Capacity: overloaded
```

Result:

```text
Strategic Flexibility: low
```

---

# 13. Debt

## 13.1 Role

**PROPOSAL**

Debt is a strategic acceleration mechanism.

It converts:

```text
future financial flexibility
```

into:

```text
current liquidity
```

The approved failure direction already establishes that debt should create repayment pressure, reduced flexibility and restructuring risk rather than behave as free money. DISCOVERY_WORKSHEET_COMPLETED

## 13.2 Minimum debt state

Each debt instrument only needs:

```text
principal outstanding
interest / financing cost
repayment schedule
next obligation
maturity
status
```

No detailed bond market or complex financing instruments belong in the vertical slice; detailed debt markets are explicitly excluded. PRODUCT_BOUNDARIES

## 13.3 Debt consequences

Debt provides:

```text
Immediate:
+ Cash

Delayed:
- future Cash
- Strategic Flexibility
+ Financial Pressure
+ restructuring risk
```

That is the intended risk/reward structure.

---

# 14. Financial Pressure

**PROPOSAL — derived state**

Do not treat distress as simply:

```text
Cash < 0
```

Financial Pressure should consider multiple conditions:

```text
liquidity
near-term obligations
expected inflows
debt servicing
recurring cost base
ability to reduce costs
available recovery actions
```

Conceptually:

```text
Financial Pressure
= obligation burden
+ liquidity shortfall risk
+ leverage burden
- credible near-term inflows
- available corrective options
```

No fixed numeric terminal threshold is proposed here.

That remains Director-controlled as requested.

---

# 15. Distress → Restructuring → Recovery

Preserve the Phase 2 failure ladder:

```text
Stable
→ Warning
→ Distress
→ Restructuring
→ Stabilized / Terminal
```

and the established product rule that recovery gameplay normally precedes game over. CORE_LOOP

## Stable

Normal operating position.

The player can reasonably pursue growth or competition.

## Warning

Signals indicate future obligations may become unsafe.

Possible player responses:

- preserve cash;
- stop optional spending;
- reject expensive commitments;
- reduce risk.

This stage should usually provide the **cheapest recovery options**.

## Distress

The company cannot safely continue its current commitments.

Effects may include:

- sponsor confidence pressure;
- reduced negotiating leverage;
- reputation damage;
- worsening financing conditions;
- forced decision triggers.

## Restructuring

The player must deliberately sacrifice something to restore viability.

Vertical-slice actions may include:

- sell valuable player contract;
- release/cut expensive commitments where contracts permit;
- reduce staff cost;
- refinance at worse terms;
- accept unfavorable commercial terms;
- halt optional capability spending.

The project already identifies forced downsizing, asset/player sales, sponsor loss and worse refinancing as valid recovery consequences. DISCOVERY_WORKSHEET_COMPLETED

## Recovery / Stabilized

Requirements should be conceptual rather than a fixed hard threshold at this stage:

- credible ability to meet obligations;
- sustainable recurring cash position;
- no unresolved critical default state;
- capacity no longer critically overloaded.

## Terminal

The economy layer can expose:

```text
Terminal insolvency candidate
```

but **must not define the final campaign-ending numeric threshold**.

That requires Project Director approval.

---

# 16. Reputation → Audience → Commercial Value Relationship

The relationship should be **directional but non-deterministic**.

Recommended model:

```text
Competitive Result
      ↓
Competitive Significance
      ↓
Reputation change
      ↓
Audience/Fandom momentum
      ↓
Commercial Value
      ↓
Sponsor opportunities
      ↓
Potential Cash
```

Not:

```text
Win = Reputation +5
Reputation = Fans +1000
Fans = Cash +$10,000
```

Each link should have context.

### Example

Winning a small routine match:

- negligible reputation;
- negligible audience change.

Winning a major competition unexpectedly:

- strong reputation gain;
- audience growth;
- improved sponsor market position.

Winning repeatedly as the dominant organization:

- still strategically valuable;
- smaller marginal reputation gains.

This produces diminishing returns naturally.

---

# 17. Competitive Performance Must Not Be Stored Here

This specialist should consume **competitive outcomes supplied by the competition system**.

Simulation & Economy does not design match resolution.

Required input should be abstract:

```text
Result
Event importance
Expectation context
Visibility
Relevant reward
```

The economy layer then determines economic/resource consequences.

This preserves system boundaries and avoids redefining match simulation.

---

# 18. Company Value

**PROPOSAL — derived strategic concept**

Company Value should represent the economic strength and future earning potential of the organization.

It is **not equal to Cash**.

For the vertical slice, a conceptual derivation is sufficient:

```text
Company Value
≈
Financial Position
+ Commercial Strength
+ Valuable Contract / Talent Assets
+ Reputation Contribution
+ Audience Contribution
+ Sustainable Organizational Capability
- Debt / Liabilities
- Distress / Structural Risk
```

Do not attempt realistic corporate valuation models.

The project explicitly excludes detailed accounting and requires selective rather than universal simulation. PRODUCT_BOUNDARIES

### Important distinction

A company may have:

```text
High Company Value
Low Cash
```

Example: strong players, sponsors and audience, but immediate liquidity problems.

Or:

```text
High Cash
Low Company Value
```

Example: cash reserve but weak roster, shrinking audience and no meaningful commercial pipeline.

This distinction makes liquidity crises possible without pretending the entire company has become worthless overnight.

---

# 19. Immediate vs Delayed Consequences

The core design already calls for delayed consequences so decisions cannot always be evaluated instantly. CORE_LOOP

## Immediate

Suitable for consequences directly created by a transaction:

```text
Sign player
→ Cash decreases
→ future payroll obligations increase
→ roster changes
→ capacity load changes
```

Take debt:

```text
→ Cash increases
→ debt liability appears
→ repayment schedule appears
```

Release staff:

```text
→ capacity decreases
→ future cost decreases
```

## Delayed

Suitable for systemic effects requiring time:

```text
Star signing
→ potential audience growth later
→ sponsor leverage later
```

```text
Underinvest scouting
→ Information Quality deterioration becomes relevant on future decisions
```

```text
Overload organization
→ operational/delegation quality deteriorates over time
```

```text
Repeated competitive success
→ reputation accumulates
→ commercial opportunities improve
```

This prevents optimization through instant perfect feedback.

---

# 20. Organizational Overload

Capacity should create **soft degradation before hard failure**.

Recommended pattern:

```text
Load <= Capacity
→ normal operation

Load moderately > Capacity
→ reduced operational effectiveness
→ poorer recommendations / slower organizational recovery
→ higher risk

Load severely > Capacity
→ significant performance loss
→ mistakes / delays / unresolved issues
```

Do not use:

```text
Capacity 99/100 = perfect
Capacity 101/100 = organization disabled
```

The system should create escalating pressure rather than arbitrary cliffs.

---

# 21. Information as an Economic Trade-off

Information Quality is important because it changes the **quality of decisions**, not merely a percentage modifier.

Example decision:

### Cheap unknown player

Low information:

```text
Expected ability: broad uncertainty
Contract demand: low
Upside: high
Risk: high
```

Improved scouting:

```text
uncertainty narrows
```

The player may then discover either:

- the bargain is attractive;
- the bargain was false.

Therefore scouting/analytics expenditure has value without directly increasing competitive power.

This fits the game's information-based decision philosophy. GAME_VISION

---

# 22. Anti-Runaway Controls

The competitive-business growth loop needs explicit braking mechanisms.

### Diminishing reputation returns

Repeated expected wins create less reputation than breakthrough achievements.

### Audience inertia

Audience cannot grow infinitely from isolated successes and can decay when attention falls.

### Rising commitment cost

Better players/staff and greater ambitions generally create higher recurring obligations.

### Capacity pressure

Growth increases organizational load, so expansion without support degrades effectiveness.

### Competitive response

Rivals compete for players, sponsors and attention rather than allowing uncontested compounding. Rival organizations are already part of the accepted evolving ecosystem. PRODUCT_BOUNDARIES

### Opportunity scarcity

The best sponsors, players and staff cannot simultaneously belong to every organization.

### Debt service

Borrowing increases current spending power but reduces future flexibility.

### Delayed payback

Investments in information, audience or organization do not immediately turn into cash.

### Exposure from success

Success should raise expectations and cost structure enough that maintaining dominance is not free.

This is critical to prevent:

```text
win
→ money
→ stronger roster
→ easier win
→ more money
→ unstoppable snowball
```

---

# 23. Minimal Consequence Chains

The first prototype should support at least these systemic chains.

## Chain A — Competitive growth

```text
Strong result
→ Reputation ↑
→ Audience momentum ↑
→ Commercial Value ↑
→ improved sponsor opportunity
→ future Cash potential ↑
```

## Chain B — Aggressive roster investment