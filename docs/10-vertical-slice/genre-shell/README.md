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
