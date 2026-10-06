# Sponsor interaction

Main → **Company context · live sponsor workspace** opens a CompanyHost sharing
Main's actual IGameSession. Default is the structured company list. Select Business
& Finance's sponsor matter and open its decision entry. Optional map uses the same
MapSnapshot, navigation and entry callback. The standalone Stage 3 fixture scene
remains a labelled fixture; it does not become a second sponsor implementation.

SponsorWorkspaceView consumes SponsorPresenter, which emits the existing typed
Request through ISponsorSession. Shared components only emit typed UiIntent.
Review acceptance is local; the inline material confirmation describes identity,
revision, scheduled money, immediate load, end day, uncertainty and no termination.
The final CommitmentReview activation sends one command. Focus moves to Back after
opening confirmation, requiring deliberate focus of the final action; key repeats
on the former review action cannot accidentally confirm.

Presenter states: Review → Confirm → Pending → Accepted/Rejected. Pending blocks
re-entrant Refresh and Submit even if the view is rebound. Application is
synchronous, so pending lasts the synchronous command; there is no artificial
delay. The kit immediately disables its commitment control. Existing Application
receipts remain the only gameplay idempotency authority; Guid request IDs consume
no gameplay RNG. Acceptance refreshes data and disables further acceptance.

Any real Application rejection displays its exact response message, refreshes
actor-safe data and returns to a fresh review opportunity if still legal. A stale
action is submitted with the reviewed revision and rejected by Application, not
silently upgraded or automatically retried. Missing/expired/claimed/ineligible/
full-slot/finished data cannot enable the final action.

Tab/Shift+Tab traverse native controls; Enter/Space activate focus. Escape marks
input handled before removing the view: confirmation → review → origin. Reading
regions support PageUp/PageDown/Home/End and follow focus. Critical contract terms
and actions remain outside any unexpected fold; supporting records may scroll.
Color never decides whether acceptance is good; costs and benefits use neutral
document/comparison semantics. Company and sponsor names are observed text.

Return restores list/map mode, owning scope, matter ID, focus and scroll after
native layout. Removed matter selects nearest Business & Finance context and
explains the missing situation. Load/new campaign in Main cannot reuse an open
workspace: the contextual host must first be closed and is disposed on return.
Opening again creates a new session-local navigation instance and observation.

Repeated render removes the old subtree and clears EntityLabel binding callbacks
on ExitTree. No per-frame gameplay query, UI simulation, optimistic cash update,
timer-driven resubmit, reusable decision framework or independent save store.
