# Interaction model

Status: PROPOSAL. The canonical simulation cycle remains DEC-013.

## Hybrid model and roles

| Surface | Purpose | Interaction constraint |
| --- | --- | --- |
| Corporate operating map | Orient company, supported functions and active business relationships | Select scope/situation; never a decorative mandatory traversal or all-entity graph |
| Contextual workspace | Investigate and make a coherent decision | Keep company, scope and originating situation visible |
| Document / entity | Inspect a person, offer, contract or report with provenance | Object is directly navigable; actions depend on legality and authority |
| Timeline / affairs | Understand timing, consequences and matters needing attention | Reflect authoritative chronology and checkpoint semantics |

Start with a bounded structural map plus accessible list/matrix equivalent.
Compare a hierarchical portfolio map (clear ownership), function/scope matrix
(clear shared services) and relationship graph (clear dependencies but expensive
at scale) in a later low-fidelity prototype. The preferred hybrid combines a
small ownership map with contextual cross-links. Do not build all three systems.

## Decision loop

Situation → Context → Information → Comparison → Decision → Commitment →
Consequence → Review → Adaptation.

This is a presentation lens over DEC-013: situation/context/information support
Observe and Prioritize; comparison supports Decide; commitment uses Commit and
Delegate/Intervene; advancing/resolution produces Consequence; Review/Adapt remain.
It never replaces the daily simulation cycle or omits time advancement.

Example: sponsor offer approaching deadline → company/discipline scope → signed
payment terms and current workload → compare accepting versus leaving available
→ select → review material commitment → submit SponsorDecision → accepted terms
and scheduled receipts → later settlement/preparation review → adjust priorities.
Leaving available is no command; it is not a new “reject sponsor” mechanic.

## Workspace and command lifecycle

Every meaningful decision exposes situation, entity, options, evidence,
uncertainty, costs/benefits, resource commitments, expected timing, authority and
reversibility. Staff advice is evidence with a rationale, not a highlighted correct
answer. The current three coach modes and risk ceiling retain their actual rules.

1. Observe an immutable actor-safe projection at a committed revision.
2. Edit a local draft. Drafts do not alter cash, ownership, time or eligibility.
3. Review material irreversible terms inline; use confirmation where the cost or
   replacement risk warrants it. Do not require a modal for every routine action.
4. Submit one typed command through Application with stable target IDs, expected
   revision and unique request ID; attach checkpoint context when supported.
5. Pending disables duplicate activation. Application idempotency remains the
   authority. Do not invent a successful optimistic gameplay mutation.
6. Acceptance shows acknowledgement and a refreshed revision. Rejection explains
   invalid/stale terms, retains safe draft data and requires renewed review.
   Never silently resubmit a stale commitment at a new revision.

On cross-navigation retain draft/selection/return context locally. Returning must
revalidate legality. Back/Escape closes a transient inspector before leaving its
origin; leaving a dirty draft offers keep/discard within the session. Load/new
campaign invalidates old drafts. Closing a view never undoes a committed command.

## Affairs classification and time

Priority and matter class are separate. A high-value opportunity can be actionable
without blocking. Application/simulation determine stops, never CSS-like urgency.

| Class | Meaning | Behavior |
| --- | --- | --- |
| BLOCKING | Required legal decision or mandatory safe-boundary checkpoint | Explains stop reason, authority and resolution; only this class may automatically interrupt ongoing advance |
| ACTIONABLE | Optional response with material choice | Shows why, deadline, entity and cost of ignoring; no automatic stop |
| INFORMATIONAL | Relevant change without required action | Aggregate into review/affairs; no automatic stop |
| HISTORICAL | Resolved or dated record | Available for inspection with date; no alert or automatic stop |

Normal completion of “advance to next checkpoint” is distinct from interruption.
The existing post-match review return and fixture completion stay valid. Required
preparation/escalation and distress stops are shown as blocking when Application
reports them. A future deadline stop policy must be specified in Application and
reviewed; this package does not add arbitrary-date travel or new scheduler rules.

Time presentation shows current date, known next checkpoint, important due items,
pending effects and the reason for stopping. Acknowledge-only dismissal clears
local attention, never a simulation blocker. Group notifications by cause and
entity, refresh an existing matter rather than adding one every tick, and retain
unresolved obligations after the player closes an inspector. Routine settlements
belong in a digest; changed risk status can surface a new situation.

## Consequence feedback

| Horizon | Feedback | Evidence rule |
| --- | --- | --- |
| Immediate | Command accepted/rejected; terms and current changed facts | Receipt/revision; no claimed outcome before owner commit |
| Short-term | Next relevant checkpoint shows actual changes and pending effects | Link cause/entity and due date; separate expected from realized |
| Long-term | Trend/history and accumulated exposure when records exist | Dated observations, consistent horizon; no fabricated historical series |

Finance settles before matches in the current cycle. Earned prizes/bonuses may
be receivable later; they cannot retroactively pay an earlier obligation. Do not
turn every mutation into a popup or reveal hidden competitive inputs in explanations.

## Navigation and keyboard

Use the [hierarchy/route contract](INFORMATION_ARCHITECTURE.md). Tab/Shift+Tab move
through meaningful controls, arrows inspect lists, Enter activates the focused
action and Escape restores context. Focus is visible and restored by stable ID
after rebind; if missing, move to a labelled containing region. Text editing
suppresses global time-advance shortcuts. Tooltips are available on focus; context
menus have a keyboard equivalent and never contain the only path to a critical
action. No drag-only map navigation or hover-only costs.
