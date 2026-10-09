# Player Rating Formulas v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

Dependencies: [source/authority index](README.md), [contract and exact field names](PLAYER_DATA_CONTRACT_V1.md), [attribute catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md), [validation](VALIDATION_CALIBRATION_V1.md), [existing arithmetic/RNG](../../../src/ManagementGame.Domain/Determinism.cs).

## Interpretation and arithmetic contract

**PROPOSAL — all formulas/parameters in this document.** These estimate contextual performance proxies for game design. They are not official skill rankings, measurements of mechanics, personality, intelligence, or causal player contributions. No current Competition coefficient is changed. No real person is rated.

Inputs are validated normalized observations and a frozen benchmark artifact. Outputs are per-feature values, attribute summaries, evidence confidence and nullable integer ratings. A complete audit retains inputs, valid sets, exclusions, context, coefficients and each transformation. Unknown is not zero and never silently becomes 50.

Candidate offline arithmetic profile: decimal values rounded to six places with ties-to-even after each division, logarithm/exponential lookup, multiplication and addition, in explicit sorted order (played_at, match_id, game_id, observation_id; all IDs ordinal). Means sum in that order. Keep stored intermediates; display percentages only after calculation. Final rating uses ties-to-even to an integer, then clamps to [1,99]. Examples print analytical values where repeating fractions are abbreviated; applying the six-place profile gives the same final integers. Parameters 50, 15, [1,99] and all thresholds require calibration.

For reproducibility, a future model artifact must supply a versioned six-decimal decay lookup for every integer age 0–364 (and entropy lookup for supported counts), generated with a documented high-precision method and verified golden vectors. Runtime consumes frozen integer capabilities; it does not fit a model or evaluate transcendental functions. Those lookup artifacts are not supplied by this specification, so production model release remains blocked. Integer sampling continues to use existing rng-v1. JSON output uses invariant culture; no local timezone, dictionary enumeration or wall-clock defaults.

## Feature definitions

Let T = duration_seconds / 60 minutes. All divisions below require known, compatible inputs. Only observations within the supported completed-game window are admitted. Missing input yields null with a reason. No infinity or artificial denominator epsilon.

| Feature ID | Formula using contract fields | Unit / range / zero behavior |
|---|---|---|
| kda | (kills + assists) / max(1, deaths) | Nonnegative ratio; intentional deathless convention, not true division by zero. Preserve deaths and deathless flag in audit. Diagnostic only. |
| kp | (kills + assists) / team_kills | Fraction [0,1]. team_kills=0 and numerator=0 → unknown/no opportunities. Positive numerator with zero team kills → invalid row. |
| cspm | (cs_lane + cs_jungle) / T | CS/min ≥0; missing either component → unknown unless a verified equivalent total is mapped upstream. |
| gpm | gold_total / T | Acquired game gold/min ≥0. Starting/passive gold included consistently. |
| dpm | damage_to_champions / T | Champion damage/min ≥0; never all-target damage. |
| damage_share | damage_to_champions / team_damage_to_champions | Fraction [0,1]; team total zero → unknown; individual above total → invalid. |
| gold_share | gold_total / team_gold_total | Fraction [0,1]; zero team total → unknown. |
| damage_per_gold | damage_to_champions / gold_total | Damage per acquired gold ≥0; zero gold → unknown, not a huge score. |
| death_rate | deaths / T | Deaths/min ≥0. Zero deaths → observed zero. Lower direction is used cautiously in Combat. |
| gd15 | gold_at_15 - opponent_gold_at_15 | Signed gold; exact 900-second paired role snapshot. |
| xpd15 | xp_at_15 - opponent_xp_at_15 | Signed XP; same snapshot and paired role. |
| csd15 | cs_at_15 - opponent_cs_at_15 | Signed CS; lane+jungle definition must match on both sides. |
| vpm | vision_score / T | Score/min ≥0; not wards/min. Provider definition/version must be pinned. |
| champion_entropy | H = -Σ p_c ln(p_c), p_c = count_c / n_games | Nats, 0..ln(m), m distinct observed champions; omit zero-count terms. n=0 → unknown; one champion →0. |
| effective_champions | exp(H) | Effective count, 1..m; equal use of two champions →2. Diagnostic breadth, not mastery. |
| normalized_diversity | H / ln(m) | Fraction [0,1]; m=1 →0 by convention. Highly sample-dependent; never a capability rating on its own. |

Early features are not constructed for jungle/support in v0.1, even if telemetry contains paired position values. Lane swaps and ambiguous role assignments are quarantined for Laning; their other features may remain usable if role classification is defensible. Matches with substitutes produce separate actual-player rows, never redistributed totals. Percentages use fractions internally. Result is recorded but not a positive skill feature.

