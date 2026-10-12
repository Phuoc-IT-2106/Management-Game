# Validation and Calibration v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

Dependencies: [authority/registers](../../../10-vertical-slice/player-data/README.md), [contract](PLAYER_DATA_CONTRACT_V1.md), [catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md), [formulas](PLAYER_RATING_FORMULAS_V1.md), [NPC](NPC_GENERATION_V1.md), [current slice verification](../../../10-vertical-slice/VERIFICATION.md), [persistence foundation](../../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md).

## Evidence labels

**FACT:** only the document/example checks explicitly listed at the end were executed for this task. Everything in the test matrices below is **PROPOSAL — PLANNED, NOT EXECUTED**. Passing arithmetic or JSON parsing does not establish empirical rating accuracy, schema-validator implementation, balanced NPCs, cross-platform determinism or simulation compatibility.

## Planned schema and identity tests

| ID | Requirement / cases | Acceptance behavior |
|---|---|---|
| S01 | Required fields, nullability, exact numeric/string/bool types | Reject missing R, unexpected null, numeric strings, fractional integers and booleans passed as integers. |
| S02 | Duplicate/unknown JSON keys, unknown enums, malformed Unicode/IDs | Fail explicitly; no silent field dropping or case-insensitive aliases. |
| S03 | Bounds/date precision, finite numbers, input resource limits | Enforce contract bounds; invalid date/year pairing and overlong values fail. Preserve current loader's safety limits until a separately reviewed format changes them. |
| S04 | Definition, entity, provider and observation duplicates | Reject duplicate active biography/entity; retain verified aliases and source revisions without double counting. Shared legacy template ID is not duplicate biography evidence. |
| S05 | References, organization/contract ownership and role eligibility | Resolve every required foreign key; one active owner; transfer atomically preserves IDs and history. Source team cannot become campaign employer by refresh. |
| S06 | Unknown/deferred/not_applicable, empty/deferred objects | Null requires status/reason; deferred fields cannot supply power; unknown is not0 or50. Jungle/support Laning is not_applicable. |
| S07 | Provenance and named-use rights | Production public pack requires verified source/use record, dataset/model IDs and metric semantics. Example-only and prohibited data cannot enter production. |
| S08 | Provider game/series and metric mismatch | Reject series aggregate as game row; reject earned-gold/current-wallet substitutions or all-target damage mapped to champion damage. |
| S09 | Paired early snapshots, team totals, duplicates across providers | Reject inconsistent time/role/total; quarantine unresolved mappings; no double sample credit. |
| S10 | Real/NPC common gameplay contract | Same capability/runtime shape and range; origin only changes provenance/acquisition. NPC has no fabricated external identities/public statistics. |

## Planned formula tests

| ID | Requirement / cases | Expected property |
|---|---|---|
| F01 | Division by zero, duration0, team_kills0, gold0, deathless, empty entropy | W07 behavior; no infinity; zero opportunities ≠ poor performance. |
| F02 | Empty sample and all-missing features | Unknown with reasons and n=0; no measured neutral score. |
| F03 | n_series=1/2/3/4/12, effective count around thresholds | Mean minimum3; variance minimum12; unequal weights may fail despite high raw game count. |
| F04 | Missing GD/XPD/CS; missing vision score; feature masks; <50% admitted games | W03; no ward substitution; valid masks renormalize only at defined thresholds; coverage affects confidence. |
| F05 | All five role weight tables and exclusions | Weights sum1; Support DPM weight0; no Laning for jungle/support; same raw VPM can yield different contextual values. |
| F06 | Sparse/zero-sigma cells, disconnected tiers, new patch, pregame context leakage | Unknown or declared approved fallback; no implied global skill equality, future-data leakage or automatic old-patch mapping. |
| F07 | Outlier clipping and malformed negatives/totals | Invalid rows quarantined; valid extreme z clipped±3 with audit trace; raw data preserved. |
| F08 | Decimal half ties, culture, ordering, repeated evaluation | W07; same ordered inputs/artifact produce identical output bytes/hashes; no local timezone/culture dependence. |
| F09 | Series aggregation, recency and single shrinkage | Five games in one series do not create five independent series; W02/W06/W09; variance shrunk only once. |
| F10 | DPM/resource-ratio overlap, mean/variance separation | No duplicate bonus; unused diagnostics cannot alter resolution; high Consistency does not raise Combat mean. |
| F11 | Weighted variance denominator0, reference variance0, common masks | Unknown for invalid variance reference/sample; explicit single-era scope; valid formula yields W01 Consistency57. |
| F12 | Proxy interpretation and unidentifiable attributes | Labels remain estimates; no personality/mechanics/causal Adaptability inferred from box scores. |
| F13 | Parameter sensitivity: K10/20/40, half-life90/180/270, clip2/3/4, mapping scale10/15/20 | Publish rank/decision/uncertainty changes; weights are calibration parameters, not immutable truth. |
| F14 | Frozen campaign and model upgrade | W05 new artifact62→56 does not mutate existing campaign62; explicit version rejection or reviewed migration. |

