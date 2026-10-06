# Stage 5 visual composition direction

Status: **ACTIVE — DIRECTOR CLARIFICATION** under DEC-023 and
[Stage 5 visual / UX governance](STAGE5_VISUAL_UX_GOVERNANCE.md).
Date: 2026-10-06 (Asia/Saigon). Scope: governance and specification only.

Source: Director instruction “PHASE 5 — VISUAL COMPOSITION ALIGNMENT”, attachment
`06f89646-a886-40c6-921b-59568b763beb/Pasted text.txt`.
Source SHA-256: `42719a05965b0a6dc883a577ce15f4ad4b242e3dd61abdba12ebe031eb61f9df`.
Inspected local and remote main: `1241bc1d4972481fd5c91c621147da4d6d922a18`.

## Authority and exact correction

This is additive, later authority for interpreting the preserved
[50-rule brief](STAGE5_VISUAL_UX_BRIEF.txt). It changes only the assumption that
list-first/default navigation requires a visually dominant list on the company
screen. It does not replace accepted decisions, navigation or Application contracts,
approve final art, or accept a Stage 5 screen. DEC text remains unchanged.

The target is a **COMPANY OPERATING / COMMAND SPACE**: company + owned operating
portfolio + corporate functions + active business relationships and situations +
time + commitments + consequences, perceived as one connected management-game
space. The player should understand “I am operating this company.” Neither an
administrative tree with inspector and affairs strip nor a generic dashboard is
the intended final company-level identity.

## Evidence reconciled with the clarification

