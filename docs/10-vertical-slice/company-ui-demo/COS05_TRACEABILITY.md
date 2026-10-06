# COS-05 exploration — semantic traceability

Status: **ANALYSIS / PROPOSAL — no new read contract or route accepted**.
Baseline: `b38da8f16fc2b5e60e3c2263caeb91b6332cdf5f`, 2026-10-06 (Asia/Saigon).
Sources/authority: [repository reconstruction](COS05_SOURCE_REVIEW.md) and
[COS-05 v1](COMPANY_OPERATING_SPACE_BLUEPRINT.md). Applies identically to
[directions A and B](COS05_VISUAL_EXPLORATION.md).

## Source vocabulary and route status

- **S:** [SponsorSnapshot / SponsorTerms / SponsorAgreement / SponsorPayment](../../../src/ManagementGame.Application/SponsorWorkspace.cs), via ISponsorSession.ObserveSponsors.
- **G:** [Situation and its row records](../../../src/ManagementGame.Application/Contracts.cs), via IGameSession.Observe and [Observations.Build](../../../src/ManagementGame.Application/Observations.cs).
- **N:** [MapSnapshot / MapNavigation / MapReturn](../../../game/Client/UI/OperatingMap/MapModel.cs) and [SponsorPresentation.Company](../../../game/Client/UI/SponsorWorkspace/SponsorPresenter.cs).
- **K:** [implemented component contracts](../ui-kit/COMPONENT_CATALOG.md), including branch DayTrack.
- **LIVE:** current Company host route works. **INTERNAL:** gameplay/read screen exists in Main; Company destination/return integration is proposed. **READ:** inspect only, no command. These are authoring labels, never fake navigation entries in player UI.

All values use matching session/company/revision/day. G carries a company display
name but no CompanyId; S supplies CompanyId/CampaignId. A future combined presenter
must check their consistency, not join by company name. N scope IDs represent the
approved bounded hierarchy; they do not create new Domain business entities.

## Eight responsibilities

