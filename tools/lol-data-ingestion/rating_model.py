"""Research player-rating model, 2025-2026: pure functions (no database, no network). Used by rate_players.py.

This is a research model, not an official Riot, PandaScore or Leaguepedia rating and not approved game balance. Every
coefficient below is labelled AUTHORED (chosen and documented by hand) or FITTED (estimated from the data at run
time). See reports/PLAYER_RATING_MODEL.md for the full contract and worked examples.

Pipeline
  1. seat features      per player and game, from published box-score (and, when a source has them, timeline) fields;
  2. context z-scores   each feature against peers in the same role, competition, year and game result (wins and
                        losses separately, so a team's results do not leak into individual numbers), with small groups
                        pooled toward the role-year-result norm; winsorized at +-3;
  3. seat attribute scores  role-specific authored coefficients per attribute; a feature the source lacks contributes
                        0 (the peer average), never a re-weighted share of the others;
  4. player indices     time-decayed means, shrunk by empirical Bayes (fitted signal and noise variances per role and
                        attribute), expressed in signal-SD units; ChampionPool from champion diversity relative to what
                        the sample size predicts and from performance away from the two most played champions;
  5. level              candidate model C1: a game-level logistic model in which each lineup's strength is the sum of
                        its five players' contributions (side advantage included); each contribution has a prior centred
                        on its competition's level plus gamma times the player's role-weighted box-score index;
                        competition levels have a prior centred on their PandaScore tier's level. Fitted by penalized
                        maximum likelihood with time-decay weights;
  6. 1-99 scale         Level = ANCHOR_LEVEL + 5 * POINTS_PER_LOGIT * (contribution - mean contribution of tier-a
                        domestic players). Five players' average Level difference of POINTS_PER_LOGIT is one logit of
                        single-game win odds, the same reading as the game's LogitScale;
  7. attributes         attribute = Level + PROFILE_SCALE * (index - role-weighted mean index); the role-weighted mean of
                        the five attributes is therefore the Level, and the Overall is recomputed from the five rounded
                        attributes with the game's own role weights and round-half-even (Ratings.InRole).
"""
from collections import Counter, defaultdict
import math

import numpy as np
from scipy import optimize, sparse

MODEL_VERSION = "lol-research-rating 2.0.0"
ATTRIBUTES = ("Mechanics", "EarlyGame", "Teamfight", "Macro", "ChampionPool")
ROLES = ("top", "jng", "mid", "bot", "sup")
GAME_ROLE = {"top": "top", "jng": "jungle", "mid": "mid", "bot": "bot", "sup": "support"}

# AUTHORED: the game's tier-a ability band median (rules v4 generation: 60 / 70 / 78 / 88 at 25 / 50 / 23 / 2%).
ANCHOR_LEVEL = 70.0
# AUTHORED: points of attribute deviation per signal-SD of an attribute index (the profile's spread around the Level).
PROFILE_SCALE = 6.0
# AUTHORED: z-score winsorization and the pseudo-count pulling small context groups toward the role-year-result norm.
Z_CAP = 3.0
GROUP_PRIOR_SEATS = 100.0
PATCH_PRIOR_SEATS = 200.0
# FITTED (temporal validation on 2023-2026 data, 2026-10-14; 365, 730 and 1095 days tried): half-life of the
# time-decay weight for ability.
HALF_LIFE_DAYS = 365.0
# AUTHORED: pseudo-games for ChampionPool reliability; prior sd of competition levels around their tier (logits).
POOL_PRIOR_GAMES = 20.0
COMPETITION_SD = 0.25
TIER_SD = 2.0

