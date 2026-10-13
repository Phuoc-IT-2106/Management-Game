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
| 3 | Logistic series resolver, familiarity, an 8-team league, playoffs | Done (`46c0839`) |
| 4 | Lifecycle: development, retirement, youth intake, form, morale, popularity, conduct | Done (`7cdcd31`) |
| 5 | Rivals with real rosters, budgets and posture AI | Done (`a4a5a6b`) |
| 6 | Measurement and balance (gates 1 and 2) | Stopped after 3 rounds: gate 1 reputation and gate 2 progress open (decision) |
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

## Phase 4 — careers: development, retirement, youth and condition

### Calibration first

The user asked (2026-10-12) for the supporting data to be complete before any new crawl or NPC generation. The 2026
snapshots already crawled were enough, so no API call was made. Everything below was computed offline.

- **`calibrate.py`** gains a `careers` section. It is aggregate only, like the rest of the report:
  - the share of players who leave the observed rosters after a split, by age band;
  - where players new to a team come from: another team in the data, or nowhere in the data;
  - the ages of those new to the observed rosters.
- **`apply_calibration.py`** writes two lifecycle tables into the content:
  - **`RetirementByAge`**: the age histogram read as a stationary life table, 1 − (players a year older) / (players).
    It is pooled in three-year bands, never falls with age, is zero before 25 and certain from 34;
  - **`IntakeAges`**: the histogram's growth from one age to the next, for ages 17 to 21.
- **Tool tests:** 17, one of them new. The report keeps its other sections unchanged.

| Careers measure (2026, one observed year) | Value |
|---|---|
| Leave the observed rosters after a split, ages ≤ 27 | 8.9–10.4% |
| The same, ages 28 and over | 16.0% |
| Players new to a team who come from another team | 53.9% |
| Players new to a team who are new to the observed rosters | 46.1% (median age 24.1) |
| Retirement table (basis points per season) | 25–27: 2,766; 28–30: 4,712; 31–33: 5,000; 34: 10,000 |
| Intake ages (weights) | 17: 9, 18: 23, 19: 33, 20: 56, 21: 18 |

"Leaving the observed rosters" also counts players who drop below the observed tiers. The game has no lower tier, so it
reads the exit as retirement; the rating scale (below) keeps strong players on longer.

### Development

Code: `Lifecycle.cs`, class `Careers`.

- **When.** Once a season, at season end, for the company's players and for free agents. Rivals join in phase 5.
- **The draw.** A keyed draw per person and season in the age's band (content `Development`):
  - up to 19: +2 to +6;
  - 20–24: 0 to +3;
  - 25–27: −1 to +1;
  - 28 and over: −4 to −1.
- **Scaling.** Growth moves by up to ±25 percentage points each with Professionalism, the head coach's Development and
  playing time (the share of the season's matches played; `Employment.Played` counts them). Free agents grow without a
  coach or matches. Professionalism also slows decline.
- **Applying it.** Every skill moves by the change, so the role rating moves by about as much. Growth never takes the
  primary-role rating above Potential.
- **Notice.** One Development notice lists each changed rating ("Slyvka +2 to 75"). Its inbox kind is Development, in the
  new Squad filter.

### Retirement

- **Chance.** The calibrated table by age, scaled 4% per rating point below (more likely) or above (less likely) a pivot
  of 70, from a quarter to three times. At 34 retirement is certain.
- **A company player** can only retire when the contract ends, not in the middle of it. The move is `Retired` from the
  company, with a notice ("retired at 27 when the contract ended"). Transfer News tells it ("Orinex retires", the
  company → Retired).
- **Free agents** may retire at season end. The move is `Retired` from the open market; it is not news.

### The free-agent pool

- **It persists.** Free agents no longer expire every 28 days; they stay until signed or retired. `Candidate.Deadline`
  is gone.
- **Size.** The campaign opens the pool with `PoolSize` (20) players, one role after another.
- **Each season end:**
  - free agents develop and age, and some retire;
  - `YouthIntake` (5) players arrive at the calibrated ages, one role after another;
  - above `PoolCap` (30), the players with the least potential retire, the oldest first;
  - every listing's terms follow its player.
- **Emergency only.** A player is generated during a season only for a role that neither the roster nor the market can
  fill. The company is told why ("No Jungle was on the market…"), as a Talent notice.

### Condition during the season

- **Form.** After a match, each player in the lineup moves `FormStep` (1) plus up to `FormSurprise` (4) more, the more
  unexpected the result.
- **Morale.** A win +4, a loss −3, a match on the bench −2.
  - Low morale (below 35) halves readiness recovery.
  - A low-morale player with Ambition of 65 or more will not renew. The profile says so, and the command is refused.
- **Settling.** Every 7 days form steps back toward steady and morale settles 2 toward 60.
- **Popularity.** A win adds 1 × importance (2 in the second half, 3 in the playoffs); a match on the bench costs 1.
  It reaches the company in two ways:
  - a match's audience moves ×(1 + (the lineup's mean popularity − 50) / 200);
  - each player at popularity 80 or more adds 5% to what sponsors consider fair, for at most two stars.
