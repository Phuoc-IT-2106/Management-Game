# Team Data Standard v0.1

Status: **PROPOSED — NOT APPROVED** · 2026-10-08  
[Package / authority](README.md) · [Shared primitives / validation](SHARED_GENERATION_AND_VALIDATION_V01.md)

## 1. Responsibility and relationships

**PROPOSAL:** Team là competitive unit thuộc một Organization. Organization sở hữu tiền, employment và business obligations; Team không phải Company thứ hai. Phạm vi trial: một discipline candidate LoL, một primary team của Company, một compressed team mỗi rival. Current fixture dùng Role Arena A–E; top/jungle/mid/bot/support trong examples là proposed role policy riêng, chưa có approved adapter.

Team DefinitionId mô tả identity/catalog; Team EntityId là unit trong campaign, OrganizationId là owner. Team → organization, registrations → team+competition+season, fixture → registered teams; membership → person+team; staff assignment → coach+team; employment → person+employer. Ratings và salary chỉ đọc từ People/Contracts authority. Ownership transfer/M&A, nhiều đội cùng công ty và cross-discipline eligibility deferred.

## 2. Evidence, estimates and current compatibility

**FACT:** Current [Model](../../../src/ManagementGame.Domain/Model.cs), [Competition](../../../src/ManagementGame.Domain/Competition.cs) và [Observation](../../../src/ManagementGame.Application/Observations.cs) không có dedicated authoritative Team aggregate. Company.People, Company.Coach, Company.Plan/Work và World fixtures/results cung cấp competitive projection. Current competition chỉ đọc Person.Execution/Adaptability/Consistency/Readiness và các inputs đã triển khai; bộ player statistics v0.2 không tự map sang E/A/K.

Public roster, staff names, dates, competition entry và results có thể là source facts khi có evidence. Team strength, causal coaching impact, private contract terms và chemistry không phải facts suy ra từ tên đội hoặc win rate. Public observed results ≠ latent capability ≠ actor estimate. Imported valid roster bindings được giữ khi initialize; thiếu ratings/rights thì staging hợp lệ nhưng chưa được admit. Generated path chỉ khác acquisition, không có origin bonus.

## 3. Static definition and field dictionary

Common envelope/provenance trong shared spec; format=`team-definition-example-v01`, schema_version=1. Mọi field dưới đây required. N nghĩa là nullable có lý do. Unknown keys fail; không support arbitrary provider payload ở definition.

| Field | Type / constraint | Meaning / consumer |
|---|---|---|
| definition_id / definition_version | ID / positive integer | Stable catalog identity / revision |
| display_name / short_name | string1..120 /1..12 | Presentation, never seed or join key |
| discipline_id | ID | Trial `discipline:lol-candidate`; eligibility policy reference |
| region_id | ID | Versioned regional pool context; no innate ability bonus |
| organization_definition_id | DefinitionId | Initial catalog affiliation, runtime owner held separately |
| competition_definition_ids | unique ID array,1..4 trial | Source/intended competition context; not campaign registration |
| representation | enum explicit/compressed | Initialization fidelity, not public entity quality |
| role_policy_id | ID | Trial `roles:lol-candidate-v01`, exactly five primary roles |
| provenance | shared object | Origin, source cutoff, rights and external identities |

`source_facts` public path object has exactly `reported_roster`, `reported_staff`, `history`, `coverage` (`unknown`/`partial`/`complete`), `unknown_reason` (string N). Generated path uses null. Empty arrays with coverage=unknown mean no evidence, not no staff/players. Nonempty entries:

| Array | Exact row keys / semantics |
|---|---|
| reported_roster | person_definition_id (ID), role (role ID), start_date (date N), end_date (date N), source_ref (URI); end exclusive, unknown endpoints explicitly null |
| reported_staff | coach_definition_id (ID), staff_role (`head_coach`), start_date N, end_date N, source_ref; same date convention |
| history | competition_definition_id (ID), season_label (string1..120), matches (integer), wins (integer), losses (integer), source_ref; same resolved-match unit, wins+losses=matches; draw formats unsupported in this trial |

