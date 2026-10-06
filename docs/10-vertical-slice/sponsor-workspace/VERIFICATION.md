# Executed verification — 2026-10-05 (Asia/Saigon)

Local HEAD and read-only remote main matched
`a381d8f26b55132d697e9df9b93e97dcdfc474dc`. Production gameplay is the pre-existing
working implementation. Windows; SDK 10.0.401/runtime 10.0.12; Godot
4.7.2.stable.mono.official.ed1daf0bf; Debug; Compatibility OpenGL/Intel Iris Xe;
Segoe UI, Compact, text scale 1. No dependencies/toolchain/content/schema upgrade.

## Actual execution

Using `build/Environment.ps1` and its pinned executables:

- Baseline and final `dotnet build ManagementGame.slnx -c Debug -m:1
  -p:RestoreLockedMode=true`; additional Client/SponsorWorkspace project builds.
- New tests/SponsorWorkspace DLL; existing Domain, Integration, UI, UiKit and
  OperatingMap DLLs; tools/Headless DLL.
- Main `--smoke` with production content and separate artifact save directory.
- Native OperatingMap `--map-verify --map-viewport=1280x720` and UI Lab
  `--lab-verify --lab-viewport=1280x720` using headless Godot.
- `build/Capture-SponsorWorkspace.ps1 -Quick -NoBuild` during iteration, then
  `build/Capture-SponsorWorkspace.ps1 -Output
  docs/10-vertical-slice/sponsor-workspace/evidence/candidates` for the final matrix.
- `git diff --check` and SHA-256 comparison with the starting working files.

Native Windows access was used for rendered/input runs and existing File.Replace
tests. [Logs](evidence/checks/) and [manifests](evidence/candidates/) record results.
Final source digest:
`a19c65950bbfeef6486b4b2fd035c2a882587638fb7df07ea8037e9cd7de0dfc`.
Manifests record starting commit, dirty state, source files, actual render/window
dimensions, snapshot/revision, seed, checks and raw timings. No clean-checkout claim.

| Check | Result |
| --- | --- |
| Solution/Client builds | PASS, zero warnings/errors |
| Sponsor tests | PASS 42 |
| Native sponsor | PASS 23 per review viewport, 720p and 1080p |
| Captures | 7 PNG/JSON pairs, asserted dimensions, exit 0, empty stderr |
| Domain / Integration / UI | PASS 4 / 162 (63 boundaries) / 3 |
| UiKit pure / native | PASS 126 / 24 |
| OperatingMap pure / native | PASS 48 / 101 |
| Main smoke | PASS, 2 results, 7 bound rows, save/load |
| Gameplay regression | Unchanged hashes below |
| Starting-file preservation | 740 baseline files; 736 unchanged; only Project State and the three intended runtime files changed |

Sponsor tests cover live identity, actor-safe reads (private rival changes produce
identical projection), dates/terms, estimate/unknown distinction, stable IDs under
reordering, correct request/revision, acceptance, stale rejection, re-entrant
pending refresh, repeated commit, existing receipts, missing/expired/claimed/end/
reputation/full-slot/finished guards, cancel/return, removed-matter fallback,
schema-1 save/load, extra-query determinism and actual preparation conversion.
Source checks reject Domain imports/Capture access in workspace UI. Existing kit
tests cover callback reset and unavailable/retired binding behavior.

Native checks use real Controls with synthetic pointer, Tab/Shift+Tab, Enter,
Space, Escape, visible focus, keyboard scroll, list/map routing, focus return,
stale feedback, success/disabled action, spam suppression and real save/load.
They repeat 25 rebind/confirmation and 25 enter/back cycles. Commands operate on
a real session from the production composition; screenshots are not the E2E test.

Headless before/after and Main hash:
`70ef7dbf4b3acfe2504e0f203f4b223731663af56c398930c989b4dfb5274a12`.
Integration hash:
`f924b06d5f3ffad3c4d14225d743e48336a6ab778dbea768db0bd6ee1757a23b`.

## Performance observations

Final review-1280x720 and success-1280x720; raw values retained. Five warmup renders
precede repeated rebind/confirmation. Entry samples include correctness-check entries.

| Metric | Samples | ms |
| --- | ---: | ---: |
| Initial workspace bind work in warm process | 1 | 28.78 |
| Initial entry including five layout frames, warm process | 1 | 149.42 |
| Company selection + workspace entry work | 29 | p95 73.61 |
| Evidence/terms rebind work | 25 | p95 36.46 |
| Confirmation opening work | 25 | p95 40.19 |
| Successful command acknowledgement | 1 | 20.44 |
| Post-command Application refresh | 1 | 0.46 |
| Enter/back plus ten layout-settle frames | 25 | p95 349.63 elapsed |
| Retained workspace Controls | before / after | 53 / 53 |

Single samples are not p95. Elapsed navigation includes test waits, not isolated
frame work/player latency. No deliberate application debounce. Debug/local hardware
is not controlled Release or target-hardware qualification. Rebind/confirmation
exceed DEC-022's provisional 33.3 ms target; performance acceptance remains open.
Potential bounded follow-up: avoid whole-view replacement when opening confirmation,
then remeasure. No broad optimization or historical QG-02 pass is claimed.

## Resolved findings and limits

- Escape originally removed the view before marking the event handled; marking it
  first fixed the exception and preserved origin focus.
- Single-case PowerShell capture array initially flattened; corrected its shape.
- Image inspection found Unicode test-fixture text had been misdecoded during an
  edit. Final UTF-8 text renders correctly; candidates were regenerated.
- Success copy no longer implies a free additional slot or offers an accept/leave
  comparison after signing. Receipt evidence is labelled live, not a prototype.
- No Domain rule/content/persistence changes. Only Main entry, the Session partial
  declaration, and OperatingMap's live seam changed existing runtime files.

Physical input/DPI, mixed monitors, human comprehension, Release/target hardware,
screen reader/all-language coverage, second-machine determinism, final art and
distribution remain open. Synthetic native checks discharge none of those DV gates.
Golden candidates require human review. Phase 5 remains open; next stage not started.
