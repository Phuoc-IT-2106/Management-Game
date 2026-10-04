# Phase 4 — Technical Foundation Closure

Date: 2026-10-04 (Asia/Saigon). Approval owner: Project Director.
Decision: [DEC-022 — Production Technical Foundation Accepted](../01-governance/DECISION_LOG.md#dec-022--production-technical-foundation-accepted).
Reviewed baseline: `258be419a0731a71cc7141ab1907c79b444424b5`.

## Outcome

**PASS WITH DEFERRED VALIDATION OBLIGATIONS.** Phase 4 is closed. The technical foundation is accepted for production work, and **Phase 5 — Vertical Slice BUILD is authorized after this closure**.

Authority is the Director/user's explicit final closure instruction, including permission to record DEC-022 as Accepted when this closure task is authorized. This package records that instruction; it does not infer approval from qualification results alone. DEC-021 required an accepted Phase 4 exit before production BUILD; DEC-022 now satisfies that requirement without modifying DEC-001 through DEC-021.

The architecture is coherent, no unresolved architectural blocker is demonstrated, and the implementation risks are bounded and understood. Remaining evidence belongs at the development, Vertical Slice, distribution or release gates below. Missing ideal hardware alone does not justify open-ended Phase 4 work. This is acceptance under explicit conditions, not a claim that every engineering uncertainty is solved.

## Accepted technical foundation

- Godot .NET + C#; Godot is the presentation/runtime host.
- Desktop modular monolith with engine-independent C# Domain and Application orchestration.
- Company/World gameplay authority, stored causes and derived aggregates, and explicit cross-system outcome boundaries.
- Deterministic/headless testing and causal debugging.
- Versioned snapshots with bounded save compatibility, validated recovery and explicit supported-version policy.
- Data-driven content with schema validation and content identity.
- Native Godot controls, bounded table rows and measured UI workflows.

The [ownership model](STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [save foundation](PERSISTENCE_SAVE_LOAD.md), [content foundation](DATA_DRIVEN_CONTENT.md), [testing strategy](TESTING_DETERMINISM_DEBUGGING.md), [UI foundation](UI_TECHNICAL_FOUNDATION.md), [repository/build boundaries](REPOSITORY_BUILD_TOOLING.md) and [prototype disposition](PROTOTYPE_MIGRATION_DISPOSITION.md) are accepted at foundation level, subject to this closure's explicit exceptions and deferred gates. Detailed production schemas, numeric contracts, content scope and implementations still need their normal design and verification work.

## Engine/runtime disposition

Accept ADR-TF-001 for Phase 5 with these conditions, superseding its original PROPOSED label and qualification-only status. Adopt Godot .NET + C# as the production technical foundation. Unity remains a fallback only if later evidence demonstrates disproportionate UI infrastructure cost or another major technical failure. Current evidence does not warrant Unity reconsideration. Godot adoption is revisable through a later documented Director decision; changing host still carries UI/adapter rewrite and runtime-retargeting costs.

Initial Phase 5 development pin, taken from the executed qualification reports:

| Component | Pin |
| --- | --- |
| Engine | Godot 4.7.2 stable mono (`4.7.2.stable.mono.official.ed1daf0bf`) |
| Export templates | Matching 4.7.2 stable mono |
| .NET SDK | 10.0.401 |
| .NET runtime | 10.0.12 |
| Target framework | `net10.0` |

**This pin is for reproducible Phase 5 development, not a permanent release-support promise.** It accepts the locally exercised combination; it does not approve the qualification laptop as supported target/minimum hardware or establish clean Windows support. The follow-up's recommendation to retain a qualification-only pin is superseded for Phase 5 development only. Its OS-support and native-prerequisite uncertainties remain at DV-01/DV-05.

Future engine/runtime upgrades require targeted requalification before adoption: build/export and local smoke, relevant host/headless determinism, save durability and UI checks for affected assumptions, plus clean-machine execution before distributing the changed artifact. Templates must match the engine. Release servicing/support must be reviewed at the release gate. No toolchain files or installations change in this documentation task.

## Architecture disposition

Accept ADR-TF-002, confirming the architecture acceptance already recorded in the qualification summary. Preserve the modular monolith, one campaign writer, engine-independent Domain, Application orchestration, Godot presentation, immutable observations and typed commands. Company State and World State remain the only top-level gameplay authorities. The technical execution envelope holds cursor/seed/receipt bookkeeping; it is **not a third gameplay authority**. Owning systems interpret outcomes and apply their own changes. Deterministic/headless testing, bounded save compatibility and data-driven content remain required.

Keep the Phase 3 Python prototype as a behavioral reference/test oracle only. Do not port it wholesale. Its counts, horizon, fictional discipline, coefficients and balance remain noncanonical under DEC-020/021; parity concerns approved behavior, not Python/C# bitwise identity.

## QG-01 — Historical INCONCLUSIVE; explicit closure exception

[QG-01 report](qualification/QG01_GODOT_EXPORT.md) and [follow-up](qualification/FOLLOWUP_SUMMARY.md) establish local Debug build, Release export/run, C# interaction and file round trip. The follow-up artifact loaded its bundled runtime and exited successfully. No fundamental export blocker was demonstrated.

No clean-machine execution or external debugger session exists. **Historical QG-01 remains INCONCLUSIVE.** The missing clean-machine/export prerequisite is reclassified as **DEFERRED DISTRIBUTION GATE** (DV-01), which must pass before external tester distribution, public demo, release candidate, or any claim that the build is self-contained on supported clean Windows systems. Development-host success cannot discharge it.

The external debugger workflow is **DEFERRED DEVELOPER-TOOLING VALIDATION** (DV-02). It is not a Phase 4 exit blocker unless development becomes impractical. If that happens during Phase 5, resolve the tooling impediment before dependent work proceeds; do not represent build success as breakpoint/step evidence.

## QG-02 — Historical FAIL; Phase 5 engineering policy

**Historical QG-02 remains FAIL against its original uniform 16.7 ms frame budget.** The [report](qualification/QG02_DENSE_UI.md) and [follow-up](qualification/FOLLOWUP_SUMMARY.md) retain all earlier observations. The final sustained run passed 51 correctness assertions, query budgets, response proxy, memory retention and bounded controls; 84 separate layout assertions passed. Frame p95 remained 23.258 / 22.222 / 24.265 ms at 1k/10k/100k rows on the unapproved laptop.

Native Godot Tree remains viable. Reusing a bounded page of rows materially reduced binding work and allocations without a large custom table framework. This demonstrates a tractable implementation path and no engine/architecture blocker. It does not demonstrate universal 60 FPS, physical usability, or a pass on approved hardware.

The Director adopts the following **workflow-specific Phase 5 engineering budgets**, replacing the uniform budget for future engineering work only:

| Workflow / metric | Phase 5 policy |
| --- | --- |
| Continuous scrolling / direct navigation | Target approximately 60 Hz / 16.7 ms where practical |
| Discrete table rebind / page change / sort/filter/layout | Provisional p95 frame-work target around 33.3 ms |
| Ordinary management-action acknowledgement, without deliberate debounce | <=100 ms p95 |
| Completed query at 10k records | <=250 ms p95 |
| Completed query at 100k records | <=1 s p95 |
| Search debounce | Record intentional delay separately from processing latency and also report total response; the fixture's 150 ms debounce is not processing time |

These budgets are not proof that existing QG-02 passed either its historical gate or every new workflow target. Some follow-up wall intervals exceed 33.3 ms and some workflow response proxies exceed 100 ms. Production performance remains unverified.

At DV-05, record the approved hardware/OS/power conditions, Release artifact, workload, warm-up and sample count. Define frame work separately from engine-smoothed delta, wall-frame intervals and input acknowledgement; retain wall samples and hitch observations so p95 cannot conceal stalls. The UI/Technical Lead proposes the measurement contract and hitch tolerance for Director acceptance. Report cold start separately. Do not add unrelated p95s. Physical usability remains DV-03/DV-04. Correct entity identity, stale-action rejection, bounded controls/subscriptions and memory retention remain required regardless of timing.

## QG-03 — PASS within bounded scope

[QG-03](qualification/QG03_DETERMINISM.md) passed the bounded deterministic core/host fixture: eight runner/host variants matched every one of 32 transition hashes; 43 contract assertions passed in each Debug/Release build. The Domain assembly references only System libraries. Follow-up host regression retained the same hashes. No engine-independence blocker was found.

This does not establish second-machine determinism, production gameplay coverage or long-campaign behavior. Those obligations remain DV-06/DV-07 and normal production integration testing.

## QG-04 — PASS within local NTFS scope

[QG-04](qualification/QG04_SAVE_DURABILITY.md) passed 166 assertions, including 27 actual process kills across nine checkpoints, corruption/backup recovery, sharing locks, ACL denial and simulated disk-full handling. No save-architecture blocker was found.

The evidence covers the bounded local Windows/NTFS fixture. It does not establish universal power-loss durability, other storage environments, a production save schema or production migrations. Bounded compatibility is an accepted foundation; production save migration policy is **not solved** (DV-08).

## Risks carried forward and deferred validation gates

All obligations below are **OPEN / DEFERRED**, not completed tests. Owners are accountable roles until named people are assigned. The Technical Lead maintains this register and includes its status in Phase 5 milestone/distribution reviews. Attach evidence and Director disposition before closing an obligation. A gate cannot be bypassed silently; material changes to these conditions require an explicit governance decision. These are future gate obligations, not reasons for indefinite Phase 4 optimization.

| ID | Risk / obligation | Accountable owner | Required future gate / evidence |
| --- | --- | --- | --- |
| DV-01 | Clean supported Windows distribution; OS/native prerequisites unknown | Build/QA lead; Director approves support matrix | **Before external tester distribution, public demo, release candidate, or self-contained clean-Windows claim**: complete [clean-machine checks](../../qualification/CLEAN_MACHINE_CHECKS.md) on an approved supported Windows target without Godot, SDK or separately installed managed runtime, offline. Record full artifact inventory, native prerequisites, startup/render, physical action, file round trip and exit. Resolve support-matrix discrepancy recorded in QG-01; the laptop is not the support baseline |
| DV-02 | External debugger workflow unverified | Technical Lead / developer-tooling owner | Phase 5 developer-tooling validation: supported debugger attaches, breaks, inspects variables, steps and continues. Resolve before dependent development if lack of tooling makes development impractical |
| DV-03 | Physical keyboard/mouse usability unverified | UI lead + QA | Before Vertical Slice UI quality gate: real focus, navigation, selection, activation, text editing, tooltips and scrolling checks using [manual procedure](../../qualification/UI_MANUAL_CHECKS.md); synthetic events are insufficient |
| DV-04 | Windows DPI and mixed-monitor behavior unverified | UI lead + QA | Before Vertical Slice UI quality gate: actual Windows 100/125/150/200% scaling and mixed-DPI monitor moves at agreed resolutions, preserving focus/selection and reachable actions; application scaling alone is insufficient |
| DV-05 | Target/minimum hardware and production performance undefined | Director + Technical Lead | During Vertical Slice, before its performance acceptance: approve hardware/OS/power/display baseline, metrics and hitch tolerance; evaluate representative production workflows against the Phase 5 budgets. Required before minimum-system or performance claims |
| DV-06 | Second-machine determinism absent | Simulation lead + QA | Before Vertical Slice determinism acceptance or cross-machine claims: replay identical versioned fixtures on a second qualified Windows machine and compare every transition hash; retain environments and first-divergence diagnostics |
| DV-07 | Long-campaign scale, memory/history growth and throughput unknown | Simulation lead + Technical Lead; Director approves scale | Vertical Slice scale review defines the bounded people/rival/content/history envelope; before campaign-scale acceptance or broader-production expansion, measure tick/query/save/load and retained memory/history at that horizon. Record retention policy without dropping pending effects or changing rules |
| DV-08 | Production save compatibility/migrations and storage claims unresolved | Persistence lead + Director | At production save integration, define schema/content version identity and recovery; before first distributed build, approve supported-version matrix and disclose experimental compatibility limits. Before promising any migration edge, test round trip, continuation, failure recovery and original-file preservation. Additional storage/power-loss claims require separate evidence |
| DV-09 | UI performance/correctness regression during real screen integration | UI lead + QA | At affected Phase 5 changes and each Vertical Slice performance review: preserve repeatable Release workloads and raw timing/correctness/memory/control evidence; investigate regressions within bounded scope. Disproportionate framework cost or major failure returns the engine ADR to Director |
| DV-10 | Engine/runtime upgrade or release-support drift | Build lead + Technical Lead; Director accepts release target | Before adopting an upgrade: targeted requalification described above. Before release candidate: review supported OS/runtime servicing and repeat applicable distribution checks; development pin is not a release-support guarantee |

The historical [risk register](TECHNICAL_RISK_REGISTER.md) and [open questions](PHASE4_OPEN_QUESTIONS.md) remain useful design inputs. This closure supersedes their Phase 4 blocking deadlines for engine choice, hardware and qualification. Q-01/Q-09/Q-10 are disposed through this decision; Q-04/Q-05/Q-06 move to DV-05/DV-07; Q-02/Q-03 remain save-policy/integration work under DV-08. Q-08 numeric/schema details require explicit contracts and vectors at first core implementation. Historical source discrepancies remain visible in the index; missing gameplay detail is not invented. Scope expansion still requires separate approval.

## Phase 5 authorization and evidence preservation

**Phase 5 — Vertical Slice BUILD may now begin**, with the objective: build the smallest production Vertical Slice proving the real management loop through production architecture. Follow the one-company / one-discipline / one-primary-team scope in the roadmap. Authorization to develop does not authorize external distribution before DV-01 or expand gameplay scope.

This closure changes governance and documentation only. No Vertical Slice implementation, gameplay system, production screen, engine switch, prototype port or new qualification cycle is started here.

**Historical evidence is not rewritten.** QG-01 remains INCONCLUSIVE; QG-02 remains FAIL; QG-03 and QG-04 retain their bounded PASS results. Qualification reports, raw evidence and prior DEC entries are preserved. The earlier summaries' advice to keep Phase 4 open and Phase 5 unauthorized describes their review-time disposition; DEC-022 explicitly supersedes that recommendation and earlier PLAN/SPEC-only labels without changing their evidence. Foundation specifications are accepted subject to this closure, not treated as proof that their future tests already ran.

**PHASE 4 CLOSED — PHASE 5 VERTICAL SLICE AUTHORIZED**