`initialization` is null for unadmitted public example; generated example exactly `team_id`, `organization_id`, `registration_id` (runtime IDs). These refs bind the supporting world fixture. They do not authorize production ingestion or a second state store. Catalog-only references must exist in a declared manifest; absent full definitions remain illustrative stubs, not production admission.

## 4. Runtime-owned state and attributes

| Candidate | Classification / owner | Named consumer or disposition |
|---|---|---|
| Identity, initial affiliation, discipline/region | Core proposed definition | Catalog/import/initialization; immutable revision |
| Runtime owner, competitive unit reference | Core proposed under Company/World | Eligibility and schedule joins, no new root authority |
| Membership episodes / roles | Core proposed under People | Select legal lineup; no repeated ratings/contracts |
| Active lineup / reserve | Current Plan + proposed projection | Plan owns selected IDs; reserves=owned eligible roster minus lineup |
| Availability/readiness | Current People | Recruitment reset, daily recovery, match fatigue; absence/injury mechanisms deferred |
| Lineup capability / role coverage | Current-derived / Core projection | Resolver base contribution / admission and plan validation |
| Competitive preparation | Current Work-derived | Resolver preparation term; never a stored extra team buff |
| Composition fit | Deferred | Needs approved draft/utility/role consumers; Player v0.2 G1 remains closed |
| Chemistry / hidden cohesion bonus | Rejected | No justified mechanism; no field or multiplier |
| Roster stability | Experimental diagnostic | Jaccard of previous/current roster IDs; explanation only, no strength modifier |
| Recent form / opponent-adjusted record | Experimental diagnostic | Review past results, not independently rewarded rating |
| Competitive reputation | Derived Company projection | Company reputation already drives business; no duplicate Team reputation pool |
| Matches/wins/losses/win rate | Derived World results | Results view; unresolved/duplicate/cancelled fixtures excluded |
| Registration / season / schedule / status | Core proposed World references | Registered, scheduled or completed projections; history not current registration |
| Qualification / promotion / relegation | Deferred | No rules added; current season rank/prize consumers remain unchanged |

The world fixture dictionary is normative for proposed runtime reference shapes. Fixtures/results remain World-owned. Team history caches, if later approved, must be disposable projections keyed by result revision and window; cannot independently mutate wins or rewards. One player current owner, one active team assignment, one current employment; historical assignments closed before transfer.

## 5. Formula contract and numerical witnesses

All numbers below are **synthetic**. `RE` = current Numbers.Divide, round-to-even; `F` = integer floor for nonnegative inputs. Diagnostic outputs use Decimal precision50, six decimal places, half-even, as shared spec. Null input never becomes0. Current expressions are preserved, not replacements. Trial lineup size n=5 and one person per required primary role.

### T01 — Lineup capability (current calculation)

Inputs: selected owned legal people, E_i=Execution and r_i=Readiness. Output B in capability points:

`B = RE(sum(E_i*r_i), n*100)`.

Example E=[70,68,72,66,74], r=[90,80,100,70,90]: products=[6300,5440,7200,4620,6660], sum30220, denominator500, raw60.44 → B=60. Multiplying average E by average readiness is not equivalent and is prohibited. Context: current legacy E/readiness scales only; no role/league normalization inside this formula. Requires complete legal lineup, no statistical sample threshold. n=0 or missing owned rating fails plan/admission; public estimate uses T07. Recompute on lineup/readiness change and match resolution. Consumer: existing resolver base term, once. Tests: E100/r100→100; E0/r1→0; readiness0 fails current snapshot admission; ties-to-even; invalid roster cannot divide by zero.

### T02 — Role coverage and suitability (structural, not a buff)

