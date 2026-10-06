# Performance acceptance — 2026-10-06

## Finding recorded before the presentation fix

Inspected baseline `7ad0501fd0719c48b8c759a491771525567fa863`.
The first measurement used that rendering algorithm plus measurement instrumentation
(source digest `4b4dcf97210f43963210874bb3b92ad9939c1ae2f2eb4f1704d4cb956f35fb92`).
No fixture or production rendering change preceded this measurement.

50 measured iterations after five warmups on the same local Windows / Intel Iris Xe
machine, native Godot 4.7.2 Compatibility renderer at 1280×720. Debug rebind p95
20.3002 ms and Review→Confirmation 20.4443 ms. Managed Release rebind p95
53.8067 ms and Review→Confirmation 44.7857 ms. Release exceeds 33.3 ms, so a
bounded presentation fix is warranted. Release was slower in this sequential
local run; this is not evidence that compiler optimization causes the difference.
Machine scheduling, warmup and power/thermal conditions were not controlled.

Measured cause: full view reconstruction. In each configuration 100/100 observed
rebind/confirmation transitions replaced the document region; Theme identity was
unchanged. Release section medians: document creation 8.2062 ms, evidence creation
7.5060 ms, shell 3.5258 ms, actions 2.3458 ms, retire 0.5032 ms (300 binds).
Document/evidence dominate synchronous binding work. Deferred layout/QueueFree
cost is not isolated by these section timers. No evidence supports rebuilding
Theme as the cause or changing Application/Domain: command p95 0.3472 ms,
refresh p95 0.2503 ms. Retained workspace Controls remain 53→53.

Selected fix: retain the workspace shell, update feedback/actions on phase changes,
and replace document/evidence regions only when their displayed data changes.
Always bind the current offer ID/revision to commitment actions; never use display
text or revision alone as an identity key. No framework or gameplay change.

## Measurement method and limits

`build/Measure-SponsorWorkspace.ps1` builds Debug and optimized managed Release,
stages each in an isolated project and checks the loaded assembly's configuration,
JIT optimization attribute, module ID and staged DLL SHA. Godot's editor engine
loads the Debug path, so copying Release there is explicit and verified. Both use
the same native editor engine and GodotSharp binding; this is a representative
managed Release measurement, **not an exported release-engine qualification**.
Release needs a separate generated lock in ignored `obj` because the pinned SDK
omits GodotSharpEditor; the checked-in Debug lock and package versions are intact.

Each iteration uses a real session and fresh offer: initial bind, a real coach
revision change followed by ObserveSponsors/Render, review, back, explicit review
and successful commit, return, and repeated enter/back. Query work for ordinary
rebind occurs before its timer; post-command query is separately timed. Discrete
work excludes fixed frame waits; enter/back elapsed includes ten settle frames.
Nearest-rank p95; 50 samples per metric. Initial bind is warm-process creation,
with one cold-process first bind reported separately. No physical-input, frame
latency, target hardware, distribution or historical QG-02 pass is claimed.

## Final result

Verified runtime source: `043ad9bee1c149eb9cce407a419a1e5eda6f8799`;
source digest `736c78060aa436589d24b65d82b1cbe467f4b12e5cb85f93c8aa295c74a77aa1`.
The only intervening fixture edit is the technical company name. Scenario, viewport,
seed, workload, warmups and sample counts are identical. Timings are milliseconds;
all table values are p95 over 50 iterations.

| Work | Before Debug | Before Release | Final Debug | Final Release |
| --- | ---: | ---: | ---: | ---: |
| Initial bind, warm process | 21.6824 | 47.0692 | 65.4021 | 46.8678 |
| Ordinary rebind, fresh revision | 20.3002 | 53.8067 | 5.3073 | **3.6017** |
| Review → Confirmation | 20.4443 | 44.7857 | 10.2538 | **10.9039** |
| Confirmation → Review | 19.2972 | 46.7171 | 4.6243 | 5.1681 |
| Successful command acknowledgement | 0.3650 | 0.3472 | 0.2472 | **0.2249** |
| Post-command Application refresh | 0.1452 | 0.2503 | 0.1597 | 0.1357 |
| Commit, query and changed-data rendering | 15.4648 | 35.1039 | 28.4509 | 27.1531 |
| Return after signing, company refresh | 29.9897 | 70.8675 | 61.5260 | 46.4207 |
| Repeated company selection + entry work | 44.1254 | 93.2199 | 89.2472 | 72.5825 |
| Repeated back work | 32.2489 | 79.3549 | 77.7367 | 64.7703 |
| Enter/back including ten frame waits | 233.4798 | 366.9358 | 349.4719 | 332.7713 |

Single cold initial binds (not p95): before Debug/Release 21.9091/47.3991 ms;
final Debug/Release 59.2821/84.9869 ms. These and the different Debug versus Release
ordering show local variability; do not infer broad speedups from one machine run.

All four runs retain **53→53 Controls**. The observed terms-scroll region replacements
drop from 100 to 0 across 50 ordinary rebinds and 50 confirmation transitions.
Native regression separately asserts actual document retention, scroll position,
current revision metadata, and immediate retired commitment callback cleanup.

The fix keeps the shell and scroll containers, compares bounded sponsor display
records, updates header/feedback, and rebuilds only changed document/evidence or
action regions. Fresh revision rebind deliberately changes real Application revision
without changing the terms; it is not a stale cached snapshot. Successful commitment
does change terms, load and receipts, and its full render is measured separately.
Same-revision changed company/offer/cash is covered by native tests. Commitment
buttons always bind current identity/revision. Application/Domain code is unchanged.

**Performance acceptance: PASS for the two scoped concerns.** Release rebind and
confirmation are below 33.3 ms p95; command acknowledgement is below 100 ms.
Initial construction and company-map navigation still exceed 33.3 ms p95; their
Release p95 synchronous work remains below 100 ms. They involve creating the first
view or refreshing the existing map, unlike retained-region transitions. Carry them
forward explicitly under local/target-hardware validation; broad map or component
optimization would exceed this narrow pass. This is not a claim that every UI
operation or frame meets DEC-022. No need for another optimization round here.

Raw samples, section profiles, build logs, engine output and source inventories:
[before](evidence/performance/before/) and [after](evidence/performance/after/).
Reproduce with `build/Measure-SponsorWorkspace.ps1 -Output <fresh-artifact-path>`.
Historical QG-02 and the wider DV gates remain unchanged.
