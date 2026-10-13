# Team, Coach & Rival Organization Data Foundation v0.1

Status: **PROPOSED — NOT APPROVED**; superseded in part by [rules v4](../RULES_V4_PLAYER_WORLD.md), which gives rivals real
squads instead of compressed capability (2026-10-13)  
2026-10-08 · PLAN → SPEC → VERIFY · Specification only

## Scope and deliverables

Bộ tiêu chuẩn riêng cho competitive Team, Coach person và Rival Organization; share identity/provenance, numerical conventions và generation/validation infrastructure. Phạm vi nhỏ: một Company, một discipline candidate, một primary team, limited compressed rivals và bounded explicit talent/staff market. Không production C#, world generator, live import, model/save migration hoặc accepted decision nào được thay đổi. Các JSON là documentation envelopes; `example_only=true` ngăn hiểu nhầm là ContentLoader/save input.

| File | Contract / purpose |
|---|---|
| [TEAM_DATA_STANDARD_V01.md](TEAM_DATA_STANDARD_V01.md) | Team identity, reference roster/coach,7formula categories, budget-constrained allocation |
| [COACH_DATA_STANDARD_V01.md](COACH_DATA_STANDARD_V01.md) | Four current attributes, observable versus unknown, correlated profiles, employment/delegation |
| [RIVAL_ORGANIZATION_DATA_STANDARD_V01.md](RIVAL_ORGANIZATION_DATA_STANDARD_V01.md) | Company-level compressed rivals,8rules, bounded resources/fidelity |
| [SHARED_GENERATION_AND_VALIDATION_V01.md](SHARED_GENERATION_AND_VALIDATION_V01.md) | Shared primitives, rng-v1 keys/order, graph invariants, validation plan |
| [real_team](examples/real_team.example.json) / [generated_team](examples/generated_team.example.json) | Synthetic public-path shell / hand-authored primary team |
| [real_coach](examples/real_coach.example.json) / [generated_coach](examples/generated_coach.example.json) | Unknown public capabilities / synthetic preparation coach |
| [real_organization](examples/real_organization.example.json) / [generated_rival](examples/generated_rival.example.json) | Unknown public finance/profile / synthetic compressed rival |
| [synthetic_world](examples/synthetic_world.example.json) | Supporting seventh example:1full Company,3compressed rivals,12explicit players,4explicit coaches; coherent graph and initial buffer |

“Real” filenames demonstrate import-path shape; names and records remain explicitly synthetic. No real person's ability, psychology or pay, or real company's finances, is fabricated as verified evidence. Public unknown records are valid staging examples but are not campaign-admissible. World reference catalog contains illustrative definition stubs; it is not a complete production pack.

## Repository baseline and source authority

**FACT — inspected working tree**, not an assumption that HEAD equals all local code:

| Item | Observed baseline |
|---|---|
| Branch | `stage5-sponsor-instruments` |
| HEAD / source revision | `14b9f48134264d8c04f3dfb268b93ee295b825f8` |
| Content / rules | Development fixture `slice-rules-v3`, Content schema3; noncanonical Role Arena A–E |
| Save | Schema3; schema2 rejected; exact content hash/rules/rng/canonical identity validation |
| RNG | Existing `rng-v1`; no second PRNG/generator implemented |
| Worktree | Already dirty across Domain/Application/Infrastructure, UI, content, tests, docs; Player Data package already present and untracked. Full pre-task status and989file hashes retained locally |
| Authority | Company/World plus technical execution identity; no Team/Coach/entity-data gameplay root |

Local audit snapshot: `artifacts/entity-data-v01-checks/initial.json`; source byte copies under `source-copies/`. These ignored review artifacts preserve the starting worktree separately from git HEAD. They are not deliverable runtime assets. Only this package and a link addition in the vertical-slice index are intentionally edited. Concurrent changes are identified against these hashes and preserved, not reset or swept into this work.

Read and cross-checked sources, with implemented code taking precedence over stale fixture descriptions:

