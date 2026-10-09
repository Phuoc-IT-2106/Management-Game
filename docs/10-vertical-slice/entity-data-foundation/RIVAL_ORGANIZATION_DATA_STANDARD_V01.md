# Rival Organization Data Standard v0.1

Status: **PROPOSED — NOT APPROVED** · 2026-10-08  
[Package / authority](README.md) · [Shared identity / generation](SHARED_GENERATION_AND_VALIDATION_V01.md)

## 1. Company-level responsibility and fidelity

Organization là business owner và competitor, không đồng nhất với competitive Team. Company-first design liên kết roster/payroll/preparation/competition với cash, sponsors và survival. **FACT:** current [Rival](../../../src/ManagementGame.Domain/Model.cs) là compressed World entity: Id, Name, Strength, Analysis, Adaptability, Posture, Budget, Need. Nó chưa có người chơi, Coach, payroll, reputation, audience hay ledger riêng. Team/Definition references ở đây là proposed contract, không current fields.

Scope: one full player Company, one primary Team, bounded compressed rivals with one team reference each. Conceptual future organization có nhiều teams, nhưng multi-team/M&A/global sponsor networks không thuộc v0.1. World remains sole rival authority. Source/provider catalog is external evidence, not a third gameplay owner.

| Interaction | Player full fidelity | Rival compressed minimum / limitation |
|---|---|---|
| Competition | People, Coach, Plan, Work, contracts, detailed result effects | Strength/Analysis/Posture, fixture ID and result references; Adaptability stored but not consumed competitively |
| Talent | Explicit persons, one-owner transfer, employment and finite quoted market | No autonomous player/coach hiring currently; abstract talent reservation only in trial manifest |
| Sponsors | Contract eligibility, installments, load, promises, negotiation | Need+Budget selects eligible lapsed inbound offer; claim removes an opportunity; no rival revenue ledger |
| Reputation / audience | Existing Company pools and outcome effects | No independent values/updates; unsupported rather than invented popularity |
| Finance | Cash ledger, obligations, settlements, payroll/fees/prizes | Budget points, a bounded opportunity gate; no cash conversion, salary debt or bankruptcy model |
| Strategy | Explicit player choices / authority-limited policy | Need-driven strength drift, Analysis threshold response, seasonal reset |

The abstraction deliberately cannot answer rival net worth, staff workload, payroll or real survival forecasts. No simulation rating from origin or real-world fame. A later materialization requires approved identity, talent reservation and economic mapping; cannot spawn a second copy of a real person already in World/Company.

## 2. Source facts, estimates and schema

Financial layers must stay separate: (1) verified public statement with currency/date/consolidation basis; (2) explicit scenario endowment assumption; (3) generated latent profile/budget points; (4) unknown. Public valuation/revenue is not liquid game cash and never Rival.Budget. No live sources imported by this task. `real_organization.example.json` is a synthetic public-path shell with unknown numbers, not a claim about a real corporation.

Common envelope per shared spec; format=`organization-definition-example-v01`, schema_version=1, all keys required.

| Definition field | Type / constraint | Meaning |
|---|---|---|
| definition_id / definition_version | ID / integer≥1 | Stable catalog organization / revision |
| display_name / short_name | string1..120 /1..12 | Presentation; adapter must respect current Rival name≤60 |
| region_id / discipline_id | IDs | Market/discipline context; candidate LoL does not become approved canon |
| fidelity | enum `compressed` | v0.1 rival admission only; Company data stays existing full authority |
| compressed_reference_id | ID | Versioned comparable strength population, trial `legacy-tier70-trial` |
| initial_profile | object | Required profile shape below; source shell may be unknown |
| provenance | shared object | Origin, rights, cutoff, external identities |

Initial profile exact keys: `strength`, `analysis`, `adaptability`, `posture`, `budget_points`, `need`, `profile_basis`, `unknown_reason`. First three integer N; posture enum conservative/balanced/aggressive N; budget/need integer N. basis unknown requires six nulls+reason; generated_scenario requires all six valid and reason=null. Scenario assumptions are not verified source finance. Runtime `compressed_profile` in world fixture has exactly those first six fields, no basis/reason: initialization evidence stays in definition, runtime mutations under World.

