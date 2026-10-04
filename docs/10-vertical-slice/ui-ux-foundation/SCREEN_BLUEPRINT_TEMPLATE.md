# Screen / workspace blueprint template

Status: PROPOSAL. Copy the template into a future screen specification; complete
every field or explain “not applicable.” It is not an artifact-template skill.
Blueprint approval precedes UI implementation; a mockup is optional subordinate
reference, never the source of gameplay rules.

```text
SCREEN / WORKSPACE NAME:
Blueprint ID / version:
Status: Draft / Reviewed / Approved / Superseded
Owner / reviewers / approval record:
Source decisions / UX architecture / component versions:
Current versus future scope:

Role:
Type (screen taxonomy):
Hierarchy position (company, function, portfolio scope):
Player question:
Primary decision (or inspection purpose):
Entry conditions / originating situation:
Exit conditions / return destination:

Required information:
  Primary:
  Secondary:
  On demand:
  Historical / raw (player-safe versus internal):
Entities (stable IDs and ownership):
Read models (fields, source owners, revision, observation boundary):
Commands (exact existing types; proposed additions clearly marked):
Outcomes (immediate, next checkpoint, long term):
Uncertainty (known / estimated / unknown, confidence, age, basis):
Authority / delegation / escalation:
Trade-offs / timing / reversibility:
Attention priority / matter class / reason:

Canonical components (version and any reviewed gap):
Layout regions and density / reflow rules (not a final pixel design):
Navigation behavior / cross-links / return to decision:
Keyboard behavior / focus restoration / shortcut suppression:
Draft lifetime / cancellation:
Submission, pending, acceptance, rejection and stale refresh:

Empty state:
Loading state:
Error state:
Unavailable state:
Performance expectations / bounded visible data / measurement:
Accessibility / text expansion / reduced motion:
Player-branding behavior / identity fallbacks:
Deferred art / Tier 0 representation:
Anti-patterns:
Acceptance criteria (Given / When / Then):
Automated checks:
Golden capture cases / viewport / brand / states:
UX review framework (all 11 dimensions with evidence):
Open questions / dependencies / migration and rollback:
```

## Worked semantic example: sponsor commitment review

**PROPOSAL — illustrative blueprint, not approved implementation.** Company scope
→ Business & Finance → Sponsorship → Offer → Decision workspace. Player question:
“Is this scheduled income worth its delivery load and future constraints?” Entry:
select an available observed offer from affairs or the sponsor workspace. Exit:
accepted commitment review or return to the originating situation; closing without
accepting does not reject the offer in simulation.

Primary: offer ID/name, availability, deadline, payment/bonus terms, load and
accept/leave trade-off. Secondary: company Cash, committed forecast, current
capacity/load and preparation implications. On demand: due items/source terms.
Historical: dated settlements if available, not technical receipts in ordinary UX.

Entities: World offer; Company signed contract/Finance items after acceptance.
Current fields come from `Situation.Offers`, Cash, Forecast, Load and Capacity at
one revision. Current `SponsorRow` lacks complete end-date/cadence terms shown by
the client using literals: extend the actor-safe term projection before replacing
that wording. Do not infer payment schedule from a screenshot or repeat literals.

Command: existing `Request(id, revision, new SponsorDecision(offerId))`. This is
fixed-term acceptance, not negotiation. Recommendation/forecast is not guaranteed
profit. Player controls this material commitment; no new commercial delegation.
Immediate outcome: signed terms and receivables, not immediate Cash. Short-term:
scheduled settlement and changed load. Long-term: review supported obligations
and effects; do not fabricate a season history. High/actionable while available;
not blocking solely because an offer is valuable.

Compose NavigationContext, DocumentView, ResourceValue, ComparisonView,
CommitmentReview and ValidationMessage within DecisionWorkspace. Regions are
situation, terms/comparison, supporting evidence and commitment. Stack evidence
at narrower sizes without hiding material terms. Finance cross-link preserves
selected offer/draft/return route; coming back revalidates its revision.

Tab order follows terms → evidence links → commit/cancel. Enter activates focus;
Escape returns safely. No implicit acceptance on selection. While pending disable
duplicate activation; rejection retains reviewed selection and explains expiry,
claim or changed eligibility. Require fresh review before retrying stale terms.

Empty: no offers, with route back. Loading: no actionable stale terms. Error:
failed projection/command explains retry without mutating state. Unavailable:
expired/claimed offer remains inspectable as dated context with acceptance disabled.
Bound related bills to the relevant horizon/page. Target acknowledgement follows
DEC-022; no all-history rebind. Text labels and visible focus convey all states.
Company identity comes from campaign projection with safe brand accents; Tier 0
symbols suffice and no sponsor art is required.

Acceptance examples:

- Given an available offer, acceptance creates terms/items once and does not
  immediately add scheduled payment to Cash.
- Given an offer claimed after inspection, stale acceptance changes nothing and
  explains the new availability without selecting a different offer.
- Given a finance cross-link and return, the same offer ID remains selected; the
  draft is revalidated before commitment.
- Given a long renamed company and warning-like brand color, all terms, warning
  labels and keyboard focus remain readable.

Automated checks cover command routing, revision rejection, selection, safe
projection and owner outcomes. Later captures cover available, pending, rejected
and unavailable with brand variants at the candidate viewport classes. Review
all eleven [UX dimensions](UX_PRINCIPLES.md); review status is pending for this
example. Missing structured terms is the key dependency. Rollback retains the
internal sponsor interface and existing commands.
