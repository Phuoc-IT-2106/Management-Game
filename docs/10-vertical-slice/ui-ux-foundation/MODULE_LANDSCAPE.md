# Formal module landscape

Status: PROPOSAL. Each matrix is a responsibility specification, not a new Domain
module or implementation order. “Owned concepts” names the authoritative home
under Company/World; the UX module owns no gameplay state. Future facts require
separate approval before schema or command work. FACT rows describe inspected
workspace implementation. DECISION references retain accepted constraints.

## A. Company & Strategy

| Field | Contract |
| --- | --- |
| Responsibility | Operate the whole company; relate priorities, commitments, risk and portfolio |
| Player decisions | Choose which situation deserves attention; future strategic objectives and capital allocation |
| Owned concepts | Company identity under Company; future approved company priorities. No duplicate company-health aggregate |
| Read-only projections | Liquidity, capacity/load, portfolio relationships, risks, upcoming decisions and division performance when supported |
| Inputs | Campaign identity; owner-provided situation and commitment summaries |
| Outputs | Scoped navigation and return context now; future typed strategy commands only after approval |
| Dependencies | Finance, People, Competitive Portfolio, Affairs and Risk projections |
| Cross-module consequences | Future allocation changes compete for liquidity, staff capacity and flexibility; current selection alone changes nothing |
| Information requirements | Cause, affected scope, timing, accountable actor and cost/opportunity of action or delay |
| Workspace types | Company/portfolio space; contextual decision workspace |
| Phase 5 implementation level | FACT: company name and summary exist in dashboard. PROPOSAL: company context and operating-map prototype later; no strategy simulator |
| Future-expansion role | Stable root for multiple businesses without esports terminology in the shell |
| Failure/risk states | Missing identity; situations omit obligations; strategy becomes a KPI wall or invented score |

## B. Business & Finance

| Field | Contract |
| --- | --- |
| Responsibility | Economic viability, commercial commitments and allocation constraints |
| Player decisions | Accept sponsor terms; assess signing/release affordability; future budgets, financing, partnerships and revenue channels |
| Owned concepts | Company Finance: Cash, items, settlements, arrears; Contracts: signed terms; Commercial: reputation/audience and delivery; World: offers and claims |
| Read-only projections | Committed cash forecast, liquidity pressure, payroll exposure, commercial opportunity and future division performance |
| Inputs | Employment/sponsor terms, due dates, competitive outcomes and opportunity availability |
| Outputs | Current SponsorDecision; navigation to SigningDecision/ReleaseDecision; future financing/budget commands separately specified |
| Dependencies | Talent, Competition, Organization load, World markets, Risk and calendar |
| Cross-module consequences | Sponsor load reduces process capacity; hiring changes payroll/depth; settlement changes future affordability |
| Information requirements | Cash versus scheduled/conditional inflows; outstanding obligations, dates, source agreement, forecast horizon and assumptions |
| Workspace types | Financial flow view, dense inspection, document, commitment decision; future negotiation |
| Phase 5 implementation level | FACT: finance and sponsor slices, forecast, arrears, signing/release costs. No editable budgets, debt command or renewal negotiation |
| Future-expansion role | Shared contract/finance semantics for media, merchandise, commercial rights, partnerships and scoped budgets |
| Failure/risk states | Double-counted cash; unsigned opportunity displayed as income; hidden arrears; forecast treated as guaranteed profitability |

**DECISION:** Commercial Value creates leverage/opportunities, not direct Cash
(DEC-003/011/012/017 and integrated evidence). **PROPOSAL:** FinancialFlowView
uses Faucet → Pool → Sink: signed due receipts → Cash → due obligations; conditional
prizes/bonuses remain visibly conditional until earned and scheduled. A financing
inflow would also create repayment exposure, not free growth. Cash is not profit.
Show date and causal agreement for each flow. Never invent balancing amounts.

## C. Competitive Portfolio (Axis B)

