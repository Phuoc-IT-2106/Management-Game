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

Final measurements and evidence links follow after verification.
