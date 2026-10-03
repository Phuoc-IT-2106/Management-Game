# Phase 4 — Technical Foundation

Status: **READY FOR DIRECTOR SPEC REVIEW**
Date: 2026-10-04. Mode: PLAN / SPEC. Approval owner: Project Director.

**FACT:** This package specifies the proposed production foundation. Review readiness is not an accepted Phase 4 exit gate. ADR-TF-001 and ADR-TF-002 remain PROPOSED. No engine installation, C# project, gameplay implementation, repository reorganization or Phase 5 BUILD is included.

## Deliverable index

All twelve deliverables are English specifications. Technical choices remain proposals unless attributed to an accepted DEC or the current user instruction.

| # | Required deliverable | Document |
| --- | --- | --- |
| 1 | Engine / Framework Evaluation | [Evaluation and official sources](ENGINE_EVALUATION.md) |
| 2 | Engine / Framework ADR Proposal | [ADR-TF-001 — PROPOSED](ENGINE_FRAMEWORK_ADR.md) |
| 3 | Production Architecture | [Modular monolith / ADR-TF-002 — PROPOSED](PRODUCTION_ARCHITECTURE.md) |
| 4 | State Ownership & Simulation Boundary | [Authority and command/outcome contracts](STATE_OWNERSHIP_SIMULATION_BOUNDARY.md) |
| 5 | Persistence / Save-Load Foundation | [Snapshots and bounded compatibility](PERSISTENCE_SAVE_LOAD.md) |
| 6 | Data-Driven Content Foundation | [Category schemas and manifests](DATA_DRIVEN_CONTENT.md) |
| 7 | Testing / Determinism / Debugging Strategy | [Test layers and qualification gates](TESTING_DETERMINISM_DEBUGGING.md) |
| 8 | UI Technical Foundation | [Controls, dense data and validation](UI_TECHNICAL_FOUNDATION.md) |
| 9 | Repository / Build / Tooling Proposal | [Future boundaries, CI and commands](REPOSITORY_BUILD_TOOLING.md) |
| 10 | Prototype Migration / Disposition Plan | [Asset classification and behavioral parity](PROTOTYPE_MIGRATION_DISPOSITION.md) |
| 11 | Technical Risk Register | [Risks and concrete validation](TECHNICAL_RISK_REGISTER.md) |
| 12 | Phase 4 Open Questions | [Recommendations, owners and deadlines](PHASE4_OPEN_QUESTIONS.md) |

Review entry point: [Director handoff](PROJECT_DIRECTOR_HANDOFF.md).
Audit evidence: [Final consistency review](CONSISTENCY_REVIEW.md).

## Authority and provenance

**DECISION:** [DEC-001 through DEC-021](../01-governance/DECISION_LOG.md) are canonical; DEC-021 passes the Prototype Gate and authorizes Phase 4 PLAN / SPEC. [Project State v0.6](../00-project/PROJECT_STATE.md) retains the Director-controlled exit gate.

**DECISION — current user instruction:** Windows-first, UI-heavy + 2D, offline single-player, local saves, versioned bounded compatibility direction, PLAN / SPEC only. These constraints are not new decision-log entries.

**ASSUMPTION:** Small AI-assisted team, no engine-specific personnel constraint. Hardware and world scale are unknown.

**PROPOSAL:** Godot .NET + C# preferred, Unity fallback, Unreal compared. Exact runtime/export compatibility and project-specific UI capability remain untested.

Sources were inspected in the requested order: decision log; project state; this phase's previous index; [Technical Foundation handoff](../05-handoffs/TECHNICAL_FOUNDATION_HANDOFF.md); [prototype engineering handoff](../../prototypes/reports/PROJECT_DIRECTOR_HANDOFF.md); [verification](../../prototypes/reports/verification/VERIFICATION.md); [Phase 3 specifications](../08-minimal-prototype/README.md); [specialist specifications/integration](../07-specialist-specs/README.md); [Phase 2 package](../06-core-loop-simulation/README.md). Local baseline is `6ee4df3`, with the expected GitHub origin. Four existing untracked Phase 4 drafts were reviewed and extended; they supplied no approval authority.

Labels: **FACT** = inspected evidence with limits; **DECISION** = accepted canonical or explicit user constraint; **ASSUMPTION** = unverified planning input; **PROPOSAL** = recommended but unaccepted design; **OPEN QUESTION** = unresolved decision. Normative wording inside a proposed specification does not accept that specification.

## Source discrepancy register

**FACT:** The conflicts/limitations below are reported explicitly. No historical source or canonical decision was rewritten to hide them. The current user's instruction and DEC-021 agree on phase authority, so technical specification can continue. Q-07 requests Director acknowledgment before relying on ambiguous historical detail.

| ID | Source discrepancy | Current authority / package treatment |
| --- | --- | --- |
| C-01 | Phase 2 README says review pending/unsynchronized; prototype specification says Discovery and only proposed DEC-001 | Conflicts with DEC-001–021 and Project State v0.6. Phase 3 handoff already records historical synchronization/supersession. Preserve historical text and explicitly use current phase authority |
| C-02 | Prototype engineering handoff says Technical Foundation has not started; Phase 3 Director review authorizes prototype BUILD only | Historical statements at handoff time. DEC-021 later closes Phase 3 and opens Phase 4 |
| C-03 | Three long sources end abruptly: economy at Chain B, competition at outputs, prototype at Strategic Posture checkpoint | Phase 3 handoff section 2 acknowledges truncation and its then-approved supplemental request. That missing supplement is not reconstructed here. Current request, accepted decisions and delivered evidence ground technical contracts; absent gameplay detail stays open |
| C-04 | Early Company State includes stored aggregate/value/capacity entries; competition proposal lists exposure/progression ambiguously | DEC-010/012 and Phase 3 evidence require durable causes, one World result and derived aggregates/exposure. Explicit proposed fields resolve implementation ambiguity without a new Company Value pool or duplicate results |
| C-05 | Competition specialist proposes Situational Pivot/series adaptation; prototype review makes them optional and implementation omits them | DEC-019/020/021 do not mandate their production implementation. No pivot/series system is added; later scope needs design approval |

No unresolved contradiction between this task and accepted DEC-001–021 was identified. If Director considers a historical passage still binding beyond these decisions, record and resolve that conflict before implementation.

## Review and implementation boundary

**PROPOSAL:** Review the handoff, ADRs, ownership/save boundaries and open questions. Director records accept/revise/reject dispositions and decides whether to authorize bounded qualification. QG-01 export, QG-02 large-data UI, QG-03 reproducibility and QG-04 save safety are future evidence, all NOT RUN. Spec approval does not make them pass.

**DECISION:** Phase 5 Vertical Slice BUILD remains blocked by the existing Phase 4 exit gate. This assignment stops at **READY FOR DIRECTOR SPEC REVIEW**.
