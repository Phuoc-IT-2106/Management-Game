# Coach Data Standard v0.1

Status: **PROPOSED — NOT APPROVED** · 2026-10-08  
[Package / authority](README.md) · [Shared primitives](SHARED_GENERATION_AND_VALIDATION_V01.md) · [Team formulas](TEAM_DATA_STANDARD_V01.md)

## 1. Responsibility, relationships and evidence

Coach là person có coaching role, không phải Player đổi tên combat ratings. Trial chỉ một Head Coach trong Company và tối đa3 World coach candidates. Staff trees, assistants, simultaneous employment, personalities và player-to-coach career conversion deferred. CoachId là alias của PersonId; DefinitionId mới là catalog identity. Employer/contract/team assignment là ba references khác nhau. Dated public career rows chỉ evidence, không current ownership.

**FACT:** [Model.cs](../../../src/ManagementGame.Domain/Model.cs) current Coach có sáu fields `Id, Name, Preparation, Analysis, Adaptability, Capacity`; chưa có DefinitionId. Proposed static catalog ref phải qua separately approved identity/save migration. Company.Coach là owned coach, World.CoachCandidates giữ available coaches; employment lives under Company, không duplicate trong Coach. [Snapshot validation](../../../src/ManagementGame.Infrastructure/SnapshotValidation.cs) hiện kiểm tra Preparation1..100, Analysis/Adaptability0..100, Capacity1..1000.

Public name, nationality nếu supported, appointments và verified dates có thể là source facts. Experience là duration of evidence, không effectiveness. Results dưới thời coach đồng biến với roster strength, schedule, organization resources, roster turnover và patch. Không dùng team win rate hoặc danh tiếng để gán năng lực cá nhân như fact. Không đoán private work ethic, psychology, salary hoặc causal share of victories. Public example giữ cả bốn capability unknown.

## 2. Static schema and field dictionary

Envelope/provenance theo shared spec; format=`coach-definition-example-v01`; schema_version=1; all keys required, N nullable. Generated and imported definitions có cùng shape, nhưng unknown profile không được instantiate trước admission.

| Definition field | Type / constraints | Meaning / consumer |
|---|---|---|
| definition_id / definition_version | ID / integer≥1 | Stable catalog person and revision |
| display_name | string1..120 | Display only; no ability/seed inference |
| nationality_codes | unique uppercase two-letter code array N | Only public supported nationality; unknown=null, no talent modifier |
| staff_role | enum `head_coach` | Trial only compatible role |
| birth_year | integer N | Public sourced or synthetic; no inferred age from photo |
| experience_years | decimal N | C05 documented career duration; descriptive |
| simulation_profile | object | Exact keys below; latent/estimated attributes separate from biography |
| provenance | shared object | Origin/rights/time/external identities |

Profile exact keys: `preparation`, `analysis`, `adaptability`, `capacity` (integer N), `profile_basis` (`unknown`/`generated_scenario`), `unknown_reason` (string N). Unknown profile requires all four null and nonempty reason; generated scenario requires all four numbers and reason=null. No silent mean50. This example version deliberately does not pretend to standardize production real-coach estimates: admission of any estimated profile needs a versioned method/evidence bundle and schema review, then the same four runtime fields, units and consumers as generated profiles.

Public `source_facts` exact keys: `career_memberships` (array), `coverage` (`unknown`/`partial`/`complete`), `unknown_reason` (string N). Row exact keys: `organization_definition_id` (ID), `team_definition_id` (ID N), `staff_role` (`head_coach`), `start_date`/`end_date` (ISO date N, end exclusive), `source_ref` (URI). Non-null experience requires complete eligible intervals plus explicit cutoff; rows with unbounded unknown dates cannot produce a complete career total. Generated source_facts=null.

Public unadmitted `initialization`=null. Generated initialization exact keys `person_id`, `owner_organization_id`, `employment_id`, `staff_assignment_id`: four IDs for owned coach; for a market coach the last three are null, person_id non-null, and the world market quote is required. Example generated_coach is the owned preparation coach. Employment values never live in profile.

## 3. Attribute decisions and runtime ownership

| Attribute / candidate | Classification | Decision and actual consumer |
|---|---|---|
| Preparation | Current implemented | Hiring and preparation allocation; Competitive Management daily Work conversion C01; salary C03 |
| Analysis | Current stored and priced; competitive consumer absent | Salary and market average band only; does **not** improve scouting, plan quality or preparation in current code. Trial holds60, avoids selling unsupported premium |
| Adaptability | Current implemented | Competition adaptation term C02; salary. No special behavior on hire |
| Capacity | Current implemented | Sponsor/roster load versus converted work C01; salary; distinct work-load units, can exceed100 |
| Player Development | Deferred | No approved training growth pipeline; no generated development rating |
| Strategic Specialization | Experimental profile explanation | Label describing P/D/C trade-offs; no extra match bonus or hidden counter multiplier |
| Talent Evaluation | Deferred | Observation currently uses Company.Information; new information consumer requires separate design |
| Delegation Reliability | Rejected as personality rating | Authority/policy validation handles delegation; no random disobedience, psychometric inference or private-work-ethic rating |
| Experience / career stage | Descriptive only | Career display; does not multiply attributes, salary or recommendations |
| Availability / current employer | Derived current state | World candidate versus Company coach/contract; no second availability flag that can disagree |

