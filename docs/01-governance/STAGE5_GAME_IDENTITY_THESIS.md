# Stage 5 game-identity thesis

Status: **ACTIVE — explicit user instruction**, 2026-10-06 (Asia/Saigon): resolve
the causes of the admin/debug appearance and prepare the presentation for a
genuine management game. Applies to Stage 5 presentation work under DEC-023,
together with the [visual / UX governance](STAGE5_VISUAL_UX_GOVERNANCE.md) and the
[composition direction](STAGE5_VISUAL_COMPOSITION_DIRECTION.md). Phase 5 remains open.

## Why this exists

The preserved 50-rule brief names what to avoid. It does not state what the game
should feel like, and its only positive vocabulary (#4: regions, documents, lists,
tables, inspectors) is also the vocabulary of administrative software. With final
art, palette and fonts unauthorized, every screen fell back to the lowest-risk
material: wrapped text on bordered buttons and panels. Measurement compounded it:
Stage 3.1 compared representations by Control count, activations and fit, never by
whether the screen reads as a game. The review below found five causes:

| Cause | Evidence |
|---|---|
| Form-widget material | Every entity was a full-width bordered Button with centered text and an ASCII code (`[TM]`); no drawing, textures or motion (motion tokens defaulted to reduced) |
| Organization-chart data | `MapSnapshot` carries scopes, text links and ≤4 situations; roster, rivals, fixtures and results already in `Situation` never reach the company screen |
| Master–detail tool interaction | Select → inspector → "Open decision entry"; time advance, preparation and roster decisions remain in the internal tabbed console |
| Developer text in player view | Keyboard legends, contract notes ("Equal emphasis…", "evidence supplied by presentation record"), repeated fixture notices in warning color |
| Prohibition-only process | No positive target, bottom-up generic kit, efficiency-only evaluation |

The [genre-shell amendment](STAGE5_GENRE_SHELL_AMENDMENT.md) later supplies the
structural skeleton (top bar, navigation rail, Portal, tables); this thesis governs
how that skeleton feels. P4 now requires a drawn instrument on the Portal or a
decision workspace rather than on every primary screen.

This document supplies the missing positive target. It adds to, and never relaxes,
the brief, its 14-part gate or the A–J audit.

## Thesis

**The player sits in the operations room of a company they run. Time is moving,
rivals are present, the team is theirs, and every commitment has weight.**

The intended reading in the first ten seconds, with names removed: "I run an
organization; something is due; I can act now and see what it will cost."

## Six pillars

1. **Time is the heartbeat.** The primary company space owns the current day, the
   next stop and the control that advances time. A screen without a way forward is
   a viewer, not the game.
2. **The world is present.** The player's own team and people, the next opponent
   and the rivals appear as actors with names, state and uncertainty, not only as
   lines inside documents.
3. **Commitments have weight.** A material decision has one visually distinct
   primary action and shows its consequence as before → after, with timing.
4. **Things react.** State changes are acknowledged: values that changed are
   highlighted, context transitions are shown, results are revealed. Restrained,
   explanatory motion is on by default; reduced motion remains fully usable.
5. **Instruments, not paperwork.** Material state is drawn (tracks, gauges,
   schematics) with a complete text equivalent beside or below it. Prose explains;
   it is not the primary display.
6. **Player-facing language only.** Labels use the player's verbs and nouns.
   Implementation notes, keyboard legends, contract wording and provisional-theme
   labels never occupy player space. Development identity is one compact watermark.

## Provisional material rules (authorized by this instruction)

These are replaceable provisional values, not final art canon:

- **Rows, not buttons.** Entities in lists, maps and paths render as flat,
  left-aligned rows; hover/selected surfaces and a leading selection bar replace
  per-item borders. Bordered buttons are reserved for actions.
- **One primary action treatment.** A single inverted-neutral `PrimaryAction`
  style marks the decision of the current workspace. It is not a semantic state
  color and never resembles warning, error or focus.
- **Surface tiers before borders.** Base, raised, inset and overlay surfaces carry
  structure; essential control boundaries keep a ≥3:1 border; decorative edges use
  a subtle divider.
- **Proportional tabular numerals.** Values use the UI face with tabular figures,
  not a code/monospace face. Titles use a heavier display weight.
- **Geometric status markers.** ● Known, ◐ Estimated, ○ Unknown; ▲ warning,
  ■ critical/error, • neutral/information. Words always accompany the marker.
- **Motion on by default** for acknowledgement, context transition and change
  highlight, with a single reduced-motion preference that sets durations to zero.
- **Focus drawn outside the control** so it stays visible on every fill.

Still reserved for Director review: final font licensing/bundling, final palette,
bespoke icon family, illustration/art, character portraits and any headquarters
representation. DEC-001–023, gameplay scope and DV obligations are unchanged.

## Positive acceptance checks (P1–P8)

Recorded in the [screen review record](../10-vertical-slice/ui-ux-foundation/STAGE5_SCREEN_REVIEW_TEMPLATE.md)
for every primary screen, alongside the A–J audit and 14 gate criteria:

| ID | Check |
|---|---|
| P1 | Ten-second test: with names removed, a viewer identifies a management/simulation game and can say what needs attention |
| P2 | The primary company space shows current time and the next stop, and offers the supported way forward (or states why it is blocked) |
| P3 | The player's team/people and at least the next opponent are visible as actors where the slice supports them |
| P4 | At least one material state is drawn as an instrument with a complete text equivalent |
| P5 | No developer, contract or keyboard-legend text appears in player space beyond the single development watermark |
| P6 | Exactly one primary action per workspace; destructive or irreversible actions are distinct from navigation |
| P7 | A state change produces visible acknowledgement (highlight, transition or reveal), and reduced motion still communicates it |
| P8 | At 1920×1080 the extra space shows world, evidence or history; no dead band separates content from its action |

Control count, activation count and pixel fit remain useful engineering proxies,
but they are not evidence of game identity. Human ten-second and task tests are.

## Reference games (low authority, #40)

Football Manager (inbox, calendar and a single continue control), Motorsport
Manager (headquarters and time), Out of the Park Baseball (dense data with strong
team identity), Game Dev Tycoon / Two Point series (reactive feedback). They may
inspire composition, density and tone only; never names, assets, mechanics,
leagues or values.

## Next work enabled

1. Bring time and world into the company space: project roster, coach, next
   opponent/rivals, fixtures and results from existing `Situation`/World data and
   integrate the existing `AdvanceDecision` as the primary forward control.
2. Add the smallest reusable graphical primitives (entity token, vector icon set,
   schematic canvas, bounded gauge), each with a text equivalent.
3. Rebuild the COS-05 company composition with that material, then retire the
   internal console as a player path (preparation workspace next).
