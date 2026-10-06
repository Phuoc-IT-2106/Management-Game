# COS-05 exploration — repository and product reconstruction

Status: **FACT / ANALYSIS — exploration baseline, no acceptance**.
Date: 2026-10-06 (Asia/Saigon). Sequence: PLAN → ANALYZE → EXPLORE.
Task source: Director attachment `f727ea2c-240d-48c3-90dd-db22c6913fc4/Pasted text.txt`.
Read next: [traceability](COS05_TRACEABILITY.md), then [two composition directions and recommendation](COS05_VISUAL_EXPLORATION.md).

## Repository baseline established before alternatives

| Item | Inspected fact |
| --- | --- |
| Branch | `stage5-sponsor-instruments` |
| HEAD | `b38da8f16fc2b5e60e3c2263caeb91b6332cdf5f` |
| Remote main at investigation | `1ea825ad6070d76f4f8d98ce227507fe57897e2b`, verified using `git ls-remote origin refs/heads/main` |
| Pre-task working tree | Dirty: only `docs/01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md`, a pre-existing appended dashboard-prohibition section; preserved |
| Agent guidance discovery | No AGENTS.md or CLAUDE.md found in repository file discovery; applicable session instructions retained |
| Existing exploration owner | `company-ui-demo/` contains COS-05 v1 blueprint only; no existing visual exploration document found |
| Production composition gate | No accepted Stage 5 Company composition/scene or final art direction found in current state, decisions, blueprint, stage packages or latest commits |

Relevant commits, newest first:

| Commit | Significance and authority limit |
| --- | --- |
| `b38da8f` | Sponsor DayTrack, daily-load/schedule projection, evidence/action layout; implementation candidate, not Company visual approval |
| `d96c31b` | Sponsor consequence previews, distinct entity markers, numeric amount treatment and selected edge; implementation candidate |
| `1ea825a` | Active composition clarification and draft COS-05; navigation priority does not dictate visual dominance |
| `1241bc1` | Active Stage 5 50-rule visual/UX governance |
| `55d4ce4` / `043ad9b` | Stage 4.1 engineering acceptance report / tested source, with human review still pending |
| `7ad0501` / `0e9bbee` | Live contextual sponsor implementation / published playable slice |
| `a381d8f` | Stage 3.1 comparison and LIST-FIRST / MAP-OPTIONAL recommendation, historical evidence |

## Source hierarchy and current phase

**DECISION:** [DEC-022/023](../../01-governance/DECISION_LOG.md) and
[PROJECT_STATE](../../00-project/PROJECT_STATE.md) establish Phase 5 Vertical Slice,
accepted company-first architecture and bounded gameplay. Current explicit Director
instructions and [Stage 5 governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md)
with its later [composition clarification](../../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md)
govern this exploration. COS-05 remains [DRAFT FOR DIRECTOR VISUAL REVIEW](COMPANY_OPERATING_SPACE_BLUEPRINT.md).

**FACT:** [Stage 4.1](../sponsor-workspace/acceptance/README.md) records engineering
acceptance complete at its tested source. The [human review form](../sponsor-workspace/acceptance/DIRECTOR_REVIEW_RECORD.md)
is blank. The [current sponsor README](../sponsor-workspace/README.md) calls the newer
instrument work a candidate and retains human review pending. Neither its code nor
its screenshots promote that work, COS-05 or final art to accepted status. Phase 5
BUILD authorization in DEC-022 does not waive the specific Company visual gate or
this task's investigation-only boundary.

| Discrepancy | Source hierarchy / disposition |
| --- | --- |
| Root README still says Phase 4; charter ends at Phase 0 | Stale summaries. Accepted DEC-022 and current PROJECT_STATE explicitly close Phase 4/open Phase 5. Preserve and report; no unresolved gameplay conflict |
| DOCUMENT_AUTHORITY describes an early proposed DEC-001 and stale Phase 0 state | Its approval/recency hierarchy remains useful; its source inventory predates accepted DEC-001–023. Do not use that inventory to revoke later recorded decisions |
| Foundation files retain Stage 1 PROPOSAL labels / migration table says Stage 1 | Foundation README and DEC-023 accept architecture, while examples, future modules and final appearance remain proposals; later migration note clarifies current application |
| Old kit/read-model documents and COS-05 v1 omit newer sponsor fields/DayTrack; theme documentation still describes `[Selected]` | Implementation and updated component catalog establish branch capability; selection now uses an edge marker. Existing blueprint is a draft baseline, not an exhaustive latest field inventory; this exploration records the delta |
| Historical Stage 3 map choice and Stage 3.1 list default versus visual operating space | Director clarification explicitly resolves application: retain shared semantic state and accessible list, allow dominant operating visualization. No historical finding changes |
| Stage 4.1 acceptance numbers versus newer branch code | Acceptance belongs to `043ad9b`; new local logs/candidates are additional evidence, not an exact-HEAD acceptance certificate |
| Uncommitted governance addition | Read as existing local instruction context; committed clarification already enforces the same prohibition. Conclusions do not depend on publishing this edit |

