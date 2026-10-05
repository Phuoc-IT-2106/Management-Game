# Phase 5 UX Stage 3 — Company Operating Map

Status: **FUNCTIONAL PROTOTYPE READY FOR DIRECTOR REVIEW**. 2026-10-05, Asia/Saigon.
Recommendation: **ITERATE OPERATING MAP MODEL** before production acceptance.
Phase 5 remains open. Stage 4 has not begun.

Selected direction: hybrid ownership map. Company identity stays above a small
owned portfolio branch and a distinct shared-function lane. Four situations attach
to affected scopes; a single inspector explains them. Affairs links to the same
objects. Sponsor and preparation entries demonstrate contextual handoff and exact
return, without submitting commands. Map and ownership list share one projection.

Start from repository root:

```powershell
./build/Run-OperatingMap.ps1
./build/Run-OperatingMap.ps1 -Case sponsor
./build/Run-OperatingMap.ps1 -Case list
./build/Verify-OperatingMap.ps1
./build/Capture-OperatingMap.ps1
```

Separate scene: `res://UI/OperatingMap/OperatingMap.tscn`. Default `Main.tscn`
remains the internal management UI. Launch cases are in [fixture matrix](FIXTURE_MATRIX.md).
No new dependency, graph infrastructure, reusable component, Domain/Application
contract, gameplay rule, save migration, identity editor or production art.

PLAN compared all four alternatives. SPEC recorded the screen blueprint and reuse
decision before BUILD. VERIFY includes pure/native tests, rendered candidates,
performance observations and existing gameplay regressions. Technical prototype
authorization comes from the explicit Stage 3 brief and assigned design/engineering
roles; Director blueprint/model acceptance is not asserted.

- [Blueprint](SCREEN_BLUEPRINT.md) and [alternatives](ALTERNATIVES_REVIEW.md)
- [Map model](MAP_MODEL.md), [interaction](INTERACTION_SPEC.md), [component reuse](COMPONENT_ADDITIONS.md)
- [Verification](VERIFICATION.md), [visual/usability review](VISUAL_REVIEW.md), [open questions](OPEN_QUESTIONS.md)
- [Golden candidates and manifests](evidence/candidates/) — not approved production goldens

Baseline remote main verified at `471a6425c45974207a6afc1d3af0432148400dbd`.
Implementation commit: `efea35a8937d25bd1176a3cd54cf2d9480837eb6`.
Verified focus correction / capture source: `b005fe873bfc696866c480d58273dddc02f6a819`.
The pre-existing dirty gameplay work remains separate and unchanged. Prototype
source imports only the Stage 2 kit and native Godot; pure navigation compiles
without Godot, Domain or Application. No new authority duplicates Company/World.

This experiment proves a bounded implementation is feasible. It does not prove
the map beats the list or tabs in human use. Director should compare the supplied
tasks, then accept, iterate or reject. Physical input/DPI, final art, production
discipline, live identity/read contracts and all deferred gates remain separate.
