# Portal Transfer News — transfer journeys from recorded history

Status: **IMPLEMENTED, candidate** (v2, 2026-10-09). Human/Director visual acceptance is pending.

Sources:
- v1 (2026-10-08): the user's brief "Portal Transfer News redesign — editorial news, not a player market". It turned the
  Portal's market list into an editorial feed.
- v2 (2026-10-09): the user's brief "Transfer News v2 — transfer journey, consistent article layout & clean identity
  presentation". It added who moved from where to where, recorded transfer history, uniform articles and bare identity
  art.

Evidence: [portal-evidence/transfer-news-v2](portal-evidence/transfer-news-v2/) (v1:
[portal-evidence/transfer-news](portal-evidence/transfer-news/)).

## 1. Responsibilities

| Area | Question it answers | Where it lives |
|---|---|---|
| **Transfer News** (Portal) | Who moved, from where to where, and what happened? | `PortalScreen.Transfers.cs`, read model `PortalView.Transfers` |
| **Transfer Market** | Whom can the organization sign or hire, and on what terms? | Squad → Free agents; Staff → Coach market (unchanged) |
| **World News** (Portal) | What else is happening: results, offers, league, commercial | `PortalScreen.News.cs` (unchanged) |
| **Key decisions** (Portal) | What must the player decide now (including renewals) | `PortalTasks` (unchanged) |

The Portal lists no free agents, coaches, fees, wages, ability ranges, availability or market filters. Its Transfer News
header has no route: no screen holds every transfer story yet. The market stays reachable from Squad and Staff.

## 2. Findings before v2 (inspected 2026-10-09)

**Layout.**
- The first story sat on a raised, nearly black `Card` plate with a 48 / 72 px portrait and a crest badge. The others
  were flat rows with 34 / 44 px portraits. Nothing about the first story justified the extra weight: it was simply
  first after ranking.
- Headlines said "X joins Y" or "X leaves Y". Neither the previous organization nor the destination was shown.

**Root cause of the square image backgrounds.** It was the container, not the image.
- `AssetMark` was created with `plate: true`. It drew a graphite plate (`DrawPlate`: inset surface, control cut,
  hairline rim and shadow edge) behind the art.
- The art was then drawn at 86% of the plate, by its full canvas.
- The approved PNGs are clean RGBA with fully transparent backgrounds (checked again). Their visible content fills only
  about 68–71% of the canvas (player bust 698 × 731 of 1024; staff art 729 × 582).
- So the figure covered roughly 60% of a dark box. It read as an inventory slot.
- No image pixel was black background, and no image was edited.

**Data.** v1 *inferred* moves from current records instead of storing them:

| Movement | Recorded before v2? | Previous team reliable? |
|---|---|---|
| Free agent signs (`SignPlayer`) | A generic `decision:` review; the person was found only through a live contract | Free agent by rule; but the story **disappeared** when the player was later released |
| Head coach appointed (`HireCoach`) | Same as signing | Free agent by rule |
| Head coach replaced by an appointment | **Not recorded**: the old coach went to the coach market silently | — |
| Player released (`ReleasePlayer`) | **Not recorded** with a name | — |
| Player or coach leaves at contract end | A `notice:contract:` review naming the contract | Company → free agent; but the person was named only while still on a market |
| Interim coach after a contract end | Only in notice text | — |
| Person no longer in any record (listing lapsed) | Story **dropped** | — |
| Rival rosters, rival moves | Do not exist | — |

So later changes rewrote history:
- a release erased the signing story;
- a lapsed listing erased a departure;
- a return removed the departure ("3 players leave" became the reading after one of four came back).

The current `TeamId` could not reconstruct a journey, as the brief anticipated.

## 3. Data model: transfer history (domain and save)

A typed move is recorded **when it happens** and never rewritten:

```
Move(Id, Day, PersonId, Name, Kind: Player | HeadCoach, Role, Reason, Origin?, Destination?, CauseId)
```

