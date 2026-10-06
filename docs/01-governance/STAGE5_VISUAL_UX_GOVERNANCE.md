# Stage 5 visual / UX design governance

Status: **ACTIVE — explicit user instruction**, received 2026-10-06 (Asia/Saigon).
Applies to Stage 5 presentation specifications, implementation and review under
DEC-023. Phase 5 remains open.

The [complete supplied brief](STAGE5_VISUAL_UX_BRIEF.txt) is preserved byte-for-byte
and is the controlling text for all 50 rules and the 14-part design review gate.
This page integrates that instruction into the repository; it does not replace
or abbreviate its requirements. Source: attachment `pasted-text-1.txt`, attachment
ID `b5175650-5b00-49f2-b3b0-c11fee4fa981`.
Source SHA-256:
`d361584431b805280d2daea02b9a3ba29f7005ab715912b33613d3ee8f98c6ee`.

Later interpretation authority (2026-10-06):
[Stage 5 visual composition direction](STAGE5_VISUAL_COMPOSITION_DIRECTION.md),
**ACTIVE — DIRECTOR CLARIFICATION**. The preserved brief remains byte-identical.
List-first governs structured navigation, not visual dominance. Company home is
a company operating/command space; reject both generic SaaS dashboards and
engineering/admin/debug composition. Useful panels and semantic operating
visualization are permitted under that contract. No DEC text or historical
Stage 3/4 finding is superseded; no Stage 5 implementation is authorized here.

## Authority and scope

Gameplay decision → user context → information priority → interaction → layout
→ visual style → decoration. Accepted gameplay decisions and company-first UX
architecture govern screen design. Screenshots and generated mockups are
low-authority references. The company remains above its teams, people,
competitions, sponsors and documents.

The instruction establishes design constraints and acceptance requirements. It
does not accept an existing screen, approve final art direction, prescribe a new
screen, expand gameplay, or authorize a new navigation paradigm. DEC-001–023 and
Company/World ownership remain intact. Stage 4 engineering acceptance and pending
human review retain their recorded status; historical captures are not Stage 5
passes. No new DEC or visual approval is inferred from this integration.

Older Stage 1 proposal/no-implementation labels describe that package's original
delivery scope. DEC-023 and subsequent explicit stage instructions control current
authorization. This governance applies now without promoting illustrative
blueprints or provisional tokens to approved production designs.

## Apply before implementation

1. Complete the [screen blueprint](../10-vertical-slice/ui-ux-foundation/SCREEN_BLUEPRINT_TEMPLATE.md)
   with player goal, entry context, primary question, primary/secondary information,
   actions, trade-offs, unknowns, return behavior and failure states. Classify
   tertiary and historical/raw information too.
2. Identify approved blueprint, component catalog, tokens and interaction rules.
   Record their versions and actual approval scope. Start with composition; if it
   fails, explain why and propose the smallest reusable addition. AI may compose,
   refine, implement and test within these contracts, not silently invent them.
3. For every visible region record its player question and what decision would
   worsen if removed. Give every chart an explicit question and every progress
   indicator an actual bounded quantity. Keep primary information and uncertainty
   visible without tooltips. One authoritative fact may have multiple views.
4. Use structural regions, documents, lists, comparisons and consequence-focused
   workspaces. Apply the full brief's restrictions on cards, rounding, shadows,
   badges, icons, accents, art, motion and SaaS/esports composition. No local
   component restyling or arbitrary values.
5. For company-level work, apply the composition clarification and start from the
   [draft operating-space blueprint](../10-vertical-slice/company-ui-demo/COMPANY_OPERATING_SPACE_BLUEPRINT.md).
   Identify the primary visual anchor, structured-navigation role, operating-space
   role and question answered by each instrument. The draft needs Director visual
   review; existing prototype geometry and PanelContainer primitives do not dictate
   the final screen. The provisional theme is not final art canon.

## Evidence and acceptance

Use the [Stage 5 screen review record](../10-vertical-slice/ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md)
for each major screen. Inspect 1280×720 and 1920×1080, relevant long names, large
currency values, multi-digit percentages and localized expansion. Preserve context,
primary decision information/action, uncertainty and current simulation time/state
at 720p. Use extra 1080p space only for useful comparison, evidence or history.

Explicitly answer all anti-AI review items A–J. Any YES requires correction or a
documented justification; a justification cannot waive the mandatory design gate.
Evaluate management-game identity without branding, information value, repeated-use
scannability and visual complexity. Link reproducible evidence to all 14 gate
criteria. Missing evidence stays pending; visual appeal alone cannot pass.

Reject a visual proposal that violates these rules. Keep engineering verification
separate from human visual approval and retain the existing
[verification and golden ownership rules](../10-vertical-slice/ui-ux-foundation/UI_VERIFICATION_STRATEGY.md).
Physical input/DPI and other deferred Phase 5 obligations remain open as recorded.

The later clarification adds all ten company-management identity questions and
explicit admin-UI, dashboard, decoration and current-scope tests to the review
record. A YES to generic SaaS, editor/debug interchangeability or spectacle-only
decoration requires correction. This supplements rather than renumbers the
preserved A–J audit and 14-part gate. At 720p retain a meaningful operating center,
selected scope, material situation/route and affairs access; do not retreat to a
giant list. At 1080p add useful relationships/state/evidence, not filler.

## Historical integration verification — original governance package

Executed 2026-10-06: the repository brief matches the supplied attachment
byte-for-byte; all 50 numbered rules and 14 source gate criteria are present.
The review template contains all ten A–J audit rows and all 14 acceptance rows.
All 52 local Markdown file targets across the changed documents resolve, code
fences are balanced, and `git diff --check` passes. The nine-file change is
documentation-only; gameplay, runtime, content, tests and existing evidence are
untouched. Runtime tests were not rerun for this documentation change.

This supplies no new rendered-screen evidence and makes no Stage 5 screen
acceptance claim.
