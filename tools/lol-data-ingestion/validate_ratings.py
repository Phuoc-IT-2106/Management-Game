"""Validation of the research rating model (offline): temporal holdout, model comparison, hyperparameter selection,
bootstrap uncertainty, sensitivity, and data and formula checks. Writes data/ratings/validation.json (aggregates and
player ids only; no names) and prints a summary.

Temporal design (no future leakage): every model is fitted on games dated before a cutoff, using only those games'
box scores, and scored on later games' results from their lineups. Hyperparameters of the candidate are chosen on an
inner window (fit before INNER_CUTOFF, score INNER_CUTOFF..OUTER_CUTOFF); the outer window (OUTER_CUTOFF..as-of) is
used once, for the reported comparison.

  python validate_ratings.py                 # everything (about 20-30 minutes)
  python validate_ratings.py --skip-bootstrap
"""
import argparse
from collections import Counter, defaultdict
import copy
import gc
from datetime import date
import itertools
import json
from pathlib import Path
import sqlite3
import sys
import time

import numpy as np
from scipy import optimize, stats

import rate_players as rp
import rating_model as rm

INNER_CUTOFF = date(2026, 3, 1)
OUTER_CUTOFF = date(2026, 6, 1)
GRID = {"gamma": (0.1, 0.2, 0.3), "tau": (0.2, 0.3, 0.4), "half_life": (365.0, 730.0, 1095.0)}
ELO_K = (16.0, 24.0, 32.0, 48.0)
BOOTSTRAP = 20


def reweight(seats, cutoff, half_life):
    """Time-decay game weights relative to the window's own cutoff (Prepared does the same for its indices)."""
    for s in seats:
        s["weight"] = rm.decay(cutoff.toordinal() - s["date"], half_life)


def window(seats, start, end):
    return [copy.copy(s) for s in seats if (start is None or s["date"] >= start.toordinal()) and s["date"] < end.toordinal()]


def metrics(y, p):
    y, p = np.asarray(y, dtype=float), np.asarray(p, dtype=float)
    if len(y) == 0:
        return None
    bins = []
    for lo in np.arange(0, 1, 0.1):
        mask = (p >= lo) & (p < lo + 0.1)
        if mask.sum() >= 20:
            bins.append({"bin": round(float(lo), 1), "games": int(mask.sum()), "predicted": round(float(p[mask].mean()), 3),
                         "observed": round(float(y[mask].mean()), 3)})
    auc = None
    if 0 < y.sum() < len(y):
        ranks = stats.rankdata(p)
        positives = y == 1
        auc = float((ranks[positives].sum() - positives.sum() * (positives.sum() + 1) / 2) / (positives.sum() * (~positives).sum()))
    return {"games": int(len(y)), "log_loss": round(rm.log_loss(y, p), 4), "brier": round(rm.brier(y, p), 4),
            "accuracy": round(float(np.mean((p > 0.5) == (y == 1))), 4), "auc": round(auc, 4) if auc is not None else None,
            "calibration": bins}


# ---------- models fitted on a training window ----------

def fit_candidate(train, cutoff, content, config):
    prepared = rp.Prepared(train, cutoff.toordinal(), content["weights"], config["half_life"])
    games = rp.build_games(train)
    fit = rp.fit_level(prepared, train, games, config)
    return prepared, fit


def primary_competition(train):
    home = defaultdict(Counter)
    for s in train:
        if not s["international"]:
            home[s["model_id"]][s["competition"]] += 1
    return {p: c.most_common(1)[0][0] for p, c in home.items()}


def candidate_strength(fit, prepared, config):
    comp = {c: fit["lam"][i] for i, c in enumerate(fit["competitions"])}

    def strength(pid, competition, tier):
        if pid in fit["players"]:
            return fit["beta"][fit["players"][pid]]
        base = comp.get(competition, fit["tier"].get(tier, fit["tier"].get("unknown", 0.0)))
        return base + config["gamma"] * prepared.box.get(pid, 0.0)
    return strength


