# Information architecture

Status: PROPOSAL. Navigation describes projections; it creates no state owners.

## Two independent axes

| Axis A: corporate function | Scope addressed |
| --- | --- |
| Company & Strategy | Company priorities and portfolio relationships |
| Business & Finance | Economic and commercial commitments |
| People & Organization | Responsibilities, staff, capacity and authority |
| Talent & Performance | Acquisition, development, deployment and retention |
| Operations & Assets | Enabling assets and infrastructure |
| Intelligence & Analytics | Decision evidence and uncertainty |
| Market & Growth | Expansion opportunities and strategic trade-offs |
| Governance & Risk | Obligations, exposure and recovery |
| Affairs & Time | Situations, deadlines, checkpoints and consequences |

Axis B is the operating portfolio. Competitive Portfolio is a portfolio entry,
not a tenth corporate function. The ten module briefs include this entry because
it needs its own responsibility contract.

```text
Player Company [identity from campaign]
  Competitive Portfolio
    Esports
      Development Discipline
        Primary Team
```

Future-compatible examples, not current navigation entries:

```text
Player Company
  Competitive Portfolio: Esports (Discipline A / B), Football, Basketball
  Media
  Merchandising
  Events
  Talent Management
```

Talent Management as a future business sells a service; Talent & Performance as
a corporate function manages company talent. Their similar names do not merge
responsibilities or records. A sponsor can support multiple scopes in a future
model without duplicated contracts. A department belongs to the company even
when its work serves one team.

## Semantic hierarchy

| Level | Player question | Context |
| --- | --- | --- |
| 0 Company | What organization am I building? | Campaign company ID and identity |
| 1 Function or portfolio | What area am I operating? | Function and/or portfolio lens |
| 2 Scope | Which organizational area matters? | Division, department, asset or market; nested scopes allowed |
| 3 Entity / situation | Which subject or problem? | Stable person, team, contract, opportunity or situation ID |
| 4 Decision workspace | What am I deciding? | Decision kind, actor-visible evidence and current revision |

Levels are semantic, not a five-click requirement. Esports → discipline → team
may be nested Level 2 scopes. Compress single-child paths visually while retaining
an accessible full path. A timeline link can enter Level 4 directly with context.

Examples (all identifiers generic):

- Player Company → Competitive Portfolio → Esports / Development Discipline /
  Primary Team → Competition Checkpoint → Preparation Decision.
- Player Company → Business & Finance → Sponsorship → Sponsor Offer → Commitment
  Review. Renewal Negotiation is a future example only.
- Player Company → Talent & Performance → Recruitment → Candidate → Signing
  Review. Negotiable counteroffers are future scope.

## Route and cross-domain contract

Conceptual route: `{companyId, functionKey?, scopeIds[], entityKind?, entityId?,
workspaceKind, returnContext?}`. Keys are stable and names are resolved from the
read model. This is not a proposed save schema or a requirement to create domain
entities solely to fill breadcrumbs. Current company-wide roster/coach data can
project the sole team context without inventing a second roster authority.

Cross-links change the lens while preserving company and relevant scope. A
sponsor review → finance commitment list → source agreement link returns to the
same decision draft and selected offer when still valid. Store navigation history,
filter, scroll and focus locally; validate IDs/revision before resuming a command.
After load/new campaign invalidate incompatible routes and drafts. If an entity
is gone, explain why, show permissible history and offer the nearest valid context.

Back restores origin, not a fixed dashboard. Entity labels provide direct
navigation; do not add redundant “View” buttons. Keyboard users receive the same
paths. Global entity search is deferred until navigation research justifies it;
the existing roster search remains supported.

## Current exposure

Company context plus four bounded areas: finance/sponsors; development competition
and preparation; roster/coach/preparation talent inspection; situations/deadlines/
results. Shared people, intelligence and risk projections may appear inside those
workspaces. They do not require separate corporate modules or new commands now.