| Field | Meaning |
|---|---|
| `Origin`, `Destination` | The organization the person left and joined. `null` is the open market: the person is a free agent. |
| `Name`, `Role` | The person as they were then. A move stays tellable after the person leaves every current record. Organization names are not snapshotted: the company and rivals are stable records. |
| `Reason` | `Signed`, `Appointed`, `Interim`, `Released`, `ContractEnded` or `Replaced`. |
| `CauseId` | The command or contract behind the move. A replaced coach and their successor share it, and so do a coach leaving at contract end and the interim. |
| `Id` | `move:<cause>:<person>`, unique. |

**Where moves are written.**

| Writer | Moves |
|---|---|
| `Simulation.Apply`: `SignPlayer` | Signed: free agent → company |
| `Simulation.Apply`: `ReleasePlayer` | Released: company → free agent |
| `Simulation.Apply`: `HireCoach` | Replaced: old coach, company → free agent; then Appointed: new coach, free agent → company |
| `Market.Contracts` | ContractEnded: company → free agent. For a head coach, also Interim: free agent → company |

Renewals are not moves.

**Ownership, bounds, saves.**
- **Ownership.** Moves are `World.Moves`, world history like `World.Results`, in recorded order.
- **Bound.** The season rollover keeps the season just closed and the new one. The story window is half a season.
- **Save schema 4.** The save adds `MoveDto`. Schema-3 saves are rejected: this development build supports only its
  exact identity, as with v2 → v3.
- **Unchanged.** No rule, content, RNG draw or balance changes. Gameplay facts are identical; only the canonical hash
  moves, because the state has a new field. Headless two-cycle: `590688dc…` → `c91319e6…`.
- **Validation** (`SnapshotValidation`):
  - IDs are unique and days are in order;
  - days lie within the current and the previous season and are not in the future;
  - kind and role agree;
  - the direction is the one the reason allows: joins come from the market to the company, leaves go from the company
    to the market.

The inbox resolves a contract-end notice's subject from history first. Before that, it falls back to the markets.

## 4. Pipeline

`recorded move → eligibility → editorial selection → story`, never "every listed player → a row".

1. **Observation** (`Observations.Moves`). `World.Moves`, newest first, as `MoveRow`: kind, day, person, name, role,
   from, to, cause. It is read as recorded and never rebuilt from current rosters.
2. **Projection** (`PortalProjection.TransferStories`; the Portal takes the first `TransferStoryLimit` = 4).
   - **Sides.** Each side resolves to a `JourneySide`:
     - `Organization`: the company or a rival, with name and crest;
     - `FreeAgent`: a status, with no ID, name or crest;
     - `Unknown`: an ID the company cannot name. It is labelled unknown, never replaced.
   - **Eligible.** A move within half a season, of a named person, with at least one side a named organization.
   - **Roundups.** Players making the same move on one day are one roundup ("4 players leave …"). A head-coach move, or a
     player's return, is always its own story.
   - **Ranking.** Head-coach stories first, then newest, then the newer record.
   - **Immutable stories.** A story is fixed by its own moves and the history before them. A later move adds a story and
     never rewrites an earlier one. The one exception: same-day moves can still join one roundup on that day.
3. **Presentation** (`PortalScreen.Transfers.cs`). See §6.

## 5. Movement scenarios

