# Verification record

Date: 2026-10-05, Asia/Saigon. Result: **UI KIT READY FOR DIRECTOR REVIEW**.
DEC-023: Accepted by the Director's explicit Stage 2 brief. DEC-001–022 text is
unchanged. Phase 5 remains open. This is engineering verification, not human visual
acceptance or release certification.

Implementation commit: `2776b3903d6500e218238d9a1fcccd507c06f3b1`.
The evidence/docs commit follows it. Manifests point at the implementation commit
and include a precise UI source digest. Dirty=true is expected: unrelated existing
gameplay work and documentation were present. None was folded into the UI code commit.

## Environment and commands actually executed

Windows; .NET SDK 10.0.401/runtime 10.0.12, Godot
`4.7.2.stable.mono.official.ed1daf0bf`, Compatibility OpenGL, Intel Iris Xe.
Pinned binaries were loaded with `build/Environment.ps1`. No new dependency/package
or font download was needed. Native renderer captures and save File.Replace tests
needed execution outside the restricted Windows sandbox; they completed there.

```powershell
# Before changes: build current working implementation and establish deterministic baseline.
. ./build/Environment.ps1
& $Dotnet build ManagementGame.slnx -c Debug -m:1 -p:RestoreLockedMode=true
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll

# Final kit build, pure tests, native headless Controls/focus/layout checks.
./build/Verify-UiKit.ps1

# Final rendered matrix: separate fixed render viewports, native checks in every case.
./build/Capture-UiKit.ps1 -NoBuild -Output docs/10-vertical-slice/ui-kit/evidence/candidates
./build/Capture-UiKit.ps1 -NoBuild -Quick -Output artifacts/ui-kit/repeat

# Post-change current gameplay regression, after a successful solution build.
& $Dotnet tests/Domain/bin/Debug/net10.0/Domain.dll
& $Dotnet tests/Integration/bin/Debug/net10.0/Integration.dll
& $Dotnet tests/UI/bin/Debug/net10.0/UI.dll
& $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll
& $Godot --headless --path game/Client --audio-driver Dummy --log-file 'C:/Management Game/artifacts/ui-kit/main-smoke.engine.log' -- --smoke '--content=C:/Management Game/content/fixture.json' '--saves=C:/Management Game/artifacts/ui-kit/main-smoke-saves'
git diff --check
```

Read-only `git ls-remote origin refs/heads/main` verified starting remote main at
`cd6d9866a8969cb45543acb1694913eb78d0a7df`. Governance, Phase 5 documents, every
foundation document, native Main, Application/Domain/Infrastructure, existing tests
and content were inspected before implementation. Lab blueprint/build contract
were written before the kit code.

## Results

| Check | Actual result |
|---|---|
| Pre-change and post-change solution build | PASS, 0 warnings / 0 errors |
| Final Client and UiKit builds | PASS, 0 warnings / 0 errors |
| Pure UI Kit checks | PASS 126: semantic token resolution, 60 text/surface combinations, focus/borders, seven branding cases, contrast reference vectors, immutable/name-independent branding, ID/callback resets, stale revision, unavailable intents, unknown numeric hiding, density/text/no-motion and culture stability |
| Native Godot checks | PASS 24 per final run: rebind 100 times, stable node count/current callback, selection/pressed/tooltip reset, disabled intent suppression, combined selected+focused+estimated, native focus/theme, synthetic Tab/Shift+Tab/Enter, callback release, duplicate pending action suppression, stale/rejected commitment protection, four-page horizontal layout/scroll region, static-reading focus and End key |
| Render matrix | 14 fixed candidates at 1280x720, 1920x1080, 2560x1440; three densities; 150% text; seven brands; exact PNG sizes asserted; stderr empty and process exit 0 |
| Independent repeat capture | PASS: neutral identity 1280x720 PNG byte-identical, SHA-256 `d15e887adf61183b74055f439db3dcd9aa15ea6cd7254d15d9c76e21bcbfc10a` |
| Existing Domain tests | PASS 4 |
| Existing Integration tests | PASS 162, 63 phase boundaries; final hash `f924b06d5f3ffad3c4d14225d743e48336a6ab778dbea768db0bd6ee1757a23b` |
| Existing UI presentation tests | PASS 3 |
| Headless deterministic two-cycle run | PASS; same hash before/after changes (below) |
| Existing internal management UI smoke | PASS; 2 results, 7 bound rows, save/load checked; same deterministic hash |
| Working-file preservation | SHA-256 baseline of 354 existing files; changes restricted to intended governance/foundation docs; existing gameplay/content/tests/client/build work preserved |
| Governance and whitespace | Prior decisions unchanged; scoped diff check clean |

Before/after Headless and native Main smoke hash:
`70ef7dbf4b3acfe2504e0f203f4b223731663af56c398930c989b4dfb5274a12`.
Regression runs exercise the existing **local working implementation**, including
its pre-existing uncommitted files. They are not a claim that the UI kit publishes
or finishes that unrelated gameplay implementation. The kit has no gameplay imports;
its separate pure-test assembly has no Godot/Domain/Application references.

## Review observations and resolved failures

- Initial essential border against SelectedSurface was below 3:1. Border token
  changed centrally; final contrast checks pass. Dynamic brand text and accents
  also pass, including near-black and low-contrast inputs without changing raw data.
- Initial Flow children with wrapped text and no useful minimum width consumed
  excessive height. Shared width tokens fixed that. Width also scales with larger
  text, so the Comfortable label no longer breaks its final letter onto another line.
- Headless's default dummy window was 64x64, and native Windows clamped 1080 to 1050
  before explicit render viewports were added. Those attempts are not counted as
  viewport passes. Final captures assert actual render dimensions and separately
  record native window size; this is not a physical DPI test.
- An early native shutdown raised a retired Godot resource-wrapper finalizer error
  after capture. Captured Image is now explicitly disposed; the bounded Lab flushes
  retired specimen wrappers while the native engine is alive before automated exit.
  Final capture matrix requires successful exit and empty stderr, not just a PNG.
- A sandboxed Integration attempt could not perform File.Replace. The unchanged
  test passed when run with required filesystem access. No save code was altered.
- Image review checked long Vietnamese/Unicode identity, numeric signs/grouping,
  symbol-only person identity, status/uncertainty wording, visible selected focus,
  document terms/results and missing-emblem/brand fallbacks. At 720p with 150% text,
  vertical scrolling is expected and keyboard accessible. Scroll clipping at the
  viewport edge is intentional; no horizontal overflow was found by native checks.

## Boundaries still open

GOLDEN CANDIDATES require Director/UI approval. No physical-input/DPI gate,
DV-03/DV-04, full assistive-technology/WCAG conformance, all-language glyph coverage,
cross-platform font parity, minimum hardware performance or production art approval
is claimed. The fixed captures settle native layout; system fonts/GPU rasterization
can differ on another machine. The finite Lab is not a stress/load test of a future
organization graph or table. Optional decorative motion is not implemented.

Player-facing campaign identity/read-model/DTO/content-digest/save compatibility
remains a separate task described in [IDENTITY_INTEGRATION_GAP](IDENTITY_INTEGRATION_GAP.md).
No Domain/Application/content/persistence production file, current Main UI, main
scene, deterministic rule or existing test was changed by this task. No Company
Operating Map, branding editor or future business module is implemented.

Evidence: [candidates and manifests](evidence/candidates/),
[test/build logs](evidence/checks/). Earlier failed exploratory captures remain
under ignored artifacts, not in the candidate package.