def score_games(test_games, strength, h):
    y, p = [], []
    for g in test_games:
        blue = sum(strength(x, g["competition"], g["tier"]) for x in g["blue"])
        red = sum(strength(x, g["competition"], g["tier"]) for x in g["red"])
        y.append(g["blue_win"])
        p.append(1.0 / (1.0 + np.exp(-(blue - red + h))))
    return np.array(y), np.array(p)


def fit_baseline(train, prepared, cutoff):
    """B0: player strength = tier offset (player's primary tier) + g * box index; side advantage h. Seven tier/side
    parameters and one slope, fitted by logistic regression on the training games (small ridge)."""
    games = rp.build_games(train)
    tier_of = {}
    counts = defaultdict(Counter)
    for s in train:
        if not s["international"]:
            counts[s["model_id"]][s["tier"]] += 1
    for p, c in counts.items():
        tier_of[p] = c.most_common(1)[0][0]

    def design(gs, unknown_tier=None):
        X = np.zeros((len(gs), len(rp.TIERS) + 1))
        for i, g in enumerate(gs):
            for sign, side in ((1, "blue"), (-1, "red")):
                for pid in g[side]:
                    X[i, rp.TIERS.index(tier_of.get(pid, g["tier"]))] += sign
                    X[i, -1] += sign * prepared.box.get(pid, 0.0)
        return X
    X = design(games)
    y = np.array([g["blue_win"] for g in games])
    w = np.array([g["weight"] for g in games])

    def objective(theta):
        eta = X @ theta[:-1] + theta[-1]
        p = 1 / (1 + np.exp(-eta))
        nll = -np.sum(w * (y * np.log(p + 1e-12) + (1 - y) * np.log(1 - p + 1e-12))) + 0.5 * np.sum(theta[:-1] ** 2) / 4.0
        r = w * (p - y)
        return nll, np.concatenate([X.T @ r + theta[:-1] / 4.0, [r.sum()]])
    result = optimize.minimize(objective, np.zeros(X.shape[1] + 1), jac=True, method="L-BFGS-B")
    theta = result.x

    def predict(gs):
        return 1 / (1 + np.exp(-(design(gs) @ theta[:-1] + theta[-1])))
    return predict, {"tier_offsets": dict(zip(rp.TIERS, [round(float(v), 3) for v in theta[:len(rp.TIERS)]])),
                     "box_slope": round(float(theta[len(rp.TIERS)]), 4), "side": round(float(theta[-1]), 4)}


def elo_predict(train_games, test_games, k, online=False):
    history = train_games + (test_games if online else [])
    probabilities, ratings = rm.team_elo(history, k=k)
    if online:
        return probabilities[len(train_games):]
    out = []
    for g in test_games:
        a, b = ratings.get(g["blue_team"], 1500.0), ratings.get(g["red_team"], 1500.0)
        out.append(1.0 / (1.0 + 10 ** ((b - a) / 400.0)))
    return np.array(out)


def side_rate(train_games):
    rate = np.mean([g["blue_win"] for g in train_games])
    return float(np.log(rate / (1 - rate)))


def subsets(test_games, train):
    home = primary_competition(train)
    out = {"all": [], "international": [], "tier_a_domestic": [], "cross_competition": [], "lower_tiers": []}
    for i, g in enumerate(test_games):
        out["all"].append(i)
        if g["tier"] == "s" or g.get("international"):
            out["international"].append(i)
        if g["tier"] == "a" and not g.get("international"):
            out["tier_a_domestic"].append(i)
        if g["tier"] in ("c", "d", "unknown"):
            out["lower_tiers"].append(i)
        blue = Counter(home.get(p) for p in g["blue"]).most_common(1)[0][0]
        red = Counter(home.get(p) for p in g["red"]).most_common(1)[0][0]
        if blue and red and blue != red:
            out["cross_competition"].append(i)
    return out


def tag_international(games, seats):
    flag = {}
    for s in seats:
        flag[s["game_key"]] = s["international"]
    for g in games:
        g["international"] = flag.get(g["key"], False)
    return games


