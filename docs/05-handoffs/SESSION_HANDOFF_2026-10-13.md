# Session handoff — 2026-10-13 (rules v4 done)

Status: **rules v4 is done** (phases 0–7). Gates 1 and 2 pass. The items below that are marked **(decision)** need the
user or the Director.

**Repository state.** Branch `stage5-sponsor-instruments`; nothing is pushed. This session's commits:

| Commit | What |
|---|---|
| `21dd48e` | Repository cleanup: superseded Portal captures and rejected UI drafts deleted, closed-phase documents archived in `docs/90-archive` |
| `46c0839` | Rules v4 phase 3: logistic best-of series, an eight-team league with playoffs, forced restructuring once a day |
| `7cdcd31` | Phase 4: careers (calibrated retirement and youth intake, a persistent free-agent pool, form, morale, popularity, conduct) |
| `a4a5a6b` | Phase 5: rival organizations with real squads, funds, coaches and recruitment |
| `0a7864c` | Phase 6, rounds 1–3: the league measured, balance rounds, stopped at the plan's stop point |
| `7acb62f` | Phase 6, round 4: the user's decisions (underdog start, a base reputation gain for wins) |
| `222d15a` | Phase 6, round 5: quality-scaled fees, the reputation gate's new measure; gates 1 and 2 pass |
| (this commit) | Phase 7: documents, evidence and this handoff |

Full record: [RULES_V4_PLAYER_WORLD.md](../10-vertical-slice/RULES_V4_PLAYER_WORLD.md). Evidence:
[screen-evidence/rules-v4](../10-vertical-slice/genre-shell/screen-evidence/rules-v4/).

## What this session decided with the user

- **Cleanup:** delete superseded images and drafts, and archive the documents still cited as provenance (instead of
  deleting them).
- **Data before NPCs:** finish the supporting work before any new crawl or NPC generation. The existing 2026 snapshots
  were enough: the careers aggregates, the retirement table and the intake ages were computed offline, and no API call
  was made.
- **Balance (phase 6):**
  - the company starts as an underdog (`Generation.StartingShift` −4);
  - every win earns a base reputation gain (`ReputationWinBase` 1 × importance);
  - transfer fees rise with quality (20 days' wage up to rating 70, 3 more per point, at most 60);
  - gate 1 "reputation follows results" reads the partial correlation given the starting reputation. It was 0.91; the
    raw figure was 0.54, diluted because reputation converges on what a season earns.

## What remains

In suggested order.

### 1. Governance (decision)

- **DEC entries** for rules v2, rules v3 wave 1 and rules v4, the USD relabel and the genre-shell amendment. Each stands
  on explicit user instruction.
- **The changed gate 1 measure.** The Director should confirm it.

### 2. Balance references still outside their ranges

From the last measurement: `artifacts/v4-measure` (not tracked), or rules v4, phase 6, round 5.

- **Player value: 37 points** (target 5–15). Full coach delegation never recruits, so it falls far behind an owner who
  does. **(decision)** Should a delegated coach also recruit, within limits the owner sets?
- **Outcome diversity: 0.87** (≥ 1). A well-run company ends near the top on most seeds.
- **Market depth: 87.5%** (≥ 90%): some season ends leave a role with fewer than three free agents.
- **Financial pressure: 3%** (20–60%): the economy is easy for anyone but the thrift policy.
- **Decision density: 1.93** (≥ 2). This is a gate-3 measure. Decision density had been overstated since phase 3,
  because it divided by a 56-day season; that is fixed.

### 3. Known limits

- **Rivals strengthen over seasons** through recruitment: mean rival strength about 74 at the start and 77–80 later.
  It is bounded (1 upgrade a season, funds by place) but not mean-reverting.
- **Insolvency.** A company that sells every player still stops. The measurement had 0 of 32 such runs in round 5.
- **"Retirement" at 25–27.** The calibration reads exit from the observed rosters, which includes dropping below the
  observed tiers. A 25-year-old "retires", which reads a little early.
- **UI:**
  - no crests in the league table rows (the mockup has them);
  - rival players are not shown as "under contract" in Squad or Staff;
  - the rival profile opens by clicking a standings row only;
  - Calendar art is still unused.

### 4. Not built yet (later waves)

These are from the rules v3 proposal but not in v4:
- board objectives and confidence;
- facilities;
- weekly focus pairs;
- events with choices;
- multi-term negotiation;
- a best-of match-day screen;
- **(decision)** promotion and relegation.

## How to verify

| What | Command | Last result (2026-10-13, Debug) |
|---|---|---|
| Build, suites, headless | `build/Verify.ps1 -Configuration Debug -Evidence artifacts/<dir>` | Domain 15, Integration 349, UI 3, UiKit 222, OperatingMap 48, SponsorWorkspace 48; headless `7cfbf499…` |
| Shell (headless) | `build/Verify-Shell.ps1` | 3842 / 3882 |
| Native kits | `build/Verify-UiKit.ps1`, `Verify-OperatingMap.ps1`, `Verify-SponsorWorkspace.ps1` | 24, 101, 31 |
| Rules gates | `build/Measure-Rules.ps1 -Gate 2 -Output artifacts/<dir>` | `MEASURE_GATES_PASS 1..2`, about 10 minutes |
| Calibration tool | `tools/lol-data-ingestion/.venv/Scripts/python.exe -m unittest discover -s tests` | 17 |
| Captures (an interactive desktop, unsandboxed) | `build/Capture-Shell.ps1 -Results 8 -Transfers -Output artifacts/<dir>` | both sizes pass. In this environment Start-Process cut runs short, so captures ran Godot in the foreground with the same arguments |

## Where the code lives

| Concern | Files |
|---|---|
| Logistic table and exact series | `src/ManagementGame.Domain/Logistic.cs` |
| Match resolution | `Competition.cs` (`Resolve`, `Play`, `Rivals`) |
| League, table, playoffs | `Seasons.cs` (`Schedule`, `Table`, `Playoffs`, `FinalOrder`) |
| Careers | `Lifecycle.cs` (`Careers`) |
| Rival organizations | `Rivals.cs` (`Create`, `Lineup`, `Strength`, `Departures`, `Recruit`) |
| Season end order, forced restructuring | `Simulation.cs` (`Rollover`, `ForcedSale`, `Stuck`) |
| Fees and wages | `Ratings.cs` (`Pay.Player`, `Pay.Fee`) |
| Read model | `Observations.cs` (standings, league news, rival profiles), `PortalView.cs` (world news, transfer stories) |
| UI | `ShellScreens.cs` (Competition Match and League views), `PortalScreen.*.cs` |
| Content | `content/fixture.json` (`Lifecycle`, `Rivals`, the fee and start fields), `ContentLoader.cs` |
| Measurement | `tools/Headless/Measurement.cs` |
| Calibration | `tools/lol-data-ingestion/calibrate.py` (`careers`), `apply_calibration.py` (retirement table, intake ages) |
