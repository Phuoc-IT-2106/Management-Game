# Stage 5 screen review record

Status: **BLANK TEMPLATE — no screen reviewed or accepted**.
Required by the [Stage 5 governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md).
Copy into the affected screen package and complete every applicable field. Use
PASS / FAIL / PENDING with evidence; an inapplicable case needs a reason.

## Scope and provenance

- Screen, version, player goal, primary question:
- Blueprint, approval record and authorized scope:
- Component catalog, tokens/theme and interaction rules (versions/approval scope):
- Build/commit and dirty state, fixture/content hash, campaign revision:
- Route, company/scope/entity/matter, decision origin and return target:
- Capture manifest, viewports, text scale, locale, brand and interaction states:
- Engineering reviewer/date, human reviewer/date and disposition:

## Information and consequence audit

Use one row per visible region, including persistent context and action areas.

| Region | Player question answered | Primary / secondary / tertiary / historical | Decision worsened if removed | Authoritative source/revision | Known / estimated / unknown and non-color cue |
| --- | --- | --- | --- | --- | --- |
| To complete | | | | | |

- Cause → commitment → expected/possible consequence → time:
- Primary action, secondary actions, irreversible/material commitment treatment:
- Navigation/context return, unavailable/stale/error and cancellation behavior:
- Chart questions, bounded progress quantities, or explicit absence:
- Existing component composition; any failed composition and smallest proposed addition:

## Visual composition contract — later Director clarification

Authority: [2026-10-06 composition direction](../../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md).
Complete these fields for every proposal; company-level reviews also answer all
ten identity questions below. For a decision workspace, explain its distinct
composition and preserved company/origin/return context. These checks supplement
the original audit and gate without altering the preserved brief.

- PRIMARY VISUAL ANCHOR: What is the dominant simulation representation?
- STRUCTURED NAVIGATION ROLE: How does the list/hierarchy support navigation without defining the whole screen?
- OPERATING-SPACE ROLE: How does the central representation communicate company operation/state?
- INSTRUMENT TEST: For each surrounding panel, what player question does it answer?
- ADMIN-UI TEST: Could this screen be mistaken for editor/debug/database tooling?
- DASHBOARD TEST: Could this screen be generic SaaS/business software?
- DECORATION TEST: Would gameplay/navigation/context meaning change if this visual element were removed?
- CURRENT-SCOPE TEST: Is every displayed module/entity/mechanic currently supported? Link exact source and implemented destination; distinguish integration gaps.

| # | Company-level management-game identity question | YES / NO / PENDING | Evidence / correction |
| --- | --- | --- | --- |
| 1 | Without branding, does this still read as a management/simulation game? | | |
| 2 | Is the company itself visibly the subject being operated? | | |
| 3 | Can the player understand ownership and shared corporate functions? | | |
| 4 | Are time, situations, commitments and consequences visible? | | |
| 5 | Does the center communicate a changing simulation rather than a static database? | | |
| 6 | Is the structured list available without defining the whole visual identity? | | |
| 7 | Could this exact composition be dropped into generic SaaS? | | |
| 8 | Could this exact composition be mistaken for editor/debug tooling? | | |
| 9 | Are surrounding information surfaces actual simulation instruments? | | |
| 10 | Is any visual element present only to make the screen look impressive? | | |

A YES to 7, 8 or 10 requires correction. A NO to a positive requirement requires
revision; missing evidence remains PENDING. Neither styling nor removal of cards
and gradients waives these checks. Reject both generic web/SaaS composition and
engineering/admin/debug composition. Record shared semantic-state parity, equivalent
keyboard/list routes, real targets and no-art viability for the operating visualization.

Company 1280×720 evidence: identity/context, time/checkpoint, selected scope,
meaningful operating center, material situation and decision route, affairs/time
discoverability, no primary-shell horizontal scrolling. Secondary instruments may
collapse; a giant list is not the density solution. Company 1920×1080 evidence:
useful relationships, operating state, consequences and evidence; no invented
metrics or merely stretched layout. Record unresolved visual choices explicitly.

