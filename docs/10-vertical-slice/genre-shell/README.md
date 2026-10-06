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
