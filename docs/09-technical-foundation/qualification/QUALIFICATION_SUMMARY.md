# Phase 4B — Technical qualification / foundation spike

Outcome: **READY FOR DIRECTOR QUALIFICATION REVIEW**

Date: 2026-10-04 (Asia/Saigon). Role: Technical Lead + Build/Verification Engineer.
This means the bounded investigation is ready for review, **not that all gates
passed**. Phase 4 exit remains **NOT PASSED**; Phase 5 remains **NOT AUTHORIZED**.

## Authority and scope

**DECISION — current Director/user instruction:** Phase 4 specification accepted
with conditions; ADR-TF-001 conditionally accepted for qualification; Godot .NET +
C# primary candidate, Unity fallback; ADR-TF-002 modular monolith accepted.
The prior specifications' PROPOSED/PLAN-only labels are historical. This report
records the current qualification authority without rewriting canonical decisions.
No DEC-022 was added and no phase was closed.

**FACT:** All requested governance and foundation source documents were inspected
before implementation. Code is isolated under `qualification/`; evidence lives
here. Synthetic tables, artificial counters and minimal snapshots are disposable
fixtures. The Python prototype, production gameplay and repository layout were
not ported or reorganized. No engine switch or architecture rewrite occurred.

## Environment and runtime recommendation

**FACT:** Windows x64 10.0.19044, local NTFS, Intel i5-1135G7 / Iris Xe, OpenGL
Compatibility renderer (driver 32.0.101.7080). Not an approved minimum hardware
configuration and not a clean machine. Godot **4.7.2.stable.mono.official.ed1daf0bf**,
matching **4.7.2.stable.mono** export templates. Qualification-local SDK
**10.0.401**, MSBuild **18.9.11**, runtime **10.0.12**, target **net10.0**.

**PROPOSAL:** Use the newest practical supported runtime compatible with the
selected Godot version. Executed local build, export and runtime evidence now
supports **.NET 10.0.12 / SDK 10.0.401 provisionally**. The SDK pin under
`qualification/global.json` makes this experiment reproducible; it does not lock
production runtime before QG-01 closes. Recheck supported Windows baseline and
servicing policy when selecting the release target.

Tools were downloaded into ignored workspace storage; machine-wide SDK/engine
installations were not changed. Initial sandbox restrictions affected certificate
access, MSBuild multi-project export and File.Replace. A successful ordinary
Windows execution lane is recorded separately from restricted failures.

## Gate outcomes and measured evidence

| Gate | Result | Evidence and practical limit |
| --- | --- | --- |
| [QG-01 — Windows export](QG01_GODOT_EXPORT.md) | **INCONCLUSIVE** | Local Debug interaction and Release headless/rendered export ran, wrote/read fixture data and exited 0. Export loaded its bundled runtime. Clean SDK/runtime-free offline machine and debugger attachment remain unverified |
| [QG-02 — Dense UI](QG02_DENSE_UI.md) | **FAIL on provisional frame budget** | Exported Release: query p95 24.7/28.8/83.1 ms; frame p95 19.0/19.6/19.4 ms versus 16.7 ms. Response proxies ~50 ms; 11 Controls; retained managed memory within 10%. 36 correctness and 84 layout assertions passed |
| [QG-03 — Determinism](QG03_DETERMINISM.md) | **PASS, bounded local fixture** | Eight fresh runner/host variants, all 32 transition hashes identical; 43 contract assertions per Debug/Release build. Domain assembly references only System libraries. No second-machine or production-gameplay claim |
| [QG-04 — Save durability](QG04_SAVE_DURABILITY.md) | **PASS, tested local NTFS scenarios** | 166 assertions, 27 actual process kills across nine checkpoints, sharing lock, actual ACL denial, corrupt/unsupported snapshots and simulated full disk; committed snapshots survive. No universal power-loss claim |

