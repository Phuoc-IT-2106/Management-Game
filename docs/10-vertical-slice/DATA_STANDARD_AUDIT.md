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