| Field | Contract |
| --- | --- |
| Responsibility | Represent competitive businesses owned by the company |
| Player decisions | Preparation, posture, lineup exceptions, coach authority; future division objectives and local budgets |
| Owned concepts | Company People owns membership/readiness; Competitive Management owns plan/work/authority; World owns schedule, rules context and sole results |
| Read-only projections | Team/division performance, observed opponent context, calendar and company consequences |
| Inputs | Eligible person IDs, approved discipline rules, preparation, knowledge and fixtures |
| Outputs | PreparationDecision and CoachDecision; requests to advance through Application; competition outcomes consumed by owners |
| Dependencies | Talent, staff authority, Finance/Commercial and Information |
| Cross-module consequences | Outcomes affect reputation/audience and eligible receivables through owner consumers; workload competes with commercial commitments |
| Information requirements | Eligible choices, current plan versus draft, staff recommendation, uncertainty and timing |
| Workspace types | Portfolio/division workspace, decision, event/review, timeline |
| Phase 5 implementation level | FACT: one fictional development discipline and primary team, preparation/coach tab and results. Counts/formats are DEVELOPMENT FIXTURE — NONCANONICAL |
| Future-expansion role | Additional esports disciplines or sports supply their own eligibility, roles, formats and terminology after approval |
| Failure/risk states | Illegal lineup, missing plan, stale fixture, authority escalation; fixed five-slot shell leaking into company UX |

**DECISION:** DEC-018 priorities remain in the current preparation interface.
**PROPOSAL:** Shared company components accept labelled options and scoped IDs;
the discipline adapter owns role count, terminology and legal combinations. No
generic sports-rule engine or additional discipline is implemented here.

## D. Talent & Performance

| Field | Contract |
| --- | --- |
| Responsibility | Acquire, develop, deploy and retain people as company assets |
| Player decisions | Sign/release candidate/person, inspect roster, compare readiness and cost; preparation within current rules; future recruitment/scouting/training/contracts |
| Owned concepts | Company People owns employed people; Contracts owns employment; World owns available candidates; discipline owner owns plans and performance rules |
| Read-only projections | Dossiers, ability estimates, eligibility, salary exposure, recommendation and performance evidence |
| Inputs | Actor-visible candidate/roster records, terms, finance and discipline eligibility |
| Outputs | SigningDecision/ReleaseDecision; route into preparation; future training/negotiation commands only if approved |
| Dependencies | People & Organization, Business & Finance, Competitive Portfolio, Intelligence |
| Cross-module consequences | Signing transfers sole person ownership, creates obligations/integration load; release sacrifices depth and preserves due arrears |
| Information requirements | Known own facts versus estimated candidate ability; fee, duration, salary, exit cost, readiness and deadline |
| Workspace types | Dossier, comparison, dense inspection, signing/release decision |
| Phase 5 implementation level | FACT: bounded roster/candidate, coach and preparation; no deep development program or analyst department |
| Future-expansion role | Shared identity/employment/contract vocabulary; discipline-specific tactical roles and performance evaluation |
| Failure/risk states | Candidate claimed/expired; wrong-person action after sorting; comparisons imply estimates are true ratings |

Shared talent capability handles people, agreements, costs and dates. A sport's
role suitability, team eligibility, training effects and tactical performance are
discipline rules. Common negotiation presentation is a future reusable pattern;
it does not imply the current fixed-term signing command negotiates.

## E. People & Organization

