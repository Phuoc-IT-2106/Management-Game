# Component architecture and governance

Status: PROPOSAL candidate catalog, not an already implemented UI library.
Approval establishes contracts; implementation follows incrementally as used.

## Composition boundaries

Application produces immutable actor-safe read models. Presentation adapters
format them and retain local draft/navigation state. Shared controls render
semantics and emit typed intents to a workspace presenter, which constructs
Application commands. Components never own authoritative gameplay state, draw
simulation randomness, mutate Domain, load full hidden content or write saves.

Screen scenes own layout and lifetimes. Shared behavior belongs in the library.
Bind by stable entity ID and revision; unsubscribe on leaving/freeing. Recycled
rows reset callbacks, metadata, tooltips, selection and focus ownership. Unknown
information must stay unknown through every shared component.

## Candidate catalog review

| Layer | Candidate / disposition | Contract or reason |
| --- | --- | --- |
| Atomic | Typography — adopt role binding, not a separate control for every role | Semantic role, text, locale/density; no screen-defined font scale |
| Atomic | Icon — adopt | Semantic key, size role, accessible label; no gameplay meaning from asset filename |
| Atomic | StatusIndicator — adopt | Status enum, label, reason; known/estimated/unknown separate |
| Atomic | EntityLabel — adopt | Entity reference, resolved display identity, type, navigation intent |
| Atomic | ResourceValue — adopt | Amount/unit/scope/date, fact/forecast state; never computes authoritative balance |
| Atomic | TimeMarker and ConfidenceIndicator — adopt | Date/checkpoint and supplied uncertainty/basis; no fake precision |
| Atomic | TrendIndicator — conditional | Requires comparable dated observations; do not fabricate series |
| Atomic | Tag — constrain | Metadata grouping only; use StatusBadge for status, not arbitrary visual labels |
| Atomic | Tooltip — adopt shared behavior | Focus and pointer access, text, no unique critical action or hidden truth |
| Reusable | EntityRow — adopt | Stable ID, selected/focus state, bounded bind lifecycle, direct navigation |
| Reusable | EntityCard — defer generalization | Only if a blueprint needs a compact entity summary; never universal grid |
| Reusable | StatusBadge / SectionHeader — adopt | Compose primitives; status badge does not duplicate a second status model |
| Reusable | DataTable — adapt existing bounded Tree/query behavior | Observed rows, typed columns, stable sort, page/filter, ID selection; no generic table engine |
| Reusable | ComparisonView — adopt on first use | Labelled alternatives, matched dimensions/units, missing evidence and trade-offs |
| Reusable | DocumentView — adopt | Document type/version/source, terms, references, permitted actions; opening never commits |
| Reusable | TimelineEvent / AlertItem — adopt | Stable matter/cause ID, class, priority, due date, scope, reason, navigation |
| Reusable | NavigationContext — adopt | Campaign identity, scope path, return context and keyboard equivalent |
| Reusable | SearchField — adapt roster use | Observed fields only, query revision/cancellation where asynchronous |
| Reusable | ContextMenu — conditional | Secondary shortcuts with keyboard equivalent; not sole critical action path |
| Reusable | ValidationMessage / CommitmentReview — adopt | Rejection reason, changed terms, pending/accepted state; no local legality authority |
| Pattern | CompanyOperatingMap — prototype later | Bounded scope relationships and situation overlays; list/matrix equivalent |
| Pattern | PortfolioWorkspace / DivisionWorkspace — shared shell variants | Scope, situations, evidence slots; discipline adapter supplies terminology |
| Pattern | DecisionWorkspace — adopt first contextual pattern | Situation → comparison → draft → review → submit → feedback |
| Pattern | NegotiationWorkspace — defer | Extend DecisionWorkspace when real negotiation commands exist |
| Pattern | EntityInspector / Dossier — one pattern | Compact/full layouts of the same identity/evidence contract, not duplicated systems |
| Pattern | ReportViewer — DocumentView variant | Dated evidence and result/cause links; no separate report framework |
| Pattern | FinancialFlowView — bounded specialized composition | Signed receipts, Cash, obligations, dates and forecast; no accounting engine |
| Pattern | Timeline — adopt when structured affairs exists | Bounded date range and deduplicated matters; no scheduler authority |
| Pattern | OrganizationView — defer full view | Future reporting/authority structure; current coach authority stays embedded |

“Adopt” means recommend as a canonical contract when approved, not build every
component in the next task. Implement only the subset needed by its approved
blueprint; use native Godot controls and theme variants first.

## Minimum reusable contract

Each component definition records purpose, accepted inputs, emitted events,
identity/revision rules, state combinations, layout/density behavior, token roles,
keyboard/focus, accessibility fallback, error/empty/loading/unavailable behavior,
binding lifetime, performance envelope, examples and owner/version.

For example, EntityRow takes an observed entity reference, label and permitted
status, emits `Inspect(entityId)` or selection intent, and cannot accept a Domain
Person object with private attributes. A DecisionWorkspace presenter maps its
commit intent to `Request(id, revision, SponsorDecision(offerId))` today; future
typed checkpoint/actor metadata is a separately reviewed contract extension.

## Governance rules

1. Search the catalog and current implementation before introducing a control.
2. Reuse canonical semantics and tokens. Domain components extend named slots or
   reviewed variants, not copied controls with local spacing/colors.
3. New reusable semantics require UI lead review; state/command boundaries require
   Technical Lead review; gameplay changes require the relevant accepted decision.
4. Document any local layout exception with purpose, owner and retirement condition.
   An exception does not create a second design system.
5. Version contract changes and update affected blueprints/UI Lab/goldens together.
   Preserve compatible callers or document the bounded migration.
6. Keep active tab, filter, selection, drafts and expansion local. A committed plan,
   authority envelope or financial obligation is always campaign state.

## QG-02 carry-forward

Preserve stable IDs, bounded row reuse, correct selection, stale-action rejection
and measured optimization. Use current `RosterTable` behavior as a reusable seam,
not evidence that all future screens are fast. Start with paged visible rows;
virtualization only after measurement. Maps also summarize data rather than
creating one Node per entity. If infrastructure cost grows disproportionate,
escalate under DEC-022 instead of silently building a giant framework.
