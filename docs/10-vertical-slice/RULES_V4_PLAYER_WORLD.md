# Rules v4 — the player world: roles, attributes, league, lifecycle and rivals

Status: **IN PROGRESS, built phase by phase** (started 2026-10-09). This spec is kept current as each phase lands.

**Authority.**
- The user's plan (2026-10-09) implements the [gameplay data review](GAMEPLAY_DATA_REVIEW.md). It was approved with four
  decisions:
  - five LoL-style roles;
  - real data for aggregate calibration only, with the API crawl permitted;
  - rules v4 with old saves rejected;
  - build all phases in one run.
- Like rules v2 and v3 wave 1, this stands on explicit user instruction. A Director DEC entry should confirm it.

Identity: rules `slice-rules-v4`, content schema 4, save schema 5. Saves of schema 4 or older are rejected; no
migration.

## Phases

| Phase | Scope | State |
|---|---|---|
| 0 | Safety snapshot commit, baseline | Done (`9b27842`) |
| 1 | Real-data tooling and the calibration report | Done (`101864a`) |
| 2 | Roles and the player model | Done (`3917e5c`) |
| 3 | Logistic series resolver, familiarity, an 8-team league, playoffs | Done |
| 4 | Lifecycle: development, retirement, youth intake, form, morale, popularity, conduct | — |
| 5 | Rivals with real rosters, budgets and posture AI | — |
| 6 | Measurement and balance (gates 1 and 2) | — |
| 7 | Documents and handoff | — |

## Phase 1 — real data and calibration

**Tooling** (`tools/lol-data-ingestion/`, unit tests with `python -m unittest discover -s tests`):
- `sync_pandascore.py --preset core-2026`: a bounded crawl.
  - Each query is filtered to a fixed 2026 window and sorted ascending.
  - It pages to `X-Total` (continuing past a short page) and records the totals and completeness in the manifest.
  - It stops at a quota floor of 50 remaining.
- `snapshots.py`: reads completed raw runs by collection, deduplicates by id and records provenance.
- `build_rosters.py`: dated `tournament_roster` rows rebuilt from the immutable snapshots, never from the UPSERT table.
- `calibrate.py`: `reports/calibration_2026.json` and `.md`, **aggregate statistics only**.
  - No person names, handles, birthdays, person ids or team names; a test enforces this.
- `apply_calibration.py`: writes the calibrated tables into `content/fixture.json` and refreshes `fixture.sha256`.
- `handle_check.py`: compares every generated handle with every real handle in memory and fails on a collision.

**Crawl (2026-10-09):** 49 requests, every query complete against `X-Total`:
- 136 leagues;
- 105 series;
- 277 + 278 tournaments (start and end windows);
- 3,829 matches.

**What the report says:**

| Measure | Value | Used for |
|---|---|---|
| Elo spread inside a league (top minus bottom), tier A median | 351 points; the top team beats the bottom ≈ 88% per series | Logistic scale (phase 3) |
| Series format, tier A | Regular season Bo3 82%; playoffs Bo5 70% | Regular Bo3, playoffs Bo5 (phase 3) |
| Series scores | Bo3 2–0 64%; Bo5 3–0 46%, 3–1 29%, 3–2 25% | Resolver check (phase 6) |
| Matches per team per week, tier A | ≈ 1.7 | Schedule density (phase 3) |
| Roster turnover between splits, tier A | mean 12.6% of the roster | Rival transfer activity (phase 5) |
| Roster size, tier A | 5: 76%, 6: 19%, 7: 3%, 8+: 1% | Roster bounds |
| Age, all roles | 17–31, peak 22–23; median about 23.5 (support 24.7) | `Generation.Ages` (applied) |
| Nationality, region ASIA | KR 31%, CN 19%, VN 17%, JP 12%, TW 4%, other | `Nationalities[].Weight` (applied) |

Ages come from birthdays embedded in roster objects. Their licence is unconfirmed, so the report marks them
`licence_unconfirmed` and uses them in aggregate only.

## Phase 2 — roles and the player model

**Roles.** Lineup order is `top, jungle, mid, bot, support` (`Roles.All`, a rule rather than content). A lineup lists
one player per role in that order. A player can play their primary role at 100% and an optional secondary role at
80%.

**Person** (Domain):