- **Conduct.** Every 28 days each player may break team rules: 40 basis points per point of Professionalism below 40.
  An incident costs 2 reputation, 8 popularity and 10 morale, and produces a Conduct notice.
- **Head coach.** Adaptability now multiplies the meta part of preparation (±50%). Analysis now also narrows the
  opponent's strength estimate.

### Read model and UI

- **`PersonRow`** gains the head coach's estimate of Potential (a band), the trend age suggests (Improving, At peak or
  Declining) and a renewal note. The squad profile shows "Potential 72–76 (estimate) · at peak at 27".
- **`CandidateRow`** loses its deadline. The profile no longer says "free agent until day …".
- **New inbox kinds:** Development, Conduct and Talent, gathered in a new Squad filter. They open Squad. Development is
  personnel news, so it is not world news.
- **Transfer News** has a Retired destination (`Standing.Retired`, shown as "Retired").

### Fixes found on the way

- **Lineup validation** rejected a valid lineup that covers a role with a secondary-role player. It now checks each slot
  against the role it covers.
- **Notice subjects.** Retirement, Conduct and Talent notices name their person.
- **Recommendation text.** It still said "cover every role A–E"; it now says "cover all five roles".

### Measurement

8 seeds × 5 seasons, saved in `artifacts/v4-phase4-measure` (not tracked).

| Policy | Win rate | Cash, season 5 | Insolvent runs |
|---|---|---|---|
| Coach | 72.8% | $21,726 | 0 of 8 |
| Sponsor-greedy | 76.9% | $18,295 | 1 of 8 |
| Thrift | 46.5% | $8,518 | 2 of 8 |
| Balanced | 82.9% | $24,429 | 0 of 8 |

- **Player value** is now visible: development and retirement widen the gap between owners who keep good players and
  those who do not. Balanced beats thrift by 36 points of win rate (phase 3: 16 points).
- **Gate 1, losing is not profitable:** pass, 8/8 seeds.
- **Gate 1, reputation follows results:** 0.51 (phase 3: 0.42). It still fails against the 0.6 threshold; phase 6.
- **Insolvency:** 3 of 32 runs stop after selling everyone (phase 3: 2; the rules v3 baseline: 1). The plan's condition
  for a stronger forced restructuring is met; phase 6 takes it.
- **Gate 2, empty stretches:** 17 days in the worst season (≤ 10); phase 6.

### Checks (Phase 4)

- **Integration 335** (27 new), covering:
  - development with age, the potential ceiling, and the three growth factors; professionalism slowing decline;
  - the retirement table: none before 25, certain at 34, rising with age, likelier for weaker players;
  - the public trend;
  - the opening pool and the season-end market: intake ages and roles, retirements from the market, the cap, listings;
  - playing time restarting each season;
  - retirement at contract end, its story and its notice;
  - the pool lasting through a season, and an emergency player for an unfillable role;
  - form, morale and popularity after a match, and settling afterwards;
  - renewal refusal and slower recovery at low morale;
  - conduct and professionalism;
  - the star bonus, the audience and popularity, and the head coach's adaptability.