Coach data does not own Plan, Work, Information or contract money. `Company.Authority` restricts CoachPolicy actions; actor-safe Situation is the input. Current CoachPolicy chooses per-role max Execution×Readiness and uses fixed allocations (25/25/50 for aggressive meta, otherwise50/30/20); it does not use the coach's Analysis as an omniscient player evaluator. Changing coach does not extend delegated authority or reveal World truth.

## 4. Formulas and worked examples

All witnesses synthetic. `RE` round-even, `F` nonnegative integer floor. Missing input→unknown for observation/diagnostics or admission error for owned gameplay profile, never fill with average. Current calculations need no empirical sample; same rules/config context required. Recompute at relevant daily/match/contract event, not every UI render with new randomness.

### C01 — Load-limited preparation (current)

`load=BaseLoad+3*roster_count+sum(activeSponsor.Load)`; `converted=RE(Preparation*Capacity,max(Capacity,load))`. Units work/day; Plan allocations then floor individually exactly as T06. Example P80/C65/load120:5200/120=43.333333→43; P60/C110/load120:6600/120=55. At load60 each converts its full Preparation (80 versus60). Hiring capacity coach changes an actual bottleneck; it does not add another Team bonus. Capacity0 fails; load below capacity saturates at P; no division by zero epsilon. Consumer: daily Preparation phase mutating Company competitive Work. Tests: low/high load reversal, exact boundary, allocation residue, no duplicate overload penalty.

### C02 — Adaptation and opponent analysis (current)

`adaptation=RE((F(sum(player.Adaptability)/n)+coach.Adaptability),30) - F(repeated_posture*rival.Analysis/30)`.

Repeated_posture is count of current plan posture among last3Company results in current fixture configuration, not coach experience or an independently stored streak. Player mean adaptability60: prep coach D50→RE(110,30)=4; adaptation coach D85→RE(145,30)=5. With repeated2 and rivalAnalysis75, penalty floor150/30=5, final−1 or0. Unit resolver points, which may be negative; no clamp to0. Current probability later clamps to[50000,950000] parts-per-million. No Coach.Analysis or Rival.Adaptability appears in this expression.

Input all five known owned players, opponent latent Analysis only within resolver, and existing result history; no sample inference. Zero lineup invalid, missing public ratings unknown outside resolver. Recompute at match. Consumer: Competition only once. Tests: ties, history window cap, no identity/rating duplication; actor policy cannot read rival hidden Analysis just because resolver can.

### C03 — Compensation and hiring (current units)

`salary_minor_per_day=5*(P+A+D)+2*C`. Three profiles at grade0/tradeoff0:

| Synthetic coach | P | A | D | C | Salary arithmetic | Daily salary | Converted load60 /120 |
|---|---:|---:|---:|---:|---|---:|---:|
| Preparation specialist | 80 | 60 | 50 | 65 | 5×190+130 | 1080 | 80 /43 |
| Adaptation specialist | 55 | 60 | 85 | 75 | 5×200+150 | 1150 | 55 /34 |
| Capacity specialist | 60 | 60 | 55 | 110 | 5×175+220 | 1095 | 60 /55 |

Current World quote signing fee=15×salary. Initial owned coach uses CampaignFactory initial placement, not a market signing charge. A replacement hire pays quote fee plus old coach release cost; new employment release term currently5×salary. Candidate salary is an explicit quote, not recomputed on every payroll day. Renewal uses current `max(current_salary,RE(market_salary*(100+F(reputation/5)),100))`; e.g market1080, reputation45→9%→1177, current1100 →1177. Current RenewContract sets release cost to3×new salary for either person type; do not assume the hire multiplier5 persists after renewal. References to compensation are **simulation minor units**, not real-world earnings. Recompute market/renewal quote on existing event; contract payroll follows agreed terms. Tests: exact integer arithmetic, insufficient cash no partial hire, old contract release once, current salary floor at renewal, null public profile cannot make a zero-cost coach.

### C04 — Actor-visible market band (current)