Raw data remain immutable. Physical/schema contradictions are rejected; plausible extreme games are retained in audit and their standardized signals clipped, not silently deleted. Provider metrics with different semantics require separate feature/model versions, never just a common column name.

## Benchmarks and context

**PROPOSAL:** primary cells are role × competition_group × season × patch_era. Each feature uses population mean mu and population standard deviation sigma of the eligible training cell. Minimum cell: 200 player-game rows, 20 distinct players and four teams, all preceding evaluation cutoff. The same individual's repeated rows do not constitute 20 players. Freeze the row set and coefficients. During validation use temporal holdouts and grouped series/player folds; do not let evaluation rows fit their own reference distribution.

Oriented standardized value z = clip(direction × (x - mu) / sigma, -3, 3), with direction +1 except death_rate -1. sigma=0 → unavailable benchmark, not infinite z. A sparse cell may use only a predeclared, independently reviewed pooled adjacent patch_era within the same role/competition group; record fallback and quality. Otherwise the feature is unknown. Never pool Support and Bot to fill a gap.

Percentile is a useful explanation view: 100 × (number below + 0.5 × number equal) / N against the same frozen cell, 0..100, tie-aware. It loses distance information and is not used as a second power bonus. Z-scores retain distance but are sensitive to skew/outliers; clipping limits one game's influence. Neither establishes normality or absolute skill.

| Context | Candidate treatment | Limitation |
|---|---|---|
| Role | Separate benchmarks and weights below | Role labels and actual assignments must be reliable. |
| Tier/region | Context-specific ratings by default; optional calibrated additive tier_offset_z applied once after shrinkage | Offset defaults to 0 and comparability_scope=within_context. Zero does not establish cross-tier equality. Cross-tier recruitment requires approved connected calibration or explicit uncertain priors before capability initialization. |
| Season/patch | Explicit patch_era membership and frozen prior training window | New patch is not automatically a known era; do not invent coefficients. |
| Team strength | Stratify diagnostics by pregame team strength; no causal subtraction in v0.1 | Teammates, resources and opponents confound every proxy. Win rate is not an individual bonus. |
| Champion/side/duration | Audit strata; champion-specific residual model deferred | Champion choice, composition and game length remain confounders. No hidden champion-effect correction is claimed. |

Any future context regression must use only pregame-known covariates, held-out validation, coefficient/version records and shrinkage for sparse cells. Postgame victory, future standings or same-game gold cannot be used as pregame strength. No hand-picked tier multipliers without evidence.

## Role-specific weights and missingness

All weights below apply to standardized features, not raw values. Fixed order is the column order shown. Attribute scores are not summed into a universal overall rating.

| Actual role | Laning: gd15 / xpd15 / csd15 | Combat: dpm / kp / oriented death_rate | Vision | Consistency input |
|---|---|---|---|---|
| top | .50 / .30 / .20 | .45 / .35 / .20 | 1.00 vpm in top benchmark | Role's Combat series score |
| jungle | Not applicable | .25 / .55 / .20 | 1.00 vpm in jungle benchmark | Role's Combat series score |
| mid | .50 / .30 / .20 | .45 / .35 / .20 | 1.00 vpm in mid benchmark | Role's Combat series score |
| bot | .50 / .30 / .20 | .45 / .35 / .20 | 1.00 vpm in bot benchmark | Role's Combat series score |
| support | Not applicable | .00 / .75 / .25 | 1.00 vpm in support benchmark | Role's Combat series score |

Excluded from direct power: kda (survival/participation duplication), cspm/gpm (resource allocation), damage_share/gold_share/damage_per_gold (team/resource confounding and DPM overlap), champion diversity (breadth ≠ proficiency), wins. DPM is excluded for Support to avoid rewarding damage picks by default. Combat remains imperfect for tanks/engage players even with role benchmarks; that is a release risk, not a solved correction.

Per game and attribute: let C_i = sum of original positive weights of present valid features. Laning and Combat require C_i ≥0.50 and at least two valid positive-weight features; Support therefore requires kp and death_rate. Vision requires vpm and C_i=1.00. If admitted, s_i = Σ present(w_f z_f) / C_i. Do not fill missing inputs with zero. If inadmissible, attribute score is unknown. Laning for jungle/support is not_applicable, excluded from both denominators and quality penalties.

For release, at least 50% of that player's otherwise eligible role-game rows must yield the attribute; report this game-admission fraction separately from C_i. Below it, return unknown even if the surviving games are strong. Let A = admitted_games / eligible_games. Missingness may be systematic; renormalization is only a candidate fallback, not proof of unbiased measurement. Run complete-case and missingness-stratified comparisons before adoption.

## Recency, independent samples and shrinkage

Persistent-skill window: 365 calendar-day age buckets including cutoff day; half-life 180 days. age_days = floor((as_of - played_at)/86400 seconds); future rows invalid. For valid ages 0..364 use weight a_i = 2^(-age_days/180). All evaluations supply an explicit as_of. Most recent admitted row older than 180 days → unknown for fresh campaign initialization, while historical artifact remains inspectable.

