# UI verification strategy

Status: PROPOSAL for future implementation. No UI screenshots or UI Lab are
created during this specification task. Historical evidence is not requalified.

## Future UI Lab

An internal scene catalog should show approved canonical components independently
of live campaigns, consuming fixed actor-safe presentation fixtures. It must not
become a second simulation or production navigation surface. Build it incrementally
with the components actually used by the first approved workspace.

Required component states: default, hover, focus, selected, pressed, disabled,
loading, warning, critical and unknown. Include meaningful combinations such as
selected + focus + estimated, warning + brand accent and unavailable + reason.
Include empty/error, long text, numeric extremes, no emblem and no portrait.

Temporary company branding variants: neutral/absent, bright, dark, low contrast,
same primary/secondary and warning-like. Every variant uses a technical company
identity marked DEVELOPMENT FIXTURE — NONCANONICAL. These are test data, not an
approved palette. Show raw selected colors alongside resolved accessible roles
internally so fallback behavior can be reviewed.

## Golden Screens and ownership

Candidate golden cases are: company operating space with a material situation;
sponsor commitment with trade-offs; preparation with estimates and coach authority;
roster sorted/filtered with ID selection; financial flow with overdue item; result
and timeline with a pending consequence. Add rejected/stale action and absent-art
variants. Establish baselines only after the corresponding blueprint and visual
review are approved. Current dashboard evidence is not the new design baseline.

UI lead owns component/screen goldens. QA owns reproducible capture and comparison.
Director approves architecture/art-direction changes; a designated human UI
reviewer can approve ordinary blueprint-conforming visual changes. These are roles
one developer may fill; AI-generated comparisons alone are not human approval.

Capture fixed 1280×720, 1920×1080 and 2560×1440 viewport cases. These are testing
classes, not minimum hardware or resolution requirements. Record viewport versus
window size explicitly. Fixed captures do not replace actual Windows 100/125/150/
200% scaling and mixed-monitor validation under DV-04.

Each capture manifest records commit/dirty state, pinned engine/runtime, OS,
renderer, viewport/scaling, fonts/theme/component versions, fixture/content hash,
seed, committed revision/checkpoint, route, selected IDs, locale, brand variant,
interaction state and capture trigger. Freeze animation/time at a defined capture
state, wait for layout/query completion and record any masks. Avoid masking text,
focus, warnings, terms or identity. Keep before/after/diff and review rationale.

## Update procedure

1. Reproduce existing baseline from its manifest; distinguish environment drift
   from an application change before evaluating pixels.
2. Run relevant behavior checks, then capture changed scenarios/brand variants at
   fixed viewports. Compare structurally as well as visually.
3. For intentional changes, link approved blueprint/token changes and explain
   effects on all shared-component consumers; obtain named human visual review.
4. Update golden and manifest together in the reviewed change. Do not blindly
   accept diffs or use a universal pixel threshold to excuse missing information.
5. Accidental clipping, wrong identity, missing uncertainty/focus or hidden terms
   blocks that presentation increment. Fix/revert it and retain the working
   internal interface. Record environmental-only differences separately.

An initial comparison tool may produce image overlays/diffs; sophisticated image
automation is not a prerequisite. Any tolerance for antialiasing must be narrow,
documented and visually reviewed. Brand variants cannot overwrite one another's
baseline or be “fixed” by changing the campaign's requested color.

## Functional and architectural checks

| Concern | Required evidence on affected implementation |
| --- | --- |
| Ownership | UI consumes observation/read contracts; commands use Application; no Domain mutation from controls |
| Information | Facts/estimates/unknown preserved through display, sorting and drill-down; repeat queries do not reroll |
| Identity | Name/color/emblem changes propagate, survive save/load, leave command targets/gameplay semantics unchanged |
| Command safety | Correct stable entity after sort/filter/page/rebind; stale revision rejection; duplicate receipt idempotency |
| Navigation | Cross-link/back restores valid draft, ID, scope and focus; load/new campaign clears incompatible state |
| Time/affairs | Stop reason matches Application; only blocking interrupts; normal result checkpoints preserved; no duplicate matter spam |
| Consequence | Accepted terms versus due settlement distinguished; causes, pending effects and date visible without hidden truth |
| Input/accessibility | Keyboard-only completion, physical mouse/keyboard, visible focus, text expansion, contrast and reduced motion |
| Lifecycle | No duplicate handlers/subscriptions after repeated navigation; bounded live controls and records |

## Performance and deferred gates

**DECISION:** DEC-022 targets approximately 60 Hz / 16.7 ms continuous scrolling
and direct navigation where practical; provisional p95 frame work around 33.3 ms
for discrete rebind/page/sort/filter/layout transitions; <=100 ms p95 ordinary
action acknowledgement excluding deliberate debounce; query p95 <=250 ms at 10k
and <=1 s at 100k. Measure debounce separately. These are engineering targets,
not approved minimum hardware or a claim the current client passes.

Measure affected Release workflows with build/machine identity, repeatable data,
warm-up/sample counts, raw timings, work time versus wall-frame time, live
controls, retained memory and stable-ID correctness. Include repeated navigation
and rebinding; investigate monotonic retained-control/subscription growth. Use
existing bounded query/row tooling before inventing new infrastructure. Correctness
cannot be traded for a timing pass. Large-scale synthetic queries do not prove
rendered usability or campaign scale.

Carry DV-03/04 physical input/DPI, DV-05 hardware/measurement acceptance, DV-07
campaign scale and DV-09 UI regression evidence into affected milestones.
DV-01 distribution, DV-02 debugger, DV-06 second-machine determinism, DV-08 save
compatibility and DV-10 upgrades remain under the [closure register](../../09-technical-foundation/PHASE4_CLOSURE.md).
No gate is discharged here. QG-02's historical FAIL stays unchanged; no Phase 4
synthetic requalification is requested.

## Verification of this documentation task

Check package completeness against the brief; local Markdown targets; all module
and taxonomy matrix fields; governance status and decision preservation; no final
palette/font or invented mechanics; documentation-only diff; and unchanged
pre-existing implementation file hashes. Gameplay test reruns do not establish
the quality of a specification and are unnecessary when code/content are untouched.

Executed 2026-10-04: all 19 required documents plus SOURCE_REVIEW are present;
46 package-local Markdown links resolve; all ten module matrices contain all 13
required fields; all ten taxonomy entries contain all eight required fields;
code fences are balanced. DEC-001–022 were compared with the baseline and retain
their original text. SHA-256 comparison of 334 pre-existing files found only the
two intentionally edited documentation files changed; 332 other files, including
the dirty implementation, content, tests and evidence, remain unchanged.
Documentation whitespace and staged scope are checked before commit. No runtime
tests, screenshots, generated art or UI-demo implementation were performed.