Public source_facts exact keys: `public_financial_facts` (array), `reported_team_definition_ids` (unique ID array), `coverage` (`unknown`/`partial`/`complete`), `unknown_reason` (string N). Financial row exact keys: `metric` (revenue/cash/valuation), `amount_minor` (nonnegative integer), `currency_code` (uppercase3), `minor_unit_exponent` (integer0..3), `period_start` (date N), `period_end` (date), `consolidation_basis` (string1..120), `source_ref` (URI). Currency-specific minor units belong to source evidence; no automatic FX or conversion to game money. Point-in-time metrics use period_start=null. A reported value needs a real per-field citation/rights record in a future import bundle; empty unknown array is not zero revenue.

Generated source_facts=null. Unadmitted public initialization=null. Generated initialization exact key `organization_id` (runtime ID); owned teams derive from team.organization_id, not a second independent ownership list. World reference catalog resolves illustrative IDs; production requires complete admitted definitions.

## 3. Strategic attribute inventory and ownership

| Candidate dimension | Classification | State/policy/derived and named consumer |
|---|---|---|
| Strength | Current implemented | World mutable latent capability; Competition opponent strength |
| Analysis | Current implemented | World latent points; repeated-posture penalty and public-posture response threshold |
| Adaptability | Current stored; competitive consumer absent | Compatibility field; trial60 constant; no invented counter bonus |
| Posture | Current implemented | World strategic enum; opponent preparation comparison / observation / response |
| Budget points | Current implemented | Mutable sponsor-opportunity resource, not cash or real financial capacity |
| Need | Current implemented | 0/1 priority flag; sponsor eligibility and strength drift; not psychology |
| Financial capacity / operating commitments | Deferred for rivals | No cash/payroll ledger; source evidence separate, cannot infer affordability |
| Competitive ambition | Experimental archetype label | Explains initial Strength/Analysis/Budget trade-off; no extra multiplier |
| Talent investment tendency / recruitment | Deferred | No current rival hiring transaction; no hidden bids or unlimited recruits |
| Sponsor strategy | Current limited policy | Eligibility and ordinal selection R04, not brand/network optimization |
| Risk tolerance | Deferred | Posture is competition posture, not corporate risk appetite |
| Reputation / audience | Deferred as rival pools | Do not duplicate Company effects on empty rival state |
| Information capability | Current limited Analysis consumer | Public posture response; no access to private player finances or coach intent |
| Organizational capacity/load | Deferred for rival operations | No phantom staff simulation; R05 states actual limitation |
| Current priority | Derived from Need/opportunity state | Presentation can explain available claim, cannot guarantee outcome |
| Operational status / organization age | Descriptive source/candidate context only | No dormant/bankrupt transitions or age-based strength in v0.1 |

Avoid promoting labels to latent stats. Organizations differ through existing consumed fields and resource gates. Rival.Adaptability being present is not evidence that it currently affects the match. Preserve serialization until an approved migration; no silent deletion here.

## 4. Resources: Faucet → Pool → Sink

| Resource / owner | Source and accumulation | Spend / constraint | Consequence and recovery |
|---|---|---|---|
| Rival Budget / World | Current initial40; each season **reset to40**, not +40 or +20 | Claim costs10 and sets Need0; nonnegative, reachable current0..40 | No claim if<10; next reset refreshes budget; unused points discarded, not banked wealth |
| Sponsor opportunity / World | Existing bounded offer generation by market rules | Only one claimant; current expired inbound offers may be claimed R04 | Removes opportunity for Company; cooldown; no rival cash payout |
| Rival Strength / World | Current generated64..78; cadence drift and seasonal jitter | Clamp59..83 under current fixture; not spendable currency | Performance contribution, not guaranteed victory; no recruitment implied |
| Need / World | Initial and seasonal keyed0/1 | Claim sets0 | Gates at most one claim per rival between seasonal resets; cadence drift changes sign |
| Explicit talent / Company or World | Shared admitted person definitions; existing market replenishment separate | One owner, no duplicate current contract; quoted hires cost Company cash | Release/expiry returns same identity; compressed slots cannot also count as available persons |
| Company cash / Company Finance | Initial endowment once; existing contract/prize settlement only | Payroll, running costs, release/signing fees, fines; exact ledger reconciliation | Existing recovery mechanics; scenario buffer not a solvency guarantee |
| Company reputation/audience | Existing result/business events and reputation regression | Bounded rep0..100; no transfer into Rival.Strength by this spec | Business eligibility remains causally linked to competition |
| Company capacity/load | Coach capacity and roster/sponsor load | Prep converts under load C01/T06 | Coach/roster/sponsor choices change Work; no separate rival capacity guess |

