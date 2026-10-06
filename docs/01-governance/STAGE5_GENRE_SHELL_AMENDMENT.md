# Stage 5 amendment — approved management-genre shell

Status: **ACTIVE — explicit user instruction**, 2026-10-06 (Asia/Saigon). Later
authority than the [preserved 50-rule brief](STAGE5_VISUAL_UX_BRIEF.txt), the
[visual / UX governance](STAGE5_VISUAL_UX_GOVERNANCE.md), the
[composition direction](STAGE5_VISUAL_COMPOSITION_DIRECTION.md) and the
[game-identity thesis](STAGE5_GAME_IDENTITY_THESIS.md) where they conflict. The
brief text stays byte-identical; this page records how it now applies. DEC-001–023
are unchanged; a Director DEC entry should confirm the amendment.

## Why

Reference review of Football Manager 26, Soccer Manager 26 and Esports Manager 2026
shows one shared genre skeleton: persistent global navigation, a top bar with
identity, date, money and a single Continue control, a portal/inbox of actionable
matters, dense sortable tables for squad, market, fixtures and finance, deep
profiles and a staged match day. FM26 names *Familiarity* as a design principle;
EM2026 copies the FM menu outright; SM26 surfaces buried features through a
collapsible left menu and a task bar. Treating that skeleton as a "dashboard"
hard rejection made a recognizable management game impossible to build and left
every screen as bespoke text composition. Lessons kept from the same sources:
FM26 tiles caused usability/accessibility problems; EM2026 was criticized for
squad stats not visible together, no quick profile flipping, thin post-match
analysis and notifications that do not name the person affected.

## Approved genre shell (pre-approved; no per-screen justification needed)

| ID | Element | Contract |
|---|---|---|
| S1 | **Top bar** | Company identity (name + monogram/crest), season and day, cash, attention count, one primary **Continue** control that states why it is blocked. Always visible. |
| S2 | **Global navigation rail** | Left, collapsible to a glyph rail. At most 9 primary sections, each a real playable area of the current slice; sub-tabs inside a section are allowed. Keyboard shortcuts per section. |
| S3 | **Content area + context panel** | One dominant content area per section; an optional right context panel for selection detail (profile, offer, evidence). |
| S4 | **Portal home** | Task strip (what must or should be decided), inbox of actionable messages naming the person/brand affected, next match, season record, money snapshot, recent results. It replaces the Company Operating Space as the default home. |
| S5 | **Dense tables** | Squad, free agents, coaches, offers, fixtures and finance may be table-centred; all squad facts visible together, keyboard row flipping updates the context panel. |
| S6 | **Overview tiles** | Allowed on Portal/section overviews when each tile answers one question and leads to a real destination; at most 8 per screen; no tile is decorative. |
| S7 | **Trend charts** | Allowed on dedicated analysis screens with a stated question, units and source. |
| S8 | **Generated identity marks** | Procedural monograms/crests and symbolic avatars from campaign data. |
| S9 | **Staged events** | Match day, results and season close may use a dedicated full-content presentation. |

## Rules amended

| Source | Original reading | Amended reading |
|---|---|---|
| Brief #4 (avoid dashboard composition) | Cards/regions/grids suspect by default | S4/S6 are approved; still reject equal-weight decorative grids and KPI walls without a decision reason |
| Brief #19 (tables are not identity) | Avoid table-centred screens | S5 approved; identity is carried by shell, Portal, match day and company marks |
| Brief #24 (navigation is not tabs) | No permanent top-level sections | S2 approved with the 9-section cap; no section for unbuilt mechanics |
| Brief #34 (no left SaaS sidebar) | Left navigation rejected | Collapsible left rail approved; SaaS *styling* (white surfaces, blue CTA, rounded card stacks) still rejected |
| Brief #41 (AI may not invent navigation) | No new navigation paradigm | The genre shell is pre-approved; inventing other paradigms still needs review |
| Composition direction C2/C4 and the operating-space blueprint | Company Operating Space should be the dominant home | Becomes the optional **Company** section; the Portal is home |
| Composition direction C3 and the 2026-10-06 governance note "No web/dashboard primary composition" | Dashboard composition is a hard rejection | Replaced by the shell contract above plus the rejection list below |
| Thesis P4 | Every primary screen draws an instrument | At least one drawn instrument on Portal or a decision workspace; tables satisfy S5 |

## Unchanged (non-negotiable)

Known / Estimated / Unknown remain distinguishable without color; no fake precision.
Semantic colors stay independent of branding. Every action and fact is keyboard
reachable; 1280×720 keeps identity, time, Continue, the primary decision and its
uncertainty. No invented data, people, modules or locked future sections. Shared
components are not restyled per screen. No developer, contract or keyboard-legend
text in player space beyond the development watermark. One primary action per
workspace; irreversible commitments are confirmed. Material decisions keep their
evidence and exact return.

## Still rejected

SaaS styling (white dashboards, blue CTAs, floating rounded card stacks); KPI walls
or charts without a decision question; equal-weight decorative tile grids; neon or
cyberpunk esports styling; decorative art filling space; sections for mechanics the
slice does not have.

## Review

Screens record S1–S9 conformance in the
[screen review record](../10-vertical-slice/ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md).
A–J question H ("could this belong to project-management software?") now asks
whether SaaS *styling* or a decision-less metric wall is present, not whether the
genre shell exists. The shell specification and verification live in
[genre shell](../10-vertical-slice/genre-shell/README.md).
