# Executed Stage 3 verification

2026-10-05, Asia/Saigon. Engineering prototype complete; Director acceptance pending.
Remote main read-only check returned `471a6425c45974207a6afc1d3af0432148400dbd`.
Project state/roadmap/decision log, Phase 5 package, all foundation and UI Kit
documents, current Main/UI/Application/Domain and tests were inspected before build.
PLAN alternatives and SPEC blueprint/component decisions preceded runtime edits.

Code: `efea35a8937d25bd1176a3cd54cf2d9480837eb6`; focus correction and final capture
source: `b005fe873bfc696866c480d58273dddc02f6a819`. Manifests record the latter SHA,
dirty workspace, exact UI source digest and image/fixture hashes. Unrelated dirty
gameplay implementation was already present; it is not published by these commits.

Environment: Windows, SDK 10.0.401, runtime 10.0.12, Godot
4.7.2.stable.mono.official.ed1daf0bf, Compatibility OpenGL, Intel Iris Xe (engine log),
Segoe UI. Debug build; Compact canonical density, text scale 1, reduced motion.
This is local evidence, not an approved minimum hardware or Release performance gate.

## Commands actually executed

```powershell
git ls-remote origin refs/heads/main
. ./build/Environment.ps1
& $Dotnet build ManagementGame.slnx -c Debug -m:1 -p:RestoreLockedMode=true
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll

./build/Verify-OperatingMap.ps1
& $Dotnet build tests/OperatingMap/OperatingMap.csproj -c Debug -m:1 -p:RestoreLockedMode=true
& $Dotnet tests/OperatingMap/bin/Debug/net10.0/OperatingMap.dll
./build/Verify-UiKit.ps1
& $Dotnet tests/Domain/bin/Debug/net10.0/Domain.dll
& $Dotnet tests/Integration/bin/Debug/net10.0/Integration.dll
& $Dotnet tests/UI/bin/Debug/net10.0/UI.dll
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll
& $Godot --headless --path game/Client --audio-driver Dummy --log-file 'C:/Management Game/artifacts/operating-map/main-smoke.engine.log' -- --smoke '--content=C:/Management Game/content/fixture.json' '--saves=C:/Management Game/artifacts/operating-map/main-smoke-saves'

./build/Capture-OperatingMap.ps1 -NoBuild -Quick
./build/Capture-OperatingMap.ps1 -NoBuild -Output docs/10-vertical-slice/company-operating-map/evidence/candidates
git diff --check
```

Native capture and existing integration/File.Replace smoke ran with native Windows
access. Restricted headless runs emitted the known root-certificate-store access
warning; assertions completed. Final rendered runs require exit 0 and empty stderr.
No packages downloaded, toolchain upgrade, new gameplay dependencies or system edits.

## Results

| Check | Actual result |
| --- | --- |
| Baseline solution and final affected Client/test builds | PASS, zero warnings/errors |
| Pure map tests | PASS 45: all fixtures, ancestry/attachments, four workspace kinds, exact return tuple, stale/invalid intent, duplicate entry, removed scope/matter, session reset, rename, quiet/empty, unknown and assembly boundary, malformed projection, relocated matter refresh |
| Native map checks | PASS 35 per final render: synthetic pointer, Down/Tab/Shift+Tab/Enter/Space/Escape; selected list visibility; synchronized selection; sponsor and preparation return; affairs and inspector focus; no duplicate callback; repeated nodes; canonical focus; reading scroll; all twelve scenario layouts; stale actions cleared |
| Stage 2 kit | PASS 126 pure + 24 native checks; no kit source edits |
| Existing Domain | PASS 4 |
| Existing Integration | PASS 162; 63 phase boundaries |
| Existing UI presentation | PASS 3 |
| Headless two-cycle / internal Main smoke | PASS; identical baseline and post-change gameplay hash; Main reports 2 results and 7 bound rows, save/load exercised |
| Golden candidate matrix | 12 scenarios × 2 viewports = 24 PNG/JSON pairs; viewport dimensions and native checks asserted by capture runner |
| Existing-file preservation | SHA-256 comparison: all 462 pre-existing tracked/untracked files unchanged |

Headless before/after and internal Main hash:
`70ef7dbf4b3acfe2504e0f203f4b223731663af56c398930c989b4dfb5274a12`.
Integration hash:
`f924b06d5f3ffad3c4d14225d743e48336a6ab778dbea768db0bd6ee1757a23b`.

## Local performance observations

Representative final `normal-1280x720.json`, raw samples included in every manifest:

| Measurement | Samples | Work p95 ms |
| --- | ---: | ---: |
| Initial bind (cold single sample, not a percentile distribution) | 1 | 116.30 |
| Selection update | 58 | 7.23 |
| Inspector rebind | 225 | 6.03 |
| Map/list synchronization | 225 | 0.69 |
| Decision-entry construction | 53 | 7.57 |
| Return navigation | 52 | 5.82 |
| Repeated selection + two mode changes + entry + return + next frame | 50 | 38.14 elapsed |

Five warm-up navigation cycles precede measured repetition. Work samples include
the 50 repeated cycles plus deterministic scenario/focus checks; counts are explicit,
not misrepresented as independent user trials. Initial bind excludes async settling.
Wall measurement includes several actions plus waiting for the next process frame;
it is not an isolated 60 Hz frame-work test. No intentional debounce. Mode focus
scroll waits two layout frames; selection and focus state acknowledge immediately.

Retained native subtree nodes: **154 before / 154 after** 50 repeated selection,
mode-toggle, entry and return cycles. Exactly 50 callbacks for 50 signal activations.
Normal screenshot has 153 Controls; per-scenario counts differ with inspector data.
No monotonic retained-node growth observed; no claim of a full managed-memory leak
audit, campaign-scale performance or future all-entity capacity.

Discrete measured work is below DEC-022's provisional 33.3 ms rebind and 100 ms
acknowledgement targets in this representative run. Cold construction exceeds
100 ms and is reported separately; it is not an ordinary-action acknowledgement.
The combined wall interval exceeds 33.3 ms but includes five actions; neither this
nor Debug work timings establish a frame-budget pass. QG-02 history remains unchanged.

## Resolved findings and evidence limits

- Initial synthetic pointer injection used native-window coordinates against a
  scaled render viewport. Test now injects local viewport coordinates explicitly.
  It remains synthetic input, not physical mouse evidence.
- Visual inspection found list selection could be below the fold after switching
  modes despite correct focus ID. Added a real visibility assertion and two-layout-
  frame focus scroll; final captures were regenerated from the correction commit.
  Earlier unapproved matrix is retained under ignored artifacts, not final candidates.
- No horizontal overflow across normal, sponsor, competitive, quiet, empty, long
  name, alternate brand, missing identity, list, entry, return and affairs cases.
  Vertical clipping at scroll boundaries is deliberate; 720p density still needs
  human review. Viewport scaling is not Windows DPI verification.
- Map snapshots are fixtures, not live Application observations. Replacement tests
  prove the navigation seam; actual campaign load/new wiring remains out of scope.
- No physical input, DPI/mixed monitor, screen-reader/WCAG, human find-time, 2560×1440,
  Release hardware acceptance, cross-machine or production golden approval claim.
  No DV obligation or QG result was changed. Stage 4 was not implemented.

Evidence: [check logs](evidence/checks/), [candidates](evidence/candidates/),
[visual review and Director tasks](VISUAL_REVIEW.md).