Season resets are bounded faucets explicitly present in simplified rules, not real investment income. Aggregate rival pool at trial3rivals ≤120 after reset; proposed initial profiles consume25 of allowed30 initial points. The initial30 envelope is **not** a permanent season cap. Sum initial Company money must equal declared endowment minus actual setup costs; no duplicate grant when creating Team/Coach. Sponsor count and Need gate bound claims even if Budget remains. This prevents invented rival income/snowball in the spec; it is not evidence of calibrated long-run balance.

## 5. Eight formula/rule contracts

All witnesses synthetic. Current integer operators preserved. No statistically inferred rival finance/behavior or private knowledge. Missing source values remain unknown; missing required initialized profile fails admission. Rules R01/R03/R04/R07 use current runtime state without statistical sample requirements, current rules/tier context only. Observe actor-safe projections separately.

### R01 — Resource allocation

Decision: can a rival claim one qualifying sponsor opportunity? `capacity_to_claim = (Need>0 ? min(1,floor(Budget/10)) : 0)`. Inputs nonnegative integer Budget, Need0/1; output0/1 claim. B5/N1→0; B10/N1→1; B40/N1→1, **not4 actual claims**, because first sets Need0. B40/N0→0. Evaluate when offer expires. Money units never enter. Uncertainty: latent game state exact; public financial strength unknown. Tests: thresholds9/10, no negative balance, at most one claim before reset. “Wealthier” above threshold alone does not provide a meaningful current archetype advantage.

### R02 — Recruitment affordability

Current compressed result=`unsupported`: rivals perform no explicit person recruitment and have no salaries/fees/cash. Do not compute monetary affordability from Budget or silently create hires. When a person actually enters explicit market, existing one-owner/contract rules apply regardless of imported/generated origin.

Optional future full-fidelity transaction would use the existing Team feasibility structure: `headroom = cash - fee - release_obligations - h*(existing_daily_commitments + new_salary) - reserve_floor`, affordable iff≥0 and valid identity/slot/contract. Synthetic isolated cash60000, fee12000, no prior obligations, h7, new salary1000, reserve0→41000. This is an explanatory accounting identity, not an enabled rival mechanic; full world costs cannot be omitted in an implementation. Missing cash/terms→unknown, not an approved hire. Units minor currency; checked integers; snapshot transaction cadence. Tests planned: duplicate person, fee/release double charge, unrealistically free missing salary, h0 invalid. No statistical samples needed for known contract terms.

### R03 — Competitive investment priority

Current cadence: `if day % RivalCadence == 0: Strength'=clamp(Strength + (Need>0?1:-1), rival_min-5, rival_max+5)`; current RivalCadence4, generation range64..78 → dynamic clamp59..83. S72,N1→73; S72,N0→71. This is compressed drift tied to priority flag; **no money spent, development training or actual roster purchase** is implied. Output capability points integer; evaluate once per qualifying daily phase. Uncertainty only scenario meaning, not additional random term here. Tests:59/83 saturation, same day not applied twice, effect changes after claim, no payroll entry generated.

### R04 — Sponsor opportunity evaluation

Current [Market.Expire](../../../src/ManagementGame.Domain/Market.cs): iterate unclaimed offers with `Deadline < day`, sorted ordinal OfferId. Only Origin=Inbound can go to rival. Select first rival sorted ordinal Id with Need>0 and Budget≥10. Set offer.ClaimedBy=rival.Id, Budget−=10, Need=0; brand cooldown `day+floor(SeasonLength/2)` (28days current56). Others lapse by existing cooldown rule. Example B10/N1 claims one eligible inbound; B5 cannot; a second offer sees updated Need0. Deadline=day does not expire today.