# AUTHORED seat-feature coefficients per role and attribute. Keys are feature names (seat_features); a leading "-"
# negates the z-score (fewer deaths is better). ChampionPool is computed separately (champion_pool_index).
COEFFICIENTS = {
    "Mechanics": {
        "top": {"dmg_per_gold": .25, "kda": .20, "lane_cs_pm": .20, "lane_dmg_pm": .15, "lane_gold_pm": .10, "dpm": .10},
        "mid": {"dmg_per_gold": .25, "kda": .20, "lane_cs_pm": .20, "lane_dmg_pm": .15, "lane_gold_pm": .10, "dpm": .10},
        "bot": {"dmg_per_gold": .25, "kda": .20, "lane_cs_pm": .20, "lane_dmg_pm": .15, "lane_gold_pm": .10, "dpm": .10},
        "jng": {"kda": .30, "dmg_per_gold": .25, "lane_gold_pm": .15, "cspm": .15, "dpm": .15},
        "sup": {"kda": .40, "-deaths_pm": .30, "lane_gold_pm": .15, "dmg_per_gold": .15},
    },
    "EarlyGame": {
        "top": {"gd15": .25, "xpd15": .20, "csd15": .20, "gd10": .15, "csd10": .10, "ka15": .05, "-deaths15": .05},
        "mid": {"gd15": .25, "xpd15": .20, "csd15": .15, "gd10": .15, "csd10": .10, "ka15": .10, "-deaths15": .05},
        "bot": {"gd15": .25, "xpd15": .15, "csd15": .25, "gd10": .15, "csd10": .10, "ka15": .05, "-deaths15": .05},
        "jng": {"ka15": .25, "xpd15": .20, "gd15": .20, "csd15": .10, "first_blood": .10, "-deaths15": .15},
        "sup": {"ka15": .25, "-deaths15": .25, "xpd15": .20, "gd15": .15, "first_blood": .15},
    },
    "Teamfight": {
        "top": {"dmg_share": .30, "kp": .25, "dpm": .20, "-deaths_pm": .15, "kda": .10},
        "mid": {"dmg_share": .30, "kp": .25, "dpm": .20, "-deaths_pm": .15, "kda": .10},
        "bot": {"dmg_share": .35, "dpm": .25, "kp": .20, "-deaths_pm": .20},
        "jng": {"kp": .40, "apm": .20, "-deaths_pm": .25, "dmg_share": .15},
        "sup": {"kp": .40, "apm": .30, "-deaths_pm": .20, "dmg_share": .10},
    },
    "Macro": {
        "top": {"vspm": .35, "wpm": .10, "wcpm": .15, "cwpm": .10, "gold_share": .15, "cspm": .15},
        "mid": {"vspm": .35, "wpm": .10, "wcpm": .15, "cwpm": .10, "gold_share": .15, "cspm": .15},
        "bot": {"vspm": .25, "wpm": .10, "wcpm": .15, "cwpm": .10, "gold_share": .25, "cspm": .15},
        "jng": {"vspm": .35, "wpm": .15, "wcpm": .20, "cwpm": .10, "cspm": .10, "gold_share": .10},
        "sup": {"vspm": .45, "wpm": .20, "wcpm": .20, "cwpm": .15},
    },
}
SEAT_ATTRIBUTES = ("Mechanics", "EarlyGame", "Teamfight", "Macro")
# AUTHORED: a skill has evidence for a player when, on average over their games, at least this share of its coefficient
# mass was observed (a Leaguepedia support's Macro: vision score only, 0.45); below it the skill shows no deviation.
AVAILABILITY_MIN = 0.3
TIMELINE_FEATURES = {"gd10", "gd15", "xpd10", "xpd15", "csd10", "csd15", "ka15", "deaths15", "first_blood"}
WARD_FEATURES = {"wpm", "wcpm", "cwpm"}


def role_weights(content):
    """{research role: (5 weights in basis points)} from content RoleWeights, in ATTRIBUTES order."""
    by_game_role = {w["Role"]: w["Weights"] for w in content["RoleWeights"]}
    return {role: tuple(by_game_role[GAME_ROLE[role]]) for role in ROLES}


def round_half_even(value):
    """Python's round() is round-half-even, the game's MidpointRounding.ToEven."""
    return int(round(value))


def overall_from(attributes, weights_bp):
    """Ratings.InRole at proficiency 100: round-half-even of sum(weight_bp * skill) / 10,000."""
    return round_half_even(sum(w * a for w, a in zip(weights_bp, attributes)) / 10_000)


# ---------- 1. seat features ----------

def ratio(a, b):
    return None if a is None or b in (None, 0) else a / b


