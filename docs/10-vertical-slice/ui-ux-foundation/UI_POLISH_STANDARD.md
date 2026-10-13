# UI polish standard

Status: **ACTIVE — the single source for visual polish of player-facing screens** (2026-10-08). It consolidates the user's
Portal polish briefs and their implemented passes: surface polish v1 and v2, structural geometry v3, and the top-bar
typography and USD pass ([history and evidence](../genre-shell/README.md)).

**Authority.** Governance stays above this file:
- [visual / UX governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md);
- [genre-shell amendment](../../01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md);
- [game-identity thesis](../../01-governance/STAGE5_GAME_IDENTITY_THESIS.md);
- [art production policy](ART_PRODUCTION_POLICY.md).

The approved mockups in `../ui-research/mockups/generated-reference-set/` set each screen's composition. This standard
sets how every screen is *finished*: material, geometry, hierarchy, controls, text and verification.

Values are provisional development choices (`UiTokens.Version`), not final art direction.

## 1. Principles

1. **Structure, not strokes.** Polish means real silhouettes and layered material.
   - More borders or more gold is not progress.
   - A screen that still reads as "rectangles with decorative lines attached" fails.
2. **Graphite first, steel second, brass third.**
   - Graphite carries surfaces.
   - Cool steel carries edges and structure.
   - Brass marks the primary action, the current selection and a few structural junctions only.
3. **Importance decides richness.** Richer geometry and brass go only where the gameplay weight is highest. Everything
   else stays quiet.
4. **Readability and honesty first.**
   - No polish may cut text, hide uncertainty, invent facts or shrink a click target.
   - Data always comes from the read model.
5. **One system.** Screens compose shared components. No screen restyles a shared component inline or invents its own
   frame.

## 2. Material: surface levels

| Level | Material (`Surfaces`) | Use |
|---|---|---|
| 0 Room | `SurfaceBase` + procedural room backdrop | Behind all content |
| 1 Region | `Region(tier)` | A major area answering one question |
| — Header plate | `Header` + `HeaderPlate` | The top of every region |
| 2 Plate | `Card` | Nested blocks inside a region: decisions, offers, data blocks, empty states |
| 2 Row | `Row` / `RowAlt` | List and table rows; defined by tone, never by a full outline |
| 3 Lift | `Lift` | Hover |
| 3 Selection | `Selected` | Current selection: brass |
| — Well | `Well` | Recessed: art frames, filter tracks, data wells, inputs, header icon housings (`Housing`); never behind rail art |
| — Bar | `Bar` | Top bar and rail |
| — Tag | `Tag` | Chips, counts, badges, small notes |

Rules:
- Tiers stay visibly apart. UiKit enforces the order room < region < header < hover with a ≥ 1.1 contrast step.
- Nested plates sit below the region and above the room.
- Light comes from the upper left, one direction for every frame (`FrameGeometry.Light`):
  - top edges are lit and bottom edges are in shade;
  - a press inverts the bevel;
  - wells are dark at the top with a lit lip at the bottom.
- **No opaque black outlines.** The outer edge is a graphite shadow edge with alpha ≤ 0.6, plus a contact shadow and a
  soft shadow for elevation. Inner edges are a graphite groove and a faint highlight.

## 3. Geometry

- **One angle family: every cut is 45°** (`Cuts`: four equal-leg chamfers). Only the leg length changes.
  - Never use rounded corners or other angles on framed elements.
  - Flat boxes (selection bars, square focus rings, plain surfaces) are square: `UiTokens.CornerRadius` is 0.
  - Brand swatches and identity plates use the tag or control cut.
- **Tokens** (`UiTokens`):

  | Token | Value | Use |
  |---|---|---|
  | `ChamferControl` | 4 px | Buttons, rows; 3 px for filters and tags |
  | `ChamferSmall` | 6 px | Nested plates, header icon housings, art frames, rail items and selection, stages |
  | `ChamferMedium` | 11 px | Standard regions |
  | `ChamferLarge` | 16 px | Premium regions |
  | `EdgeHairline` / `EdgeRim` / `EdgeWall` | 1 / 2 / 3 px | Hairline rims; brass walls of the premium control and the rail selection; region walls |

- **Regions are cut at all four corners.** Controls and nested plates use the restrained diagonal pair (top-left and
  bottom-right). The rail selection uses all four.