Decision/input/output: one opportunity reallocation, not sponsor expected ROI or income. No payment added to rival cash because no such ledger exists. Ties use existing ordinal order, acknowledged systematic ID priority; optional utility ranking requires approval and separate identity-neutral fairness tests. Current actor inputs are public lapse/offer eligibility, not Company hidden Plan. Tests: outbound excluded, claimed offer excluded, boundary day, single claimant, no duplicate claim/cash, offer ordering deterministic. Company capacity/contract acceptance is unchanged.

### R05 — Capacity/load constraint

Current full Company: `load=BaseLoad+3*roster_count+sum(activeSponsor.Load)`, then Coach C01. Synthetic BaseLoad35+6×3+12sponsorLoad=65. P80/C65 yields80converted; an added55load makes120→43. This is an actual Company consequence of sponsor/roster decisions.

Compressed rival capacity/load output=unknown/unsupported, because no roster/staff/sponsor load model exists. Structural action constraint is R01≤1claim per season, **not** fabricated staffing capacity. No sample or rounding beyond current sum/RE; refresh Company daily, rival gate at opportunity. Tests: do not initialize Coach.Capacity for rival from budget; do not add double overload penalty; false public load remains unknown. Any future compressed load scalar needs a named resource flow/consumer before addition.

### R06 — Financial-pressure response

Current compressed procedure: if Budget<10, skip sponsor claim; otherwise apply Need rule. No borrowing, salary default, bankruptcy or rescue capital. B5/N1 stays5 when offer passes; next season reset40 may restore eligibility if new Need1. Output business opportunity state, units points. Evaluate expiry/reset cadence; no statistical financial distress estimate. Neither Need0 nor Budget5 means bankrupt.

Player full-fidelity counterpart uses actual Company obligations/ledger and existing recovery. Current roster below MinimumRoster6 also withholds match prize and schedules fine `round-even(fixture_prize*ShortfallFinePercent/100)` with current50%; it can apply on loss/forfeit, and repeated forfeits may terminate sponsors. This is a real company–competition cost, not duplicated to rivals. Tests: no negative points, no automatic rival cash injection, preserve Company causal receipts and no-double-settlement. Optional financial survival simulation remains out of scope.

### R07 — Strategy adjustment over time

On current4-day rival update, if public results exist and Analysis≥60, `Posture'=(last_company_posture+1) mod3` with Conservative0/Balanced1/Aggressive2; otherwise retain posture. Example observed Balanced1→Aggressive2 for A75; A45 retains own posture. This response uses public completed result, never future Company intent. Strength simultaneously follows R03.

At season rollover: `Strength'=clamp(Strength+keyed_range(9)-4,59,83)`; keyed_range9 gives0..8 uniformly under existing rng-v1, so jitter−4..4. Rechoose posture via existing keyed range3; **Budget'=40**, Need'=keyed_range2. Follow existing key/domain/version exactly in eventual adapter; no reimplementation here. Example S72+jitter−4→68; oldB5→40, not45. No extra archetype multiplier after these transitions. Tests: strict seed replay, boundary clamps, no accumulation of budgets, stable organization identity, current public-result visibility. Initial archetype differences can converge after reset; cannot claim durable long-term strategy without further tests/design.

### R08 — Compressed capability initialization

Proposed trial reference `legacy-tier70-trial`: base70 plus template offset s plus discrete q; `Strength=70+s+q`, `Analysis=A0-5*q`, `Adaptability=60`. q∈[-1,0,1] with probabilities[.25,.5,.25]. No ratio from public wins, reputation or payroll. Matched reference scale is **assumed synthetic legacy scale**, not calibrated from Player v0.2 or real competition. Strength initialization requires this reference and total anonymous talent reservations, not a claim of actual six-person average.

