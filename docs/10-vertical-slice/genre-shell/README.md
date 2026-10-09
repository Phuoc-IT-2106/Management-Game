# Genre shell — player entry point

Status: **IMPLEMENTED, candidate** (2026-10-06) under the
[genre-shell amendment](../../01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md). The
shell is now the project's main scene (`res://UI/Shell/GameShell.tscn`); the
internal console (`res://Main.tscn`) stays reachable from the start screen and is
used by the console smoke. Human/Director visual acceptance is pending.

## Structure

| Region | Contract | Source |
|---|---|---|
| Start screen | Company name, world seed, New campaign (primary), Continue saved campaign, Internal console | `Composition.Create`, `Session.Load` |
| Top bar (S1) | Monogram + company name, season/day, cash and financial condition, decision count, Save/Load, **Continue** (primary). Continue becomes "Prepare match ▸" and routes to Competition when an unplanned match is due today | `Situation`, `PortalTasks.Blocker` |
| Navigation rail (S2) | Portal, Inbox (unread count), Squad, Competition, Commercial, Finance, Staff, Company; collapses to glyphs; Alt+1…8 | Eight real sections; no future modules |
| Portal (S4) | Decision strip, inbox preview, drawn four-week load/receipt/match track (P4), next match, season record with head-to-head, money | `PortalTasks`, `Inbox`, `SponsorProjection`, `Rivals`, `Fixtures` |
| Inbox | Filters (All, Offers & deals, Contracts, Matches & season); every message names who/what is affected and routes to its section | `Situation.Inbox`, `ShellSections.ForInbox` |
| Squad (S5) | Players table with every comparable fact and contract end; free agents table; context profile with renew/release or sign; keyboard row flipping updates the profile | `People`, `Candidates` |
| Competition | Next match with estimate/unknown cues, preparation plan, lineup by role, coach recommendation, coach authority, season fixtures, head-to-head | `Recommendation`, `Fixtures`, `Rivals` |
| Commercial | Active agreements, offers (open the verified sponsor workspace), approach-a-brand proposal with market guide (estimate), proposals under review | `Offers`, `Brands`, `Negotiations` |
| Finance | Cash (known), forecast (estimated), arrears, 14-day outlook with projected wages, sponsor income, payroll | `Bills`, `People`, `CoachContract` |
| Staff | Head coach contract and renewal, coach market and hire | `CoachContract`, `CoachCandidates` |
| Company | The live operating-space view, embedded (no second watermark or back control) | `SponsorCompanyHost` |

Material commitments (sign, renew, release, hire, propose) confirm in an overlay
that keeps the shell visible; offers use the existing sponsor decision workspace
and return to Commercial. Ctrl+Enter continues; Escape cancels an overlay.

## Application additions

Presentation projections only, no gameplay rule: `Situation.Inbox` (world messages
with a stable kind and cause, never the player's own decisions), `Fixtures`
(current season with results) and `Rivals` (current-season head-to-head);
`PortalTasks.Build/Blocker` and `ShellSections` in `PortalTasks.cs`.

## Verification (2026-10-06)

`build/Verify-Shell.ps1` (headless, 1280×720 and 1920×1080): 52 checks each — start
screen and player-named company; every section opens with rail selection, no
horizontal overflow, Continue visible, top bar stays one row and content keeps at
least 60% of the height; Portal task routes to its section; plan committed from
Competition; Continue reaches a result that arrives in the inbox with a stop
reason; Alt+2/Alt+3 navigation; keyboard row flipping updates the squad profile;
material commitment confirms and cancel changes nothing; offer opens the sponsor
workspace and Escape returns; Ctrl+Enter; Save/Load round trip; rail collapse.
`build/Capture-Shell.ps1` writes start, eight sections and the offer workspace at
both sizes to `artifacts/shell/candidates` (interactive desktop required).
Integration adds shell-projection checks (199 total). Existing suites unchanged.

## Known limits

Glyphs use system fonts; final icon family, crest art, palette and fonts remain
Director decisions. No match-day staging, league table, objectives or news feed
yet: those need mechanics first (see the gameplay review). Physical input/DPI
(DV-03/04) and human usability remain unvalidated.

## Portal after the approved reference — 2026-10-07

Status: **IMPLEMENTED, candidate; refinement pass applied**. The Portal, top bar and rail follow
`ui-research/mockups/generated-reference-set/01_portal_mockup.webp` (composition, navy
operations-room surfaces, outline icons, gold-tinted current section, uppercase category
labels, ring and segmented gauges). Human/Director visual acceptance is pending.

Data: everything comes from `PortalProjection.Build(Situation)` (`PortalView.cs`), built once per
observation; the top bar and the Portal render the same model. Decisions are the typed
`PortalTasks` with category, subject, reason, due day, section and stable target ID; news is the
world inbox with the stable ID of each message's subject (an open sponsorship offer reads as a
headline from its typed record, terms as the summary); next match, horizon events, season record
and money are built from fixtures, offers, negotiations, contracts and scheduled bills only.
Horizon events carry their typed kind, subject, amount and count (same-day deadlines of one kind
group into one event that routes to the section overview). Routes carry a section plus stable
target ID; offer decisions open the sponsor workspace directly.

Correctness: the pending-decision total is one number (`PortalScreen.PendingText`) in the top
bar and the Portal header; when only the most urgent cards fit, the header adds "showing N" and
"Show all". A campaign starts only with a player-chosen company name, so the content placeholder
`DEV_ORG_001` is not shown as identity; captures pass a name as harness input
(`Capture-Shell.ps1 -Company`). The single development marker moved from the content corner to the
rail footer and is shown only when `OS.IsDebugBuild()`.

Assets: `AssetIds` derives `crest:`, `logo:`, `portrait:` and `fallback:news:` IDs from stable
entity IDs. `IdentityAssets` resolves crests to generated letterless arms and brand logos and
portraits to generated letterless symbols (S8); thumbnails sit on a plate washed in the subject's
colour. News without a subject resolves to a deterministic category plate. The hero atmosphere is
the catalog ID `environment:portal`, which resolves to a procedural wash, light pool and a faint
oversized company crest until approved art exists. No images are loaded or generated as content.

Intentional differences from the mockup (the gameplay model and the
[art policy](../ui-ux-foundation/ART_PRODUCTION_POLICY.md) win): no News, League or Training
sections (no such mechanics; the Inbox feeds "Latest news"); no venue or series format; no rival
form or maps won/lost; money shows known cash, condition, arrears, scheduled receipts, estimated
payments and a 7-day estimate instead of monthly income, expenses and a month-on-month trend; no
photographic office or arena (generic HQ/arena art is rejected and no asset is authorized; the
company and club colours, light pools and crests carry the atmosphere); news art is subject marks
rather than photographs; currency is CU; the win rate supports the record instead of leading it.