## Planned generation tests

| ID | Requirement / cases | Acceptance target / evidence |
|---|---|---|
| G01 | Same seed/config/version, shuffled evaluation order, save/resume, clean-machine comparison | Identical definition bytes/IDs and output hash; rng-v1 golden vectors retained. Not established by this document. |
| G02 | Different seeds and purpose isolation | Population changes across seeds; name-purpose changes cannot alter capability draws; no probabilistic demand that every individual changes. |
| G03 | Quotas for N5/10/200/2000 and boundary failures | Counts sumN; role largest-remainder balance; exceptional≤floor(.02N); N200 gives80/90/26/4; invalid configured region sizes fail. |
| G04 | Shared-factor correlations conditional on B/stage/archetype | Lane/Combat residual covariance positive for laners; residual budget sum0; report raw and conditional matrices separately. |
| G05 | Age/stage/experience and deferred dimensions | Within tables, experience≤age-16, valid dates; no growth guarantee, invented career results or nonempty traits. |
| G06 | Attribute bounds, projection, rare talent and diversity | Budget preserved, finite termination, 1..99, nonexceptional mean≤75, elite mean≥80 proportion≤2%; no cloning due to reused key. |
| G07 | IDs, duplicate display names, team transfer, active ownership | Stable unique identities; display collision allowed; no reroll on hire/release; never simultaneous Company and World ownership. |
| G08 | Population integrity and bounded runtime cost | Test many seeds at N2000 plus N10000 boundary; measure time/memory; no rejected-sample loop without a bound; publish evidence before scaling. |
| G09 | Cohort changes and long-campaign inflow | Existing cohort immutable; recurring spawn blocked until lifecycle approved; multi-decade active elite stock/role coverage remains bounded in later integration tests. |
| G10 | Scouting/UI/staff knowledge | No definition/generation trace/hidden potential reaches actor projections; identical observation policies across origins. |

These are planned properties. The hand-worked NPC JSON is not a seeded golden vector, population simulation, or evidence of empirical correlations.

## Current compatibility and migration assessment

**FACT — baseline inspected:** HEAD14b9f48134264d8c04f3dfb268b93ee295b825f8 plus existing working changes noted in [README](../../../10-vertical-slice/player-data/README.md). The current implementation is authoritative for behavior; historical schema1 notes are not current loader behavior.

