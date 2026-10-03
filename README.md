# Strategic Company Simulator

Status: **Phase 4 — Technical Foundation. Prototype Gate Passed; production technical foundation is now authorized.**

## What is this project?

A PC business strategy and management simulation beginning with professional esports.

The player primarily acts as a CEO / executive with selective management authority. The strategic center is the company: competitive success matters because it creates reputation, audience, commercial leverage, and future options, but winning matches is not the sole success condition.

The core management thesis has passed the bounded headless-prototype gate. The project is now defining the production engine/framework, architecture, persistence, data and UI technical foundation before building the Vertical Slice.

## Prototype gate result

Phase 3 is closed with **PASS PROTOTYPE GATE**.

Measured evidence includes:
- scenarios A–M PASS;
- AC-01 through AC-13 PASS;
- 32/32 regression tests PASS;
- deterministic replay / repeated-seed verification;
- explicit causal trace review.

Prototype implementation and calibration remain noncanonical.

## Start here

1. [Project State](docs/00-project/PROJECT_STATE.md)
2. [Decision Log](docs/01-governance/DECISION_LOG.md)
3. [Game Vision](docs/00-project/GAME_VISION.md)
4. [Product Foundation](docs/03-product-foundation/README.md)
5. [Core Loop & Simulation](docs/06-core-loop-simulation/README.md)
6. [Specialist Specifications](docs/07-specialist-specs/README.md)
7. [Minimal Simulation Prototype](docs/08-minimal-prototype/README.md)
8. [Prototype engineering results](prototypes/reports/PROJECT_DIRECTOR_HANDOFF.md)
9. [Technical Foundation](docs/09-technical-foundation/README.md)

## Development sequence

Discovery ✅
→ Product Foundation ✅
→ Core Loop & Simulation ✅
→ Headless / Minimal Prototype ✅
→ **Technical Foundation ← CURRENT**
→ Vertical Slice
→ System Depth
→ Dynamic World
→ Balance
→ Expansion

Do not begin production Vertical Slice implementation until the Technical Foundation exit gate is approved.