Tokens: `operations-room-v3` (navy surfaces, gold primary action, teal positive) and one 7-step
type scale (12/14/16/20/24/32/48); secondary and muted text are now two distinct steps; tabular
figures on strong, display and numeric faces; decision rows on the inset tier; `NavSelected` is a
dark warm tint with a gold leading edge; `SecondaryAction` (dark teal fill, teal edge) marks
"Prepare match" while no plan exists. Layout values live in `ShellLayout`. State changes since the
last look (cash, condition, decision count, new decisions, a new result) flash once through
`UiMotion`; reduced motion makes this a no-op.

Verification (2026-10-07, local): `build/Verify-Shell.ps1` 258 checks at 1280×720 and 259 at
1920×1080, including the whole Portal fitting the first viewport without a scrollbar, one
pending-decision total in top bar and Portal, the development marker outside content, no text
below 12 px, typed horizon events, news art by asset ID, routes, keyboard focus and eleven cases
(long names, no / one / many decisions, no match, empty / many news, warning, distress, missing
crests and news art). Integration 215, Domain 4, UI 3, UiKit 140, OperatingMap 48,
SponsorWorkspace 48; native UI kit 24, operating map 101, sponsor 31; Headless two-cycle pass;
Debug solution build with zero warnings. Evidence: [portal-evidence](portal-evidence/)
(`build/Capture-Shell.ps1 -Results 1 -Cases`; `baseline/` holds the pre-refinement captures).

## Portal — world news and transfer news (graphite) — 2026-10-07

Status: **IMPLEMENTED, candidate**. Reference: `ui-research/mockups/generated-reference-set/11_portal_world_transfer_graphite_mockup.png`
(latest approved Portal reference). Human/Director visual acceptance is pending.

