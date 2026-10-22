# Player rating model — research method (v2, 2023–2026)

**Status: RESEARCH MODEL.** Not an official Riot, PandaScore or Leaguepedia rating, not approved game balance, and not
an input to `content/`, saves or the game's generator. Model version `lol-research-rating 2.0.0`; window: games from
2023-01-01 and before 2026-10-14 (UTC). Each coefficient is labelled **FITTED** (estimated from the data) or **AUTHORED**
(chosen by hand and documented here). Results and coverage: [PLAYER_RATING_COVERAGE.md](PLAYER_RATING_COVERAGE.md).
This document holds method, aggregate results and synthetic examples only; no player-level data.

Code: `rating_model.py` (pure functions), `rate_players.py` (pipeline and outputs), `validate_ratings.py`,
`build_performance.py`, `reconcile_identities.py`, `fetch_leaguepedia_reference.py`, `ratings_report.py`. Tests:
`tests/test_player_ratings.py`.

**What changed in v2 (user rules of 2026-10-14).** v1 placed the Overall on the linear strength scale, so the strongest
player stood several points clear of everyone (89 against 85). v2 keeps that linear Level as the strength measure but
presents the Overall on fixed, described bands, adds the record of 2023–2026 (achievements, consistency), and adds age:

| Band | Overall | Meaning (user definition) |
|---|---|---|
| ELITE | 90–93 | truly excellent over the period: top strength, major achievements, steady play |
| VERY_GOOD | 83–89 | not elite; stable, few mistakes, a good influence on the team |
| AVERAGE | 74–82 | average; inconsistent, mistakes or anonymous stretches, occasionally outstanding |
| INEXPERIENCED | 55–73 | inexperienced, many mistakes, young or with few results |
| AMATEUR_ACADEMY | 30–54 | amateur leagues, young talents still in academies |

No band has an age condition. Potential exists up to age 25 (the projected Overall at 25); after 25 the Overall no
longer grows through training (career maturity); from 29 a player is a veteran whose Overall falls slowly.

## 1. What the model produces

For every PandaScore catalogue player, exactly one record:

| Field | Meaning |
|---|---|
| `mechanics`, `early_game`, `teamfight`, `macro`, `champion_pool` | Rules v4 skills on 1–99 |
| `overall_rating`, `band` | primary-role Overall on the banded scale (recomputed from the five rounded skills with the game's role weights) and its band |
| `potential`, `potential_basis` | age ≤ 25: projected Overall at 25 (≥ the Overall, ≤ 93); from 25: equal to the Overall; unknown age or PRIOR_ONLY: empty |
| `age`, `age_source`, `age_conflict`, `career_stage` | age at the cutoff; DEVELOPING ≤ 25, PEAK 26–28, VETERAN ≥ 29, UNKNOWN |
| `level_linear`, `raw_quality`, `level_interval_80` | strength on the linear scale (70 = tier-a mean), R = Level + achievement + consistency points, approximate 80% interval of the Level |
| `achievement_score`, `achievement_points`, `titles`, `tier_a_titles`, `international_finals`, `international_titles`, `elite_evidence` | the 2023–2026 record |
| `consistency_z`, `consistency_points` | steadiness of game-to-game performance |
| `rating_prev_year`, `aging_floor_applied` | the banded level one year earlier; whether the veterans' floor raised the Overall |
| `rating_evidence_class`, `confidence`, `confidence_label` | `MEASURED_ESTIMATE` / `LIMITED_EVIDENCE` / `PRIOR_ONLY`; 0–1 and HIGH / MEDIUM / LOW / NONE |
| `games_2023` … `games_2026`, `effective_games`, `last_verified_game` | verified individual games |
| `form_score`, `form_trend`, `form_status`, `form_confidence` | recent performance against the player's own history |
| `index_2023` … `index_2026`, `index_last10/20`, `index_30d/90d/180d`, `change_2025_2026` | performance windows |
| `unavailable_feature_flags`, `source_references`, `prior_basis`, `provisional_note` | evidence and missing data |

## 2. Data

- **Per-game player statistics:** Leaguepedia scoreboards (`ScoreboardPlayers`, `ScoreboardGames`, `Tournaments`,
  `PlayerRedirects`), January 2023 to the cutoff. Fields: kills, deaths, assists, gold, CS, damage to champions, vision
  score, champion, role, side, result, team kills and gold; game length, patch, league, region. Oracle's Elixir (richer:
  gold/XP/CS differences at 10 and 15 minutes, wards, first blood) is supported by the same pipeline but was not
  downloadable on 2026-10-14 (Google Drive quota); the EarlyGame skill therefore has no evidence in this run.
