# Live decision flow

1. Company list (optional map), using the current Application sponsor snapshot.
2. Business & Finance sponsor matter: why income/load matters, deadline and ID.
3. Commercial document: payment dates, end, bonus condition and load obligation.
4. Company evidence: current cash/load/capacity, committed cash forecast and unknowns.
5. Review acceptance: local confirmation; nothing in gameplay changes.
6. Explicit commit: UiIntent → SponsorPresenter → Request(unique ID, viewed
   revision, SponsorDecision(offer ID)) → Session validation → AcceptSponsor.
7. Accepted: Company contract/receivables and World claim change, revision increments;
   observe again and show “Agreement signed.” Cash remains unchanged by signing.
8. Rejected/stale: unchanged gameplay; exact reason, fresh projection, explicit
   renewed review required. Never retry automatically.
9. Return to originating company selection; show signed status and a bounded
   Affairs receipt linked to its real agreement cause. Missing matter falls back
   to Business & Finance with explanation.
10. Later advancement through the existing internal UI runs Finance settlement and
    Preparation. Actual wins produce next-day bonus receivables through Finance;
    Commercial separately processes reputation/audience. These are not simulated
    by the workspace or claimed as already realized.

Cancel/back at steps 2–5 submits nothing; after success, Back navigates without
undoing the agreement. Save/load remains the existing Main/Application/schema-1
path and preserves the accepted terms. No fallback workspace or next UX stage
starts in this task.
