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
