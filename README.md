# Strategic Company Simulator

Status: Documentation foundation; Phase 2 — Core Loop & Simulation specialist package received for Director review. Canonical phase and decision records remain unsynchronized.

## What is this project?

A PC business strategy and management simulation beginning with professional esports. The current product center is company-first management: the player acts primarily as a CEO or executive with significant management authority. Competitive results matter because they affect the company’s opportunities and resilience. The exact executive identity remains open.

This repository organizes product knowledge and Director review. It contains no production implementation.

## Current scope

The proposed first validation scope is one player-controlled company, one esports discipline, one main team, limited staff, roster and contracts, competition, finance, sponsors, reputation and audience effects, evolving rivals, delegation, time progression, and financial pressure with recovery choices. This boundary is a **Phase 1 proposal**, not an approved specification. Long-term expansion beyond esports is future scope.

## Start here

1. Read [document authority](docs/01-governance/DOCUMENT_AUTHORITY.md) for the source hierarchy and approval rules, and the [source manifest](docs/01-governance/SOURCE_MANIFEST.md) for ZIP provenance.
2. Read the original [project state](docs/00-project/PROJECT_STATE.md) alongside the [proposed Phase 2 state](docs/00-project/PROJECT_STATE_PHASE2_PROPOSED.md). The original is stale; proposed updates await Director approval.
3. Read the [completed Discovery worksheet](docs/02-discovery/DISCOVERY_WORKSHEET_COMPLETED.md), then check the [decision log](docs/01-governance/DECISION_LOG.md), [candidate updates](docs/01-governance/DECISION_LOG_UPDATES_PROPOSED.md), and [synchronization issues](docs/01-governance/CONFLICTS_AND_SYNC_RECOMMENDATIONS.md).
4. Read the [Phase 1 product documents](docs/03-product-foundation/README.md) and [Phase 2 simulation package](docs/06-core-loop-simulation/README.md). Statements labeled DECISION inside specialist proposals are inherited claims, not evidence of formal approval.

## Repository guide

| Location | Purpose |
| --- | --- |
| `docs/00-project` | Charter, vision, roadmap, glossary, and project state. |
| `docs/01-governance` | Decision status, document authority, questions, risks, and review issues. |
| `docs/02-discovery` | Preserved Phase 0 reasoning and source material. |
| `docs/03-product-foundation` | Phase 1 specialist proposals and proposed vision changes. |
| `docs/04-system-context` | Conceptual system and expansion context, not software architecture. |
| `docs/05-handoffs` | Role-to-role summaries; handoffs do not approve decisions. |
| `docs/06-core-loop-simulation` | Phase 2 simulation model, open questions, and proposed clarifications. |
| `docs/90-archive` | Preserved source-package context and later superseded material. |
| `specs` | Future approved system specifications. |
| `research` | Evidence and research, distinct from decisions. |
| `prototypes` | Future experiments and prototype notes. |

## Development sequence

Discovery → Product Foundation → Core Loop & Simulation → Prototype → Technical Foundation → Vertical Slice → System Depth → Dynamic World → Balance → Expansion.

Product decisions and the management loop should be reviewed before production architecture or implementation begins.
