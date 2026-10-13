# Gameplay data review — players, teams, rival organizations

Status: **PROPOSAL — review for the Director and the user** (2026-10-09). Nothing here changes rules, content, saves or
accepted decisions.

**Implemented** (2026-10-13), by the user's plan, as [rules v4](RULES_V4_PLAYER_WORLD.md):
- five roles and the decision-led player model (§5.1);
- the logistic best-of resolver and the league (§6.2);
- development (§6.4) and popularity reaching the company (§6.5);
- rivals with squads and recruitment (§5.4, §6.6, minimal);
- real data for aggregate calibration only (§7.2–7.3).

Where the implementation differs, the rules v4 document is current.

Source request: the user's instruction (2026-10-09) to review the standards for attributes, formulas, metrics and
information for players, teams and rival companies. The review should say what shapes the core gameplay, what needs
fixing, and how real API data and generated NPC data should feed future gameplay.

Read for this review:
- runtime code:
  - `Model.cs`, `Competition.cs`, `Seasons.cs` (`Generator`, `Pay`), `Market.cs`, `Simulation.cs`;
  - `Observations.cs`, `content/fixture.json`;
- [Player Data v0.2](player-data/README.md) and [Entity Data v0.1](entity-data-foundation/README.md);
- [Esports competition spec](../07-specialist-specs/ESPORTS_COMPETITION_SPEC.md) and
  [Rival simulation](../06-core-loop-simulation/RIVAL_SIMULATION.md);
- DEC-001–023: in particular DEC-003/008 (company first), DEC-005 (depth only where decisions matter), DEC-006/015
  (evolving, bounded rivals) and DEC-020 (a fictional test discipline);
- the [PandaScore discovery report](../../tools/lol-data-ingestion/reports/API_DISCOVERY_2026.md) and the Oracle's Elixir
  importer.

## 1. Verdict

1. **Today the match is decided by one player number, Execution × Readiness.**
   - Adaptability and Consistency are generated and saved, but they barely move a result.
   - They are never shown to the player and are not priced.
   - Rivals are six numbers, with no people, money or identity behind them.
   - The game has no player career: no age, potential, growth, decline, form, morale or fame.
   - So the only long-term lever is buying a higher Execution.
2. **The company-first pillar has no data link to people.**
   - Reputation, audience and sponsors react only to match results.
   - No player attribute feeds the business side: no star power, professionalism or popularity.
   - That link is what would make this game different from a team-only manager.
3. **The two data packages solve a later problem first.**
   - Player Data v0.2 and Entity Data v0.1 are careful about measurement, provenance and determinism. Those are good
     constraints to keep.
   - But they derive ratings from public statistics *before* the game knows which decisions the ratings serve, and both
     end in "NEEDS REVISION / not approved".
   - The free API cannot supply player statistics, so that path stays blocked.
   - Recommendation: **design the attribute set from decisions first (DEC-005). Then calibrate the distributions of a
     generated world from real aggregate data.** Real per-person ratings become an optional, rights-gated pack later.
4. **Real data is valuable today for calibration, not as people in the game.**
   - The free tier gives the shape of the world:
     - leagues, tiers and regions;
     - tournament rosters and roster sizes;
     - nationality and age mixes;
     - Bo1/Bo3/Bo5 shares;
     - 3,828 team-level 2026 match records (finished or cancelled), enough for team ratings.
   - These are exactly the parameters an NPC generator and a rival model need.
5. **The role model is undecided** (abstract A–E versus Top/Jungle/Mid/Bot/Support). It blocks every role-weighted formula
   and every data join. It is the first decision to take (PD-D01/D02).

## 2. What the current numbers actually do

Development fixture values (`slice-rules-v3`). One point of `base − Strength` is 1.2 win-probability points
(`MatchScale` 12,000 on a 1,000,000 scale), clamped to 5–95%.