def seat_features(s):
    """Feature dict for one seat (a dict with the pf_player_games statistics and game_length_min). None = missing."""
    length = s.get("game_length_min")
    if not length or length <= 0:
        return {}
    k, d, a = s.get("kills"), s.get("deaths"), s.get("assists")
    gold, cs, dmg = s.get("gold"), s.get("cs"), s.get("damage")
    f = {
        "kda": math.log((k + a + 1) / (d + 1)) if None not in (k, d, a) else None,
        "deaths_pm": d / length if d is not None else None,
        "apm": a / length if a is not None else None,
        "kp": ratio(None if None in (k, a) else k + a, s.get("team_kills")),
        "dmg_share": ratio(dmg, s.get("team_damage")),
        "dpm": dmg / length if dmg is not None else None,
        "dmg_per_gold": ratio(dmg, gold),
        "gold_share": ratio(gold, s.get("team_gold")),
        "cspm": cs / length if cs is not None else None,
        "vspm": s["vision_score"] / length if s.get("vision_score") is not None else None,
        "lane_gold_pm": (gold - s["opp_gold"]) / length if None not in (gold, s.get("opp_gold")) else None,
        "lane_cs_pm": (cs - s["opp_cs"]) / length if None not in (cs, s.get("opp_cs")) else None,
        "lane_dmg_pm": (dmg - s["opp_damage"]) / length if None not in (dmg, s.get("opp_damage")) else None,
        "wpm": s["wards_placed"] / length if s.get("wards_placed") is not None else None,
        "wcpm": s["wards_killed"] / length if s.get("wards_killed") is not None else None,
        "cwpm": s["control_wards"] / length if s.get("control_wards") is not None else None,
    }
    for key in ("gd10", "gd15", "xpd10", "xpd15", "csd10", "csd15", "first_blood", "deaths15"):
        f[key] = s.get(key)
    f["ka15"] = None if None in (s.get("kills15"), s.get("assists15")) else s["kills15"] + s["assists15"]
    return f


FEATURES = ("kda", "deaths_pm", "apm", "kp", "dmg_share", "dpm", "dmg_per_gold", "gold_share", "cspm", "vspm",
            "lane_gold_pm", "lane_cs_pm", "lane_dmg_pm", "wpm", "wcpm", "cwpm", "gd10", "gd15", "xpd10", "xpd15",
            "csd10", "csd15", "first_blood", "deaths15", "ka15")


def feature_tuple(s):
    """seat_features as a tuple in FEATURES order (None = missing): the compact form kept on each seat."""
    f = seat_features(s)
    return tuple(f.get(name) for name in FEATURES)


# ---------- 2. context z-scores ----------

class Normalizer:
    """Means and SDs per (role, win, competition, year), pooled toward (role, win, year) with GROUP_PRIOR_SEATS.
    Features are tuples in FEATURES order (feature_tuple)."""

    def __init__(self, seats, features):
        self.stats = {}
        groups, broad = defaultdict(lambda: defaultdict(list)), defaultdict(lambda: defaultdict(list))
        for s, f in zip(seats, features):
            g, b = (s["role"], s["win"], s["competition"], s["year"]), (s["role"], s["win"], s["year"])
            for name, value in zip(FEATURES, f):
                if value is not None:
                    groups[g][name].append(value)
                    broad[b][name].append(value)
        self.broad = {b: {n: (float(np.mean(v)), float(np.var(v)), len(v)) for n, v in d.items()} for b, d in broad.items()}
        for g, d in groups.items():
            out = {}
            for n, values in d.items():
                mean_b, var_b, _ = self.broad[(g[0], g[1], g[3])][n]
                count = len(values)
                mean_g, var_g = float(np.mean(values)), float(np.var(values))
                weight = count / (count + GROUP_PRIOR_SEATS)
                mean = weight * mean_g + (1 - weight) * mean_b
                var = weight * var_g + (1 - weight) * var_b
                out[n] = (mean, math.sqrt(var) if var > 0 else None)
            self.stats[g] = out
        # Patch: the mean context z of each (role, patch) over every league, shrunk with PATCH_PRIOR_SEATS, is removed,
        # so a meta shift shared by all leagues on one patch is not credited to whoever happened to play on it.
        sums = defaultdict(lambda: defaultdict(lambda: [0.0, 0]))
        for s, f in zip(seats, features):
            for name, value in self._z(s, f).items():
                cell = sums[(s["role"], s.get("patch"))][name]
                cell[0] += value
                cell[1] += 1
        self.patch_shift = {key: {n: total / (count + PATCH_PRIOR_SEATS) for n, (total, count) in d.items()}
                            for key, d in sums.items()}

    def _z(self, s, f):
        g = (s["role"], s["win"], s["competition"], s["year"])
        stats = self.stats.get(g, {})
        out = {}
        for name, value in zip(FEATURES, f):
            if value is None or name not in stats or stats[name][1] is None:
                continue
            mean, sd = stats[name]
            out[name] = max(-Z_CAP, min(Z_CAP, (value - mean) / sd))
        return out

    def z(self, s, f):
        shift = self.patch_shift.get((s["role"], s.get("patch")), {})
        return {n: max(-Z_CAP, min(Z_CAP, v - shift.get(n, 0.0))) for n, v in self._z(s, f).items()}