For required role set R, `coverage=|{role(p):p eligible and available} ∩ R|/|R|`; role suitability `s(p,r)=1` iff p.primary_role=r, else0. Trial legal lineup iff exactly5 unique eligible owned IDs and one distinct required role each. Full roster6..8 alone does not ensure coverage. Four of five roles → coverage=0.800000; adding a second top still leaves0.8. No penalty multiplied into B: illegal lineup is rejected/forfeit follows existing rules. Missing role data gives admission error, not role0. Context fixed role-policy version; no secondary-role proficiency inferred. Recompute after roster/eligibility changes; no historical samples. Tests: duplicate IDs, all-top roster, valid five roles, missing role-policy, current min-roster6 business check distinct from five-person match lineup.

### T03 — Recent competitive form (experimental diagnostic)

Within preceding28campaign days including current day d (`max(1,d-27)≤match_day≤d`), take latest≤8 resolved eligible fixtures ordered `(day,id)`; require n≥3. For each, y_j is observed win0/1, p_j is **pre-match actor-visible** forecast in[0,1], frozen before result. `form=mean(y_j-p_j)` in[-1,1]. Example y=[1,0,1], p=[.6,.4,.5]: residuals[.4,-.4,.5], sum.5/3=0.166667. No forecast record exists in current runtime; therefore current form is unknown, not reverse-engineered from realized random inputs. Three matches is only a trial display minimum, not evidence of statistical reliability.

Use same forecast model/version, format and context; mixed versions partition, not pool. Require all selected records to have valid forecasts; report n and window; do not selectively omit losing/missing predictions. n<3 or missing forecast→unknown with reason. Wins/losses are bounded, not winsorized. Recompute after result/day-window change. Consumer: proposed review display only; no match/morale/business multiplier. Tests: empty/two samples unknown, n3 example, range±1, stale-window exclusion, forecast created after outcome rejected.

### T04 — Competition-strength standardization (experimental diagnostic)

Predeclare opponent bands and reference weights π_b summing1, same discipline/format/period and measurement version. For each band require n_b≥3, then `standardized_win_rate=sum(π_b*wins_b/n_b)`. This is direct standardization for comparison, **not a latent ability or causal tier bonus**. Toy weak opponents6/8=.75, strong1/4=.25, π=[.5,.5] → .5; raw7/12=.583333. Output fraction0..1; report both sample counts and weight profile.

Trial bands use a frozen pre-match actor-visible opponent point estimate on an admitted comparable legacy scale: weak `[0,70)`, strong `[70,100]`, weights .5/.5. No such estimate or incompatible scale→unknown; do not obtain it from hidden rival truth. Bands are assigned before outcomes; thresholds must be versioned and independently justified before actual use. One missing band, zero n, incompatible contexts or no approved cross-league bridge→unknown; do not reweight observed bands to conceal absence. Current context has no approved public cross-league mapping. Recompute at result/window change, trial window56days inclusive `[max(1,d-55),d]`. Consumer: proposed analytical comparison only. Tests: toy=.500000, no data in strong band→unknown, all wins→1, duplicate games excluded, series/game unit mismatch rejected.

### T05 — Availability-adjusted lineup (current policy-compatible)

For each role r choose `argmax(E_p*Readiness_p)` among owned eligible available players of that role, tie ordinal PersonId. Evaluate chosen tuple by T01. Under trial one primary role/person, choices are disjoint; generalized multi-role assignment deferred. E80/r50 gives4000 (40normalized), E70/r90 gives6300 (63): a lower nominal E may be better now. Reserve membership alone supplies no bonus.

Complete known own-roster inputs required, no empirical sample. Unknown external candidates use separate actor bounds, not hidden truth. If no person for a role, return infeasible; never borrow a World candidate. Recompute after readiness/roster/plan changes. Context is same role policy and current match, no team fame correction. Consumer: current CoachPolicy selection pattern / proposed roster comparison. Tests: duplicate owner, missing role, deterministic ID tie, stronger-but-tired reversal, unavailable exclusion. Generation adds budget constraints before this selection; the selector does not sign contracts.