| Field | Contract |
| --- | --- |
| Responsibility | Company responsibilities, authority, workload and organizational capability |
| Player decisions | Current coach control mode/risk boundary; future leadership, staffing gaps and delegation by department |
| Owned concepts | Company People: staff facts; Competitive Management: current coach envelope; Organization: separately approved capacity/load causes |
| Read-only projections | Capacity/load from owner inputs once; responsibility and reporting links; staff recommendations |
| Inputs | Staff capabilities, contracts, commitments and escalation reasons |
| Outputs | Current CoachDecision within existing workspace; future reviewed staffing/authority commands |
| Dependencies | Talent, Finance, portfolio scopes and Affairs |
| Cross-module consequences | Authority changes approval burden; commitments increase load; overload affects processes, not generic action points |
| Information requirements | Who may act, on what, with which constraints, evidence and review point |
| Workspace types | Embedded authority section now; future organization view and functional workspace |
| Phase 5 implementation level | FACT: one meaningful coach; capacity/load projection. No executive roster, department hiring or reporting simulator |
| Future-expansion role | Shared staff need not be children of a team; departments may serve multiple businesses |
| Failure/risk states | No legal delegated plan, low confidence, overload, ambiguous accountability or hidden automation bonus |

## F. Market & Growth

| Field | Contract |
| --- | --- |
| Responsibility | Assess expansion opportunities and company readiness |
| Player decisions | Future entry, delay, partnership or refusal across disciplines, markets and businesses |
| Owned concepts | World owns offered opportunities/claims; Company owns approved expansion commitments; no current new facts |
| Read-only projections | Readiness, information gaps, resource exposure and strategic flexibility from existing owners |
| Inputs | Market observations, cash commitments, organizational capacity and reputation |
| Outputs | Future approved opportunity decisions; none added to current command set |
| Dependencies | Strategy, Finance, Organization, Intelligence, Risk and new operating domain |
| Cross-module consequences | Cash exposure, load, reputation risk, uncertain information and lost strategic flexibility; management complexity can grow |
| Information requirements | Alternatives including wait/decline; entry and ongoing cost categories; uncertainty, reversibility and affected responsibilities |
| Workspace types | Future opportunity dossier, comparison and decision workspace |
| Phase 5 implementation level | Specification only; no growth tab, technology tree or acquisition command |
| Future-expansion role | New disciplines/sports, geography/audiences/commercial businesses; acquisitions need separate approval |
| Failure/risk states | Overexpansion, false precision, opportunity lost, ongoing load hidden behind upfront price |

No final balance values or exchange rates between resources are specified. Growth
is a strategic commitment under constraints, not a linear unlock tree.

## G. Operations & Assets

| Field | Contract |
| --- | --- |
| Responsibility | Enabling assets and operational infrastructure |
| Player decisions | Future capacity, equipment, facility, technology and logistics investment/retirement |
| Owned concepts | Company owns approved assets/operational commitments; Contracts/Finance own associated terms/items |
| Read-only projections | Capacity contribution, recurring cost, readiness and bottlenecks |
| Inputs | Asset condition if approved, scope demand, cost and time constraints |
| Outputs | Future typed asset decisions; none now |
| Dependencies | Organization, Finance, Talent and operating divisions |
| Cross-module consequences | Capability versus fixed cost, implementation delay and organizational load |
| Information requirements | Purpose, affected scope, commitments, lead time and uncertainty |
| Workspace types | Future asset dossier, functional workspace and decision |
| Phase 5 implementation level | Specification only; existing operating costs can appear in Finance without a facilities screen |
| Future-expansion role | Shared headquarters/offices/equipment, training sites and regional infrastructure as justified |
| Failure/risk states | Capacity bottleneck, unusable asset, unsupported cost model; headquarters becomes mandatory bespoke art |

## H. Intelligence & Analytics

