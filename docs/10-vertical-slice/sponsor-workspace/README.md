# Phase 5 UX Stage 4 — live sponsor commitment

Current acceptance report: [Stage 4.1](acceptance/README.md). The original Stage 4
implementation and measurements below are historical evidence from 2026-10-05.
Director/human acceptance remains separate. **Phase 5 remains open.**

Original Stage 4 recommendation: **ITERATE FIRST CONTEXTUAL WORKSPACE**. The real management
loop works; local Debug rebind/confirmation cost exceeds the provisional 33.3 ms
target and human/physical-input review remains open. Sponsor gameplay needs no
invented depth. Human superiority over the old interface has not been proven.

## Stage 5 instrument refinement — 2026-10-06 (candidate, not accepted)

Under [Stage 5 governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md), the
workspace now answers the decision's consequence directly instead of leaving
arithmetic to the player:

- Load and 7-day forecast **if accepted**, plus a 28-day **DayTrack** answering
  "When does load exceed capacity, and when do receipts and matches fall?". All
  series come from the authoritative `AcceptSponsor` preview and `Simulation.Load`;
  the read model adds `LastDay`, `LoadByDay`, `CompetitionDays` and
  `LoadByDayIfAccepted`. Daily load holds today's roster/agreements fixed and is
  labelled Estimated. Every drawn fact is restated in text.
- The reading region sizes to its content up to the available height, so actions
  follow the evidence (no dead band at 1080p); supporting evidence moved under the
  terms to balance the columns.
- Earlier same-day fixes: distinct entity markers (`[?]` reserved for Unknown),
  edge-bar selection, numeric face for amounts only, no data revision in the
  player-facing summary.

No gameplay rule, save schema or content changed. Reproduce on the pinned toolchain
with `build/Verify-SponsorAcceptance.ps1 -Output artifacts/<fresh>/checks`; window
captures need an interactive desktop session. Human Director review remains pending
and these captures are candidates only.

## Use the live path

Launch the existing client (`build/Run-Client.ps1`). Select **Company context · live
sponsor workspace**, the sponsor matter beside **Business & Finance**, then
**Open decision entry**. Use **Review acceptance** followed by explicit commitment.
Return restores company selection. Return to internal management UI for existing
advance/save/load controls. Both presentations share the actual campaign session.

LIST-FIRST / MAP-OPTIONAL is applied to live company navigation using the same
semantic model and one sponsor workspace. DEC-023 remains authoritative; no new
DEC, generic graph or gameplay mechanic. Internal sponsor tab and map fixtures remain.

## Original Stage 4 review summary (2026-10-05)

- Mechanic: fixed-offer acceptance; scheduled receipts, conditional win bonuses,
  immediate delivery load and limited active slot. No negotiation, renewal,
  termination, relationship meter, clauses or direct brand/performance bonus.
- Architecture: company context → typed intent → sponsor presenter → existing
  Request/SponsorDecision → Application validation/Domain → fresh observation.
- Read additions: live identity/revision, full term/end/payment dates, actual
  eligibility and signed receipts. No hidden truth or Domain objects enter UI.
- Known: terms and present resources. Estimated: seven-day committed cash forecast,
  excluding unsigned offers/unearned wins. Unknown: future wins and total bonuses.
- Cross-system effects: load above capacity reduces future preparation conversion;
  Finance settles scheduled receipts and actual-win bonuses. Signing itself changes
  neither Cash nor reputation/audience.
- Components: Stage 2 document/comparison/value/confidence/validation/commitment
  and entity controls; existing compact company context. Zero reusable additions.
- Safety: stale rejection, actor-safe refresh, renewed review, no automatic retry,
  duplicate suppression, mutation-free cancel, exact valid return and missing-matter
  Business & Finance fallback. Existing schema-1 save/load passes.
- Visuals: seven bounded 720p/1080p candidates; all critical terms and actions
  visible, no horizontal overflow, supporting evidence scrolls.
- Verification: 42 focused checks; 23 native sponsor checks per review viewport;
  Domain 4, Integration 162, UI 3, UiKit 126 pure/24 native, OperatingMap 48 pure/101
  native; internal Main smoke passes; existing gameplay hashes unchanged.
- Performance: retained Controls 53/53; rebind p95 36.46 ms, confirmation p95
  40.19 ms; single acknowledgement 20.44 ms, Application refresh 0.46 ms. Local Debug
  observations, not target-hardware/FPS acceptance. QG-02 history is unchanged.

## Package and provenance

[Audit](CURRENT_MECHANIC_AUDIT.md) · [Blueprint](SCREEN_BLUEPRINT.md) ·
[Read contract](READ_MODEL_CONTRACT.md) · [Interaction](INTERACTION_SPEC.md) ·
[Flow](DECISION_FLOW.md) · [Trade-offs](TRADEOFF_MODEL.md) ·
[Components](COMPONENT_REUSE.md) · [Verification](VERIFICATION.md) ·
[Visual review](VISUAL_REVIEW.md) · [Questions](OPEN_QUESTIONS.md).

Reproduce: `build/Verify-SponsorWorkspace.ps1`; captures:
`build/Capture-SponsorWorkspace.ps1`. Use the pinned environment and native Windows
access for rendering and the existing file-replacement checks.

Original verification ran against starting HEAD
`a381d8f26b55132d697e9df9b93e97dcdfc474dc` plus then-uncommitted gameplay and Stage 4
files. No commit had been created **at that verification time**; its source digest
continues to identify that historical working tree.

Publication was subsequently completed and verified in Git history:

- Gameplay baseline: `0e9bbee0ea27ff5a6409eefa62b7d532384ce312`
  — `feat: publish verified vertical slice gameplay foundation`.
- Stage 4 implementation: `7ad0501fd0719c48b8c759a491771525567fa863`
  — `feat: add live sponsor commitment workspace`; also the inspected Stage 4.1
  starting local/remote `main` HEAD.
- Stage 4.1 runtime/fixture implementation and current verification source:
  `043ad9bee1c149eb9cce407a419a1e5eda6f8799`.

The later publication does not retroactively date the original tests. Current
acceptance measurements, fixture hash and regression evidence are in the linked
acceptance package; the original hashes and captures remain unchanged.