Composition: top bar · key decisions · world news | transfer news (57% / 43%) · next match | season record | money.
The Portal hero and four-week operations are removed from the Portal. The operating horizon stays available as its
own read model (`HorizonProjection`, Application), with its tests, for calendar-type presentation; no schedule or
domain state changed. The company's
identity (generated arms and the player's name) now heads the rail, and the top bar adds the next match when the
viewport is roomy.

Approved semantic assets (`game/Client/Assets/UI/Semantic/ASSET_MANIFEST.md`, catalog `UI/Contracts/SemanticAssets.cs`)
are used only for their own concepts. If art cannot be loaded, the one fallback policy (`SemanticArt.Create`) shows
that concept's own vector icon, never another concept's art:
- Rail module art for Portal, Squad, Staff and Company; the other sections keep vector icons.
- Decision art by the typed `DecisionKind`: match preparation, contract (player and coach renewal), roster move
  (a role with no player).
- Generic player or staff art for `portrait:person:*` / `portrait:coach:*`, always beside the person's name.

Sponsor decisions show the brand's generated mark; finance and free-slot decisions show vector icons.

Data:
- **Key decisions** are `PortalTasks` with a typed `DecisionKind` and subject asset ID. Each card's verb opens the
  decision; the commitment itself is confirmed at the destination.
- **World news** is the world inbox minus personnel kinds (`PortalProjection.Personnel`). The lead story draws its
  subject's mark over a colour wash; no event image exists or is generated.
- **Transfer news** (superseded 2026-10-08 by the editorial feed in [TRANSFER_NEWS.md](TRANSFER_NEWS.md); kept
  as the record of this pass) was `PortalView.Transfers`, built only from typed records:
  - contracts in their renewal window, with the asking wage and the player's known ability;
  - people who left at contract end and are still on the market (subject resolved from the employment ID);
  - free agents and coaches on the market, with the quoted fee, wage and estimated ability.

  Each fee carries a typed `FeeCertainty` (None, Unknown, Estimated, Quoted, Confirmed). Current rules produce only
  None (renewals) and Quoted (market asking prices); the other states render with their markers and are covered by a
  verification case. Age is not modelled, so it is not shown.
  Rivals' squads, rumours and third-party negotiations do not exist in the rules and are not shown. Filters
  (All / Expiring / Departed / Free agents / Coaches) appear only for kinds that are present.
- **Season record** adds recent form (W / L / F letters); there is no league standing, so none is shown.

Tokens: `graphite-brass-v1`: charcoal surfaces with a steel cast, muted brass for the primary action and selection
only, teal / blue semantic accents. All contrast checks pass.

Verification (2026-10-07, local, Debug):
- Native `build/Verify-Shell.ps1`: 342 checks at 1280×720 and 343 at 1920×1080. They cover semantic art appearing
  only for its own concept, typed world/transfer rows, the transfer filters, routes (including a transfer item opening
  its person), Save/Load and Continue.
- Cases: long names (organization, rival, players, sponsors), zero/one/many decisions, fee certainty, no match, empty/many news, empty/many transfers, warning, distress with a negative estimate, and
  missing art (neutral crest, category plate and portrait fallbacks, no invented image).
- Suites: Integration 225, UiKit 171, Domain 4, UI 3, OperatingMap 48, SponsorWorkspace 48, Headless two-cycle;
  native UI kit 24, operating map 101, sponsor 31. UiKit also checks the eight source PNG hashes and that no mockup
  sample names or values appear in presentation code.
- Evidence: [portal-evidence/world-transfer](portal-evidence/world-transfer/).

Known gaps:
- At 30–40 px the 3D module art is less legible than the vector icons beside it.
- Inbox, Competition, Commercial and Finance have no approved art.
- A departure that has already left the market is shown only in the Inbox.
- Physical input and DPI (DV-03/04) and human review remain open.

## Surface polish pass (code only, no new art) — 2026-10-07

Status: **IMPLEMENTED, candidate**. This pass changes styling only; the information architecture, data and gameplay are
unchanged.

**Surface system** (`UI/Theme/Surfaces.cs`). `SurfaceStyle` is one scriptable `StyleBox` that draws a layered material
in a single node:
- outer shadow;
- vertical tonal fill;
- inner cool-steel stroke;
- dark outer frame;
- top-edge highlight;
- an optional lower lip (recessed);
- an optional brass accent.

`Surfaces` is the only place the materials are defined:

| Level | Material | Used for |
|---|---|---|
| 0 | `RoomBackdrop` (faint light, dark floor) | The app background behind the content |
| 1 | `Region` | Major Portal regions |
| 2 | `Card` | Cards and rows |
| 3 | `Selected` (brass leading bar) | The current selection |
| — | `Well` (recessed) | Art frames, filter tracks, the money forecast |
| — | `Bar` | Top bar and rail |
| — | `Primary` / `Secondary` buttons | Brass bevel / steel bevel |

The existing theme variations (Button, PrimaryAction, SecondaryAction, AccentOutline, Card, Entity, Nav, NavSelected)
use these materials and keep their content margins, so other screens do not move. New variations: `Segment`,
`SegmentSelected`, `PanelRegion`, `PanelCard`, `PanelWell` and `SectionCaps` (display caps with tracking).

**Button states.**
- Normal → hover: lighter, with more lift.
- Pressed: the gradient inverts, the highlight drops, and the content sinks 1 px (`Surfaces.PressShift`; `CardButton`
  does the same).
- Disabled: flat graphite.
- Focused: the existing outer ring.

Brass is reserved for Continue, the decision verbs and the current selection.

**Components** (`UI/Components/Framing.cs`, `Primitives.cs`):
- `SectionHeader.Region`: icon in a recessed badge, spaced caps title, muted context, one route, and an engraved rule
  led by a brass mark.
- `EngravedRule`.
- `PanelFrame.Region` / `Well` / `Overlay`: the overlay frames art without filling it.

The Portal's inline styles were replaced by these. Tokens are now `graphite-brass-v2`, adding the material roles
FrameOuter, FrameStroke, FrameHighlight, SurfaceShade and AccentMuted.

**Shell.**
- Top bar and rail use the bar material; top-bar dividers are engraved.
- The selected rail item is a raised brass-edged material with a semibold label; unselected items stay quieter.
- An engraved rule closes the company identity block.

**Verification (2026-10-07, local, Debug).**
- `build/Verify-Shell.ps1`: 358 checks at 1280×720 and 359 at 1920×1080. New checks:
  - every button state is a layered material;
  - hover differs from normal;
  - a press sinks the content without changing its size;
  - the selection carries a brass leading edge.
- Suites: Integration 225, UiKit 174 (material token order, brass label contrast), Domain 4, UI 3, OperatingMap 48,
  SponsorWorkspace 48, Headless two-cycle (hash unchanged).
- Native: UI kit 24, operating map 101, sponsor 31.
- Evidence: [portal-evidence/polish](portal-evidence/polish/) — `before-*` (pre-polish), the polished Portal, the
  `surface-states-*` board (a DEVELOPMENT verification board, never shown in play) and stress cases.

**Gaps that need artwork rather than code.**
- The mockup's photographic news and match imagery, and player/staff portraits.
- The arena/office atmosphere behind the top bar.
- 3D icons for the four sections without approved art.
- The lower rail ambience (`ui_sidebar_ambience_v1.png`, deferred by instruction).

## Surface polish v2 — chamfered frames and real elevation — 2026-10-08

Status: **IMPLEMENTED, candidate**. The first polish pass was judged insufficient: regions still read as flat dark
rectangles with a thin border. Sampling the capture explained why:
- Room, region and card were within about 10 levels of each other (#060607 / #111418 / #161A1F).
- The steel stroke (#343B45) was too dark to read.
- Black shadows on a near-black room are invisible.
- Headers were text on the slab.

The reference frames are instead cut-corner slabs with a bright steel rim (about #6F7B88) that turns brass at the cut
corners, a dark groove inside, and darker inner plates.

**Frame material** (`UI/Theme/Surfaces.cs`). `SurfaceStyle` is one scriptable StyleBox per frame with:
- top-left and bottom-right chamfers;
- a three-layer soft shadow plus a tight contact shadow;
- a vertical fill gradient;
- a recessed inner shade;
- a dark outer frame;
- a metallic rim lit from above (bright top, dark bottom) with brass at the chamfers fading over a set reach;
- an inner groove, a lower lip and a brass accent bar.

**Tiers** (tokens `graphite-brass-v3`; UiKit enforces the order and a ≥ 1.1 contrast step):

| Tier | Material | Colours / elevation |
|---|---|---|
| Room | SurfaceBase + room backdrop | #0C0E11 |
| Major region | `Region` | #1C2025 slab, rim #76808C → #3E4550, brass chamfers; contact shadow .9 / 3 px; soft shadow .75 over 12 px with a 7 px drop |
| Header band | `Header` | #282D34, lit top edge, steel-lipped lower divider, brass leading bar |
| Nested plate | `Card` / `Row` / `RowAlt` | #121417, dimmer rim, contact shadow |
| Hover / lift | `Lift` | #2E343B, bright rim |
| Selection | `Selected` | warm graphite, brass rim, brass bar |
| Recessed | `Well` / `Sunk` | Dark with an inner shade from the top |

**Components.**
- `PanelFrame.Major`: the region frame with its header band and padded content.
- `PanelFrame.Card` / `Well` / `Overlay`.
- `SectionHeader.Region`: the header band, with the icon in a well and a brass glyph.
- Links (View all, Finance, Squad market) are small steel secondary buttons.
- Lists:
  - World news: the lead story is an elevated card; secondary stories are row plates.
  - Transfer news: rows are alternating plates.
- Data blocks:
  - Next match: VS sits in a recessed badge.
  - Season record: the ring in a data well beside a metric plate.
  - Money: a metric plate beside a forecast well.
- Empty states sit on a plate with a status badge.
- Rail: the selected section's icon sits in a well.

**Buttons.** Primary is a brass bevel (bright rim on top, dark below, dark structural frame, contact and soft shadow).
Hover is brighter. Pressed inverts the bevel, shades the top, drops the lift and sinks the content 1 px. Local
decision verbs keep the material without the soft lift, so Continue stays strongest. Secondary buttons are graphite
with a steel bevel. Disabled is a flat plate. Focus is the outer ring.

**Verification (2026-10-08, local, Debug).**
- `Verify-Shell` 358 checks (1280×720) and 359 (1920×1080).
- Suites: UiKit 189 (tier order and steps, lit-rim contrast, header-band text contrast), Integration 225, Domain 4,
  UI 3, OperatingMap 48, SponsorWorkspace 48, Headless two-cycle (hash unchanged).
- Native: UI kit 24, operating map 101, sponsor 31.
- Evidence: [portal-evidence/polish-v2](portal-evidence/polish-v2/), with `before-*` = the first polish pass.

The lower rail ambience (`ui_sidebar_ambience_v1.png`) and the reference's photographic imagery remain artwork gaps.

## Surface polish v3 — structural geometry and metallic chrome — 2026-10-08

Status: **IMPLEMENTED, candidate**. This is a styling pass. Data, routes, actions and gameplay are unchanged. Human and
Director visual acceptance is pending.

**What v2 drew.** `SurfaceStyle` (one scriptable StyleBox per frame) already drew real polygons. Four things kept the
Portal reading as bordered rectangles:
- Only the top-left and bottom-right corners were cut.
- Brass was a tint at the cut corners that faded along the rim, so it read as corner decoration.
- The header band was a separate rectangle inset in the region, with a square right end.
- Each region was outlined by an opaque near-black frame, a black groove and a one-pixel contact halo.

Focus rings were rectangular `StyleBoxFlat`s whatever the frame's shape.

**Approach.** One geometry, reused everywhere. There are no per-panel polygons, textures or screen coordinates.

- `UI/Tokens/FrameGeometry.cs` is engine-independent and tested by UiKit. It provides:
  - `Cuts`: four 45° corner legs, with inset and outset that keep chamfers parallel, and a fit for small controls;
  - the outline and facet math;
  - one light direction;
  - a containment test.
- `SurfaceStyle` takes per-corner cuts and draws:
  - a structural **wall**: eight flat-shaded facets lit from the upper left, as one triangle array with shared vertices, so
    there are no seams. Brass inserts replace chosen chamfer facets;
  - a graphite **shadow edge** (alpha ≤ 0.6) instead of the black frame;
  - a graphite groove and a faint inner highlight;
  - rows that read by tone (`RimMode.Edges`);
  - a **focus ring** that follows the same cuts.
- `HeaderPlate` (`UI/Components/Framing.cs`) is the header plate. It draws the title tab and follows the laid-out title,
  not fixed coordinates.
- `PanelFrame.Mask` clips art and washes to a chamfer (`ClipChildren`), so square art never pokes past a cut.
- `FitOrHide` shows optional header context only when it fits whole.

**Tokens** (`graphite-brass-v4`, `UiTokens`):

| Token | Value | Use |
|---|---|---|
| `ChamferControl` | 4 px | Buttons, rows; filters and tags use 3 px |
| `ChamferSmall` | 6 px | Nested plates, icon housings, rail selection, match stage, lead news art |
| `ChamferMedium` | 11 px | Standard regions |
| `ChamferLarge` | 16 px | Premium regions |
| `EdgeHairline`, `EdgeRim`, `EdgeWall` | 1, 2, 3 px | Hairline rims; brass wall of Continue and the rail selection; region wall |

All cuts are 45°. Only the leg length changes with importance.

**Functional hierarchy** (`ShellLayout.RegionTier`):

| Tier | Elements | Treatment |
|---|---|---|
| Premium | Key decisions, Next match, Continue | 16 px octagon. Brass inserts on the top-left and bottom-right chamfer facets. Continue: 8 px diagonal cuts and a 2 px brass wall |
| Standard | World news, Transfer news, Season record, Money | 11 px octagon. One brass insert (top-left) |
| Utility | Rows, filters, tags, small buttons, metadata | 3–6 px diagonal cuts, hairline or tone-only edges, no brass |

**Headers.** Each header is a raised plate seated flush in the wall:
- Its top corners sit on the wall's inner chamfer.
- Its lower ends are cut, so it reads as a trapezoid with diagonal seams to the wall.
- A brighter upper steel edge runs along the top, with a dark lower inset line over a steel lip at the bottom.
- The icon sits in the shared housing.
- The title sits on a raised tab that ends in a 45° slant, followed by a narrow inset seam and a brass stripe (the header's
  one brass marker).
- The tab's reach and the following gap come from the title and context font sizes.

Supplementary context is shown whole or not at all. At 1280×720 the Season record header omits "Season 1"; the season is in
the top bar.

**Sidebar.**
- The company identity sits on a cut header plate whose lower inset line leads into the navigation (it replaces the thin
  rule).
- Rail items share an octagon in every state. The current item is a two-pixel brass wall with bright chamfer facets and the
  leading bar.
- The selected icon sits in the shared icon housing.
- No routes or art were added.

**Buttons.**
- Every state of a control and its focus ring share one geometry.
- Continue is the only premium control.
- Decision verbs keep the brass material with 4 px cuts and no lift.
- Secondary buttons and header links have 4 px cuts. Prepare match has 6 px cuts.
- Filter tabs and header links keep at least a 24 × 24 px click target (`UiTokens.MinimumTarget`, `CardButton.Compact`).
- Chips, counts, badges, the development note and the toast are chamfered utility tags.

**Also fixed.** At 1920×1080 a long lead headline used to push the story into its art frame's bottom margin, where the
frame could clip it. The frame now grows with its story; its vertical margins are 12 px.

**Verification (2026-10-08, local, Debug).**
- `Verify-Shell`: 917 checks at 1280×720 and 933 at 1920×1080. New chrome checks, on every case at both sizes:
  - every region is cut at all four corners with its tier's wall and brass;
  - the header plate sits flush inside the wall (≤ 0.5 px) on the inner chamfer, with cut lower ends;
  - the tab slant clears the title's glyphs at the baseline, and the next header text clears the slant and stripe;
  - header routes are aligned with the title and inside the plate;
  - no text or art crosses any surrounding chamfer (art inside a masking frame excepted);
  - all Portal and top-bar controls are at least 24 × 24 px;
  - the rail identity plate is present, and the current rail item is an angled brass frame with the shared housing;
  - every control kind keeps one geometry across normal, hover, pressed, disabled and focus;
  - cut size and brass follow the hierarchy, and Continue is the richest control;
  - shadow edges stay at alpha ≤ 0.6, and rows are tone-defined.
- Suites:
  - UiKit 199. New checks: token ranges; every edge horizontal, vertical or 45° at any size; fit; parallel insets; corner
    containment; light order.
  - Integration 225, Domain 4, UI 3, OperatingMap 48, SponsorWorkspace 48.
  - Headless two-cycle: hash `fcf6fc3e…`, unchanged.
- Native: UI kit 24, operating map 101, sponsor 31. The sponsor focus check now accepts the cut focus ring through
  `UiTheme.VisibleFocus`; `SelectedEntity` keeps its square ring.
- Evidence: [portal-evidence/polish-v3](portal-evidence/polish-v3/).
  - `before-*`: v2.
  - `detail-before-after-1280x720.png`: a 2× comparison of corners, junctions, rail, Continue and headers at true pixels.
  - The surface-states board adds a DEVELOPMENT frame-tier strip.

**Remaining differences from the reference.**
- The reference's warm gold rims on every frame are intentionally steel. Brass marks junctions only.
- Its photographic news, match and arena imagery, player and staff portraits, and the lower rail ambience
  (`ui_sidebar_ambience_v1.png`) still need rendered artwork. No art was generated.
- 3D icons for Inbox, Competition, Commercial and Finance still need rendered artwork.
- The geometry, bevels, seams and brass inserts need no assets.

## Polish standard applied to every screen — 2026-10-08

Status: **IMPLEMENTED, candidate**. The rules of polish v1–v3 are consolidated in one normative file, the
[UI polish standard](../ui-ux-foundation/UI_POLISH_STANDARD.md). Every player-facing screen now follows it. Information
architecture, data, routes and gameplay are unchanged.

**Shared pieces, so screens do not style themselves:**
- **Theme panel tiers.** `SurfaceRaised` is a standard region, `SurfaceInset` a nested plate and `SurfaceOverlay` a
  premium region. Kit structures, the sponsor workspace and the company view inherit these with unchanged content margins.
- **Inputs** are recessed wells with control cuts, and their focus ring follows the cut.
- **Choice pop-ups** are cut plates without an outer shadow.
- **Scrollbars and tooltips** use the tag material.
- **Tables:**
  - no panel of their own;
  - a header strip in caption caps;
  - rows by tone (the screen paints alternate rows and clears the tint under the selection);
  - a brass-edged selected row;
  - no guides;
  - heights from the table's own metrics, so no band of empty space.
- **Section helpers** (`ShellScreens.cs`):
  - `Page`: display title, supporting line, engraved rule with a brass lead;
  - `Card`: a region of a tier with its header plate; the question becomes the header context and its tooltip;
  - `Caps`.

**Screens:**
- **Inbox:** one Messages region; filter segments with counts; message row plates with kind, unread, day and the message.
- **Squad, Competition, Commercial, Finance, Staff:** regions by tier (see the standard's tier table).
- **Start screen:** the New campaign premium region.
- **Confirmation:** a premium region whose header names the commitment.
- **Offer workspace:** the page rule, an Offer terms premium region with the terms document as a nested plate, and a
  Company trade-off region. The reading-height fit adds the regions' chrome, so its no-dead-space check still holds.
- **Company view:** Company structure and Context regions.

**Verification (2026-10-08, local, Debug).**
- `Verify-Shell`: 1176 checks at 1280×720 and 1195 at 1920×1080. The chrome rules now also run on every section, the
  offer workspace, the confirmation and the start screen. Tables are checked to show every row with ≤ 12 px spare.
- Suites: UiKit 199, Integration 225, Domain 4, UI 3, OperatingMap 48, SponsorWorkspace 48, Headless two-cycle (hash
  unchanged).
- Native: UI kit 24, operating map 101, sponsor 31.
- Evidence: [screen-evidence/polish-standard](screen-evidence/polish-standard/), with `before-*` = after polish v3; the
  capture set now includes the confirmation.

**Known limits.**
- Section screens keep their existing composition. The approved section mockups (hero art, per-row bars, portraits,
  league and training) need mechanics or art first.
- "Return to company · Esc" in the offer workspace still wraps to two lines.
- The company identity swatch keeps its brand box.

## Review fixes — 2026-10-08

A review of the known defects found the following. Data, routes and gameplay are unchanged.

| # | Defect | Fix |
|---|---|---|
| 1 | `dotnet build -c Release` (the default of `build/Verify.ps1`) failed with NU1004. The Godot SDK references `GodotSharpEditor` in Debug only, while one lock file serves every configuration. | `Client.csproj` references it without assets outside Debug. Release, ExportRelease and Debug restore in locked mode, and no editor assembly is output outside Debug. |
| 2 | The UiKit, OperatingMap and SponsorWorkspace test projects were not in `ManagementGame.slnx`. `Verify.ps1` ran three of six suites and could only write into tracked evidence. | The projects are in the solution. `Verify.ps1` runs all six suites and takes `-Evidence <dir>`. |
| 3 | Action labels wrapped mid-phrase: "Return to company · Esc", "Continue saved campaign", "Use coach recommendation". | Shared buttons no longer wrap. They grow with the label, and action rows flow. |
| 4 | The fixtures table gave its widest column to Day and trimmed opponent names and results ("Saltmarsh F…", "✓ Vict…"). | Tables take a naming column (Opponent for fixtures; equal columns for the cash outlook). Every cell keeps its full text as a tooltip. |
| 5 | Rounded boxes remained: brand swatches, the organization panel, identity plates and flat selection boxes. | Swatches are tags, the organization panel is a plate, identity plates take the control cut, and `CornerRadius` is 0. |
| 6 | The root README still said Phase 4. The Vertical Slice README listed the skeleton as active. | Both now describe Phase 5 as it stands, including how to build and verify. |

The earlier known limits "Return to company · Esc wraps" and "the company identity swatch keeps its brand box" are
resolved.

**New checks** in `ChromeVerification.cs`, on every screen:
- no rounded boxes;
- action labels stay on one line and fit their button;
- every table cell keeps its full text as a tooltip.

**Verification (2026-10-08, local).**
- `Verify-Shell`: 1228 checks at 1280×720 and 1246 at 1920×1080.
- `Verify.ps1` in both Release and Debug: all six suites (Domain 4, Integration 225, UI 3, UiKit 199, OperatingMap 48,
  SponsorWorkspace 48) and the headless two-cycle run. The Integration and Headless hashes are identical across
  configurations.
- Native: UI kit 24, operating map 101, sponsor 31.

**Still open, not defects of this build** (they need decisions or art):
- a Director DEC entry for rules v2;
- the rules v2 balance findings;
- human visual review;
- art gaps;
- DV-01 to DV-10.

## Portal redesign — layout, content and hierarchy — 2026-10-08

Status: **IMPLEMENTED, candidate**. This changes composition, content, sizing and hierarchy only. The frame system,
data, routes, actions and gameplay are unchanged. The rules are recorded in the
[UI polish standard](../ui-ux-foundation/UI_POLISH_STANDARD.md) (shell composition, text hierarchy).

**Decision cards** (`PortalScreen.DecisionCard`).
- **Areas:** a context row (category caps, due date); then the identity on the left, the decision in the centre, and the
  action at the bottom right.
- **Sponsor offers** show the brand's mark and name only. The "Offers N CU a week for W weeks" line is gone from the card;
  the terms stay in the offer review and in the data.
- **Consequence lines** show on roomy cards only.
- **Subjects** are 16 px compact and 20 px roomy.
- **Art** is 48 px compact and 56 px roomy.
- **Compact cards** narrowed their padding so ordinary brand names fit on one line.

**No narration or developer text.**
- Continue no longer shows "Advanced to day N: …". The result arrives as the NEW match story, the decision cards and the
  inbox count.
- Remaining player notices (warnings, rejections, save and load) dock at the bottom of the rail; a collapsed rail floats
  them at the bottom right.
- The rail's "Development build / Noncanonical fixture" block is gone, as are the start screen's and the shell-hosted
  offer workspace's watermarks (the standalone verification host keeps its own). Debug builds log the marker and show it
  in the window title.
- "Internal console" on the start screen exists in debug builds only.
- The Portal's generic header subtitles ("Matters that need an executive call", "Results, market and league notices",
  "Contracts, departures and the open market") are removed.

**Top bar.** It has two regions:
- `TopBarInfo`: season, day, next match (roomy), cash, financial condition and pending decisions, each centred in an
  expanding slot between dividers;
- `TopBarActions`: Save, Load, Continue.

**Rail.**
- Navigation art is drawn by its visible content at 36/40 px, and vector icons stand in at 24/26 px.
- Rows are 48/60 px, with a 14 px gap.
- The crest is 52/64 px; the generic "ESPORTS ORGANIZATION" descriptor is gone.

**Bottom band.** Its roomy height drops from 284 to 268 px. The roomy decision row needed the space, and the bottom regions
did not.

**Verification (2026-10-08, local, Debug).**
- `Verify-Shell`: 1574 checks at 1280×720 and 1598 at 1920×1080. New checks (`LayoutVerification.cs`), on every Portal
  case:
  - each action at the bottom right, clear of the border, with one shared right alignment;
  - identity left and decision centre, with the action never meeting the name;
  - sponsor offers show the brand's mark and name, not their terms;
  - no gap before the top-bar actions wider than between facts;
  - Save, Load and Continue grouped right, Continue last;
  - rail art at least 32 px and centred with its label;
  - Collapse inside the rail;
  - no developer, fixture or narration text in player space;
  - notices docked in the rail.

  The shell flow now checks that the match result arrives as unread news and inbox with no narration.
- `Verify.ps1 -Configuration Debug`: Domain 4, Integration 225, UI 3, UiKit 215, OperatingMap 48, SponsorWorkspace 48, and
  the headless two-cycle run (hash unchanged).
- Native: UI kit 24, operating map 101, sponsor 31.
- Evidence: [portal-evidence/redesign](portal-evidence/redesign/). `before-*` is the state before this pass.

**Remaining.**
- At 1280×720 an extreme organization name still wraps to two lines and ends in an ellipsis; the full name is in the
  tooltip.
- Roomy consequence lines are one line with an ellipsis.
- The company view still names its development fixture scope ("Development Discipline"), which is part of the operating-map
  fixture.
- The approved art for Inbox, Competition, Commercial and Finance was catalogued but not yet in the rail, by instruction.
  The sidebar redesign below integrates it.

## Sidebar redesign — art, transparency and navigation layout — 2026-10-08

Status: **IMPLEMENTED, candidate**. Scope is the left rail and the shared art presentation it uses. The top bar, the
Portal regions, data, routes and gameplay are unchanged. The rules are recorded in the
[UI polish standard](../ui-ux-foundation/UI_POLISH_STANDARD.md) (§6 Rail).

**Art.** Each of the eight sections now shows its own approved module art. `SemanticAssets.ForSection` maps them, and
`SemanticArt` loads each one from `res://Assets/UI/Semantic/`:

| Section | Runtime file (`.webp`, from the `.png` source) | Before |
|---|---|---|
| Portal | `icon_module_portal_v1` | approved art |
| Inbox | `icon_module_inbox_v1` | vector `inbox` |
| Squad | `icon_module_squad_v1` | approved art |
| Competition | `icon_module_competition_v1` | vector `competition` |
| Commercial | `icon_module_commercial_v1` | vector `commercial` |
| Finance | `icon_module_finance_v1` | vector `finance` |
| Staff | `icon_module_staff_v1` | approved art |
| Company | `icon_module_company_v1` | approved art |

No asset was missing. Calendar stays catalogued and unused.

**Black backdrop: root cause and fix.**
- **The images were not the cause.** Every PNG and WebP has a fully transparent background: corners at alpha 0, no
  opaque background pixels, the object at alpha 250–255.
- **The cause was the rail.** `UpdateNav` gave the current item's icon slot the recessed icon housing (`Surfaces.Housing`):
  a near-black well with a dark inner shade. Portal is the home section, so its art always sat in a black square.
- **The fix is in the presentation.** Rail art now sits straight on the item's own surface (flat, the hover plate or the
  brass selection), in a plain `CenterContainer` slot. No image was edited and no derivative was made.
- The import settings were already correct (`fix_alpha_border`, no premultiplication, mipmaps), so they are unchanged.

**Sizes** (compact / roomy):

| | Before | After |
|---|---|---|
| Rail width | 176 | 184 / 208 |
| Art slot | 44 / 48 well around 36 / 40 art | 42 / 46 slot, art at 38 / 42 optical |
| Label | 16, 14 after the slot | 16, 12 / 14 after the slot |
| Item height | 48 / 60 | 50 / 56 |
| Gap between items | 4 | even, 8 to 20 / 40: 11 at 1280×720 (24 px to spare), 40 at 1920×1080 |
| Collapsed width | 64 requested, 84 / 88 actual (the slot overflowed) | 70 / 74, as requested |

- **Optical size.** Art is drawn by its visible content and scaled to the area of the optical square, never past its
  slot (`SemanticAssets.OpticalSize`). The trophy (tall) and the buildings (wide) now read at one size instead of one
  width.
- **Spacing.** `SpreadColumn`, a new container in `Framing.cs`, spreads the items with one clamped gap and half a gap at
  each end. What the gaps cannot take goes to the notice dock, which keeps 52 px above Collapse. The layout follows the
  container's size, with no coordinates.
- **Identity.** At 1920×1080 a 64 px crest stays stacked above the name. At 1280×720 a 42 px crest sits in the art column
  with the name on the label column, which saves about 40 px of height for the items. The name is still the player's
  own, from the model, and its tooltip now works (the label takes hover).
- **Badge and tooltip.** The unread badge is a 24 × 20 tag at the item's right. The unread count is also in the Inbox
  tooltip, so it stays reachable on a collapsed rail.

**Verification (2026-10-08, local, Debug).**
- `Verify-Shell` (headless): 2658 checks at 1280×720 and 2682 at 1920×1080.
  - A new `RailCheck` runs on every Portal case and every section, collapsed and expanded. It checks:
    - the eight sections in order;
    - one item height and even gaps of at least 8 px;
    - nothing touches the identity, the notice dock or Collapse;
    - the rail keeps its width and fits the window without squeezing its items;
    - each item has its own art, at one perceived size inside its slot and cuts, with no housing or backdrop;
    - labels are whole at 16 px on one column, centred with their art;
    - the badge is clear of its label, and the badge and tooltip carry the unread count;
    - only the current section is selected.
  - The shell flow also navigates from a collapsed item and expands again at the same section.
- Captures (unsandboxed, real art): every check above also ran during capture, 1629 at 1280×720 and 1647 at 1920×1080.
- `Verify.ps1 -Configuration Debug`: Domain 4, Integration 225, UI 3, UiKit 216 (new: all eight sections mapped,
  Calendar unused, optical-size maths), OperatingMap 48, SponsorWorkspace 48. The headless two-cycle hash `fcf6fc3e…` is
  unchanged.
- Native: UI kit 24, operating map 101, sponsor 31.

**Evidence:** [portal-evidence/sidebar](portal-evidence/sidebar/). `before-*` is the state before this pass.
- `rail-before-after-1280x720.png` and `rail-before-after-1920x1080.png` put the rails side by side: Portal, hover and
  focus, collapsed.
- `rail-art-detail-1280x720.png` shows the selected Portal item at 4×: the dark housing before, the art on the selection
  after.
- `rail-states-*` shows Inbox in its hover material and Squad with keyboard focus. These are DEVELOPMENT harness states.
- `rail-collapsed-*` shows the collapsed rail.

**Remaining.**
- The Portal source image has a small detached fragment below the building. In the rail it shows as a speck of a few
  pixels. The image is approved and was left as supplied.
- At 1280×720 a very long organization name shows fewer words than before (two lines beside the crest, then an
  ellipsis). The full name is in the tooltip.
- The rail ambience art is still an art gap.

## Top bar typography and USD — 2026-10-08

Status: **IMPLEMENTED, candidate**. The pass covers three things:
- the top bar's composition and typography;
- the shared type system, now one bundled family;
- how player-facing numbers and money are formatted across the game, with CU relabelled as USD.

The sidebar, the Portal regions' layout, routes, data and gameplay are unchanged. The rules are in the
[UI polish standard](../ui-ux-foundation/UI_POLISH_STANDARD.md) (§6 Top bar, §7 Text and numbers).

### Typography

- **Family.** Inter 4.1 (SIL OFL 1.1) ships in `game/Client/Assets/Fonts/Inter/` in three static weights:
  - Regular 400;
  - Medium 500;
  - SemiBold 600.

  Godot imports it like the other assets. Source, hashes and licence are in
  [Assets/Fonts/README.md](../../../game/Client/Assets/Fonts/README.md).
- **What it replaces.** Before, the UI asked the system for Segoe UI and, for titles and numbers, Bahnschrift.
  - These are Windows system faces, so they cannot be redistributed.
  - The look changed with the machine.
  - Two unrelated families were in use, and weights were picked per role.
- **Why Inter.**
  - It was designed for screen UI: tall x-height and open shapes at 12–16 px.
  - It has tabular figures and case-sensitive forms.
  - Its weights cover text, labels and figures in one family.
  - IBM Plex Sans was the alternative. It is wider and more mechanical in dense numeric rows.
- **Weights by role (`UiTokens.Weight`).**
  - Regular: reading text and context.
  - Medium: metric labels and data figures.
  - Semibold: values, titles, emphasis and actions.
- **Figures and caps.**
  - Every role except running text uses tabular figures, and table cells do too.
  - Only caps are tracked, by 1 px.
- **Fallback.** Glyphs Inter lacks (◐ for Estimated, among others) fall back to the system symbol faces.

### Top bar

| | Before | After |
|---|---|---|
| Structure | Icon, caption and value per fact, centred in equal slots between engraved dividers | Metrics, each a label over its value and context. Left-aligned at content width, with equal gaps; no dividers, no icons |
| Season | `SEASON 1` / `Week 1 of 8`, all at one weight | `SEASON 1` / **Week 1** of 8 |
| Day | `DAY 6 / 56` set as the caption, the half beneath | `DAY` / **6** / 56 then First half |
| Next match (1920×1080) | `vs Northwind Gaming (in 5 days)` | **Northwind Gaming** in 5 days (bounded at 240 px, whole in the tooltip) |
| Cash | `2,326 CU` | **$2,326**. Compact from a million (`$1.23M`), exact in the tooltip |
| Financial condition | ✓ Stable | ✓ Stable as a 16 px semibold status in its semantic tone |
| Pending decisions | A small number in a square badge, amber whenever anything was pending, red when blocked | **3** next due tomorrow. `0` all clear, `99+` above 99. Amber `▲ 1 blocking` only when a decision blocks Continue |
| Actions | Save and Load at 16 regular | 16 semibold. Continue keeps its 20 semibold and brass geometry |

- All labels sit on one baseline, and all values and context on another. `Seat` raises smaller text by the difference
  between its descent and the value face's.
- At 1280×720 the labels shorten (`FINANCES`, `DECISIONS`) and the next match is left to the Portal, as before.
- The bar stays 60 px tall.

### Numbers and money

**The formatter.** `ManagementGame.Formatting`, in `src/ManagementGame.Domain/Formatting.cs`, is the one place
player-facing figures are written.
- It holds two classes:
  - `Currency`: `Format`, `Cents`, `Whole`, `Signed`, `Compact`, `PerDay`, `PerWeek`, `Relabel`;
  - `Figures`: `Count`, `Capped`, `Record`, `Percent`, `OfTotal`.
- How it formats:
  - invariant culture;
  - integer arithmetic on cents, never floating point;
  - a true minus (`−$920`);
  - a word joiner inside rates.
- **Why the Domain hosts it.** Notices stored in campaign state carry amounts, so the Domain must format them.
- **How the UI kit gets money.** The kit's pure assembly may not reference gameplay assemblies (a UiKit test checks
  this). So `ResourceView` gains an optional `Display`, and money reaches the kit already formatted.

**Precision by use:**
- `Format` (exact; cents only when present): notices, decisions, confirmations, contract terms, transfer news.
- `Cents`: table columns.
- `Whole` or `Compact`: the top bar and the Money snapshot.

**Tables.**
- Amount columns are right-aligned in tabular figures.
- Amount and date columns are sized to their widest cell and are never trimmed. Before, at 1280×720:
  - `$158.60` showed as `$158.`;
  - `Day 10` showed as `Day 1`;
  - `day 28` showed as `day 2`.
- Units moved out of the column titles (`Fee`, not `Fee (CU)`).
- The proposal input's unit is in its label, `Weekly payment (USD)`. A SpinBox affix would add spaces (`$ 245 /week`).

### CU → USD

**What CU was: a display label only.**
- Money is held as `long` minor units (1/100) in the Domain, the content file and saves.
- "CU" was appended by about ten separate formatters:
  - Domain `Pay.Money`, which writes notices stored in saves;
  - the Application read models (Portal tasks, news, horizon, sponsor summary);
  - `PortalScreen`, `ShellScreens` and `SponsorPresentation`;
  - the internal console and the headless tool.

**How it was migrated.**
- It is a **nominal 1:1 relabel**: one CU minor unit is one cent. Nothing was converted.
- These are unchanged:
  - content;
  - balance constants;
  - formulas;
  - the save schema (3);
  - the content hash.
- `PROJECT_STATE` lists currency unit values as non-canonical, so no approved decision conflicts with it.

**Save compatibility.**
- Saves from before the relabel load. The schema, rules and content hash are unchanged.
- Their stored notices keep their text (`140.00 CU`), and `Currency.Relabel` shows them in dollars at observation.
- The save and its state hash are never rewritten.
- A save from the pre-change build was loaded by the new build (scratch probe: day 161, distress, arrears, 40 inbox
  rows).
  - The state hash was identical: `098227…`.
  - Cash, forecast, bills, contracts, offers, negotiations, coach and brand guides were identical.
  - 11 legacy notices read in dollars with the same figures.
  - Integration now covers this too.

**Economy unchanged.**
- `Measure-Rules -Gate 1` (4 policies × 8 seeds × 5 seasons) gives identical numbers before and after.
- Only one gate-detail string changed, from CU to `$`.

**Determinism hash.**
- The headless two-cycle hash moves from `fd905e55…` to `590688dc…`.
- The only cause is the text of notices generated from now on, which store `$`.

### Verification (2026-10-08, local, Debug)

- **Unit and integration suites** (`Verify.ps1 -Configuration Debug`):

  | Suite | Result |
  |---|---|
  | Domain | 15. New: money, counts, records, percentages, compact values, extremes, legacy relabel, host culture |
  | Integration | 246. New: a pre-relabel save loads unchanged and reads in dollars |
  | UI | 3 |
  | UiKit | 222. New: three weights, the scale, the metric hierarchy, the bundled licence, pre-formatted amounts |
  | OperatingMap | 48 |
  | SponsorWorkspace | 48 |

- **Shell** (`Verify-Shell`, headless): 3106 checks at 1280×720 and 3140 at 1920×1080. New checks:
  - `TopBarCheck` on the live Portal and every Portal case;
  - the retired `CU` unit is checked on every screen, the top bar, the rail and the confirmation;
  - amount and date columns are whole in every table;
  - a `large-values` case: 120 decisions show `99+`, and $1,234,567.89 shows as `$1.23M`.
- **Native kits:** UI kit 24, operating map 101, sponsor 31.
- **Captures** (unsandboxed): both sizes, with every Portal case.

**Evidence:** [portal-evidence/topbar](portal-evidence/topbar/). `before-*` files are the state before this pass.
- `topbar-before-after-*.png` stacks the bar before and after: at 2× for 1280×720, at 1× for 1920×1080.
- `topbar-cases.png` shows these cases:
  - zero decisions;
  - 99+ with cash in the millions;
  - a blocking decision;
  - distress;
  - a long rival name;
  - season complete.
- `finance-*`, `squad-*`, `staff-*` and `commercial-*` show dollars in tables, prose and inputs.

### Remaining

- **Wider text.** Inter is wider than Segoe UI. At 1280×720 some two-word sponsor names in Key decisions now wrap to two
  lines. The standard allows this: names wrap and are never cut.
- **Unrealistic scale.** Amounts are nominal development values. A player's wage reads $7.93–$14.79 a day and a sponsor
  $300 a week. A realistic dollar scale is a balance decision (content and rules) and was deliberately not made here.
- **Historical measurements.** `RULES_V3_INCENTIVES.md` keeps its measurements in CU as recorded; 1 CU = $1.
- **Governance (decision).** No DEC entry records the relabel yet. Like rules v2 and v3 wave 1, it stands on the user's
  explicit instruction.

## Transfer News — editorial news, not a player market — 2026-10-08

Status: **IMPLEMENTED, candidate; layout and data source superseded by v2 below (2026-10-09)**. Full spec, findings and
verification: [TRANSFER_NEWS.md](TRANSFER_NEWS.md).
Evidence: [portal-evidence/transfer-news](portal-evidence/transfer-news/).

- The Portal's Transfer News is now a short editorial feed of **completed personnel moves**. It covers signings,
  head-coach appointments, contract-end departures, returns and same-day roundups. It is no longer a list of free
  agents and coaches with fees, wages, ability and filters.
- The Transfer Market is unchanged and stays in Squad (Free agents) and Staff (Coach market). The panel has no header
  route, because no screen holds every story.
- Stories come from records only, through a new read model:
  - `Observations.Moves` (decision and contract-notice records) → `Situation.Moves`;
  - `PortalProjection.Transfers` (half-season window, returns, roundups, head coach first, at most 4).
  - No domain, content or save change; the headless hash is unchanged.
- Every story is `● CONFIRMED`. The rules have no rival rosters, public interest or prominence data. So there are no
  rumours, rival transfers or "major" labels; [TRANSFER_NEWS.md §4](TRANSFER_NEWS.md#4-supported-and-unsupported-news)
  lists what each would need.
- Checks: Integration 257; Shell 3472 / 3508; the other suites unchanged; captures at both sizes, including a live run
  to the season-1 contract ends.

## Transfer News v2 — transfer journeys, uniform articles, bare identity — 2026-10-09

Status: **IMPLEMENTED, candidate**. Full spec, findings and verification: [TRANSFER_NEWS.md](TRANSFER_NEWS.md).
Evidence: [portal-evidence/transfer-news-v2](portal-evidence/transfer-news-v2/).

- **Journey.** Every story shows who moved, from where and to where: origin → destination. Each side is an
  organization (crest and name), "Free agent" (a status, with no crest) or `○ Team unknown`.
  - Returns come from the free-agent market, with the earlier departure as context only.
  - A team-to-team journey is supported by the read model and UI. The rules have no rival rosters yet.
- **Recorded history.** Transfer history is now domain state.
  - `World.Moves` records each move when it happens: signing, appointment, interim, release, contract end, replaced
    coach.
  - It is bounded to two seasons and saved (save schema 4; schema-3 saves are rejected).
  - Before v2, moves were inferred from current records, so a release erased the signing story and a return erased
    part of a departure.
  - Stories are now fixed by their own history: a later move adds a story and never rewrites one.
  - Headless hash `590688dc…` → `c91319e6…`, with no gameplay change.
- **Layout.** One article for every story: a flat row on the region surface, engraved rules between stories, no
  enlarged lead plate.
  - The identity column (44 / 52 px) holds bare generic player or staff art, drawn by its visible content.
  - The black squares were the `AssetMark` plate, not the images.
  - Headlines are one line; two only for a long name.
- **Checks.** Integration 270; Shell 3792 / 3830; captures at both sizes. They include a live run of departures, a
  return, a signing, an appointment with the replaced coach and a release, then Save and Load.
