# Prototype Migration / Disposition Plan

Status: PROPOSED. Date: 2026-10-04. Owner: Technical Lead / QA / design.

**DECISION:** DEC-021 approves the bounded management thesis, not Python architecture, calibration or production balance. No assets are moved, deleted or ported in Phase 4.

## Asset disposition

All dispositions are **PROPOSAL**. Multiple rows may reference an asset because its behavior and implementation have different destinations.

| Class | Existing assets | Production treatment |
| --- | --- | --- |
| Retain as behavioral reference | [BUILD_SPEC](../../prototypes/BUILD_SPEC.md), [Director handoff](../../prototypes/reports/PROJECT_DIRECTOR_HANDOFF.md), specialist integration and Phase 2 rationale | Preserve ownership, causality and bounded-scope lessons with provenance and limitations |
| Retain as test oracle | [tests](../../prototypes/tests/test_simulation.py), [verification tests](../../prototypes/tests/test_verification.py), [verification.json](../../prototypes/reports/verification/verification.json), representative A–M traces and [TRACE_REVIEW](../../prototypes/reports/TRACE_REVIEW.json) | Pin reference commit/calibration; compare behavioral properties and trace relationships. Exact Python output is an oracle only for the unchanged prototype rules |
| Conceptually port | `src/information.py`, `competition.py`, `coach.py`, `state.py`, `simulation.py`, `economy.py`, `world.py` under `prototypes/` | Observation-only decisions, pure resolver, common executor, owner-specific effects, cursor/receipts and keyed uncertainty become new contracts and tests |
| Rewrite | Python runtime/data classes, CLI, serialization, scheduling implementation, policy adapters and verification harness | Implement reviewed C# modules and DTOs later; no line-by-line/module-for-module translation; rewrite UI and production persistence from specifications |
| Archive as noncanonical calibration/reference | [calibration.json](../../prototypes/data/calibration.json), [calibration log](../../prototypes/data/CALIBRATION_LOG.md), [scenario fixtures](../../prototypes/scenarios/scenarios.json), fixed policies and exact seed reports | Keep in current location as a historical reference. Retire from active build only after replacement parity evidence; physical archive movement needs a later task |

## Prototype-to-production behavioral parity plan

**PROPOSAL:** For every contract, create an engine-independent C# fixture and an explicit expected relationship. Use a small adapter only to extract reference evidence; do not import Python runtime into the product. Numeric tolerance must be named and justified for deliberate numeric/rule changes; never use tolerance to hide an ownership violation.

| ID / behavioral contract | Phase 3 evidence | Future production acceptance |
| --- | --- | --- |
| PB-01 Deterministic replay | AC-13; pause/resume/replay tests | Same initial identity/commands yields identical per-boundary hashes; save at checkpoint then continue matches uninterrupted; extra logging/queries leave draws unchanged |
| PB-02 Company/World ownership | Handoff ownership table; results referenced once | Recruitment/release transfers one current person owner atomically; one result owner and one financial-item balance; invalid transaction leaves both roots unchanged |
| PB-03 Competition cannot directly mutate finance | AC-04; `test_competition_is_pure_and_emits_no_cash` | Pure resolution leaves Company finance unchanged; downstream authorized Finance consumer applies any defined reward exactly once |
| PB-04 Information cannot buff true strength | AC-05; information/observation regressions | Paired quality inputs change estimates/error and possibly choices, not true player/opponent capability or event-keyed match draw; repeated observations do not reroll |
| PB-05 Commercial Value cannot print Cash | AC-03/04; unsigned-offer regression | Alter derived value or create offer: Cash unchanged. Accept terms: schedule explicit receivable. Settlement only at legal boundary; duplicate outcome cannot double-pay |
| PB-06 Manual/delegated common simulation | AC-06; E/F pairs | Identical selected plans with identical uncertainty yield exact competitive/economic facts; compare full hashes separately because authority configuration differs. Impossible envelope escalates without illegal auto-action |
| PB-07 Obligations constrain future choices | AC-08; financing beyond-horizon test | Financing creates principal/cost commitments; affordability/forecast includes outstanding future items; no horizon cutoff deletes debt and late payment keeps missed date |
| PB-08 Recovery requires sacrifice | AC-09; K/L and no-intervention counterfactual | Reach a recoverable distress state; intervention trades capability/opportunity/terms for viability while preserving legacy liabilities; no free reset or unapproved terminal threshold |
| PB-09 Rival adaptation changes decision value | AC-07/11; G and adaptation ablation | Paired fixed-seed contexts with/without supported rival adaptation change relative plan value in at least one designed case; scarce opportunity has only one claimant |
| PB-10 Causal outcomes remain explainable | AC-12; hash-bound A–M trace review | Follow decision → result → owning consumer → delayed effect → agreement/payment IDs; reconcile every material Cash delta; player explanation respects information boundary |

Supplementary regression contracts retain one-time overload conversion, eligibility, stale/invalid command atomicity, delayed-effect idempotency and finite opportunities. These tests protect approved semantics, not exact rival count, six-person roster, match count or 84-day schedule in production.

## Comparison method and exit

**PROPOSAL:** Freeze the reference revision, fixture/rules/calibration and selected trace hashes. Extract given/when/then fixtures with known ownership, commands and causal IDs. Implement production fixtures under separately approved production rules. Compare invariant outcomes first; use Python numerical goldens only for an intentionally unchanged formula with identical inputs. Publish differences as implementation defect, intended numeric representation change, approved design change, or unsupported prototype assumption. QA owns evidence; domain designers own semantic interpretation; Director approves changes to accepted behavior. No new acceptance is inferred from a passing test.

Do not require C# hashes to equal Python hashes: DTO layout, rounding and RNG protocol are proposed to change. Require repeatability within the declared production identity and parity of the listed contracts. Frozen prototype regression runs are useful reference checks, not production CI dependencies. Retirement from active reference use requires PB-01–PB-10 passing, explained differences and Director review of the retained archive.

## Limits carried forward

**FACT:** Phase 3 did not prove long-horizon solvency/balance, broad talent bidding, all information domains, human override skill, all recovery paths, full rival finances, UI performance or disk saves. K/L strongly exercise one restructuring route; end-date viability can coexist with negative future committed cash. Strong-start M remained highly successful despite active diminishing returns. The true-state oracle was diagnostic only. Production tests must not claim these limitations resolved by architectural separation.

**OPEN QUESTION:** Production balance and content choices are later design work. This plan creates no additional staff roles, industries, match mechanics or recovery features.