# ---------- 3. seat attribute scores ----------

def seat_scores(role, z):
    """{attribute: score} for the seat-level attributes. A missing feature contributes 0 (the peer average)."""
    scores = {}
    for attribute in SEAT_ATTRIBUTES:
        total = 0.0
        for key, weight in COEFFICIENTS[attribute][role].items():
            name, sign = (key[1:], -1.0) if key.startswith("-") else (key, 1.0)
            total += weight * sign * z.get(name, 0.0)
        scores[attribute] = total
    return scores


def attribute_availability(role, z_keys):
    """Share of each attribute's coefficient mass whose feature was present."""
    out = {}
    for attribute in SEAT_ATTRIBUTES:
        coefficients = COEFFICIENTS[attribute][role]
        mass = sum(coefficients.values())
        present = sum(w for k, w in coefficients.items() if k.lstrip("-") in z_keys)
        out[attribute] = present / mass
    return out


# ---------- 4. empirical Bayes indices ----------

def decay(age_days, half_life=HALF_LIFE_DAYS):
    return 0.5 ** (max(age_days, 0.0) / half_life)


def weighted_stats(values, weights):
    w = np.asarray(weights, dtype=float)
    v = np.asarray(values, dtype=float)
    total = w.sum()
    mean = float((w * v).sum() / total)
    n_eff = float(total ** 2 / (w ** 2).sum())
    var = float((w * (v - mean) ** 2).sum() / total) * (n_eff / (n_eff - 1)) if n_eff > 1 else None
    return mean, n_eff, var


def eb_fit(groups, min_eff=5.0):
    """groups: {key: (values, weights)} for one role and attribute. FITTED: (signal variance, noise variance per seat).
    Method of moments: var(player means) = signal + mean(noise / n_eff)."""
    rows = [weighted_stats(v, w) for v, w in groups.values() if len(v) >= 2]
    usable = [(m, n, var) for m, n, var in rows if n >= min_eff and var is not None]
    if len(usable) < 10:
        return 0.0, 1.0
    noise = float(np.mean([var for _, _, var in usable]))
    means = np.array([m for m, _, _ in usable])
    signal = float(np.var(means) - np.mean([noise / n for _, n, _ in usable]))
    return max(signal, 1e-6), max(noise, 1e-6)


def eb_index(values, weights, signal, noise):
    """(shrunk index in signal-SD units, n_eff, reliability)."""
    if not values or signal <= 1e-6:
        return 0.0, 0.0, 0.0
    mean, n_eff, _ = weighted_stats(values, weights)
    reliability = signal / (signal + noise / n_eff)
    return mean * reliability / math.sqrt(signal), n_eff, reliability


# ---------- ChampionPool ----------

def effective_champions(counts):
    total = sum(counts.values())
    if total == 0:
        return 0.0
    entropy = -sum(c / total * math.log(c / total) for c in counts.values() if c)
    return math.exp(entropy)


def fit_diversity_curve(samples):
    """samples: [(games, effective champions)]. FITTED: quadratic of log(effective) on log(games), per role."""
    x = np.array([math.log(n) for n, _ in samples])
    y = np.array([math.log(max(e, 1.0)) for _, e in samples])
    if len(samples) < 20:
        return (0.0, 1.0, 0.0), 1.0
    coefficients = np.polyfit(x, y, 2)
    residual_sd = float(np.std(y - np.polyval(coefficients, x))) or 1.0
    return tuple(float(c) for c in coefficients), residual_sd


def champion_pool_raw(champion_counts, curve, residual_sd, off_main_gap, off_main_n):
    """Unshrunk ChampionPool signal: diversity above what the sample size predicts (in residual SDs), plus how much of
    the player's performance holds away from the two most played champions (gap in seat-score units, capped)."""
    games = sum(champion_counts.values())
    if games == 0:
        return 0.0
    expected = float(np.polyval(curve, math.log(games)))
    diversity = (math.log(max(effective_champions(champion_counts), 1.0)) - expected) / residual_sd
    breadth = max(-2.0, min(2.0, off_main_gap)) * off_main_n / (off_main_n + 10.0) if off_main_n else 0.0
    return 0.6 * max(-3.0, min(3.0, diversity)) + 0.4 * breadth * 2.0