def evaluate_window(seats, train_end, test_end, content, configs, elo_ks, train_start=None):
    train = window(seats, train_start, train_end)
    test = window(seats, train_end, test_end)
    for s in test:
        s["weight"] = 1.0
    reweight(train, train_end, 365.0)
    train_games, test_games = rp.build_games(train), tag_international(rp.build_games(test), test)
    y = np.array([g["blue_win"] for g in test_games])
    results = {"train_games": len(train_games), "test_games": len(test_games), "models": {}}
    h0 = side_rate(train_games)
    results["models"]["M0_side_only"] = metrics(y, np.full(len(y), 1 / (1 + np.exp(-h0))))
    for k in elo_ks:
        results["models"][f"ELO_frozen_k{int(k)}"] = metrics(y, elo_predict(train_games, test_games, k))
    candidates = {}
    prepared_cache = {}
    for config in sorted(configs, key=lambda c: c["half_life"]):
        key = config["half_life"]
        if key not in prepared_cache:
            prepared_cache[key] = rp.Prepared(train, train_end.toordinal(), content["weights"], key)
        prepared = prepared_cache[key]
        reweight(train, train_end, key)
        fit = rp.fit_level(prepared, train, rp.build_games(train), config)
        strength = candidate_strength(fit, prepared, config)
        _, p = score_games(test_games, strength, fit["h"])
        name = f"C1_g{config['gamma']}_t{config['tau']}_hl{int(config['half_life'])}"
        candidates[name] = (config, metrics(y, p), p)
        print(f"  {name} log_loss {candidates[name][1]['log_loss']} converged {fit['converged']}", flush=True)
    for name, (config, m, _) in candidates.items():
        results["models"][name] = m
    reweight(train, train_end, 365.0)
    predict_b0, b0_params = fit_baseline(train, prepared_cache.get(365.0) or next(iter(prepared_cache.values())), train_end)
    results["models"]["B0_tier_plus_box"] = metrics(y, predict_b0(test_games))
    results["b0_parameters"] = b0_params
    results["_test_games"], results["_candidates"], results["_train"] = test_games, candidates, train
    return results


# ---------- checks on the final model ----------

def spearman(a, b):
    return round(float(stats.spearmanr(a, b).correlation), 4)


