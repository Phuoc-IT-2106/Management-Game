# Data standard audit — players, clubs, staff and rival organizations

Date: 2026-10-14. Scope: the code as built at `d2af485` (rules v4 and its follow-ups). The audit asks whether the data
model is stable and standardized enough to build on: formulas, attributes, scales, identifiers and conventions for
players, the company, rival organizations and staff.

**Answer: mostly, but not yet.** The model is deterministic, integer-only and tested, with 369 integration checks and a
save validator that covers ownership, ledgers, schedules and history. Seven gaps remain before more content or
features are built on it:
1. no single data dictionary of the model as it is implemented;
2. no stated rounding rule;
3. many coefficients still hard-coded;
4. generated names and handles repeat within one campaign;
5. a released player re-enters the market at a wrong price;
6. rival and staff data are thinner than the company's, and that asymmetry is not documented;
7. a money scale that does not read as US dollars.

The fixes for 1–5 are mechanical. Items 6 and 7, and part of 3, need decisions.

---

## 1. What is already solid

| Area | Where |
|---|---|
| One owner per person: the company, a rival or the open market; checked on every load | `SnapshotValidation.cs:33` |
| Integer arithmetic, seeded draws keyed by entity and purpose, content order never changes a world | `Determinism.cs`, `Generator.cs` |
| Bounds on every content table, and a content file pinned by SHA-256 | `ContentLoader.Validate`, `content/fixture.sha256` |
| Person attributes range-checked on load (skills and mind 1–99, form, morale, popularity) | `SnapshotValidation.cs:18–23` |
| Calibration from aggregate data only, pinned by the report's hash; no real person in the game | `Calibration` block, `tools/lol-data-ingestion` |
| Transfer history typed and validated by direction | `SnapshotValidation.cs:89–105` |
| Derived values never stored: rating, rival strength, standings | `Ratings`, `Rivals.Strength`, `Seasons.Table` |

## 2. Findings

Priority: **P0** prevents wrong data or bugs now. **P1** is needed before more content or features. **P2** is a design
decision. "Decision" marks items that need the user or the Director.

### 2.1 Cross-cutting conventions

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| C1 | **No data dictionary of the implemented model.** Entity Data v0.1 and Player Data v0.2 are PROPOSED and superseded in part. Rules v4 records phases, not one table per entity. | `entity-data-foundation/README.md`, `player-data/README.md` | Write `DATA_DICTIONARY.md`: one table per entity giving each field's meaning, scale, unit, owner, who sees it (known, estimated or hidden), whether it is stored or derived, and its validator. Mark v0.1 and v0.2 as historical. | P0 |
| C2 | **Three scales for "the same kind" of number.** Skills, mind and potential are 1–99. Readiness is 1–100. Morale and popularity are 0–100, but generation draws popularity in 1–99. Coach skills are 1–100. Reputation and information are 0–100. Form is −10..10. | `Model.cs:24–36`, `Generator.cs:81`, `SnapshotValidation.cs:23–25` | Fix one scale per family: abilities 1–99 for players and coaches alike, condition 0–100, form −10..10. State it in C1, and align the generator and validator. | P1 |
| C3 | **Rounding is not one rule.** `Numbers.Divide` rounds half to even. Many formulas use C#'s `/`, which truncates toward zero, so a negative term rounds differently from a positive one. | `Determinism.cs:10–14`; `/` in `Pay.Player`, `Competition.Resolve`, `Careers.Change`, `Generator.Player` | Choose one rule (recommended: `Numbers.Divide` everywhere in rules code) and convert the formulas, or document per formula where truncation is intended. This changes hashes, so do it with a measurement round. | P1 |
| C4 | **Coefficients in code, against the rules v4 principle** ("every coefficient from content"). Examples: secondary-role proficiency 80%; the wage curve (/5, popularity /2, ambition /5, youth headroom); release cost ×3; the coach wage; the match terms (matchup +3/−1, opponent read 25%, leadership threshold 60, style /4 ±3, composure /12 ±2); generated readiness 80–98 and morale 50–70; rival reputation steps. | `Ratings.cs:8,35–55`; `Competition.cs:23–40`; `Generator.cs:77–81`; `Rivals.cs:126`; `Simulation.cs:84` | Move tunable values into content (a `Formulas` block). Keep true rules (five roles, best-of formats) in code. List which is which in C1. | P1 (decision: which are rules) |
| C5 | **Keyed-draw names are not registered.** Domains and purposes ("player", "intake", "rival", "scouting", ...) are free strings. A new draw can silently reuse a key and correlate two outcomes. | `KeyedRandom.Range` call sites | Keep a registry table of domain, key shape and purpose in C1, plus a test that fails on an unregistered domain. | P1 |
| C6 | **ID shapes are conventions, not rules.** They include `company:player`, `rival:rNN`, `person:gNNNNN`, `coach:gNNNNN`, `employment:…`, `fixture:s…`, `move:…`. Uniqueness is checked; shape is not. | `CampaignFactory.cs:28`, `SnapshotValidation.cs` | Document the shapes in C1, and validate the prefix per owner, for example that every rival person is `person:`. | P1 |
| C7 | **Formulas are tested by direction, not by value.** The rating has an exact check, but wage, fee, release cost, renewal demand, development, retirement chance, rival strength and the logistic table are checked as "more than / less than". | `tests/Integration/Program.cs:162–166, 790–831` | Add golden tables: a few fixed inputs per formula, with their exact outputs. A formula change then shows as a deliberate test edit. | P0 |