- **Achievements:** Leaguepedia `TournamentResults` (placement of every team in every event, 2023 to the cutoff).
- **Ages:** PandaScore stage-roster birthdays, else the Leaguepedia `Players.Birthdate` of the player's high-confidence
  Leaguepedia identity (two sources more than a year apart: flagged, PandaScore kept). Personal data, used locally for
  age only; the licence of the PandaScore birthdays is unconfirmed.
- **Identity, teams, competitions:** PandaScore catalogue (9,915 players), 2023–2026 stage rosters. A game's
  competition tier is the PandaScore tier of the stage on which both teams were registered when it was played.
- **Unit of evidence:** a *verified game* is a complete, primary (deduplicated) game whose seat belongs to a
  `HIGH_CONFIDENCE_MATCH` identity. Registration alone is never treated as participation.

## 3. Seat features and context normalization

**Derived features** (per player and game; a missing input leaves the feature missing):

| Feature | Definition |
|---|---|
| `kda` | ln((K + A + 1) / (D + 1)) |
| `deaths_pm`, `apm` | deaths, assists per minute |
| `kp` | (K + A) / team kills |
| `dmg_share`, `gold_share` | share of the side's damage / gold |
| `dpm`, `dmg_per_gold` | damage per minute; damage per gold |
| `cspm`, `vspm` | CS, vision score per minute |
| `lane_gold_pm`, `lane_cs_pm`, `lane_dmg_pm` | end-of-game difference to the opposing player in the same role, per minute |
| `gd10/15`, `xpd10/15`, `csd10/15`, `ka15`, `deaths15`, `first_blood` | timeline values (Oracle's Elixir only) |
| `wpm`, `wcpm`, `cwpm` | wards placed, cleared, control wards per minute (Oracle's Elixir only) |

**Context z-score.** Each feature is compared with peers in the same **role × game result × competition × year**.
Separating wins from losses keeps a team's results out of individual numbers (a winning carry's damage is compared with
other winning carries). Small groups are pooled toward the role × result × year norm with 100 pseudo-seats (AUTHORED).
Then the shared shift of each **role × patch** across all leagues (shrunk with 200 pseudo-seats, AUTHORED) is removed,
so a patch that inflates kills everywhere is not credited to whoever played on it. Values are winsorized at ±3
(AUTHORED): one extreme game cannot dominate.

## 4. Skill indices (formula contract)

**Seat score** for a skill = Σ coefficient × z-score. Coefficients are **AUTHORED**, role-specific, and sum to 1.0 per
role and skill; a leading minus means fewer is better. A feature the source lacks contributes **0 (the peer average)**;
the other coefficients are **not** re-weighted to fill the gap.

| Skill | Top / Mid | Bot | Jungle | Support |
|---|---|---|---|---|
| Mechanics | dmg/gold .25, kda .20, lane CS .20, lane dmg .15, lane gold .10, dpm .10 | same as Top/Mid | kda .30, dmg/gold .25, lane gold .15, cspm .15, dpm .15 | kda .40, −deaths/min .30, lane gold .15, dmg/gold .15 |
| EarlyGame | gd15 .25, xpd15 .20, csd15 .20 (Mid .15), gd10 .15, csd10 .10, ka15 .05 (Mid .10), −deaths15 .05 | gd15 .25, csd15 .25, xpd15 .15, gd10 .15, csd10 .10, ka15 .05, −deaths15 .05 | ka15 .25, xpd15 .20, gd15 .20, csd15 .10, first blood .10, −deaths15 .15 | ka15 .25, −deaths15 .25, xpd15 .20, gd15 .15, first blood .15 |
| Teamfight | dmg share .30, kp .25, dpm .20, −deaths/min .15, kda .10 | dmg share .35, dpm .25, kp .20, −deaths/min .20 | kp .40, assists/min .20, −deaths/min .25, dmg share .15 | kp .40, assists/min .30, −deaths/min .20, dmg share .10 |
| Macro | vspm .35, wards .10, wards cleared .15, control wards .10, gold share .15, cspm .15 | vspm .25, wards .10, cleared .15, control .10, gold share .25, cspm .15 | vspm .35, wards .15, cleared .20, control .10, cspm .10, gold share .10 | vspm .45, wards .20, cleared .20, control .15 |

Support damage carries little weight in Teamfight on purpose: a support's kill participation, assists and survival carry
that skill, so low damage is not read as low teamfight ability.

**Player index (empirical Bayes).** For player *p* in role *r* and skill *A*: the time-decayed mean *m* of seat scores
(weight 0.5^(age / half-life), half-life 365 days, FITTED by validation) and the effective sample n_eff = (Σw)²/Σw².
Per role and skill, the signal variance τ² and the per-game noise σ² are **FITTED** by method of moments over players
with n_eff ≥ 5: Var(m) = τ² + mean(σ²/n_eff). The index is *m* × τ²/(τ² + σ²/n_eff), divided by τ: it is in signal-SD
units and shrinks toward the peer average (0) as the sample shrinks. Synthetic example (τ² = 0.04, σ² = 1, every game
+0.3): 1 game → 0.06; 5 games → 0.25; 20 games → 0.67; 80 games → 1.14 signal SD. Three excellent games stay close to
average; a long strong record does not.

**ChampionPool.** Not the raw champion count. Per role, the expected effective number of champions (exp of the usage
entropy) given the number of games is **FITTED** (quadratic in log games). The raw signal is
0.6 × (log effective champions − expected) / residual SD (capped ±3) + 0.4 × 2 × (performance on champions other than
the two most played, minus performance on those two, in role SDs, capped ±2 and weighted by off-main games / (off-main
games + 10)). It is standardized per role and shrunk by games / (games + 20) (AUTHORED pseudo-games). Forty games on
three champions scores below the role's expectation on diversity, but a player who performs as well off their main
champions recovers part of it; ten champions in ten games is shrunk heavily.

## 5. Level: how strong the player is

Two models were built and compared on later games (section 9).

**Baseline B0 (transparent).** A player's contribution = an offset for their PandaScore tier + *g* × their role-weighted
box-score index. Seven tier and side parameters and one slope, fitted by logistic regression on game results.

**Candidate C1 (selected).** A game-level logistic model: logit P(blue wins) = h + Σ_blue β − Σ_red β, each lineup the
sum of its five players' contributions β (h = side advantage). Priors (MAP, L-BFGS, time-decay-weighted likelihood):

- β_p ~ N(Σ_c share_pc · λ_c + γ · J_p, τ²): centred on the player's competition level (shares of domestic games;
  international events excluded) plus γ times J_p, the player's role-weighted skill index;
