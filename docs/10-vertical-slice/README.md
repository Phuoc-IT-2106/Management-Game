# Phase 5 — Vertical Slice

Status: BUILD IN PROGRESS. Phase 5 remains open; Director review is required.

Current presentation constraints: [Stage 5 visual / UX governance](../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md),
received 2026-10-06. All 50 rules and the 14-part gate apply to Stage 5 screen work;
this does not accept existing candidates or close Phase 5.

Phase 5 start baseline: latest main `1aa3d6e` fetched and checked on 2026-10-04 (Asia/Saigon).
Authority: DEC-001–022 and the user's Phase 5 instruction. The user explicitly
approved a labeled fictional 5v5 development fixture in this task.

- Completed:
  - canonical-source review and bounded scope/spec;
  - production skeleton and one complete management path;
  - [rules v2 continuous campaign](RULES_V2_CONTINUOUS_CAMPAIGN.md);
  - [rules v3 wave 1: incentives and measurement](RULES_V3_INCENTIVES.md) (2026-10-08), gate 1 passed;
  - the player-facing [genre shell](genre-shell/README.md), with every screen finished to the
    [UI polish standard](ui-ux-foundation/UI_POLISH_STANDARD.md) (2026-10-08);
  - [rules v4: the player world](RULES_V4_PLAYER_WORLD.md) (2026-10-13): five roles, a calibrated generated world, an
    eight-team league with playoffs, careers, and rivals with squads. Gates 1 and 2 pass.
- Active:
  - Director/human review of the screens;
  - DEC entries confirming rules v2, rules v3 wave 1 and rules v4, and the changed reputation gate;
  - the remaining balance references: player value, outcome diversity, market depth, financial pressure and decision
    density.
- Blockers: no implementation blocker currently; see OPEN_QUESTIONS.md.
- Deferred: final content/balance, all expanded systems, DV-01–DV-10 obligations.
- Exit: launch, meaningful decisions, ordered daily simulation, competition/business
  consequences, two consecutive cycles, safe save/load, deterministic verification,
  bounded measured roster UI, ownership intact; Director makes gate disposition.

Documents: [scope](VERTICAL_SLICE_SCOPE.md), [plan](IMPLEMENTATION_PLAN.md),
[architecture](ARCHITECTURE_MAPPING.md), [content](DATA_SCOPE.md),
[tests](TEST_PLAN.md), [evidence](VERIFICATION.md), [questions](OPEN_QUESTIONS.md).

## Company UX foundation — accepted; Stage 2 UI Kit

The [company UX foundation](ui-ux-foundation/README.md) is accepted presentation
architecture: company identity and context, separate corporate functions and
operating portfolio, contextual decisions, component/design-system governance
and incremental presentation migration. DEC-023 is ACCEPTED; DEC-001–022 remain
canonical. The existing functional client remains an internal engineering path.
[Stage 2](ui-kit/README.md) implements the bounded provisional theme, minimal UI
Kit and internal UI Lab. This does not close Phase 5 or approve future modules,
final art direction, identity/save migration or the Company Operating Map build.

## Player data specification — proposed

[Player Data Foundation v1](player-data/README.md) defines candidate player data,
attributes, rating formulas, NPC generation and validation (v0.1, 2026-10-08).
It is specification only, PROPOSED — NOT APPROVED; LoL remains a working design
candidate. It does not change current gameplay, content, saves or accepted decisions.

## Team, coach and rival organization data specification — proposed

[Entity Data Foundation v0.1](entity-data-foundation/README.md) defines separate
Team, Coach and compressed Rival Organization contracts, numerical profiles,
generation constraints and synthetic relational examples (2026-10-08).
It remains PROPOSED — NOT APPROVED and preserves Company/World ownership,
Player Data v0.2 and current rules/content/save v3 boundaries.

## Gameplay data review — proposal

[Gameplay data review](GAMEPLAY_DATA_REVIEW.md) (2026-10-09) audits what the current player, coach, team and rival numbers
actually do, and proposes a decision-led attribute set. It also covers formulas, and a data strategy where real API data
calibrates a generated NPC world. PROPOSAL only; the role model and data use need Director decisions.

## World experiment — potential, real rosters and a 4,000-NPC world

[World experiment](world-experiment/README.md) (2026-10-15) is an isolated research build: dynamic potential and
training, verified real-team rosters, and a 4,000-NPC labour market with a free-agent pool below 10%. It is EXPERIMENT
only: production code, content, rules and saves are unchanged, and the integration needs Director decisions
([review](world-experiment/HYBRID_WORLD_INTEGRATION_REVIEW.md); four of them verified in
[HYBRID_WORLD_DECISION_VERIFICATION](world-experiment/HYBRID_WORLD_DECISION_VERIFICATION.md)).