# ---------- 5. level models ----------

class LineupData:
    """Games as a sparse lineup matrix: +1 for blue players, -1 for red players."""

    def __init__(self, games, player_index):
        rows, cols, vals = [], [], []
        self.y = np.zeros(len(games))
        self.w = np.zeros(len(games))
        for i, g in enumerate(games):
            for p in g["blue"]:
                rows.append(i), cols.append(player_index[p]), vals.append(1.0)
            for p in g["red"]:
                rows.append(i), cols.append(player_index[p]), vals.append(-1.0)
            self.y[i] = g["blue_win"]
            self.w[i] = g["weight"]
        self.X = sparse.csr_matrix((vals, (rows, cols)), shape=(len(games), len(player_index)))


def fit_contributions(data, prior_box, competition_share, competition_tier, tiers, gamma, tau,
                      sigma_c=COMPETITION_SD, sigma_t=TIER_SD, start=None):
    """Candidate model C1 (MAP). Parameters: player contributions beta, competition levels lam, tier levels t, side h.
    beta_p ~ N(sum_c share_pc lam_c + gamma * box_p, tau^2); lam_c ~ N(t_tier(c), sigma_c^2); t ~ N(0, sigma_t^2).
    Returns dict with beta, lam, tiers, h and the per-player likelihood curvature (for approximate SDs)."""
    n_players = data.X.shape[1]
    S = competition_share                      # players x competitions (sparse, rows sum to 1 or 0)
    n_comp = S.shape[1]
    T = competition_tier                       # competitions x tiers (one-hot)
    n_tier = T.shape[1]
    box = np.asarray(prior_box, dtype=float)

    def unpack(theta):
        return theta[:n_players], theta[n_players:n_players + n_comp], theta[n_players + n_comp:-1], theta[-1]

    def objective(theta):
        beta, lam, tier, h = unpack(theta)
        eta = data.X @ beta + h
        p = 1.0 / (1.0 + np.exp(-eta))
        nll = -np.sum(data.w * (data.y * np.log(p + 1e-12) + (1 - data.y) * np.log(1 - p + 1e-12)))
        r = data.w * (p - data.y)
        g_beta = data.X.T @ r
        g_h = r.sum()
        prior_mean = S @ lam + gamma * box
        d_beta = beta - prior_mean
        lam_mean = T @ tier
        d_lam = lam - lam_mean
        nll += 0.5 * np.sum(d_beta ** 2) / tau ** 2 + 0.5 * np.sum(d_lam ** 2) / sigma_c ** 2 + 0.5 * np.sum(tier ** 2) / sigma_t ** 2
        g_beta = g_beta + d_beta / tau ** 2
        g_lam = -(S.T @ d_beta) / tau ** 2 + d_lam / sigma_c ** 2
        g_tier = -(T.T @ d_lam) / sigma_c ** 2 + tier / sigma_t ** 2
        return nll, np.concatenate([g_beta, g_lam, g_tier, [g_h]])

    theta0 = np.zeros(n_players + n_comp + n_tier + 1) if start is None else start
    result = optimize.minimize(objective, theta0, jac=True, method="L-BFGS-B", options={"maxiter": 3000, "gtol": 1e-6})
    beta, lam, tier, h = unpack(result.x)
    p = 1.0 / (1.0 + np.exp(-(data.X @ beta + h)))
    curvature = np.asarray(abs(data.X).T @ (data.w * p * (1 - p))).ravel() + 1.0 / tau ** 2
    return {"beta": beta, "lam": lam, "tier": dict(zip(tiers, tier)), "h": float(h), "theta": result.x,
            "converged": bool(result.success), "iterations": int(result.nit), "beta_sd": 1.0 / np.sqrt(curvature)}


def predict(data, beta, h):
    return 1.0 / (1.0 + np.exp(-(data.X @ beta + h)))


def log_loss(y, p):
    p = np.clip(p, 1e-9, 1 - 1e-9)
    return float(-np.mean(y * np.log(p) + (1 - y) * np.log(1 - p)))


def brier(y, p):
    return float(np.mean((p - y) ** 2))