| Scenario | Journey shown | Headline / summary | Status |
|---|---|---|---|
| Free agent signs | Free agent → Company | "X signs with Company" / "Role A player, signed from the free-agent market." | Supported |
| Player released | Company → Free agent | "X released by Company" / "…released before the contract ended." | Supported (new) |
| Contract ends (player) | Company → Free agent | "X leaves Company" / "…the contract ended without a renewal." | Supported |
| Contract ends (head coach) | Company → Free agent | "X leaves Company" / "Head coach; the contract ended without a renewal, and Y covers as interim." | Supported |
| Interim head coach | Free agent → Company | "Y takes interim charge of Company" | Supported (new) |
| Head coach appointed | Free agent → Company | "X named head coach of Company" | Supported |
| Head coach replaced | Company → Free agent | "X leaves Company" / "Head coach; replaced by Z." | Supported (new) |
| Return (left, then re-signed) | **Free agent** → Company | "X re-signs with Company" / "…back N days after the contract ended / being released." The earlier departure is context only and stays its own story. | Supported |
| Team to team | A → B | "X joins B" / "…moved directly from A." | Read model and UI ready; the rules have no rival rosters (DEV case only) |
| Organization no longer known | ○ Team unknown → B, or A → ○ Team unknown | "X joins B" / "…where they came from is not on record." | Read model and UI ready (DEV case only) |
| Neither side a named organization | — | not told | — |

Labels distinguish player and staff stories:
- Player signing / release / departure / transfer, and the plurals for a roundup;
- Staff appointment / departure / transfer;
- Interim appointment.

Every story is `● CONFIRMED`.

## 6. Presentation

- **One article for every position.**
  - Each story is a flat `Entity` row on the region's own surface. It lifts on hover, and stories are divided by
    engraved rules.
  - There is no lead plate and no nested panel. Only the status chip is a tag.
  - Every story has the same padding, identity column, type sizes and journey row.
  - Significance orders the feed and never enlarges a story.
- **Identity column** (44 / 52 px).
  - A person is shown with the approved generic art of their kind, by ID: player art for `person:`, staff art for
    `coach:`.
  - The art is drawn **bare**: no plate, frame or well. It is sized by its visible content
    (`SemanticArt fitContent + optical`), so the transparent margin no longer shrinks it.
  - Missing art is the neutral vector glyph alone.
  - A roundup shows its count ("4 / players").
- **Text column.**
  - Meta row: `● CONFIRMED` chip, the kind label in caps, and the age on the right.
  - Headline, 14 / 16 semibold, one line. It takes two lines only when the person's whole name needs them; a name is
    never cut. A longer headline shortens only its organization, which the journey shows whole, and the full text is in
    the tooltip.
  - Journey row, origin → arrow → destination, always in that order:
    - organization sides are a crest (20 px) and the name; the destination is in the stronger tone;
    - "Free agent" is plain words, with no crest or shield;
    - unknown is `○ Team unknown`, with the full "Previous / New team unknown" in the tooltip.
  - The sides take equal shares, so arrows line up from story to story. On a roomy Portal the spare width is shared
    with a trailing space, so the arrow stays near the names.
  - Summary: 1920×1080 only, one line.
- **Sizes.** 3 stories at 1280×720 and 4 at 1920×1080, all fitting the first viewport.
- **Tooltip.** Headline, journey and summary.
- **Click.** Opens the person in Squad or Staff; a roundup opens Squad.

## 7. Verification (2026-10-09, local, Debug)

| Suite | Result |
|---|---|
| Integration | 270 (v1: 257) |
| Domain / UI / UiKit / OperatingMap / SponsorWorkspace | 15 / 3 / 222 / 48 / 48 |
| Headless | two-cycle pass, hash `c91319e6…` |
| Shell (`Verify-Shell`, headless) | 3792 checks at 1280×720 and 3830 at 1920×1080 (v1: 3472 / 3508) |
| Native kits | UiKit 24, OperatingMap 101, SponsorWorkspace 31 (unchanged) |
| Captures (`Capture-Shell -Results 1 -Cases -Transfers`, unsandboxed) | both sizes |

**New Integration checks.**
- Moves are recorded with their direction for a signing, an appointment and the replaced coach. Contract ends are
  recorded with the cause of the notice.
- Stories tell their journey: free agent → company, company → free agent.
- History stays correct: signed (day 1) → released (a later day) → re-signed (a later day) gives three stories, each with
  its own day's journey. Every story told before a later move is still told identically after it.