Opportunity template s0/A075 with q1 →S71/A70; q−1→S69/A80; no clipping on nine supported points. Units legacy points; integer arithmetic; output valid compressed profile plus source/version evidence. Generate once for an admitted slot, preserve ID; missing comparable real-source reference→unadmitted/unknown rather than reroll. Tests: support bounds, pairwise strength-analysis trade-off, aggregate budget/talent envelope, unknown real profile rejected for campaign use. No statistical sample requirement for a hand-authored scenario; cross-origin comparable real calibration is an unmet future gate.

## 6. Candidate Numerical Profile v0.1

| Field ID | Type | Min | Reference/default | Max | Unit | Source/rationale | Confidence/status | Calibration dependency |
|---|---|---|---|---|---|---|---|---|
| strength | integer N | 1 | reference70 | 100 | capability points | current snapshot range | current technical / proposed reference | resolver and supply calibration |
| current generation / dynamic strength | integer | 64/59 | configured / prior | 78/83 | points | current generation range ±5 growth boundary | fixture fact | balance, not canon |
| trial initial strength | integer | 69 | template+q | 73 | points | R08 finite support | synthetic | matched population reference |
| analysis | integer N | 0 | trial60 | 100 | analysis points | consumers R07/C02 | proposed explicit bounds; current snapshot lacks this check | contextual repetition tests |
| adaptability | integer N | 0 | trial60 | 100 | legacy points | stored field, no competitive consumer | compatibility only | future consumer decision |
| budget_points | integer N | 0 | current40; trial5/10 | 40 | opportunity points | current reachable/reset bound, not validator max | proposed candidate cap | sponsor opportunity rates |
| need | integer N | 0 | explicit | 1 | priority flag | generator range2 / claim reset | current generated invariant | behavior balance |
| posture | enum N | — | explicit | — | conservative/balanced/aggressive | current strategic enum | current | response behavior |
| rival_count | integer | 2 | trial3; current4 | 12 | organizations | bounded candidate workload | proposed trial | schedule/performance |
| teams_per_rival | integer | 1 | 1 | 1 | competitive units | v0.1 scope | proposed | no multi-team |
| initial_budget_total | integer | 0 | toy25 | 30 | opportunity points | trial aggregate envelope | proposed | initial opportunity balance |
| seasonal_budget_total | integer | 0 | 40×rival_count | 480 | opportunity points | reset40×max12, trial max120 | current rule / candidate scope | season balance |
| claim_cost / seasonal claim cap | integer | 10/0 | 10/1 if Need1 | 10/1 | points / claims per rival | R01/R04 | current | sponsor cadence |
| priority cadence / response threshold | integer | 4/60 | 4/60 | 4/60 | days / analysis points | current fixture/R07 | current fixture | config/code version |
| seasonal jitter | integer | -4 | uniform9 values | 4 | strength points | R07 current | current | long-campaign balance |
| template weights | integer | 0 | 4000/3500/2500 | 10000 | basis points sum10000 | proposed population mix | synthetic | prevalence calibration |
| q / q weights | integer | -1/0 | 0 /2500,5000,2500 | 1/10000 | points / basis points | R08 | synthetic | joint distribution |
| reserved players / coaches per rival | integer | 6/1 | 6/1 | 6/1 | anonymous slots | offline capacity manifest | proposed | future materialization |
| public amount_minor | integer N in evidence | 0 | verified only | 9007199254740991 | source currency minor units | exact interoperable integer ceiling | technical, no finance inference | currency/date/consolidation |
| minor_unit_exponent | integer | 0 | source-specific | 3 | base10 exponent | evidence currency metadata | technical | supported currency registry |
| reputation/audience/capacity/cash for rival | absent / unknown | — | no default | — | unsupported | no current state/consumer | deferred | separately approved finance design |

All quantitative object fields inherit shared version/day/count/ID bounds. A profile outside candidate ranges fails staging/admission; no coercion. Current snapshot's weaker checks for Rival.Analysis/Adaptability/Need/Budget upper bound are recorded as compatibility facts, not silently changed C# validation.

## 7. Generation, archetypes and bounded world

