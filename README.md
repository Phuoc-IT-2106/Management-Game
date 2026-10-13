# Strategic Company Simulator

Status: **Phase 5 — Vertical Slice, BUILD IN PROGRESS.** Phase 4 Technical Foundation closed with deferred validation
obligations (DEC-022). Phase 5 stays open until Director review.

## What is this project?

A PC business strategy and management simulation beginning with professional esports.

The player primarily acts as a CEO / executive with selective management authority. The strategic center is the company:
competitive success matters because it creates reputation, audience, commercial leverage and future options, but winning
matches is not the sole success condition.

## Where it stands

- **Production architecture.** Godot 4.7.2 .NET with C# on .NET 10.0.401 (pinned). It is a modular monolith:
  - engine-independent Domain and Application;
  - Infrastructure for JSON content and snapshot saves;
  - a Godot client;
  - a headless runner.
- **Gameplay (rules v4, noncanonical development values):**
  - continuous seasons;
  - a negotiated sponsor market;
  - staggered contracts and renewals;
  - a coach market;
  - incentives that make losing costly (rules v3 wave 1);
  - five roles and a role-weighted player model;
  - a world generated per seed, calibrated from aggregate 2026 data;
  - an eight-team league with best-of series and playoffs;
  - player careers (development, retirement, youth intake);
  - rivals with real squads that recruit and trade.
- **Player client: the genre shell.** It has a top bar with Continue, a navigation rail, the Portal home, and Inbox, Squad,
  Competition, Commercial, Finance, Staff and Company sections, plus the sponsor decision workspace. Every screen follows the
  [UI polish standard](docs/10-vertical-slice/ui-ux-foundation/UI_POLISH_STANDARD.md).
  - It is set in one bundled family, Inter (SIL OFL 1.1).
  - Money shows in US dollars: a nominal 1:1 relabel of the development currency unit, with no economic change.
- **Open items:**
  - Director/human review of the screens;
  - DEC entries confirming rules v2, rules v3 wave 1 and rules v4, and the changed reputation gate;
  - the remaining balance references (player value, outcome diversity, market depth, financial pressure);
  - the deferred validation obligations DV-01 to DV-10.

## Start here

1. [Project State](docs/00-project/PROJECT_STATE.md)
2. [Decision Log](docs/01-governance/DECISION_LOG.md)
3. [Game Vision](docs/00-project/GAME_VISION.md)
4. [Vertical Slice](docs/10-vertical-slice/README.md), including [rules v2](docs/10-vertical-slice/RULES_V2_CONTINUOUS_CAMPAIGN.md), [rules v3 wave 1](docs/10-vertical-slice/RULES_V3_INCENTIVES.md) and [rules v4](docs/10-vertical-slice/RULES_V4_PLAYER_WORLD.md)
5. [Genre shell](docs/10-vertical-slice/genre-shell/README.md) and the [UI polish standard](docs/10-vertical-slice/ui-ux-foundation/UI_POLISH_STANDARD.md)
6. [Stage 5 visual / UX governance](docs/01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md) and the [genre-shell amendment](docs/01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md)
7. [Technical Foundation](docs/09-technical-foundation/README.md) and its [closure register](docs/09-technical-foundation/PHASE4_CLOSURE.md)
8. Earlier phases: [Product Foundation](docs/03-product-foundation/README.md), [Core Loop & Simulation](docs/06-core-loop-simulation/README.md), [Minimal Prototype](docs/08-minimal-prototype/README.md)

## Build and verify (Windows, PowerShell)

The pinned SDK and Godot binaries live under `qualification/.tools` (`build/Environment.ps1` checks the pins).

| Purpose | Command |
|---|---|
| Build, all six test suites and the headless two-cycle run, into a scratch folder | `build/Verify.ps1 -Configuration Debug -Evidence artifacts/verify` |
| The same into tracked evidence (`docs/10-vertical-slice/evidence`) | `build/Verify.ps1` (Release) |
| Native shell checks at 1280×720 and 1920×1080 | `build/Verify-Shell.ps1` |
| Rules measurement: four owner policies × 8 seeds × 5 seasons, failing if a gate is not met (gates 1 and 2 pass in rules v4) | `build/Measure-Rules.ps1 -Gate 2` |
| Native UI kit, operating map and sponsor workspace checks | `build/Verify-UiKit.ps1`, `build/Verify-OperatingMap.ps1`, `build/Verify-SponsorWorkspace.ps1` |
| Screen captures (needs an interactive desktop) | `build/Capture-Shell.ps1 -Results 1 -Cases -Output artifacts/<dir>` |
| Play | `build/Run-Game.ps1` |

## Development sequence

Discovery ✅
→ Product Foundation ✅
→ Core Loop & Simulation ✅
→ Headless / Minimal Prototype ✅
→ Technical Foundation ✅ (with deferred validation obligations)
→ **Vertical Slice ← CURRENT**
→ System Depth
→ Dynamic World
→ Balance
→ Expansion