- **Long run.** A sound owner plays eight seasons:
  - the pool stays between one player per role and its cap (24, 26, 27, then 30 from season 4);
  - 30 players retire;
  - the population's mean age stays at 23.3–24.3, against 23.5 in the calibrated mix.
- **Shell native:** 3832 / 3871. **Native kits:** 24 / 101 / 31.
- **Headless hash:** `4ba9e86a…`.

## Phase 5 — rival organizations with squads, budgets and recruitment

### Data check before generation

No new crawl was needed. The 2026 report already gives what rival generation calibrates against:
- the league's strength spread: tier-A Elo, top against bottom about 88% per series;
- squad sizes: 5 players 76%, 6 19%, 7 3%;
- turnover between splits: 12.6% in tier A;
- where newcomers come from: 54% from another team.

There is no real data on budgets, so the values for funds and wage caps are authored.

The entity data proposal (v0.1) kept rivals compressed, with no hidden squads. The approved rules v4 plan replaces that
with real squads.

### The rival model

Code: `Model.cs` (`Rival`, `RivalContract`, `Strategy`, content `RivalRules`) and `Rivals.cs`.

- **A rival organization** has:
  - players and their contracts (`RivalContract`: wage and last day);
  - a real head coach, whose Analysis and Adaptability are the rival's;
  - the season's transfer funds and a reputation;
  - the match posture it tends to play;
  - a strategy (Contend, Balanced, Rebuild or Conserve);
  - tendencies: Aggression (how readily it buys) and YouthFocus;
  - its run of poor seasons.
- **Strength is derived, not stored.** It is the mean rating in role of the rival's best lineup (an exact search over
  primary and secondary roles), plus `Edge` (4), which stands for its own preparation. The old `Strength`, `Need` and
  `Budget` drift is gone. Company matches and rival-against-rival matches both read it.
- **Ownership.** Every person has exactly one owner: the company, a rival or the open market. Snapshot validation checks
  this across players and coaches.

### The opening league

- **Target strength.** Each rival draws a target from `Generation.Rival` (68–82, the phase 3 calibration).
- **The squad** has 6–8 players by `RosterSizes`, one per role and the rest keyed. It comes from the shared player
  distribution, shifted so the lineup lands near the target less Edge. A squad that misses by more than 2 points is
  redrawn, at most twice, and the closest draw stands.
- **The rest:** contracts run one or two seasons; reputation follows the target; the strongest contend and the weakest
  rebuild; posture and tendencies are keyed.

### Season end

Order: the company's development → the open market → rival departures → rival recruitment.

1. **Departures** (`Rivals.Departures`):
   - every rival player develops, with the head coach and playing time (starters as regulars);
   - a player whose contract ends retires, is kept or joins the market. A player is kept as a starter, as someone close
     to the weakest starter, or as a young player with room to grow, within the wage cap; a rebuilding rival lets players
     of 28 and over go;
   - reputation moves with the final place (+7 for the champion, −7 for last);
   - funds reset to `FundsBase` + `FundsPerPlace` for each place above last;
   - strategy follows place, poor seasons and funds;
   - a rival placed 6th or lower for 2 seasons replaces its head coach from the coach market. The moves are a replaced
     coach and an appointment.
2. **Recruitment** (`Rivals.Recruit`), rivals in order of reputation:
   - **fill:** every role in the best lineup and the registered minimum, from the market;
   - **upgrade:** a contender or balanced rival seeks up to `MaxMoves` (2) players at least 3 points better than its
     weakest starter. `BuyPercent` (54%, scaled by aggression) of the time it buys from another rival that can spare
     the player, for a fee of 20 days' wage paid to the seller; otherwise it signs from the market;
   - a rebuilding rival lets one veteran go who is not needed; a squad above 8 releases its weakest non-starter.