def final_checks(result):
    records, model, seats = result["records"], result["model"], result["seats"]
    rated = [r for r in records if r["rating_evidence_class"] != "PRIOR_ONLY"]
    measured = [r for r in records if r["rating_evidence_class"] == "MEASURED_ESTIMATE"]
    checks = {}
    # Coverage and formula.
    ids = [r["player_id"] for r in records]
    weights = result["content"]["weights"]
    role_key = {v: k for k, v in rp.ROLE_LABEL.items()}
    formula_errors, bound_errors = 0, 0
    for r in records:
        attrs = [r["mechanics"], r["early_game"], r["teamfight"], r["macro"], r["champion_pool"]]
        if any(not 1 <= a <= 99 for a in attrs + [r["overall_rating"]]):
            bound_errors += 1
        role = role_key.get(r["primary_role"])
        if role and rm.overall_from(attrs, weights[role]) != r["overall_rating"]:
            formula_errors += 1
    checks["coverage"] = {"catalog_rows": len(records), "unique_ids": len(set(ids)),
                          "by_class": dict(Counter(r["rating_evidence_class"] for r in records)),
                          "prior_only_with_games": sum(1 for r in records if r["rating_evidence_class"] == "PRIOR_ONLY" and any(r[f"games_{y}"] for y in rp.YEARS)),
                          "measured_with_zero_games": sum(1 for r in records if r["rating_evidence_class"] != "PRIOR_ONLY" and not any(r[f"games_{y}"] for y in rp.YEARS)),
                          "bound_errors": bound_errors, "overall_formula_errors": formula_errors,
                          "prior_only_without_role": sum(1 for r in records if r["rating_evidence_class"] == "PRIOR_ONLY" and not r["primary_role"])}
    # Distributions.
    def dist(values):
        v = np.array(values, dtype=float)
        return {"n": int(len(v)), "mean": round(float(v.mean()), 2), "sd": round(float(v.std()), 2),
                "p5": float(np.percentile(v, 5)), "p25": float(np.percentile(v, 25)), "p50": float(np.percentile(v, 50)),
                "p75": float(np.percentile(v, 75)), "p95": float(np.percentile(v, 95)), "max": float(v.max()), "min": float(v.min())} if len(v) else None
    checks["overall_distribution"] = {cls: dist([r["overall_rating"] for r in records if r["rating_evidence_class"] == cls])
                                      for cls in ("MEASURED_ESTIMATE", "LIMITED_EVIDENCE", "PRIOR_ONLY")}
    checks["skill_distribution_rated"] = {k: dist([r[k] for r in rated]) for k in ("mechanics", "early_game", "teamfight", "macro", "champion_pool")}
    checks["overall_by_role_measured"] = {role: dist([r["overall_rating"] for r in measured if r["primary_role"] == role]) for role in rp.ROLE_LABEL.values()}
    checks["overall_by_tier_measured"] = {t: dist([r["overall_rating"] for r in measured if r["primary_tier"] == t]) for t in rp.TIERS}
    checks["confidence_labels"] = dict(Counter(r["confidence_label"] for r in records))
    checks["form"] = {"status": dict(Counter(r["form_status"] for r in records)), "trend": dict(Counter(r["form_trend"] for r in records)),
                      "score": dist([r["form_score"] for r in records if r["form_score"] is not None])}
    # Small samples: Levels of players with few games stay near their competition, never at the top.
    levels = {r["player_id"]: r["_level"] for r in rated}
    games = {r["player_id"]: sum(r[f"games_{y}"] for y in rp.YEARS) for r in rated}
    buckets = {"1-4": (1, 4), "5-9": (5, 9), "10-19": (10, 19), "20-39": (20, 39), "40-99": (40, 99), "100+": (100, 10 ** 6)}
    checks["small_sample"] = {name: dist([levels[p] for p in levels if lo <= games[p] <= hi]) for name, (lo, hi) in buckets.items()}
    comp_mean = defaultdict(list)
    for r in rated:
        if r["primary_competition"]:
            comp_mean[r["primary_competition"]].append(r["_level"])
    comp_mean = {c: np.mean(v) for c, v in comp_mean.items()}
    few = [abs(r["_level"] - comp_mean[r["primary_competition"]]) for r in rated if games[r["player_id"]] <= 3 and r["primary_competition"] in comp_mean]
    many = [abs(r["_level"] - comp_mean[r["primary_competition"]]) for r in rated if games[r["player_id"]] >= 100 and r["primary_competition"] in comp_mean]
    checks["small_sample"]["mean_abs_deviation_from_competition_mean"] = {"games<=3": round(float(np.mean(few)), 2) if few else None,
                                                                        "games>=100": round(float(np.mean(many)), 2) if many else None}
    checks["small_sample"]["max_overall_with_fewer_than_10_games"] = max((r["overall_rating"] for r in rated if games[r["player_id"]] < 10), default=None)
    # Team results: how much of the Level is the team's 2026 win rate?
    team_games = defaultdict(lambda: [0, 0])
    player_team = {}
    by_player = defaultdict(list)
    for s in seats:
        if s["year"] == 2026 and not s["international"]:
            team_games[s["team_key"]][0] += s["win"]
            team_games[s["team_key"]][1] += 1
            by_player[s["model_id"]].append(s["team_key"])
    for pid, teams in by_player.items():
        player_team[pid] = Counter(teams).most_common(1)[0][0]
    pairs = [(r["_level"], team_games[player_team[f"ps:{r['player_id']}"]][0] / team_games[player_team[f"ps:{r['player_id']}"]][1], player_team[f"ps:{r['player_id']}"])
             for r in measured if f"ps:{r['player_id']}" in player_team and team_games[player_team[f"ps:{r['player_id']}"]][1] >= 20]
    if pairs:
        lv, wr, team = zip(*pairs)
        lv = np.array(lv)
        team_ids = {t: i for i, t in enumerate(sorted(set(team)))}
        team_means = defaultdict(list)
        for v, t in zip(lv, team):
            team_means[t].append(v)
        within = np.mean([np.var(v) for v in team_means.values() if len(v) >= 3])
        checks["team_dependence"] = {"players": len(pairs), "corr_level_team_win_rate": round(float(np.corrcoef(lv, wr)[0, 1]), 3),
                                     "share_of_level_variance_between_teams": round(float(1 - within / np.var(lv)), 3),
                                     "mean_within_team_sd": round(float(np.sqrt(within)), 2),
                                     "note": "Teammates share results; individual separation comes from the box-score prior and lineup changes."}
    # Competition comparability.
    fit = model["fit"]
    comp_levels = []
    links = model["competition_link"]
    games_per_comp = Counter(s["competition"] for s in seats)
    k = model["points_per_logit"]
    for i, c in enumerate(fit["competitions"]):
        comp_levels.append({"competition": c.split(":", 1)[1], "tier": fit["tier_of"][c], "games_seats": games_per_comp[c],
                            "level_points": round(rm.ANCHOR_LEVEL + 5 * k * (fit["lam"][i] - model["anchor"]), 1),
                            "link": links.get(c, "WEAK")})
    comp_levels.sort(key=lambda x: -x["games_seats"])
    checks["competition_levels_top40"] = comp_levels[:40]
    checks["competition_links"] = dict(Counter(links.values()))
    checks["tier_levels_points"] = result["manifest"]["tier_levels_points"]
    # Rankings.
    lists = result["lists"]
    checks["rankings"] = {label: {"published": len(rows), "duplicate_ids": len(rows) - len({r["player_id"] for r in rows}),
                                  "min_games_2026_in_role": min((r["games_2026_in_role"] for r in rows), default=None),
                                  "evidence_classes": dict(Counter(r["evidence_class"] for r in rows)),
                                  "leagues": dict(Counter(r["league"] for r in rows).most_common(8)),
                                  "links": dict(Counter(r["competition_link"] for r in rows))}
                          for label, rows in lists.items()}
    checks["rankings"]["Overall"]["roles"] = dict(Counter(r["role"] for r in lists["Overall"]))
    # v2: bands, gates, potential, age rules.
    checks["bands"] = {"all": dict(Counter(r["band"] for r in records)),
                       "rated": dict(Counter(r["band"] for r in rated)),
                       "by_primary_tier_measured": {t: dict(Counter(r["band"] for r in measured if r["primary_tier"] == t)) for t in rp.TIERS},
                       "overall_range": [min(r["overall_rating"] for r in records), max(r["overall_rating"] for r in records)],
                       "elite_without_evidence": sum(1 for r in records if r["band"] == "ELITE" and not r["elite_evidence"]),
                       "prior_only_above_cap": sum(1 for r in records if r["rating_evidence_class"] == "PRIOR_ONLY" and r["overall_rating"] > rm.CAP_PRIOR_ONLY),
                       "capped_at_89_by_gate": sum(1 for r in rated if not r["elite_evidence"] and r["raw_quality"] is not None
                                                   and rm.banded(r["raw_quality"]) >= rm.ELITE_FLOOR - 0.5),
                       "elite_evidence_players": sum(1 for r in rated if r["elite_evidence"])}
    with_age = [r for r in rated if r["age"] is not None]
    checks["age"] = {"rated_with_age": len(with_age), "rated": len(rated),
                     "sources": dict(Counter(r["age_source"] for r in records if r["age_source"])),
                     "conflicts": sum(1 for r in records if r["age_conflict"]), "stages": dict(Counter(r["career_stage"] for r in rated)),
                     "potential_below_overall": sum(1 for r in records if r["potential"] is not None and r["potential"] < r["overall_rating"]),
                     "potential_differs_at_25_plus": sum(1 for r in records if r["potential"] is not None and r["age"] is not None
                                                         and r["age"] >= rm.GROWTH_END_AGE and r["potential"] != r["overall_rating"]),
                     "potential_above_93": sum(1 for r in records if (r["potential"] or 0) > rm.SCALE_MAX),
                     "potential_gap_by_age": {a: dist([r["potential"] - r["overall_rating"] for r in with_age
                                                       if r["age"] == a and r["potential"] is not None]) for a in range(17, 26)},
                     "aging_floor_applied": sum(1 for r in rated if r["aging_floor_applied"]),
                     "veterans_dropping_more_than_allowed": sum(1 for r in rated if r["age"] is not None and r["age"] >= rm.VETERAN_AGE
                                                                and r["rating_prev_year"] is not None
                                                                and r["overall_rating"] < r["rating_prev_year"] - rm.aging_drop(r["age"]) - 1),
                     "development_curve": result["curve"]}
    checks["achievements"] = {"players_with_titles": sum(1 for r in rated if r["titles"]),
                              "players_with_international_finals": sum(1 for r in rated if r["international_finals"]),
                              "players_with_two_tier_a_titles": sum(1 for r in rated if r["tier_a_titles"] >= 2),
                              "stats": result["manifest"]["achievement_stats"]}
    return checks


