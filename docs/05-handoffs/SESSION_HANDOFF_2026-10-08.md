# Session handoff — 2026-10-08 (paused)

Status: **PAUSED by the user.** Everything below was committed in `9b27842` (2026-10-09). Rules v4 continued from there
and partly supersedes [What remains](#what-remains): its phase table in
[RULES_V4_PLAYER_WORLD.md](../10-vertical-slice/RULES_V4_PLAYER_WORLD.md) is the current progress record.

**Repository state when paused.**
- Branch `stage5-sponsor-instruments`, HEAD `14b9f48`.
- The working tree also carried earlier sessions' uncommitted work (Portal redesign, polish passes, semantic assets).

This session did three pieces of work:
- **A.** Sidebar (left rail) redesign. Done and verified.
- **B.** Rules v3 wave 1: incentives and measurement. Done; gate 1 passed.
- **C.** Top bar typography and the CU → USD relabel. Done and verified.

Both are verified (see [How to verify](#how-to-verify)). Everything still open is in [What remains](#what-remains).

## A. Sidebar redesign — done

Full record: [genre-shell README, "Sidebar redesign"](../10-vertical-slice/genre-shell/README.md) and
[UI polish standard §6 Rail](../10-vertical-slice/ui-ux-foundation/UI_POLISH_STANDARD.md).
Evidence: [portal-evidence/sidebar](../10-vertical-slice/genre-shell/portal-evidence/sidebar/).

- All eight rail sections show their own approved module art (`SemanticAssets.ForSection`). Inbox, Competition,
  Commercial and Finance were vector icons before. No asset was missing; Calendar stays catalogued and unused.
- **Black square behind the Portal art.** It was the selected item's icon housing (`Surfaces.Housing`), not the PNG.
  The PNGs have clean alpha. Rail art now sits on the item's own surface; no image was edited.
- **Sizes (720p / 1080p).**
  - Rail width 184 / 208; collapsed 70 / 74 (it used to overflow to 84 / 88).
  - Art slot 42 / 46, drawn at one perceived size (`SemanticAssets.OpticalSize`).
  - Labels 16 px, on one column.
  - Items 50 / 56 px, spread evenly by the new `SpreadColumn` (gap 8–20 / 8–40 px) above a 52 px notice dock.
- **Identity.** At 1280×720 the crest sits beside the name; at 1920×1080 it is stacked above it. The name's tooltip now
  works.
- **Badge.** The Inbox unread count is a pill at the item's right and is also in the tooltip, so it stays reachable when
  the rail is collapsed.
- **Checks.** `RailCheck` runs on every Portal case and section, expanded and collapsed. Capture runs now also produce
  `rail-states-*` (hover and focus) and `rail-collapsed-*`.

## B. Rules v3 wave 1 — done, gate 1 passed

Full record: [RULES_V3_INCENTIVES.md](../10-vertical-slice/RULES_V3_INCENTIVES.md).
Evidence: [rules-v3/evidence](../10-vertical-slice/rules-v3/evidence/).
Source proposal: the user's [Rules v3 doc](https://claude.ai/artifact/2NFWaMQeFuQSEx9XeBoEeH). Its "Kết quả đợt 1"
section and its decision checklist were updated this session.

- **Rules.** `slice-rules-v3`, content schema 3 and save schema 3; v2 saves are rejected.
  - Running cost: 25 CU per day.
  - Registered roster minimum: 6. Below it, each match withholds the prize and costs a fine of 50% of the prize.
  - Sponsor termination clause: 2 forfeits in a season.
  - Season-end reputation: moves 25% of the way toward a target of 15–85, set by the win rate.
- **Bug fix (rules v2).** The predictability penalty counted every result since the campaign began. It now counts the
  last 3 (`PredictabilityWindow`).
- **UI.**
  - Portal: a blocking `RosterShortfall` decision.
  - Finance: a Running costs card; the outlook's day column is whole.
  - Inbox: `League` fine notices and `Commercial` sponsor-termination notices.
- **Measurement.** `tools/Headless --measure` (`Measurement.cs`) plays four owner policies (coach, sponsor-greedy,
  thrift, balanced) × 8 seeds × 5 seasons through the Session API and writes a JSON report.
  `build/Measure-Rules.ps1 -Gate 1` fails if a gate-1 metric fails.

| Gate 1 metric | Rules v2 | Rules v3 wave 1 |
|---|---|---|
| Losing is not profitable (balanced ≥ thrift cash, season 5) | 3/8 seeds | 8/8 |
| Reputation follows results (correlation) | 0.522 | 0.807 |

## C. Top bar typography and USD — done

Full record: [genre-shell README, "Top bar typography and USD"](../10-vertical-slice/genre-shell/README.md) and
[UI polish standard §6 Top bar, §7 Text and numbers](../10-vertical-slice/ui-ux-foundation/UI_POLISH_STANDARD.md).
Evidence: [portal-evidence/topbar](../10-vertical-slice/genre-shell/portal-evidence/topbar/).

- **Font.** Inter 4.1 (SIL OFL 1.1) is bundled in three weights (`game/Client/Assets/Fonts`). It replaces the system
  Segoe UI and Bahnschrift everywhere.
- **Top bar.** The bar is one row of label → value → context metrics: equal gaps, no dividers, two shared baselines.
  Pending decisions is now a metric:
  - `3` next due tomorrow;
  - `0` all clear;
  - `99+` above 99;
  - amber only when a decision blocks Continue.
- **Money.** `ManagementGame.Formatting` (`src/ManagementGame.Domain/Formatting.cs`) is the one formatter for money
  and figures.
  - It is a nominal 1:1 relabel: cents stay cents, and the economy and saves are unchanged.
  - Old saves load with the same state hash. Their stored "CU" notices read in dollars.
  - The headless hash moved to `590688dc…`, only because new notices store `$` text.
- **Tables.** Amount and date columns are right-aligned or fitted and never trimmed.

## D. Portal Transfer News redesign — done (later session, same day)

Full record: [TRANSFER_NEWS.md](../10-vertical-slice/genre-shell/TRANSFER_NEWS.md).
Evidence: `portal-evidence/transfer-news` (removed 2026-10-12, superseded; in git history at `3917e5c`).
Also uncommitted.

- The Portal's Transfer News is now an editorial feed of completed personnel moves. It is no longer a market list. The
  Squad and Staff markets are unchanged.
- The change is in the read model only:
  - `Observations.Moves` and `Situation.Moves`;
  - `PortalProjection.Transfers` with `TransferStory`;
  - `PortalTransfer`, `TransferKind` and `FeeCertainty` are removed.
- No domain, content or save change; the headless hash is still `590688dc…`.
- Checks: Integration 257; Shell 3472 / 3508; captures at both sizes.
- **Open (decision):** rumours, rival transfers and prominence need new simulation mechanisms (TRANSFER_NEWS.md §4).
  They fit naturally into rules v3 wave 4 ("active rivals").

## E. Transfer News v2 — done (2026-10-09, next session)

Full record: [TRANSFER_NEWS.md](../10-vertical-slice/genre-shell/TRANSFER_NEWS.md).
Evidence: [portal-evidence/transfer-news-v2](../10-vertical-slice/genre-shell/portal-evidence/transfer-news-v2/).
Also uncommitted.

- **Journeys.** Transfer News shows every move as origin → destination: an organization's crest and name, "Free
  agent", or an explicit unknown.
- **Domain and save change.**
  - `Move` and `World.Moves` record each move when it happens: signing, appointment, interim, release, contract end,
    replaced coach.
  - Save schema 4; schema-3 saves are rejected.
  - Rules, content and balance are unchanged. The headless hash moved to `c91319e6…` only because the state gained the
    field.
- **History.** Stories are fixed by their own history, so a release or a return no longer rewrites an earlier story.
- **Layout.** Uniform articles: no lead plate, and bare identity art with no plate behind people.
- **Open (decision):** rival moves, rumours and prominence still need new simulation mechanisms (TRANSFER_NEWS.md §8).

## What remains

In suggested order. Items marked **(decision)** need the user or the Director before work starts.

### 1. Review and commit (decision)

- Review the full working-tree diff, which includes earlier sessions' uncommitted work, and decide how to split
  commits.
- Run [How to verify](#how-to-verify) again before committing.

### 2. Governance (decision)

- Director DEC entries to confirm rules v2 and rules v3 wave 1. Both are recorded as "ACTIVE — explicit user
  instruction" and are not yet DEC-confirmed.
- The proposal says each later wave needs a DEC entry before it starts.
- The USD relabel (section C) also stands on explicit user instruction, with no DEC entry yet.
- A realistic dollar scale is a separate balance decision. Today a wage reads $7.93–$14.79 a day.

### 3. Insolvency dead end — fix before making the economy harsher

- A company in distress with zero cash and no incoming payment within 3 days cannot advance.
  - Releasing a player needs cash.
  - The weekly market does not run while time is stopped.
- This was unreachable in v2. It is now reachable: the thrift policy hit it in 1 of 32 runs.
- The user chose **forced restructuring (DEC-016), no campaign end** as long-term failure. Build that path first; it
  belongs in wave 2.

### 4. Rules v3 wave 2 — season structure

Gate 2: multi-year progress (league place improves ≥ 1 by season 5, not at the ceiling before season 4) and empty
stretches ≤ 10 days. Today: 13 days; progress is not measurable.

**Decided by the user (2026-10-08):**
- long-term failure → forced restructuring;
- longer seasons with **6–8 rivals**, not 56 days with 4.

**Still open (decision):**
- promotion and relegation;
- whether rival-vs-rival matches stay within DEC-015's bounded rival simulation.

**Work:**
1. League table (`Standing` per season):
   - rivals play each other with a reduced resolver;
   - top 4 go to the playoffs (semi-final, final);
   - prizes by place.
   - Content changes: `SeasonLength`, `RivalCount`, schedule, and season-end archiving of rival-vs-rival results.
2. Board objectives at season start (2–3 by company tier) and board confidence 0–100 that affects next season's budget.
   Two low seasons force restructuring.
3. Off-season (days 42–56) becomes a transfer window and pre-season camp: rest (Readiness) or train (growth, costs
   money).
4. The meta shifts 2–3 times a season, with Inbox rumours whose reliability follows Information.
5. Re-tune the economy with the measurement:
   - the reputation target should use league place too;
   - check financial pressure (now 0.6% of seasons; target 20–60%).
6. UI:
   - Portal: mini league table and objective cards;
   - Competition: full table and playoff bracket;
   - a season summary event screen.

### 5. Rules v3 waves 3–4 (later)

**Wave 3 — long-term progression.** Gate 3: no solved preparation (≤ 5 points between the best and the mean focus pair)
and ≥ 2 decisions a week (today 1.80).
- Player lifecycle: age, hidden potential, growth and decline, form, morale.
- Facilities: analysis room, training centre, commercial department, assistant coaches, academy. These are the cash
  sinks.
- Weekly focus pairs replace the % split. Today the coach always recommends 50/30/20 or 25/25/50.
- Information comes from facilities and scouting.
- Rotation decisions replace the Readiness tax. Readiness now drains about 8 per season.

**Wave 4 — richness.**
- Active rivals: they buy players, bid for yours, have identities and rivalries.
- Events with choices.
- Multi-term negotiation.
- Bo3 matches and a match-day screen.

**Open (decision):**
- what the post-match analysis may show, and whether true probabilities are ever shown;
- how much age and potential information is visible;
- how many event templates a season needs, and who writes them.

### 6. Sidebar leftovers (small)

- The Portal source PNG has a detached fragment below the building. It shows as a speck of a few pixels in the rail.
  The approved asset was left as supplied; the user decides whether a derivative is wanted.
- At 1280×720 a very long company name shows fewer words beside the crest. The full name is in the tooltip.
- At 1280×720 the notice dock keeps 52 px (two lines). A three-line notice is clipped at the top.
- Hover in the evidence captures is forced by a style override in the harness, not a real pointer.

## How to verify

| What | Command | Last result (2026-10-09, Debug) |
|---|---|---|
| Build, all suites, headless | `build/Verify.ps1 -Configuration Debug -Evidence artifacts/verify` | Domain 15, Integration 270, UI 3, UiKit 222, OperatingMap 48, SponsorWorkspace 48; headless `c91319e6…` (was `590688dc…` before transfer history, `fd905e55…` before the USD relabel) |
| Shell (headless) | `build/Verify-Shell.ps1` | 3792 checks at 1280×720 and 3830 at 1920×1080 |
| Native kits | `build/Verify-UiKit.ps1`, `build/Verify-OperatingMap.ps1`, `build/Verify-SponsorWorkspace.ps1` | 24, 101, 31 |
| Rules gate | `build/Measure-Rules.ps1 -Gate 1 -Output artifacts/rules-v3` | Gate 1 pass |
| Captures (unsandboxed, interactive desktop) | `build/Capture-Shell.ps1 -Results 1 -Cases -Output artifacts/<dir>`; add `-Transfers` for the live transfer run | both sizes pass (2026-10-09, with `-Cases -Transfers`) |

Scratch outputs of this session are in `artifacts/` (ignored by git):
- `sidebar-before`, `sidebar-after`;
- `rules-v3/variants`: the economy sweep;
- `rules-v3/shell`.

## Where the code lives

| Concern | Files |
|---|---|
| Rail layout and checks | `game/Client/UI/Shell/GameShell.cs`, `ShellLayout.cs`, `LayoutVerification.cs`, `ChromeVerification.cs`, `PortalVerification.cs` |
| Art mapping and perceived size | `game/Client/UI/Contracts/SemanticAssets.cs`, `game/Client/UI/Identity/SemanticArt.cs` |
| Even spreading container | `SpreadColumn` in `game/Client/UI/Components/Framing.cs` |
| Wave 1 rules | `src/ManagementGame.Domain/Model.cs`, `Finance.cs`, `Market.cs`, `Simulation.cs`, `CampaignFactory.cs` |
| Content, saves, validation | `content/fixture.json` and `.sha256`, `src/ManagementGame.Infrastructure/ContentLoader.cs`, `SnapshotDtos.cs`, `SnapshotStore.cs`, `SnapshotValidation.cs` |
| Observation, Portal task, money | `src/ManagementGame.Application/Contracts.cs`, `Observations.cs`, `PortalTasks.cs`, `PortalView.cs`, `SponsorWorkspace.cs` |
| Finance screen and card wording | `game/Client/UI/Shell/ShellScreens.cs`, `PortalScreen.cs` |
| Measurement | `tools/Headless/Measurement.cs`, `tools/Headless/Program.cs`, `build/Measure-Rules.ps1` |
| Money and figure formatting | `src/ManagementGame.Domain/Formatting.cs` (`Currency`, `Figures`) |
| Type system, bundled font | `game/Client/UI/Tokens/UiTokens.cs`, `game/Client/UI/Theme/UiTheme.cs`, `game/Client/Assets/Fonts/` |
| Top-bar metrics and checks | `GameShell.cs` (`BuildTopBar`, `Metric`, `Seat`, `PendingMetric`), `LayoutVerification.cs` (`TopBarCheck`), `ChromeVerification.cs` (`CurrencyTextCheck`, whole table columns) |
| Tests | `tests/Integration/Program.cs`, `tests/UiKit/Program.cs`, `tests/SponsorWorkspace/Program.cs` |