- λ_c ~ N(t_tier(c), 0.25²): each competition near its PandaScore tier (s, a, b, c, d, unknown); t ~ N(0, 2²).

γ, τ and the half-life are **FITTED** by temporal validation (section 9). Leagues with no international games still
connect through players who moved between them; a competition whose players played fewer than 30 verified games in
other competitions is marked `competition_link = WEAK`.

International events are recognized from the data, not from a region label: a competition is international when most
of its games oppose lineups whose players' home competition (the one they played most that year) differs — Worlds, MSI,
First Stand, EMEA Masters, the Asian Games (national teams), invitationals.

## 6. Linear Level, banded Overall, skills

- **Linear Level** = 70 + 5 × k × (β − β̄_anchor). The anchor (AUTHORED) is 70 = the game's tier-a ability-band median
  (rules v4 generation 60/70/78/88), placed at the games-weighted mean contribution of players with ≥ 10 games in
  PandaScore tier-a domestic stages in 2026. *k* = points per logit = content `Balance.LogitScale` / 100, read at run time
  (7.0 in the current working tree; 9.0 at HEAD). Reading: a difference of *k* points in the five players' average Level is
  one logit of single-game win odds, the same reading as the game's match model. It is kept in `level_linear`.
- **Raw quality R** = Level + achievement points (0 to +4) + consistency points (−1.5 to +1.5):
  - *Achievements* (AUTHORED points by event class and place, place 1 / 2 / 3–4 / 5–8): Worlds or MSI 10 / 7 / 5 / 3;
    other top international events (First Stand, Esports World Cup and the like) 6 / 4 / 2.5 / 1; tier-a league 5 / 3 /
    1.5 / 0.5; tier b 2.5 / 1.5 / 0.75 / 0.25; tier c 1.2 / 0.7 / 0.3; tier d and unknown 0.5 / 0.25. Only the final stage
    of a split or event awards them in full (the playoffs page that ended last; for an event whose standings sit on a
    parent page, that page); regular seasons, earlier playoff stages and qualifiers award 30% and never a title. A
    placement counts for a player who played at least 20% of that team's games in the event (full credit from 50%).
    Points decay with a 2-year half-life. Achievement points = 4 × (1 − exp(−score / 15)), saturating.
  - *Consistency*: the decay-weighted SD of the player's game-to-game performance (role-SD units), shrunk toward the
    population with 20 pseudo-games and turned into a z-score (z > 0 steadier). Points = z, capped ±1.5. It is not biased
    by role (mean z by role −0.11 to +0.04 in the trial run).
