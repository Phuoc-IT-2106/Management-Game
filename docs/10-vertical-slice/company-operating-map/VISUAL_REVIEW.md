# Visual and usability review

All images are **GOLDEN CANDIDATES — NOT APPROVED**. Reviewer: Codex in the assigned
UX/QA role, inspecting native renders and behavior evidence. No human usability
session, physical-input trial or Director visual approval has occurred.

## Findings

The company name, emblem fallback, fixture notice, current day and next checkpoint
remain visible in every mode. The full selected path explains where a situation
belongs even when the map scrolls. Distinct headings and scope kinds separate
owned portfolio from corporate functions. Labelled supports/serves relationships
avoid ownership ambiguity without drawing a web of edges. No art is required.

At 720p, the map and inspector scroll independently. Primary team preparation is
near the fold; focusing it scrolls it into view. The company inspector also offers
all four direct situation links. Long names wrap without horizontal clipping.
Empty state fits the complete branch and functions, with no invented alert.

The inherited identity block uses considerable vertical space; sponsor evidence
and its entry action require inspector scrolling at 720p. This is functional but
deserves an observed task test before acceptance. Expanded Affairs competes with
the map for vertical space, hence its collapsed default and persistent checkpoint
summary. Label repetition (root + breadcrumb + inspector) helps orientation but
may be reducible. Do not solve density by shrinking typography or hiding uncertainty.

Recommendation: **ITERATE OPERATING MAP MODEL**. The bounded hybrid is viable and
does not trigger a structural stop condition, but neither visual inspection nor
synthetic input establishes superiority to the list or tabs. Keep both representations
until a Director/human task comparison. Do not proceed to Stage 4 in this task.

## Director questions and evidence

| Question | Engineering observation / remaining decision |
| --- | --- |
| 1. Company clearly root? | Persistent configured identity; company owns competitive branch; entry retains company context |
| 2. Function/portfolio/division/team/situation distinct? | Two labelled lanes; textual kinds and attached decision markers; test comprehension with user |
| 3. Important matters found quickly? | Two High markers and direct company-inspector links; actual search time unmeasured |
| 4. Better than flat tabs? | Context and return are explicit; comparative human evidence pending |
| 5. Avoid web dashboard? | Ownership/support relations, no KPI cards/charts or flat module tabs; aesthetic judgment remains human |
| 6. Avoid decorative graph? | Fixed small branch, meaningful support labels, no arbitrary layout or edge framework |
| 7. Understandable at 720p? | No horizontal overflow across all cases; selected path and identity retained; scrolling cost remains |
| 8. Inspector enough context? | Scope, reason, status, date, consequence, uncertainty and direct entry; no fabricated values |
| 9. Affairs helps or noise? | Scope linkage works; expanded density suggests leaving collapsed until needed |
| 10. Scale conceptually beyond esports? | Company/functions are independent of branch labels; scale not implemented or load-tested |
| 11. Usable without art? | Text, native focus, symbols, semantic surfaces and brand fallback cover every path |
| 12. Cost proportional? | Zero new reusable components; eight scopes/four matters; bounded native containers and local presenter |

## Heuristic review

| Heuristic | Disposition |
| --- | --- |
| Orientation | Engineering pass: company + path persist |
| Hierarchy | Engineering pass: explicit contains and types; user explanation pending |
| Attention | Revise/validate: limited emphasis; find-time not measured |
| Context | Engineering pass: why, time, consequences and uncertainty visible/readable |
| Navigation | Engineering pass: direct selection and one entry action |
| Return | Verified both commercial/noncommercial, list and Affairs origins |
| Density | Iterate: 720p scrolling and expanded Affairs compete for space |
| Consistency | Pass: Stage 2 components/theme unchanged |
| Art cost | Pass: no custom assets |
| Implementation cost | Bounded, no generalized infrastructure; metrics in verification |

Foundation's eleven dimensions: player purpose, information fit, company context,
consistency, configurable identity and art cost have direct visual evidence;
decision value/trade-off visibility show handoff context only; feedback describes
navigation with no pretend accepted outcome; technical cost is measured locally;
repeatability is synthetic, awaiting human comparison. No gameplay decision-quality
claim follows from a read-only stub.

## Director task protocol

Use fresh participants or counterbalance map/list/tab order. Record completion time,
wrong turns, assistance, and a short explanation, without inventing results:

1. Identify the company, owned team ancestry, and which functions support it.
2. Find the sponsor matter, explain its scope/trade-off/uncertainty, enter and return.
3. Find preparation through the team, then through Affairs; explain its uncertainty.
4. Switch to list; confirm the same selection; repeat without the old tab hierarchy.
5. In quiet/empty, identify the next checkpoint and explain absence of urgency.
6. Repeat at 720p with long name and low-contrast branding.

Compare the existing internal UI on equivalent supported observations, recognizing
its live data differs from the presentation fixtures. Accept only after users can
answer the brief's eight orientation/context/return questions. Prefer the list if
the map adds navigation cost without comprehension value. No fabricated user scores.