def bootstrap(result, reps=BOOTSTRAP, seed=20261014):
    """Game-resampling bootstrap of the level fit (box-score indices held at the full-data values; documented)."""
    model, config, content = result["model"], result["config"], result["content"]
    games = model["games"]
    rng = np.random.default_rng(seed)
    levels = defaultdict(list)
    for _ in range(reps):
        sample = [games[i] for i in rng.integers(0, len(games), len(games))]
        fit = rp.fit_level(model["prepared"], result["seats"], sample, config)
        anchor, _ = rp.anchor_beta(fit, result["seats"])
        for p, i in fit["players"].items():
            levels[p].append(rm.level(fit["beta"][i], anchor, model["points_per_logit"]))
    out = {}
    for label, rows in result["lists"].items():
        sds, ranks = [], []
        level_rows = []
        for r in rows:
            values = levels.get(f"ps:{r['player_id']}", [])
            if len(values) >= reps // 2:
                sds.append(float(np.std(values)))
                level_rows.append(values)
        # Rank stability within the published list: per replicate, rank the listed players by their bootstrap Level.
        if level_rows:
            width = min(len(v) for v in level_rows)
            matrix = np.array([v[:width] for v in level_rows])
            rank_matrix = (-matrix).argsort(axis=0).argsort(axis=0) + 1
            spans = np.percentile(rank_matrix, 90, axis=1) - np.percentile(rank_matrix, 10, axis=1)
            top10_hold = float(np.mean([(rank_matrix[:10, j] <= 10).mean() for j in range(width)]))
        else:
            spans, top10_hold = [0], None
        out[label] = {"players": len(sds), "mean_level_sd": round(float(np.mean(sds)), 2) if sds else None,
                      "max_level_sd": round(float(np.max(sds)), 2) if sds else None,
                      "median_rank_span_80": float(np.median(spans)), "top10_stay_in_top10_share": round(top10_hold, 3) if top10_hold is not None else None}
    return {"replicates": reps, "note": "games resampled with replacement; box-score indices fixed at the full-data values", "lists": out}


