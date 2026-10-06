# Company UX direction

Status: PROPOSAL, constrained by DEC-001/002/003/008/022 and the Director brief.

Current interpretation (2026-10-06): DEC-023 accepts this architecture. The active
[Director composition clarification](../../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md)
requires company-level visual identity to read as a **Company Operating / Command
Space**. Structured hierarchy is required navigation/reference, not the dominant
visual prescription. Ownership, shared functions, situations, time, commitments
and consequences form one connected company. Both administrative tree/editor
composition and generic SaaS dashboard composition fail. Panels, charts, lists and
other instruments are allowed when they answer current simulation questions.
The [Stage 5 blueprint](../company-ui-demo/COMPANY_OPERATING_SPACE_BLUEPRINT.md)
is a draft semantic composition, not final art or implementation approval.

The intended question is “What company am I building, and which commitment should
I make next?” Competitive teams are owned business units. Competition generates
strategic and commercial consequences within the company's broader objectives.

## Company as the highest context

The shell shows campaign organization identity, current date/checkpoint and the
scope of the current workspace. Home is a company operating space: relationships
between the active business and supporting functions, with situations attached
to the relevant owner. The company remains identifiable while inspecting a person,
contract or match. A company summary may contain a few decision-relevant values;
it is not a permanent KPI wall.

A strategic situation contains a cause, affected scope, deadline/horizon,
responsible actor, available response and cross-company consequence. For example,
an available sponsor could improve scheduled receipts while increasing delivery
load and reducing preparation throughput. Selecting it opens a commitment
workspace with finance and competition evidence. It does not open a generic
“sponsor statistics” dashboard as the mandatory first step.

Strategy is expressed through linked commitments now. Persistent strategic goals,
division budgets, capital-allocation commands and expansion postures are future
proposals; naming Company & Strategy does not implement them.

## Identity contract

Company identity is player-defined campaign data, accessed through a versioned
read model. Name, abbreviation, emblem reference and primary/secondary colors
must be representable without changing screen code. IDs carry identity in rules,
routes and commands. Display names never act as keys. No canonical player company,
logo or color scheme is prescribed. See [branding rules](PLAYER_BRANDING_RULES.md).

Technical fixtures use `DEV_ORG_001`, `TEST_COMPANY` or `FIXTURE_ORGANIZATION` and
the visible marker **DEVELOPMENT FIXTURE — NONCANONICAL**. The existing fixture
name is a migration defect, not a recommended brand or project canon.

## Why this scales

Corporate functions operate on scoped references to portfolio entities. A shared
contract view can serve employment and sponsorship while discipline-specific
views explain eligibility and preparation. Adding a separately approved sport or
media business would add portfolio scope and relevant capabilities, not change
the meaning of company finance or navigation root. No universal tactical schema
or generic domain plugin framework is required now.

Future media, merchandising, events, talent management and traditional sports
remain examples. They must earn scope through meaningful decisions and separate
approval. The current slice presents one development discipline and primary team;
it must not display empty future modules as apparent playable features.

## Alternatives considered

| Model | Value | Problem / disposition |
| --- | --- | --- |
| Current dashboard and tabs | Cheap, working access to commands | Retain internally; weak company relationships and situation continuity |
| Headquarters exploration | Spatial identity | Art/navigation cost; no mandatory 3D or photorealistic space |
| Universal card grid | Simple reusable layout | Conceals hierarchy and favors metric summaries; reject as universal model |
| Full relationship graph | Can explain dependencies | Density, keyboard and implementation cost; defer until a concrete need |
| Operating map + workspaces + documents + affairs | Company context with task-specific depth | Preferred; validate orientation and repeated use before production rollout |

Success is a player explaining how a commitment affects their company, finding
its consequences and adapting across repeated cycles. Screenshot novelty is not
success. Core workflows must remain usable with text, symbols and no portraits.