def team_elo(games, k=24.0, start=1500.0, home=0.0):
    """Reference model: chronological game-level Elo per source team. Returns (pre-game probabilities, final ratings)."""
    ratings, probabilities = defaultdict(lambda: start), []
    for g in games:
        a, b = ratings[g["blue_team"]], ratings[g["red_team"]]
        expected = 1.0 / (1.0 + 10 ** ((b - a - home) / 400.0))
        probabilities.append(expected)
        ratings[g["blue_team"]] = a + k * (g["blue_win"] - expected)
        ratings[g["red_team"]] = b - k * (g["blue_win"] - expected)
    return np.array(probabilities), dict(ratings)


# ---------- 6-7. scale and attributes ----------

def level(beta, anchor_beta, points_per_logit):
    return ANCHOR_LEVEL + 5.0 * points_per_logit * (beta - anchor_beta)


def attributes_from(level_value, indices, weights_bp, available):
    """Five 1..99 attributes around the Level. indices: {attribute: signal-SD index}; an attribute without evidence
    (available False) takes the role-weighted mean of the available ones, so it shows no deviation in either direction.
    Returns (rounded attributes, unrounded attributes)."""
    weights = {a: w / 10_000 for a, w in zip(ATTRIBUTES, weights_bp)}
    present = [a for a in ATTRIBUTES if available.get(a)]
    if present:
        mass = sum(weights[a] for a in present)
        filler = sum(weights[a] * indices[a] for a in present) / mass
    else:
        filler = 0.0
    filled = {a: (indices[a] if available.get(a) else filler) for a in ATTRIBUTES}
    centre = sum(weights[a] * filled[a] for a in ATTRIBUTES)
    raw = [level_value + PROFILE_SCALE * (filled[a] - centre) for a in ATTRIBUTES]
    rounded = [max(1, min(99, round_half_even(v))) for v in raw]
    return rounded, raw


def confidence_label(value):
    return "HIGH" if value >= 0.7 else "MEDIUM" if value >= 0.4 else "LOW" if value > 0 else "NONE"


# ---------- v2: the banded Overall scale ----------
#
# The linear Level (section 6) keeps the competitive reading (k points of five-player average = one logit). The
# presented Overall maps a raw quality R = Level + achievement points + consistency points through a fixed, monotone,
# piecewise-linear transform onto the user's bands (2026-10-14). Bands describe what a value means; no quota fills
# them. Knots are AUTHORED in R units, where 70 is the mean tier-a domestic player of the as-of year.

BANDS = (("ELITE", 90, 93), ("VERY_GOOD", 83, 89), ("AVERAGE", 74, 82), ("INEXPERIENCED", 55, 73),
         ("AMATEUR_ACADEMY", 30, 54))
BAND_KNOTS = ((10.0, 30.0), (47.0, 54.5), (61.0, 73.5), (72.0, 82.5), (80.0, 89.5), (92.0, 93.0))
SCALE_MIN, SCALE_MAX = 30.0, 93.0
ELITE_FLOOR = 90           # the first value of the ELITE band
CAP_WITHOUT_ELITE_EVIDENCE = 89
CAP_PRIOR_ONLY = 73        # no verified game: by definition not yet an established competitor
ACHIEVEMENT_MAX_POINTS = 4.0
ACHIEVEMENT_SCALE = 15.0   # achievement score at which 63% of the maximum is reached
CONSISTENCY_POINTS = 1.0   # points per consistency z
CONSISTENCY_MAX_POINTS = 1.5
CONSISTENCY_PRIOR_GAMES = 20.0
ELITE_MIN_CONSISTENCY_Z = -1.0  # excludes only markedly erratic players (about the bottom 6%)
GROWTH_END_AGE = 25        # potential exists up to this age; after it the Overall no longer grows with training
VETERAN_AGE = 29
POTENTIAL_QUANTILE_Z = 0.8416   # 80th percentile of the projected growth


def banded(r):
    """Monotone piecewise-linear R -> Overall scale, clamped to [SCALE_MIN, SCALE_MAX]."""
    knots = BAND_KNOTS
    if r <= knots[0][0]:
        return SCALE_MIN
    for (x0, y0), (x1, y1) in zip(knots, knots[1:]):
        if r <= x1:
            return y0 + (r - x0) * (y1 - y0) / (x1 - x0)
    return SCALE_MAX


def band_of(overall):
    for name, low, high in BANDS:
        if overall >= low:
            return name
    return BANDS[-1][0]


def achievement_points(score):
    """Saturating points for an achievement score (0 -> 0, large -> ACHIEVEMENT_MAX_POINTS)."""
    return ACHIEVEMENT_MAX_POINTS * (1.0 - math.exp(-max(0.0, score) / ACHIEVEMENT_SCALE))


