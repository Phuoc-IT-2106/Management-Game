# NPC Generation v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

Dependencies: [authority and blockers](../../../10-vertical-slice/player-data/README.md), [shared contract](PLAYER_DATA_CONTRACT_V1.md), [catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md), [rating scale](PLAYER_RATING_FORMULAS_V1.md), [validation](VALIDATION_CALIBRATION_V1.md), [existing rng-v1](../../../../src/ManagementGame.Domain/Determinism.cs), [current generation](../../../../src/ManagementGame.Domain/Seasons.cs).

## Purpose and limits

**FACT:** existing generation independently rolls Execution/Adaptability/Consistency within fixture ranges and uses a shared player:generated template ID. **PROPOSAL:** a later bounded generator should produce correlated capabilities and durable individual definition IDs, preserving existing rng-v1. This document specifies candidate generation, not an implementation or a change to current content.

An NPC has authored latent game capabilities, no fabricated provider account, public match record or measured rating confidence. After campaign initialization the resolver must consume the same effective capability contract for every origin. Champion repertoire, measured Adaptability, development potential and personality remain deferred/null. Do not generate secret bonuses for them merely because generation makes them easy to invent.

## Inputs and deterministic identity

Required future configuration: generator_version=npc-candidate-v01; RNG=rng-v1; world_seed (uint64); content/configuration ID and content hash; explicit as_of date; ordered region allocation; cohort_id; population N; sorted name pools; all tables below; role/age weights; snapshot of occupied definition/entity IDs. No current time, network response or process-global RNG. Reject missing configuration, N<5 or N>10000 for this bounded candidate; 10000 is a technical review cap, not required game population.

Region weights are positive integers summing to 10000 basis points, keyed by unique region IDs. Largest remainder means floor(N×weight/total_weight), then assign remaining slots in descending fractional-remainder order; ties use the declared category order, or ordinal region ID for regions. Name pools contain nonempty, distinct NFC strings sorted ordinally before sampling. Slot numbers are globally unique within a cohort, not counters restarted per region. Generated output IDs must pass the contract's length/character limits; invalid input namespaces fail before drawing.

Stable key = cohort_id + region_id + zero-padded immutable slot number with labeled delimiters, e.g. cohort:example-001:region:example:slot:000042. Content policy assigns region to slots before player generation. Team/employer does not appear in this key. Definition ID uses world namespace, cohort and slot; entity ID uses the campaign's persisted allocator at instantiation. Both survive team transfers. Re-run with identical configuration returns the same definitions in slot order; it does not create additional people.

Use KeyedRandom.Range(world_seed, "player-data-npc-v01", stable_key, purpose, count) and the existing KeyedRandom.Draw for ordering. Distinct purposes: role-order, stage-order, band-order, archetype, base-ability, age, experience, factor-x, factor-y, name-given, name-family. Purpose names and draw-index conventions are versioned; do not re-use an ability draw for scouting. New attributes require new purpose keys, not insertion into a sequential stream. Region/organization/player namespaces supply context without replacing rng-v1 or deriving ad hoc random algorithms. Organizational assignment uses a separate domain and cannot alter person identity or ability.

**FACT:** rng-v1 already hashes length-prefixed NFC UTF-8 fields with SHA-256 and uses rejection sampling for Range. Reuse it; no System.Random, floating Gaussian sampler, modulo shortcut, or custom RegionSeed algorithm. Ordering draws sort by unsigned Draw value then ordinal slot key. Category pools and output definitions have a fixed ordinal order. Existing generator behavior must stay pinned for old campaigns.

## Population allocation

**PROPOSAL — synthetic starter population, not real demographics.** N is a cohort size decided before generation. Region controls content/name selection and allocated population only, not skill by nationality or ethnicity. For v0.1 one configured region is sufficient; no global-world simulator is required.

1. Allocate configured region counts by largest remainder. For each region allocate roles top/jungle/mid/bot/support at 20% each; largest remainder ties follow that role order. Require at least five slots per represented region, or reject configuration. Exact role quotas guarantee baseline coverage.
2. Allocate career stages 25% rookie, 55% established, 20% veteran by the same largest-remainder rule (ties rookie, established, veteran).
3. Exceptional-band count = floor(.02N). Allocate the remaining N minus this count among replacement/regular/standout in ratios 40:45:13 using largest remainder. Thus small cohorts never exceed a 2% exceptional cap through rounding.
4. Assign the counted role, stage and band labels to slots through separate keyed sorted orders. This preserves each marginal quota without assuming band and role are independent after later calibration. Stage then changes ability/experience as below. Report realized per-role rarity; do not secretly resample weak roles until they become strong.
5. Freeze the cohort manifest before assignment to organizations. Changing N/configuration creates a new cohort/artifact, not a reroll of existing people. Organization assignment must use role eligibility and approved roster limits; candidate overflow stays World-owned. An impossible lineup allocation fails validation rather than duplicating players.

