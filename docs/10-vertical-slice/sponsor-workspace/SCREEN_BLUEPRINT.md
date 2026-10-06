# Sponsor / Commercial Commitment — SW-01 v1

Status: REVIEWED for implementation within the Director's explicit Stage 4 brief.
The brief approves this bounded workspace, exact action and regions; no additional
gameplay/identity scope is requested. UX/Application/UI/QA role review completed
against the audit before BUILD. Final human visual/Director acceptance remains
pending; this is not a claim of separate human blueprint sign-off.

Authority: DEC-023, DEC-022; accepted Stage 3.1 LIST-FIRST / MAP-OPTIONAL disposition.
Phase 5 remains open; no new DEC. Stage 2 engineering-neutral-v1 components/theme.

Role: CEO reviewing a business commitment. Type: Decision + Document workspace.
Hierarchy: campaign Company → Business & Finance → Sponsorship → offer → commitment.
Question: “Is this commitment worthwhile for my company now?” Decision: accept
the exact current offer, or return without a command.

## Information and layout

- Compact company/day/checkpoint/path header, text identity and no required emblem.
- Two reading columns at 720p/1080p: commercial document with payment dates, end,
  bonus condition, ongoing delivery load and deadline; company decision evidence
  with current cash, load/capacity, forecast and unknowns.
- Always-visible bottom review/back area. Material confirmation replaces the
  action region inline, using CommitmentReview; one explicit final commit.
- Supporting comparison/receivable details scroll vertically. No horizontal
  scrolling. Extra 1080p height reveals evidence rather than unrelated metrics.
- Known terms versus estimated seven-day forecast versus unknown future wins
  remain explicit. Never project guessed revenue, strength or preparation loss.

Immediate: signed agreement, claim, load and scheduled receivables; no cash receipt.
Future: Finance settlement on due days; load affects future preparation; actual
wins create next-day bonuses. No UI calculation of authoritative consequences.
Acceptance consumes a limited active slot; no termination command exists.

## Contracts and lifecycle

Application-owned structured sponsor snapshot includes campaign/company identity,
revision/day/checkpoint, observed offers/terms, current commitments, receivable
dates, current resources and forecast. Explicit player-only projection; no hidden
World/competitive inputs. Offer IDs and agreement IDs remain distinct. No Domain
objects enter screen controls. Full identity/save migration is out of scope.

Entry retains offer ID/revision, originating matter/scope, map/list mode, focus and
scroll. Both representations route through one callback to the same presenter.
Navigation defaults to list. Existing fixture prototype remains separately runnable.
Main's internal tabs remain available; workspace shares its actual session.

Presenter handles typed review/commit/back intents. A review is local. Commit uses
existing Request + SponsorDecision; pending latch spans rebinding and prevents
duplicate dispatch. Existing Application receipts remain the sole gameplay
idempotency mechanism. Acceptance/rejection refreshes actor-safe state. Stale
rejection shows original reason and requires a fresh review; never auto-resubmit.
Rebinding releases old callback targets. New/load session disposes the workspace.

Escape exits confirmation to review first, then returns to exact valid origin;
back submits nothing. Missing offer returns to Business & Finance with explanation.
Empty/unavailable data has no enabled commit. Loading/pending disables commit.
Application errors retain exact messages. Tab/Shift+Tab, Enter/Space, focus ring,
keyboard scrolling and native pointer use Stage 2 semantics; reduced motion.

## Component and complexity review

Compose DocumentView, ComparisonView, ResourceValue, ConfidenceIndicator,
SectionHeader, TimeMarker, EntityLabel, ValidationMessage and CommitmentReview.
Reuse compact Stage 3.1 company context; do not restore oversized NavigationContext.
No new reusable framework/component or art. Bounded current fixture offers and
contract evidence; measure initial bind, rebind, confirmation, acknowledgement,
refresh, repeated enter/back and retained Controls against DEC-022 targets.

Purpose, decision value, information fit, trade-offs, company context, feedback,
consistency, live identity, no-art viability, technical cost and repeatability:
implementation review passes by audit/design; verify each against built evidence.

Acceptance: valid commitment updates live observation once; no immediate Cash;
stale/removed offer cannot mutate; cancel/back preserves hash; IDs survive rebinding;
signed state survives schema-1 save/load; internal UI and regression hashes survive.
Golden candidates: review, uncertainty, confirmation, success, stale/rejection,
long names at 720p and standard 1080p. Synthetic input does not close DV-03/04.
Rollback: retain internal sponsor tab and standalone map; remove new entry path.