| Field | Range today | Consumer | Real effect | Shown / priced |
|---|---|---|---|---|
| Person.Execution | 62–76 at start; 58–88 on the market | `base = Σ(E×R)/(5×100)` | One player +10 at readiness 90: +1.8 base, about +2.2 points | Own players exact, market as a band / salary `E²/5` |
| Person.Readiness | 80–98 at start; capped at 70 on signing; −8 per match; +1 per day | Multiplies E | One E70 player at 70 instead of 95: −3.5 base, about −4.2 points | Shown / not priced |
| Person.Adaptability | same ranges | `(⌊mean A⌋ + coach A)/30`, round-even | **The whole range moves the term by about 1 point** (62→4, 76→5) | Hidden / free |
| Person.Consistency | same ranges | Noise width `8 + 4·posture − ⌊mean K/20⌋` | Changes width by about 1; symmetric noise, so the **expected** result does not change | Hidden / free |
| Coach.Preparation, Capacity | 58–80 / 72–90 | Work conversion under load | Real; this is where preparation choices live | Market band / priced |
| Coach.Analysis | 58–80 | Pay and the market band only | **No gameplay effect** | Priced, useless |
| Coach.Adaptability | 58–80 | Adaptation term | About 1 point across the range | Priced |
| Company.Information | 70, never changes | Opponent estimate width and confidence | No faucet or sink: a constant | Shown |
| Rival.Strength | 64–78, drift ±1 per 4 days by Need, seasonal ±4 | Opponent term | The main difficulty dial | Shown as an estimate band |
| Rival.Analysis | 64–78 | Penalty on repeated posture; posture counter at ≥60 | Real; the only rival "intelligence" | Hidden |
| Rival.Adaptability | 64–78 | none | **No effect** | Hidden |
| Rival.Budget / Need | 40, reset each season / 0–1 | Claims one lapsed sponsor offer; Need steers strength drift | Strength grows for free when Need = 1 | Hidden |

Other facts:
- **Market players beat starters.** The market range (58–88) is wider and higher than the starting roster (62–76), yet
  nothing like age or potential explains why.
- **The world is small and anonymous.**
  - Names come from 40 given × 40 family names, with no nationality and no in-game handle.
  - There are 20 organization names.
  - Rival-vs-rival matches do not exist, so a league table is impossible (rules v3 wave 2).
- **Starting strength.** A typical starting roster has base ≈ 61–62 (mean Execution 69 × mean readiness 0.89). The mean
  rival is ≈ 71, so the company starts about 9–10 points behind before preparation. That is a balance fact, not a bug, but every new attribute must be scaled against
  it.

## 3. What gameplay needs, by decision

