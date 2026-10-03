# Director Handoff — Phase 4 Technical Foundation

Outcome: **READY FOR DIRECTOR SPEC REVIEW**
Date: 2026-10-04. Mode: PLAN / SPEC. Engine ADR: **PROPOSED**.

## Facts verified

**FACT:** DEC-001–DEC-021 and Project State v0.6 authorize Phase 4 and retain the Director exit gate. All twelve deliverables are in the [index](README.md), accompanied by this handoff and [consistency review](CONSISTENCY_REVIEW.md). Existing local drafts were reviewed and completed.

**FACT:** Phase 3 records A–M/AC-01–13 PASS, 32 passing regression tests, 1,664 repeated-state matches, 13 full-trace matches and 128 manual/delegated parity pairs. These are inspected historical Python results, not production verification. Bounded horizon, limited markets/recovery/information and unproven long-term balance remain explicit.

**FACT:** Official sources checked on 2026-10-04 document Godot .NET Windows export, Controls, headless CLI and MIT terms; Unity C# profiles, UI Toolkit, testing and commercial plans; Unreal C++/Blueprint, UMG, packaging, testing and royalties. The [source register](ENGINE_EVALUATION.md#official-source-register) records links and retrieval limits. None proves project performance or exact .NET 10 integration. Historical conflicts/truncation are reported as C-01–C-05 in [README](README.md#source-discrepancy-register); no missing gameplay text was invented.

## Proposals and recommended candidate

**PROPOSAL:** Prefer **Godot .NET + C#**, with Godot 4.7.2 as researched candidate and .NET 10 LTS conditional on qualification. Unity 6.3 LTS is the fallback for dense UI. Unreal is technically capable but judged a poorer small-team fit because of expected C++/build/asset integration burden. This is engineering judgment, not a measured ranking.

ADR-TF-001 remains PROPOSED. QG-01 proves export, QG-02 dense UI, QG-03 core/host reproducibility. Failure returns evidence and an amended ADR to Director, not an automatic switch. Unity migration rewrites UI/host and may retarget C# APIs; Unreal also requires language migration. No binary portability is promised.

## Architecture summary

**PROPOSAL:** Modular monolith: Presentation → Application → Domain responsibilities, validated immutable content and persistence adapters. Domain references neither engine UI nor filesystem. One campaign writer stages owner-specific changes, dispatches outcomes deterministically and commits state/cursor/receipts atomically. Staff and UI read observations and submit commands through the same executor. ADR-TF-002 remains PROPOSED.

## State ownership summary

**PROPOSAL:** Company owns player-company people, signed terms, Finance items, Reputation/Audience, organizational/information causes, competitive commitments and recovery. World owns external people/rivals, schedule/results, opportunities/markets, meta and calendar/execution identity. Contracts owns terms; Finance owns payment balances. Transfers preserve IDs and change current owner atomically. Derived values do not become authority; module histories, checkpoints and effects stay with their owners.

## Persistence strategy

**PROPOSAL:** Versioned JSON snapshot DTOs at committed boundaries, stable IDs, exact content manifests, saved cursor/seed/effects/receipts and rebuilt derived state. Validate separately before replacing a session. Safe writes plus backups require Windows fault injection. Experimental save breaks need notice; migrations cover declared supported versions only. No perpetual compatibility or migration implementation is promised.

Category-specific immutable definitions cover players, staff, organizations, competitions, sponsors, contract templates, traits and balance/config. Runtime facts and accepted terms remain campaign-owned. No universal schema, executable mods or new gameplay category is introduced.

## Testing strategy

**PROPOSAL:** Pure domain, integration, deterministic scenarios, save round trips, replay, UI/component and export tests share reproducible fixtures. Fixed phases, keyed RNG, integer numeric rules, stable iteration and canonical hashes exclude machine/runtime diagnostics. Debug tools inspect ownership, step/accelerate the same daily loop, trace causes, control seeds and validate saves/data. Developer truth remains separate from player observations.

## UI foundation

**PROPOSAL:** Godot Controls and C# presentation models compose reusable panels. Begin with bounded pagination; qualify recycling only if needed. No node per simulation record. QG-02 tests sort/filter/search, correct-ID actions, keyboard/mouse, scaling and latency with synthetic data. QG-01 runs the complete Windows artifact without development SDK or separately installed managed runtime. QG-01–QG-04 are all **NOT RUN**.

## Prototype disposition

**PROPOSAL:** Retain reports/tests/traces as behavioral references and bounded oracles; conceptually port verified boundaries; rewrite production modules, UI, persistence and tooling; preserve calibration/policies as noncanonical history. [PB-01–PB-10](PROTOTYPE_MIGRATION_DISPOSITION.md) protect replay, ownership, finance/information/commercial boundaries, delegation, obligations, costly recovery, rival pressure and explainability. Cross-language bitwise equality is not required. Nothing is moved or deleted now.

## Risks and open questions

**OPEN QUESTION:** Engine/runtime, supported saves, autosave/recovery, minimum Windows PC, world scale, retention, historical discrepancies, numeric protocols, UI budgets and post-review authority remain [Q-01–Q-10](PHASE4_OPEN_QUESTIONS.md), each with recommendation, owner, gate and delay cost. [R-01–R-15](TECHNICAL_RISK_REGISTER.md) each name concrete validation and failure response.

Largest risks: .NET export compatibility, custom table effort, unknown hardware/scale and save/determinism correctness. Hardware/scale uncertainty prevents performance guarantees, not specification review. No gameplay expansion resolves these questions.

## Decisions requiring Director approval

**PROPOSAL:** Record explicit dispositions for engine/runtime ADR-TF-001; architecture ADR-TF-002; ownership/command/outcome/time boundaries; save compatibility/recovery; content schemas/manifests; determinism/tests/debugging; UI strategy/budgets; repository/build layout; prototype parity/disposition; and open-question ownership/risk acceptance. Acknowledge source discrepancies. No approval or DEC entry is recorded by this handoff.

## Recommended next action

**PROPOSAL:** Director reviews and records accept/revise/reject dispositions. If satisfactory, separately authorize bounded qualification and assign owners. Review measured evidence before declaring the foundation ready for substantial Vertical Slice work; the canonical Phase 4 exit gate still controls Phase 5 BUILD.

**FACT:** This task stops at **READY FOR DIRECTOR SPEC REVIEW**. No engine installation, C# project creation or production implementation occurred.