def consistency_points(z):
    return max(-CONSISTENCY_MAX_POINTS, min(CONSISTENCY_MAX_POINTS, CONSISTENCY_POINTS * z))


def enforce_cap(attributes, weights_bp, cap):
    """Lower the highest-weighted attributes above the cap, one point at a time, until the role formula gives an
    Overall at or below the cap. Deterministic; changes nothing when the Overall already respects the cap."""
    attributes = list(attributes)
    order = sorted(range(len(attributes)), key=lambda i: (-weights_bp[i], i))
    guard = 0
    while overall_from(attributes, weights_bp) > cap and guard < 500:
        for i in order:
            if attributes[i] > cap or all(a <= cap for a in attributes):
                attributes[i] = max(1, attributes[i] - 1)
                break
        guard += 1
    return attributes


def aging_drop(age):
    """Largest yearly fall of the Overall for a veteran (user decision 2026-10-14): 2 at 29-30, 3 from 31; none before."""
    if age is None or age < VETERAN_AGE:
        return None
    return 2.0 if age <= 30 else 3.0


def career_stage(age):
    if age is None:
        return "UNKNOWN"
    return "DEVELOPING" if age <= GROWTH_END_AGE else "PEAK" if age < VETERAN_AGE else "VETERAN"


def isotonic_decreasing(values, weights):
    """Pool-adjacent-violators fit of a non-increasing sequence (weighted)."""
    blocks = [[v, w, 1] for v, w in zip(values, weights)]
    i = 0
    while i < len(blocks) - 1:
        if blocks[i][0] < blocks[i + 1][0]:
            v0, w0, n0 = blocks[i]
            v1, w1, n1 = blocks[i + 1]
            blocks[i] = [(v0 * w0 + v1 * w1) / (w0 + w1), w0 + w1, n0 + n1]
            del blocks[i + 1]
            i = max(i - 1, 0)
        else:
            i += 1
    out = []
    for v, _, n in blocks:
        out += [v] * n
    return out


def projected_growth(age, growth_by_age, sigma):
    """(expected growth, upper growth) in R points from `age` up to GROWTH_END_AGE: the sum of the fitted yearly growth
    for each remaining year plus the POTENTIAL_QUANTILE_Z upper deviation of the true-growth SD over those years."""
    if age is None or age >= GROWTH_END_AGE:
        return 0.0, 0.0
    years = range(int(age), GROWTH_END_AGE)
    expected = sum(max(0.0, growth_by_age.get(a, 0.0)) for a in years)
    return expected, expected + POTENTIAL_QUANTILE_Z * sigma * math.sqrt(len(years))


# ---------- form ----------

FORM_RECENT_GAMES = 10
FORM_RECENT_DAYS = 60
FORM_MIN_GAMES = 5
FORM_PRIOR_GAMES = 10.0
FORM_POINTS_PER_SD = 10.0  # AUTHORED: form points per seat-performance SD of recent-minus-earlier gap (game Form scale -10..10)


def form(performance, as_of_ordinal, points_per_sd=FORM_POINTS_PER_SD):
    """performance: [(date ordinal, seat performance in role SD units)] oldest first, for one player. Form is the recent
    mean minus the player's own earlier mean, shrunk by recent sample size, on -10..+10 (points_per_sd points per SD).
    Returns (score or None, trend, confidence, recent games)."""
    recent = [(d, v) for d, v in performance if as_of_ordinal - d <= FORM_RECENT_DAYS][-FORM_RECENT_GAMES:]
    if len(recent) < FORM_MIN_GAMES:
        return None, "UNKNOWN", 0.0, len(recent)
    earlier = performance[:len(performance) - len(recent)]
    if len(earlier) < FORM_MIN_GAMES:
        return None, "UNKNOWN", 0.0, len(recent)
    gap = float(np.mean([v for _, v in recent]) - np.mean([v for _, v in earlier]))
    shrink = len(recent) / (len(recent) + FORM_PRIOR_GAMES)
    score = int(max(-10, min(10, round_half_even(points_per_sd * gap * shrink))))
    trend = "IMPROVING" if score >= 3 else "DECLINING" if score <= -3 else "STABLE"
    confidence = shrink * min(1.0, len(earlier) / 20.0)
    return score, trend, round(confidence, 3), len(recent)