Raw commands, results, screenshots, hashes and failure logs are under `evidence/`.
QG-01 captured 195 export files / 195,768,757 bytes. Later QG-03 export adds the
shared core; its own manifest identifies that artifact. The local generated
exports and fault fixtures remain under ignored `qualification/artifacts/`.
Large engine/SDK/template archives are intentionally not committed.
The final exported UI benchmark has a separate `qg02-artifact-manifest.json`;
it identifies the measured UI artifact. The final QG-03 rerun/manifest includes
the subsequent explicit core ExportRelease optimization setting and identifies
the latest retained Windows artifact; the UI workload does not invoke that core.

## Failures and architectural implications

**FACT:** Godot can log a managed export failure while returning exit code zero.
The scripts now require managed evidence, scan relevant export errors and reject
missing executables. Initial missing solution and lingering console-wrapper
problems received bounded fixes. Multi-project export passed outside the sandbox.
Replay output directories are unique per run so old JSON cannot create a pass.

**FACT:** The dense UI correctness and bounded-control approach worked without a
large custom framework, but measured interaction performance did not meet the
provisional targets. One mouse-injection coordinate bug and narrow-layout issues
were corrected with native transforms/layout containers. The report preserves
failed attempts and distinguishes application scaling from actual Windows DPI.

**PROPOSAL:** Keep the modular monolith and engine-independent Domain/Application
boundary. The artificial deterministic fixture found no host divergence. Keep
Company/World gameplay authority separate from technical execution bookkeeping.
The NTFS snapshot strategy merits continued review with explicit storage limits.
These findings do not certify production systems, balance, content or save schemas.

## Engine implications and remaining risks

**FACT:** No tested stop condition demonstrated an unviable export path, unavoidable
custom table framework, engine-bound Domain, unfixable hash divergence or lost
committed snapshot. Therefore the investigation continued through all four gates.
Missing evidence and measured UI misses must not be converted into an engine pass.

**PROPOSAL:** Retain Godot as the conditional candidate pending QG-01 completion and
QG-02 remediation. Unity remains the first reconsideration option if a bounded
Release-profile investigation shows disproportionate custom infrastructure is
necessary. No Unity implementation is authorized by this report.

Remaining risks / **OPEN QUESTIONS**:

- Director must assign clean supported Windows hardware/VM and approve minimum
  hardware. Prove full artifact startup, physical interaction and local file
  round trip offline without Godot, SDK or separately installed managed runtime.
- Verify the actual external debugger breakpoint/step workflow. A Debug build and
  headless editor import are not debugger-attachment evidence.
- Diagnose UI frame/response misses on an approved PC and Release build; complete
  physical keyboard/mouse, actual Windows 100/125/150/200% scaling and mixed-DPI
  checks. Decide whether the provisional budgets remain appropriate.
- Repeat determinism on a second qualified Windows machine before broader claims.
  Full PB-01–PB-10 production rules, campaigns, content identities and history
  growth are outside this artificial fixture.
- Save support remains local tested NTFS. Physical full-disk, hardware power loss,
  controller caches, sync/network/removable storage and migration policies need
  their own evidence before being promised.
- This spike is replaceable qualification code. It is not a production save/UI/
  command framework or an implicit start of Vertical Slice.

## Recommended Director disposition and next action

**PROPOSAL:** Accept receipt of the qualification evidence; acknowledge the local
bounded QG-03/QG-04 passes; keep QG-01 inconclusive and QG-02 failed/open. Retain
ADR-TF-001 as conditional. Do **not** pass the Phase 4 exit gate or authorize
Phase 5 from these results.

Authorize only a bounded follow-up for clean-machine/debugger validation and
Release UI profiling on agreed hardware. Decide engine disposition after those
results. Do not expand table infrastructure indefinitely or silently change
engines. This assignment ends with the committed evidence and this handoff.

## Bounded gate commits

1. `6c0cb1e` — QG-01 export fixture and inconclusive report.
2. `a20671a` — QG-02 synthetic UI, measured budget misses and layout evidence.
3. `bee97ac` — QG-03 standalone/host determinism.
4. `4bdc636` — QG-04 save durability and interruption evidence.

The final handoff commit retains audit corrections, rerun evidence and this summary.