No material unresolved conflict between accepted gameplay decisions prevents
documentation exploration. The differences above are disclosed source-age and
implementation/approval distinctions. No new DEC or acceptance is recorded.

## Sources reviewed, grouped by responsibility

| Group | Sources and what they establish |
| --- | --- |
| Canonical governance | [Document authority](../../01-governance/DOCUMENT_AUTHORITY.md), [decision log](../../01-governance/DECISION_LOG.md), [state](../../00-project/PROJECT_STATE.md), active [governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md), preserved [brief](../../01-governance/STAGE5_VISUAL_UX_BRIEF.txt), [composition direction](../../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md): decision/approval boundaries and rejection criteria |
| Product/system specifications | [Vision](../../00-project/GAME_VISION.md), [charter](../../00-project/PROJECT_CHARTER.md), [slice scope](../VERTICAL_SLICE_SCOPE.md), [architecture mapping](../ARCHITECTURE_MAPPING.md), [state boundary](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [simulation cycle](../../06-core-loop-simulation/SIMULATION_CYCLE.md), [resource model](../../06-core-loop-simulation/RESOURCE_MODEL.md): approved concepts versus broader proposals; current implementation decides what is playable |
| UX/UI specifications | [COS-05](COMPANY_OPERATING_SPACE_BLUEPRINT.md), [foundation index](../ui-ux-foundation/README.md), [company direction](../ui-ux-foundation/COMPANY_UX_DIRECTION.md), [information architecture](../ui-ux-foundation/INFORMATION_ARCHITECTURE.md), [interaction model](../ui-ux-foundation/INTERACTION_MODEL.md), [priority](../ui-ux-foundation/INFORMATION_PRIORITY.md), [module landscape](../ui-ux-foundation/MODULE_LANDSCAPE.md), [art policy](../ui-ux-foundation/ART_PRODUCTION_POLICY.md), [migration](../ui-ux-foundation/PHASE5_UX_MIGRATION_PLAN.md), [review template](../ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md): semantic composition and review requirements |
| Kit and workspace contracts | [Components](../ui-kit/COMPONENT_CATALOG.md), [tokens](../ui-kit/TOKEN_CATALOG.md), [theme](../ui-kit/THEME_IMPLEMENTATION.md), sponsor [blueprint](../sponsor-workspace/SCREEN_BLUEPRINT.md), [interaction](../sponsor-workspace/INTERACTION_SPEC.md), [reuse](../sponsor-workspace/COMPONENT_REUSE.md), [current refinement note](../sponsor-workspace/README.md): reuse boundaries and exact fixed-offer flow |
| Implementation | [Main](../../../game/Client/Main.cs), [MapModel](../../../game/Client/UI/OperatingMap/MapModel.cs), [OperatingMap](../../../game/Client/UI/OperatingMap/OperatingMap.cs), [CompanyHost](../../../game/Client/UI/SponsorWorkspace/SponsorCompanyHost.cs), [SponsorPresenter](../../../game/Client/UI/SponsorWorkspace/SponsorPresenter.cs), [workspace view](../../../game/Client/UI/SponsorWorkspace/SponsorWorkspaceView.cs), [DayTrack](../../../game/Client/UI/Components/DayTrack.cs), [presentation records](../../../game/Client/UI/Contracts/Presentation.cs), [UiTokens](../../../game/Client/UI/Tokens/UiTokens.cs), [UiTheme](../../../game/Client/UI/Theme/UiTheme.cs), [primitives](../../../game/Client/UI/Components/Primitives.cs), [structures](../../../game/Client/UI/Components/Structures.cs): actual controls, formatting, navigation and current instrument |
| Gameplay implementation | [Domain model](../../../src/ManagementGame.Domain/Model.cs), [Simulation](../../../src/ManagementGame.Domain/Simulation.cs), [Finance](../../../src/ManagementGame.Domain/Finance.cs), [Session](../../../src/ManagementGame.Application/Session.cs), [contracts](../../../src/ManagementGame.Application/Contracts.cs), [observations](../../../src/ManagementGame.Application/Observations.cs), [sponsor projection](../../../src/ManagementGame.Application/SponsorWorkspace.cs): owners, legal actions, information boundaries and preview semantics |
| Verification/evidence | Stage 3.1 [comparison](../company-operating-map/refinement/COMPARISON_MATRIX.md), [720p review](../company-operating-map/refinement/720P_REVIEW.md), [recommendation](../company-operating-map/refinement/DIRECTOR_RECOMMENDATION.md); Stage 4.1 [verification](../sponsor-workspace/acceptance/VERIFICATION.md) and blank human review; current [sponsor tests](../../../tests/SponsorWorkspace/Program.cs), [kit tests](../../../tests/UiKit/Program.cs), [native sponsor harness](../../../game/Client/UI/SponsorWorkspace/SponsorVerification.cs); local candidate manifests/logs noted below |

## Reconstructed product

**DECISION:** The game is company-first, beginning with professional esports. The
player acts as CEO/executive with selective direct management. Teams are owned
operating units; winning is valuable for company outcomes. Executive fantasy does
not imply implemented budgets, acquisitions, departments or expansion commands.

**SUPPORTED NOW — FACT:** one company, one development discipline/primary team,
roster/candidates, one coach, preparation allocation/posture/lineup, manual/recommend/
autonomous coach authority with risk ceiling, fixed signing/release and sponsor
acceptance, cash/obligations/receipts, competition/results, reputation/audience effects,
checkpoint advance, save/load. Current six starting players, two rivals, four matches,
28-day horizon and CU denomination are development fixtures, not final content canon.

**FUTURE / ARCHITECTURAL ONLY:** other sports/businesses, multiple teams/disciplines,
deep staff hierarchy, facilities, M&A, budgets/capital allocation, full calendar,
broad scouting, financing UI, relationship systems and custom negotiation. Do not
turn broader resource/specification examples into menus or commands.

**DECISION:** Observe → Prioritize → Decide → Commit → Delegate / Intervene →
Advance Time → Resolve → Review → Adapt (DEC-013). COS supports observation,
prioritization and return to consequences. Contextual workspaces handle commitment.
Time advancement invokes Application; ordinary orientation never changes state.

**FACT:** Company owns people/employment, sponsor agreements, cash/items/settlements,
reputation/audience/information, preparation plan/work, coach authority, recovery,
pending commercial effects and reviews. World owns chronology, schedule/results,
rivals, offers/candidates and meta. Application serializes typed requests, validates
revision/idempotency, builds actor-safe reads and stages Domain transitions.
Execution IDs/revision/receipts/seed are technical bookkeeping, not a third gameplay
authority. UI owns local selection/focus/scroll/drafts and formatted observations;
it must not own cash, eligibility, schedule, blockers, match results, hidden truth
or a second authoritative forecast.

**FACT:** Autonomous coach uses the same observed recommendation and decision
executor; no plan/low-confidence situations can escalate. Manual control has no
hidden bonus. A coach is a current responsibility, not an implemented department tree.

| Real pressure | Permitted meaning on Company screen | Boundary |
| --- | --- | --- |
| Cash, due bills, arrears, scheduled receipts | Can a commitment be supported by current liquidity and dated obligations? | Cash is not profit; accepting sponsor does not pay immediately; no arbitrary-day cash forecast exists |
| Capacity and load | Sponsor/roster commitments constrain future preparation conversion when load exceeds capacity | Not action points; no exact future win penalty; completed preparation retained |
| Reputation/audience | Observed company standing, eligibility and later result consequences when relevant | No automatic cash conversion or mandatory KPI bar |
| Information quality | Supplied opponent/candidate estimates, confidence and unknown outcomes | Raw Company.Information is not exposed as a general player meter; no hidden rival strength/probability/variance |
| Time/opportunity | Offer/candidate deadlines, next match, payment dates, supported advance/stop messages | NextCheckpoint currently means next fixture day, not every possible interruption |
| Flexibility/recovery | Limited sponsor slot, release cost/depth loss, remaining obligations and recovery status | No invented flexibility/health score or termination/loan command |

## What the existing UI establishes

| Classification | Actual evidence / reuse implication |
| --- | --- |
| Gameplay-semantic requirements | Stable IDs, Company context, actor-safe known/estimated/unknown, deliberate commitments, current revision, pending latch/stale rejection, owner-refreshed consequences and exact valid return |
| Reusable infrastructure | UiTokens/UiTheme/BrandResolver, semantic components, MapSnapshot/MapNavigation/MapReturn, native focus and bounded scrolling, SponsorPresenter/CompanyHost lifecycle, DayTrack data/text/drawing |
| Provisional appearance | engineering-neutral-v1 remains provisional despite branch changes: dark palette, 4px radius, fonts, panels, selection edge and markers are implementation choices, not approved art |
| Technical fixtures | DEV_ORG_001 marker, MapFixtures scope keys and scenario matters, constrained scope/matter counts, fixed horizon/denomination |
| Historical experiments | Stage 3/3.1 map/list shells and Stage 4 captures test bounded navigation/workspaces; they do not establish final Company composition or human preference |

Main still uses tabs and supplies existing gameplay/save/load/advance. CompanyHost
opens a list-default OperatingMap on the same session and swaps to the sponsor
workspace. Only Sponsor category invokes its live presenter; a receipt opens
read-only campaign evidence. Other prototype matters are fixtures. MapSnapshot is
bounded to eight scopes/four situations. It cannot simply be filled with every
live bill/person/result. MapReturn carries company/scope/matter/mode/focus/scroll;
session and revision are managed through the host/snapshot, not invented extra
MapReturn fields.

Current sponsor mechanics are fixed offers. AcceptSponsor validates opportunity,
reputation/end/deadline and the active-slot limit, claims the offer, creates an
agreement/receivables and immediate load. Scheduled finance settlement follows
advance; actual wins may create next-day bonuses. No negotiation, counteroffer,
bidding, clauses, renewal, termination or relationship bar exists.

The branch adds LoadIfAccepted, ForecastIfAccepted and daily load arrays from a
pure authoritative preview. Daily projections hold today's roster/agreements fixed;
they are estimates, not forecasts of unknown decisions. CompetitionDays is an array
of dates without fixture IDs. DayTrack has no click/focus/command contract. Interactive
dates or full event IDs would require a reviewed mapping; never route by array index.

Sponsor payments expose amount, remaining balance and due day, but no actual
settlement timestamp. General Situation.Bills contains outstanding items, not a
complete paid history. Domain settlements exist but are not exposed by these reads.
Either direction may review refreshed cash/remaining receipts and dated competitive
results; it must not infer a paid date from a due date or promise a complete dated
financial history without a separately reviewed actor-safe projection.

## Evidence inspected and limits

Visually inspected the tracked Stage 3.1
[map](../company-operating-map/refinement/evidence/candidates/map-sponsor-1280x720.png)
and [list](../company-operating-map/refinement/evidence/candidates/list-sponsor-1280x720.png)
sponsor candidates. Both are visibly structure-plus-inspector experiments. Historical
geometry credits map's simultaneous relationships and lower height, list's fewer
Controls, and equal scripted task access. None is human comprehension evidence.

Also inspected local `artifacts/stage5-step2/candidates/review-1280x720.png` and
`review-1920x1080.png`, with matching JSON manifests. These are existing local
artifacts, not new captures or versioned acceptance assets. Both manifests record
`d96c31bda8c908e40f9a8977480b0f07e7f90621`, `Dirty=true`, source digest
`9affb689fc0a0f1d278943498a7dce2f894a76ebce90c9cfe2ce98a956517683`.
Do not relabel them clean `b38da8f` captures.

Observed: at 720p current terms/consequence text/actions are visible while the
DayTrack is below the initial evidence fold; at 1080p the day track and its text
explanation are visible. This supports keeping a concise critical consequence
outside a secondary instrument; it does not prove either proposed Company layout.

Read existing `artifacts/stage5-step2/checks/` logs: sponsor reports 57, kit pure
132/native 24, map pure 48/native 101, and Main/headless two-cycle success. Inspected
test source for preview purity, authoritative load/forecast/dates, hidden-input
exclusion, text equivalents, stale/pending/ID/return behavior. These are existing
run records; this exploration reruns no runtime test and grants no new pass.

## Documentation verification

Executed documentation checks across the three new records and the linked blueprint:

- 107 local link targets and one heading anchor resolve; wireframe fences balance,
  UTF-8 has no replacement characters, and no trailing whitespace was found.
- 24 Markdown tables have consistent column counts, excluding fenced wireframes.
  The initial table checker also read ASCII diagram borders as tables; after excluding
  code fences, the corrected check passed without a document change.
- All eight COS regions, both A–K evaluations, two wireframes and eight risk
  categories per direction, twenty comparison criteria and fourteen challenge
  questions are present. Source/semantic review accompanies these structural checks.
- `git diff --check` passes. Against the pre-edit SHA-256 baseline of 907 tracked
  files, only the blueprint's five-line cross-link changed; the other 906 files
  match byte-for-byte, including the existing dirty governance edit, acceptance
  records, historical evidence and all production code.

The delivery consists of three new documentation files and that blueprint link.
No runtime tests were rerun, no new captures were made, and no Domain/Application,
token, content, asset or acceptance-state change is part of this task. Existing test
results above remain historical evidence. Approval remains with the Director.