### T06 — Preparation contribution (current owners and rounding)

Inputs P=Coach.Preparation, C=Coach.Capacity, `L=BaseLoad+3*People.Count+sum(activeSponsor.Load)`.

`converted=RE(P*C,max(C,L))`; daily Work increment for component a is `F(converted*Plan.a/100)` for execution/opponent/meta. Allocations sum100. P80,C65,L120→RE(5200,120)=43; allocations50/30/20 give21/12/8, sum41. Rounding residue is lost; no redistribution or second overload penalty.

At match: opponentMatch=100 if expected posture correct, otherwise25; metaFactor=70 if current Meta balanced, otherwise120. `prep=RE(Work.Execution + F(Work.Opponent*opponentMatch/100) + F(Work.Meta*metaFactor/100), PreparationScale)`. Example Work300/150/90, wrong expected opponent and aggressive Meta →300+37+108=445; /30→15. Work here is accumulated work, not the daily increments above.

Units: work/day and resolver preparation points. Capacity>0 and PreparationScale>0 required; incomplete plan does not invent work. No sample minimum; context current rules/config revision. Update daily Work once, then resolver consumes it once; preserve existing consumption/reset lifecycle. Consumer: Simulation preparation and Competition. Tests: L≤C converts P exactly; C0 invalid; nonnegative floors; allocation100%; no analysis-derived or Team preparation bonus added afterward.

### T07 — Incomplete-information estimates (projection)

Given actor-known bounds [l_i,u_i] on E and known r_i, lineup interval=`[sum(l_i*r_i)/(n*100), sum(u_i*r_i)/(n*100)]`, reported six decimals; a mathematical envelope, not a calibrated confidence interval or probability. Five players each E∈[60,80], r80 →[48,64]. If readiness also unknown, use admitted bounds with nonnegative interval multiplication; unknown/unbounded essential input→unknown. Do not treat public unknown as[50,50]. Requires all five eligible person bounds, no n<5 shortcut.

Existing compressed rival observation is separate: `width=max(4,F((100-Company.Information)/4))`; keyed noisy estimate around true strength; display `[max(0,estimate-width),min(100,estimate+width)]`. Synthetic Information70→width7; estimate70→[63,77]. This displayed band is also not a statistical confidence guarantee. Coach.Analysis does not narrow it. Observer refresh must reuse current deterministic observation identity, not reroll until preferred.

Consumer: actor-safe roster/rival comparison, not resolver latent-state overwrite. Normalize only comparable admitted scales; public Player v0.2 statistics do not yet supply E bounds. Update when observation/roster/readiness version changes. Tests: missing estimate→unknown, boundsordered, known point estimate collapses, no hidden true E exposed to staff/UI, correlated uncertainties do not justify probability claims.

Additional diagnostics: `win_rate=wins/resolved_matches` with n=0→null; record by season uses same unit/context. `stability=|old∩new|/|old∪new|`, empty union→unknown; changing one player in a five-person lineup gives4/6=.666667. Only display changes; no automatic performance bonus.

**Current resolver composition, unchanged:** probability in parts-per-million is `clamp(500000+(B-rival.Strength+prep+matchup+adaptation+variance)*MatchScale,50000,950000)`. Matchup is3 when posture matches public Meta, otherwise−1; adaptation is C02; variance spread=`max(1,Variance+4*posture_index-floor(mean_consistency/20))`, integer uniform over ±spread. Current MatchScale12000, Variance8; Consistency is integer-floor mean of selected people. These technical rule inputs are current fixture values, not new Team ratings. Synthetic B75, opponent70, prep0, matchup3, adaptation4, variance0 →644000; result draw900000 would lose. This is arithmetic with a hypothetical draw, not a seeded simulation. Neither B nor a diagnostic grants guaranteed wins; same probability/outcome then drives existing Finance/Commercial receipts once.