Required inputs: same pinned seed/rng/version/reference as shared spec, region/tier context, organization/name pool, ordinal slot registry, initial point envelope, sponsor opportunity constraints, anonymous talent/staff reservations. Age, financial band and brand position may label a future scenario but have no automatic stat effects or new fields in this schema.

| Template / weight | S at q0 | A0 | D | Posture | Budget | Need | Representable trade-off |
|---|---:|---:|---:|---|---:|---:|---|
| competitive_pressure /4000 | 72 | 45 | 60 | aggressive | 5 | 1 | Higher immediate strength, cannot afford initial sponsor claim, no A≥60 response |
| opportunity_contest /3500 | 70 | 75 | 60 | conservative | 10 | 1 | Lower immediate strength, can claim, stronger repeated-posture response |
| balanced_reference /2500 | 71 | 60 | 60 | balanced | 10 | 1 | Middle strength/analysis, eligible for one opportunity |

Finite mixture support9. Conditional covariance(S,A)=−5Var(q)=−2.5, Var(q)=.5. Whole-mixture means [S,A]=[71.05,59.25], covariance rows [[1.2475,−13.7125],[−13.7125,180.6875]] in squared points. This is an authored diversity model, not an empirical correlation. Initial trio fixture deliberately one each, q0; not a random seed output and not claimed to realize40/35/25 marginal frequencies. For a general bounded cohort use shared largest-remainder quota allocation then keyed assignment to stable slots; sum initial template budgets≤30 for the trial3. Other sizes require explicit envelope adjustment or deterministic reject, not funds created later.

Competitive threat diagnostic for comparing this exact resolver context is `Strength+floor(repeated_posture*Analysis/30)`, omitting equal common terms; no stored bonus. At repeated0: pressure72>contest70; repeated3:72+floor135/30)=76 versus70+floor225/30)=77, order reverses. This demonstrates one contextual trade-off, not full strategic dominance proof. Need-claim interactions change future drift, and season reset erases Budget differences; durable archetype policies need separate approval.

“Win-now investor” only partially representable as competitive_pressure label; no investment ledger implied. Talent developer, financially conservative operator, commercial brand with audience, and complex risk trader are deferred because current consumers do not exist. Do not add private motives or company lifespan parameters to fill archetype names.

Order: reserve org/team IDs → choose template/quota/q → validate initial points → reserve6anonymous players+1coach each → create team/registration refs → allocate shared explicit Company/World talent separately → schedule/final graph validation. Never instantiate a full hidden roster per rival. Rival reservation totals18players/3coaches + explicit12players/4coaches =30/7 in fixture. Anonymous supply accounting bounds scope; it does not prove elite-talent quality distribution. Generated explicit talent remains subject to approved Player generator, which is not implemented by this task.

## 8. Lifecycle, validation and open decisions

Runtime under World: immutable IDs, source definitions retained; cadence and seasonal updates R03/R07; sponsor claim R04; no current hiring/debt/bankruptcy cascade. Renames update display revision, not definition/entity identity. New source financial news cannot update campaign resources. Any explicit player introduced later must be a unique admitted person, removed from previous owner/anonymous reservation in one approved transaction; historical membership never creates duplicate employment. Save migration must preserve rng/content/schema identities, not regenerate rivals on load.

Required design cases: invalid budget/Need/profile bounds; unknown public balance accepted in staging but not fabricated; orphan team; no shared persons between owners; offer claimant uniqueness; two expired inbound offers and Need resetting; Analysis59/60 threshold; strength clamps; current seasonal budget reset and aggregate bound; no automatic income from winning; counterexample repeated posture; full Company obligations remain financially meaningful. Runtime seed replay/multi-season economy and materialization tests remain planned, not executed by these docs.

**OPEN QUESTIONS / Director:** ED-02 initial resource/template trial versus current B40; ED-03 inactive Adaptability and long-term archetype convergence; ED-04 calibrated common strength reference and provider rights; ED-05 reference/save migration or any explicit rival recruitment. Risks include ordinal sponsor priority, unrealistically homogeneous seasonal behavior, pseudo-financial precision, abstract talent counts mistaken for a real roster and source finance mistaken for game currency. Deliverable supports a bounded reviewable trial without claiming these are solved production systems.