def sensitivity(result, variants):
    """Each variant changes one setting and rebuilds the records. Level-model settings refit on the same box-score
    side; rating-scale settings (achievement and consistency points) only rebuild the records; half-life variants
    recompute everything in place, so they run last (the reproducibility run reloads the seats afterwards)."""
    base = result["records"]
    content, seats, model0 = result["content"], result["seats"], result["model"]
    base_overall = {r["player_id"]: r["overall_rating"] for r in base if r["rating_evidence_class"] == "MEASURED_ESTIMATE"}
    base_top = {label: {r["player_id"] for r in rows} for label, rows in result["lists"].items()}
    out = {}
    ordered = sorted(variants.items(), key=lambda kv: "half_life" in kv[1])
    db = sqlite3.connect(rp.DATA_DIR / "staging.sqlite3")
    try:
        for name, change in ordered:
            scale = {k: v for k, v in change.items() if k.isupper()}
            config = dict(result["config"], **{k: v for k, v in change.items() if not k.isupper()})
            saved = {k: getattr(rm, k) for k in scale}
            for k, v in scale.items():
                setattr(rm, k, v)
            try:
                if "half_life" in change:
                    model = rp.rate(seats, content, config)
                elif any(k in change for k in ("gamma", "tau", "sigma_c")):
                    fit = rp.fit_level(model0["prepared"], seats, model0["games"], config)
                    anchor, _ = rp.anchor_beta(fit, seats)
                    k_ = model0["points_per_logit"]
                    model = dict(model0, fit=fit, anchor=anchor,
                                 levels={p: rm.level(fit["beta"][i], anchor, k_) for p, i in fit["players"].items()},
                                 level_sd={p: 5.0 * k_ * fit["beta_sd"][i] for p, i in fit["players"].items()})
                else:
                    model = model0
                model["competition_link"] = model0["competition_link"]
                records, _, _ = rp.build_records(db, seats, model, content, config, rp.AS_OF, result["births"], result["prev"], result["curve"])
            finally:
                for k, v in saved.items():
                    setattr(rm, k, v)
            lists, _ = rp.rankings(records, max_sd=rp.max_level_sd(config, content["points_per_logit"]))
            overall = {r["player_id"]: r["overall_rating"] for r in records if r["player_id"] in base_overall}
            common = sorted(set(overall) & set(base_overall))
            out[name] = {"spearman_overall_measured": spearman([base_overall[p] for p in common], [overall[p] for p in common]),
                         "mean_abs_overall_change": round(float(np.mean([abs(base_overall[p] - overall[p]) for p in common])), 3),
                         "elite_count": sum(1 for r in records if r["band"] == "ELITE"),
                         "top100_overlap": {label: len(base_top[label] & {r["player_id"] for r in rows}) for label, rows in lists.items()}}
            print("  sensitivity", name, out[name]["spearman_overall_measured"], flush=True)
    finally:
        db.close()
    return out