- **Banded Overall level** = T(R), a fixed monotone piecewise-linear transform (AUTHORED knots, R → Overall):
  10 → 30, 47 → 54.5, 61 → 73.5, 72 → 82.5, 80 → 89.5, 92 → 93; below 10 → 30, above 92 → 93. In R terms a typical
  tier-c league regular sits near 47 (the amateur/academy edge), the bottom of tier-a regulars near 61, a tier-a regular
  one SD above the mean near 80. The top is compressed (12 R points above the elite edge add 3.5 Overall points), so the
  best players form a group instead of one runaway value. No quota fills a band.
- **Gates and caps.** ELITE needs `elite_evidence`: an international final (1st or 2nd at Worlds, MSI, First Stand, EWC
  or a similar top event) or at least two tier-a domestic titles in 2023–2026, as a regular (≥ 50% of the team's games),
  and consistency z ≥ −1.0 (only markedly erratic players fail it). Without it the Overall stops at 89: the skills are
  lowered one point at a time, highest weight first, until the role formula gives ≤ 89. A PRIOR_ONLY placeholder stops at
  73. The veterans' floor (section 8b) may raise the level, never above a cap.
- **Skills** = banded level + 6 × (index − role-weighted mean index) (6 points per signal SD, AUTHORED, tested in
  sensitivity). A skill without evidence — on average less than 30% of its coefficient mass observed (AUTHORED; e.g.
  EarlyGame without timeline data; a support's Macro from vision score alone is 45% and counts) — takes the
  role-weighted mean of the available indices, so it equals the Level and shows no deviation either way. Skills are rounded half-to-even and clamped to 1–99.
- **Overall** = round-half-even(Σ weight_bp × skill / 10,000), exactly `Ratings.InRole` at proficiency 100, recomputed
  from the five rounded skills. Role weights (basis points), read from `content/fixture.json` at run time and identical
  in the working tree and at HEAD on 2026-10-14:

| Role | Mechanics | EarlyGame | Teamfight | Macro | ChampionPool |
|---|---:|---:|---:|---:|---:|
| Top | 2500 | 2500 | 2500 | 1000 | 1500 |
| Jungle | 1500 | 2500 | 2000 | 3000 | 1000 |
| Mid | 3000 | 2000 | 2000 | 1500 | 1500 |
| Bot | 3000 | 2000 | 3000 | 500 | 1500 |
| Support | 1000 | 2000 | 2500 | 3500 | 1000 |

- **Roles.** The primary role is the role with most verified 2026 games (else all games). A player with games in several
  roles gets a rating per role: the Level moved by 5 × k × γ × (that role's index − the player's overall index).
  Actual roles come from match records only.

**Worked examples (synthetic; computed with the model's functions).**

| Case | Level | Achievement pts | Consistency pts | R | T(R) | Gate | Skills (M/E/T/Ma/P) | Overall |
|---|---:|---:|---:|---:|---:|---|---|---:|
| Mid, international final and titles | 84.0 | 3.80 | 0.30 | 88.1 | 91.86 | passes | 94 / 92 / 91 / 89 / 91 | **92** ELITE |
| Same strength, no major title | 84.0 | 2.20 | 0.30 | 86.5 | 91.40 | fails → ≤ 89 | 91 / 89 / 88 / 87 / 88 | **89** VERY_GOOD |
| Average tier-a support | 68.0 | 0.50 | −0.40 | 68.1 | 79.31 | — | 76 / 79 / 79 / 81 / 76 | **79** AVERAGE |
| Academy jungler | 46.0 | 0.13 | 0.00 | 46.1 | 53.92 | — | 56 / 54 / 54 / 53 / 54 | **54** AMATEUR_ACADEMY |

Profiles: mid Mechanics +1.0, Teamfight +0.5, Macro +0.2, Pool +0.4; support Mechanics −0.2, Teamfight +0.3, Macro +0.6,
Pool −0.3; jungler Mechanics +0.4, Macro −0.2, Pool +0.1; EarlyGame unavailable in all (equal to the level). The second
row shows the gate: the same strength without an international final or two tier-a titles stops at 89, through its skills
(the role formula on 91 / 89 / 88 / 87 / 88 gives 88.9 → 89).

## 7. Evidence classes and placeholders

| Class | Rule |
|---|---|
| `MEASURED_ESTIMATE` | ≥ 40 verified games (2023–2026) and ≥ 20 effective (decay-weighted) games |
| `LIMITED_EVIDENCE` | 1 or more verified games, below that; the value leans on the priors |
| `PRIOR_ONLY` | no verified game (none found, or the identity is not a high-confidence match) |

A **PRIOR_ONLY** record is a deterministic placeholder: T of the 25th percentile raw quality R (AUTHORED, conservative) of
rated players whose primary tier equals the tier of the player's most recent PandaScore stage registration, or the
declared global prior (the same percentile over all rated players) when no tier is known; at most 73 (no verified game:
not yet an established competitor). All five skills equal that value, the confidence is 0, potential is empty, and
`provisional_note` states that individual ability is not established. No random value, name hash or invented difference
is used. Unique Overalls are not enforced.

## 8. Form

Form compares recent performance with the player's **own** earlier performance; it does not enter the Overall (no
double counting). Seat performance = the role-weighted seat score in role-SD units. Recent = the last 10 games within 60
days of the cutoff (at least 5); earlier = all older games (at least 5). `form_score` = round(10 × (recent mean −
earlier mean) × n/(n + 10)), clamped to −10..+10 (the game's Form scale); `form_trend` IMPROVING ≥ +3, DECLINING ≤ −3,
otherwise STABLE. A player without games in the last 60 days gets `form_status = NO_RECENT_EVIDENCE` and trend UNKNOWN:
inactivity is never read as bad form. Synthetic example: 30 earlier games at 0.0, 10 recent at +0.6 → +3, IMPROVING.

## 8b. Age: maturity, veterans and potential (user decisions 2026-10-14)

- **Age** at the cutoff from the birthdate (section 2). `career_stage`: DEVELOPING ≤ 25, PEAK 26–28, VETERAN ≥ 29.
- **Maturity.** The rule "after 25 the Overall does not grow" applies to potential and development: from 25 the potential
  equals the Overall. The measured Overall itself still follows real performance at any age (user choice).
- **Veterans.** From 29 the Overall may fall at most 2 points a year (3 from 31) below the rating one year earlier: the
  whole pipeline is rerun as of the cutoff minus 365 days, and the veteran's banded level is raised to (last year's banded
  level − allowed drop) when it would fall further. A real decline therefore shows over several years instead of at once.
- **Development curve (FITTED).** Each calendar season 2023–2026 is rated on its own (C1 without the box prior, anchored on
  that season's tier-a players). For players with ≥ 30 games in two consecutive seasons and a known age: change = age
  effect + b × (Level − 70) + error. The b term absorbs regression to the mean, and all ages share the seasons' anchoring,
  so growth is read relative to ages 25–27, where by the rule development has ended: growth(a) = effect(a) − effect(25–27),
  made non-increasing in age (isotonic) and ≥ 0, none from 25. The spread of true growth is the residual variance of
  under-25s minus that of over-25s (the same measurement process without development).
- **Potential** (age ≤ 25, measured or limited evidence) = T(R + Σ growth over the remaining years + 0.8416 × σ_growth ×
  √years), at least the Overall and at most 93. If σ_growth is 0 the 80th percentile equals the expected path, and the
  report says so. Synthetic examples with the fitted curve of the trial run (+5.6 at 17, +3.1 at 20, +1.2 at 23–24):
  age 19, R 60 → Overall 72 now, potential 84–85; age 22, R 75 → 85 now, 89; age 24, R 80 → 89.5, 89.8.

## 9. Validation

`validate_ratings.py`, run 2026-10-15 on the 2023–2026 data (35 minutes). No future leakage: each model is fitted only on
games (and box scores) before its cutoff and scored on later results from the lineups actually fielded.

**Selection (inner window).** Fit on 45,448 games before 2026-03-01, score 4,383 games from 2026-03-01 to 2026-05-31.
Grid γ ∈ {0.1, 0.2, 0.3}, τ ∈ {0.2, 0.3, 0.4}, half-life ∈ {365, 730, 1095} days (27 fits, all converged). Best log loss
0.6155 at **γ 0.2, τ 0.3, half-life 365**; the same γ and τ gave 0.6177 at 730 days and 0.6190 at 1095 days. Giving the
older seasons more weight makes later games harder to predict, so the strength part follows the last year or two, while
the 2023–2026 record enters through achievements (2-year half-life). Team Elo was tuned on the same window (K 16 best).

**Comparison (outer window, used once).** Fit on 49,831 games before 2026-06-01; score 5,469 games from 2026-06-01 to
2026-10-13. Lower log loss and Brier are better.

| Model | Log loss | Brier | Accuracy | AUC |
|---|---:|---:|---:|---:|
| **C1 candidate (selected)** | **0.6169** | **0.2149** | **65.1%** | **0.711** |
| C1 without the box-score prior (γ = 0) | 0.6204 | 0.2163 | 64.6% | 0.707 |
| Team Elo, updated after every test game (reference; sees results the others do not) | 0.6506 | 0.2295 | 61.5% | 0.661 |
| B0 baseline (tier offsets + box index) | 0.6578 | 0.2332 | 59.9% | 0.639 |
| Team Elo frozen at the cutoff | 0.6721 | 0.2393 | 58.3% | 0.621 |
| Side advantage only | 0.6901 | 0.2485 | 54.1% | 0.500 |

By subset (log loss; C1 / B0 / frozen Elo): international games 0.574 / 0.634 / 0.693 (1,239 games); cross-competition
games 0.593 / 0.635 / 0.661 (2,333); tier-a domestic 0.639 / 0.672 / 0.648 (764); lower tiers 0.615 / 0.656 / 0.677
(4,289). The v1 model (2025–2026 data only) scored 0.6195 on the same window.

**Calibration (outer, C1).** Close in the middle (0.55 → 0.54, 0.74 → 0.74), mildly over-confident toward the ends (0.16 →
0.20, 0.85 → 0.80).

**Checks on the final model** (all games in the window):

| Check | Result |
|---|---|
| Coverage | 9,915 records = 9,915 unique PandaScore ids; 0 PRIOR_ONLY with games; 0 rated records with zero games |
| Formula | 0 records where the Overall differs from the role formula on the five rounded skills; 0 values outside the bounds; Overall range 30–93 |
| Bands and gates | 0 ELITE without elite evidence; 0 PRIOR_ONLY above 73; every ELITE player has an international final; 2 players held at 89 by the gate |
| Age rules | 0 potentials below the Overall or above 93; 0 differences between potential and Overall from age 25; 36 veterans held by the yearly floor; 0 veterans below last year's rating by more than the allowed drop |
| Role balance | mean measured Overall by primary role 56.5–56.6; Top 100 overall: Top 21, Jungle 21, Mid 18, Bot 19, Support 21 |
| Small samples | players with ≤ 3 games sit 2.1 Level points (mean absolute) from their competition's mean, players with ≥ 100 games 5.9 |
| One exceptional game | winsorized at ±3 SD and shrunk; tested (`test_one_exceptional_game_is_shrunk`) |
| Missing features | contribute the peer average; the unavailable EarlyGame equals the level; tested |
| Team results | correlation of Level with the 2026 team win rate 0.20 (1,719 measured players); 87% of Level variance lies between teams (league level plus team strength); mean within-team SD 5.1 points |
| Bootstrap (20 game resamples, box indices fixed) | mean Level SD of listed players 2.2–2.6 points (max 4.3); share of each list's top 10 staying in its top 10: Support 0.83, Mid 0.81, Bot 0.80, Jungle 0.79, Top 0.73, Overall 0.71 |
| Sensitivity (Spearman of measured Overalls; Top 100 overlap; ELITE count, base 33) | γ ×0.5 / ×1.5: 0.996 / 0.996, 96–99, 35 / 32; τ ×0.75 / ×1.25: 0.996 / 0.997, 93–99, 31 / 36; σ_c ×2: 0.996, 97–100, 33; achievement points ×0.5 / ×1.5: 1.000 / 1.000, 97–100, 22 / 44; no consistency points: 0.998, 97–100, 33; half-life ×0.5 / ×1.5: 0.979 / 0.996, 89–99, 29 / 39 |
| Reproducibility | two runs give byte-identical outputs (10 files), also through `rate_players.py` directly |
| Deduplication | 13 duplicated Leaguepedia games removed (same day, same ten champions); 0 duplicated seats |

The size of the ELITE group is most sensitive to the achievement points (22 at half, 44 at one and a half times): that
weight is an AUTHORED design choice and the place to adjust if the elite group should be smaller or larger.

Scale facts: side advantage 0.101 logit; tier levels in Level points a 69.7, b 51.9, c 45.3, d 35.3, unknown 26.6;
competition levels for example LCK 76.2, LPL 72.7, LEC 68.8, LCS 65.7, LCP 63.8, LFL 62.0, CBLOL 60.6, LCK Challengers
League 60.3, LDL 52.8. Development curve (growth per year relative to ages 25–27, R points): 17: 5.6, 18: 4.0, 19: 3.8,
20: 3.1, 21: 2.6, 22: 2.5, 23–24: 1.2; 2,609 season-to-season transitions with known age. The spread of true growth is
not measurable (residual SD 9.19 under 25 against 9.41 over 25), so the 80th-percentile potential equals the expected
path.

## 10. Ranking eligibility

A Top 100 list (per role, and overall) includes a player only when: identity `HIGH_CONFIDENCE_MATCH` to a PandaScore
player; class MEASURED or LIMITED; ≥ 20 verified 2026 games in that role; last game within 180 days of the cutoff;
Level SD ≤ 0.8 × the prior SD of the Level (5 × k × τ; the data must have narrowed it by a fifth). Order: Overall in the
role, then raw quality R, then 2026 games, then provider id. The overall
list ranks primary-role Overalls and is **EXPERIMENTAL** across regions; the local report also lists the strongest
players within each competition, a comparison that does not depend on cross-region levels.

## 11. Known limits

- **No timeline data in this run.** EarlyGame equals the Level for everyone; it becomes informative once the Oracle's
  Elixir files are imported (same pipeline, no code change).
- **Box scores are weak proxies** for decision-making, shotcalling and mechanics as such. Macro rests on vision and
  resource share; Mechanics on efficiency and lane differences. None of the skills measures reaction speed or click
  precision.
- **Team results.** Teammates share results; individual separation comes from the box-score prior (γ) and lineup
  changes. Players of dominant teams sit high together.
- **Cross-region levels** rest on international games and player movement; they are EXPERIMENTAL, and leagues marked
  WEAK carry more uncertainty than their interval shows. The interval comes from the likelihood curvature and ignores
  correlation between teammates; bootstrap spreads are reported separately.
- **Banded scale.** T is AUTHORED: the knots encode the user's band definitions on the strength scale; moving a knot
  moves everyone in that band. The gates use titles and finals, which reward players on strong teams; strength and
  consistency still decide the order inside a band.
- **Achievements** count events Leaguepedia records with placements; an event whose standings are missing awards
  nothing. Regular seasons and earlier stages count at 30%.
- **Ages** exist only where a birthdate is published; unknown age means no potential and no veterans' floor.
- **Identity.** Only high-confidence identities count. A player held for review keeps a PRIOR_ONLY record until the
  evidence resolves; their games still inform their teammates' and opponents' ratings under the source identity.

## 12. Reproduce

See the README section "Player performance and research ratings, 2023-2026". `data/ratings/rating_manifest.json` records
the model version, configuration and its hash, the content file hash, every source snapshot hash and the cutoff.