| Inspected source | Established evidence and limit | Stage 5 application |
| --- | --- | --- |
| [DEC-023](DECISION_LOG.md#dec-023--company-first-ux-architecture), [company direction](../10-vertical-slice/ui-ux-foundation/COMPANY_UX_DIRECTION.md), [information architecture](../10-vertical-slice/ui-ux-foundation/INFORMATION_ARCHITECTURE.md) | Company is root; function and portfolio are distinct; home already described as operating space | Clarify composition within this architecture, not a new simulation or navigation paradigm |
| [Stage 3 blueprint](../10-vertical-slice/company-operating-map/SCREEN_BLUEPRINT.md), [Stage 3.1 comparison](../10-vertical-slice/company-operating-map/refinement/COMPARISON_MATRIX.md), [720p evidence](../10-vertical-slice/company-operating-map/refinement/720P_REVIEW.md) | Same semantic snapshot/IDs/selection/return; list fewer Controls; map simultaneous relationships and better measured vertical density; bounded geometry/task scripts, not human preference | Preserve every result. These prototypes did not test the final company visual direction |
| [Stage 3.1 recommendation](../10-vertical-slice/company-operating-map/refinement/DIRECTOR_RECOMMENDATION.md) | Historical ACCEPT LIST-FIRST / MAP-OPTIONAL; later accepted for Stage 4 | Structured reference remains required; historical default does not prescribe Stage 5 visual dominance |
| [Sponsor interaction](../10-vertical-slice/sponsor-workspace/INTERACTION_SPEC.md), [Stage 4.1 acceptance](../10-vertical-slice/sponsor-workspace/acceptance/README.md) | Fixed-offer review/commit/refresh/exact return; engineering acceptance complete, human review pending | Preserve decision-workspace exemplar and its approval limits |
| [MapModel.cs](../../game/Client/UI/OperatingMap/MapModel.cs), [OperatingMap.cs](../../game/Client/UI/OperatingMap/OperatingMap.cs) | Shared MapSnapshot/MapNavigation; live list default; existing inspector and narrow-width list fallback | Implementation evidence, not a final composition specification; no runtime change here |
| [SponsorPresenter.cs](../../game/Client/UI/SponsorWorkspace/SponsorPresenter.cs), [SponsorCompanyHost.cs](../../game/Client/UI/SponsorWorkspace/SponsorCompanyHost.cs) | Live projection contains sponsor matters/receipt; company view hides during decision; return refreshes observation | Do not mislabel all prototype matters as live; preserve transition rather than keeping a full-size map behind a decision |
| [Kit catalog](../10-vertical-slice/ui-kit/COMPONENT_CATALOG.md), [tokens](../10-vertical-slice/ui-kit/TOKEN_CATALOG.md), [theme](../10-vertical-slice/ui-kit/THEME_IMPLEMENTATION.md) | Small semantic primitives; several render PanelContainer; engineering-neutral-v1 explicitly provisional | Reuse semantics, not an obligatory bordered-card composition or permanent palette |

The mismatch is an inference from these sources: a successful navigation prototype
and its list-default integration could be mistaken for the final visual design,
while anti-dashboard/anti-decoration rules could be overread as prohibiting an
operating visualization and useful instruments. The Director explicitly corrects
both interpretations; no usability result is retroactively changed.

## C1 — List-first navigation authority

LIST-FIRST means reliable structured company hierarchy/reference, explicit ownership
order, deterministic keyboard-accessible navigation, and shared stable IDs,
selection, session/revision and return context. No player must understand a spatial
illustration to navigate. List/map selection never creates a second gameplay truth.

It does not require the list to occupy the dominant center, turn home into an
administrative tree editor, define the game's visual identity, or make an operating
space visually secondary. A structured equivalent remains readily discoverable
and usable; whether it is persistent, compact or disclosed is a review question.
The historical LIST-FIRST / MAP-OPTIONAL conclusion stays recorded as such.

## C2 — Dominant operating space with semantic parity

The Company Operating Space may be the dominant visual and orientation surface if:

1. it projects the same authoritative semantic company model;
2. equivalent structured navigation remains available;
3. no gameplay fact exists only in the visual representation;
4. keyboard/accessibility paths do not require spatial interaction;
5. every interactive region represents real scope, function, situation,
   relationship or supported workspace and has navigation/state/context meaning;
6. it is not decorative wallpaper.

Owned structure remains Player Company → Competitive Portfolio → Esports →
Development Discipline → Primary Team, with shared functions distinguished from
owned businesses. Relationships must distinguish ownership from support, constraint
and chronology. No generic graph engine, all-entity network, fake 3D traversal or
invented city-builder follows from this direction.

## C3 — No dashboard means structural rejection

Generic web/SaaS/dashboard composition remains a **HARD REJECTION** as the primary
gameplay model: sidebar + KPI-card wall, analytics company home, universal equal-card
grid, generic admin/project-management composition, static metrics + CTA as the
whole experience, or finance/talent/competition reduced to unrelated web pages.

Panels, lists, tables, charts, status regions, top-level navigation, contextual
summaries, timeline regions and resource values are allowed as **simulation
instruments**. Each answers an actual player question through projection,
orientation, shortcut, consequence preview or decision entry into the same state.
Omit any region without current decision/context value. Do not fill space with
metrics or automatically create a permanent KPI bar.

The structural test is whether the player operates a changing company/world or
browses business software. Styling alone cannot answer or repair this distinction.

## C4 — Composition roles and workspace transition

The [draft company blueprint](../10-vertical-slice/company-ui-demo/COMPANY_OPERATING_SPACE_BLUEPRINT.md)
specifies eight semantic roles, not final pixels:

1. Persistent company identity, current day/date/checkpoint, selected scope/path and
   material strategic attention. Resolve identity from campaign; never hard-code it.
2. Supported Company / Business & Finance / Competitive Portfolio / Talent &
   Performance / Affairs & Time access. A lens is not automatically a new module.
3. Structured ownership navigator for orientation, exact scope and keyboard parity.
4. Central company operating visualization expressing actual scopes, relationships,
   current state and attached matters.
5. Bounded active situations: why now, scope, deadline/horizon, consequence,
   priority/class and uncertainty where relevant; not generic notifications.
6. Contextual management instruments with a named current question and source.
7. Affairs/time access preserving chronology, due items and existing stop semantics.
8. Transition into a contextual decision workspace with company, function/scope,
   originating matter, time/revision and return context preserved.

Company-level and decision-workspace compositions may differ. The operating space
need not remain visible at full size during a decision. Sponsor remains the verified
exemplar: company context → commercial document + company evidence/trade-off →
review → commitment → refreshed consequence → exact valid return. No universal
layout is imposed. Selection does not commit; stale commitments require fresh
review, pending prevents duplicates, and new/load sessions invalidate old context.
UI attention priority never changes simulation stops; no full calendar is invented.

## C5 — Current scope and reference discipline

Current scope: one player company, one development esports discipline, one primary
team, limited competition, current finance/sponsor, roster/coach/preparation and
affairs/results. Other functions may remain architecture/specification or embedded
evidence, never fake playable navigation. Do not add Football, Basketball, Media,
Merchandising, M&A, Facilities or locked future divisions to suggest an empire.
Future scale comes from architecture and composition.

References may inform composition, hierarchy, density, sense of company scale,
central-space/instrument relationships and management-game atmosphere. They may
not import company names, logos, currency/audience values, sponsors, leagues,
sports, future divisions, negotiation systems, modules, mechanics or art assets.
Every displayed gameplay element must trace to current project authority; a
reference image is never a content specification.

## C6 — Semantic visualization versus decorative art

Reject decoration used to fill space: generic esports arenas, futuristic HQ
wallpaper, cyberpunk cities, random office images and noninteractive illustrations
with no state/navigation role. Removing cards or gradients alone does not pass.

A semantic operating visualization represents actual scopes/functions/relationships,
can be selected/inspected, reflects state, anchors orientation and leads to real
contextual workspaces. It preserves stable IDs, actor-safe data and a text/list
equivalent. A future illustrated headquarters/company campus may enrich this
representation only under those conditions and later art review. Bespoke HQ art
is never a Stage 5 dependency; a schematic/vector/structured version must work.

## C7 — Shared primitives and provisional aesthetics

Keep Stage 2 semantic tokens/theme roles, IDs, EntityLabel, ResourceValue,
TimeMarker, ConfidenceIndicator, StatusIndicator, DocumentView, ComparisonView,
CommitmentReview and NavigationContext semantics. Components are primitives,
not the final Company composition. A PanelContainer implementation does not require
every region to become a bordered card. Compose existing controls first; if that
fails, propose the smallest centralized visual-system refinement with rationale
and shared-consumer review. Never restyle components locally per screen.

`engineering-neutral-v1` remains **PROVISIONAL DEVELOPMENT THEME**. `#171A1F`,
4px radius, current fonts and panel treatment are replaceable engineering choices,
not permanent aesthetic requirements. Preserve semantic roles, brand independence,
contrast requirements and visible focus. Later reviewed visual exploration may use
the same token architecture. This correction changes no token values or assets.

## C8 — Viewport contract

1280×720 is a hard design stress case. Preserve company identity/context, current
time/checkpoint, selected scope, primary operating visualization or an equivalent
meaningful center, current material situation, route to its decision and affairs/time
discoverability. Secondary instruments may compress, collapse or become contextual.
Do not solve density by reverting the center to a giant list. No horizontal scrolling
for the primary gameplay shell; do not hide critical uncertainty or shrink text to
evade overflow. Structured navigation remains keyboard-reachable.

1920×1080 uses additional space for relationships, evidence, operating state,
useful contextual instruments, affairs/time and situation consequences. Do not
merely stretch the 720p layout or invent metrics to fill it. Final proportions,
reflow and visual anchors need new evidence; Stage 3.1 measurements are historical.

## C9 — Two failure modes and management-game identity gate

Reject both A: generic web/SaaS dashboard and B: engineering/admin/debug interface.
B includes a company screen dominated by a tree, editor-like inspector, internal
technical hierarchy used as identity, database-browsing gameplay, or composition
that communicates implementation structure rather than company operation.

For each company-level proposal answer all ten questions with review evidence:

1. Without branding, does this still read as a management/simulation game?
2. Is the company visibly the subject being operated?
3. Can the player understand ownership and shared corporate functions?
4. Are time, situations, commitments and consequences visible?
5. Does the central composition communicate a changing simulation rather than a static database?
6. Is the structured list available without defining the whole visual identity?
7. Could this exact composition be dropped into generic SaaS?
8. Could this exact composition be mistaken for editor/debug tooling?
9. Are surrounding information surfaces actual simulation instruments?
10. Is any visual element present only to make the screen look impressive?

A YES to 7, 8 or 10 requires correction, not an aesthetic waiver. A NO to a positive
requirement requires revision or explicit pending evidence. Use the expanded
[screen review record](../10-vertical-slice/ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md)
alongside the unchanged 50 rules, A–J audit and 14-part gate. No result is claimed here.

## Traceability and remaining gate

| Contract | Normative authority |
| --- | --- |
| C1–C2; scope of supersession; C4 central role; C8 meaningful center | Explicit 2026-10-06 Director clarification, reconciled with DEC-023 and historical Stage 3/3.1 evidence above |
| C3 and both C9 failure modes/ten questions | Explicit Director clarification; preserved brief rules 3–4, 19, 34, 42–44 |
| C4 context, commands, chronology, return | [Interaction model](../10-vertical-slice/ui-ux-foundation/INTERACTION_MODEL.md), Stage 3/4 contracts above; workspace-size distinction made explicit by Director |
| C5 supported scope/reference exclusions | Explicit Director clarification; [module landscape](../10-vertical-slice/ui-ux-foundation/MODULE_LANDSCAPE.md), brief rules 24, 39–41 |
| C6 semantic art distinction | Explicit Director clarification; [art policy](../10-vertical-slice/ui-ux-foundation/ART_PRODUCTION_POLICY.md), brief rules 31–33 |
| C7 primitives/provisional values | Explicit Director clarification; inspected Stage 2 catalog/tokens/theme, brief rules 8, 36–38 |
| C8 density and C9 review evidence | Explicit Director clarification; brief rules 46–49 and [verification strategy](../10-vertical-slice/ui-ux-foundation/UI_VERIFICATION_STRATEGY.md) |

The draft blueprint still needs Director visual review: central representation,
relative emphasis, navigation disclosure, instrument selection, reflow, relationship
grammar and any shared theme refinement remain open. Human Stage 4 review, identity
migration and all existing DV/QG obligations retain their status. This task authorizes
no Company scene, runtime/layout/token change, artwork/icons, gameplay, saves/content,
Application/Domain changes, removal of map/list or internal Main, or universal UI framework.

## Documentation verification — 2026-10-06

- Inspected latest remote main with `git ls-remote`; it matches the local baseline
  recorded above. Read the requested governance, foundation, kit, Stage 3/3.1 and
  sponsor packages plus actual token/theme/component/map/sponsor implementations.
- Scope: two new specifications and twelve updated Markdown documents. All 109
  local Markdown targets and four heading fragments resolve; code fences balance
  and `git diff --check` passes. Blueprint has eight regions with all ten requested
  fields; review retains A–J and all fourteen original gate rows, adding ten identity
  questions and eight composition fields.
- SHA-256 comparison against the pre-task working tree: 892 of 904 pre-existing
  tracked files byte-unchanged; the only twelve changed files are intended docs.
  Runtime, gameplay, Application/Domain, content, saves, tokens, tests and assets
  are unchanged. No runtime tests were rerun for documentation quality.
- DECISION_LOG.md and STAGE5_VISUAL_UX_BRIEF.txt remain byte-identical to the
  pre-task files and unchanged against Git HEAD after checkout line-ending
  normalization. Brief SHA-256 remains
  `d361584431b805280d2daea02b9a3ba29f7005ab715912b33613d3ee8f98c6ee`.
- Stage 3/3.1 and Stage 4 evidence is unchanged. Only the recommendation receives
  an appended current-status note; all original recommendation text is retained.
  The pre-existing uncommitted governance dashboard-prohibition addition is
  preserved separately from this correction's commit.
- Normative traceability is recorded in C1–C9 above and S1–S7 in the blueprint;
  new composition requirements come from the explicit Director clarification,
  while unimplemented integration and visual choices remain labelled as gaps/draft.
  No rendered-screen, usability or human visual acceptance is claimed.

Recommendation: **READY FOR STAGE 5 VISUAL EXPLORATION**. Stop before implementation.
