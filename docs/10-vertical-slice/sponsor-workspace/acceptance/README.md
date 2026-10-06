# Phase 5 UX Stage 4.1 — sponsor workspace acceptance

**Outcome: Stage 4 engineering acceptance is complete.**

**Recommendation: ACCEPT FIRST CONTEXTUAL WORKSPACE.** Human Director review is
pending; no human usability result or golden-image approval is claimed. Phase 5
remains open, and this delivery stops before the next UX stage.

Inspected local/remote `main`: `7ad0501fd0719c48b8c759a491771525567fa863`.
Verified published gameplay baseline: `0e9bbee0ea27ff5a6409eefa62b7d532384ce312`.
Stage 4.1 implementation and tested runtime source:
`043ad9bee1c149eb9cce407a419a1e5eda6f8799`.
The following documentation/evidence commit records these completed checks; it
does not retroactively change the original Stage 4 verification provenance.

The measured issue was rebuilding the entire view for state-only transitions.
The fix retains the shell and unchanged reading regions, updates feedback/actions,
and refreshes changed data. No gameplay, Application/Domain ownership, reusable
framework, semantic palette or overall information architecture was changed.

| Final local p95 | Debug | Managed Release |
| --- | ---: | ---: |
| Ordinary rebind | 5.3073 ms | 3.6017 ms |
| Review → Confirmation | 10.2538 ms | 10.9039 ms |
| Confirmation → Review | 4.6243 ms | 5.1681 ms |
| Command acknowledgement | 0.2472 ms | 0.2249 ms |
| Application refresh | 0.1597 ms | 0.1357 ms |

50 iterations per metric; Controls remain 53→53. Both scoped Release presentation
concerns meet 33.3 ms p95; acknowledgement meets 100 ms. Initial view creation and
company entry/return remain separate bounded carry-forwards, with local Release
p95 synchronous work under 100 ms. The editor engine is shared by both managed
configurations; exported-engine/target-hardware qualification and QG-02 are unchanged.

Only content change: `Northstar Esports` → **DEV_ORG_001**, with visible
**DEVELOPMENT FIXTURE — NONCANONICAL**. Content/canonical hashes legitimately changed.
Behavior equivalence passes after normalizing only name/content identity. Schema 1
save/load passes; old exact-content saves correctly reject without replacing state.

All required regressions pass: SponsorWorkspace 52; native sponsor 29 at each review
viewport; Domain 4; Integration 162; UI 3; UiKit 126 pure/24 native; OperatingMap
48 pure/101 native; Headless and internal Main smoke/save/load match. Seven visually
inspected 720p/1080p candidates retain readable core terms, actions, context and
Known/Estimated/Unknown distinctions. No material architecture issue was found.

[Performance and raw evidence](PERFORMANCE_REVIEW.md) ·
[Fixture identity and compatibility](FIXTURE_IDENTITY_CLEANUP.md) ·
[Executed verification](VERIFICATION.md) · [Visual review](VISUAL_REVIEW.md) ·
[5–10 minute Director protocol](DIRECTOR_REVIEW_PROTOCOL.md) ·
[Blank review record](DIRECTOR_REVIEW_RECORD.md) · [Remaining limits](OPEN_QUESTIONS.md).

Reproduce: `build/Measure-SponsorWorkspace.ps1 -Output <fresh-artifact-path>` and
`build/Verify-SponsorAcceptance.ps1`. Run sequentially on the pinned local toolchain
so other verification work does not compete with the performance workload.
