# COS-05 A — isolated runnable prototype

DEVELOPMENT / NONCANONICAL, 2026-10-06. Director visual acceptance is pending.
Baseline: `stage5-sponsor-instruments`, HEAD `7ca1daef337444d46022ad3c3a767e94f6ad5e42`.
The existing dirty `docs/01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md` was preserved.

From the repository root in PowerShell:

```powershell
.\build\Run-CompanyOperatingField.ps1
.\build\Run-CompanyOperatingField.ps1 -Size 1920x1080 -NoBuild
# Reproduce each capture and exercise selection / review / cancel / accept / return:
.\build\Run-CompanyOperatingField.ps1 -Size 1280x720 -Capture -Verify -NoBuild
.\build\Run-CompanyOperatingField.ps1 -Size 1920x1080 -Capture -Verify -NoBuild
```

Entry: `res://UI/Lab/CompanyOperatingField.tscn`. Each launch creates a fresh isolated
fixture session. Select company scopes or Atlas Hardware; open the real Sponsor
decision, review/confirm or cancel, then return to refreshed company context.
Ownership & functions is the structured equivalent; Affairs & timing exposes the
passive DayTrack at 720p. Tab/Enter/Space and Escape use native controls.

**Composition:** The company encloses an owned competitive branch between shared
Finance and Talent responsibilities. Atlas's actual delivery obligation points to
shared capacity and its conditional effect on future preparation before day 6.
The smaller view retains that decision; the larger view adds the existing load/
receipt horizon and conditional financial evidence. No new metrics or gameplay.

**Added files:** `build/Run-CompanyOperatingField.ps1`;
`game/Client/UI/Lab/CompanyOperatingField.cs`, `CompanyOperatingFieldEvidence.cs`,
`CompanyOperatingField.tscn`; this note; the four PNG/JSON files below. All source
addition is prototype-only. No existing runtime, content, save schema or token file
changed; `project.godot` still launches `Main.tscn`.

**Reuse:** `Composition.Create`, `Situation`, `SponsorSnapshot`,
`SponsorPresentation.Company/Track`, `MapNavigation/MapReturn`, `SponsorPresenter`,
unchanged `SponsorWorkspaceView`, `UiTokens/UiTheme/UiContext`, `SemanticText`,
`EntityLabel`, `SectionHeader`, `TimeMarker`, and `DayTrack` with text equivalents.
The local layout adds company containment, a delivery-to-capacity link, eight-scope
inspection and two viewport compositions; it does not introduce a UI framework.

**Captures:** [1280×720](operating-field-evidence/company-field-1280x720.png) /
[manifest](operating-field-evidence/company-field-1280x720.json);
[1920×1080](operating-field-evidence/company-field-1920x1080.png) /
[manifest](operating-field-evidence/company-field-1920x1080.json).
Both show revision 0, day 1, `offer:atlas`, state hash
`0858ad9fa39ab3c68ce9e56dc638f90ffcc98591ff6621136fb463f9e0cc9a9a`.
Load 65 → 90 against capacity 80; 350 CU receipts on days 4/11/18/25; deadline 8.
Captures precede the test commitment. Source digests also match.

**Verification actually run:** `dotnet build ManagementGame.slnx -c Debug -m:1
-p:RestoreLockedMode=true` (zero warnings/errors), then Debug builds/runs of
`tests/{Integration,UI,UiKit,OperatingMap,SponsorWorkspace}` plus Domain DLL:
Domain 4, Integration 162, UI 3, UiKit 132, OperatingMap 48, SponsorWorkspace 57 PASS.
Both capture commands above passed geometry, non-overlap, keyboard selection,
structured scope parity, timing disclosure, real workspace confirmation/cancel,
unchanged-state cancellation, exact matter/focus return, and signed-load/no-cash refresh.
Existing Godot headless `SponsorVerification.tscn -- --sponsor-verify
--sponsor-viewport=1280x720`: 31 PASS. Default production scene `-- --smoke` with
isolated artifact saves: PASS, two results and save/load. `git diff --check`: PASS.
Logs and source manifest: `artifacts/company-operating-field/`. Initial sandbox
certificate-store and atomic-file-replacement errors were resolved by rerunning
local checks outside the sandbox; final native logs contain no errors/exceptions.

**Visual self-review (assistant, not Director approval):** inspected both PNGs.
Requested questions 1–7: yes (company subject, ownership, finance relationship,
Sponsor matter, consequence and route). 8–11: no (no dominant SaaS/debug composition,
spectacle-only element or invented gameplay). 12–13: yes (720p retains primary
content; 1080p adds sourced evidence). Revised Talent's placement outside the owned
team enclosure and aligned delivery load with the capacity relationship.

**Limits:** one existing fixture/offer; bounded English geometry at the two requested
sizes. No arbitrary-name/localization, physical-input/DPI or campaign-scale
qualification. Preparation/talent editing and time advance remain in Main; the
prototype offers inspection plus the real Sponsor path. No save/load UI here;
closing discards the session. Full branding and Director acceptance remain pending.

Next: Director keep / revise / reject review of the two captures. Stop here;
no production Company replacement is part of this delivery.