| Evidence | Relevance / reconciliation |
|---|---|
| [Project State](../../00-project/PROJECT_STATE.md), [Decision Log](../../01-governance/DECISION_LOG.md) | Current phase and accepted authority; do not amend DEC-001–023 or promote later unimplemented waves |
| [Game Vision](../../00-project/GAME_VISION.md), [Product Boundaries](../../03-product-foundation/PRODUCT_BOUNDARIES.md) | Company-first scope and explicit exclusions; LoL remains candidate |
| [Technical Foundation](../../09-technical-foundation/README.md), [ownership](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md) | Company/World boundaries and single-person ownership |
| [Content](../../09-technical-foundation/DATA_DRIVEN_CONTENT.md), [persistence](../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md) | Versioned content, strict save identity, no silent DTO admission |
| [Competition specialist spec](../../07-specialist-specs/ESPORTS_COMPETITION_SPEC.md), [economy specialist spec](../../07-specialist-specs/SIMULATION_ECONOMY_SPEC.md) | Design intent; broader proposals not automatically current runtime facts |
| [Rules v2](../RULES_V2_CONTINUOUS_CAMPAIGN.md), [rules v3 wave1](../RULES_V3_INCENTIVES.md) | Recurrent markets, employment, incentives; current approved task scope distinct from later waves |
| [Player v0.1 archive](../../90-archive/10-vertical-slice/player-data/README_V01.md), [review](../../90-archive/10-vertical-slice/player-data/PLAYER_DATA_REVIEW_V1.md), [v0.2](../player-data/README.md) | Measurement/latent/knowledge separation, repaired statuses and generation contracts |
| [Model](../../../src/ManagementGame.Domain/Model.cs), [CampaignFactory](../../../src/ManagementGame.Domain/CampaignFactory.cs) | Actual Person/Coach/Company/Rival/World/Content shapes and initial ownership |
| [Competition](../../../src/ManagementGame.Domain/Competition.cs), [Simulation](../../../src/ManagementGame.Domain/Simulation.cs), [Finance](../../../src/ManagementGame.Domain/Finance.cs) | Exact consumed ratings, work/fatigue/payroll, causal receipts and rival seasonal reset |
| [Seasons / Generator / Pay](../../../src/ManagementGame.Domain/Seasons.cs), [Market](../../../src/ManagementGame.Domain/Market.cs) | Current generator, salary, recurrence, claim and hiring boundaries |
| [Observations / CoachPolicy](../../../src/ManagementGame.Application/Observations.cs) | Actor-safe knowledge; Coach.Analysis has no information-quality consumer |
| [ContentLoader](../../../src/ManagementGame.Infrastructure/ContentLoader.cs), [SnapshotDtos](../../../src/ManagementGame.Infrastructure/SnapshotDtos.cs), [SnapshotStore](../../../src/ManagementGame.Infrastructure/SnapshotStore.cs), [SnapshotValidation](../../../src/ManagementGame.Infrastructure/SnapshotValidation.cs) | Actual schema3 identities, ranges and relational guards |

Labels throughout: **FACT**=observed code/source; **DECISION**=inherited approved boundary; **ASSUMPTION**=explicit synthetic trial context; **PROPOSAL**=candidate contract/formula/range; **OPEN QUESTION**=unresolved choice. All new standards remain proposed even where they document current facts. Development values are not production balance/canon.

## Relationship to Player Data v0.2

No Player Data file is rewritten. Player v0.2 retains measurement/latent/actor-observation separation, conditional L/C candidates, diagnostic Vision/game consistency, fixed reference-mask/covariance requirements and unresolved G1 role-consumer gates. Team/Coach/Organization do not activate them indirectly. Current E/A/K in the world fixture are authored legacy scenario numbers; they are not outputs of Player v0.2 formulas or a calibrated LoL-to-A–E adapter. Stable PersonId survives transfers; no Team-owned copy of player attributes/contract.

Real and generated entities become comparable only after admission to the same calibrated definitions, units, context and runtime consumers. A common0–100 label alone does not establish this. Public profile unknown remains unknown until evidence or a separately identified scenario estimate is approved. NPC latent numbers may be known internally while the actor sees only existing observations.

## Contract versions, attributes and formulas