| Area | Current behavior | Future integration needed; no change made here |
|---|---|---|
| Person / definition identity | Eight fields; generated people use reusable player:generated template | Explicit biography/definition contract and capability state; migrate legacy template references using saved EntityId, never merge biographies. |
| Content validation | Strict development content schema2, slice-rules-v2, NONCANONICAL label and SHA-256 manifest | A separately reviewed player-pack format/admission path, typed references/limits and hash/version dependencies. Do not append these snake_case fields to fixture.json. |
| Team/roster | Company.People owns primary roster; Plan.Lineup references people; roles A–E enforce coverage | Director role mapping and role eligibility; no extra Team/roster truth. Source current team stays historical evidence. |
| Transfer/recruitment | World.Candidates owns market Person; employment/salary/release/deadline participate in atomic actions | Preserve ID, owner transfer and financial transaction; retain career/development without regenerating on transfer. Current Application lookup already follows stable released-player IDs. |
| Match resolution | Execution×Readiness contributes base; Adaptability contributes adaptation; Consistency changes spread | Approved capability consumer or versioned adapter, with ablation and explainability. No automatic Laning→Execution, vision→Information or new Adaptability replacement. |
| Economy | Pay.Player uses Execution squared /5, minor currency units/day; recruitment and renewal consume ability-linked terms | Calibrate compensation and roster budgets alongside capability spread. A new rating scale can distort affordability even if match odds appear stable. |
| Training/development | Work/preparation and daily readiness exist; no persistent player growth/potential/form system | Separate typed People progression proposal; avoid treating preparation Work as permanent skill. Growth, decline and role learning remain blocked. |
| Scouting/observations | Own PersonRow exposes Execution/readiness; candidate strength is a band; coach uses permitted read model | Typed observer estimates/ranges for new dimensions; prevent raw definition/provenance/latent state leaks. Do not expose all hidden values just because a JSON object exists. |
| Save/load | SaveEnvelope schema2 and explicit PersonDto; strict content/rules/RNG/canonical compatibility | New DTO/schema and old-save disposition, migration fixtures, replay/hash evidence. Existing saves remain untouched. |
| Content hash/rules | Campaign.Execution pins rules/content; save validates them | Bind player definitions, benchmark/model/generator versions into an immutable pack manifest whose digest is included in the campaign content identity. Preserve old pack availability or reject clearly. |
| Headless tests | Domain/Application detached from Godot; deterministic commands/resolution already tested in repository | Add semantic data/ownership/determinism tests only in authorized implementation. Pure offline fitting must not become a runtime network dependency. |

**PROPOSAL migration sequence, not implementation authorization:**

1. Resolve PD-D01–D05 for the actual scope; acquire rights and frozen calibration data only when authorized. Specify every required future resolver input and the treatment of legacy Adaptability.
2. Review a versioned immutable definition/rating pack and campaign capability contract. Under the current proposal, an applicable required capability that remains unknown blocks initialization; do not synthesize a real person's value. A prior-based fallback would require an explicitly labeled new method and Director review, not implicit50.
3. Define whether existing campaigns stay on their old rules/content or receive an opt-in migration. Recommended first implementation: new campaigns only, old campaigns retain the old pack/loader path where supported. Current single-version loader does not already provide that dual-version support.
4. If migration is approved, map IDs, roles, current ability, readiness, owners, references and legacy fields explicitly. Save a backup, validate before/after invariants, record version/hash/receipt and test rollback/failure. Never recompute progressed characters from fresh public stats.
5. Verify deterministic save/load/resume, actor-safe policy, recruitment/renewal finances and competition outcomes before expanding population or adding growth. Retain unresolved cross-machine/long-scale/migration obligations from the foundation gates.

## Calibration plan

**PROPOSAL:** first construct a coverage and selection report by provider, competition, role, season, patch, champion and team. Show missingness, duplicate rate and unsupported cells before calculating any headline rating. Professional public samples represent selected competitors, not the entire talent population. Small or poorly connected tiers cannot be made comparable by normalization alone.

Separate chronological training, model-selection validation and final held-out evaluation windows. Group games within series and prevent individual/team leakage where the experiment asks for unseen-person/team generalization. Train benchmark/variance/tier coefficients only on the appropriate training partitions. Do not tune K on the final holdout. Retain older patches for stress tests, including newly appearing patches that must return unknown until supported.

Compare transparent candidate models with simpler role-only means and the current authored gameplay baseline in separate experiments: statistical prediction of future contribution is not the same as good management gameplay. Report prediction error, rank stability under bootstrap by series, calibration of actual uncertainty intervals if eventually supplied, clipping rate, missingness bias, per-role/team/champion residuals and selection effects. The confidence score currently defined is an adequacy index, not a calibrated probability or interval coverage claim.

