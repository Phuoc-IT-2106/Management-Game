# Phase 4B — Director follow-up

Date: 2026-10-04 (Asia/Saigon). Baseline Director-review commit:
`ec248027336db56b201bb2b8a10d929b32c819b8`.

**READY FOR DIRECTOR FOLLOW-UP REVIEW**

## Objective and authority

Resolve as much QG-01 deployment/debugger uncertainty and QG-02 performance
uncertainty as the available environment permits. The user explicitly confirmed
that only the current laptop is available. This is bounded qualification code,
evidence and documentation. ADR-TF-002 remains accepted; Godot .NET remains the
conditional primary candidate and Unity remains fallback. Phase 4 Exit Gate is
**NOT PASSED**; Phase 5 is **NOT AUTHORIZED**. No canonical DEC entry was modified
and DEC-022 was not added.

## Environment

Windows 10 Pro 10.0.19044 x64, Dell Vostro 3400, Intel i5-1135G7 (4C/8T),
8,299,257,856 bytes physical RAM, Iris Xe driver 32.0.101.7080, one 1920x1080
60 Hz monitor. Actual Windows DPI: 144 / 150%. Godot 4.7.2 stable mono official
ed1daf0bf, Compatibility/OpenGL 3.3, SDK 10.0.401, MSBuild 18.9.11, runtime
10.0.12, net10.0. Release profiles confirm `debugBuild: false`; dummy audio.
No renderer, runtime, OS scaling or power-plan change was adopted.

Balanced power, discharging battery (46%) and about 1 GB free RAM were observed
during the investigation. These are environmental confounders, not isolated
causes. The laptop is **not Director-approved target/minimum hardware**. There is
no measured result on approved hardware. Evidence: `followup-environment*.log`,
`followup-load-context.log`, `followup-display.json` and each profile's metadata.

## QG-01 new evidence and final status

**INCONCLUSIVE.** Clean-machine and external-debugger sub-checks remain open.

- Windows Sandbox / VirtualBox / VMware executables were absent; hypervisor
  presence alone does not establish an available Windows guest. Multiple system
  runtimes and installed development tools disqualify this host as clean.
- Debug build: zero warnings/errors. Installed VS Community 2022 / Preview is
  17.14.36119.2; VS Code is 1.140.0 without the C# debugger extension. No supported
  external debugger session was exercised. Breakpoint, variable inspection,
  step and continue are NOT RUN; build success is not debugger evidence.
- The final follow-up Release export is separately inventoried: **199 files,
  195,918,925 bytes**, under ignored `qualification/artifacts/followup/`. Exact
  relative copied-file candidates and SHA256s are in
  `evidence/followup-reuse-manifest.json`. No files were copied to a clean machine,
  no network was disconnected, and no clean-machine launch/exit exists.
- The initial new-directory export invocation failed because its output directory
  did not exist; creating that directory resolved it. Failure log retained as
  `followup-qg02-profile-export.log`; this is not a packaging stop condition.

The artifact manifest SHA256 is
`9E8C4826C1B1D2004B43123554CFDCBDE3CE2711979E19C4C47BB6692F6CDDAF`.
The managed Host.dll SHA256 is
`90FF3AFEADB8B756E8A1DA9D6DF7C51CB3ADB8DAA157AA902268EC4A19E8EE71`.
The EXE is the common engine template; its hash alone cannot identify this build.
`qualification/CLEAN_MACHINE_CHECKS.md` supplies a pending execution procedure,
not a completed test. Local smoke and regression results are recorded below.

## Runtime/toolchain recommendation

Keep the exact tested Godot/template 4.7.2 mono + SDK 10.0.401 + runtime 10.0.12 +
net10.0 combination as the **qualification reproduction pin**. It has executed
fixture compatibility evidence; no competing runtime was tested in this follow-up.
The .NET 10 LTS horizon is a secondary consideration, not a substitute for evidence.

