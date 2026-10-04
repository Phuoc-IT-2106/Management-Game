# Company Operating Map Prototype

Blueprint COM-03 v1, 2026-10-05. Status: reviewed for bounded prototype construction
by the assigned UX Architect / Interaction Designer / UI Engineer roles under the
user's explicit PLAN → SPEC → BUILD → VERIFY instruction. This records technical
prototype authorization, not separate Director approval or production acceptance.
Sources: DEC-023, [template](../ui-ux-foundation/SCREEN_BLUEPRINT_TEMPLATE.md),
[kit](../ui-kit/README.md), engineering-neutral-v1.

Role: company-level orientation and situation navigation. Type: Company / portfolio
workspace. Hierarchy: Level 0/1, inspecting nested scope/entity Level 2/3 and routing
to a Level 4 stub. Player question: “What part of my company needs attention and why?”
Primary decision: none; route to a decision context. Entry: separate development
scene. Exit: close window; Back returns to parent/company, workspace Back restores origin.

Primary information: configurable company identity, supported ownership branch,
three functions, attached situations and selected context. Secondary: reasons,
priority/class, status, dates and support relationships. On demand: evidence and
workspace handoff. Historical/raw: only development provenance/revision in evidence.

Entities use stable technical IDs, never names. Read model: immutable bounded
presentation snapshot, revision and session key, organization identity, scopes,
relationships and four situations. No Domain objects, session, content pack, RNG,
clock or saves. Existing Application observations lack complete company identity,
scope and matter contracts; this isolated deterministic study uses presentation
fixtures, not a partial live campaign mixed with fabricated facts. No Application
contract change. Commands: navigation only. Outcomes: selected scope, situation,
workspace kind and return focus. No cash/time/eligibility changes.

Known/estimated/unknown remain labelled with basis. Sponsor terms describe only
supported income/load/preparation trade-offs; amounts and exact outcome are not
invented. Authority: player chooses what to inspect; stubs cannot submit. No draft,
pending, acceptance or gameplay rejection states are simulated. Timing is fixed
fixture day/checkpoint. Priority Critical/High/Normal/Background is presentation
classification, independent of actionable/informational class; at most two
emphasized matters. Remaining matters remain visible with normal emphasis.

Layout: persistent NavigationContext identity; compact current path and time;
scrollable map/list region beside independently scrollable inspector; collapsible
Affairs with chronological direct links. At 1280×720 the inspector retains the
shared InspectorWidth, ownership and support lanes fit the remaining width;
vertical scroll is allowed, horizontal overflow fails verification. Large viewports
use more reading room, not more data. Below two MinimumColumnWidths plus inspector
and margins, use the structured list. No mandatory art, animation or drag interaction.

Canonical components: NavigationContext, EntityLabel, SectionHeader, StatusBadge,
SemanticText, TimeMarker, ConfidenceIndicator, AlertItem, TimelineEvent,
DocumentView, ValidationMessage; UiContext native composition, UiTokens, UiTheme,
BrandResolver. No new reusable component is necessary. Inspector is screen composition.

Mouse: click scope inspects; click attached matter/affair sets affected scope and
situation; direct decision-entry action opens stub. Keyboard: Tab/Shift+Tab follow
visible controls; arrows move between map/list entity controls; Enter/Space activates;
Escape goes workspace → original selection → parent/company. Home/company is explicit.
PageUp/PageDown/Home/End scroll the focused reading region. Restore focus by intent
ID and origin region; fallback to selected scope. No time shortcut or text draft.

Map/list share data and selection. Workspace retains company/session/revision,
scope, originating situation, mode, focus and map scroll. Snapshot replacement
rejects old revisions; changed session invalidates local context even if IDs match.
Missing entity clears stale actions and falls back to nearest surviving ancestor
with explanation. Missing matter falls back to its scope and closes its entry.

Empty: retain company and supported structure; no fake alerts. Quiet: normal
commitments and next checkpoint. Missing identity: Player company · identity
unavailable, stable ID retained. Missing emblem: Stage 2 text fallback. Long name:
wrap full identity. Loading/error/unavailable: no async source in this prototype;
invalid snapshots reject before replacement, stale targets show ValidationMessage.

Performance: bounded eight scopes/four matters, one inspector, two representations;
no nodes for simulated people. Measure initial bind, selection, inspector build,
synchronization, repeated navigation and retained nodes, work vs settled frame time.
Use DEC-022 thresholds diagnostically; no approved hardware/60 FPS claim.

Acceptance: Given a sponsor/preparation affair, activation selects its scope;
opening entry and returning restores exact origin and focus. Given mode switch,
selection is unchanged. Given removed team/matter or new session, stale entry is
unavailable and context is explained. Given quiet/long/missing/alternative brand
fixtures, company remains recognizable with no horizontal overflow.

Automated checks: stable IDs, invalid revision/session/target, both entry paths,
keyboard input, equivalent selection, lifetime/callback counts, uncertainty,
layout and legacy regressions. Candidates at 1280×720, 1920×1080; 2560×1440 if cheap.
All eleven foundation review dimensions and Director questions are assessed in
VISUAL_REVIEW.md. Human usability, physical input/DPI and final acceptance remain
open. Reversal: remove separate scene/scripts/tests; internal Main stays untouched.