## Positive game-identity checks (P1–P8)

Source: [game-identity thesis](../../01-governance/STAGE5_GAME_IDENTITY_THESIS.md).
These supplement the A–J audit and 14-part gate; a FAIL requires correction.

| ID | Check | PASS / FAIL / PENDING | Evidence |
| --- | --- | --- | --- |
| P1 | Ten-second test: names removed, viewer identifies a management game and what needs attention | | |
| P2 | Current time, next stop and the supported way forward (or why it is blocked) are on the primary company space | | |
| P3 | Own team/people and at least the next opponent are visible as actors where supported | | |
| P4 | At least one material state is drawn as an instrument with a complete text equivalent | | |
| P5 | No developer, contract or keyboard-legend text in player space beyond the single watermark | | |
| P6 | Exactly one primary action; irreversible actions distinct from navigation | | |
| P7 | State changes are visibly acknowledged; reduced motion still communicates them | | |
| P8 | 1080p space shows world/evidence/history; no dead band between content and its action | | |

## Anti-AI visual review

Answer YES / NO for every row. Every YES needs a correction with recheck evidence
or documented justification. An unanswered row remains PENDING.

| ID | Question | YES / NO | Evidence; correction/recheck or justification |
| --- | --- | --- | --- |
| A | Is every region unnecessarily inside a card? | | |
| B | Are large rounded corners used everywhere? | | |
| C | Are pills substituting for hierarchy? | | |
| D | Are gradients, glow or decoration driving identity? | | |
| E | Is usable area wasted to imitate marketing/web layouts? | | |
| F | Are metrics shown without a decision reason? | | |
| G | Are icons repeated without semantic value? | | |
| H | Could this screen belong to project-management software? | | |
| I | Could this screen belong to a generic neon esports website? | | |
| J | Were existing components locally redesigned? | | |

## Identity, density and repeated-use evidence

- Without branding/names/logos, does this read as a management/simulation game?
- Could this exact screen be dropped into generic SaaS? If yes, revise its
  simulation context and decision structure.
- After 100 uses, what makes the interaction fast, predictable and scannable?
- Identify the dominant workspace, contextual hierarchy and primary focus.
- 1280×720: evidence for company/context, primary information/action, critical
  uncertainty and current time/state; clipping, scroll and keyboard reachability.
- 1920×1080: useful comparison/evidence/history; no merely stretched or filler areas.
- Relevant long company/sponsor/player names, large currency, multi-digit
  percentages and localized text expansion: fixture and result for each.
- No portraits/art, warning-like branding, focus, contrast, reduced motion and
  repeated navigation: evidence or justified applicability per case.

## Mandatory design review gate

| # | Criterion | PASS / FAIL / PENDING | Evidence / remaining correction |
| --- | --- | --- | --- |
| 1 | Company-first context preserved | | |
| 2 | Player purpose immediately understandable | | |
| 3 | Information hierarchy matches decision importance | | |
| 4 | Uncertainty visible | | |
| 5 | Actions reflect actual gameplay | | |
| 6 | 720p remains usable | | |
| 7 | Shared components consistent | | |
| 8 | Semantic colors correct and independent of branding | | |
| 9 | No critical information depends on decoration | | |
| 10 | Anti-AI review passes with all YES findings resolved or justified | | |
| 11 | Screen does not resemble a generic SaaS dashboard | | |
| 12 | Screen does not rely on generic neon esports styling | | |
| 13 | Repeated-use scannability plausible | | |
| 14 | No unsupported gameplay invented | | |

Disposition: PENDING / REVISE / REJECT / ACCEPT, with rationale and named reviewer.
Any failed criterion prevents acceptance; pending evidence prevents a pass claim.
The later composition checks also block acceptance when failed or pending;
all original fourteen criteria still apply.
Reject a proposal that violates the governing rules even if visually attractive.
Record engineering results separately from human acceptance; do not promote
candidate captures to approved goldens through this template alone.