### 2.2 Players

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| P1 | **Handles and names repeat.** There are 525 handle combinations (35 prefixes × 15 suffixes) and 256 names per nationality. A 4-season test save (day 308, 74 players) has 4 handles shared by 8 players and 1 duplicated legal name. Transfer News, the inbox and tables identify people by handle. | `content/fixture.json` (`Handles`, `Nationalities`); `artifacts/save-tests-*/rivals.save.json` | Generate unique handles: retry with the next keyed draw while the handle is in use, among living people and the transfer history. Enlarge the pools. Validate handle uniqueness among current people on load. Legal names may repeat, but name and nationality should not. | P0 |
| P2 | **A released player re-enters the market at a wrong price.** The listing uses the contract's release cost (3 days of wage) as the asking fee, instead of the market fee (20–60 days). | `Simulation.cs:74` | List with `Careers.Listing(person, season, content)`, as rival departures already do. | P0 |
| P3 | **The release cost is a flat 3 days of wage**, whatever the contract's remaining length. It is cheap next to the 20–60 day transfer fee. | `Lifecycle.cs:78`, `Simulation.cs:84` | Make it a share of the remaining wages, in content. This is an economy rule. | P2 (decision) |
| P4 | **Invariants not checked on load:** potential ≥ rating (generation and development keep it, but nothing checks it); nationality is one of the content's nationalities (only "two letters" is checked); handle uniqueness (P1). | `SnapshotValidation.cs:18–23` | Add the three checks. | P0 |
| P5 | **Age has no calendar meaning.** A player ages one year every season, and a season is 76 days. Contracts run in seasons. The UI shows "Season 1, Year 1". | `Ratings.Age`, `Balance.SeasonLength` | State in C1 that one season is one competitive year. Keep "age" for the season count, and never show a birth date. | P1 |
| P6 | **The wage curve is flat.** Rating 50 asks $5.00 a day and rating 90 asks $16.20 (×3.2). Real esports pay spreads far wider between rookies and stars. | `Ratings.cs:35–43` | Revisit the curve together with the money scale (E1). | P2 (decision) |

### 2.3 Clubs: the company and rival organizations

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| O1 | **The company's identity is a name only.** DEC-023 requires "name, abbreviation, emblem reference and primary/secondary identity colors" as campaign data. The crest is derived from the ID, and there is no abbreviation or colors. | `Model.cs:56`; `PortalView.cs:48` | Add `Abbreviation` (2–4 letters), `CrestAssetId` and two colors to the company, chosen on the start screen. Give rivals the same fields from content. | P1 |
| O2 | **Rival players' condition is frozen.** Readiness, form, morale and popularity of rival players never change, and rival strength ignores them. The company's base strength uses readiness and form. | `Rivals.cs` (no condition updates), `Rivals.cs:38`, `Competition.cs:21` | Either simulate condition for rivals in a light form, or document it as not simulated and hide those fields for rivals. Today a frozen popularity is shown in rival profiles. | P1 (decision) |
| O3 | **Rival economy is abstract and resets.** Funds are set from the final place every season, with no carry-over. Wages are capped but never paid. There are no sponsors and no insolvency. Reputation moves by a fixed step per place. | `Rivals.cs:125–128, 208` | Acceptable under DEC-015 (bounded rival simulation). State in C1 which company quantities rivals do not have. | P2 (decision) |
| O4 | **Two reputation models.** The company's reputation moves by surprise, a base win gain and a season-end pull. A rival's moves by `teams + 1 − 2 × place`, clamped to 5–95. | `Competition.cs` (`Commercial.Consume`), `Rivals.cs:126` | Document both. Before reputation drives rival sponsorship or recruitment more, consider one formula. | P2 |
| O5 | **No club attributes beyond squads.** Facilities, academy and region are listed for later waves (board objectives, facilities). | handoff "Not built yet" | No action now. Add them through C1 when a wave needs them. | — |