| Field | Contract |
| --- | --- |
| Responsibility | Improve decisions with evidence and explicit information limits |
| Player decisions | Inspect/compare evidence now; future targeted scouting/research investment |
| Owned concepts | Company Information owns approved capability/knowledge causes; World retains hidden external truth |
| Read-only projections | Estimates, confidence, source age, financial forecasts and performance analysis |
| Inputs | Actor-permitted facts and versioned observation rules; never raw hidden truth passed to UI |
| Outputs | Read-only reports/comparisons now; future research requests through approved commands |
| Dependencies | Each domain's observation boundary; Organization and Finance for future capability investment |
| Cross-module consequences | Better knowledge can change choices and recommendations; no direct true-strength buff |
| Information requirements | Known/estimated/unknown distinction, horizon, basis, source/revision and unavailable evidence |
| Workspace types | Evidence sections, comparison, report, dense inspection |
| Phase 5 implementation level | FACT: opponent/candidate estimates and forecast; embedded evidence, no broad analytics department |
| Future-expansion role | Talent, rivals, commercial markets, audience, competition and finance use domain-specific knowledge |
| Failure/risk states | Stale report, confidence without basis, truth leakage via sorting/tooltips, observation reroll |

**DECISION:** Information Quality improves knowledge and decisions, not underlying
player/team/company strength (DEC-018). Unknown is neither zero nor bad performance.

## I. Governance & Risk

| Field | Contract |
| --- | --- |
| Responsibility | Understand structural obligations, exposure and recovery choices |
| Player decisions | Review obligations and use supported sponsor/release recovery routes; future debt/restructuring decisions |
| Owned concepts | Contracts owns signed terms; Finance owns balances/missed dates; Recovery owns stage/reasons. Risk UI owns none |
| Read-only projections | Liquidity warnings, concentration/exposure when justified, distress and recovery evidence |
| Inputs | Commitments, overdue items, forecasts, capability sacrifices and calendar |
| Outputs | Navigation to supported decisions; future restructuring/financing commands separately specified |
| Dependencies | Finance, People, Competition, Affairs and Strategy |
| Cross-module consequences | Recovery may sacrifice competitive depth or accept commercial load; legacy liabilities remain |
| Information requirements | Cause, obligation owner, original missed date, options, sacrifices and unresolved forecast uncertainty |
| Workspace types | Embedded warning/decision now; future risk functional workspace and document |
| Phase 5 implementation level | FACT: warning/distress and constrained recovery exist; no final terminal threshold or legal/compliance simulator |
| Future-expansion role | Cross-division commitments/risk with one source of terms and financial balances |
| Failure/risk states | Duplicate ledgers; instant bankruptcy inferred from one metric; free reset; hidden legal mechanics |

## J. Affairs & Time

| Field | Contract |
| --- | --- |
| Responsibility | Explain what is happening and what requires attention |
| Player decisions | Prioritize, inspect deadlines/results, resolve required commitments and advance to meaningful checkpoints |
| Owned concepts | World Calendar owns chronology; initiating Company/World owners own situations/events; Application coordinates advance |
| Read-only projections | Timeline, grouped affairs, stop reasons, consequence links and review history |
| Inputs | Schedule, market deadlines, financial due items, owner checkpoints and command outcomes |
| Outputs | Existing AdvanceDecision and navigation; no second scheduler or UI-authored simulation stop |
| Dependencies | Every participating owner's actor-safe event/commitment projection |
| Cross-module consequences | Advancing invokes ordered simulation; pending decisions may expire and consequences mature |
| Information requirements | Current date, next destination/stop reason when known, deadlines, affected entity, consequence of ignoring and responsibility |
| Workspace types | Timeline/affairs, event/review, linked decision/document |
| Phase 5 implementation level | FACT: advance/checkpoint behavior, deadlines and review strings exist. PROPOSAL: structured affairs projection later |
| Future-expansion role | One chronology across meetings, negotiations, competition, reports and approved domains |
| Failure/risk states | Missed deadline hidden, notification spam, UI priority alters simulation, stale review mistaken for current state |

## Current scope boundary

**PROPOSAL:** A/B/C/D/J relationships are visible through company context and the
four bounded workspace areas. E/H/I supply embedded information, not separate
build obligations. F/G remain specification-only. Finance and risk references to
future debt do not authorize a loan implementation; talent references to training
do not authorize a new development simulation.