**Do not pin production yet.** Clean deployment/native prerequisites and an
external debugger remain unverified. VS 17 is not the supported net10.0 IDE target;
use an appropriately supported VS/VS Code C# setup for the pending session. Windows
10 Pro 19044 is not in the current .NET 10 supported Windows matrix (21H2 entries
are Enterprise/IoT). A clean supported Windows target must be selected and tested.
See [QG-01](QG01_GODOT_EXPORT.md#director-follow-up--2026-10-04) for executed versus
published-support distinctions and primary-source references.

## QG-02 profiling findings and optimizations

The untouched final Release artifact matched all 199 files in the historical
QG-03 manifest. A fresh sustained run passed 36 correctness assertions but measured
frame p95 **91.388 / 90.739 / 95.697 ms**, versus historical **19.048 / 19.600 /
19.444 ms**. A build overlapped part of that initial run; do not treat the temporal
change as an isolated code effect. Subsequent profiling, controls and final
sustained execution ran sequentially without concurrent qualification builds.

Separate Release workflow profiling found:

- Idle and scroll-only engine delta p95 16.667 ms; independent wall p95
  17.078–17.858 ms. Engine delta smooths some spikes; raw wall samples are retained.
- Original bind p95 14.646–38.308 ms across workflows. Repeated same-page rebuilds
  missed badly even with no GC collections, so GC alone is not the cause.
- Worker sorting at 100k had p95 91.545 ms; this is not UI-thread time. GPU render
  p95 was usually below 1.3 ms, rising to 4.074 ms on resize. Render CPU p95 was
  5.145–13.648 ms. Monitor update lag prevents summing these p95s into a frame.
- Allocation totals, current-thread bind allocation, Gen0/1/2 counts, process CPU,
  rendering counters/timers, response proxies and all frame samples are retained.
  Individual interop crossings and GC pause durations were not separately traced.

The single bounded optimization reuses up to 100 built-in TreeItems and updates
only changed text/tooltip cells. It retains at most one page's strings, removes
surplus rows, refreshes entity metadata and restores selection by ID. Godot still
owns layout, scrolling and focus; no custom table/virtualization framework exists.
Fifteen assertions were added for recycled-slot selection/identity and short,
empty and repopulated result sets. All original interactions and budgets remain.

Before/after diagnostic comparison (same-page binding; milliseconds except bytes):

| Rows | Original bind p95 | Same-binary rebuild control bind p95 | Reuse bind p95 | Control / reuse wall-frame p95 | Control / reuse allocated bytes per bind p95 |
| --- | --- | --- | --- | --- | --- |
| 1,000 | 23.129 | 48.138 | 0.680 | 52.454 / 17.014 | 59,232 / 20,448 |
| 10,000 | 18.745 | 44.792 | 0.752 | 48.090 / 17.075 | 59,240 / 20,456 |
| 100,000 | 18.092 | 50.730 | 2.053 | 45.671 / 17.109 | 59,240 / 20,456 |

These are short four-second diagnostic stages after warm-up, not long-run
acceptance or randomized repeated trials. The same-binary control shares the
new bookkeeping but discards TreeItems each bind. It establishes a useful reuse
path; it is not identical to the original algorithm and does not establish a
portable speedup percentage. Paging/sort/filter still miss strict frame limits.

Light instrumentation did not remove the residual misses: at 100k, full/light
wall p95 was 34.901 / 32.765 ms paging and 48.358 / 47.885 ms filtering. Disabling
VSync only for a diagnostic process lowered idle/scrolling wall p95 to 3.916 /
9.693 ms, but paging remained 38.802 ms and sorting response proxy rose from
143.968 to 312.384 ms. Uncapping is not a general remedy and is not the final
configuration. Search response includes the intentional 150 ms debounce; it is
not assessed against the 100 ms page-binding proxy budget.

## Final sustained result and regression audit

**QG-02: FAIL on the current machine against the unchanged 16.7 ms frame budget.**
**Director-approved target hardware: INCONCLUSIVE / unavailable.**

Final exported Release, default VSync, full original workload plus identity checks:

| Rows | Fresh pre-change frame p95 ms | Final frame p95 ms | Final query p95 ms | Final page-response proxy p95 ms | Managed retained ratio | Controls before/after |
| --- | --- | --- | --- | --- | --- | --- |
| 1,000 | 91.388 | 23.258 | 33.153 | 50.271 | 1.03257 | 11 / 11 |
| 10,000 | 90.739 | 22.222 | 54.963 | 50.118 | 1.00100 | 11 / 11 |
| 100,000 | 95.697 | 24.265 | 124.021 | 50.332 | 1.00016 | 11 / 11 |

The pre-change column has the environmental/build-overlap limitation described
above; use the controlled workflow arms for attribution. The final result is also
worse than the historical 19–19.6 ms run. Do not present temporal comparisons as a
universal speedup or erase either earlier result. Worst final engine-delta frames
were 80.645 / 94.098 / 61.727 ms; p95 does not imply absence of hitches.

The final run passed **51/51 correctness assertions** (original 36 plus 15 reuse
checks), with 100 navigation rebuilds per dataset and 300.189 seconds of sustained
interaction in total. Query budgets at 10k/100k, response proxy <=100 ms, retained
managed growth <10%, and bounded controls passed. No new 1k query budget is
invented. Final private bytes were 338,268,160 / 333,262,848 / 370,950,144; these
are observations, not an approved memory budget.

**Physical/DPI validation result:** the rendered Release layout run passed
**84 assertions across 12 size/application-scale combinations** on the observed
150% Windows environment. Captures at 1280x720 with 1x and 2x application scale
were inspected: toolbar controls remain reachable, the action wraps onto the
second line at 2x, the table retains usable height, and horizontal scrolling is
required for the right-hand columns at 2x. The screenshots do not prove actual
scrollbar manipulation, physical focus or tooltip usability.

**Local export smoke:** `followup-reuse-smoke.log` records the exact launch,
.NET 10.0.12 loaded from the artifact's own data directory,
`QG01_INTERACTION_PASS`, and exit 0. The inspected capture reads **Read-back
verified**. This proves the synthetic C# file round trip on this development
machine, not deployment to a clean machine or physical button input.

**Regression scope:** UI changes rebuild the shared Host assembly, so one fresh
rendered Release host replay was compared against retained QG-03 evidence:
initial hash, all 32 transition hashes and final hash match exactly. Final hash:
`a3561cf23f1ddf7abedc0052a789db2f27d017b8ea87c2fe4e209f92135000fa`.
No Domain/Application/Runner, persistence, target framework, package lock or shared
build/toolchain file changed. This targeted check preserves the relevant host
assumption behind QG-03; it is not a newly rerun full determinism matrix. QG-04
was not rerun because save infrastructure and its runtime/build assumptions are
unchanged. Historical QG-03 PASS and QG-04 PASS remain applicable within their
original evidence scope.

`followup-audit.json` records independently recomputed raw-sample percentiles,
51 correctness / 84 layout checks, 32 matched transitions, and exact verification
of all 199 final artifact files and source fingerprints. The audit confirms
`currentMachinePerformancePass: false`; a successful harness exit is not a gate
performance PASS. No earlier evidence or canonical decision was overwritten.

```powershell
./qualification/Followup.ps1 -Mode Smoke -Name reuse-smoke
./qualification/Followup.ps1 -Mode Regression -Name reuse-regression
python qualification/AuditFollowup.py
```

## Hardware and frame-budget interpretation

| Candidate explanation | Evidence-based disposition |
| --- | --- |
| A. Bounded implementation/profiling issue | Supported: eliminating repeated TreeItem reconstruction materially reduces work and allocation; remaining transitions still need budgeted evaluation. |
| B. Machine/renderer/environment issue | Contributes to uncertainty: old/new unchanged runs differ greatly and VSync changes cadence; power, memory, driver and compositor causes were not individually isolated. No renderer defect proved. |
| C. Provisional threshold unsuitable | A Director product/measurement decision, not established by a failed run. Distinguish continuous scrolling from discrete table transitions and smoothed delta from wall intervals. |
| D. Disproportionate Godot infrastructure | Not demonstrated. Native Tree plus a bounded page cache remains sufficient for this synthetic experiment. |

**Recommendation only:** retain a 60 Hz goal for active scrolling/navigation;
evaluate a separate transition envelope (for example, p95 <=33.3 ms frame work
during discrete rebind/resize and <=100 ms page acknowledgement), existing query
limits of 250 ms at 10k / 1 s at 100k, and explicit accounting for the 150 ms search
debounce. Idle screens may eventually use a lower redraw rate if wake-up
responsiveness is measured. This is a management UI with discrete transitions;
uniform 60 FPS during every transition is a product choice, while continuous
scrolling should remain smooth. The evidence supports separating workflows, **not
accepting the proposed numbers**: some observed wall intervals exceed 33.3 ms,
and no physical input-to-photon test exists. Director must approve hardware,
metric definition, hitch tolerance and any revised budget. **16.7 ms remains the
accepted provisional budget for this report.**

## Physical input / DPI evidence

Actual Windows scaling was observed at 150%; one monitor prevents mixed-DPI
movement. Automated synthetic keyboard/mouse routing, app scales 1/1.25/1.5/2,
and three window sizes are separate evidence. Real mouse, real keyboard, physical
focus/action usability, and Windows 100/125/200% remain **INCONCLUSIVE**. An OS
150% observation and captured layout do not constitute a complete human usability
pass. See `qualification/UI_MANUAL_CHECKS.md`.

## Godot viability and Director disposition

Godot .NET remains a viable **conditional** candidate: bounded native UI changes
show a tractable path and no architectural stop condition is demonstrated.
**Unity/engine ADR reconsideration is not warranted by this evidence.** No large
custom framework, persistent proven input correctness defect, fundamental export
failure or unsupported runtime workaround was required. No production gameplay
or Vertical Slice work was introduced.

Recommended Director disposition: accept the bounded remediation/evidence for
review while leaving QG-01 open and QG-02 failed under the current performance
budget. Keep ADR-TF-002 accepted and Godot conditional. Do not close Phase 4,
authorize Phase 5, or pin production tooling on this evidence.

Recommended next action: assign a clean supported Windows machine and supported
debugger session, approve the target PC and power conditions, then evaluate the
retained candidate on that hardware with physical input/OS DPI coverage. Decide
the workflow-specific budget explicitly before any new remediation scope.

## Limitations and evidence inventory

No clean guest, approved hardware, external debugger session, physical input,
full Windows DPI matrix or mixed monitors. One laptop, sequential short diagnostic
trials, changing machine conditions, instrument overhead and delayed monitors
limit attribution. No independent GPU capture or interop/GC pause trace. No audio,
network or native-prerequisite qualification. A faster machine must not be used
to hide a remaining implementation defect. All earlier failures and historical
QG-03/QG-04 reports remain intact.

`evidence/followup-*` contains fresh commands/exits, raw samples, environment,
artifact/source fingerprints and captures. `qualification/Followup.ps1` reproduces
the follow-up runs using separate evidence names. `AuditFollowup.py` independently
recomputes percentiles and checks the locally retained artifact/source hashes,
correctness/layout markers and exact QG-03 replay hashes. Generated tools/binaries
remain ignored. Source fingerprints are byte-level observations of this workspace;
line-ending conversion can change them without a logical source change.
