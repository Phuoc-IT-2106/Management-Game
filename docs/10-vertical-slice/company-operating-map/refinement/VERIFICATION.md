# Executed verification

2026-10-05, Asia/Saigon. Baseline local HEAD and remote main both
`c226f46381e02139ea822058c9f844b8539537ec`. `git ls-remote` initially failed in the
restricted proxy environment, then succeeded with authorized native access.

Environment: Windows, Godot 4.7.2 stable mono, SDK 10.0.401/runtime 10.0.12,
Debug, Compatibility OpenGL / Intel Iris Xe, Segoe UI, Compact, text scale 1.
No package/toolchain, Stage 2 kit, gameplay or content changes.

## Actual commands

```powershell
git ls-remote origin refs/heads/main
. ./build/Environment.ps1
& $Dotnet build ManagementGame.slnx -c Debug -m:1 -p:RestoreLockedMode=true
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll
./build/Verify-OperatingMap.ps1
./build/Verify-UiKit.ps1
./build/Capture-OperatingMap.ps1 -Refinement -Quick -Output artifacts/map-refinement/preview
./build/Capture-OperatingMap.ps1 -Refinement -Quick -Output artifacts/map-refinement/preview2
./build/Capture-OperatingMap.ps1 -Refinement -Output docs/10-vertical-slice/company-operating-map/refinement/evidence/candidates
& $Dotnet tests/Domain/bin/Debug/net10.0/Domain.dll
& $Dotnet tests/Integration/bin/Debug/net10.0/Integration.dll
& $Dotnet tests/UI/bin/Debug/net10.0/UI.dll
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll
& $Godot --headless --path game/Client --audio-driver Dummy --log-file 'C:/Management Game/artifacts/map-refinement/main-smoke.engine.log' -- --smoke '--content=C:/Management Game/content/fixture.json' '--saves=C:/Management Game/artifacts/map-refinement/main-smoke-saves'
git diff --check
```

The final matrix was regenerated after the full-identity capture adjustment and
after correcting the proxy counter to exclude re-inspection of the current
company from context transitions. Geometry/counter changes are not silently
applied to old manifests. Final candidates identify source digest
`ea59db3235dd7535fbbd163cf407809fd7054945fb9341f35a36c70cacaa2a73`.
Their Commit is the starting HEAD and Dirty=true; the per-file source digest
identifies the exact pre-commit implementation. The delivery commit contains
that source plus documentation/evidence; no claim of a clean baseline build.

## Results

| Check | Executed result |
| --- | --- |
| Baseline solution and final Client build | PASS, zero warnings/errors |
| Pure Operating Map | PASS 48, preserving 45 prior checks + semantic ancestry/branch/matter compaction checks |
| Final native Operating Map | PASS 101 at each render viewport; includes all 35 existing checks and refined matched tasks |
| UI Kit | PASS 126 pure + 24 native; kit source unchanged |
| Domain | PASS 4 |
| Integration | PASS 162, 63 phase boundaries |
| Existing UI presentation | PASS 3 |
| Headless before/after and Main save/load smoke | PASS, identical gameplay hash; Main has 2 results and 7 bound rows |
| Visual matrix | PASS 20 PNG/JSON pairs, exact viewport sizes, successful exits, empty stderr |
| Repeated navigation | 106 retained nodes before / 106 after 50 cycles; exactly 50 callbacks |
| Preservation | 617 pre-existing tracked/untracked files hashed; only 7 intended map/test/launcher files changed; 610 preserved |

Native checks preserve synthetic pointer, Down/Tab/Shift+Tab/Enter/Space/Escape,
stable map/list selection, focus, Affairs scope/return, inspector origin, empty/
quiet/branding/missing identity, stale removed context and no horizontal overflow.
Refinement checks add individually reachable compressed scopes, full path,
compact header, preparation via dated drawer, both views' stress handoffs and
zero-scroll entry visibility. Pure tests preserve session reset, old revision
rejection, relocation and exact return tuple. No live campaign refresh is wired.

Headless and Main hash:
`70ef7dbf4b3acfe2504e0f203f4b223731663af56c398930c989b4dfb5274a12`.
Integration hash:
`f924b06d5f3ffad3c4d14225d743e48336a6ab778dbea768db0bd6ee1757a23b`.
These regressions exercise the existing local gameplay implementation, including
its pre-existing uncommitted files. That unrelated work is not published here.

## ENGINEERING PROXIES: shared runtime

Final map-normal-1280x720 manifest; five warm-up cycles precede repetition.
Samples include synthetic navigation and fixture checks, not independent people.

| Metric | Samples | Work p95 ms |
| --- | ---: | ---: |
| Initial bind (one cold observation, not a distribution) | 1 | 123.58 |
| Selection update | 74 | 9.46 |
| Map/list synchronization | 257 | 0.93 |
| Inspector rebind | 257 | 6.86 |
| Decision entry | 59 | 12.64 |
| Return synchronous work | 58 | 9.68 |
| Selection + two toggles + entry + return + next process frame | 50 | 50.89 elapsed |

Both representations are retained in the same process. These shared timings
cannot establish a map-versus-list performance winner. Final focus/scroll restores
after layout frames and is excluded from synchronous return time; epoch checks
discard outdated pending restorations. Cold bind exceeds 100ms; combined elapsed
work exceeds 33.3ms and comprises multiple actions. No FPS, Release hardware,
memory-leak audit or production performance gate pass is claimed.

## Resolved failures and limits

- Initial compile found Godot/UI Kit StatusIndicator name ambiguity; explicit alias fixed it.
- Initial header swatch text had no useful minimum width and wrapped vertically;
  a nonwrapping [CO] swatch resolved it without shrinking type.
- Expanded exact-return test exposed premature scroll restoration after list
  layout. Restore now waits for native layout and rejects stale deferred work.
- Restricted headless execution hit the known Windows certificate-store warning;
  final native/capture checks ran with required access and empty stderr. Existing
  save replacement tests similarly used native access; no save code was changed.

Human usability, physical input/DPI, mixed monitors, assistive technology,
localization/text scaling beyond the tested setting, live identity integration,
approved hardware and future company scale remain unverified. All governance,
QG history and DV obligations retain their prior status. Stage 4 was not started.

Evidence: [capture manifests/logs](evidence/candidates/) and [check logs](evidence/checks/).