### 2.4 Staff (coaches)

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| S1 | **A coach has no age, nationality, birth season, career or development.** Coaches never age, improve, decline or retire. The coach market is regenerated. | `Model.cs:36`, `Generator.Coach` | Coach v1: add `BornSeason` and `Nationality` (already drawn for the name, then thrown away), with slow development or decline at season end. | P1 |
| S2 | **Attribute scale and meaning.** Skills are 1–100, while players are 1–99. `Capacity` is a different kind of number (20–1000, a workload size). `Preparation` sets daily work conversion. | `Generator.cs` (`Coach`), `Simulation.cs:212` | Put coach skills on the player scale (C2). Describe `Capacity` as a workload unit in C1. | P1 |
| S3 | **The coach wage formula stands alone.** It is (four skills × 4) + capacity × 2. Unlike player wages, it has no reputation, popularity or age term. | `Ratings.cs:51` | Align it with the player wage curve in the money-scale work (E1). | P2 |
| S4 | **One staff role.** The mockup `09_staff` shows assistant, performance and analyst roles. DEC-014 defers a deeper hierarchy. | `09_staff_mockup.webp`, DEC-014 | A staff-role model is a feature decision. If it is built, it should share the person, contract and estimate conventions of C1. | P2 (decision) |
| S5 | **Rival head coaches have no contract.** They change only after poor seasons. | `Model.cs:74` (`Rival.Coach`) | Fine for now (O3). Document it in C1. | — |

### 2.5 Data stability

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| D1 | **Every rules change breaks every save** (schema 1 → 6 so far, never migrated, as DV-08 allows). Fine for development, not for testers. | `SnapshotStore.cs:15` | Before external testers, decide a compatibility policy: versioned DTOs with forward migrations, at least for additive fields such as the coach's recruitment limits. | P2 (decision; DV-08) |
| D2 | **The content hash is refreshed by hand.** A one-line content edit needs `fixture.sha256` recomputed (done in Python this session). | `ContentLoader.cs:54` | Add `build/Rehash-Content.ps1`. Make `Verify.ps1` print the content hash next to the headless hash. | P1 |
| D3 | **Cross-machine determinism is not yet shown** (DV-06). The headless hash is pinned on one machine. | PROJECT_STATE, DV-06 | Run the headless and Integration hashes on a second machine before any determinism claim. | P1 |
| D4 | **Real-data pipeline.** It is research-only and isolated; the calibration is pinned. | `tools/lol-data-ingestion` | Keep the boundary. Any new calibration goes through `apply_calibration.py` and a new pinned report, never a live crawl in play. | — |

### 2.6 Economy scale (decision)

| # | Finding | Evidence | Recommendation | Priority |
|---|---|---|---|---|
| E1 | **The money does not read as US dollars.** Starting cash is $2,600. Running costs are $25 a day. A champion earns $1,500. Wages are $5–16 a day, and brand base payments are $180–1,100 a week. DEC-027 relabelled CU as dollars at 1:1. The measured financial pressure is 3%, against 20–60%. | `content/fixture.json` (`StartingCash` 260000, `OperatingCost` 2500, `PlacePrizes`, `Brands`) | Decide one money scale, for example ×100 or ×1000 with a steeper wage curve (P6, S3), and retune the economy for financial pressure in the same round. | P2 (decision) |

## 3. Suggested order