`average=F((P+A+D)/3)`; `band=[max(0,F(average/10)*10-10),min(100,F(average/10)*10+20)]`. Preparation coach average63 →[50,80]. Capacity does not enter this band; it is not an Overall Rating or calibrated interval. Current interface may expose other owned values; proposed market capacity presentation needs actor-safe contract review rather than silently claiming this band describes capacity. Unknown public profile produces no derived band. Update on current observation identity, no repeated draws; consumer current coach market read model. Tests: extremes0/100 for band helper, floor boundaries, same average with different capacity yields same band and different overload outcome.

### C05 — Documented experience (proposed descriptive)

Input source-supported coaching employment intervals `[start_date,end_date)`, explicit as_of and agreed role inclusion. Set cutoff to the UTC calendar date of as_of, excluding any partial current day; cap each eligible end at cutoff. `years=total_days(union(clipped_eligible_intervals))/365.2425`, round-half-even to1decimal after interval union. Synthetic complete nonoverlapping730days/365.2425=1.998672 (six decimals)→2.0years. Overlapping jobs count elapsed time once; missing starts or unknown coverage→null complete-career total (may separately show documented minimum with explicit label, not in this schema). Negative intervals invalid, one fully evidenced interval sufficient. Same date convention/context; refresh source revision/as_of, not campaign tick. Consumer biography display only. No P/A/D/C formula from years.

### C06 — Rating inference research boundary

**Deferred, not an executable estimator:** a hierarchical game-outcome model could include roster strength, opponent strength, patch, side and team effects plus coach effects. Coach effect requires connected coach transitions and overlap/control data; a single dominant roster under one coach does not identify it separately from team/resources. Before any admission require licensed complete samples, definition/unit lineage, train/holdout by time and organization, uncertainty interval and held-out calibration versus roster-only baseline. No defensible universal minimum sample or coefficient is established here. Failure to identify→unknown; posterior point estimate cannot masquerade as verified quality. No arbitrary mapping from estimated log-odds to Preparation/Capacity: mechanisms need their own measurements. Synthetic generated values are scenario abstractions, never estimates of named real coaches.

## 5. Candidate Numerical Profile v0.1

| Field ID | Type | Min | Reference/default | Max | Unit | Source/rationale | Confidence/status | Calibration dependency |
|---|---|---|---|---|---|---|---|---|
| preparation | integer N | 1 | explicit; trial48..87 | 100 | preparation points | current validation/C01 | current; synthetic trial | load/output balance |
| analysis | integer N | 0 | trial60 | 100 | legacy points | current pricing/band only | current inactive competitive | consumer decision ED-03 |
| adaptability | integer N | 0 | explicit; trial43..92 | 100 | adaptation points | current C02 | current | resolver contexts |
| capacity | integer N | 1 | trial55..120 | 1000 | load units | current validation, independent scale | current | sponsor/roster load |
| birth_year | integer N | 1900 | explicit/null | as_of year | Gregorian year | conservative catalog range | proposed technical | source / adult trial roster |
| generated age | integer | 24 | uniform integer24..55 | 55 | years at cutoff | descriptive synthetic only | proposed trial | population realism |
| experience_years | decimal N,1dp | 0 | measured/null | 80 | elapsed years | C05; ≤age−18 when generated | proposed descriptive | dated coverage |
| generated experience | integer | 0 | uniform0..min(30,age−18) | 30 | completed years | synthetic career guard | proposed trial | no ability causality |
| career_stage | derived enum | — | early<3; established3..9; veteran≥10 | — | label | experience projection | descriptive | no multiplier |
| grade g | integer support | -5 | 0 | 5 | profile points | pmf25/50/25 | synthetic | population calibration |
| tradeoff x | integer support | -2 | 0 | 2 | P/D exchange points | pmf25/50/25 | synthetic | joint-distribution check |
| template weights | integer | 0 | 4000/3000/3000 | 10000 | basis points, sum10000 | deliberate diversity | proposed | cohort goals |
| salary / fee / release_cost | integer | 0 | formula/contract | 1000000000 | minor units | current pay + shared cap | simulation, not source finance | economy tuning |
| load | integer | 0 | explicit,60/120 examples | 2147483647 | work-load units | checked sum | current-derived | sponsor portfolio |
| converted | integer | 0 | C01 | 100 | work/day | P bound | current-derived | overload curve |
| repeated_posture | integer | 0 | observed | 3 | recent results | fixture PredictabilityWindow | current fixture | configured window |
| adaptation term | integer | -10 | C02 | 7 | resolver points | A,D0..100; repeated≤3 | derived fixture bound | changing window requires new bounds |
| market band endpoint | integer N | 0 | C04 | 100 | legacy point bound | actor observation | not statistical confidence | observation design |
| coach slots / market candidates | integer | 1/0 | 1/3 | 1/3 | persons | trial/current company structure | bounded trial | capacity/performance |