- The person is still named after their listing is removed.
- A return comes from the free agent, with the departure as context, and the departure is still told.
- A → B then B → company (read model): the first story keeps A → B.
- An unknown organization stays unknown; a move with no named organization is not told.
- Saves: history is in the save, schema 4. Save → load gives identical stories, and a schema-3 save is rejected.
- Validation rejects a signing from the company itself, a move dated in the future, and a coach recorded as a player.
- History stays bounded to two seasons, and every coach contract end has an interim.

**Native checks** (`TransferNewsCheck`, on the live Portal and every case):
- the stories are the model's, in order, at most 3 / 4;
- every story carries its certainty and kind label;
- headline: one line, two only for a long name; the name is whole and the full text is in the tooltip;
- **same treatment**: every story is an `Entity`, with one headline size, one identity-column width and no nested
  plates;
- **bare identity**: a person's `AssetMark` has no plate and resolves to the art of their kind (player or staff) or the
  neutral glyph; a roundup shows its count;
- **journey**: origin, then the arrow, then the destination, left to right, without overlap and inside the story;
- each side matches the model:
  - an organization shows its crest asset ID and its name, at least 40 px wide;
  - "Free agent" shows no crest and is whole;
  - unknown shows the marker, is whole and has the full tooltip;
- no market text, filter or route; no story repeated in world news.

**Live run** (`TransferEvidence`):
1. Play to the season-1 contract ends. Check company → free agent.
2. Re-sign a departed player, sign a fresh free agent, appoint a coach (the old one is replaced) and release a player.
3. Check the return, the appointment, "Head coach; replaced by …" and the release story.
4. Check that every story told before step 2 is unchanged.
5. Save, then load: identical stories.

**DEV cases** (DEVELOPMENT / NONCANONICAL fixtures, the campaign's own people renamed "DEV …", never written to a
campaign):
- `one-story`, `roundup`, `many-transfers`, `long-names`, `missing-art`, `empty-transfers`;
- `journeys`: rival → company, unknown → company, company → unknown;
- `return`: departure, then re-sign.

**Evidence files** ([transfer-news-v2](portal-evidence/transfer-news-v2/)):
- `transfer-news-before-after-*.png`: the same live campaign before v2 (lead plate, boxed portraits, no journey,
  "3 players leave" after a return) and after;
- `portal-transfers-departures-*`, `-moves-*` and `-reloaded-*`: the live run;
- `portal-case-*`: the cases above;
- `portal-*`: day 6, the empty state;
- `squad-*` and `staff-*`: the market, unchanged.

## 8. Known limits and unsupported scenarios

- **Rival moves do not exist.**
  - Rivals have no rosters, so every live journey involves the company and the open market.
  - Team-to-team and unknown-organization journeys are verified only with DEV fixtures.
  - Rival people and roster actions (RIVAL_SIMULATION §5–6, rules v3 wave 4) would write `Move`s with rival origins or
    destinations. The read model and UI already tell them.
- **Free agents' earlier careers are unknown.** Generated free agents have no earlier career. Signing them shows "Free
  agent → Company" and never an invented former team.
- **No rumours, reports or developing stories.** A public-information mechanism is needed first. Hidden `Rival.Need`
  must never become a rumour.
- **Significance is coarse.** It is only head coach vs player, because no prominence data exists. No story is featured
  or enlarged.
- **Fees stay private terms.** They are never shown.
- **Deleted organizations.** No rule deletes an organization. If one disappears, its moves show `○ Team unknown`.
  Organization names are not snapshotted.
- **Same-day roundups.** On the day itself, a second identical move turns a single story into a roundup.
- **Bounded history.** Two seasons; older moves are not news.
- **Bounded text.** At 1280×720 a headline naming a long organization is shortened (the journey and tooltip keep it
  whole), and the summary is tooltip-only. 1920×1080 with fewer than four stories leaves space under the list.
- **Old saves.** Saves from before schema 4 do not load.
