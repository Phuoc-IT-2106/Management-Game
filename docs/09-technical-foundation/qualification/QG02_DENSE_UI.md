# QG-02 — Dense management UI

Date: 2026-10-04 (Asia/Saigon). **Result: FAIL against provisional performance budgets on this machine.**

## Objective, environment and implementation

Bounded synthetic experiment using Godot 4.7.2 .NET, SDK 10.0.401 / runtime
10.0.12, Windows x64 10.0.19044, NTFS, i5-1135G7 / Intel Iris Xe. Same environment
as QG-01; not Director-approved minimum hardware. Compatibility/OpenGL renderer
and dummy audio. Final measurements use the exported **Release** app outside the
restricted sandbox (`debugBuild: false`, .NET 10.0.12, Windows display, recorded
in raw JSON). Earlier Debug runs are retained separately. This is not a production
screen or gameplay system.

`SyntheticTable.cs` generates exactly 1,000 / 10,000 / 100,000 plain records with
12 mixed fields (integers, signed amounts, names, categories, boolean, date and
nullable values), duplicate sort keys and expanded text. Arithmetic fixture
version `synthetic-arithmetic-v1` has no random/environmental inputs.
`DenseUi.cs` uses one built-in Tree with 100-row pages, ordinal/tie-broken typed
sorting, worker-thread queries, 150 ms search debounce, latest-query-wins binding,
stable selected ID, pending/stale action rejection, keyboard/mouse and scrollbars.
Only bounded TreeItems represent the displayed page. No per-record Nodes.