Proposed documentation schema versions: Team1 (`team-definition-example-v01`), Coach1 (`coach-definition-example-v01`), Organization1 (`organization-definition-example-v01`), relational fixture1 (`entity-world-example-v01`). Generation specification=`entity-foundation-trial-v01`, uses current rng-v1. None is Content schema4 or save schema4. A future admission/DTO migration requires an explicit compatibility decision and tests.

| Domain | Active/current or core reference attributes | Diagnostic / deferred / rejected |
|---|---|---|
| Team | Organization/competition refs; roster/staff refs; legal role coverage, readiness-weighted lineup and current Work-derived preparation | Form, standardized record, stability diagnostic only; composition fit/qualification deferred; chemistry bonus rejected; reputation projects Company |
| Coach | Preparation, Adaptability, Capacity consumed; Analysis stored/priced/market-band only; employer/assignment references | Experience descriptive; specialization label experimental; Development/Evaluation deferred; personality reliability rejected |
| Rival | Strength, Analysis, Posture, Budget points, Need; organization/team references | Adaptability stored without competitive consumer; finance/reputation/audience/staff hierarchy/full recruitment deferred |

| Formula IDs | Inventory / disposition |
|---|---|
| T01–T02 | Current readiness-weighted capability; structural role coverage/suitability |
| T03–T04 | Proposed forecast-residual form and direct opponent-band standardization; diagnostics, not power |
| T05–T07 | Available-lineup selection, current preparation math, actor information bounds |
| C01–C04 | Current load conversion, adaptation term, compensation/renewal and observation band |
| C05 / C06 | Proposed career-duration diagnostic / deferred causal-rating research boundary, no fake estimator |
| R01–R04 | Current opportunity budget gate, recruitment explicitly unsupported, strength drift and sponsor claim procedure |
| R05–R08 | Full-vs-compressed capacity, pressure response, current strategy/reset, proposed bounded strength initialization |

Each domain gives inputs, units, bounds, rounding, missing behavior, cadence, consumers, worked numbers and test cases. Common numerical contract covers exact integers, explicit nulls, no bool-as-number, checked money and Decimal diagnostic rounding. Finite coach mixture has27points; rival mixture9. Their probabilities and covariance are authored trial parameters, not empirically fitted coefficients. Team allocation compares two finite feasible objectives without any archetype bonus.

## Generation dependencies and integrity

Use hybrid two-pass order: validate/import+reserve IDs → organization endowment and Team/competition refs → anonymous compressed talent/staff reservations → single explicit admitted player/coach pool → bounded roster/coach allocation under budget → contracts/assignments and unassigned World market → registrations/schedule → atomic full validation. This avoids independent team generators creating duplicate talent or unaffordable contracts.

DefinitionId, runtime EntityId, OrganizationId, TeamId, PersonId/CoachId, membership, staff assignment, employment and external identities have distinct meanings. Owned roster/current coach require one matching employment and assignment; World market people have neither active employer nor team. Membership end is exclusive, current employment end inclusive. Historical source rows never create current owner. Missing role/coach/reference/budget aborts initialization; no partial world, infinite retry or hidden cash top-up. Detailed stable keys, sorted iteration and6804tuple upper bound are in shared spec.

## Director decisions and implementation readiness

| ID | Required disposition before dependent implementation | Smallest reviewable choice / open issue |
|---|---|---|
| ED-01 | Admit Team/Coach/Organization reference contracts and discipline policy | Keep Company/World owners; decide role IDs and whether any LoL adapter is approved; no automatic G1 opening |
| ED-02 | Approve a bounded synthetic numerical trial |1Company/3rivals, explicit12players/4coaches, resource envelopes, two team objectives, coach/rival tables; all tunable, not canon |
| ED-03 | Resolve inactive compatibility fields and policy limits | Freeze trial Coach.Analysis and Rival.Adaptability at60; retain current runtime until a concrete consumer/migration is approved; assess seasonal archetype convergence and ordinal claim bias |
| ED-04 | Authorize evidence/admission protocol for actual public entities or diagnostics | Rights+identity+context calibration; real coach capability/finance can remain unknown; forecasts and reference panels required before form/standardization |
| ED-05 | Separately approve any implementation/save/content change | Dedicated validator/adapter, stable identity/transfer tests, version handling; explicit rival recruitment or materialization needs additional scope approval |

