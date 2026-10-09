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
| 1 | Real-data tooling and the calibration report | Done |
| 2 | Roles and the player model | Done |
| 3 | Logistic series resolver, familiarity, an 8-team league, playoffs | — |
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