To avoid counting a best-of-five as five independent series, first compute s_j = weighted mean of admitted game scores within each match_id j using a_i. C_j is the same weighted mean of C_i. W_j = maximum a_i among those admitted games. Then:

~~~text
observed_z = sum(W_j * s_j) / sum(W_j)
n_eff = (sum(W_j))^2 / sum(W_j^2)       # effective series, not games
K = 20                                # candidate prior strength, in series
r = n_eff / (n_eff + K)
adjusted_z = r * observed_z + tier_offset_z
rating = clamp(round_even(50 + 15 * adjusted_z), 1, 99)
~~~

No sample → unknown. Core mean ratings require at least three admitted series and n_eff ≥3. A tiny accepted sample is strongly shrunk; an unaccepted sample remains unknown, not a displayed prior 50. Prior mean is zero within the declared benchmark. It is not a belief that all regions have equal absolute skill. K must be tuned on held-out stability/error, not chosen per famous player. Shrink once at the attribute level; do not also shrink every feature and then apply r again. Age weighting changes the mean and effective diversity of evidence; equal-aged old series can retain n_eff, so freshness is also reported explicitly rather than pretending Kish n_eff measures age.

Candidate evidence confidence, a data-adequacy score, not posterior certainty:

~~~text
coverage = A * sum(W_j * C_j) / sum(W_j)
freshness = sum(W_j) / n_series
q_context = 0.75 exact supported cell; 0.50 approved pooled cell
confidence = r * coverage * freshness * q_context
~~~

Use the minimum applicable q_context when features use different eligible cells. Unavailable context blocks the affected feature; it is not a numerical q=0 rating. Bands: low <0.35; moderate [0.35,0.70); high ≥0.70. These bands are candidate labels. Even high evidence adequacy does not remove proxy or causal limitations. NPC authored truth has no measurement confidence; scouting confidence is separate for both origins.

Recent form diagnostic: 28-day window (age buckets0..27), half-life 14 days; compare recent composite mean with persistent mean, cap diagnostic delta to [-1,1] z. Minimum three effective series. The overlapping windows are not independent evidence: do not apply both as two strength boosts. Dynamic form is deferred from runtime until a single explicit interaction is approved. Patch adaptation diagnostic compares supported pre/post windows (at least ten series each) under separate context baselines; patch, roster and champion changes confound it. It does not generate an Adaptability rating in v0.1.

## Consistency model

Consistency means variation of contextual Combat contribution, not courage, reliability as an employee, or a second mean-skill score. Use the same admitted series and fixed feature mask for a given release. If masks differ, intersect their available positive-weight feature sets across all admitted games, apply the original role weights renormalized over that intersection, and recompute every score. If this common mask fails Combat's two-feature/coverage gate, return unknown; do not select a favorable subset of games. At least 12 admitted series and n_eff ≥12; at least two represented patch_eras or documented single-era scope. Do not compare raw variance from different role composites.

Let v_raw = Σ W_j(s_j - observed_z)^2 / (ΣW_j - ΣW_j^2/ΣW_j). Nonpositive denominator → unknown. Divide by a positive frozen role/context reference variance v_ref to obtain v = v_raw / v_ref. To fit v_ref, use the arithmetic mean of v_raw across at least20 distinct eligible training players from at least four teams in the declared role/context cell, each with at least12 effective series and the identical feature mask. Each player contributes one variance, independent of how many extra games they played; apply the same recency/arithmetic rules with the training cutoff. Do not include evaluation rows or substitute a feature sigma. A cell failing these requirements, or v_ref≤0, blocks Consistency release; adjacent-era pooling follows the same explicit benchmark-fallback policy. Clip v to [0,4] in the measurement artifact and flag clipping.

~~~text
v_adjusted = r * v + (1-r) * 1
consistency_z = clip((1-v_adjusted) / 0.5, -3, 3)
consistency = clamp(round_even(50 + 15*consistency_z), 1, 99)
~~~

This shrinks variance toward role reference 1 once; do not shrink consistency_z again. Use the same confidence formula with Consistency's retained rows. A stable weak performer may have high Consistency and low Combat. In a later resolver Consistency should control one variance term, not also mean strength. Reducing variance can hurt an underdog's upset chance; higher is not universally preferable.

## Worked numerical examples

All people, game values, benchmark parameters and summaries here are synthetic. References W01–W09 are arithmetic review cases, not empirical calibration evidence.

### W01 — complete feature row and full sample

30-minute bot game: kills=6, deaths=2, assists=8, cs_lane=250, cs_jungle=20, gold_total=15000, damage_to_champions=24000, vision_score=30; team_kills=20, team_gold_total=60000, team_damage_to_champions=80000. At 15 minutes: gold 5800 vs 5400, XP 7200 vs 6900, CS 150 vs 140.

