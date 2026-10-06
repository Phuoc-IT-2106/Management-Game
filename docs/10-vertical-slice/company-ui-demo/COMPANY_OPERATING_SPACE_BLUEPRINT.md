# Company Operating Space — Stage 5 composition blueprint

Blueprint COS-05 v1. Status: **DRAFT FOR DIRECTOR VISUAL REVIEW**.
Date: 2026-10-06 (Asia/Saigon). Specification only; no implementation or visual
acceptance. Review owner: Project Director, with UX/UI and technical review.
Authority: DEC-023, [Stage 5 governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md)
and the active [Director composition clarification](../../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md).

## Purpose, boundary and source register

Player goal: operate the company by understanding what needs attention, where it
belongs, and the commitments/consequences of entering a decision. Primary question:
“Where does my company need attention now, and what will acting affect?” Type:
Company / portfolio space, transitioning to contextual decision/document workspaces.
Company is the root; scopes/functions are lenses, not independent gameplay owners.

All region requirements below apply the Director's eight requested semantic roles
and C1–C9 contract. Reflow/disclosure descriptions are draft design proposals, not
measured usability or approved pixels. The register identifies actual current
data/interaction support separately from future presentation integration.

| Ref | Authoritative source and inspected support |
| --- | --- |
| S1 | [DEC-023](../../01-governance/DECISION_LOG.md#dec-023--company-first-ux-architecture), [information architecture](../ui-ux-foundation/INFORMATION_ARCHITECTURE.md), [module landscape](../ui-ux-foundation/MODULE_LANDSCAPE.md): company → Competitive Portfolio → Esports → Development Discipline → Primary Team; shared functions separate; current Company, Business & Finance, Competitive Portfolio, Talent & Performance, Affairs & Time lenses |
| S2 | [Application sponsor projection](../../../src/ManagementGame.Application/SponsorWorkspace.cs): ISponsorSession.ObserveSponsors / SponsorSnapshot: CampaignId, CompanyId, CompanyName, Revision, Day, NextCheckpoint, Cash, CommittedForecast, Load, Capacity, Reputation, Offers, Agreements. SponsorTerms supplies actual eligibility, dates, load and payment schedule; SponsorPayment supplies Id/DueDay/Amount/Remaining |
| S3 | [Application contracts](../../../src/ManagementGame.Application/Contracts.cs), [Observations.Build](../../../src/ManagementGame.Application/Observations.cs): IGameSession.Observe / Situation: Revision, Day, Company, Status, Finished, Cash/Forecast, Load/Capacity, Coach/Delegation/Ceiling, NextFixtureId/NextMatchDay, observed opponent evidence, CommittedPlan/Recommendation/RecommendationReason, People/Candidates, Bills, Results, Review, PendingConsequences. Company/World own facts; Application exposes actor-safe projections |
| S4 | [MapModel](../../../game/Client/UI/OperatingMap/MapModel.cs), [OperatingMap](../../../game/Client/UI/OperatingMap/OperatingMap.cs), [Stage 3.1 evidence](../company-operating-map/refinement/COMPARISON_MATRIX.md): shared MapSnapshot/MapNavigation, ID selection, ancestry, session/revision and MapReturn scope/matter/mode/focus/scroll; prototype bounds eight scopes/four matters, not campaign-scale qualification |
| S5 | [SponsorPresentation.Company / SponsorPresenter](../../../game/Client/UI/SponsorWorkspace/SponsorPresenter.cs), [CompanyHost](../../../game/Client/UI/SponsorWorkspace/SponsorCompanyHost.cs), [workspace view](../../../game/Client/UI/SponsorWorkspace/SponsorWorkspaceView.cs): live list-default company projection includes up to three offers and next outstanding receipt; Business & Finance/Affairs functions; fixed sponsor decision transition and refresh/return |
| S6 | [Interaction contract](../ui-ux-foundation/INTERACTION_MODEL.md), [priority](../ui-ux-foundation/INFORMATION_PRIORITY.md), [sponsor interaction](../sponsor-workspace/INTERACTION_SPEC.md), [Stage 4.1 evidence](../sponsor-workspace/acceptance/README.md): chronology, blocking versus attention, known/estimated/unknown, pending/stale/review/exact return; engineering accepted, human review pending |
| S7 | [Stage 2 component catalog](../ui-kit/COMPONENT_CATALOG.md), [tokens](../ui-kit/TOKEN_CATALOG.md), [theme](../ui-kit/THEME_IMPLEMENTATION.md), [branding](../ui-ux-foundation/PLAYER_BRANDING_RULES.md): semantic primitives and engineering-neutral-v1 provisional values; NavigationContext semantics do not mandate its original large header |

The current live host is a sponsor integration, not a complete company operating
space. Stage 3 fixture preparation/talent/obligation matters are not live evidence.
S3 provides gameplay data for those domains; the new shell still needs reviewed
adapters, supported destinations and matter identity/classification before exposing
their new routes. Do not populate this gap with fixture-only or invented situations.

S3 has no CompanyId field: retain S2 campaign/company identity with the current
session and validate matching revision/day when composing observations. No mixed
revision financial truth. Presentation scope IDs are stable bounded navigation keys,
not newly invented Domain divisions. Full player-authored identity fields and
persistence remain a separate migration; current live identity is ID/name with
neutral/text fallback and the development-fixture marker.

## Semantic composition

Persistent company/time context anchors a meaningful central operating space.
Supported function/portfolio access and a structured hierarchy orient the player;
bounded situations and question-led instruments explain current work and time.
These are semantic relationships, not a required three-column grid. No dimensions,
final typography, icons, art assets or fixed panel count are approved here.

### 1. Persistent company / time context

| Field | Draft contract |
| --- | --- |
| Player question | Which company and scope am I operating, and when? |
| Authoritative data source | S2 CompanyId/CompanyName, CampaignId, Revision, Day/NextCheckpoint; S4 selected path; S3 Status only for supported material attention |
| Supported current content | Actual observed company name; development marker; current simulation day and known next checkpoint; selected company/function/scope path. No invented real-world calendar date |
| Interaction | Company/path selection by stable ID; keyboard access to full identity/path; no command on selection |
| Existing components | NavigationContext semantics in compact composition; EntityLabel, SemanticText, TimeMarker, StatusIndicator; shared brand fallback |
| Information priority | Primary identity, time, selected scope and actual stop/material attention; secondary full name/path if compacted; no automatic resources strip |
| 720p behavior | Retain identity, day/checkpoint and selected context; wrap or accessible full-label inspection for long names without tooltip-only meaning; avoid oversized header |
| 1080p behavior | Reveal useful full path and attention context; no permanent KPI bar added with width |
| Explicitly forbidden | Hard-coded company, fabricated identity fields/date, technical IDs as product identity, cash/audience wall, context lost during decision |
| Visually unresolved | Compact hierarchy, full-name disclosure and attention placement using shared roles |

### 2. Supported function / portfolio access

| Field | Draft contract |
| --- | --- |
| Player question | Which supported company capability or owned area addresses this matter? |
| Authoritative data source | S1 exposure boundaries; S2 sponsor/finance; S3 competition, talent and affairs data; S5 existing live sponsor destination |
| Supported current content | Company context, Business & Finance, Competitive Portfolio, Talent & Performance, Affairs & Time. Broader gameplay currently exists through internal Main; new-shell routes beyond sponsor require reviewed integration |
| Interaction | Select lens while retaining company/scope; direct entry only to an implemented supported destination. Do not pretend a named lens has a finished workspace |
| Existing components | EntityLabel, SectionHeader, NavigationContext semantics; canonical native navigation controls |
| Information priority | Primary current lens/decision route; secondary other supported access; no equal-weight module gallery |
| 720p behavior | Compact or reflow supported access with visible labels and keyboard reachability; preserve current lens; no horizontal shell scroll |
| 1080p behavior | Show clearer relation between function and owned scope, not more modules |
| Explicitly forbidden | Fake playable/locked future entries, SaaS sidebar plus content dashboard as mental model, duplicate owners, permanent tab per architectural seam |
| Visually unresolved | Access placement, grouping and current-lens emphasis; destination handoff design beyond sponsor |

### 3. Structured ownership navigator

| Field | Draft contract |
| --- | --- |
| Player question | What does the company own, and exactly which scope is selected? |
| Authoritative data source | S1 hierarchy; S4 MapScope.Id/ParentId, Path, shared selection; S5 current bounded live projection |
| Supported current content | Player Company → Competitive Portfolio → Esports → Development Discipline → Primary Team; supported shared functions in their own group, never beneath the team |
| Interaction | Deterministic keyboard and pointer selection; compressed single-child path retains all semantic IDs/ancestry; same matter/selection as central visualization; restore focus/scroll by stable origin |
| Existing components | EntityLabel/EntityRow and compact NavigationContext semantics; reuse MapNavigation contract, not a new tree framework |
| Information priority | Primary selected scope/path and reliable navigation access; supporting ownership reference, not automatic dominant center |
| 720p behavior | Compact navigator or clearly labelled keyboard-reachable disclosure; selected scope stays visible; expanded list may scroll vertically. No forced giant-list replacement for operating center |
| 1080p behavior | May remain beside operating center with more ancestry visible; extra width does not promote it to visual identity |
| Explicitly forbidden | Spatial-only navigation, independent list state, row-index/name keys, whole-screen admin tree or copying internal technical hierarchy into product labels |
| Visually unresolved | Persistent versus disclosed representation, width/emphasis and focus order across disclosure |

### 4. Central company operating visualization

| Field | Draft contract |
| --- | --- |
| Player question | How do my owned business and shared capabilities connect, and where does current work affect the company? |
| Authoritative data source | S1 structure, S4 Scopes/Links/Situations and selected IDs; S5 live sponsorship support/constraint relationships; future S3 adapters only with actor-safe, revision-consistent fields |
| Supported current content | Company root, sole operating branch, real finance support/constraint on team preparation, current sponsor matters/receipt and checkpoint context. Talent/competition state may enter only through reviewed S3 integration |
| Interaction | Select/inspect scope, labelled relationship or attached matter; resolve to same structured selection and contextual destination. Every interactive region has a real target and text/list equivalent |
| Existing components | EntityLabel, SemanticText, StatusIndicator, TimeMarker, ConfidenceIndicator; bounded native schematic composition over shared navigation. No generic graph component required |
| Information priority | Primary visual/orientation anchor: company, selected scope, current material situation and consequence connection. Secondary support relationships; tertiary detailed evidence |
| 720p behavior | Keep a legible schematic or equivalent meaningful company center; simplify secondary relationships and disclose details; retain material situation/route. Do not substitute a giant tree or require pan/drag to reach decisions |
| 1080p behavior | Expose simultaneous relationships, actual operating state and consequence evidence where useful, rather than stretching nodes or filling space |
| Explicitly forbidden | Decorative HQ wallpaper, fake 3D traversal/city-builder, all-entity graph, invented divisions, facts available only visually, hidden simulation inputs, node per person |
| Visually unresolved | Schematic form, central emphasis, containment/support grammar, selection/state treatment and relationship density. Illustrated campus is optional later review, never a prerequisite |

### 5. Bounded active situations / strategic matters

| Field | Draft contract |
| --- | --- |
| Player question | Why should I act now, where, by when, and what happens if I do or wait? |
| Authoritative data source | S2 offer deadline/eligibility/terms, agreements/payments; S5 live matter IDs; S3 fixture/plan, candidates/people, Bills and Results for later reviewed adapters; S6 priority/class rules |
| Supported current content | Live sponsor offers, signed commitments and next outstanding sponsor receipt. Existing preparation, roster/talent, financial obligations and result/checkpoint content may appear when projected from actual S3 records with valid targets, not copied from MapFixtures |
| Interaction | Select matter/affected scope, inspect evidence, enter supported decision; reconcile one cause across center/affairs rather than duplicate notifications |
| Existing components | AlertItem semantics, EntityLabel, TimeMarker, ConfidenceIndicator, StatusIndicator; existing panel wrappers do not dictate card stacks |
| Information priority | Primary material matter: why now, scope, deadline/horizon, consequence and route; priority and class separate; relevant uncertainty visible. Secondary remaining choices; historical items on demand |
| 720p behavior | Keep current material matter and route readable in bounded region; other matters disclose/scroll vertically without hiding primary uncertainty |
| 1080p behavior | Reveal more relevant consequence/evidence and bounded remaining matters, not an unbounded activity feed |
| Explicitly forbidden | Invented urgency/deadlines/risk score, generic notifications, UI priority causing simulation stops, fake action on receipt, empty-state filler |
| Visually unresolved | Matter emphasis and bound, attachment versus side-region balance, grouping and refreshed-attention treatment |

### 6. Contextual management instruments

| Field | Draft contract |
| --- | --- |
| Player question | What current constraint or evidence changes the decision I am approaching? |
| Authoritative data source | S2 Cash/CommittedForecast/Load/Capacity/Agreements; S3 Bills, fixture/plan/recommendation, roster/coach and dated results; same session/revision as current context |
| Supported current content | Finance: what is scheduled versus received, and what is due? Competition: what is the next checkpoint and current preparation? Talent: which roster/coach context affects it? Affairs: when does this commitment matter? Only fields relevant to selection are shown; broader new-shell integration remains pending |
| Interaction | Projection, orientation, shortcut, consequence preview or decision entry to real source context. Summary never independently recalculates money, eligibility or outcomes |
| Existing components | ResourceValue, TimeMarker, ConfidenceIndicator, DocumentView, ComparisonView, EntityLabel, SectionHeader; no universal dashboard-widget framework |
| Information priority | Secondary by default; primary when evidence is necessary to the current choice; tertiary detailed records; raw technical receipts remain internal |
| 720p behavior | Compress/collapse secondary instruments into contextual access; move decision-critical terms into the decision workspace with visible uncertainty; no horizontal shell scroll |
| 1080p behavior | Add useful matched evidence, relationships and supported history; no invented historical series or detached metrics |
| Explicitly forbidden | Equal-card KPI grid, filler metrics, implied guaranteed forecast, unsigned revenue as cash, calculated success percentages without source, chart without player question |
| Visually unresolved | Which instruments deserve simultaneous exposure, evidence grouping and any smallest central component refinement |

### 7. Affairs / time access

| Field | Draft contract |
| --- | --- |
| Player question | What is due next, what changed, and why did progression stop? |
| Authoritative data source | S2 Day/NextCheckpoint, offer deadlines/payment dates; S3 NextFixtureId/NextMatchDay, Bills.DueDay/MissedDay, Results.Day, Review/PendingConsequences and actual AdvanceDecision response; S6 chronology/stop rules |
| Supported current content | Current day/checkpoint, sponsor deadlines/receipts; existing financial obligations, candidate deadlines and results when integrated. Review/PendingConsequences include text, not a complete structured affairs/event API |
| Interaction | Discoverable affairs access, chronological inspection and same-ID matter entry; advance only through existing Request + AdvanceDecision where the supported control is integrated; selection/dismissal never advances or clears a blocker |
| Existing components | TimeMarker, TimelineEvent, AlertItem, EntityLabel and ValidationMessage; no full calendar/scheduler component |
| Information priority | Primary current time, known next checkpoint and actual stop reason; secondary upcoming relevant matters; historical/resolved records on request |
| 720p behavior | Affairs access and next relevant timing stay discoverable even collapsed; expand bounded vertical chronology without losing company/selected context/material route |
| 1080p behavior | Reveal useful upcoming/realized consequences and causal source links; no calendar invented to occupy width |
| Explicitly forbidden | UI-authored time stops, arbitrary-date travel, unknown date treated as zero, acknowledgement undoing obligations, fabricated structured IDs parsed from prose |
| Visually unresolved | Drawer/strip/contextual region and available chronology depth; structured matter/stop-reason gaps need a reviewed projection plan before build |

### 8. Transition into contextual decision workspace

| Field | Draft contract |
| --- | --- |
| Player question | Is this commitment worthwhile for my company now, and how do I return to its origin? |
| Authoritative data source | S2 current SponsorSnapshot/SponsorTerms, S4 MapReturn and session/revision validation, S5 SponsorPresenter; S6 sponsor interaction/Stage 4.1 evidence |
| Supported current content | Fixed sponsor acceptance: company → Business & Finance → originating offer/matter → commercial document + company trade-off/evidence → review → commitment → refreshed consequence → exact valid return |
| Interaction | Operating space yields its area to decision workspace; local Review/Confirm; existing Request(id, reviewedRevision, SponsorDecision(offerId)); Pending prevents duplicates; accepted/rejected refresh; stale requires fresh review. Back/Escape leaves confirmation first, then returns; absent origin falls back with explanation |
| Existing components | DocumentView, ComparisonView, ResourceValue, TimeMarker, ConfidenceIndicator, CommitmentReview, ValidationMessage, EntityLabel and compact NavigationContext semantics |
| Information priority | Primary terms, deadline, load, scheduled income, uncertainty, deliberate commit/back; secondary company evidence; tertiary related records. Company/scope/matter/time remain clear |
| 720p behavior | Do not keep full-size operating space visible; reserve clear terms/evidence and review/back access; supporting evidence may scroll vertically, no horizontal scroll |
| 1080p behavior | Use additional space for terms, comparison and causal evidence, not unrelated home instruments |
| Explicitly forbidden | Universal layout for all screens, acceptance on selection, invented negotiation/termination, optimistic cash, stale auto-retry, lost return context, direct Domain mutation |
| Visually unresolved | Visual transition and compact context treatment. Existing sponsor layout is a verified engineering exemplar, not final Stage 5 visual approval |

## Shared states, lifecycle and unresolved integration

Entry is company context or a supported scope/matter; exit is a contextual workspace
or exact valid origin. Company-level navigation has no gameplay draft or commitment.
The only decision exemplar specified here is existing SponsorDecision. Other current
typed decisions (PreparationDecision, CoachDecision, SigningDecision, ReleaseDecision,
AdvanceDecision) remain unchanged and require their own reviewed presentation paths.

Empty/quiet retains real ownership, identity and time without fake alerts. Loading
must not expose actionable stale data. Error retains valid context with explanation;
unavailable/removed targets disable affected actions and fall back to nearest valid
scope. Same-session old revisions reject; new/load session invalidates old origins
and drafts. Exact return preserves valid company/scope/matter, mode, focus and scroll;
no promise to restore a removed target. Focus remains distinct from selection.

Keyboard paths must cover all facts/routes without spatial understanding or drag;
disclosure restores focus, and scrolling follows the focused reading region.
Known/estimated/unknown need words as well as system roles; no critical hover-only
facts. Long Unicode names, text expansion, brand contrast and reduced motion remain
review cases. Shared tokens/roles preserve contrast; no local component styling.

Before later BUILD, review the minimal integration plan for S3-based matter IDs,
scope/cause mapping, priority/class and unavailable destinations. Do not manufacture
a blocking classification from presentation urgency or free-form review strings.
Do not reinterpret eight-scope/four-matter prototype bounds as a tested full-company
adapter. Structured affairs, full identity and broader exact-return integration
remain gaps, not newly authorized gameplay systems. Retain internal Main and the
existing map/list and sponsor paths throughout any later bounded migration.

## Review record and exploration exit

PRIMARY VISUAL ANCHOR: meaningful company operating representation (draft form open).
STRUCTURED NAVIGATION ROLE: reliable ownership/reference and equivalent keyboard
route, supporting rather than defining the whole composition.
OPERATING-SPACE ROLE: connect actual company scopes, state, situations and consequences.
INSTRUMENT TEST: each instrument answers the question in region 6 and identifies
source/revision; omit it if that value is absent.
ADMIN-UI TEST / DASHBOARD TEST: both require explicit negative findings with evidence.
DECORATION TEST: removing a visual must remove identified gameplay/navigation/context
meaning, not merely spectacle. CURRENT-SCOPE TEST: each displayed entity/module/mechanic
maps to S1–S5; integration gaps cannot be passed off as playable content.

Complete the [Stage 5 screen review](../ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md)
with all ten management-game identity questions, A–J and original 14 criteria.
All visual/usability fields are **PENDING**. Later exploration must demonstrate:

- 1280×720 retains company/time/selected scope, meaningful operating center, material
  matter/decision route and affairs access without primary-shell horizontal scrolling;
- 1920×1080 adds useful relationships/evidence/consequences rather than stretched panels;
- structured and visual routes select identical IDs and return context; no fact is visual-only;
- sponsor commitment still requires review, shows scheduled versus received money,
  refreshes consequences and restores valid origin without altering commands;
- quiet, missing target, stale revision, long names, absent art and uncertainty remain clear;
- both dashboard and admin/debug failure modes are rejected without hiding useful instruments.

Open Director decisions: schematic form, dominant visual hierarchy, navigator
disclosure, instrument/matter density, relationship grammar, 720p reflow, richer
1080p composition, and whether a smallest central theme/component refinement is
needed. Final palette/font/radius and optional illustrated enrichment remain separate
reviews. No reference names/logos/values/sponsors/leagues/sports/future divisions,
negotiation mechanics, modules or art assets may be copied into this blueprint.

This specification creates no captures or usability proof, changes no runtime,
and does not discharge Stage 4 human review or any DV/QG obligation. Stop before BUILD.