## 6. Candidate Numerical Profile v0.1

Shared IDs/versions/day/amount/sample bounds also apply. Values are candidate references, not production canon.

| Field ID | Type | Min | Reference/default | Max | Unit | Source/rationale | Confidence/status | Calibration dependency |
|---|---|---|---|---|---|---|---|---|
| roster_count | integer | 6 | 6 | 8 | persons | Current fixture Balance; trial exactly6 | current fixture / proposed trial | roster policy |
| lineup_count / role_count | integer | 5 | 5 | 5 | persons/roles | working discipline candidate | proposed | discipline decision |
| reserve_count | integer | 1 | 1 | 3 | persons | roster minus lineup | derived trial | substitution scope |
| execution / adaptability / consistency | integer | 0 | explicit,70/60/60 examples | 100 | legacy points | current Person validation | current, synthetic values | Player adapter not approved |
| readiness | integer | 1 | explicit | 100 | readiness points | current snapshot and fatigue floor1 | current | readiness balance |
| lineup_capability | integer | 0 | T01 | 100 | capability points | E*readiness | current-derived | resolver balance |
| role_suitability / eligibility | integer/bool | 0/false | evaluated | 1/true | indicator | primary-role rule | proposed projection | role policy |
| coverage / stability / win_rate / standardized_win_rate | decimal N | 0 | derived/null | 1 | fraction | T02/T04/diagnostics | diagnostic | source coverage/band panel |
| recent_form | decimal N | -1 | null | 1 | forecast residual | T03 | experimental | forecast protocol |
| recent_form.window / limit / min_n | integer | 28/8/3 | 28/8/3 | 28/8/3 | days/matches/matches | bounded trial | proposed | out-of-time forecast evaluation |
| standardization.window / min_cell_n | integer | 56/3 | 56/3 | 56/3 | days/matches | trial season / floor | proposed | competition comparability |
| history.matches / wins / losses | integer | 0 | actual/unknown row omitted | 10000 | resolved matches | consistent match unit | source fact when verified | deduplication; n=w+l |
| opponent reference weight | decimal | 0 | .5/.5 toy | 1 | fraction, sum1 | reference panel | synthetic | fixed target population |
| allocation_execution/opponent/meta | integer | 0 | explicit,50/30/20 | 100 | percent, sum100 | current Plan | current | plan policy |
| daily converted / component work | integer | 0 | formula | 100 | work/day | P≤100 | current-derived | P/C/load |
| accumulated Work component | integer | 0 | current ledger value | 2147483647 | work | checked integer; overflow fails | technical | lifecycle/work cap |
| prep resolver output | integer | 0 | formula | 2147483647 | resolver points | checked computation | current-derived | PreparationScale/overflow |
| Company reputation projection | integer | 0 | fixture45 | 100 | reputation points | existing Company | current | no duplicate Team pool |
| budget_horizon | integer | 7 | 7 | 7 | days | trial screening only | proposed | cashflow scenarios |
| initial_cash / salary / fees / reserve_floor | integer | 0 | explicit, floor0 | 1000000000 | minor units | shared amount contract | proposed technical | economy/currency |

The prep output technical cap is not a recommended play value. Checked intermediate arithmetic must be wide enough; validation rejects any state exceeding storage bounds before cast. Derived metric windows and quotas are config revisions, not hidden global defaults.

## 7. Import, generation and lifecycle

Import: provider rights+revision → raw snapshot → identity crosswalk → dated public roster/staff/results → normalized definition → separately admitted simulation profile → initialization. Partial source roster must not remove existing campaign people. Resolve ambiguous aliases by evidence; never merge by name only. Salary/release terms need approved scenario values if unavailable, clearly separate from reported financial facts. No public rating derivation from team win rate. League/tier is a context reference until cross-context calibration exists.

