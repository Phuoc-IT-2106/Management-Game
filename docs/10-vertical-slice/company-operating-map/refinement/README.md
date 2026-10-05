# Phase 5 UX Stage 3.1 — Director refinement gate

2026-10-05, Asia/Saigon. **Refinement complete; Director disposition pending.**
Phase 5 remains open. Stage 4 has not started.

Recommendation: **ACCEPT LIST-FIRST / MAP-OPTIONAL**. The map remains a useful
optional relationship overview; the common shell and inspector deliver the
verified task access and return behavior. This is a conservative engineering/UX
recommendation, not a claim of demonstrated human superiority.

Exactly one map and one structured tree share the same immutable MapSnapshot,
stable IDs, selection, inspector, dated Affairs and return state. No gameplay,
live integration, persistence, reusable kit or project decision changes.

- [Specification and removal audit](REFINEMENT_SPEC.md)
- [Refined map](MAP_REFINED.md) and [refined tree](LIST_REFINED.md)
- [Task comparison and engineering proxies](COMPARISON_MATRIX.md)
- [720p review](720P_REVIEW.md) and [visual review](VISUAL_REVIEW.md)
- [Executed verification](VERIFICATION.md)
- [Director recommendation](DIRECTOR_RECOMMENDATION.md), [open questions](OPEN_QUESTIONS.md)

```powershell
./build/Run-OperatingMap.ps1 -Mode map -Case sponsor
./build/Run-OperatingMap.ps1 -Mode list -Case sponsor
./build/Verify-OperatingMap.ps1
./build/Capture-OperatingMap.ps1 -Refinement -Output artifacts/map-refinement/repeat
```

Launch mode is an explicit comparison control, not a saved preference. The legacy
launcher default remains map for reproducibility; adopting list as the product
default follows Director acceptance. Main.tscn remains the internal game client.
Stage 3 documents and screenshots remain historical; this package describes the
current refinement. Candidates are not approved production goldens.
