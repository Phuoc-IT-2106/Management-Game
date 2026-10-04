# Information priority

Status: PROPOSAL. UX classification only; no gameplay balance constants.

## Attention heuristic

For each situation assess decision relevance, consequence magnitude, urgency,
player responsibility, uncertainty and reversibility. A conceptual shorthand is:

`AttentionPriority ~ relevance × consequence × urgency × responsibility`

Do not implement this as a numeric gameplay formula. Use ordered categories with
written reasons. Responsibility includes accountable delegated work; it is not
zero merely because a coach can act. Uncertainty increases the visibility of
evidence limits; irreversible exposure increases the need for review. Neither
makes a choice inherently good/bad nor changes underlying probabilities.

Evaluate required blockers first, then expiring/material decisions, then other
current choices, then background context. Within a class use relevant deadline,
scope and a stable ID tie-break. Do not reorder a focused item under the player's
cursor during interaction; announce refreshed matters after the current action.

| Priority | Definition / example | Permitted presentation |
| --- | --- | --- |
| Critical | Material required intervention, e.g. current distress stop or illegal required lineup | Persistent stop reason and focused route to resolution; critical label/icon; never relies on brand color or repeated modal |
| High | Material near-term opportunity or commitment requiring attention, e.g. sponsor deadline | Emphasized affairs entry, deadline, trade-off and direct decision link; no automatic interruption solely for priority |
| Normal | Relevant comparison or choice without immediate material loss | Normal workspace/affairs placement, inspectable evidence |
| Background | Context/history without current decision relevance | On-demand detail or grouped review; no flashing badges or blocking dialog |

Priority does not conceal information or alter command availability. Domain rules
remain authoritative. Missing deadline or confidence is shown as unknown, not
fabricated to permit ranking. Explain ranking with “payment due before competition”
or “offer expires at the shown day,” not an opaque score.

## Density and disclosure

| Layer | Purpose | Sponsor commitment example |
| --- | --- | --- |
| Primary / Level 1 | Current decision and situation summary | Offer identity, deadline, scheduled income, load, accept/leave trade-off |
| Secondary / Level 2 | Relevant evidence and comparison | Cash forecast, existing delivery load, preparation constraints |
| Tertiary / Level 3 | Detailed records on demand | Agreement clauses, due items, eligibility explanation |
| Historical/raw / Level 4 | Dated history, audit records; debug separately | Prior settlements and cause chain; technical receipts only internally |

A player can stop at the depth needed. Material costs, uncertainty and authority
cannot be hidden at Level 3 to make the primary surface look simpler. Ordinary
decisions must not require raw logs or knowledge of execution IDs.

## Fact and uncertainty display

Known facts show units and effective date when necessary. Estimates carry an
estimate label, range/category and confidence/basis if supplied. Forecasts name
their horizon and included/excluded commitments; they are not guaranteed receipts.
Unknown uses an explicit label and reason if known. “Not applicable,” “no records,”
“unavailable,” “loading” and numeric zero are different states.

Multiple projections of the same cash balance must reference the same revision.
Avoid repeating a metric unless the local decision needs it; identify company vs
scope and period to prevent accidental aggregation or false comparison. Unknown
values cannot become discoverable through sorting, filtering, tooltips or exports.

**ASSUMPTION:** This qualitative ranking will reduce competing emphasis. Validate
with repeated preparation/sponsor/roster cycles before adopting automatic ranking
beyond the current explicit checkpoint and deadline data.
