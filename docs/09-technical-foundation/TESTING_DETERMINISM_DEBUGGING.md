# Testing / Determinism / Debugging Strategy

Status: PROPOSED. Date: 2026-10-04. Owner: Technical Lead / QA.

**DECISION:** DEC-009/010/012/014/017/021 preserve daily progression, explicit authority, derived-state discipline, delegated parity, cross-system outcomes and reproducible validation.

**FACT:** [Phase 3 verification](../../prototypes/reports/verification/VERIFICATION.md) reports 32 regression tests, 1,664 repeated scenario pairs, 13 representative replays and 128 manual/delegated parity pairs passing. These are recorded Python results, not newly executed C# or engine tests. Only the documented finite fixtures/seeds/horizon were validated.

## Deterministic execution contract

**PROPOSAL:** Guarantee repeatability for identical initial Company/World snapshot, accepted commands, seed, rules/content/RNG/canonicalization versions and qualified runtime target. Initially qualify Windows x64 in both headless runner and engine host. No cross-engine, cross-version, cross-architecture or Python/C# bitwise equality is promised.

The fixed daily order and exact resume cursor are authoritative in [state ownership](STATE_OWNERSHIP_SIMULATION_BOUNDARY.md#daily-progression-and-transaction-boundaries). No physics, frame delta, wall clock, dictionary iteration order or asynchronous completion order selects gameplay results. One worker processes all mutations. Parallel queries read immutable snapshots; deterministic operation counts bound staff/rival planning.

**PROPOSAL — RNG protocol:** Use versioned, event/domain-keyed draws instead of a shared mutable RNG. Proposed `rng-v1` maps SHA-256 over an unambiguous length-prefixed UTF-8 encoding of protocol ID, root seed, domain ID, stable event ID, purpose and draw index to a 64-bit unsigned integer in a specified big-endian order. Seed is a saved unsigned 64-bit value; generated once at campaign creation, it is then a fixed input. Each domain/purpose has a registry entry. Integer ranges use rejection sampling with a deterministic retry index, avoiding modulo bias; probability resolution compares integers against an explicitly scaled threshold. Specify golden vectors before implementation; do not depend on `System.Random`, `GetHashCode` or engine RNG stability across versions.

Information draws use actor ID, subject ID and knowledge revision in their event key. Reopening a panel or evaluating an extra staff plan must not change a match draw. Alternative plans compare against the same keyed uncertainty where paired testing requires it. Event IDs come from semantic event identity or deterministic counters, never log count, GUID creation timing or thread order. If a future stateful stream is justified, persist its algorithm and counter and test its isolation explicitly.

**PROPOSAL — ordering:** Sort transactions by the phase's declared priority, due day, stable subject/event ID and sequence; document this tuple per phase. Use ordinal ASCII ID ordering, stable tie-breaks and deterministic aggregation. Financial receipts precede obligations as specified. UI locale collation may differ for display, but commands carry IDs and never use displayed row indices. Shuffling file/dictionary insertion order must leave gameplay unchanged.

**PROPOSAL — numbers:** Cash and financial amounts use checked signed 64-bit integer minor units; the production denomination remains a design question. Rates, probabilities and continuous gameplay quantities use explicit integer fixed-point scales (initial technical proposal: one million units per whole for rates). Domain schemas declare units/scales and allowed ranges. Use checked wide intermediate arithmetic and round once at the declared rule boundary, to nearest with ties to even, including negative values. Division by zero, overflow and invalid ranges reject the operation atomically; no platform-dependent wraparound or implicit clamping. Allocated payment totals must reconcile; residual minor units are assigned by stable item ID. Float/double may format charts but do not become authoritative state. A new curve requiring transcendentals needs deterministic approximation and versioned test vectors. These numeric choices need Q-08 approval and are not Python calibration ports.

## Hashing and replay

**PROPOSAL:** Canonical gameplay hash is SHA-256 of a version-tagged canonical projection: Company + World, cursor, future effects/checkpoints/receipts, ID counters, seed, rules/RNG identity and gameplay manifest digest. Write UTF-8 without BOM; fixed field names/order; no whitespace; integers in invariant base-10; explicit null policy; object/map keys sorted ordinally; sets sorted by stable ID; semantic sequences retain order. Store gameplay strings in normalized Unicode NFC at input boundaries. The canonicalization version is part of the contract.

Exclude filesystem paths, machine/user names, OS/runtime diagnostic labels, wall-clock timestamps, build machine data, engine instance IDs, UI layout/preferences, logs, profiling timings, caches and the hash field itself. Include any setting that changes decisions, such as authority mode; never exclude it merely to make a parity test pass. Component hashes for Company/World/modules/cursor locate the first divergent transaction. File integrity checksums are distinct from gameplay hashes.

A replay bundle contains the initial snapshot, seed, exact rules/content/RNG identity, command IDs/payloads/actors/checkpoint revisions, ordered accepted decision journal and expected transition hashes. In verification mode rerun staff policies and compare proposals to the journal; in journal playback use recorded accepted commands and do not also execute autonomous choices. Reject missing/extra/out-of-order commands and version mismatches. Save/load continuation records the loaded boundary, not an invented fresh starting campaign. Full-state hash comparison remains strict; manual/delegated parity compares explicitly listed gameplay fields while retaining differing authority configuration in full hashes.

## Test layers and acceptance

All entries below are **PROPOSAL — future tests**, not completed evidence. Owners are roles until assigned people exist.

| ID / layer | Required evidence / failure condition | Owner / cadence |
| --- | --- | --- |
| T-01 Pure domain units | Rule invariants, legal/illegal transitions, checked rounding, RNG golden vectors; resolver cannot mutate finance | Domain engineer / each change |
| T-02 Application integration | Multi-owner recruitment/contract/finance transaction commits wholly or rolls back; duplicate/stale commands; exact retry versus conflicting reuse | Application engineer / PR |
| T-03 Deterministic scenarios | Repeat seeded runs; shuffled input order, cultures, frame rates, logging on/off; equal hashes at every boundary | Simulation engineer / PR reduced set, nightly expanded |
| T-04 Save round trip | Save at every legal phase/checkpoint boundary; reload/continue equals uninterrupted run; all future obligations and receipts survive | Persistence engineer / PR |
| T-05 Replay | Complete journal matches; tampered seed/content/command rejected; earliest divergence reported; replay does not execute staff twice | QA / PR |
| T-06 UI/component | View-model tests without engine plus engine component focus, commands, stale views and selection-ID checks; no direct domain references | UI engineer / PR |
| T-07 Smoke/export | Qualified Windows export starts offline, renders, advances, saves, reloads and exits successfully on a clean machine | Build engineer / candidate build |
| T-08 Cross-system parity | PB-01–PB-10 from the parity plan, same manual/staff plans and draws; information leakage and commercial-cash boundaries | QA + domain owners / PR |
| T-09 Persistence failure/migration | Every declared supported edge, unsupported versions, malformed references, write failure and backup recovery; original remains intact | Persistence engineer / PR and candidate |
| T-10 Content validation | Category and cross-reference errors, duplicate IDs, unsupported effects, manifest mismatch, reordered inputs | Content/tooling owner / content PR |
| T-11 Scale/performance | QG-02 plus measured daily-step, save/load and history growth at agreed Q-05 scale; no skipped rules or checkpoints | Technical Lead / qualification and affected changes |
| T-12 Causal trace | Follow cause IDs from decision/result to contract/effect/payment; reconcile Cash and explain material outcomes without exposing hidden truth | QA/design / scenario changes |
| T-13 Dependency boundaries | Core compiles/tests without engine installed; assembly reference allowlist; no engine/UI/I/O or system RNG in domain | Technical Lead / PR |

Tests assert contracts rather than mirror methods. Keep deterministic defects separate from balance questions. Changing a test expectation needs the approved rule change and regenerated evidence, never a lowered threshold to hide a regression.

## Future qualification gates

**PROPOSAL:** All gates are **NOT RUN**. Director review and separate implementation authority precede experiments. Gate acceptance is separate from accepting an ADR. Failures return evidence and bounded remediation options to Director.

| Gate | Setup / method | Pass criterion and retained evidence |
| --- | --- | --- |
| QG-01 Godot .NET Windows export | Pin exact Godot .NET, export templates, .NET SDK/runtime and packages; verify external debugging; export Release x64; copy complete artifact to a clean supported Windows VM/PC without Godot, development SDK or separately installed .NET runtime; disconnect network | Start/render/input/advance/save/reload/exit without downloading dependencies. Record OS/hardware, versions, artifact hash, debugger/export logs and clean-machine result. Confirm any native redistributable packaging. An editor-only success fails this gate |
| QG-02 Large-data UI | Synthetic 1,000/10,000/100,000 row observation datasets, 12 mixed columns, long names, missing values and duplicate sort keys; page/window size bounded; repeated sort/filter/search, resizing, focus and command actions | Follow UI foundation's provisional budgets and exact correctness checks. Record raw timings, p95 frame/input/query times, memory and control counts. Synthetic counts are stress inputs, not required world size; gate cannot close without agreed Q-04 hardware |
| QG-03 Core/host determinism | Same fixture/seed/journal in headless and exported host, debug/release, differing locale/logging/frame rate; save at each cursor and resume; repeat across two qualified Windows machines | Identical per-transaction hashes, no RNG shifts or repeated effects; all PB contracts pass. Retain bundles and first-divergence diagnostics |
| QG-04 Save durability | Inject termination before/after temp write, flush, backup and replace; simulate full disk, denied permissions, locked files and corruption; run all supported migrations | At least one validated previous/new committed snapshot survives each tested interruption; recovery is explicit; no partial state loaded and no original destroyed. Document filesystem and limits; do not claim universal power-loss immunity |

## Debug tools

**PROPOSAL:** A read-only state inspector exposes root/module ownership, IDs and revisions in development builds. A stepper advances one transaction, phase or day and stops on checkpoints. Time acceleration runs the same daily loop with reduced rendering only; it never skips finance/rivals/effects. Seed control creates/replays named fixtures. A trace viewer follows decision/event/consumer IDs, before/after values, due times, numeric rule version and RNG key/draw index. It distinguishes developer truth from player-safe explanation.

Save validator reports envelope/version/checksum/reference failures without changing files. Data validator reports schema and semantic errors without launching the engine. Reproduction bundles include starting snapshot and exact manifest/journal; exclude local account paths. Debug state edits, if later added, are explicit recorded commands in a forked test campaign. Diagnostic logging must be removable without changing gameplay. Bounded log buffers must never discard pending gameplay effects. Retail UI never displays developer-only hidden truth.

**OPEN QUESTION:** Q-04/Q-05 set hardware and production scale; Q-06 sets history retention; Q-08 approves numerical/replay details. No engine benchmark, export, C# test or migration implementation is claimed complete.