- **Real silhouettes, not drawn-on lines.**
  - The outline, fill, wall facets, shadow and focus ring all follow the same cut polygon.
  - The wall is drawn as one triangle array, so adjoining facets share vertices and no seams appear.
- **Content stays rectangular.** Cuts never move layout or hit areas. Content margins must clear the cut: a corner's
  horizontal plus vertical margin ≥ its leg (`FrameGeometry.Contains`).
- **Art follows the cut.** Square art inside a cut frame is masked with `PanelFrame.Mask` and framed with
  `PanelFrame.Overlay` of the same cut.
- **No fixed screen coordinates** in components. Geometry derives from the control's rectangle, tokens and the laid-out
  children. Layout values live in `ShellLayout`.

## 4. Hierarchy: three tiers

| Tier | Where | Treatment |
|---|---|---|
| **Premium** | The region holding the screen's primary decision or the next match (at most two per screen); Continue | 16 px octagon. Brass inserts on the top-left and bottom-right chamfer facets. Continue: 8 px diagonal cuts, 2 px brass wall, soft lift |
| **Standard** | Every other region | 11 px octagon. One brass insert (top-left) |
| **Utility** | Rows, filters, tags, small buttons, metadata, inputs | 3–6 px diagonal cuts, hairline or tone-only edges, no brass |

- The tier is chosen by gameplay weight, never to decorate.
- A screen without a primary decision has no premium region.
- Brass appears only on:
  - the primary action;
  - the current selection (rail item, filter tab, table row);
  - region chamfer inserts by tier;
  - the header seam stripe;
  - semantic numbers that are already brass, such as an estimate.

### Current tier assignments

| Screen | Premium regions | Standard regions |
|---|---|---|
| Portal | Key decisions, Next match | World news, Transfer news, Season record, Money snapshot |
| Inbox | none | Messages |
| Squad | Profile (renew, release, sign) | Players, Free agents |
| Competition, Match view | Next match, Preparation plan | Coach authority, Season fixtures, Head-to-head |
| Competition, League view (rules v4) | League standings | the selected rival's profile, Playoffs, Matches, League information |
| Commercial | Approach a brand | Active agreements, Offers, Proposals under review |
| Finance | none | Position, Next 14 days, Sponsor income, Payroll |
| Staff | none | Head coach, Coach market |
| Company | none | Company structure, Context |
| Offer workspace | Offer terms | Company trade-off |
| Start screen | New campaign | none |
| Confirmation overlay | The commitment | none |

Other theme-level panels (`ui.Panel` with `SurfaceRaised`, `SurfaceInset` or `SurfaceOverlay`) automatically get the
standard region, the nested plate or the premium region.

## 5. Region headers and page headers

**Region header** (`SectionHeader.Region` → `HeaderPlate`). The plate is seated flush in the region's wall:
- Its top corners sit on the wall's inner chamfer and its lower ends are cut, so it reads as a trapezoid with diagonal
  seams to the wall.
- It has a brighter upper steel edge and a dark lower inset line over a steel lip.
- It holds, in order:
  1. the icon, in the shared housing;
  2. the title, in spaced display caps on a raised tab that ends in a 45° slant;
  3. a narrow inset seam and one short brass stripe parallel to the slant;
  4. optional context;
  5. at most one route ("View all →"), aligned with the title.

Context is **supplementary**: it is shown whole where it fits and omitted where it does not (`FitOrHide`), never cut
mid-word.

**Page header** (section screens). The title uses the display face, with one supporting line beneath. An engraved rule
with a short brass lead closes the header. There is no hero art until approved art exists.

## 6. Controls

- **One geometry per control.** Normal, hover, pressed, disabled and the focus ring share the same cuts.
  - Hover is brighter with more lift.
  - Pressed inverts the bevel and sinks the content 1 px (`Surfaces.PressShift`) without changing size.
  - Disabled is a flat plate.
  - Focus is a ring outside the control that follows its cuts. Square flat controls keep a square ring.
- **One primary action per workspace.**
  - The brass `PrimaryAction` is reserved for that workspace's decision.
  - Continue is the shell's premium control.
  - Local decision verbs use brass with 4 px cuts and no soft lift.
  - Secondary actions are graphite with a steel bevel.
  - A strong secondary route, such as Prepare match while no plan exists, uses the teal `SecondaryAction`.
