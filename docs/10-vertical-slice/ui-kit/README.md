# Phase 5 UX Stage 2 — Design System + Minimal UI Kit

Status: **UI KIT READY FOR DIRECTOR REVIEW**. Date: 2026-10-05 (Asia/Saigon).
Authority: Director's explicit Stage 2 brief; DEC-023 Accepted. Phase 5 remains open.

This bounded implementation translates the [accepted foundation](../ui-ux-foundation/README.md)
into one **PROVISIONAL DEVELOPMENT THEME**, actor-safe presentation contracts,
17 small reusable components and a deterministic internal UI Lab. No campaign,
Domain/Application command or persistence migration is needed to run the Lab.

## Build contract established before implementation

- Tokens, immutable presentation records and brand contrast calculations are
  plain C# in `game/Client/UI/`; native Controls/Theme are thin rendering adapters.
- A separate `UI/Lab/UiLab.tscn` entry coexists with unchanged `Main.tscn/Main.cs`.
  No application-wide theme is installed on the internal management UI.
- Native Theme variations define semantic typography, statuses, spacing/density,
  focus and surfaces. A shared resolver receives identity data and only returns
  branding roles. Raw campaign choices and system status colors are untouched.
- Rebinding replaces callback targets, IDs, revision and all local selection/
  availability state. Components emit presentation intents; no gameplay executor.
- The lab has bounded specimens, not one Control per campaign entity. Unknown,
  estimated, domain status and interaction states remain independent.
- Plain .NET tests exercise tokens/contrast/data; real Godot checks exercise
  binding, keyboard focus, reflow and native theme behavior. Fixed viewport
  screenshots and manifests remain GOLDEN CANDIDATES pending human review.

Documents: [tokens](TOKEN_CATALOG.md), [theme](THEME_IMPLEMENTATION.md),
[components](COMPONENT_CATALOG.md), [Lab blueprint](UI_LAB.md),
[capture procedure](VISUAL_VERIFICATION.md), [identity gap](IDENTITY_INTEGRATION_GAP.md),
[verification](VERIFICATION.md), [questions](OPEN_QUESTIONS.md).

Start: `./build/Run-UiLab.ps1`. Check: `./build/Verify-UiKit.ps1`.
Capture: `./build/Capture-UiKit.ps1`. The pinned Godot/.NET environment is required.
These launch the independent Lab scene and leave the current management entry intact.

Delivery: semantic colors/typography/spacing/sizing/motion, native Theme variations,
safe organization branding, all 17 requested components, four Lab pages, three
densities, up to 150% text and seven branding fixtures. Verification covers pure
contracts, real Godot binding/focus/layout and gameplay regression. See the linked
report for exact counts, source commit, capture evidence and unresolved gates.

Review [golden candidates](evidence/candidates/), not approved production screens.
Identity/save integration is documented but deferred. No map, company wizard,
branding editor, final art, future business module or new simulation was built.
The next recommended task is a bounded Company Operating Map prototype, after
Director review of this package. Stage 2 stops here; Phase 5 remains open.