These IDs are local proposals, not additions to Decision Log. None is automatically approved by creation of the spec. Open evidence requirements: real-world cross-origin scale calibration, workload/economy long-run balance, source rights, forecast identifiability, true talent-quality distribution and fair sponsor priority. They do not block delivery of bounded synthetic examples.

**Ready now:** schema and offline reference-validator implementation after review, exact arithmetic witnesses, fixed graph fixture and finite-distribution audit. **Not ready for production admission:** public ability inference, Player adapter, world generation, new runtime ownership/DTOs or broad balance claims. Recommended smallest next task: approve ED-01/02 then implement a standalone admission validator against these seven fixed examples and rejection cases; only afterward propose one initialization adapter with deterministic golden and transaction/save tests. Do not begin with a new full rival economy.

## Verification record

Completed verification on **2026-10-09 (Asia/Saigon)** with `python artifacts/entity-data-v01-checks/verify.py`; detailed output is retained in `artifacts/entity-data-v01-checks/report.json`. The source baseline and fixed synthetic evidence cutoff remain2026-10-08. This is an isolated documentation-fixture checker, not a complete production schema importer or generation implementation.

| Actual verification | Result / limit |
|---|---|
| Offline checks | **101 checks passed**:7strict-parsed JSON files, exact example shapes/types/ranges, unknown/provenance fences, graph joins and arithmetic witnesses |
| Negative mutations | **26 rejected**: missing/unknown keys, bool-as-version, invalid attributes/readiness/capacity, fake public profile, duplicate JSON/person/employer/staff IDs, broken refs/ownership/registration, role/roster errors, unaffordable buffer, phantom rival cash, invalid Need/budget and talent double-counting |
| Formula and resource witnesses | Lineup60; daily conversion43→21/12/8; prep15; form1/6; standardized win rate.5; coach pay1080/1150/1095; initial seven-day bill64778 and cash remainder195222; contextual coach/rival reversals; hypothetical stronger-team loss |
| Distribution arithmetic | All27coach and9rival support points, total mass1, ranges and exact mixture/conditional moments; no random population sampled |
| Finite feasibility audit |2016candidate tuples from fixed authored pool, below6804bound/10000cap; seven-day costs56546..65807; at cash60000 only596tuples feasible. This checks constraints, not a generated world or balance gate |
| Documents |62relative links resolve; UTF-8/status fences checked; `git diff --check -- docs/10-vertical-slice/README.md` passed |
| Preservation |37protected baseline files unchanged: Domain, Infrastructure, content, Decision Log and all17Player Data files. No baseline file missing |
| Concurrent changes | Three Application files changed outside this task: Contracts/Observations add recorded personnel-move projections, PortalView changes transfer-news presentation; inspected against saved source bytes. Other concurrent changes in handoff/genre-shell/UI-polish docs preserved. These do not change documented Domain formulas or ownership |

`initial.json` records the pre-task branch/HEAD/dirty status and989file hashes. This package and the vertical-slice index link are the only intentional repository edits from this task; ignored helpers/reports are local review artifacts. No commit/push performed for this specification task.

**NOT RUN:** seeded generator/replay, live import, runtime integration/build, empirical measurement calibration, multi-season balancing, source-rights admission, production ContentLoader/save ingestion. The shared validation matrix lists those future cases separately. Passing structural/synthetic checks does not imply equal real/generated outcomes, realistic finance, approved role mapping or production readiness.

## Change history

| Date | Version | Change |
|---|---|---|
| 2026-10-08 | v0.1 | Initial distinct Team/Coach/Rival contracts; current v3 reconciliation; bounded synthetic world, numerical witnesses and review gates; PROPOSED — NOT APPROVED |
| 2026-10-09 | v0.1 verification | Completed offline schema/graph/math/distribution review; clarified career-date cutoff and six-decimal duration; retained proposed status and original synthetic cutoff |