| COS region / gameplay purpose | Source state and entity/scope | Existing infrastructure and destination | Decision supported; information boundary | 720p requirement / permitted disclosure |
| --- | --- | --- | --- | --- |
| R1 Persistent context: know company, place and time | S CompanyId/CompanyName/CampaignId/Revision/Day/NextCheckpoint; N ScopeId/Path; G Status when relevant | Compact NavigationContext semantics, EntityLabel, TimeMarker, StatusIndicator; Company/path selection LIVE | Orient before choosing a matter. Day/identity known; absent next checkpoint is absent, not day zero. No raw revision in product header | Company, day/known checkpoint, selected scope and actual material stop/context remain. Full ancestry/name may disclose with keyboard-accessible text, not tooltip-only |
| R2 Supported function/portfolio access: choose responsibility | Approved Company, Business & Finance, Competitive Portfolio, Talent & Performance, Affairs & Time lenses; S finance/sponsor; G competition/people/results | EntityLabel/SectionHeader; sponsor LIVE, receipt READ; Main Finance, Team & people, Competition & coach, Review & results INTERNAL | Finance/roster/preparation/coach choices use existing typed requests in their workspaces. A lens label does not prove a new Company route is implemented | Current lens and meaningful decision route remain; secondary access compacts into labelled disclosure. No future module entries |
| R3 Structured hierarchy: precise ownership/reference | N Scopes.Id/ParentId, Path, ScopeId/SituationId; Company → Portfolio → Esports → Discipline → Team, separate shared functions | MapNavigation/EntityLabel/EntityRow, focus/scroll return; structured selection LIVE | Inspection only; same IDs and facts as visual center; functions support the team rather than being owned by it | Selected context and discoverable keyboard navigator remain. Full tree can collapse; do not replace operating center with a giant list |
| R4 Central operating representation: understand relationships/state | N scopes/links/current matters; S agreements/load/capacity/receipt state; G plan/coach/people/results when mapped | Native bounded composition with K labels/status/confidence, shared selection; sponsor LIVE, preparation/talent INTERNAL | Qualitative sponsor load → reduced future preparation conversion when overloaded is sourced. A link never computes a new effect or exposes win probability | Meaningful company center and selected relationship stay. Hide secondary edges/detail before losing ownership/function distinction; all facts have text equivalents |
| R5 Active matters: explain why now and route to choice | S offer ID/deadline/CanAccept/Availability; payment ID/due/remaining; G fixture, person/candidate, bill/result IDs; N matter mapping | AlertItem semantics, TimeMarker, ConfidenceIndicator; sponsor LIVE, receipt READ, broader matters INTERNAL pending mapping | Known terms/availability, estimated future evidence, unknown outcomes. Priority/class are distinct. Actual blocker authority remains Application; neither urgency nor absence of a plan alone invents an unconditional stop | Current material matter, scope, timing, consequence, uncertainty and action remain. Additional matters disclose/scroll; quiet has real ongoing context, no fake alert |
| R6 Instruments: answer a selected decision question | S Cash/CommittedForecast/Load/Capacity; eligible offer LoadIfAccepted/ForecastIfAccepted/LoadByDayIfAccepted; G Bills/People/Coach/plan/evidence | ResourceValue, ComparisonView, DocumentView, DayTrack, ConfidenceIndicator; sponsor evidence LIVE, other internal reads INTERNAL | Compare accept versus leave without committing; current facts known, seven-day forecast and fixed-roster daily load estimated; future wins/bonuses unknown. No arbitrary future cash curve | Critical trade-off summary remains; full chart, bill breakdown, roster details and extra evidence contextualize or move to workspace. No filler metrics |
| R7 Affairs/time: connect obligations, checkpoint and consequences | S Day/LastDay/NextCheckpoint/CompetitionDays, payment/deadline fields; G NextFixtureId/NextMatchDay, Bills.DueDay/MissedDay, Results.Day, Review/PendingConsequences, Response.Message | TimeMarker, TimelineEvent, DayTrack plus labelled entity controls; current affairs LIVE sponsor/receipt; AdvanceDecision and broader review INTERNAL | Select date/event for evidence only where real ID exists. Advance is existing Application request, never calendar drag or skip-to-date. Raw review strings are not an event API | Day/checkpoint, affairs access and actual stop message stay. Longer chronology/full horizon/history disclose; no primary-shell horizontal scrolling |
| R8 Decision transition: commit safely and return | S current terms/CanAccept; N origin; SponsorPresenter.Phase; Request/Response | CompanyHost → SponsorWorkspaceView, DocumentView/ComparisonView/CommitmentReview; LIVE fixed sponsor acceptance | Review → Confirm → Pending → Accepted/Rejected; refreshed owner state; no immediate Cash; stale fresh review. Back commits nothing; disappeared origin falls back to valid context | Full Company center may yield to workspace. Keep company/scope/matter/time, terms/critical uncertainty and deliberate review/back; retain valid ID/focus/scroll on return |

## Exact destination and identity audit

| Subject | Stable identity / present path | Supported action | Integration restriction |
| --- | --- | --- | --- |
| Player Company / owned branch | S.CompanyId; N approved scope constants and ParentId chain | Select Company/portfolio/Esports/discipline/team | Scope path is presentation, not fake divisions or separate rosters |
| Sponsor opportunity | SponsorTerms.OfferId; N `matter:` + offer ID; Business & Finance → live presenter | Request(id, reviewedRevision, SponsorDecision(offerId)) | OfferId and agreement ID are distinct; inspect/compare never accepts |
| Signed agreement / receipt | SponsorAgreement.Id / SponsorPayment.Id; receipt matter targets agreement | Read-only current campaign evidence in OperatingMap | No payment, collect-now, renewal or termination action; opening receipt is not sponsor acceptance |
| Preparation / competitive checkpoint | G.NextFixtureId, PlanView.FixtureId / Lineup person IDs | PreparationDecision, CoachDecision through Main | Company route, draft preservation and exact return not yet implemented; next date alone is not an ID |
| Person / candidate | PersonRow.Id / CandidateRow.Id, scoped to sole team and company talent function | SigningDecision / ReleaseDecision in Main; actor-safe inspection | No contract negotiation/development program; new-shell dossier is not already present |
| Financial obligation | BillRow.Id / Cause; amount, due/missed day | Inspect Main Finance; recover only through legal existing gameplay choices | No standalone pay/borrow/budget command; broader affair links need mapping |
| Settled money | S payment Id/Amount/Remaining/DueDay and refreshed Cash; G outstanding Bills | Inspect current cash and receipt balances | Neither read exposes actual settlement timestamps or full paid history. DueDay is not a paid date; do not bypass the read boundary to inspect Domain settlements |
| Result / pending effect | ResultRow.Id/Day known; Review and PendingConsequences are strings | Inspect Main Review & results and adapt existing decisions | Do not parse text into invented stable IDs or reveal Domain CompetitiveOutcome.Probability/Variance |
| Advance | Existing AdvanceDecision and current Request.Revision | Application advances to actual checkpoint or rejects with actual reason | Host currently requires returning to Main. No date-click advance; no UI scheduler |

