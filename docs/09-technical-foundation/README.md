# Phase 4 — Technical Foundation

Status: **CLOSED — PASS WITH DEFERRED VALIDATION OBLIGATIONS**
Date: 2026-10-04 (Asia/Saigon). Approval owner: Project Director.

**DECISION:** [DEC-022 — Production Technical Foundation Accepted](../01-governance/DECISION_LOG.md#dec-022--production-technical-foundation-accepted) closes Phase 4 and then authorizes **Phase 5 — Vertical Slice BUILD**. The [closure package](PHASE4_CLOSURE.md) records accepted conditions, historical QG results, future performance budgets and the current deferred-obligation register. This closure task changes documentation/governance only; no Vertical Slice implementation was started.

**Accepted foundation:** Godot .NET + C#, modular monolith, engine-independent Domain/Application, Godot presentation/runtime host, Company/World gameplay authority, outcome boundaries, deterministic/headless testing, bounded save compatibility and data-driven content. The technical execution envelope is not a third gameplay authority. Godot remains revisable through later evidence; Unity is fallback if disproportionate UI infrastructure cost or another major technical failure is demonstrated.

**Phase 5 development pin:** Godot 4.7.2 stable mono, matching 4.7.2 mono export templates, .NET SDK 10.0.401, runtime 10.0.12, target `net10.0`. This pin is for reproducible development, not a permanent release-support promise. Engine/runtime upgrades require targeted requalification.

## Deliverable index

The original specifications below retain their review-time text. DEC-022 and [PHASE4_CLOSURE.md](PHASE4_CLOSURE.md) supply their current foundation acceptance, exceptions and deferred gates; older PROPOSED, NOT RUN and Phase-5-blocked labels describe earlier stages. Acceptance of a foundation is not evidence that every specified future test has passed. Detailed production design work remains subject to normal review.

| # | Required deliverable | Document |
| --- | --- | --- |
| 1 | Engine / Framework Evaluation | [Evaluation and official sources](ENGINE_EVALUATION.md) |
| 2 | Engine / Framework ADR | [ADR-TF-001 — accepted subject to closure conditions](ENGINE_FRAMEWORK_ADR.md) |
| 3 | Production Architecture | [Modular monolith / ADR-TF-002 — accepted by DEC-022](PRODUCTION_ARCHITECTURE.md) |
| 4 | State Ownership & Simulation Boundary | [Authority and command/outcome contracts](STATE_OWNERSHIP_SIMULATION_BOUNDARY.md) |
| 5 | Persistence / Save-Load Foundation | [Snapshots and bounded compatibility](PERSISTENCE_SAVE_LOAD.md) |
| 6 | Data-Driven Content Foundation | [Category schemas and manifests](DATA_DRIVEN_CONTENT.md) |
| 7 | Testing / Determinism / Debugging Strategy | [Test layers and qualification gates](TESTING_DETERMINISM_DEBUGGING.md) |
| 8 | UI Technical Foundation | [Controls, dense data and validation](UI_TECHNICAL_FOUNDATION.md) |
| 9 | Repository / Build / Tooling Proposal | [Future boundaries, CI and commands](REPOSITORY_BUILD_TOOLING.md) |
| 10 | Prototype Migration / Disposition Plan | [Asset classification and behavioral parity](PROTOTYPE_MIGRATION_DISPOSITION.md) |
| 11 | Technical Risk Register | [Risks and concrete validation](TECHNICAL_RISK_REGISTER.md) |
| 12 | Phase 4 Open Questions | [Recommendations, owners and deadlines](PHASE4_OPEN_QUESTIONS.md) |

Current entry point: [Phase 4 closure and Phase 5 authorization](PHASE4_CLOSURE.md).
Historical specification review: [Director handoff](PROJECT_DIRECTOR_HANDOFF.md) and [consistency review](CONSISTENCY_REVIEW.md).

## Qualification evidence and closure exceptions

Reviewed baseline: `258be419a0731a71cc7141ab1907c79b444424b5`. Read the [qualification summary](qualification/QUALIFICATION_SUMMARY.md) and [follow-up summary](qualification/FOLLOWUP_SUMMARY.md) as preserved evidence. Their recommendations to keep Phase 4 open, defer production pinning and withhold Phase 5 authority are explicitly superseded by DEC-022 for Phase 5 development under the closure conditions.

| Historical gate | Retained result | Current disposition |
| --- | --- | --- |
| [QG-01](qualification/QG01_GODOT_EXPORT.md) | INCONCLUSIVE | Local export/C# file round trip works. Clean Windows execution becomes DEFERRED DISTRIBUTION GATE; debugger becomes DEFERRED DEVELOPER-TOOLING VALIDATION |
| [QG-02](qualification/QG02_DENSE_UI.md) | FAIL against original 16.7 ms budget | Correctness/query/memory/control bounds pass; native Tree and row reuse show a tractable path. Future engineering uses workflow-specific budgets; historical result stays FAIL |
| [QG-03](qualification/QG03_DETERMINISM.md) | PASS, bounded local fixture | Core/host per-transition hashes match; second-machine and production evidence remain future obligations |
| [QG-04](qualification/QG04_SAVE_DURABILITY.md) | PASS, tested local NTFS scenarios | Save architecture accepted; production migration policy and broader durability claims remain unqualified |

The [DV-01–DV-10 register](PHASE4_CLOSURE.md#risks-carried-forward-and-deferred-validation-gates) is authoritative for current owners and gate deadlines: clean Windows distribution; external debugger; physical input; DPI/mixed monitors; target/minimum hardware/performance; second-machine determinism; long campaigns; production save policy; UI regressions; upgrades/release support. These obligations remain OPEN / DEFERRED, not Phase 4 optimization work.

## Authority and provenance

**DECISION:** [DEC-001 through DEC-022](../01-governance/DECISION_LOG.md) are canonical. DEC-001–021 are unchanged. DEC-021 passes the Prototype Gate and opens Phase 4; DEC-022 accepts its exit under explicit conditions. [Project State v0.7](../00-project/PROJECT_STATE.md) opens Phase 5. Authority for this closure is the Director/user's explicit final closure instruction, not an inferred all-gates-pass result.

**DECISION:** Preserve Windows-first, UI-heavy + 2D, offline single-player and local saves with bounded compatibility. Earlier PLAN/SPEC-only restrictions are superseded by DEC-022's Phase 5 BUILD authorization; this particular closure task remains governance/documentation only.

**ASSUMPTION:** Small AI-assisted team, no engine-specific personnel constraint. Hardware and world scale are unknown.

**FACT:** Local qualification establishes bounded export/runtime/UI/core/save evidence, not clean-machine support, universal 60 FPS or solved production migrations. Keep the Phase 3 Python prototype as behavioral reference/test oracle only; do not port it wholesale.

Historical specification provenance: the original package at baseline `6ee4df3` reviewed the decision log, project state, previous index, [Technical Foundation handoff](../05-handoffs/TECHNICAL_FOUNDATION_HANDOFF.md), [prototype engineering handoff](../../prototypes/reports/PROJECT_DIRECTOR_HANDOFF.md), [verification](../../prototypes/reports/verification/VERIFICATION.md), [Phase 3 specifications](../08-minimal-prototype/README.md), [specialist specifications/integration](../07-specialist-specs/README.md) and [Phase 2 package](../06-core-loop-simulation/README.md). Its four inherited drafts supplied no approval authority. The present closure reviews the later qualification/follow-up evidence at the baseline identified above.

Labels: **FACT** = inspected evidence with limits; **DECISION** = accepted canonical or explicit user constraint; **ASSUMPTION** = unverified planning input; **PROPOSAL** = recommended but unaccepted design; **OPEN QUESTION** = unresolved decision. Normative wording inside a proposed specification does not accept that specification.

## Source discrepancy register

**FACT:** The inherited conflicts/limitations below remain visible. DEC-022 accepts the foundation using current decision authority; it does not reconstruct truncated sources or approve ambiguous gameplay detail. Such detail requires separate resolution before implementation relies on it.

| ID | Source discrepancy | Current authority / package treatment |
| --- | --- | --- |
| C-01 | Phase 2 README says review pending/unsynchronized; prototype specification says Discovery and only proposed DEC-001 | Conflicts with DEC-001–021 and Project State v0.6. Phase 3 handoff already records historical synchronization/supersession. Preserve historical text and explicitly use current phase authority |
| C-02 | Prototype engineering handoff says Technical Foundation has not started; Phase 3 Director review authorizes prototype BUILD only | Historical statements at handoff time. DEC-021 later closes Phase 3 and opens Phase 4 |
| C-03 | Three long sources end abruptly: economy at Chain B, competition at outputs, prototype at Strategic Posture checkpoint | Phase 3 handoff section 2 acknowledges truncation and its then-approved supplemental request. That missing supplement is not reconstructed here. Current request, accepted decisions and delivered evidence ground technical contracts; absent gameplay detail stays open |
| C-04 | Early Company State includes stored aggregate/value/capacity entries; competition proposal lists exposure/progression ambiguously | DEC-010/012 and Phase 3 evidence require durable causes, one World result and derived aggregates/exposure. Explicit proposed fields resolve implementation ambiguity without a new Company Value pool or duplicate results |
| C-05 | Competition specialist proposes Situational Pivot/series adaptation; prototype review makes them optional and implementation omits them | DEC-019/020/021 do not mandate their production implementation. No pivot/series system is added; later scope needs design approval |

No unresolved contradiction between closure and accepted DEC-001–021 was identified. DEC-022 satisfies DEC-021's Phase 4 exit dependency. If a historical passage is considered binding beyond current decisions, record and resolve the substantive conflict before relying on it.

## Review and implementation boundary

**DECISION:** Foundation acceptance and Phase 5 development authorization are distinct from completion of deferred validation. Clean Windows execution must pass before external tester distribution, public demo, release candidate or self-contained clean-Windows claims. Physical input/DPI, approved hardware, second-machine determinism, campaign scale, save policy and UI regression work remain assigned future gates.

**Historical evidence is not rewritten:** all qualification reports and raw evidence are retained, including INCONCLUSIVE/FAIL results. This closure performs bounded documentation verification only and starts no gameplay implementation or new optimization cycle.

**PHASE 4 CLOSED — PHASE 5 VERTICAL SLICE AUTHORIZED**