DEC-005 asks for depth only where it creates decisions. Each entry below names the decision it creates and the company
link. The **Wave** column maps to the rules v3 roadmap in the
[handoff](../05-handoffs/SESSION_HANDOFF_2026-10-08.md#what-remains).

| Need | Decision it creates | Company link | Wave |
|---|---|---|---|
| Role ratings from a few role-weighted skills | Who plays where; role-flexible cover for shortfalls | Roster minimum and fines | 2 |
| Team style: early versus late game | Plan against this opponent; sign for the style | — | 2–3 |
| Familiarity: lineup time together | Stability versus upgrading; transfer timing | Integration cost of every signing | 2 |
| Age, potential, development, decline | Buy veterans or develop youth; when to sell | Facilities as cash sinks (wave 3) | 3 |
| Form and morale | Rotation; who starts the final | — | 3 |
| Consistency with a real consumer | Underdogs want volatile players; favourites want steady ones | — | 2 |
| Composure (big matches) | Lineup for playoffs and finals | Prize and reputation at stake | 2 |
| Communication / shotcalling | At least one leader in the five | — | 3 |
| Professionalism | Sign a talented but unreliable star? | Scandal risk to reputation and sponsor clauses | 3 |
| **Popularity (fan following)** | Sign for business, not only strength | **Audience, sponsor appeal, merchandise**: the company-first hook | 2–3 |
| Ambition and loyalty | Renewal difficulty; transfer requests after poor seasons | Wage demands and market value | 3–4 |
| Market value and wage demand from all of the above | Every signing becomes a value trade, not only `E²` | Payroll pressure | 2 |
| Scouting knowledge per attribute | Pay to know; the bargain hidden in the market | Information as a resource with faucets and sinks | 3 |
| Rival rosters of real `Person`s | Poaching, bidding wars, rival transfer news | Transfer News v2 is ready for team-to-team moves | 4 |
| Rival finances, reputation and personality | Who outbids you; who sells cheap; who fades | Sponsor and talent competition (DEC-006) | 4 |
| League tiers, promotion and relegation | Multi-year goals; budget by tier | Board confidence and revenue by tier | 2 (decision open) |

**Not needed, per DEC-005:**
- per-champion ratings, draft simulation and in-game event or minimap simulation;
- per-region salary law;
- real-time stats per game in the UI.

Champion depth is abstracted as **Champion pool / meta flexibility**, a single attribute that interacts with meta shifts.

## 4. Review of the existing proposal packages

| Package | Keep | Change |
|---|---|---|
| Player Data v0.2 | The three layers (latent truth / measured estimate / actor's scouting estimate) and their non-mixing rule; strict "unknown is unknown"; key grammar and determinism; no fabricated person data; the 1–99 display mapping | Stop requiring real-statistics measurement (gates B/C) before gameplay design. Replace the two-attribute L/C proposal with the decision-led set in §5, where "early game" applies to all five roles, so Jungle and Support are not excluded. Move measurement to an optional real-data pack track. |
| Entity Data v0.1 | Company/World as the only owners; one owner per person; hybrid two-pass world generation; no hidden cash or duplicate talent; rejection of chemistry *bonuses* without a consumer | Allow **explicit rival rosters** of real `Person` records drawn from the same world pool, instead of anonymous reserved slots. Without them, rival transfers, poaching and team-to-team news cannot exist. Give rivals a budget band and a personality (§5.4). |
| Competition spec §3.2, §10, §14 | Role proficiency, familiarity, form, lineup stability, rival development, "a roster upgrade has an integration cost" | Already the right target. The runtime simply never implemented it. |
| Rival simulation §3–7 | The four postures (contend, balanced, rebuild, conserve), decision windows and minimum actions | Adopt as the wave-4 contract. Start with transfers and coach changes, before finance detail. |

## 5. Proposed standard (candidate v1, all values tunable)

Scales:
- Abilities use **1–100 internally**. Display uses the existing information markers: Known values exact, Estimated
  values as bands.
- Money is cents, as today.
- Every generated value comes from rng-v1, keyed by stable ID and purpose.

### 5.1 Player

| Group | Fields | Notes |
|---|---|---|
| Identity | `Id`, `Name`, **`Handle`** (in-game name), `Nationality`, `BirthDay` (campaign day; age is derived), `HomeRegion` | The handle is the esports identity. Generated handles must not copy real handles. |
| Role | `PrimaryRole` (Top / Jungle / Mid / Bot / Support, once PD-D01/02 is decided; today A–E), `RoleProficiency[role]` 0–100 | The primary role is 100; off-roles are typically 40–75 |
| Technical (5) | `Mechanics`, `EarlyGame` (laning, early jungle pressure, lane support), `Teamfight` (damage, engage, peel), `Macro` (vision, objectives, rotations), `ChampionPool` (meta flexibility) | Replaces Execution and Adaptability. Role rating = Σ role weight × attribute (§6.1). |
| Mental (3) | `Consistency`, `Composure`, `Communication` | Each gets a consumer (§6.2) or is not shipped |
| Personality (hidden, 3) | `Professionalism`, `Ambition`, `Loyalty` | Never shown exactly; inferred from events and scouting |
| Career | `Potential` (hidden ceiling for the role rating), development state | Age curve in §6.4 |
| Condition | `Readiness` (exists), `Form` (rolling, −10..+10), `Morale` (0–100) | Form comes from match outcomes and surprise, not from hidden truth |
| Commercial | **`Popularity`** 0–100 (fan following), derived `MarketValue` | Popularity grows with wins, big-match moments and a big organization; it decays with bench time |
| Contract | salary, end day, release cost, `Role promise` (starter / rotation / substitute) | A broken promise hurts morale. This is part of the wage negotiation. |

The legacy mapping exists so saves and tests can migrate within a new rules version:
- Execution ≈ the role rating;
- Adaptability ≈ ChampionPool;
- Consistency is kept as is.

### 5.2 Coach

`Preparation` and `Capacity` keep their current roles. The other fields get consumers or become new fields:
- **`Analysis`** feeds scouting quality: it narrows opponent and candidate estimate bands, which today come only from
  `Information`.
- `Adaptability` becomes **meta reading**: it scales the meta preparation term.
- `Development` (new) drives player growth (§6.4).
- `ManManagement` (new) drives morale recovery.
- Personality and identity fields follow the player.

### 5.3 Team (derived, owned by Company or World; no third owner)

| Field | Formula |
|---|---|
| `TeamPower` | Mean role rating of the five, weighted by readiness and form |
| `Familiarity` 0–100 | +1 per shared match-day (or training week) for each pair in the five, up to 100; a new member starts at 0 with everyone. The team value is the pair mean. |
| `Style` (early ↔ late) | (Σ EarlyGame − Σ Teamfight) of the five, standardized |
| `Leadership` | Max Communication in the five; below a threshold, a coordination penalty |
| `Elo` | Public rating updated after every series; used for standings, power ranking and rival AI evaluation |

### 5.4 Rival organization

| Group | Fields |
|---|---|
| Identity | `Id`, `Name`, `ShortName`, `Region`, `Tier` / league, `FoundedYear` (flavour) |
| Competitive | **Explicit roster of `Person`s** (6–8) and a head coach, from the shared world pool; `Elo` |
| Business | `BudgetBand` (wage cap and transfer budget per season, from tier and reputation), `Reputation`, `Audience` |
| Personality (dynamic posture) | `Contend`, `Balanced`, `Rebuild` or `Conserve`; switches on results, finance and roster age (Rival simulation §4) |
| Behaviour tendencies | `TransferAggression`, `YouthFocus`, `SellingTendency`, `ScoutingRegion` (0–100 each; flavour that changes actions) |

The current `Strength`, `Analysis` and `Posture` become derived:
- Strength = the team power of the rival roster;
- Analysis = the rival coach's analysis;
- posture stays.

## 6. Candidate formulas

All coefficients are starting points to tune with `tools/Headless --measure`. None is canon.

### 6.1 Role rating

`RoleRating(p, r) = Σ_a w[r][a] · A_a(p) · RoleProficiency(p, r) / 100`

| Role | Mechanics | EarlyGame | Teamfight | Macro | ChampionPool |
|---|---|---|---|---|---|
| Top | .25 | .25 | .25 | .10 | .15 |
| Jungle | .15 | .25 | .20 | .30 | .10 |
| Mid | .30 | .20 | .20 | .15 | .15 |
| Bot | .30 | .20 | .30 | .05 | .15 |
| Support | .10 | .20 | .25 | .35 | .10 |

When real per-game statistics are permitted (Oracle's Elixir), role weights can be checked against what separates winning
from losing players in each role. Until then they are authored.

### 6.2 Match (extends the current resolver; same structure, new inputs)

```
base       = Σ_5 RoleRating × Readiness/100 × (1 + Form/100) / 5
           + familiarity_term (0..+4) + leadership_term (−3..0)
style      = (our style − their style) × plan emphasis      (replaces part of matchup; bounded ±3)
meta       = (team ChampionPool mean − 60)/20 × meta-shift size   (replaces Adaptability; bounded ±3)
composure  = (mean Composure − 50)/25 when Importance = 2          (finals and playoffs; ±2)
noise      = keyed uniform(−spread, +spread), spread = Variance + 4·posture − (mean Consistency − 50)/10
p_game     = logistic((base − opponent + prep + style + meta + composure + noise) / s)
p_series   = Bo1/Bo3/Bo5 from p_game                                   (Competition spec §11)
```

Notes:
- **Why logistic.** It replaces the linear 1.2-points-per-point line with a curve that can be fitted to real team Elo
  gaps (§7.2). The bounded terms keep any single attribute from dominating.
- **Consistency becomes a real choice.** With spread on the logit scale, a weaker team gains from a wide spread and a
  stronger team from a narrow one. This is the decision the current symmetric noise never creates; it is a design
  claim to verify with measurement.
- **Rivals use the same formula** with their own roster, so rival-vs-rival games (wave 2) need no second resolver.

### 6.3 Value and wage

```
MarketValue = f(RoleRating, age factor, Potential gap (estimated, not true), Popularity, years left)
WageDemand  = base_wage(MarketValue) × (1 + Ambition/200) × org factor(reputation; bigger orgs pay less premium)
              × role-promise factor
```

This replaces `E²/5`, so every priced attribute is one the player can weigh.

### 6.4 Development

The per-season change in role rating has an age curve:
- below 20: +2..+6 while under Potential;
- 20–24: +0..+3;
- 25–27: −1..+1;
- 28 and over: −1..−4.

It is scaled by Professionalism, coach Development, playing time and (wave 3) facilities, and keyed by rng-v1 with
`season:person`.

Calibrate the curve with the **age distribution by tier** in real rosters: survival in top tiers is the proxy for when
pros peak and leave. It is not a measured ability curve.

### 6.5 Popularity → company (the company-first link)

- Match audience gain is multiplied by `1 + (mean Popularity of the five − 50)/200`.
- Sponsor fair payment gets `+(star bonus)` for each player with Popularity ≥ 80, capped.
- Popularity: wins +, big-match wins ++, bench −, scandal −−. A scandal is rolled from low Professionalism and pays out
  through the existing reputation effects and sponsor termination clauses.

### 6.6 Rival AI (wave 4, minimal)

At each window (contract ends, transfer window, after a poor run), a rival evaluates its weakest role:
- buys the best affordable free agent or makes a bid under its budget band;
- releases the over-budget veterans;
- changes coach after two poor seasons.

Its posture sets the evaluation: Contend weights role rating now; Rebuild weights age and potential; Conserve weights
wage. Every action writes `World.Moves` with real origins and destinations, so Transfer News reports rival moves with
no change.

## 7. Data strategy

### 7.1 What the free real data can and cannot supply

| Available (PandaScore Free, observed 2026-10-08) | Not available |
|---|---|
| 136 leagues; tournaments with **tier s/a/b/c/d**, region, dates, **expected rosters** (dated roster evidence) | Player-level game stats or lineups (Historical plan, or Oracle's Elixir CSV) |
| Teams (name, acronym, country, current players) | League or region on teams; academy or substitute flags |
| Players (handle, partial real name, nationality, role top/jun/mid/adc/sup, current team) | Birthday and age on `/players` (only embedded, licence unclear); licensed images |
| 3,828 2026 matches (finished or cancelled): opponents, scores, Bo format, game winners and lengths | Contracts, salaries, finances, transfers with dates |

### 7.2 Use real data as calibration (recommended default)

Compute aggregate parameters only, and store them as a versioned **calibration report**. Content generation then reads
the report.

| Parameter | From | Feeds |
|---|---|---|
| Team Elo per 2026 team → distribution and the gaps between tiers | Match results | Rival strength by tier; logistic scale `s` in §6.2 |
| Roster size distribution (5 / 6 / 7 / 8+) | Expected rosters | MinimumRoster, MaxRoster, rival roster size |
| Roster turnover between consecutive tournaments of a team | Expected rosters over time | Rival transfer frequency per window |
| Nationality mix per region | Players, rosters | NPC nationality and name pools per region |
| Age distribution by role and tier | Embedded birthdays, *if licence allows* | NPC age generation; the development curve |
| Bo1 / Bo3 / Bo5 shares and games per series by stage | Matches | Season format (wave 2) |
| Game length distribution | Games | Flavour, time between matches |
| Role-specific stat spreads (with Oracle's Elixir, if permitted) | Per-game stats | Attribute variance by role; the role weights in §6.1 |

**Why aggregates.**
- No person is represented and no real rating is invented.
- The game stays fictional, so publicity and trademark questions do not arise.
- Provider terms still apply to derived data and must be checked before shipping (PD-D03).

### 7.3 Generated NPC world (pipeline)

1. **Config + calibration report**: regions, tiers, organizations per tier, quotas.
2. **Organizations**:
   - generated name, short name, region, tier;
   - budget band from tier;
   - posture and tendencies.
3. **People pool** (durable people, one owner each):
   - nationality by region;
   - name and handle from per-nationality pools;
   - age from the curve;
   - role by quota;
   - abilities from a **role template + archetype**, with authored covariance (for example a lane bully: high EarlyGame
     and Mechanics, lower Macro), then shifted by tier;
   - Potential from age;
   - personality;
   - Popularity from rating, tier and age.
4. **Allocation**: two-pass, per Entity Data v0.1. Each organization gets 6–8 players and a coach within its budget; the
   rest go to the free-agent and coach markets.
5. **Lifecycle**:
   - a youth intake each season (16–17 year olds, low rating, high potential);
   - retirement by age and rating;
   - this keeps scarcity bounded and gives generations, replacing the endless reroll of `Market.Refresh`.

Determinism and validation reuse rng-v1, the v0.2 key grammar and the two-pass integrity checks.

### 7.4 Optional real-name pack (later, rights-gated)

- A separate content pack built offline from staged real records.
- Real people would get **estimated** attributes: their layer is "estimated", and unknown stays unknown.
- It is never shipped by default.
- Before any work: provider terms (PandaScore, Oracle's Elixir), publisher trademark rules and players' publicity rights.
  This document is not legal advice.
- A user-import model, where players load their own pack, is the common genre practice.

### 7.5 Tooling fixes needed before the crawl (from the discovery report)

- `sync_pandascore.py`:
  - add filter, sort and range parameters;
  - check the collected count against `X-Total`;
  - crawl ascending within a fixed window, to stop pagination drift.
- Store `expected_roster` as **dated roster rows**. The UPSERT keeps only the latest payload.
- Add `elo_2026.py` (team ratings from results) and `calibration_report.py` (the aggregates in §7.2).
- Output a versioned JSON with source hashes and no personal identities.

## 8. Fix list

| Priority | Fix | Why |
|---|---|---|
| P0 | Decide the role model and discipline (PD-D01/D02) | Blocks every role formula and every data join |
| P0 | Either give Adaptability, Consistency, Coach.Analysis and Rival.Adaptability a consumer (§6) or stop generating and pricing them | Decorative stats undermine trust and balance |
| P0 | Narrow the market range or give it a reason (age and potential) | The market dominates the starting roster for no stated reason |
| P1 | Add age, potential and development; a durable people pool with youth intake and retirement | The core long-term loop; required by wave 3 |
| P1 | Price wages from value (§6.3) | Every attribute becomes a trade-off |
| P1 | Popularity → audience and sponsors (§6.5) | The company-first identity |
| P1 | Logistic resolver with a familiarity term; rival-vs-rival resolution | League table (wave 2) and integration cost |
| P1 | Calibration crawl and report (§7.2, §7.5) | Gives distributions real support |
| P2 | Explicit rival rosters, budget bands and posture AI (§6.6) | Rival transfers, poaching, a world that moves (wave 4) |
| P2 | Scouting knowledge per attribute; Information with faucets and sinks | Information as a resource |
| P3 | Optional real-name pack | Only after rights approval |

## 9. Decisions requested

| ID | Question | Recommendation |
|---|---|---|
| PD-D01 / PD-D02 | Discipline and role model | Adopt a LoL-shaped five-role model (Top, Jungle, Mid, Bot, Support) with generic in-game naming. It joins the real data directly, and the roles stay fictional enough for DEC-020. |
| PD-D02 | Attribute set | §5.1: 5 technical, 3 mental, 3 hidden personality, plus career, condition and Popularity. Each enters only with its consumer. |
| PD-D03 | Real data use | Aggregate calibration only (§7.2), after a provider-terms check. No real people in the default game. |
| PD-D04 | NPC population | A durable pool with youth intake and retirement (§7.3), sized per tier from the calibration report |
| ED-01 / ED-05 | Rival fidelity | Explicit rosters from the shared pool; a business layer as bands, not ledgers |
| — | Save and rules versioning | New attributes arrive with a new rules version, as in v2 → v3. Old saves are rejected or migrated through the legacy mapping in §5.1. |

## 10. Suggested next steps

1. The Director or the user decides §9 PD-D01/D02 and PD-D03.
2. Tooling: the §7.5 fixes, then the bounded 2026 crawl (about 48 requests) and the first calibration report.
3. Rules v3 wave 2, extended:
   - role model;
   - logistic resolver with familiarity;
   - rival-vs-rival results and the league table;
   - value-based wages;
   - the measurement gates added for the new terms.
4. Wave 3: people lifecycle (age, potential, development, youth, retirement), form and morale, Popularity, scouting.
5. Wave 4: explicit rival rosters and posture AI, feeding the existing Transfer News journeys.