Results: kda=7; kp=0.7; cspm=9; gpm=500; dpm=800; damage_share=0.3; gold_share=0.25; damage_per_gold=1.6; death_rate=0.066667/min; gd15=400; xpd15=300; csd15=10; vpm=1.

Synthetic Laning benchmarks: mu=(100,0,4), sigma=(250,250,5). Each z=1.2; s=.5×1.2+.3×1.2+.2×1.2=1.2. A hand-constructed forty-series full-coverage aggregate with mean 1.2, all W=1, gives n_eff=40, r=2/3, adjusted_z≈0.8 and rating **62**. Forty distinct series are required; copying the same game forty times fails deduplication. The example JSON contains one excerpt row and explicitly separate aggregate inputs.

For that example artifact, synthetic Combat mean=1.5 → rating **65**, Vision mean=.6 → **56**, and reference-normalized Combat variance v=.64 → v_adjusted=.76, consistency_z=.48 → **57**. With coverage=1, freshness=1, q=.75, confidence=.5 for all four. The summary is illustrative, not derived from the single excerpt or actual provider data.

### W02 — small and empty sample

Four equal-weight series with mean z=1.2: r=4/24=1/6; adjusted_z=.2; rating **53**, confidence=.125 at full coverage/freshness and q=.75. One or two series fail the three-series gate; no sample also returns unknown. Never report 50 as a measured rating for them.

### W03 — missing early-game fields

GD@15 and XPD@15 absent, CSD@15 z=1.2 present: C_i=.2 and only one feature, so Laning is **unknown**, not 62 or zero. If only XPD@15 is absent, GD z=1.2 and CS z=.6 give coverage .7 and score (.5×1.2+.2×.6)/.7=1.028571. Forty equally weighted admitted series with this mask: adjusted_z≈.685714 → rating **60**; confidence=(2/3)×.7×1×.75=**.35**. If fewer than half eligible games retain a valid mask, release is unknown regardless of this calculation.

### W04 — same raw metric, different role/tier

Synthetic vpm=1.0: top benchmark mu=.4/sigma=.3 gives z=2; support benchmark mu=1.5/sigma=.5 gives z=-1. Forty full-weight series produce Vision **70** and **40**, respectively. A different competition's top benchmark mu=.6/sigma=.2 yields z=2 too; this does NOT prove equal cross-tier skill. Without connected calibration both numbers are within-context only. If a separately approved synthetic tier_offset_z=-.3 were used, adjusted_z=(2/3)×2-.3≈1.033333 gives an analytical pre-round value65.5 → **66** (also66 under the six-place arithmetic profile). This offset is an arithmetic illustration, not an accepted tier penalty.

### W05 — patch/window recalibration and campaign freeze

For an unchanged hypothetical gd15=400, old benchmark mu=100/sigma=250 gives z=1.2; new supported patch benchmark mu=250/sigma=250 gives z=.6. If all three Laning standardized signals shift likewise across the complete artifact, n_eff=40 gives **62 → 56** in the newly versioned artifact. This is a baseline change, not proof the player lost skill. Existing campaign retains **62**. An unsupported patch produces unknown rather than reusing the old coefficient silently.

### W06 — unequal recency

Two series at ages 0 and 180 days have W=(1,.5). For s=(1,0): observed_z=2/3, n_eff=2.25/1.25=1.8, freshness=.75. This correctly fails the minimum effective-series gate. Equal aging does not change Kish n_eff; the freshness factor and 180-day latest-evidence gate handle staleness separately.

### W07 — edge cases and rounding

Zero team kills and zero participation → kp unknown; positive participation → invalid. Zero champion team damage → share unknown. Zero deaths with six kills/eight assists → kda=14 and death_rate=0. duration_seconds=0 → invalid before any feature. Two equally used champions: H=ln(2), effective_champions=2, normalized_diversity=1. Values 62.5 and 63.5 round to **62** and **64**; unclamped values 104 and -5 clamp to **99** and **1** after rounding.

### W08 — correlation/overlap check

Raising raw DPM while gold/damage share also rise changes only the DPM term in core Combat; the three diagnostic resource ratios supply no additional power. Adding Vision later must not also reward identical ward effects through Map Impact. Mean Combat and Consistency affect different proposed resolver terms. These are design constraints, not evidence that remaining correlations are harmless.

### W09 — sensitivity illustration

At n_eff=40 and observed_z=1.2: K=10 yields rating **64**; K=20 yields **62**; K=40 yields **59**. Larger prior strength compresses ratings toward the benchmark. Calibration must report rank stability, uncertainty and recruitment decisions across K, half-life, clipping and mapping scale; choosing the most dramatic spread is not an acceptance criterion.
