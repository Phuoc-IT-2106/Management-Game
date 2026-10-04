# Visual capture and review

All images in this package are **GOLDEN CANDIDATES — NOT APPROVED**. A Director/UI
review must explicitly accept a candidate before it becomes a production baseline.
No automated image comparison or computer-vision approval is implied.

From repository root in PowerShell, with the pinned toolchain installed:

```powershell
./build/Run-UiLab.ps1
./build/Run-UiLab.ps1 -Page interaction -Brand warning -Density Comfortable -TextScale 1.5
./build/Capture-UiKit.ps1
```

The capture script builds Client in locked restore mode, launches the independent
Lab scene, runs native assertions, waits 12 frames after layout/focus/scroll,
waits frame_post_draw, and saves viewport PNG plus JSON. `-NoBuild` reuses the
current binary; use only after a successful current build. `-Quick` runs one case.
Default output: ignored `artifacts/ui-kit/candidates`. The scene never loads content
or writes campaign saves. `--lab-output` is the capture trigger, not a gameplay flag.

Windows can clamp a 1920x1080 window to its desktop work area (observed 1920x1050).
Therefore each capture explicitly sets its own **render viewport** with Godot
Viewport content scaling. Layout is independently recomputed at 1280x720,
1920x1080 or 2560x1440; this does not stretch one predesigned 1280 canvas into all
cases. The PNG dimensions are asserted against the requested case. Native window
dimensions are separately recorded. Manual Lab uses normal resizable window layout.
These are viewport classes, not minimum hardware requirements or DPI evidence.

| Page | Brand | Density / text | Viewport | Scroll fraction |
|---|---|---|---|---|
| identity | neutral | Default / 1 | 1280x720 | 0 |
| interaction | warning | Default / 1 | 1920x1080 | 0 |
| identity | bright | Compact / 1 | 1920x1080 | 0 |
| identity | dark | Comfortable / 1.5 | 1280x720 | 0 |
| identity | low-contrast | Default / 1 | 2560x1440 | 0 |
| identity | identical | Default / 1 | 1280x720 | 1 |
| identity | missing-emblem | Default / 1 | 1920x1080 | 0 |
| interaction | neutral | Default / 1 | 1280x720 | 1 |
| documents | neutral | Default / 1 | 1920x1080 | 0, .5, 1 |
| branding | neutral | Default / 1 | 1920x1080 | 0, .5, 1 |

14 fixed cases. Every case also checks layout of all four pages at that case's
viewport/density/text scale. Selection/focus/estimated/warning and native toggled,
disabled/loading/error/empty examples coexist. Pointer is moved to (0,0) so capture
does not accidentally assert a random hovered control. Hover is native and manually
inspectable; the fixed screenshot is not proof of physical mouse behavior.

Manifests record commit, working-tree dirty flag, SHA-256 source digest, viewport,
native window, page, theme/token version, fixture version/identity/digest, brand,
density/text scale, reduced motion, state, scroll fraction/pixels, settling rule,
engine/runtime/OS/renderer/resolved font, assertion names, node count and PNG hash.
`source-files.txt` lists each UI `.cs`/`.tscn` SHA-256 in sorted relative-path order;
SourceDigest hashes those lines joined with LF, without a final LF. The digest
therefore identifies UI source even when unrelated gameplay work is dirty.
FixtureDigest hashes the selected identity; FixtureVersion and SourceDigest cover
all other fixed records. Branding page intentionally shows all seven brand cases.

Review procedure: use the same engine, OS/font availability, graphics backend and
case; compare the PNGs with the candidate. Inspect truncation/overflow, meaningful
hierarchy, focus, information/status distinction, large text and branding safety.
Changes require review and fresh manifests, never silently overwrite an approved
baseline. System fonts/GPU rasterization can prevent cross-machine byte identity.
The harness fails on nonzero exit, engine stderr, missing manifest, wrong dimensions
or missing native assertions; image hash equality is an additional local repeatability
check, not the cross-platform contract.

Current actual evidence and limitations: [VERIFICATION](VERIFICATION.md).