## Executed method and exact command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File qualification/QG02.ps1 -Configuration Release
```

Expanded build and rendered engine commands are retained in the logs. For each
dataset: independent numeric-sort/tie oracle, unknown placement, combined
filter/search, stale and excluded actions, latest-query race, injected keyboard
selection/activation, injected mouse selection, 30 sort/filter cycles, 100 actual
table teardown/rebuild cycles with handler detachment, and 100 seconds of repeated
paging/scrolling/resizing, alternating actual top/bottom row scrolling. Total
rendered interaction is at least five minutes.
The harness also varies three window sizes and 1/1.25/1.5/2 application scale
factors. This is not a Windows OS DPI matrix or a mixed-monitor test.

Frame samples are process-frame deltas during the steady interaction interval,
after cold queries and navigation warm-up. Query times include worker dispatch
and UI binding. Input acknowledgement is page binding through two process frames,
a proxy, not physical input-to-photon latency. Managed memory is collected around
navigation; engine objects and final private bytes are separate measurements.
Retain all raw samples; do not infer production throughput from these workloads.

## Failures and limits

Initial run failed mouse injection on the second dataset after application
scaling. The fixture had passed viewport coordinates to window-level input;
the bounded harness fix applies `Viewport.GetFinalTransform()` before injection.
The failure is retained in `evidence/qg02-initial-input-failure.log`.
Godot documents this coordinate transform in its
[Viewport API](https://docs.godotengine.org/en/stable/classes/class_viewport.html).

No physical-input usability session, Windows scaling/mixed-DPI monitor test, or
approved-hardware acceptance has occurred. The five minutes are distributed over
the three datasets, not five minutes per dataset. Navigation cycles recreate the
table and subscriptions, not an entire production route hierarchy. Actor-hidden
data is absent from this synthetic fixture; it cannot prove production information
security. See `qualification/UI_MANUAL_CHECKS.md` for remaining checks.

## Retained artifacts

Source, gate script, build and benchmark logs, initial failed-run log,
`evidence/qg02.json` raw checks/timings/memory/object observations and
`evidence/qg02-render.png` final rendering. Generated binaries stay ignored.

## Measured result and recommendation

**FACT:** All 36 correctness assertions passed; engine exit 0. A subsequent
12-combination rendered layout check passed 84 reachability assertions (six
toolbar controls plus usable table height per combination), also exit 0.

| Rows | Query p95 ms | Frame p95 ms | Response proxy p95 ms | Controls before/after | Managed retained ratio |
| --- | --- | --- | --- | --- | --- |
| 1,000 | 24.685 | 19.048 | 50.139 | 11 / 11 | 1.00723 |
| 10,000 | 28.799 | 19.600 | 50.228 | 11 / 11 | 1.00016 |
| 100,000 | 83.086 | 19.444 | 50.209 | 11 / 11 | 1.00002 |

Query budgets (250 ms at 10k, 1 s at 100k) and managed retained memory (<10%
growth) were met. Engine object counts during each ten-sample navigation series
were constant: 2816 / 2828 / 2828 respectively. The small measured managed growth
includes the retained measurement list. No duplicate command callback occurred
after 100 rebuilds per dataset. There are 101 live TreeItems including root,
not 100,000 Nodes. Interaction durations total 300.192 seconds. First queries for
each newly generated dataset took 6.320 / 11.427 / 41.113 ms respectively; these
are first-query observations, not process cold-launch timings.

All frame p95 values still miss 16.7 ms. All Release response-proxy p95 values meet
100 ms. No approved-hardware pass is claimed. Final process private bytes were
approximately 298 / 290 / 328 MB; these are observations, not a memory budget.

After the sustained run, bounded layout corrections replaced the toolbar HBox
with a native HFlowContainer, wrapped header/status text, widened the date column,
and synchronized the benchmark dataset selector. The initial final capture had
incorrectly displayed the 1k selector while exercising 100k data; this was a
harness presentation error, not a different dataset. The final layout capture
was inspected at 1280×720 with 2× application scaling, with all actions reachable.
The table requires horizontal scrolling at that scale, intentionally.

Layout follow-up commands (also expanded in `qg02-layout*.log`):

```powershell
. ./qualification/Environment.ps1
Invoke-Recorded 'qg02-layout-build' $Dotnet @('build', "$HostProject/Host.csproj", '-c', 'Debug')
Invoke-Recorded 'qg02-layout' $Godot @('--path', $HostProject, '--audio-driver', 'Dummy', '--', '--ui', '--layout-check')
```

The timing table measures the **final wrapping layout and actual alternating
scrolling in exported Release**. The audit reran the corrected Debug workload
(36 checks PASS, frame p95 82.994 / 80.428 / 75.334 ms) and then the same workload
in Release (36 checks PASS, table above). Earlier HBox timings are retained in
`qg02-initial-timings.json`; corrected Debug timings/log in `qg02-debug-final.*`.
Different build/execution contexts prevent attributing the entire improvement
to compiler optimization alone. The final frame miss is not merely an untested
Debug-only observation. The core is not invoked by this UI workload.

Initial evidence collection had
a PowerShell positional `Select-String` error after the successful engine run;
the script now names `-Path`, and the corrected collection steps were executed.

**PROPOSAL:** Keep QG-02 open/failed pending a bounded Release profiling/remediation investigation
on approved hardware and physical/DPI testing. This fixture required no large
custom table framework, so the measured miss alone is not evidence of a major
engine/architecture blocker. Continue independent QG-03 and QG-04 as authorized.
Do not silently switch engine; return to Director before expanding UI infrastructure.

## Director follow-up — 2026-10-04

Historical observations above are retained unchanged. The user limited this
follow-up to the existing laptop, which is still not approved minimum hardware.
See [FOLLOWUP_SUMMARY.md](FOLLOWUP_SUMMARY.md) for the consolidated final results.

### Baseline and profiling method

The first new five-minute run used the existing final Release artifact. All 199
files matched the retained QG-03 export manifest, including Host, Application and
Domain binaries. Its six managed DLL/PDB differences from the earlier QG-02
manifest reflect the already-recorded QG-03 integration, not a new UI change.
`followup-qg02-untouched-release.*` retains the executed result: 36 correctness
checks passed; frame p95 was 91.388 / 90.739 / 95.697 ms at 1k/10k/100k, and page
response p95 was 134.220 / 118.486 / 136.669 ms. This does not reproduce the old
19 ms observation. A Debug build/export preparation overlapped part of that
initial run, so it is an environmental observation, not a controlled causal A/B
estimate. Subsequent workflow runs and final sustained validation ran sequentially
without concurrent builds or another qualification benchmark.

The new `DenseUiProfile.cs` harness first profiled the unchanged bind algorithm in
exported Release, then the bounded reuse candidate. It separates idle, paging,
scrolling, sorting, filtering, search binding (including the real 150 ms debounce),
same-page row binding and resize/layout. Each workflow has 60 warm-up frames and
at least four measured seconds for each dataset. These short samples are
diagnostic, not substitutes for the sustained acceptance workload. Sorting and
filtering queries still run on a worker; binding still occurs on the UI thread.

Raw evidence includes engine process-frame delta and independent Stopwatch
wall-clock intervals, bind duration/current-thread allocated bytes, worker query
duration, process allocation totals, Gen0/1/2 collection counts, process CPU time,
Godot process monitor, draw calls, viewport CPU/GPU render time and CPU render
setup time. Profiling uses the built-in
[Performance monitors](https://docs.godotengine.org/en/stable/classes/class_performance.html)
and [RenderingServer timers](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html).
Monitors may lag by up to one second and must not be added to unrelated p95s or
treated as per-operation attribution. GC pause duration and individual interop
boundary costs were not separately traced. Binding includes formatting, native
calls and synchronous native work; deferred layout/render work occurs later.

The engine delta can smooth out wall-clock spikes. For example, the original
100k sorting profile reports delta p95 29.996 ms but wall p95 59.998 ms. The
historical acceptance metric remains reported as defined; wall samples provide
additional evidence, not a replacement metric chosen to obtain PASS.

### Findings before changing UI code

All three datasets had idle and scroll-only delta p95 16.667 ms, with wall p95
17.078–17.858 ms. Bind p95 across workflows was 14.646–38.308 ms. Repeated same-page
rebuilds produced delta p95 109.351 / 79.396 / 53.990 ms even with **zero GC
collections** during those stages. Thus GC alone does not explain the misses.
Worker sort p95 at 100k was 91.545 ms, distinct from main-thread bind cost.
GPU rendering was generally below 1.3 ms except resize (up to 4.074 ms); viewport
render CPU p95 ranged from 5.145 to 13.648 ms. There is synchronous and deferred
CPU/UI cost worth reducing before considering a renderer or engine replacement.

The laptop was on Balanced power, battery discharging (46% at observation), with
about 1 GB free physical RAM. These are confounders, not proof of a power or memory
root cause. No driver, power-plan, OS DPI or engine renderer setting was changed.

### Bounded remediation

`DenseUi.Bind` now retains at most 100 native TreeItems and a 100-row cache of
display strings. It updates text/tooltip only when that cell changes, frees
surplus items for short/empty results, refreshes entity metadata, and explicitly
clears/restores visual selection by stable ID. Table teardown clears the cache.
The standard Godot Tree still owns layout, input, focus and scrolling. There is
no custom table/virtualization framework or dataset-wide presentation cache.

The existing 36 checks are retained and 15 additional assertions cover recycled
slot selection, text/ID agreement, one-row and empty-result shrinkage, and bounded
repopulation. The five-minute paging/scrolling/resizing workload, 30 query cycles,
100 navigation rebuilds per dataset, debounce and thresholds remain intact.

`--rebuild-rows` retains a diagnostic reference arm in the same binary: discard
the cached items before every bind. It shares the candidate's bookkeeping and
explicit selection/scroll handling, so it isolates reuse but is not byte-for-byte
the historical implementation. A light-instrumentation arm omits native monitor
polling, render timers and detailed bind/query lists while retaining frame/wall
sampling and the same workflows. Neither arm is used to waive acceptance checks.

Reproduction (PowerShell, repository root; use fresh evidence names):

```powershell
./qualification/Followup.ps1 -Mode Export -Name reuse
./qualification/Followup.ps1 -Mode Manifest -Name reuse
./qualification/Followup.ps1 -Mode Profile -Name profile-reuse
./qualification/Followup.ps1 -Mode Profile -Name profile-reuse-light -Light
./qualification/Followup.ps1 -Mode Profile -Name profile-rebuild-control -RebuildRows
./qualification/Followup.ps1 -Mode Sustained -Name reuse-sustained
./qualification/Followup.ps1 -Mode Layout -Name reuse-layout
```

The first export to the new follow-up directory failed because the directory did
not yet exist (`followup-qg02-profile-export.log`). Creating that owned output
directory fixed the invocation; this was not a Godot/.NET packaging failure.

### Physical input and DPI boundary

Windows API observation confirms **150% / 144 DPI and one monitor**. Automated
Release input, app-scale and resize checks run in that environment. Application
scale factors 1/1.25/1.5/2 do not constitute Windows 100/125/150/200% testing.
Physical mouse/keyboard checks, physical focus/selection/action usability, OS
100/125/200%, and mixed-DPI movement remain **INCONCLUSIVE**. No physical-input
pass is inferred from synthetic events or screenshots. See
`qualification/UI_MANUAL_CHECKS.md` for the pending matrix.

### Final follow-up measurement and disposition

The default-VSync Release candidate completed 300.189 seconds of sustained
interaction and passed all 51 correctness checks (36 historical + 15 reuse
assertions). The separate rendered layout run passed 84 checks. Inspected 1280x720
captures show a reachable wrapped toolbar and usable table at 2x application
scale; right-hand columns require horizontal scrolling as intended.

| Rows | Fresh original frame p95 ms | Final frame p95 ms | Query p95 ms | Page-response proxy p95 ms | Retained ratio | Controls before/after |
| --- | --- | --- | --- | --- | --- | --- |
| 1,000 | 91.388 | 23.258 | 33.153 | 50.271 | 1.03257 | 11 / 11 |
| 10,000 | 90.739 | 22.222 | 54.963 | 50.118 | 1.00100 | 11 / 11 |
| 100,000 | 95.697 | 24.265 | 124.021 | 50.332 | 1.00016 | 11 / 11 |

The original column's build-overlap/environment limitation remains material.
Controlled same-binary rebuild versus reuse wall-frame p95 for same-page binding
was 52.454 vs 17.014, 48.090 vs 17.075 and 45.671 vs 17.109 ms. Allocation p95
per same-page bind fell from about 59.2 KB to 20.5 KB. Light instrumentation left
residual misses, and diagnostic VSync-off improved idle cadence but not all
workflows; it was not adopted. Full per-workflow before/after data and methodology
are in `followup-profile-*.json`, with independently recomputed summaries in
`followup-audit.json`. No single repeat establishes an environment-independent
speedup, and the old 19–19.6 ms measurements are not erased.

**Current machine: FAIL** against 16.7 ms; response, query, memory retention and
control bounds pass. **Approved hardware: INCONCLUSIVE** because unavailable.
There is a tractable bounded implementation improvement, plus unresolved machine
and pacing effects. A disproportionate Godot UI framework is not demonstrated;
Unity reconsideration is not warranted. The proposed workflow-specific budget
discussion in [FOLLOWUP_SUMMARY.md](FOLLOWUP_SUMMARY.md) is a recommendation only;
the existing budget remains unchanged. Phase 4 is not closed; Phase 5 is not
authorized.