1. **P0, mechanical, about one session:**
   - unique handles and the load checks (P1, P4);
   - the released player's listing (P2);
   - golden formula tables (C7);
   - the data dictionary (C1), with the ID shapes (C6) and the keyed-draw registry (C5).
2. **P1, conventions:**
   - one scale per attribute family (C2);
   - one rounding rule (C3), with a measurement round;
   - coefficients into content (C4);
   - coach v1 fields (S1, S2);
   - company identity fields (O1);
   - the content rehash script (D2);
   - a second-machine hash run (D3).
3. **P2, decisions for the user or the Director:**
   - the money scale and wage curve (E1, P6, S3, P3);
   - rival condition and economy (O2, O3, O4);
   - staff roles (S4);
   - save compatibility (D1).

None of this changes accepted decisions. C3, C4, E1 and any change to generation change the headless hash and the
balance, so each should end with `build/Measure-Rules.ps1 -Gate 2`.

---

## 4. Rating and distribution measurement (2026-10-14)

**Method.** A throwaway probe (not in the repository) generated 12 worlds at the start, then played 6 of them for five
seasons with a delegating owner. It dumped every player, coach and organization, with 3,137 player records and 1,305
league series between rivals. Real references come from
[calibration_2026.md](../../tools/lol-data-ingestion/reports/calibration_2026.md).

### 4.1 What matches real data

| Measure | Model | Real 2026 | Verdict |
|---|---|---|---|
| Team spread inside a league | Rival strength SD 5.3 points, about 0.59 logits at 9 points per logit | Team Elo SD about 108, about 0.62 logits | matches |
| Top vs bottom of a league | top-bottom spread 11–18 points; a 12–14 point favourite wins 92% of Bo3 | 351 Elo, 88% per series | close |
| Ages | mean 23.5, 17–31 | peak 22–23, 16–37 | matches (calibrated) |
| World drift over five seasons | rival players 68.9 → 70.0, market about 70, rival strength 76.1 → 76.9, age 23.5 → 22.4 | — | stable |

### 4.2 What does not

| # | Finding | Measured | Why it matters | Fix |
|---|---|---|---|---|
| R1 | **Ability comes from four authored bands** (60/70/78/88, weights 25/50/23/2%). The distribution is lumpy: peaks at 60–65, 70–73 and 78–81, with dips at 66–67 and 74–77. Nothing in it comes from real data. | rating mean 68.6, SD 8.0, 47–92 | Ratings cluster instead of separating players by ability | A continuous quantile table per tier, calibrated from per-player real data (§5) |
| R2 | **The five skills are nearly one number.** Each skill is band + archetype offset + jitter ±4. Skills correlate 0.51–0.81. A player's skills spread SD 4.7 around their mean, and the role rating correlates 0.991 with the plain mean of the skills. | correlation matrix, within-player SD | Role weights barely matter. A great laner and a great teamfighter differ only by the archetype's ±8. | Draw skills from a calibrated covariance (role profile + independent part); derive archetypes by clustering real profiles |
| R3 | **Free agents are better than contracted players** at the start: market mean 70.1, rival players 68.1, company 66.8 (the underdog shift is intended). | means by group | In a real league the open market is mostly fringe and young players | Generate the market from a lower tier band |
| R4 | **Potential carries no information after 21.** Headroom is 0.4–1.6 points from age 21, and potential correlates 0.97 with rating. | age table | Real players improve until about 23–24; scouting potential matters only for teenagers | Calibrate headroom by age from real rating trajectories, when per-player data exists |
| R5 | **Mind attributes are uniform and inert.** Consistency, composure and communication are uniform 35–85 and independent of everything. No squad ever triggers the communication penalty (0 of 96). Composure gives 0 to +1 in big matches; consistency moves spread by −1 to +2. | distributions, effect sizes | Three attributes shown in the profile barely act; uniform draws give as many 35s as 85s | Bell-shaped draws. Real proxies: consistency from game-to-game variance, composure from playoff vs regular performance. Effect sizes large enough to matter (measure them) |
| R6 | **Coaches barely differ.** Each skill is uniform 58–80 and independent. Across that range, analysis narrows estimates by about 2 points, the meta-work multiplier moves 1.08 → 1.30, the adaptation term +0.8 and growth +4% → +15%. The coach wage is almost flat ($11.34–14.06 a day, SD $0.57). | coach table | Hiring a better coach changes little and costs almost the same | A wider, bell-shaped skill range; a wage that follows quality; effect sizes checked like players' |
| R7 | **Series are too even.** Bo3 2–0 is 54% (real 64%). Bo5 is 3–0 20% (real 46%), 3–1 47% (29%) and 3–2 33% (25%). | 1,305 series | Games in a series are independent draws. Real series share the day's form, so the better side sweeps more often | A series-level shared draw (form on the day) plus a smaller per-game variance, tuned to the real score mix at the same favourite win rate |
| R8 | **Popularity is rating.** It correlates 0.91 with rating, has a maximum of 70 at the start, and nobody reaches the star threshold of 80. | distribution | Star sponsorship never works at the start; popularity adds nothing that rating does not | Real proxy: following and region (unavailable on the Free plan); otherwise a separate draw correlated with rating, not determined by it |
| R9 | **Leagues cannot be compared.** Real Elo was computed per league, so the tier medians are all about 1500 (A 1505, C 1502, D 1503). | calibration report | Real teams from different leagues cannot be placed on one scale | Use international events (MSI, Worlds) or a prior per league, before any cross-league rating |
| R10 | **Rival strength adds a flat Edge of 6.** A rival with the same players as the company is 6 points stronger. Rival readiness and form are ignored, and the company's are applied. | `Rivals.Strength`, `Competition.Resolve` | The asymmetry is hidden from the player | Document it, or replace Edge with the rival coach's preparation through the same formula |

