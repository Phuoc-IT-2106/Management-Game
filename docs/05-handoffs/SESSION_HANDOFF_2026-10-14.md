# Session handoff — 2026-10-14 (rules v4 follow-ups)

Status: the 2026-10-13 handoff's UI follow-ups and two balance references are done. **Every open item, the data tooling
included, is listed below in one order.** Items marked **(decision)** need the user or the Director.

**Repository state.** Branch `stage5-sponsor-instruments`; nothing is pushed.

| Commit | What |
|---|---|
| `e917a05` | Crests in the league standings; rival players and head coaches shown under contract in Squad and Staff |
| `9a29251` | The autonomous head coach recruits within the owner's fee and wage limits (DEC-014 envelope); save schema 6 |
| `4215cc7` | The coach releases the player an upgrade displaces; a floor of 3 free agents per role at season end |
| (this commit) | Proposed DEC entries, documents, evidence, and the archived 2026-10-08 handoff |

**Not committed:** the data tooling in `tools/lol-data-ingestion/`. This covers the full-2026 PandaScore collection and
the Oracle's Elixir import and identity pipeline. Its tests pass (47). The user approved two commits for it, then
redirected the session before they were made; see item 3.

Full records:
- [RULES_V4_PLAYER_WORLD, "After rules v4"](../10-vertical-slice/RULES_V4_PLAYER_WORLD.md#after-rules-v4-2026-10-14);
- [PROPOSED_DECISIONS](../01-governance/PROPOSED_DECISIONS.md);
- [screen-evidence/rules-v4-followups](../10-vertical-slice/genre-shell/screen-evidence/rules-v4-followups/).

## What this session decided with the user

- **Order of work:** the UI follow-ups first, then the balance references.
- **A delegated coach recruits** within limits the owner sets: a fee and a daily wage per signing.
- **The coach may release** the player an upgrade displaces, when the exit cost fits the fee limit. Renewals stay with
  the owner.
- **A floor of free agents:** after the rivals recruit at season end, every role keeps at least three.
- **DEC drafts:** the entries still missing are drafted as PROPOSED, for the Director to accept.

Measured with `build/Measure-Rules.ps1 -Gate 2`. Player value moved from 37 to 11.4 points, inside its 5–15 range.
Market depth moved from 87.5% to 100%. Gates 1 and 2 still pass.

## What remains

In suggested order.

### 1. Governance (decision)

Accept, change or reject DEC-024 to DEC-030 in [PROPOSED_DECISIONS](../01-governance/PROPOSED_DECISIONS.md):
- rules v2;
- the genre-shell amendment;
- rules v3 wave 1;
- USD;
- rules v4;
- the gate 1 measure;
- coach recruitment.

On acceptance, move each entry into the decision log and update the accepted range in PROJECT_STATE.

### 2. Balance references still outside their ranges (decision)

From `artifacts/recruit-measure-r2` (not tracked):

| Reference | Value | Range | What it would take |
|---|---|---|---|
| Financial pressure | 3.1% of seasons | 20–60% | An economy decision: running costs, wages, prize and sponsor levels |
| Outcome diversity | 0.87 places | ≥ 1 | More spread in the world or in match variance; a well-run company ends near the top on most seeds |
| Decision density (gate 3) | 1.97 a week | ≥ 2 | Gate-3 mechanisms: weekly focus pairs and events with choices |
| Reputation, raw correlation (reference) | 0.478 | ≥ 0.6 | Nothing if DEC-029 is accepted (the gate reads the partial measure, 0.893) |

### 3. Data tooling (`tools/lol-data-ingestion/`, research only)

- **Commit (decision).** The user approved two commits: the full-2026 collection, then the Oracle's Elixir pipeline.
  Confirm before making them. The token scan of the 16 pending files found nothing.
- **Oracle's Elixir import (user action).** The pipeline is built and tested on synthetic data. It waits for the 2026
  CSV:
  1. Download the CSV from the official page.
  2. Save it in `data/import/oracles-elixir/`.
  3. Run `import_oracles_elixir.py`, then `reconcile_identities.py`.

  See [ORACLES_ELIXIR_2026_INTEGRATION.md](../../tools/lol-data-ingestion/reports/ORACLES_ELIXIR_2026_INTEGRATION.md).
- **Logos (decision).** They are `LICENSE_REVIEW_REQUIRED`. `cache_logos.py` fetches nothing without
  `--license-approved`.
- Do not rerun `calibrate.py`: `calibration_2026.json` is pinned by the content's SHA-256.

### 3a. Rating distributions (done 2026-10-14, not committed)

See [DATA_STANDARD_AUDIT §4–6](../10-vertical-slice/DATA_STANDARD_AUDIT.md#4-rating-and-distribution-measurement-2026-10-14).
Changes:
- the day's form in series (real score mix);
- continuous ability;
- distinct skills;
- a weaker open market;
- bell-shaped mind with real effects;
- coaches who differ, with wages that follow quality.

Real players: proposed DEC-031, a locally built pack, blocked on the Oracle's Elixir CSV.

### 3b. Data standard audit

[DATA_STANDARD_AUDIT.md](../10-vertical-slice/DATA_STANDARD_AUDIT.md) lists what the model still needs before more content
is built on it. It covers players, clubs, staff and rivals:
- **P0, mechanical:**
  - unique handles (4 duplicates in a 4-season save);
  - a released player's market price;
  - load-time invariants;
  - golden formula tests;
  - a data dictionary.
- **P1, conventions:**
  - one scale per attribute family;
  - one rounding rule;
  - coefficients into content;
  - coach and company identity fields.
- **P2, decisions:** the money scale and wage curve, rival condition and economy, staff roles, save compatibility.

### 4. Known limits

- **Rivals strengthen over seasons.** Mean rival strength is about 74 at the start and about 77 later. It is bounded,
  but it does not revert to the mean.
- **"Retirement" at 25–27.** The calibration reads exits from the observed tiers.
- **UI:**
  - the Matches tables of the League view have no crests (the mockup has them);
  - the full rival profile still opens only from a standings row;
  - Calendar art is unused;
  - the coach's signings are told in Transfer News like any signing, with no message saying the coach made them.

### 5. Not built yet (later waves)

- board objectives and confidence;
- facilities;
- weekly focus pairs;
- events with choices;
- multi-term negotiation;
- a best-of match-day screen;
- **(decision)** promotion and relegation.

## How to verify

| What | Command | Last result (2026-10-14, Debug) |
|---|---|---|
| Build, suites, headless | `build/Verify.ps1 -Configuration Debug -Evidence artifacts/<dir>` | Domain 15, Integration 369, UI 3, UiKit 222, OperatingMap 48, SponsorWorkspace 48; headless `a789c421…` after the distribution changes (`86cd640e…` before) |
| Shell (headless) | `build/Verify-Shell.ps1` | 3861 / 3901 |
| Native kits | `build/Verify-UiKit.ps1`, `Verify-OperatingMap.ps1`, `Verify-SponsorWorkspace.ps1` | 24, 101, 31 |
| Rules gates | `build/Measure-Rules.ps1 -Gate 2 -Seeds 16 -Output artifacts/<dir>` | `MEASURE_GATES_PASS 1..2` with 16 seeds (about 20 minutes); with the default 8 seeds, multi-year progress is noisy (0.75) |
| Data tooling | `tools/lol-data-ingestion/.venv/Scripts/python.exe -m unittest discover -s tests` | 47 |
| Captures (interactive desktop, unsandboxed) | Godot in the foreground with the arguments of `build/Capture-Shell.ps1` | both sizes pass; Start-Process cuts runs short here |

## Where the code lives

As in the [2026-10-13 handoff](SESSION_HANDOFF_2026-10-13.md#where-the-code-lives), plus:

| Concern | Files |
|---|---|
| Coach recruitment | `src/ManagementGame.Application/CoachRecruiting.cs`; `Session.Submit` runs it on advance; `Authority` in `Model.cs` |
| Free-agent floor | `Careers.Floor` in `Lifecycle.cs`, called from `Simulation.Rollover`; `Lifecycle.PoolFloor` in `content/fixture.json` |
| Crests in tables, under-contract views | `ShellScreens.cs` (`Table(crest:)`, Squad market, Staff coach market); `GameShell.OpenTarget` |
| Save schema 6 | `SnapshotStore.Schema`, `AuthorityDto`, `SnapshotValidation` |
