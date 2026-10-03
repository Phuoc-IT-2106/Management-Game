# Strategic Company Simulator

Status: **Phase 3 — Headless / Minimal Prototype. Prototype specification approved; ready for BUILD / VERIFY.**

## What is this project?

A PC business strategy and management simulation beginning with professional esports.

The player primarily acts as a CEO / executive with selective management authority. The strategic center is the company: competitive success matters because it creates reputation, audience, commercial leverage, and future options, but winning matches is not the sole success condition.

The project is currently validating the management simulation before selecting the production engine or technical architecture.

## Current validation scope

The approved headless prototype is intentionally narrow:

- 1 player-controlled company;
- 1 fictional 5v5 role-based esports test discipline;
- 1 primary team;
- 6 contracted players (5 starters + 1 flex substitute);
- 1 Head Coach;
- up to 4 bounded rival organizations;
- a small talent market;
- a limited sponsor/commercial loop;
- basic liquidity, obligations, debt and restructuring;
- Reputation, Audience/Fandom, Organizational Capacity and Information Quality;
- bounded competitive preparation, strategy and delegation;
- deterministic seeded scenarios and causal traces.

The exact numbers, 84-day horizon, rival count, ratings and coefficients are **prototype calibration, not product canon**.

## Start here

1. [Project State](docs/00-project/PROJECT_STATE.md)
2. [Decision Log](docs/01-governance/DECISION_LOG.md)
3. [Game Vision](docs/00-project/GAME_VISION.md)
4. [Product Foundation](docs/03-product-foundation/README.md)
5. [Core Loop & Simulation](docs/06-core-loop-simulation/README.md)
6. [Specialist Specifications](docs/07-specialist-specs/README.md)
7. [Minimal Simulation Prototype](docs/08-minimal-prototype/README.md)
8. [Prototype BUILD / VERIFY Handoff](docs/05-handoffs/PROTOTYPE_BUILD_VERIFY_HANDOFF.md)

## Development sequence

Discovery ✅
→ Product Foundation ✅
→ Core Loop & Simulation ✅
→ **Headless / Minimal Prototype ← CURRENT**
→ Technical Foundation
→ Vertical Slice
→ System Depth
→ Dynamic World
→ Balance
→ Expansion

Do not choose the production engine or begin the production vertical slice until the prototype has been built and verified against its acceptance criteria.