## 5. Real players with true-to-ability numbers: what it takes

**Decision (2026-10-14).** The user decided that real players and teams enter the game. This is recorded as proposed
[DEC-031](../01-governance/PROPOSED_DECISIONS.md): a pack built locally from the user's own data, kept in git-ignored
`data/`, never committed or redistributed.

**Current state.** Rules v4 decided aggregate calibration only, with no real person in the game. The PandaScore Free plan
has no per-player statistics. So no real player can carry numbers that reflect their ability today, and the generator
is not calibrated per player either. Only ages, team spread, formats, roster sizes and turnover come from real data.

**Pipeline that would do it** (research side, `tools/lol-data-ingestion`):
1. **Source.** Oracle's Elixir 2026 game-level player rows. The import and the identity reconciliation are built and
   wait for the CSV. Participation verified by reconciliation decides who counts.
2. **Per-player metrics, role-normalized**:
   - lane: gold, CS and XP difference at 10 and 15 minutes;
   - damage per minute and damage share;
   - kill participation and deaths share;
   - vision score per minute;
   - first-blood involvement;
   - the number of distinct champions and their results.

   Each metric becomes a z-score within the role and league.
3. **Opponent and league adjustment.** Weight by opponent team Elo. Link leagues through international events (R9).
4. **Shrinkage.** Shrink toward the role's mean by games played (empirical Bayes), so a player with 6 games does not
   outrank one with 60.
5. **Mapping to the game's skills**:
   - mechanics: damage and solo outcomes;
   - early game: the lane differences;
   - teamfight: kill participation, damage share and deaths;
   - macro: vision and objectives;
   - champion pool: breadth with results;
   - consistency: the inverse of game-to-game variance;
   - composure: playoff vs regular performance.

   Communication stays unobservable, so it is unknown or authored. Percentiles become the 1–99 scale.
6. **Validation.** Team strength rebuilt from five player ratings must predict real series about as well as Elo (Brier
   0.22), and the rating spread inside a league must match the team spread.
7. **NPC compliance.** Fit the generator to what step 5 produces, so a generated player is statistically
   indistinguishable from a real one of the same role, tier and age:
   - the quantile table per tier and role (R1);
   - the skill covariance (R2);
   - archetypes as clusters;
   - headroom by age (R4);
   - mind distributions (R5).

   A test then compares generated and real distributions (per-quantile difference, correlation matrices).

**Constraints.**
- Real names, handles and likeness in the game need rights the project does not have.
- PandaScore terms forbid redistributing its data. Oracle's Elixir's terms must be read when the CSV is downloaded.
- The safe use is aggregate:
  - fit the generator from real per-player numbers;
  - keep any per-player table in git-ignored `data/`, never in content or saves.

## 6. Changes made on 2026-10-14 (the fixes that need no per-player data)