| Group | Fields | Who sees them |
|---|---|---|
| Identity | Name (legal), **Handle** (shown in play), Nationality, BornSeason (age is the current season minus BornSeason; one season is one competitive year) | Public |
| Role | Role, SecondaryRole | Public |
| Skills, 1–99 | Mechanics, EarlyGame, Teamfight, Macro, ChampionPool | Exact for own players; others as a rating band |
| Mind, 1–99 | Consistency, Composure, Communication | Exact for own players |
| Personality, 1–99 | Professionalism, Ambition, Loyalty | **Never shown** |
| Career | Potential (the hidden ceiling of the rating) | A band only |
| Condition | Readiness 1–100, Form −10..10, Morale 0–100 | Own players |
| Commercial | Popularity 0–100 | Public |

The old Execution and Adaptability are gone:
- the role rating replaces Execution;
- ChampionPool takes Adaptability's place in the adaptation term.

Consistency moved into Mind. Coaches gain `Development`.

**Formulas** (`Ratings.cs`, `Pay`):
- **Rating in role.** `RoleRating = Σ weight[role][skill] × skill / 10,000 × proficiency / 100`, round-even. The weights
  come from content:

  | Role | Mechanics | Early | Teamfight | Macro | Pool |
  |---|---|---|---|---|---|
  | Top | 25 | 25 | 25 | 10 | 15 |
  | Jungle | 15 | 25 | 20 | 30 | 10 |
  | Mid | 30 | 20 | 20 | 15 | 15 |
  | Bot | 30 | 20 | 30 | 5 | 15 |
  | Support | 10 | 20 | 25 | 35 | 10 |

- **Wage.** `rating² / 5`, then ×(100 + (Popularity − 50)/2)%, ×(100 + (Ambition − 50)/5)%, and up to age 21
  ×(100 + headroom)%.
  - Fee is 20 × wage and release cost 3 × wage, as before.
  - Renewal demand also falls with Loyalty.
- **Coach wage.** `(Preparation + Analysis + Adaptability + Development) × 4 + Capacity × 2`.

**Generation** (`Generator.cs`; content `Generation`, `RoleWeights`, `Nationalities`, `Handles`):
- **Drawing a player:**
  - age from the calibrated table;
  - an ability band (60 / 70 / 78 / 88 at 25 / 50 / 23 / 2%), shifted by age;
  - an archetype of the role (three per role, such as lane bully, shotcaller or vision caller): skill offsets plus a
    communication offset;
  - keyed jitter ±4.
- **One distribution for everyone.** The starting roster, the market and future rivals share it. Measured over 8 seeds,
  the roster mean rating is 71.4 and the market mean is 70.1, which fixes the old unexplained market advantage.
- **Potential.** The rating plus the age's headroom: +8..22 at 17, falling to 0..2 by 27.
- **Popularity.** Follows the rating and prime age (22–27).
- **Names.** From per-nationality pools written for the fixture. Family-first order for KR, CN, VN and TW.
- **Handles.** Prefix plus suffix (525 combinations); `handle_check.py` found no collision with 2,314 real handles.
- **Deterministic.** Every draw is keyed by entity id and purpose, and tables are sorted before drawing, so content
  order never changes the world (tested by shuffling every pool).

**What the company observes:**
- Its own players exactly: rating, skills, mind, condition and popularity.
- Free agents as bands, with a stable keyed centre and half-width `max(3, (100 − Information)/5 + (100 − coach
  Analysis)/10)`. Potential's band is 1.5 times wider. Each band always contains the true value.
- Coach Analysis therefore has a consumer now (scouting).

**UI** (approved reference `03_squad_mockup`):
- **Squad table:** Player (handle), Role, Rating, Age, Readiness, Wage/day, Contract ends.
- **Free agents:** Player, Role, Age, Rating (est.), Potential (est.) on roomy screens only, Fee, Wage/day.
- **Profile:** handle, role, secondary role, age, nationality and legal name; a large rating; eight attribute bars;
  status (readiness, form, morale, popularity); contract (wage, end, market value); renew and release.
