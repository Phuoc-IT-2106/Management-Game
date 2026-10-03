# RESOURCE MODEL

Status: Phase 2 — Core Loop & Simulation specialist proposal

## 1. Foundation

**DECISION — inherited**

Core decisions require meaningful opportunity cost. Money must not be the only meaningful resource. Competitive success creates business leverage but should not create automatic exponential growth.

**FACT**

Phase 1 explicitly left the 3–5 essential resource pools beyond money unresolved.

## 2. Primary Authoritative Resource Pools

### 2.1 Cash / Liquidity

**PROPOSAL**

Cash is the hard financial pool used to meet obligations and fund decisions.

It must be distinguished from accounting profit and from company value.

Primary properties:

- current cash;
- committed near-term outflows;
- committed near-term inflows;
- liquidity runway / pressure signal.

### 2.2 Organizational Capacity

**PROPOSAL**

Organizational Capacity represents how much operational and coordination load the current staff structure can sustain without material degradation.

It is not merely employee count.

Capacity is increased by capable staff and stable structure, and consumed by:

- preparation workload;
- scouting / analysis workload;
- sponsor obligations;
- crises;
- roster turnover;
- growth / complexity;
- delegated responsibilities.

Overload should reduce execution quality, information quality, or response speed rather than act only as a binary block.

### 2.3 Reputation

**PROPOSAL**

Reputation represents external perceived standing and credibility of the company.

It changes more slowly than single-match performance and affects:

- talent attraction;
- sponsor interest;
- negotiation leverage;
- expectations;
- commercial opportunities.

Reputation is not identical to audience size.

### 2.4 Audience / Fandom

**PROPOSAL**

Audience represents the scale and engagement of people paying attention to the organization.

It affects:

- sponsor reach;
- commercial value;
- brand momentum;
- value of future opportunities.

Audience may grow from competitive relevance, notable players, sustained reputation, and sponsor/media exposure; it may decline when relevance falls.

### 2.5 Information Quality

**PROPOSAL**

Information Quality is a domain-specific resource state, not one universal number.

Minimum domains for the first slice:

- opponent / competition knowledge;
- roster / talent evaluation confidence;
- financial forecast confidence;
- sponsor / market knowledge.

Better information narrows uncertainty; it does not guarantee a better decision.

**ASSUMPTION**

Information can be represented as quality / confidence / coverage rather than as collectible "information points".

## 3. Important Derived States — Not Separate Primary Pools

### Commercial Value

**PROPOSAL**

Commercial Value should be a derived signal from some combination of:

- reputation;
- audience;
- recent competitive relevance;
- sponsor fit / market conditions.

It should not be an independent pool in the first slice, avoiding duplicated state.

### Competitive Capability

**PROPOSAL**

Competitive Capability is a derived condition of roster quality, staff quality, preparation, availability, familiarity, and strategic fit. It is not a spendable resource.

### Strategic Flexibility

**PROPOSAL**

Strategic Flexibility is a derived constraint from:

- cash reserves;
- debt / fixed obligations;
- contract length and exit cost;
- roster slots;
- sponsor restrictions;
- organizational capacity.

It should be surfaced to the player, but need not be stored as a standalone pool.

### Company Value

**PROPOSAL**

Company Value is a derived strategic / failure indicator, not spendable cash.

For the first slice it may depend on financial health, roster/contract assets, reputation, audience, sponsor relationships, and risk exposure at abstracted depth.

## 4. Faucet → Pool → Sink Map

**PROPOSAL**

| Resource Pool | Faucets | Pool | Sinks / Pressure |
|---|---|---|---|
| Cash | sponsor payments, prize money, player/contract sales, financing | liquid cash | payroll, signing costs, staff costs, debt service, capability investment, restructuring costs |
| Organizational Capacity | hiring capable staff, improved delegation fit, stable roster/staff, selective capability investment | available execution capacity | workload, sponsor obligations, scouting/preparation, crises, turnover, complexity |
| Reputation | competitive success, credible roster/staff moves, reliable sponsor delivery, sustained stability | external standing | repeated poor performance, public failures, sponsor conflict, instability, distress/restructuring damage |
| Audience | competitive relevance, stars, reputation, sponsor exposure, sustained success | engaged following | declining relevance, repeated underperformance, star exits, inactivity, brand mismatch |
| Information Quality | scouting/analysis work, staff expertise, accumulated observations | confidence/coverage by domain | staleness, meta/market change, staff loss, insufficient capacity, new unknown targets |

## 5. Anti-Runaway Controls

**PROPOSAL**

The first slice should use systemic brakes rather than arbitrary caps:

- higher-quality roster/staff create higher fixed commitments;
- success increases expectations and renewal costs;
- reputation gains should have diminishing returns;
- audience growth should slow without continued relevance;
- capacity pressure rises as commitments accumulate;
- rivals compete for the same talent and sponsor opportunities;
- sponsor value depends on fit and obligations, not audience alone;
- leverage/debt improves current cash but reduces future flexibility.

## 6. Resource Conversion Principles

**PROPOSAL**

Resources should convert imperfectly and with delay.

Examples:

- Cash → better roster does not guarantee wins.
- Wins → reputation are stronger when stakes/opponent quality matter.
- Reputation → audience requires sustained relevance.
- Audience → sponsor value depends on sponsor fit and market demand.
- Cash → organizational capacity requires hiring/investment and time.
- Capacity → information requires allocating work to scouting/analysis.

This prevents a single universal conversion path from dominating.

## 7. First-Slice Economy Boundary

**DECISION — inherited**

Do not simulate detailed accounting, taxes, full macroeconomics, or a detailed debt market.

**PROPOSAL**

The first slice needs only enough finance to support:

- liquidity decisions;
- recurring payroll / staff commitments;
- sponsor income;
- prize / competition income;
- major signing / exit costs;
- simple debt / financing pressure;
- restructuring.

## 8. Open Questions

**OPEN QUESTION**

- Should audience be one aggregate pool or 2–3 segments in the first prototype?
- Which staff actions are allowed to consume Organizational Capacity explicitly?
- How should company value be estimated without creating a detailed valuation simulator?
- Is financing available from campaign start or only during distress / strategic borrowing opportunities?
