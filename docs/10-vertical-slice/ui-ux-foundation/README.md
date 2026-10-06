# Company UX foundation

Status: **ACCEPTED PRESENTATION ARCHITECTURE** under DEC-023.
Date: 2026-10-04 (Asia/Saigon). Owner: Product UX Architect / UI Architecture Lead.
Accepted: 2026-10-05 (Asia/Saigon). Approval owner: Project Director.

Current Stage 5 visual work also follows the user's 2026-10-06
[strict visual / UX governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md):
all 50 rules and the 14-part gate apply. Use the
[screen review record](STAGE5_SCREEN_REVIEW_TEMPLATE.md) alongside each blueprint.
This instruction does not retroactively accept existing screens or final art.

The player builds and operates their own company. Corporate functions describe
what they are doing; the operating portfolio describes where it applies. A
company operating map, contextual workspaces, entities/documents and affairs
provide context for decisions. Esports is the first operating domain.

## Authority and boundaries

**DECISION:** DEC-001–022 remain canonical. In particular DEC-008 already establishes
company-first direction; DEC-022 preserves Company/World authority and closes
Phase 4. This package specifies presentation, not a replacement simulation.

**FACT:** The Director's current brief requires configurable company identity,
bounded scope, system-driven visuals and no production redesign in this task.
The Director's Stage 2 instruction accepts this package as presentation
architecture and authorizes a bounded provisional theme, minimal kit and internal
UI Lab. Original PROPOSAL and no-implementation labels in individual documents
describe the Stage 1 review-time package; DEC-023 and this note control current
architecture authority. Future modules, example screen blueprints, final aesthetic
choices and unresolved questions are not automatically approved for implementation.

Labels throughout: **FACT** = inspected evidence; **DECISION** = accepted record;
**ASSUMPTION** = unvalidated working premise; **PROPOSAL** = reviewable recommendation;
**OPEN QUESTION** = unresolved issue. A document's proposal status applies to its
unlabelled normative rules and tables. It cannot elevate itself above governance.

Current scope stays one company, one development esports discipline, one primary
team and a limited competitive environment. Only Business & Finance, Competitive
Portfolio, Talent & Performance and Affairs & Time need bounded workspaces beneath
the company context. The other functions are architectural seams, not empty tabs,
locked features or implementation requirements.

## Reading and implementation order

| Review area | Documents |
| --- | --- |
| Evidence and authority | [Source review](SOURCE_REVIEW.md), [accepted DEC-023](../../01-governance/DECISION_LOG.md#dec-023--company-first-ux-architecture) |
| Product structure | [Direction](COMPANY_UX_DIRECTION.md), [information architecture](INFORMATION_ARCHITECTURE.md), [module matrix](MODULE_LANDSCAPE.md) |
| Interaction | [Principles](UX_PRINCIPLES.md), [interaction model](INTERACTION_MODEL.md), [information priority](INFORMATION_PRIORITY.md), [taxonomy](SCREEN_TAXONOMY.md) |
| System design | [Design system](DESIGN_SYSTEM_FOUNDATION.md), [branding](PLAYER_BRANDING_RULES.md), [visual grammar](VISUAL_GRAMMAR.md), [components](COMPONENT_ARCHITECTURE.md) |
| Delivery discipline | [Blueprint template](SCREEN_BLUEPRINT_TEMPLATE.md), [AI rules](AI_UI_IMPLEMENTATION_RULES.md), [art policy](ART_PRODUCTION_POLICY.md), [verification](UI_VERIFICATION_STRATEGY.md) |
| Migration and review | [Current disposition](CURRENT_UI_DISPOSITION.md), [migration plan](PHASE5_UX_MIGRATION_PLAN.md), [questions](OPEN_QUESTIONS.md) |

Stage 1 created specifications only. The [Stage 2 UI Kit](../ui-kit/README.md)
implements the explicitly authorized subset. No future division, full Company
Operating Map, player-facing identity editor or final art direction is authorized.

## Exit evidence

| Brief criteria | Review evidence |
| --- | --- |
| 1–6: company, identity, two axes, future/current boundaries | Direction, information architecture, branding and all ten module matrices |
| 7–9: navigation, interaction, priority | Interaction model and information priority, with checkpoint semantics |
| 10–15: system, branding, grammar, components, taxonomy, AI | Corresponding documents and reusable blueprint |
| 16–18: art, verification, disposition | Art policy, verification strategy, file/symbol disposition |
| 19–20: preserve gameplay; no redesign | Documentation-only change set; pre-existing file hashes checked separately from this package |

DEC-023 acceptance establishes presentation direction; it does not certify the
existing internal client, close Phase 5 or discharge its deferred validation gates.