- **Competition lineup:** one choice per role, secondary-role players marked "(secondary)".
- **Text:** Portal decisions and notices name the roles ("No player for Jungle"); Transfer News uses role nouns ("Top
  laner", "Jungler").

**Checks (Phase 2):**
- **Integration 284**, newly covering:
  - the age table and its mean;
  - bounds, potential ≥ rating, secondary ≠ primary;
  - archetype variety and secondary-role share;
  - young players trading rating for headroom;
  - the nationality mix;
  - the market and roster sharing one mean;
  - the rating formula and proficiency;
  - wages rising with rating and popularity;
  - role-ordered lineups, with a wrong-role lineup rejected and secondary-role cover;
  - personality and exact potential never observed;
  - bands containing the truth and narrowing with information.
- **Shell native:** 3792 / 3830 checks.
- **Headless hash:** `4f9896b5…`.

## Phase 3 — matches, the league and its playoffs

### Match resolution

Code: `Competition.cs` and `Logistic.cs`.

- **Series.** A match is a best-of-n series: best of 3 in the regular season and best of 5 in the playoffs, as the
  calibration report found for tier A.
- **One game.** The strength difference is a fixed part plus that game's keyed variance. The game win probability is
  the logistic of the difference over `LogitScale`.
  - The logistic comes from an integer table in millionths (logits 0.00 to 8.00 in steps of 0.01, symmetric), so no
    floating point runs during play.
  - Variance and result use the keys `variance:<game>` and `result:<game>`.
- **The fixed part:**
  - base: role rating × readiness × (100 + Form)%;
  - preparation: completed work over `PreparationScale`;
  - matchup: +3 when the posture suits the meta, −1 otherwise;
  - adaptation: champion pools and the coach's Adaptability, minus the rival's read of a repeated posture;
  - familiarity: 0 to +4;
  - leadership: when the best Communication in the lineup is below 60, up to −3;
  - style: an early-game lineup in an aggressive meta, or a teamfight lineup in a conservative one, ±3;
  - composure: in matches of importance 2 or more, ±2.
- **Spread.** `Variance` + 4 × posture − (mean Consistency − 50) / 10. Consistency narrows the outcome; an aggressive
  posture widens it.
- **Recorded outcome.** Games won and lost. `Probability` is the exact series win probability before variance
  (`Logistic.Series`: integer arithmetic, rounded half to even once).
- **Rival against rival.** The same game model, from the rivals' compressed `Strength`.
- **Scale.** `LogitScale` 900 means 9 rating points per logit. Across the rival range of 14 points, the top team wins
  a best of 3 about 91% of the time; the report found 88% for the top against the bottom of a tier-A league.

### Familiarity and fatigue

- **Familiarity** is stored per contract (`Employment.Familiarity`, 0–100):
  - the starting roster begins at `StartingFamiliarity` (60), and a new signing at 0;
  - it grows by `FamiliarityPerDay` (1) each day and by `FamiliarityPerMatch` (4) for each match played;
  - the lineup's mean, divided by 25, enters the game difference.
- It replaces the old readiness cap on signing. The plan proposed familiarity per pair of people; one value per
  contract keeps the save small and says the same thing in the game.
- **Match fatigue.** Each match costs the lineup `MatchFatigue` (4) readiness; it was a fixed 8.

### The league

Code: `Seasons.cs`.

- **Format.** Eight teams: the company and `RivalCount` (7) rivals.
  - `SeasonLength` is 84. The first round is on day 6 and a round follows every 4 days.
  - The regular season is a seeded circle-method double round robin of 14 rounds. Each round the company plays one
    rival and the other six play three matches; the return leg swaps sides.
  - The second half carries double stakes.
- **Storage.**
  - `World.Fixtures` keeps its meaning: the company's matches.
  - `World.LeagueFixtures` and `World.LeagueResults` hold the rest of the league. League results leave at season end.
  - `World.Finishes` keeps the final order of the last ten seasons.
- **Table.** Derived from results and never stored (DEC-012). One point per series won. Ties go to map difference,
  then head-to-head among the teams still level, then maps won, then a seeded draw.
- **Playoffs.** The top four play semifinals: 1 against 4 and 2 against 3.
  - Day 58 is the last regular day; the semifinals are on day 62 and the final on day 66. Both are best of 5, at
    importance 3.
  - Playoff fixtures are derived from results and added when a day closes; validation requires exactly these.
  - Days 67–84 are the off-season. Contracts end at the season's end.
- **Final order.** Champion and runner-up first, then the other semifinalists by table place, then the rest of the
  table.
- **Place prizes** (`PlacePrizes`): 1st $1,500, 2nd $800, 3rd and 4th $400, due the day after the season ends. The
  match prize is unchanged; phase 6 balances the two.
- **League notices.** The end of the regular season (the company's place and semifinal opponent), the final pairing,
  and the champion. Their inbox kind is Season, so they open Competition.

### Forced restructuring (DEC-016)

This removes the insolvency dead end found in rules v3 wave 1.

- **Stuck** means: distress, no cash, obligations overdue from earlier days, and no incoming payment within three days.
- **Once a day,** in the Finance phase before settlement, the board sells a contract to the open market for its market
  fee (20 × wage):
  - the most valuable contract whose departure still leaves a legal lineup, or failing that the most valuable one;
  - the proceeds pay obligations that same day;
  - recovery moves to Restructuring, a Released move is recorded and a Finance notice says what it cost.
- **Time stops** only when nothing is left to sell.
- **Bug fixed.** The first version ran the sale at the start of every phase. It could sell twice in one day under one
  settlement ID, and snapshot validation then rejected the state ("Duplicate/invalid settlement IDs").
- **Still open (phase 6).** A company that sells everyone still stops: 2 of 32 thrift runs. The board's next lever is
  undecided.

### Read model and UI

- **Situation** gains:
  - `Standings` (`StandingRow`);
  - `LeagueMatches` (`LeagueMatchRow`);
  - `LeagueNews` (`LeagueNewsRow`);
  - `League` (`LeagueInfo`);
  - `LastSeasonPlace`.
- **`FixtureRow`** gains `Stage`, `Games` and `Score`.
- **League news** is derived from results, never stored:
  - a new leader at the top of the table after a round day;
  - an upset, when a rival beats one at least three places above it.
  It joins the Portal's world news by day.
- **Horizon** gains the end of the regular season and the playoff matches of other teams.
- **Competition** (references `04_competition` and `05_league`) has a Match | League segment track, so no rail item is
  added:
  - **Match** keeps the workspace. Fixtures show the stage and the score ("Victory 2–1").
  - **League** shows the standings (#, Team, W, L, Map diff, Pts; the company's row selected), this week's and next
    week's matches, the playoffs once they exist, and the league's format, key days and place prizes.
- **Portal season record** shows the league place ("5th of 8"), and on roomy screens the playoff line ("Top 4").
- **Not yet:** crests in table rows. The mockup shows them, but tables have no identity marks yet.

### Provisional balance

Phase 6 tunes these values:
- `PreparationScale` 30 → 70. Under the steeper logistic, preparation added about 15 points and every policy won
  90–96% of its matches. Now preparation adds about 6.6 points.
- Rival `Strength` 64–78 → 68–82.

Measurement: 8 seeds × 5 seasons, saved in `artifacts/v4-phase3-measure` (not tracked).

| Policy | Win rate | Cash, season 5 | Insolvent runs |
|---|---|---|---|
| Coach | 68.8% | $20,527 | 0 of 8 |
| Sponsor-greedy | 67.6% | $14,900 | 0 of 8 |
| Thrift | 65.0% | $6,464 | 2 of 8 |
| Balanced | 81.2% | $26,471 | 0 of 8 |

- **Gate 1, losing is not profitable:** pass, 8/8 seeds.
- **Gate 1, reputation follows results:** **fails**, correlation 0.422 against the 0.6 threshold (rules v3 wave 1:
  0.807). The season's reputation target still follows the win rate only; phase 6 adds the league place.
- **Gate 2, empty stretches:** 17 days in the worst season, against a limit of 10.
- **Gate 2, multi-year progress:** measurable now that a table exists; phase 6 adds it to `Measurement.cs`.

### Checks (Phase 3)

- **Integration 308** (24 new), covering:
  - the logistic table against the logistic function, within one millionth;
  - exact series probabilities, and series that stop at the majority;
  - even sides splitting wins and sweeps; a 14-point favourite winning most best-of-threes;
  - the double round robin and a schedule that is a pure function of seed and season;
  - every tie-break: map difference, head-to-head both ways, maps won;
  - a season played through:
    - semifinals from the table, and the observed standings;
    - the regular-season notice opening Competition;
    - the leader story and the order of world news;
    - a save in the middle of the playoffs;
    - the final order, the recorded finish and the place prize, and last season's place;
  - familiarity;
  - forced restructuring: one sale a day, and time stopping with nothing left to sell.
- **Career test.** Its owner never renews, and now keeps the league's registered minimum of six. Without that, the
  longer season's fines bankrupted it.
- **Shell native:** 3832 checks at 1280×720 and 3871 at 1920×1080 (phase 2: 3792 / 3830). The League view is checked
  at both sizes.
- **Native kits:** 24 / 101 / 31.
- **Headless hash:** `d760e35c…`.
- **Content:** the fixture's calibration pin now matches the committed report (the report is stored with LF).
