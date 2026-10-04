# Screen / workspace taxonomy

Status: PROPOSAL. These are composition patterns, not ten mandatory screens.
All use campaign identity, actor-safe read models and typed commands. Every
instance needs a [blueprint](SCREEN_BLUEPRINT_TEMPLATE.md); current/future status
comes from the module matrix. No final layout or art direction is specified.

## 1. Company / portfolio space

| Field | Contract |
| --- | --- |
| Purpose | Orient ownership and active situations across the company |
| Player question | Where does my company need attention? |
| Required information | Company identity, active scope relationships, material situations, current date/checkpoint |
| Allowed interactions | Select scope/situation; inspect relationship; return home; switch map/list equivalent |
| Typical components | NavigationContext, EntityLabel, CompanyOperatingMap, AlertItem, TimeMarker |
| Forbidden anti-patterns | KPI wall, all-entity network, empty future division tiles, mandatory headquarters traversal |
| Performance considerations | Render bounded active scopes and summarized relations; expand on demand; no Node per simulation entity |
| Example cases | Player Company with one development competitive unit and finance/preparation situations |

## 2. Functional workspace

| Field | Contract |
| --- | --- |
| Purpose | Address one corporate capability within explicit scope |
| Player question | Which commitment or constraint should I address here? |
| Required information | Function/scope, material constraints, evidence and relevant entities |
| Allowed interactions | Filter scope, inspect evidence, open linked decision or source agreement |
| Typical components | SectionHeader, FinancialFlowView, ComparisonView, EntityRow |
| Forbidden anti-patterns | Page equals database table; finance reduced to cash plus sponsor list |
| Performance considerations | Query only needed horizon/scope; avoid recomputing entire campaign per navigation |
| Example cases | Current company cash obligations and scheduled sponsor receipts |

## 3. Division workspace

| Field | Contract |
| --- | --- |
| Purpose | Operate an owned business with local rules and company consequences |
| Player question | What must this division prepare or change? |
| Required information | Owning company, division/team scope, next checkpoint, local eligibility/plan, shared constraints |
| Allowed interactions | Inspect team/competition; compare plan; navigate finance or authority evidence |
| Typical components | DivisionWorkspace, TimelineEvent, EntityRow, ConfidenceIndicator |
| Forbidden anti-patterns | Five permanent roster slots in shell; company disappears; discipline terms generalized to all businesses |
| Performance considerations | Lazy detail; bounded visible roster/events; no full World binding |
| Example cases | Development Discipline / Primary Team preparation window |

## 4. Entity / dossier

| Field | Contract |
| --- | --- |
| Purpose | Understand one person, team, asset or counterparty |
| Player question | Who/what is this, and how does it affect my decision? |
| Required information | Stable entity identity, type, scope, known facts, uncertainty, relationships and relevant terms |
| Allowed interactions | Follow references, compare, open eligible decision, return to original draft |
| Typical components | EntityInspector, EntityLabel, StatusBadge, DocumentView, ComparisonView |
| Forbidden anti-patterns | Portrait required for identity; copied contracts; hidden true rating in tooltip |
| Performance considerations | Bound related records and history; preserve ID when sorting/rebinding |
| Example cases | Candidate inspected from signing review; coach authority dossier |

## 5. Decision workspace

| Field | Contract |
| --- | --- |
| Purpose | Compare options and commit a meaningful choice |
| Player question | What should I commit, under which constraints? |
| Required information | Situation, options, evidence, uncertainty, cost, timing, authority, reversibility and expected consequences |
| Allowed interactions | Draft, compare, review, submit typed command, cancel unsubmitted draft |
| Typical components | DecisionWorkspace, ComparisonView, ResourceValue, ConfidenceIndicator, validation/commit controls |
| Forbidden anti-patterns | Green “best” option without evidence; hidden costs; optimistic cash change; stale auto-retry |
| Performance considerations | Compare actor-safe projections; no simulation reruns on every hover; immediate pending feedback |
| Example cases | Sponsor acceptance, person release, preparation allocation |

