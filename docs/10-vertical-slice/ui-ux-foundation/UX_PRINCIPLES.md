# UX principles and review framework

Status: PROPOSAL. Accepted decisions cited here remain binding independently.

| Principle | Required review evidence |
| --- | --- |
| 1. Company-first | Company and scope remain visible in a team/person decision |
| 2. Configurable player identity | Same workspace survives changed name, emblem and colors |
| 3. Decision-first | Primary information explains a choice, not merely a metric |
| 4. Situation-driven | Entry explains why this matter exists now |
| 5. Context before action | Scope, timing, terms and responsible actor precede commitment |
| 6. Progressive disclosure | Summary → evidence → records → history/internal detail |
| 7. One authoritative fact, multiple projections | Owner/revision documented; cross-links do not duplicate facts |
| 8. Dense data remains available | Inspection path exists without dominating every workspace |
| 9. Tables are inspection tools | Company identity/navigation does not mirror database tables |
| 10. Different patterns, one language | Domain interaction varies within common tokens/semantics |
| 11. Perceptible state changes | Accepted action refreshes affected facts and explains differences |
| 12. Visible trade-offs | Costs, benefits, uncertainty and foregone alternatives shown together |
| 13. Unknown looks unknown | Explicit unknown label, no zero or fabricated midpoint |
| 14. Estimates are not facts | Confidence/range, basis and horizon where available |
| 15. Understandable delegation | Who acts, authority, limits, escalation and review are visible |
| 16. Explainable consequences | Decision → owner-applied effect → due/result review can be followed |
| 17. No critical bespoke-art dependency | Every workflow works at character Tier 0 |
| 18. Consistency outranks novelty | Existing patterns reused before proposing a variant |
| 19. Reusable patterns outrank one-off screens | New semantics need an owner and reviewed contract |
| 20. AI follows approved components | Blueprint and component mapping accompany implementation |
| 21. Readability/accessibility before decoration | Keyboard, focus, scaling, contrast and noncolor meaning checked |
| 22. Future compatibility preserves current scope | Future module appears in specification, not as a fake current feature |

## Qualitative complexity budget

The reviewer examines competing primary actions, simultaneous high-emphasis
regions, navigation nesting, unique components, local styling exceptions and
information density. Give one decision coherent emphasis; group secondary
actions by purpose. Nested inspectors must preserve a clear return path. A
workspace requiring several unfamiliar patterns, multiple unrelated commitments,
or repeated modal steps returns to UX review before code or polish.

There is no universal numeric cap. A dense comparison can legitimately show
many rows; an executive situation usually should not. Document why complexity
helps this player question, and test repeated use rather than first impressions.

## Review form (required for every future blueprint)

Record Pass / Revise / Not applicable with evidence and reviewer for each:

| Dimension | Question |
| --- | --- |
| Player purpose | What is the player trying to understand? |
| Decision value | What meaningful decision becomes possible? |
| Information fit | Does each primary field support that decision? |
| Trade-off visibility | Are costs, benefits, alternatives and uncertainty visible? |
| Company context | Is its organizational scope understandable? |
| Feedback | Can the player explain consequences and timing? |
| Consistency | Does it compose the canonical patterns? |
| Player identity | Does it correctly use configurable campaign identity? |
| Art cost | Is bespoke art necessary, and does the text/symbol path work? |
| Technical cost | Is required infrastructure proportionate to this workflow? |
| Repeatability | Does this remain useful without repetitive confirmations? |

Failures return to specification. A screen does not progress to visual polish
while its player purpose or command semantics are unresolved.

## Anti-pattern disposition

Reject dashboard-for-every-domain, universal card grids, page = system = database
table, arbitrary buttons, redundant “View” actions, context-free duplicated
metrics/KPI walls, hidden trade-offs and color implying an inherently correct
choice. Reject modal/notification spam, fixed organization identities/colors,
mandatory AI portraits, local token systems and screenshot-driven guessing.

Reject a Node per simulation entity, giant custom table infrastructure without
measurements and copied concept-image components. A useful existing table or
button is not prohibited: its purpose, semantics and cost must be justified.