The user decided:
- real players and teams in the game from a locally built pack (DEC-031, proposed; §5);
- to fix now what needs no per-player data, with authored values to be replaced once the Oracle's Elixir CSV arrives.

| # | Change | Where |
|---|---|---|
| R7 | **The day's form.** One keyed draw within ±`SeriesForm` (12) is shared by every game of a series, on top of each game's own variance. `Variance` 8 → 4, around which posture moves ±4. `LogitScale` 900 → 700. These values were tuned in a replay of 1,305 simulated series to the real score mix and favourite win rate. | `Competition.Play`, `Competition.Spread`; content `Balance` |
| R1 | **Continuous ability.** The four bands become 41 values (52–92) with normal weights, keeping the old mean and SD (69.7, 6.8). | content `Generation.AbilityBands` |
| R2 | **Distinct skills.** `SkillJitter` 4 → 7, archetype offsets ×1.5. | content |
| R3 | **A weaker open market.** `Generation.MarketShift` −2 for the opening pool and for emergency listings. | `Market.Refresh`, content |
| R5 | **Mind that acts.** Mind and personality are bell-shaped (the mean of two keyed draws). Leadership is graded: the best communicator, a point per 5 above or below 80 (−3..+2), instead of a penalty no squad ever met. Composure in big matches: a point per 4 of the lineup's mean above or below 60 (±3). Consistency now narrows a smaller base variance. | `Generator.Bell`, `Competition.Resolve` |
| R6 | **Coaches that differ.** Coach skills are bell-shaped in 45–92 (was uniform 58–80). The wage follows quality: the mean skill squared over four, plus twice the capacity. | `Generator.Coach`, `Pay.Coach`, content |

**Measured after the change** (same probe):

| Measure | Before | After | Real 2026 |
|---|---|---|---|
| Rating distribution | lumpy, peaks at 60–65 / 70–73 / 78–81 | one smooth peak, mean 68.4, SD 7.9 | — (per-player data pending) |
| Skill correlations | 0.51–0.81 | 0.15–0.59 | — |
| A player's skill spread (SD around their mean) | 4.7 | 7.2 | — |
| Market vs rival players (start) | 70.1 vs 68.1 | 68.8 vs 68.3 | market at or below contracted |
| Mind SD (consistency) | 15.0, uniform | 10.6, bell-shaped | — |
| Leadership term across squads | always 0 | −3..+2, centred on 0 | — |
| Coach skill SD, coach wage range | 6.8; $11.34–14.06 | about 10; $9.46–18.72 | — |
| Bo3 2–0 | 54% | 65% | 64% |
| Bo5 3–0 / 3–1 / 3–2 | 20 / 47 / 33% | 41 / 32 / 27% (41 series) | 46 / 29 / 25% |
| Favourite with a 12–17 point gap wins Bo3 | 92–96% | 84–93% | 88% (top vs bottom) |
| Rival strength spread per league | 11–18 | 11–15 | ≈ 14 (calibrated) |
| World drift over five seasons | stable | stable | — |

**Gates** (`build/Measure-Rules.ps1 -Gate 2`, artifacts `dist-measure-r1` and `dist-measure-r1-16`):

| Seeds | Gates 1–2 | Multi-year progress (places by season) | Player value | Market depth | Financial pressure | Outcome diversity |
|---|---|---|---|---|---|---|
| 8 (default) | gate 2 **open** | 0.75 (2.3, 1.5, 2.3, 1.3, 1.5) | 15.3 | 100% | 1.9% | 0.87 |
| 16 | **pass** | 1.38 (2.6, 1.6, 1.7, 1.5, 1.3) | 13.1 | 100% | 5.6% | 0.66 |

With 8 seeds, multi-year progress turns on one noisy season. With 16 seeds the gates pass, so the rules were not changed
to chase the 8-seed figure.

**(Decision)** Should the gate standard become 16 seeds? About 20 minutes instead of 10.

Outcome diversity fell: the balanced owner wins the league on 14 of 16 seeds. That is part of the open economy and
difficulty question (handoff item 2).

Verification after the change:
- Integration 369 (`e196d247…`);
- shell 3861 / 3901;
- native kits 24 / 101 / 31;
- headless `a789c421…`.

Not committed yet.