## 6. Negotiation workspace

| Field | Contract |
| --- | --- |
| Purpose | Compare proposed terms and reciprocal concessions when negotiation exists |
| Player question | Which terms can I accept or counter, and what do I give up? |
| Required information | Parties, versioned offer, editable supported terms, deadline, limits, concessions and uncertainty |
| Allowed interactions | Future supported counteroffer/accept/withdraw commands; inspect terms and compare revisions |
| Typical components | DecisionWorkspace with DocumentView and ComparisonView; NegotiationWorkspace extension deferred |
| Forbidden anti-patterns | Fake negotiation sliders on fixed offers; invented NPC willingness; modal chains |
| Performance considerations | Bound offer history; cancel stale evaluations; preserve term revision |
| Example cases | Future Sponsor Contract renewal; current fixed sponsor acceptance uses category 5 instead |

## 7. Dense-data inspection

| Field | Contract |
| --- | --- |
| Purpose | Inspect many comparable records efficiently |
| Player question | Which records fit my criteria and warrant attention? |
| Required information | Scope, column meaning/units, count/page, filters, selected stable ID, unknown-state semantics |
| Allowed interactions | Sort/search/filter, page, keyboard-select, inspect/compare entity; explicit legal action |
| Typical components | DataTable, SearchField, EntityRow, StatusBadge, pagination |
| Forbidden anti-patterns | Row index as ID; one Node per record; hidden-truth filtering; giant custom table framework |
| Performance considerations | Bounded reusable rows, stable sort ties, latest query wins, measured rebind/memory/focus |
| Example cases | Current roster or financial commitments; historical records when supported |

## 8. Event / review

| Field | Contract |
| --- | --- |
| Purpose | Explain a resolved event and its company consequences |
| Player question | What changed, why, and what should I adapt? |
| Required information | Event date/result, contributing known decisions, actual changes, pending effects and uncertainty |
| Allowed interactions | Follow causes/entities, compare expected/actual, open next decision, return to affairs |
| Typical components | ReportViewer, TimelineEvent, TrendIndicator, EntityLabel |
| Forbidden anti-patterns | Victory confetti substitutes for financial consequences; every mutation becomes popup; hidden resolver truth |
| Performance considerations | Bounded event summary/history; no unbounded string concatenation on every refresh |
| Example cases | Competition result followed by dated receivable and audience/reputation consequences |

## 9. Document / report

| Field | Contract |
| --- | --- |
| Purpose | Inspect terms or dated evidence as a decision object |
| Player question | What does this agreement commit, or what does this report support? |
| Required information | Document ID/type, source owner, parties/scope, version/date, terms/evidence and status |
| Allowed interactions | Navigate references, compare terms, inspect history; open authorized decision |
| Typical components | DocumentView, ResourceValue, TimeMarker, ConfidenceIndicator |
| Forbidden anti-patterns | Decorative paper with unreadable text; accepting by merely opening; historical terms masquerade as current |
| Performance considerations | Bound/section long documents and related history; searchable text if justified |
| Example cases | Sponsor terms, employment commitment, observed opponent report |

## 10. Timeline / affairs

| Field | Contract |
| --- | --- |
| Purpose | Relate decisions, deadlines and consequences through time |
| Player question | What is happening, what is due, and why did time stop? |
| Required information | Current date/checkpoint, blocking/actionable/informational/historical matters, entity, reason, deadline and consequence of ignoring |
| Allowed interactions | Inspect/filter/group, resolve via linked workspace, advance through existing Application command |
| Typical components | Timeline, TimelineEvent, AlertItem, NavigationContext, TimeMarker |
| Forbidden anti-patterns | Sole unexplained Advance button; notification spam; presentation invents a scheduler |
| Performance considerations | Bounded time window; aggregate routine items; stable focus across refresh; old queries discarded |
| Example cases | Upcoming competition, candidate/sponsor deadlines, due items and recent result review |