Generation follows shared hybrid order. Required inputs: pinned seed/rng/config version, discipline/role policy, tier reference, region pool, organization endowment, fixed imported bindings, shared finite talent+coach pool, roster bounds and competition eligibility. Precompute cost from the same employment terms that will be committed. Initial feasibility:

`cash_after_buffer = starting_cash - committed_setup_fees - 7*(sum(player_salary)+coach_salary+daily_operating_cost) >= reserve_floor`.

No unsigned sponsor revenue or expected prize is credited. Trial starting roster follows current CampaignFactory initial placement with no signing fees; subsequent market acquisitions use actual quoted fees and release obligations. Synthetic six E=[70,68,72,66,74,62] salaries `floor(E²/5)`=[980,924,1036,871,1095,768], sum5674. Prep coach1080 + op2500 →9254/day; seven days64778; cash260000 leaves195222≥0. This proves a seven-day initialization buffer only, not season solvency.

Two **candidate objectives**, both constrained by the same budget and current context:

| Archetype | Exact tuple ranking, descending unless stated | Trade-off |
|---|---|---|
| lean_operation | min total daily payroll; then max T01; then ordinal IDs | More cash buffer, usually less current competitive output; no invented growth potential |
| established_competitor | max T01; then max daily converted work at declared load; then min payroll; then ordinal IDs | Pays more to improve present readiness/prep; lower runway |

No stochastic skill bonus per archetype. Trial one primary team selects `lean_operation` or `established_competitor` explicitly; probability not inferred from one sample. Multiple explicit teams and development-oriented roster growth are deferred. Example world is hand-authored and is **not** claimed to be optimizer output.

Offline finite-pool audit of the12authored players and4coaches yields2016legal tuples with seven-day obligations56546..65807. At starting cash60000 only596tuples meet the buffer, versus all2016at260000. This witness makes the budget constraint concrete; it is not a seed run, a selected generated roster or proof of long-term solvency.

Shared bounded enumeration and atomic reject rules apply. Replenishment is existing World market behavior (every28days in current fixture, target4 candidates plus role deficits,3 coach candidates), not this initializer rerunning. Player sign keeps identity and caps readiness70; daily+1 cap100; selected match participants−8 floor1. Release/expiry moves the same person to World; replacement contracts do not reroll biography. Membership intervals close, roster projections refresh. Current release prevents uncovered role; future Team join must preserve it. Coach assignment changes only with valid employer transaction. Competition results update World and existing Company business effects once; no Team reward copy.

## 8. Consumers, validation and open decisions

People owns identity/readiness/membership; Contracts owns employment terms; Competitive Management owns Plan/Work; load derives from People, Contracts and Organization inputs once; Competition owns legal-plan validation and resolver; World owns fixtures/results/market; Finance owns cash, payroll obligations and prizes; Observation builds actor-safe bands; UI receives projections. These are existing module boundaries or proposed projections under them, not additional authorities.

Required design checks: exact schema and external crosswalk; primary/reserve disjoint+complete; role-policy/ref validity; no duplicated contracts; history never creates owner; numeric examples T01–T07 including unknown paths; independent current business minimum roster6 versus match lineup5; budget failure aborts whole allocation; salary sensitivity; prepared lineup can still lose due current variance/opponent and context; diagnostics never alter probabilities. Executed checks are listed in README; seeded generation, adapter calibration and runtime integration remain planned.

**OPEN QUESTIONS / Director:** ED-01 role/discipline and Team reference admission; ED-02 approval of bounded numerical trial; ED-04 actor forecast and opponent reference panel before diagnostics; ED-05 any runtime/schema migration. Risks: legal lineup versus financially viable roster, public roster date ambiguity, counting series twice, unexplained E mapping, duplicate reputation and phantom synergy. No one of these requires blocking delivery of the documentation examples.