g and x drawn independently conditional on template; age/experience use separate purposes. Experience may correlate with age by feasible support only, not generated ability. Generated biography uses no private-personality fields. Historical source facts outside technical bounds are quarantined for schema review, not silently clamped.

## 6. Deterministic generated-coach distribution

Use shared rng-v1 key grammar and version; each table entry ordinal order as below. Draw template by weights, g from[-5,0,5] weights[2500,5000,2500], x from[-2,0,2] same weights. Draw age then experience from the finite supports above using independent purposes. Define `birth_year=as_of.year-age` for synthetic year-only biography (not exact birthday). Role=head_coach. Availability follows allocation, not independent random employer flag.

| Template ID | Weight | P0 | A0 | D0 | C0 | Mechanism / cost trade-off |
|---|---:|---:|---:|---:|---:|---|
| preparation | 4000 | 80 | 60 | 50 | 65 | High low-load Work, capacity bottleneck, weaker adaptation |
| adaptation | 3000 | 55 | 60 | 85 | 75 | Better C02, lower Work, highest base wage |
| capacity | 3000 | 60 | 60 | 55 | 110 | Better overloaded Work, weaker unconstrained P |

`P=P0+g+x; A=60; D=D0+g-x; C=C0+2g`. No clipping/retry: validate all27support points before generation. Values fall within P48..87,D43..92,C55..120. Grade supplies positive common variation; x creates preparation/adaptation trade-off. Conditional covariance(P,D)=Var(g)−Var(x)=12.5−2=10.5; covariance(P,C)=covariance(D,C)=2Var(g)=25; Var(C)=50. Across templates, the exact mixture means for [P,D,C] are [66.5,62,81.5], covariance rows [[139.75,−120,−97.25],[−120,245.5,−0.5],[−97.25,−0.5,415.25]] in squared point/load units. Do not report conditional correlation as whole-cohort behavior. Analysis constant makes its correlation undefined, not a claim of perfectly known causal relation. Finite-support covariance check is arithmetic, not a seed run or empirical calibration.

Generation order: reserve definition/runtime ID → template/g/x → biography → deterministic name from admitted pool (duplicate names allowed, IDs not) → compensation quote → pool allocation under Team budget → employment+assignment or World candidate. No all-dimension-dominant coach from “famous” flag. Table permits grade dominance at higher cost within a template; claims of no universal best refer to cross-template load/adaptation/cost contexts, not pairwise mathematical impossibility. Three worked templates above satisfy sample requirement; world includes an additional lower-grade preparation coach P75/A60/D45/C55, salary1010.

## 7. Import, employment, updates and consumers

Import requires supported provider schema, stable external identity crosswalk, provenance/rights/time, explicit source coverage and staff role. Do not infer performance ratings or real pay from career history. A separately approved simulation initialization profile must meet the same current ranges and pay/load consumers as NPCs; origin never changes rules. Unknown capabilities block applicable initialization only, not source-catalog preservation.

Hiring: validate availability, current ownership, employer/team compatibility, cash for quoted fee+existing release obligation, one active Head Coach; atomic ownership move with same PersonId. Former coach returns to World candidate by current rule. Close old assignment/contract consistently; no simultaneous active staff episodes. Release/expiry follows existing authority; current interim coach generation has −12 skill shift, does not leave Company without a valid coach. Renewal retains identity/profile, updates employment terms and end date. Imported source refresh cannot overwrite these campaign facts.

P/A/D/C stay stable unless an approved rule mutates them; this specification adds no training/development growth. Capacity is not spendable fatigue. Preparation contribution accrues Work daily. Experience biography does not automatically tick simulation ability. Company.Information retains observation responsibility; CoachPolicy consumes actor-safe inputs and returns authority-bounded recommendations; no hidden ratings injected into staff UI or recommendations.

## 8. Validation and unresolved decisions

Schema: null public attributes+reason accepted in staging, rejected for initialization; generated complete numeric profile required; no fake external evidence; exact career date intervals; duplicate coach/person ID and incompatible employment rejected. Formulas: C01 load/capacity reversals, C02 negative result/history window, C03 price and renewal rounding, C04 Capacity omitted, C05 union rather than summed overlap. Distributions: enumerate27support points, probability1, moments/covariance, range bounds; generated age/experience feasibility, independent purpose stability. Runtime planned: hire/release/expiry/save identities, quote-once settlement, actor-safe recommendations, pay causes a real cash trade-off. Do not claim more expensive adaptation specialist universally superior to preparation specialist.

**OPEN QUESTIONS:** ED-03 whether to retain Analysis solely as compatibility field until a reviewed consumer exists; ED-02 candidate tables and wage sensitivity; ED-04 evidence for any public estimate; ED-05 catalog/assignment/save migration. Required next evidence is load-context and economy scenario testing after approval, not fabricated real coach rankings. No new Core skill beyond existing four is activated.
