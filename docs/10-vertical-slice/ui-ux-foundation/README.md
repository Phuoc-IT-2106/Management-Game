# Company UX foundation

Status: **UX FOUNDATION READY FOR DIRECTOR REVIEW**. PLAN / SPEC only.
Date: 2026-10-04 (Asia/Saigon). Owner: Product UX Architect / UI Architecture Lead.
Approval owner: Project Director. DEC-023 is **PROPOSED**.

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
These task constraints are binding. Detailed contracts below are **PROPOSAL**
pending review, even where they use “must” to define the proposed standard.

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
| Evidence and authority | [Source review](SOURCE_REVIEW.md), [proposed DEC-023](../../01-governance/DECISION_LOG.md#dec-023--company-first-ux-architecture) |
| Product structure | [Direction](COMPANY_UX_DIRECTION.md), [information architecture](INFORMATION_ARCHITECTURE.md), [module matrix](MODULE_LANDSCAPE.md) |
| Interaction | [Principles](UX_PRINCIPLES.md), [interaction model](INTERACTION_MODEL.md), [information priority](INFORMATION_PRIORITY.md), [taxonomy](SCREEN_TAXONOMY.md) |
| System design | [Design system](DESIGN_SYSTEM_FOUNDATION.md), [branding](PLAYER_BRANDING_RULES.md), [visual grammar](VISUAL_GRAMMAR.md), [components](COMPONENT_ARCHITECTURE.md) |
| Delivery discipline | [Blueprint template](SCREEN_BLUEPRINT_TEMPLATE.md), [AI rules](AI_UI_IMPLEMENTATION_RULES.md), [art policy](ART_PRODUCTION_POLICY.md), [verification](UI_VERIFICATION_STRATEGY.md) |
| Migration and review | [Current disposition](CURRENT_UI_DISPOSITION.md), [migration plan](PHASE5_UX_MIGRATION_PLAN.md), [questions](OPEN_QUESTIONS.md) |

No component library, UI Lab, final palette/font, image, new domain, branding
editor or UI demo is created here. Existing gameplay and the uncommitted client
remain untouched. The next proposed task is a bounded Design System + UI Kit,
after Director disposition of this package.

## Exit evidence

| Brief criteria | Review evidence |
| --- | --- |
| 1–6: company, identity, two axes, future/current boundaries | Direction, information architecture, branding and all ten module matrices |
| 7–9: navigation, interaction, priority | Interaction model and information priority, with checkpoint semantics |
| 10–15: system, branding, grammar, components, taxonomy, AI | Corresponding documents and reusable blueprint |
| 16–18: art, verification, disposition | Art policy, verification strategy, file/symbol disposition |
| 19–20: preserve gameplay; no redesign | Documentation-only change set; pre-existing file hashes checked separately from this package |

Readiness means the specification can be reviewed. It does not mean DEC-023 is
accepted, the current client satisfies it, or a Phase 5 quality gate has passed.