def temporal(db_path, content, quick=False):
    """Inner selection and outer comparison. Loads its own seats and returns plain results only, so nothing of the
    windows stays in memory for the later stages (the machine has about 8 GB)."""
    db = sqlite3.connect(db_path)
    try:
        seats = rp.load_seats(db, rp.AS_OF)
    finally:
        db.close()
    grid = {"gamma": (0.2,), "tau": (0.3,), "half_life": (365.0,)} if quick else GRID
    configs = [dict(rp.CONFIG, gamma=g, tau=t, half_life=h) for g, t, h in itertools.product(grid["gamma"], grid["tau"], grid["half_life"])]
    inner = evaluate_window(seats, INNER_CUTOFF, OUTER_CUTOFF, content, configs, ELO_K)
    best_name = min(inner["_candidates"], key=lambda n: inner["_candidates"][n][1]["log_loss"])
    best = inner["_candidates"][best_name][0]
    best_k = min(ELO_K, key=lambda k: inner["models"][f"ELO_frozen_k{int(k)}"]["log_loss"])
    print("inner best", best_name, inner["_candidates"][best_name][1]["log_loss"], "elo k", best_k, flush=True)
    inner_report = {k: v for k, v in inner.items() if not k.startswith("_")}
    del inner
    gc.collect()
    ablation = dict(best, gamma=0.0)
    outer = evaluate_window(seats, OUTER_CUTOFF, rp.AS_OF, content, [best, ablation] if best["gamma"] else [best], [best_k])
    del seats
    elo_online = elo_predict(rp.build_games(outer["_train"]), outer["_test_games"], best_k, online=True)
    y = np.array([g["blue_win"] for g in outer["_test_games"]])
    outer["models"]["ELO_online_reference"] = metrics(y, elo_online)
    groups = subsets(outer["_test_games"], outer["_train"])
    model_predictions = {name: c[2] for name, c in outer["_candidates"].items()}
    model_predictions["ELO_frozen"] = elo_predict(rp.build_games(outer["_train"]), outer["_test_games"], best_k)
    model_predictions["M0_side_only"] = np.full(len(y), 1 / (1 + np.exp(-side_rate(rp.build_games(outer["_train"])))))
    predict_b0, _ = fit_baseline(outer["_train"], rp.Prepared(outer["_train"], OUTER_CUTOFF.toordinal(), content["weights"], best["half_life"]), OUTER_CUTOFF)
    model_predictions["B0_tier_plus_box"] = predict_b0(outer["_test_games"])
    by_subset = {subset: {name: (metrics(y[index], p[index]) or {}).get("log_loss") for name, p in model_predictions.items()} | {"games": len(index)}
                 for subset, index in groups.items()}
    report = {"inner": inner_report, "selected": {"config": best, "name": best_name, "elo_k": best_k},
              "outer": {k: v for k, v in outer.items() if not k.startswith("_")}, "outer_log_loss_by_subset": by_subset}
    print("outer", json.dumps({k: (v or {}).get("log_loss") for k, v in outer["models"].items()}), flush=True)
    print("selected config", json.dumps(best), flush=True)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=rp.DATA_DIR / "staging.sqlite3")
    parser.add_argument("--skip-bootstrap", action="store_true")
    parser.add_argument("--skip-sensitivity", action="store_true")
    parser.add_argument("--skip-temporal", action="store_true")
    parser.add_argument("--reuse-temporal", type=Path, help="take inner/outer/selected from a temporal checkpoint JSON")
    parser.add_argument("--out", type=Path, default=rp.OUT_DIR / "validation.json")
    parser.add_argument("--quick", action="store_true", help="smoke test: one grid point, 2 bootstrap replicates, 1 variant")
    args = parser.parse_args()
    started = time.time()
    content = rp.load_content()
    report = {"rating_model_version": rm.MODEL_VERSION, "as_of": rp.AS_OF.isoformat(), "inner_cutoff": INNER_CUTOFF.isoformat(),
              "outer_cutoff": OUTER_CUTOFF.isoformat()}
    checkpoint = args.out.with_name("validation_temporal.json")
    if args.reuse_temporal:
        report.update(json.loads(args.reuse_temporal.read_text(encoding="utf-8")))
        print("temporal results reused from", args.reuse_temporal, flush=True)
    elif not args.skip_temporal:
        part = temporal(args.db, content, args.quick)
        checkpoint.parent.mkdir(parents=True, exist_ok=True)
        checkpoint.write_text(json.dumps(part, indent=1, ensure_ascii=False, default=float) + "\n", encoding="utf-8", newline="\n")
        report.update(part)
        gc.collect()
    config = report.get("selected", {}).get("config") or rp.CONFIG
    result = rp.run(args.db, rp.AS_OF, rp.OUT_DIR, config=config)
    report["final_config"] = result["config"]
    report["checks"] = final_checks(result)
    print("checks done", round(time.time() - started), flush=True)
    if not args.skip_bootstrap:
        report["bootstrap"] = bootstrap(result, reps=2 if args.quick else BOOTSTRAP)
        print("bootstrap done", round(time.time() - started), flush=True)
    first = dict(result["manifest"]["outputs"])
    if not args.skip_sensitivity:
        variants = {"gamma_x0.5": {"gamma": result["config"]["gamma"] * 0.5}, "gamma_x1.5": {"gamma": result["config"]["gamma"] * 1.5 or 0.05},
                    "tau_x0.75": {"tau": result["config"]["tau"] * 0.75}, "tau_x1.25": {"tau": result["config"]["tau"] * 1.25},
                    "sigma_c_x2": {"sigma_c": result["config"]["sigma_c"] * 2},
                    "achievement_points_x0.5": {"ACHIEVEMENT_MAX_POINTS": rm.ACHIEVEMENT_MAX_POINTS * 0.5},
                    "achievement_points_x1.5": {"ACHIEVEMENT_MAX_POINTS": rm.ACHIEVEMENT_MAX_POINTS * 1.5},
                    "no_consistency_points": {"CONSISTENCY_POINTS": 0.0},
                    "half_life_x0.5": {"half_life": result["config"]["half_life"] * 0.5},
                    "half_life_x1.5": {"half_life": result["config"]["half_life"] * 1.5}}
        report["sensitivity"] = sensitivity(result, dict(list(variants.items())[:1]) if args.quick else variants)
        print("sensitivity done", round(time.time() - started), flush=True)
    # Reproducibility: a second identical run gives identical outputs (the first run's data is released first).
    del result
    gc.collect()
    again = rp.run(args.db, rp.AS_OF, rp.OUT_DIR, config=config)
    report["reproducibility"] = {"identical_output_hashes": first == again["manifest"]["outputs"], "files": len(first)}
    del again
    report["seconds"] = round(time.time() - started)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(report, indent=1, ensure_ascii=False, default=float) + "\n", encoding="utf-8", newline="\n")
    print("wrote", args.out, "seconds", report["seconds"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