- **Click targets are at least 24 × 24 px** (`UiTokens.MinimumTarget`, `CardButton.Compact`). Small controls favour
  legibility and reliable clicking over ornament.
- **Action labels never wrap.** A button is at least the action width and grows with its label. "Return to company · Esc"
  reads as one action, not two stacked fragments. A row of actions flows to the next line instead.
- **Filters** are segments on a recessed track. The active segment is a lifted plate with a brass top edge.
- **Tags** (status chips, counts, badges, notes) are utility tags, with words carrying the meaning and tone supporting it.
- **Inputs** (line edits, spin boxes) are recessed wells with control cuts. Choice lists use the secondary button
  material.
- **Tables** (S5) sit inside a region:
  - column titles are a header strip in caption caps;
  - rows alternate tone;
  - the selected row is a brass-edged selection;
  - no vertical guides or per-cell borders;
  - the column that names the row (a player, an opponent) gets the most width, while day and amount columns stay narrow;
  - a cell the table trims keeps its full text as a tooltip.

### Shell composition (Portal redesign, 2026-10-08)

- **Decision cards** have three functional areas:
  - a context row: the category in caps on the left, the due date on the right;
  - the identity on the left (approved art, the subject's mark or the category icon) and the decision in the centre;
  - the action at the bottom right, beside the decision.

  The decision column takes the remaining width, so a long name wraps and never meets the action.
- **Sponsor offer cards** show the brand's mark and name only. Commercial terms belong to the offer review.
- **Consequence lines** appear on roomy cards only; compact cards keep them in the tooltip and at the destination.
- **Top bar** (top-bar typography pass, 2026-10-08). Two regions:
  - **Metrics.** The game state is one row of metrics. Each is a caps label over one value line: the value, then its
    subordinate context.
    - Season: `SEASON 1` over **Week 1** of 8.
    - Day: `DAY` over **6** / 56, then the half.
    - Next match (roomy only): the opponent, bounded at 240 px with the name in the tooltip, then when.
    - Cash: dollars; compact from a million, exact in the tooltip.
    - Financial condition (`FINANCES` when compact): the status in its semantic tone.
    - Pending decisions (`DECISIONS` when compact): see below.
  - **Spacing.** Metrics keep their content width and are left-aligned. The free width is shared as equal gaps (at least
    16 px), with no engraved dividers and no icons. The gap before the actions is never wider than the gaps between
    metrics.
  - **Baselines.** Every value line is one value line tall. Context and status sit on the value's baseline (`Seat`), so
    the labels form one row and the values another.
  - **Pending decisions** is a metric like the others, never a notification badge:
    - the count is the Portal's own total: `0`, `3`, `12`, `99+`;
    - the context says what it means now: `all clear`, `next due tomorrow`, or `▲ 1 blocking`;
    - amber appears only when a decision blocks Continue, and red never;
    - the metric opens the Portal's key decisions.
  - **Actions.** Save, Load and Continue stay grouped at the right, Continue last and strongest.
- **Rail** (sidebar redesign, 2026-10-08; values compact / roomy, all in `ShellLayout`):
  - **Width.** The rail is 184 / 208 px wide, so the longest label fits beside the art.
  - **Identity.** The company crest is the largest identity mark, with the player's name and no generic descriptor.
    - Roomy rails stack a 64 px crest above the name.
    - Compact rails set a 42 px crest in the art column and the name on the label column, which keeps the identity short.
  - **Items.** Each item is one component: art slot, label, optional unread badge.
    - All eight sections show their own approved module art (`SemanticAssets.ForSection`).
    - The art sits in a 42 / 46 px slot, drawn by its visible content at one perceived size (38 / 42 px optical,
      `SemanticAssets.OpticalSize`), so a tall trophy and a wide building read alike.
    - A missing asset shows that concept's vector icon in the same slot.
  - **Nothing behind the art.** Rail art sits straight on the item's own surface: flat, the hover plate or the brass
    selection. No housing or backdrop: a dark well behind transparent art reads as a black box.
  - **Labels.** Labels are 16 px and start 12 / 14 px after the slot, all on one column. The current section is semibold
    primary text; the others are secondary.
  - **Unread badge.** A 24 × 20 tag at the item's right, clear of the label. The unread count is also in the item's
    tooltip.
  - **Spacing.** Items are 50 / 56 px click targets, spread through the navigation region by `SpreadColumn`:
    - one even gap of at least 8 px and at most 20 / 40 px;
    - half a gap above the first item and below the last;
    - the rest of the height goes to the notice dock, which always keeps 52 px (two lines) above Collapse.
  - **Collapsed.** A collapsed rail (art slot plus insets, 70 / 74 px) shows the crest and the art alone, centred. Every
    item keeps its states and its route.
- **Notices** (warnings, rejections, save and load) dock in the rail's notice dock and never cover content. A collapsed
  rail floats them at the bottom right.
- **No narration of advancing time.** What changed reaches the player as decision cards, unread news and the inbox
  count.

## 7. Text and numbers

- **One family, three weights.** Every role is set in Inter 4.1 (SIL OFL 1.1, `Assets/Fonts/Inter`), bundled with the game.
  - Weights: Regular 400 for reading text and context, Medium 500 for metric labels and data figures, Semibold 600 for
    values, titles, emphasis and actions (`UiTokens.Weight`).
  - No role picks its own weight or family.
  - Glyphs the family lacks, such as ◐, fall back to the system symbol faces.
- Use the 7-step type scale: 12 / 14 / 16 / 20 / 24 / 32 / 48. Nothing is below 12 px.
- **Tabular figures.** Values that change use tabular figures (`tnum`): every role except running text, and table cells.
- Section titles and category labels are uppercase caps. Running text stays sentence case.
- **Tracking.** Only caps get tracking, 1 px: section titles and metric labels. Caps labels also use case-sensitive
  forms (`case`).
- Text is never clipped:
  - person names are never truncated;
  - bounded text ends in an ellipsis, with the full text in the tooltip and at the destination;
  - optional text is omitted whole.
- Known, Estimated and Unknown keep their markers and words (● ◐ ○). Colour never carries meaning alone.
- **Hierarchy** (compact / roomy):

  | Element | Size and face |
  |---|---|
  | Top-bar metric labels | 12 medium caps, muted |
  | Top-bar values | 20 semibold, tabular; a word status 16 semibold |
  | Top-bar context | 14 regular, muted |
  | Top-bar actions | Save and Load 16 semibold; Continue 20 semibold |
  | Rail labels | 16; the current section semibold |
  | Region titles | 16 / 20 display caps |
  | Decision subjects | 16 / 20 semibold |
  | Decision category and due date | 12 caps |
  | Card actions | 14 semibold |
  | Lead news headline | 16 / 24 |
  | News rows | 16 semibold |
  | Transfer headlines | 14 / 16 semibold for every story, one line (two only for a long name) |
  | Metadata | 12 |

  Raise a level only where it carries a decision. Never raise every size together.
- **Numbers and money** come from one formatter, `ManagementGame.Formatting`; no screen formats a number itself.
  - **Currency.** Money is the US dollar: a nominal 1:1 relabel of the old development unit "CU".
    - Amounts stay integer cents in the simulation and in saves.
    - The `$` sign is never stored in a number.
  - **Precision by use:**
    - `Currency.Format` is the exact amount, cents shown only when present (`$2,476`, `$9.80`). Use it for prose,
      decisions, confirmations and notices.
    - `Currency.Cents` always shows cents. Use it in table columns, so figures line up.
    - `Currency.Whole` and `Currency.Compact` round to whole dollars (`$2,326`; `$1.23M` from a million). Use them only
      in glanceable summaries (the top bar, the Money snapshot), with the exact amount in the tooltip or one click away.
  - **Signs.** Negatives and expenses use a true minus before the sign (`−$920`). A change always carries its sign
    (`+$765`).
  - **Rates.** Rates are `$9.80/day` and `$300/week`. A word joiner keeps a rate on one line.
  - **Other figures.**
    - Counts use comma thousands separators (`1,250`).
    - Records use an en dash (`5–3`).
    - Percentages are whole (`63%`).
    - A count in a small space caps at `99+`.
  - **Tables.** Amount columns are right-aligned. Amount and date columns are never trimmed: a cut `$158.` reads as
    another number.
- **Player space carries no developer, fixture or narration text** (user instruction, 2026-10-08; it replaces the earlier
  single-watermark allowance):
  - debug builds put the development marker in the log and the window title;
  - the internal console entry exists in debug builds only;
  - Portal regions carry no generic descriptive subtitles.

## 8. Data, art and identity

- **Everything comes from the read model.** Never hardcode names, values, records, news or states. Mockup sample content is
  never data; UiKit fails on known mockup samples in code.
- **Facts the simulation does not own are not shown** (venue, series format, maps won, trends): show unknown or omit.
- **News is not a market.** A news panel shows events, never listings: no fees, wages, ability or availability. Each
  story carries its certainty as the information marker plus word (`● CONFIRMED`). A label less certain than the
  records support (rumour, report) needs a public-information mechanism first
  ([Transfer News](../genre-shell/TRANSFER_NEWS.md)). A person's headline leads with their whole name.
- **News articles share one treatment** ([Transfer News](../genre-shell/TRANSFER_NEWS.md), 2026-10-09).
  - No story is enlarged for being first; a featured layout needs an explicit editorial importance rule and more
    content to show.
  - A move shows its journey, origin → destination, always in that order.
  - Each side is an organization's crest and name, "Free agent" (a status, never a crest or shield), or
    `○ Team unknown`; never a guessed organization.
- **Nothing is drawn behind a person.**
  - Generic person art is drawn bare (`AssetMark` without a plate), sized by its visible content.
  - A dark plate or well behind transparent art reads as an inventory slot.
- **Approved semantic assets stand only for their own concept** (`SemanticAssets`). Missing art falls back to that
  concept's own vector icon, never to another concept's art.
- **No generated or invented imagery:** no portraits, photographs, arena or office art, and no 9-slice textures.
- **Remaining art gaps are listed, not faked:**
  - news, match and arena imagery;
  - player and staff portraits;
  - the rail ambience.

  The 3D section icons for Inbox, Competition, Commercial and Finance (batch `portal_assets_v2`) are in the rail since
  2026-10-08. The Calendar icon is catalogued but unused (`Assets/UI/Semantic/ASSET_MANIFEST.md`).

## 9. Layout

- **1280×720 is the stress target; 1920×1080 is the detailed variant.** Viewports at least 900 px tall show more, never
  stretched copies.
- No horizontal overflow. The top bar stays one row. Content keeps at least 60% of the height.
- **The Portal fits the first viewport at both sizes.** Section screens may scroll vertically.
- Compact viewports show fewer items, never smaller type.

## 10. Where it lives in code

| Concern | Use |
|---|---|
| Tokens: colours, type, chamfers, edges, target size | `UI/Tokens/UiTokens.cs` |
| Outline maths: cuts, insets, light, containment | `UI/Tokens/FrameGeometry.cs` |
| Materials and the frame renderer | `UI/Theme/Surfaces.cs` (`SurfaceStyle`) |
| Theme variations: buttons, rows, filters, panels, inputs, tables | `UI/Theme/UiTheme.cs` |
| Region, plate, well, housing, mask, overlay, header plate, optional text, evenly spread column | `UI/Components/Framing.cs` (`PanelFrame`, `HeaderPlate`, `FitOrHide`, `SpreadColumn`, `EngravedRule`) |
| Region header | `SectionHeader.Region` (`UI/Components/Primitives.cs`) |
| Section page, region, table and caps helpers | `Page`, `Card`, `Table`, `Caps` in `UI/Shell/ShellScreens.cs` |
| Compact click target | `CardButton.Compact()` |
| Portal composition and tiers | `UI/Shell/ShellLayout.cs` (`RegionTier`) |
| Type roles, weights, the bundled family | `UiTokens` (`Weight`, `Tabular`, `Tracking`, `FontPath`); faces in `UiTheme.Face` |
| Top-bar metrics and baseline seating | `GameShell.Metric`, `Value`, `Context`, `Seat`, `PendingMetric`; spacing in `ShellLayout.Metric*` |
| Money, counts, records, percentages | `src/ManagementGame.Domain/Formatting.cs` (`Currency`, `Figures`) |

Do not add inline `StyleBoxFlat`s, rounded corners or per-screen colours. If a need is missing, extend the shared material
and record it here.

## 11. Applying the standard to a screen

1. Open the screen's mockup and its blueprint. Keep the information architecture and data. Polish is not a redesign.
2. Wrap each question the screen answers in a region (`PanelFrame.Major` with `SectionHeader.Region`) and pick its tier
   (§4).
3. Put the screen's content on the right levels:
   - lists and tables become rows;
   - nested blocks become plates;
   - art and filters sit in wells.
4. Use the theme's control variations, with one primary action. Keep every target at least 24 × 24 px.
5. Replace any remaining inline styles, rounded boxes and per-item borders.
6. Verify at 1280×720 and 1920×1080 (§12) and capture before/after evidence.

## 12. Verification

**Automatic checks**

`build/Verify-Shell.ps1` runs these at both sizes, on four kinds of screen (`ChromeVerification.cs`):
- the Portal, for every case;
- every section screen;
- the offer workspace;
- the confirmation overlay and the start screen.

The checks:
- each region is cut at all four corners with its tier's wall and brass;
- each header plate sits flush in its wall (within 0.5 px) on the inner chamfer, with cut lower ends;
- the tab slant clears the title, and following header text clears the slant and stripe;
- header routes are aligned with the title and inside the plate;
- no text or art crosses any surrounding chamfer (art inside a masking frame is the exception);
- every control is at least 24 × 24 px;
- at most two premium regions per screen;
- no rounded boxes on any panel, label or button;
- action labels stay on one line and fit their button;
- decision actions sit at the bottom right with one shared right alignment and never meet the name;
- sponsor offers show the brand's mark and name without terms;
- the top bar (`TopBarCheck`):
  - its metrics span the bar with equal gaps, and no wider gap before the actions;
  - every metric is a label and a value, set in the bundled family;
  - labels are muted 12 px caps, subordinate to their values;
  - labels share one baseline, and values and context share one below it;
  - nothing is clipped;
  - cash, week, day and the pending count come from the read model, with amber only when a decision blocks;
- no player-facing text names the retired `CU` unit, on any screen, in the top bar, the rail or the confirmation;
- amount and date columns of tables show whole;
- the rail (`RailCheck`, expanded and collapsed, on every section):
  - fits the window at its own width without squeezing its items;
  - lists the eight sections in order at one item height, evenly spaced, clear of the identity, the notice dock and
    Collapse;
  - each item shows its own module art at one perceived size inside its slot and cuts, with no housing or backdrop;
  - labels are whole at 16 px on one column, centred with their art;
  - the unread badge is clear of its label, and the badge and tooltip carry the inbox's unread count;
  - only the current section is selected;
- notices dock in the rail;
- no developer or narration text appears;
- tables sit in a region, with rows by tone and the brass selection, and show every row with no empty band;
- every table cell keeps its full text as a tooltip;
- no clipped single-line text and nothing below 12 px.

The theme-level checks run once:
- every control keeps one geometry across its states and focus ring;
- cut size and brass follow the hierarchy, and Continue is the richest control;
- shadow edges stay at alpha ≤ 0.6, and rows are tone-defined.

UiKit checks the tokens and outline maths: every edge is horizontal, vertical or 45°; insets stay parallel; cuts fit
small controls; light order and containment hold.

**Captures**

Captures run unsandboxed: `build/Capture-Shell.ps1 -Results 1 -Cases -Output artifacts/<dir>`. Judge at 100% scale, then
with a 2× detail sheet. Evidence goes under the screen's `…-evidence/` folder, with `before-*` files.

**Review questions** (all must be yes)
1. Are the region silhouettes visibly cut?
2. Do the headers have clear angled geometry?
3. Do the frame layers look structurally connected?
4. Are steel and brass consistent and restrained?
5. Are heavy black outlines gone?
6. Is the result visible at 100%?
7. Is readability unchanged or better?

## 13. Anti-patterns

These failed review before:
- Gold lines drawn on the corners of rectangles, or brass fading along every edge.
- Header bands that are flat rectangles with a thin underline.
- Opaque black outline, groove and halo stacks.
- Equal decoration everywhere.
- Rounded SaaS cards, white dashboards, blue calls to action, neon or cyberpunk styling.
- Decorative art filling space.
- Bolts, rivets, glowing circuitry.
- Separate PNGs per panel, or fixed per-component coordinates.
- Trimming optional words mid-word.
