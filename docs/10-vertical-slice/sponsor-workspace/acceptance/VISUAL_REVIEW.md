# Visual regression — GOLDEN CANDIDATES

Seven final images were captured and visually inspected during the 2026-10-06
engineering pass. They are **GOLDEN CANDIDATES**, not human-approved golden images.
Source: `043ad9bee1c149eb9cce407a419a1e5eda6f8799`; per-image hashes, actual viewport,
source digest, snapshot, phase and checks are in the adjacent JSON manifests.

| Candidate | Engineering observation |
| --- | --- |
| [720p review](evidence/candidates/review-1280x720.png) | DEV_ORG_001 and noncanonical notice visible; amount/dates/end/load/bonus/deadline above fold; actions and focus visible. |
| [720p confirmation](evidence/candidates/confirmation-1280x720.png) | Core terms remain visible; explicit commitment and keep-reviewing actions visible; lower supporting evidence scrolls. |
| [720p success](evidence/candidates/success-1280x720.png) | Signed state, Cash 2,600 CU unchanged, load 90/80, day-4 receivable and disabled signed action clearly shown. |
| [720p stale/rejected](evidence/candidates/stale-1280x720.png) | Error tone/copy visible; current data refreshed, fresh explicit review required; context preserved. |
| [1080p review](evidence/candidates/review-1920x1080.png) | Full terms, comparison and evidence readable, actions anchored and focus visible. |
| [1080p confirmation](evidence/candidates/confirmation-1920x1080.png) | Terms, trade-off and explicit action readable with full context. |
| [720p long identity](evidence/candidates/long-1280x720.png) | Synthetic presentation-only long identity/title, including Vietnamese diacritics, wraps or fits without horizontal overflow; no fixture content change. |

Automated bounds checks pass in every candidate: no visible horizontal workspace
overflow, Back and main action inside viewport, complete core document inside the
terms scroll viewport. Review variants additionally execute the full 29-check native
input/regression sequence at both resolutions.

Known present Cash/terms, Estimated committed forecast and Unknown future wins are
preserved. Error, estimate, focus and neutral colors use the existing semantic
tokens; no brand color overrides were added. Company and Business & Finance context
remain visible. At 720p comparison/supporting text below the fold intentionally
scrolls; keyboard scroll is verified. Stage 4's overall style and layout are retained.

The Director still needs to perform the [five-task protocol](DIRECTOR_REVIEW_PROTOCOL.md)
and record actual comprehension, preference and approval. This visual inspection
does not establish physical-input, DPI, accessibility or final art acceptance.