3. **Youth intake** rises to 10 a season. With rival squads the world holds about 83 players, and about 11 leave each
   season.
4. **Sponsors.** A rival that takes a lapsed inbound offer is now a draw weighted by reputation.

**New move:** `Transferred` (rival to rival), shown as `MoveKind.Transfer`. Transfer News already tells team-to-team
moves ("Tarnka joins Paper Tigers GG", one crest to the other). Rival signings, departures, releases and retirements
are now world transfer news too.

### Read model and UI

- **`Situation.RivalProfiles`:** each rival's squad as scouted (handle, role, age, a rating band), its head coach, the
  posture it is believed to play and a strength band (`LevelEstimate`).
- **League view.** Selecting a team in the standings opens its profile below: head coach, posture and strength
  estimates, and the squad table. Before any selection it shows the next opponent.
- **Not built:** showing rival players as "under contract" in Squad and Staff. Those screens list only the company's
  people and the open market.

### Measurement

8 seeds × 5 seasons, saved in `artifacts/v4-phase5-measure` (not tracked).

| Policy | Win rate | Cash, season 5 | Insolvent runs | Mean rival strength faced |
|---|---|---|---|---|
| Coach | 53.7% | $14,473 | 0 of 8 | 80.2 |
| Sponsor-greedy | 59.1% | $12,268 | 1 of 8 | 80.1 |
| Thrift | 25.4% | $6,774 | 0 of 8 | 80.3 |
| Balanced | 67.2% | $16,670 | 0 of 8 | 80.2 |

- **Rivals improve.** Recruitment and development lift mean rival strength from about 74 at the start to about 80 by
  each season's end, so win rates fall (phase 4: 73–83%). A contender's upgrades are bounded, but nothing yet pulls a
  strong league back. Phase 6 balances the drift against the company's own growth.
- **Insolvency:** 1 of 32 runs, back to the rules v3 baseline (phase 4: 3).
- **Gate 1, losing is not profitable:** pass, 8/8. **Reputation follows results:** 0.556 (phase 4: 0.51), still below
  0.6.
- **Player value:** 13.6 points. **Empty stretches:** 17 days.

### Checks (Phase 5)

- **Integration 348** (13 new), covering:
  - opening squads that cover every role, with one- or two-season contracts and funds;
  - derived strength across the calibrated spread, and a rival weakening when it loses its best player;
  - varied strategies;
  - a deterministic season end, after which every rival covers all five roles with funds of zero or more;
  - every rival move going where it says;
  - several seasons of recruitment and trading, with churn compared to the calibration (16% against about 25% a season);
  - a save and load of rival squads;
  - public profiles, team-to-team stories, and a coach replaced after poor seasons.
- **Long run.** With rivals, the whole population's mean age stays at 23.4–24.0, against 23.5. The pool stays at
  23–29, and about 10 players retire each season.
- **Shell native:** 3842 / 3882 (the rival profile region and table are checked). **Native kits:** 24 / 101 / 31.
- **Headless hash:** `fca16245…`.

## Phase 6 — measurement and balance (stopped at the plan's stop point)

**State: stopped after three tuning rounds, as the plan requires.** Gate 1 "reputation follows results" and gate 2
"multi-year progress" still fail. The values below are the best known state, committed as a restore point. The options
for closing the two gates need the user's decision.

### Measurement

Code: `tools/Headless/Measurement.cs`.

- **Gate 2 multi-year progress is now measured** from the league table: the balanced owner's mean final place, season 1
  against season 5. It passes on a gain of at least one place, with seasons 1–3 averaging above 1.5 (not already at
  the ceiling).