Review Combat against independently assessed tank/engage and damage-oriented examples; remove or redesign a feature if it systematically penalizes useful play. Audit low-death incentives. Evaluate whether Vision adds decision value conditional on Combat and preparation; if it only duplicates existing signals, defer it. Track feature/attribute correlations and ablate one attribute at a time. Neither association with wins nor agreement with fan rankings establishes causal individual skill.

For NPCs report per-role and per-stage mean/SD/p5/p50/p95, minima/maxima, covariance before/after budget projection, archetype frequencies, rare-talent counts, name collisions, ID collisions, projection frequency and active ownership integrity across many seeds. Compare real/NPC conditional cohorts after accounting for source selection and measurement shrinkage. Do not force a recruitable pool to match only elite professional players or excuse arbitrary strength differences as realism.

Candidate release gates for Director review:

- Zero schema/reference/ownership violations or unintended actor leaks; repeated evaluation identical under the declared profile.
- No unsupported role/cell silently rated; all outputs explainable through a trace and pinned source/model versions.
- No held-out role subgroup degradation hidden by an improved global average; publish error/uncertainty and decide tolerances before inspecting final holdout.
- NPC quotas/bounds/budgets hold for all tested seeds; distributions remain within approved cohort targets.
- Bounded gameplay scenarios show at least two roster archetypes preferred under distinct strategy/cost/readiness conditions; no universally winning build found in the tested grid. This is bounded evidence, never proof that an optimal meta cannot exist.
- Longer campaign tests monitor elite stock, wage/revenue ratios, roster concentration, win persistence and recruitment churn. Freeze ratings and constrain lifecycle budgets; if success compounds into dominance, adjust approved economy/world constraints rather than secretly weakening a player's identity.

Numeric error tolerances, production population targets and strategy-grid coverage are unresolved PD-D03/D04 decisions. Current placeholder parameters are uncalibrated; empirical release remains blocked.

## Planned integration tests

| ID | Test obligation |
|---|---|
| I01 | Snapshot/definition migration respects legacy fields, role eligibility, version/hash rules and unchanged older saves. |
| I02 | Hire/release/renew/transfer preserves IDs, unique owner, financial receipts and readiness/progression; source refresh has no live mutation effect. |
| I03 | Attribute ablation, cost-aware lineup choice, favorite/underdog variance, strategy fit and multi-seed economy reveal useful trade-offs; no unconditional all-rounder advantage. |
| I04 | UI/manual/delegated staff use permitted observations; hidden market capability/potential/trace unavailable. |
| I05 | Pure headless deterministic run equals save/resume and cross-machine golden results for approved versions; no API access at runtime. |
| I06 | Long campaign follows fixed data versions and approved progression/entry/exit budgets without elite accumulation or salary runaway. |

## Required design review — fifteen questions