## Causal chains permitted in either direction

**FACT — sponsorship:** observed offer → explicit accept → signed agreement,
delivery load and scheduled receivables → future finance settlement; overload
constrains future preparation conversion → competition remains uncertain → actual
results may create next-day bonuses/prizes and commercial effects → review/adapt.
Signing itself changes neither Cash nor reputation/audience. A drawn line from
receipt to preparation means shared company constraints, never that each receipt
automatically funds or increases preparation.

**FACT — people/preparation:** signing pays current fee, creates salary obligations,
changes roster/readiness/load; release spends exit cost, loses depth and preserves
already-due obligations. Preparation allocation affects future work, retaining
completed work. The UI can explain these rules qualitatively; it must not fabricate
per-player counterfactual forecasts absent an actor-safe preview.

**FACT — time/result:** finance settles before competition; future wins do not pay
earlier bills. Supplied future load is conditional on today's roster/agreements.
Result outcomes are known only after resolution. Place expected/conditional effects
apart from realized results; equal dates do not prove a causal connection.
Settlement is an authoritative simulation event, but these reads expose its current
cash/remaining-balance effects rather than a complete dated settlement ledger.

## Information hierarchy used in both alternatives

| Layer | Company orientation | Sponsor selected |
| --- | --- | --- |
| PRIMARY | Company/time/scope, ownership/shared-function distinction, material matter/why now/consequence/route, actual stop reason | Same context plus deadline, scheduled versus immediate money, load/capacity pressure and unknown wins; deliberate route to review |
| SECONDARY | Relevant shared-support relationships, next obligation/checkpoint and plan/coach evidence | Committed versus if-accepted forecast, conditional daily load and known payment timing |
| TERTIARY | Full structured hierarchy, roster/bills/terms and all relevant dated entries on request | Full agreement/payment records, eligibility basis, full day horizon |
| HISTORICAL / RAW | Dated player-safe results/reviews on request; internal receipt/revision/hash/debug records excluded from primary UX | Dated outcomes after advance, with supported causal explanations; technical IDs remain binding metadata |

## Mapping gaps and minimum later engineering implications

1. **FACT:** the live company adapter is sponsor-bounded. **PROPOSAL:** a reviewed
   bounded company presenter can compose S/G reads and existing destinations. No
   new gameplay is needed, but selection/draft/return for non-sponsor routes needs
   specification and verification before BUILD.
2. **FACT:** CompetitionDays lacks IDs; DayTrack is noninteractive. **PROPOSAL:**
   reuse its drawing as evidence and attach separate EntityLabels from real source
   IDs. Only the next fixture has an ID in G; later competition marks remain
   noninteractive schedule context until a reviewed actor-safe ID projection exists.
3. **FACT:** broad matter class/priority/stop reasons are not one authoritative
   structured read model. **PROPOSAL:** carry actual Application responses and
   sourced facts; review any minimal typed read addition. No text-parsed blockers,
   opaque urgency score or visual stop rules.
4. **FACT:** prototype bounds eight scopes/four matters and list fallback are
   historical implementation constraints. **PROPOSAL:** preserve bounded rendering,
   ID semantics and keyboard equivalents; review a deterministic selection bound
   instead of stuffing every event into that fixture model or silently dropping it.
5. **FACT:** full campaign branding/editor is unfinished. **PROPOSAL:** use actual
   company ID/name and neutral/text fallback; keep development notice in evidence.
   No reference brand or identity migration is authorized by this exploration.

These are implementation gates and known limitations, not untraceable gameplay.
Both proposals remain communicable through documentation/wireframes. No prototype
or production change is needed to complete the present exploration.