- **Outcome diversity** (reference): the spread of the balanced owner's season-5 place across seeds.
- **New references for the rules v4 world:**
  - **market depth:** season ends with at least three free agents in every role;
  - **rival churn:** newcomers as a share of rival squads each season, against the calibrated 15–35%;
  - **rival spread:** strongest minus weakest rival, against the calibrated 10–20 points.
- **The balanced owner manages its squad,** as the plan asks: it renews by value (a good player aged 28 or younger, or
  one the squad needs). It upgrades by estimated rating, signing a free agent at least 4 points better than the weakest
  player in that role when the fee is under half the cash, then letting the displaced player go.
- **Bug fixed.** Decision density divided by 56, the rules v3 season length. It now uses the content's season length,
  so earlier phase 3–5 densities were overstated by half.

### Rule and content changes

- **Reputation target:** half the season's record and half its final place (`Simulation.ReputationTarget`).
- **Pool cap sheds veterans first.** It used to remove the least potential. With 15 young players a season that was a
  selection effect that raised the whole population's mean rating by about 0.9 a season.
- **`AgeProfiles` made consistent.** A young player's expected ceiling (age shift plus mean headroom) was 4–7 points
  above prime-age players, so each cohort that replaced the opening players lifted the world. Every age now expects
  about +1.
- **Content:**
  - season length 84 → 76;
  - reputation regression 25 → 40%;
  - rival edge 4 → 6, one upgrade a season at a 4-point margin, funds 12,000 + 4,000 per place;
  - youth intake 15;
  - retirement 2% per rating point.

### Rounds

8 seeds × 5 seasons each; the logs are in `artifacts/v4-phase6/` (not tracked).

| Round | Change | Coach / balanced win rate | Reputation r | Empty days | Balanced places, seasons 1→5 |
|---|---|---|---|---|---|
| 1 | Reputation by record and place; new measures | 54 / 67% | 0.545 | 17 | 2.9 → 4.0 (falls) |
| 2 | Season 76, regression 40, fewer rival upgrades, edge 6, intake 15 | 52 / 67% | 0.591 | 9 ✓ | 2.9 → 4.9 (falls) |
| 3 | Consistent age profiles; balanced owner upgrades | 55 / 83% | 0.561 | 9 ✓ | 2.1 → 1.9 (at the top) |
| Committed (3b) | Pool cap sheds veterans; retirement 2% per point | 58 / 85% | 0.569 | 9 ✓ | 2.1 → 1.6 (at the top) |

Other results in the committed state:
- losing is not profitable: 7/8 ✓;
- insolvency: 0 of 32;
- rival churn 16.6% ✓; rival spread 17.1 ✓;
- market depth 87.5% (≥ 90%);
- player value 26.5 points (5–15).

### Why the two gates stay open

- **Multi-year progress depends on how good the company is at the start, relative to the league.**
  - A passive owner declines as the opening squad ages: places 2.9 → 4.9.
  - An active owner is near the top from season 2: places 2.1 → 1.6.
  - Neither can gain a place, because the company starts among the best. Fixing it means deciding how strong a new
    organization is on day one.
- **Reputation.** The in-season change is weighted by surprise: a favourite gains little from a win and loses a lot in a
  loss. That keeps the season's reputation change from tracking the win rate, even with the season-end pull at 40%.

### Options for the user

1. **Start the company as an underdog** (recommended), for the "new organization rising" fantasy. Generate the opening
   squad 3–5 points weaker (or the league 2–3 points stronger), so a well-run company starts mid-table and climbs.
   This is likely to close multi-year progress and to bring player value back toward 5–15.
2. **Reputation, either:**
   - (a) raise the season-end pull further (about 60%); or
   - (b) give each win a base reputation gain, not only the surprise; or
   - (c) redefine the gate around the league place, which in rules v4 is a better measure of results than the win
     rate. This one needs the Director, because it changes the proposal's gate.
3. **Keep the measured owner as defined, or limit its upgrades to the off-season.** Its strength decides what
   "progress" means.

Phase 7 (documents and handoff) waits for this decision; the plan ends with every gate passed.