1. **Why each attribute?** Laning separates early advantage, Combat mean contribution, Vision information contribution, Consistency variation. Deferred candidates lack a distinct measured consumer. The catalog gives reasons and twenty fields for each; none is necessary to keep today's slice working.
2. **Which player decisions?** Recruitment, cost-aware lineup/role choice, risk tolerance and bounded preparation investment. Full mechanical play, champion drafting and growth schedules are not added.
3. **Which systems change?** Future competitive inputs and explanatory scouting/transfer views, with separate financial valuation and progression approval. Competition still emits outcomes to company systems; attributes do not directly pay cash or train themselves.
4. **Can public data infer them?** It can estimate role-context early/combat/vision/variation proxies where covered. It cannot establish true mechanics, map causality, character traits, general adaptability or champion mastery from box scores alone.
5. **What if data are insufficient?** Unknown/deferred/not_applicable with reasons and coverage. Reject initialization if an approved required input remains unknown; a fictional substitute belongs to an explicitly fictional pack, not a fabricated real-player rating.
6. **Why is higher not always best?** Cost, readiness, role coverage and uncertainty matter. Higher Consistency reduces upset variance; a strong favorite and an underdog can rationally prefer different profiles. These trade-offs need integration evidence.
7. **Why select a weaker specialist?** Under a reviewed strategy consumer, higher Vision can compensate for lower Combat in an information-oriented plan; an affordable laner can preserve cash for staff/preparation. Current code does not yet implement that Vision interaction, so it is a testable proposal, not a claimed behavior.
8. **Do NPCs use identical rules?** Yes, proposed shared effective capabilities/ownership/resolution, with acquisition provenance excluded from strength calculations. Unknown data handling must not covertly favor generated players.
9. **Must distributions match?** Conditional comparable cohorts should align after calibration. Raw professional data and a broader recruitable pool have selection differences; the proposed NPC distribution is an unvalidated starting hypothesis, not empirical realism.
10. **Can inflation/snowball occur?** Yes: unconstrained growth/replenishment and existing wage/revenue feedback can accumulate elite teams. Fixed cohort quotas, frozen baselines, no automatic rerolls and future lifecycle budgets reduce risks; multi-decade economy tests remain necessary.
11. **What changes on growth/decline/role/team change?** Future typed runtime People state under current Company/World owner changes capability/role/condition. Transfers atomically change ownership and employment, retaining EntityId/DefinitionId. Immutable definition, source profile and historical observations do not mutate to simulate these events.
12. **How do formula versions change safely?** Produce a new dataset/benchmark/model/definition pack, pin it for new campaigns; existing campaigns retain their rules/content. Any migration needs explicit DTO/ID/role/value mappings and evidence, never a silent public refresh.
13. **Can every rating be reproduced/explained?** Proposed production trace includes row lineage, feature masks, benchmarks, weighted series, shrinkage, context adjustment, rounding and hashes. Examples demonstrate arithmetic only; the excerpt lacks the forty-row source dataset and is labeled accordingly.
14. **Are hidden constructs mislabeled as facts?** No capability here is an established public fact. Four released candidates are proxies, four remain deferred, and NPC values are authored latent inputs. Neither performance variance nor a guessed birthday implies personality.
15. **Is there an absolute optimal meta?** Not proven absent. Budgets, role-specific models, cost/readiness and conditional consumers aim to support multiple viable choices; ablations and strategy sweeps are required. If Vision adds no independent choice or an archetype dominates, defer/redesign it before BUILD expansion.

## Document review performed in this specification task

**FACT — executed 2026-10-08:** from the repository root, ran `python artifacts/player-data-spec-review/verify_docs.py`. The one-off review helper and its baseline/result reports are local ignored artifacts, not shipped gameplay tools or a production validator.

Result: **PASS**, 612 document/example assertions over six Markdown files and two JSON examples. Both JSON files parsed with duplicate-key/nonfinite-value rejection; their reviewed keys, ranges, null semantics, references and shared definition/runtime shape agreed. All 68 local Markdown links resolved. All eight candidate entries contain twenty numbered definition fields. Formula checks covered W01–W07/W09, example rating/confidence outputs, rounding, the NPC's hand-worked residual/budget and illustrative cohort counts; W08's gameplay overlap claim remains a planned integration test.

The review corrected an incorrect CoachPolicy file link and W04's rounding result (65.5 rounds to 66). The final example outputs agree with the documented six-decimal/ties-to-even profile. This is bounded example verification, not proof of a production formula implementation or empirical validity.

A baseline manifest covered 1129 existing files. Hash comparison found no changes to src/, content/ or the inspected authority/foundation source documents. This task added the player-data package and appended its entry to the existing vertical-slice index. Separate UI/presentation-document edits were observed concurrently and left untouched; they are recorded in the local report, not attributed to this specification or reset. A clean working tree is not claimed.

No full build, gameplay suite, live API/import, NPC generation run, empirical calibration, save migration or cross-machine test was executed. No gameplay/content/save/decision-log edit, commit or push was performed by this task. All S/F/G/I acceptance requirements above remain planned despite the narrower document checks passing.