For N=200 and one region: 40 per role; stages 50/110/40; ability bands 80/90/26/4. For N=10: exceptional0; remaining bands 4/5/1. These quotas are illustrative and must be calibrated against the campaign's recruitment scope.

## Conditional ability and career model

| Ability band | Target share | Base ability B values | Integer choice weights |
|---|---|---|---|
| replacement | 40% before capped allocation | 35, 40, 45 | 25, 50, 25 out of100 |
| regular | 45% | 48, 55, 62 | 25, 50, 25 |
| standout | 13% | 66, 70, 74 | 25, 50, 25 |
| exceptional | ≤2% | 80, 83, 86 | 25, 50, 25 |

Draw u=Range(...,100); [0,25), [25,75), [75,100) selects the corresponding base. This discrete distribution is intentionally transparent and requires smoothing/calibration review; do not claim it is empirically fitted.

| Career stage | Age years (uniform integer) | Stage shift S | Experience years (uniform inclusive integer) |
|---|---|---|---|
| rookie | 18–20 | -3 | 0..min(2,age-16) |
| established | 21–26 | 0 | 2..min(10,age-16) |
| veteran | 27–32 | +1 | 5..min(14,age-16) |

Age/stage are synthetic population conditions, not a real-person decline law. For generated profiles, choose birth_year=as_of.year-age and a fixed fictional January 1 date; age is then exact at the as_of date. Do not apply this convenience to real profiles. experience_years is prospective gameplay career context, not a forged public history. Do not invent team results/contracts to justify it. A later career system must reconcile explicit history before using years as a mechanic.

Current ability base A=B+S. Rookies can still be exceptional; age alone never guarantees high ability or future growth. Development projection remains null, so S affects initial distribution only. There is no annual +1 or predetermined retirement/decline effect. Any future potential model must be conditional on current ability, finite headroom, opportunity and uncertainty for both origins; population entry/exit and progression budgets must be reviewed together.

## Archetypes, shared factors and trade-offs

For top/mid/bot use active order (laning, combat, vision, consistency). For jungle/support use (combat, vision, consistency); laning is not_applicable and excluded from budgets.

| Archetype | Laner offsets (L,C,V,K) | Jungle/support offsets (C,V,K) |
|---|---|---|
| striker | +6,+4,-6,-4 | +6,-2,-4 |
| controller | -4,-2,+8,-2 | -3,+6,-3 |
| steady | -2,-2,-2,+6 | -3,-3,+6 |

Archetype selection weights (striker/controller/steady): top/mid/bot 40/30/30; jungle30/40/30; support20/50/30. Sample by the same integer cumulative-interval method. Offsets sum to zero in each active vector, so specialization is not free extra ability. These are management-facing capability profiles, not a new tactical class system.

Independently draw only two shared residual factors x,y from {-2,-1,0,1,2}, integer weights {1,2,4,2,1} out of10. All attribute residuals then derive jointly:

~~~text
laners:         e = (2x, 2x, -2x+y, -2x-y)
jungle/support: e = (2x, -x+y, -x-y)
raw[a] = A + archetype_offset[a] + e[a]
budget = count(active attributes) * A
~~~

This creates positive lane/combat residual correlation and trade-offs against information/stability, with a second factor trading Vision and Consistency. Shared B also creates positive overall capability correlation across the population. Do not demand a negative raw population correlation where the design only specifies a conditional residual correlation. More than one role/archetype can fit a strategy; no all-rounder receives the offsets of multiple archetypes.

Equal rating vectors are allowed; uniqueness applies to identities, not every capability combination. Career stages, names, role profiles and residual factors supply variation. Population review must distinguish legitimate ties from accidental clones caused by reusing a slot/purpose key; do not reroll until every player has unique ratings.

Clamp raw values to [1,99]. If sum differs from budget, cycle active attribute IDs in ordinal order, adding one point to each non-capped value when sum<budget, or subtracting one from each value above1 when sum>budget, until equal. The feasible budget bounds are checked first; failure rejects configuration. This finite integer projection preserves budget without sampling until a desired result appears. It may soften an extreme profile; record before/after. No per-player rescale toward100.

