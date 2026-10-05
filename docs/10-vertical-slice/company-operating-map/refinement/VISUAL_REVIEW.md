# Visual candidate review

Reviewer: Codex in assigned UX/UI engineering role, 2026-10-05. Native PNGs
inspected using the image viewer. **No human usability session, physical-input
trial, Director approval or production golden acceptance is claimed.**

Exactly 20 candidates: five states × two views × two render viewports. Preparation
uses the existing `competitive` fixture key. Long-name images show the full name
in company inspection with alternate low-contrast branding and normal situation
density. Native task 5 separately exercises both handoffs with that identity.

| State | Map 720p | Tree 720p | Map 1080p | Tree 1080p |
| --- | --- | --- | --- | --- |
| Normal | [PNG](evidence/candidates/map-normal-1280x720.png) | [PNG](evidence/candidates/list-normal-1280x720.png) | [PNG](evidence/candidates/map-normal-1920x1080.png) | [PNG](evidence/candidates/list-normal-1920x1080.png) |
| Sponsor | [PNG](evidence/candidates/map-sponsor-1280x720.png) | [PNG](evidence/candidates/list-sponsor-1280x720.png) | [PNG](evidence/candidates/map-sponsor-1920x1080.png) | [PNG](evidence/candidates/list-sponsor-1920x1080.png) |
| Preparation | [PNG](evidence/candidates/map-competitive-1280x720.png) | [PNG](evidence/candidates/list-competitive-1280x720.png) | [PNG](evidence/candidates/map-competitive-1920x1080.png) | [PNG](evidence/candidates/list-competitive-1920x1080.png) |
| Quiet | [PNG](evidence/candidates/map-quiet-1280x720.png) | [PNG](evidence/candidates/list-quiet-1280x720.png) | [PNG](evidence/candidates/map-quiet-1920x1080.png) | [PNG](evidence/candidates/list-quiet-1920x1080.png) |
| Long identity + alternative brand | [PNG](evidence/candidates/map-long-name-1280x720.png) | [PNG](evidence/candidates/list-long-name-1280x720.png) | [PNG](evidence/candidates/map-long-name-1920x1080.png) | [PNG](evidence/candidates/list-long-name-1920x1080.png) |

Each PNG has an adjacent JSON manifest with fixture hash, source digest, viewport,
mode/selection, renderer/font, geometry and image hash. `map-normal` at each size
also records the complete native suite and both representations' task proxies.
Other candidates have layout verification only; an empty Checks array in those
manifests does not mean a second full suite ran. Full native coverage is explicit.

Findings: the shared header keeps identity/time/context visible; the full Unicode
name wraps legibly in inspection. Sponsor reason, consequence, uncertainty basis
and entry all fit at 720p. Preparation retains Unknown and an explicit explanation,
without a fabricated win probability. Quiet has normal ongoing matters and a
no-urgent message. Status and selected focus remain distinct from branding.

Map lanes clearly separate owned structure from functions, with explicit support
labels. The tree uses fewer surfaces and one reading sequence, but its stacked
matter groups use more height. At 720p selected tree states partly clip the last
Affairs function row; the persistent Affairs entry remains visible. At 1080p both
leave substantial empty space; filling it with extra data would expand scope.

Historical Stage 3 sponsor rendering was inspected for comparison. The old entry
action lay below the inspector fold; the refinement makes it visible. Initial
local iterations caught a narrow identity glyph wrapping vertically and list
scroll restoration before layout; both were corrected before this candidate set.

Purpose, information fit, company context, consistency, player identity and art
cost are supported by these renders. Trade-offs and uncertainty are readable;
decision value remains limited to a read-only handoff. Navigation feedback and
repeatability have synthetic checks. Technical cost has bounded node/timing
evidence. Human comprehension, company feel and repeated-use preference are open.

Recommendation remains list-first/map-optional. Visual neatness alone is not
evidence of human superiority. All candidates retain the provisional theme and
require Director review; no historical golden or DV/QG status was replaced.
