# Phase 5 UX Stage 4 — live sponsor commitment

Outcome: implemented and locally verified first live contextual decision workspace;
Director acceptance pending. **Phase 5 remains open.** No next UX stage started.

Director recommendation: **ITERATE FIRST CONTEXTUAL WORKSPACE**. The real management
loop works; local Debug rebind/confirmation cost exceeds the provisional 33.3 ms
target and human/physical-input review remains open. Sponsor gameplay needs no
invented depth. Human superiority over the old interface has not been proven.

## Use the live path

Launch the existing client (`build/Run-Client.ps1`). Select **Company context · live
sponsor workspace**, the sponsor matter beside **Business & Finance**, then
**Open decision entry**. Use **Review acceptance** followed by explicit commitment.
Return restores company selection. Return to internal management UI for existing
advance/save/load controls. Both presentations share the actual campaign session.

LIST-FIRST / MAP-OPTIONAL is applied to live company navigation using the same
semantic model and one sponsor workspace. DEC-023 remains authoritative; no new
DEC, generic graph or gameplay mechanic. Internal sponsor tab and map fixtures remain.

## Review summary

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

Commit SHA(s): no new commit created. Verified starting HEAD:
`a381d8f26b55132d697e9df9b93e97dcdfc474dc`. Substantial production gameplay work was
already uncommitted. Stage 4 remains reviewable in the working tree without silently
publishing that unrelated work. Final source hashes and preservation evidence identify
the tested files. A coherent clean-checkout baseline requires separate disposition
of the pre-existing implementation.