The four-band cap controls mean ability: non-exceptional mean≤75; exceptional mean≤87 under these parameters. A specialist may exceed80 in one attribute without being an elite all-rounder. Define audit elite as active-attribute mean≥80; its proportion can be lower than, but never exceed, the exceptional quota. Bounds/rare quotas do not by themselves prove absence of a dominant strategy; that requires simulation and economy tests.

## Hand-worked NPC example

Invented mid player, age22, established, regular band B=55, S=0, striker, x=1,y=-1:

~~~text
offsets  = ( 6, 4,-6,-4)
residual = ( 2, 2,-3,-1)
ratings  = (63,61,46,50)
sum      = 220 = 4*55; no projection needed
~~~

This demonstrates arithmetic for [npc_player.example.json](examples/npc_player.example.json). It is **not** asserted to be the output of seed42 or a completed generator. Seed42 in the example records a hypothetical run input; trace_kind explicitly states hand_worked_not_seed_output. Actual seed/config golden definitions must be produced and reviewed during the authorized implementation task, using rng-v1.

generation_evidence exact required fields: generator_version/rng_version/configuration_id/cohort_id/region/role/career_stage/ability_band/archetype (strings with values from this spec); world_seed (uint64 decimal string to avoid JSON consumer precision loss); slot (nonnegative integer); trace_kind (hand_worked_not_seed_output in examples, generated_and_verified only after implementation); age_years/experience_years/base_ability/stage_shift (integers); factors (object with integer x,y); offsets/residuals/pre_projection/final_ratings (maps of active attribute IDs to integers); projection_changed (boolean). Production trace must additionally bind the configuration hash, name-pool hash and generator build/arithmetic version through a separately versioned artifact. Do not fabricate their hashes in an example.

## Identity, lifecycle and inflation controls

- Sort name pools; sample name components with separate purposes. Display-name collisions are allowed; IDs never collide. A UI may disambiguate by handle suffix derived from immutable slot. Changing display name does not change identity.
- Definition ID uniqueness and single campaign instantiation are admission checks. Transfers preserve both IDs and effective capability. Recruitment only moves ownership/creates employment; it does not regenerate ability, readiness, potential or scouting history.
- Initial cohorts and each approved later entry cohort have a fixed rarity budget. Unlimited daily full-population rolls would accumulate elite players even at2%; therefore recurring spawning is **not authorized by this v0.1 generator proposal**. A later lifecycle must constrain inflow against vacancies/exits and test the active population over decades.
- Freeze reference distributions; do not recenter every successful campaign's ratings upward. Do not raise talent bands in response to the user's wins or wealth without explicit reviewed world rules.
- Salaries, contracts, readiness and company financial consequences are owned by their existing systems. Generator cannot give a cheap elite a hidden salary exemption. Revisiting the current Execution-based salary curve is a separate integration dependency.
- Champion specialization, adaptability, personality, injuries and growth/decline are evaluated as future dimensions but excluded from generation outputs used by simulation here. No rich history subsystem or universal sport adapter is introduced.

## Real/NPC distribution and knowledge

**ASSUMPTION:** initial public data would overrepresent selected professional competitors; a recruitable NPC pool may include replacement-level players. Therefore unconditional histograms need not match. **PROPOSAL:** compare conditional role/tier/career cohorts on the same approved capability scale; differences require a documented population-selection rationale. Shrinkage compresses measured ratings while generated truth is authored: calibrate measurement error and initial latent distribution jointly before declaring shared-scale fairness. These starter tables are not evidence of that calibration, and no lower-uncertainty NPC bonus is allowed.

True internal capability is an input to simulation. Observed performance is an outcome generated by shared rules. Scouting estimate is an observer-specific belief based on allowed evidence. Scouting confidence measures that observer's knowledge, not whether the player was generated. Potential, if eventually authored, remains hidden and is not guaranteed realized ability. UI and AI staff receive the same permitted projection; they must never receive generation_evidence, latent x/y, exact unknown-market capability, or future growth via serialized definition objects. Save/debug artifacts need not be actor-visible artifacts.

No scouting algorithm or growth mechanic is implemented in this task. Planned determinism, distribution, ownership and information-leak tests are in [validation](VALIDATION_CALIBRATION_V1.md).
