# AI UI implementation rules

Status: PROPOSAL mandatory workflow upon approval. Existing accepted decisions
and the Director's current no-implementation constraints already apply.

Current authority note (2026-10-06): the opening status describes Stage 1 delivery.
DEC-023 accepts the architecture; subsequent stage instructions govern authorized
implementation. The explicit [Stage 5 visual / UX governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md)
now requires approved blueprint, component catalog, tokens and interaction rules
before visual implementation, plus the [Stage 5 review record](STAGE5_SCREEN_REVIEW_TEMPLATE.md).
It does not grant blanket approval to illustrative screens or provisional designs.

## Authority

Read in order: accepted project decisions → approved UX architecture → approved
design system → component contracts → approved screen specification → visual
reference/mockup. Check approval status and current project state; this proposed
package does not override DEC-001–023. A screenshot never overrides any rule.

## Required workflow and evidence

| Step | Deliverable / exit check |
| --- | --- |
| UX requirement | Player question, situation, meaningful decision, accepted source and bounded scope |
| Screen taxonomy | Select pattern(s); explain why they fit instead of inventing a new screen family |
| Approved screen blueprint | Complete template, exact commands/read fields, uncertainty/authority, states, review owner and approval |
| Canonical components | Map requirements to catalog and versions; propose gaps for review before duplication |
| Implementation | Small presentation increment on current Application contracts; native controls and shared theme roles |
| Automated checks | Relevant projection, command, stable-ID, stale-action, token and behavior checks |
| Screenshot | Fixed reproducible state, viewport, build, content/branding identity; screenshots supplement behavior evidence |
| Visual review | Human review of purpose, readability, state semantics, identity and regression; Stage 5 A–J audit and all 14 gate criteria with evidence; accept/revise/reject with rationale |

Do not skip blueprint review because a concept image looks complete. If no new
approval is needed under already accepted scope, proceed within that scope;
review requirements are not an excuse to ask repeatedly for previously granted
authorization. This task stops at specifications and creates no screenshots.

## Prohibited shortcuts

Do not invent a visual system per screen, arbitrary colors/spacing/typography,
canonical company names/logos, fixed player-brand colors, redundant components,
unapproved mechanics or silent architecture changes. Do not infer gameplay from
mockups, implement screenshots by visual guessing, expose hidden truth, or bind
one Node per campaign entity. Do not replace working gameplay to accommodate a
layout or treat UI preferences as Company/World authority.

Find existing components first. A new domain variant must reuse shared behavior
and explain its additional semantics. Put layout choices in a blueprint and token
values in the design system. Any new command or read field identifies its owner,
revision and observation boundary. Unsupported behavior is an explicit gap,
not fabricated data or a disabled future module advertised as playable.

## Implementation handoff checklist

- Record the relevant accepted decision, blueprint version, scope and command list.
- Identify changed files and preservation of unrelated work; never bundle a dirty
  implementation into a documentation or presentation commit accidentally.
- Verify renamed company, changed brand, absent art and stable entity selection.
- Verify accepted/rejected/pending commands and return-to-decision after refresh.
- Run only relevant checks; report actual evidence and remaining gates accurately.
- Review before/after screenshots with intentional changes explained; do not
  update goldens simply to silence a diff.
- Stop at the authorized milestone. Design System/UI Kit, map prototype and
  production migration are separate tasks, not implied by this foundation.
