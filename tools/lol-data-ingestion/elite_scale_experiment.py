"""Elite rating-scale experiment (offline, D1 of the hybrid-world decision verification).

EXPERIMENT ONLY. It reads the research ratings of the uncapped scale experiment (data/ratings/experiment-uncapped/) and the
staging database, changes nothing in them, and writes aggregates to data/world/decisions/:

  - the raw-quality (R) and linear-Level tail of rated players, and the slope of each segment of the banded transform T;
  - candidate re-knotted transforms of the elite segment (post hoc on R, elite-evidence gate kept) and the Overall counts
    each one gives (no quota: a transform maps R, it never fills a band);
  - a competitive calibration on the 2026 primary games whose ten seats are all rated players: a logistic fit of the blue
    side's result on the lineup-average difference divided by the game's points per logit (LogitScale / 100), overall
    and by matchup strength. A slope above 1 in a stratum means the scale shows smaller differences there than the
    results support. The ratings are as of 2026-10-14 and were fitted on these games, so this is an in-sample check of
    the transform's shape, not an out-of-sample validation;
  - a sidecar for the world experiment (one candidate transform): for each player of the real pack, the Overall change
    and the new potential, so the world can be run with the re-knotted elite.

Aggregates only are printed; the sidecar holds provider ids and numbers, no names, and stays in git-ignored data/.

    .venv\\Scripts\\python.exe elite_scale_experiment.py [--sidecar K96]
"""
import argparse
from collections import Counter, defaultdict
import json
import math
from pathlib import Path
import re
import sqlite3
import sys

import numpy as np
from scipy import optimize

import rating_model as rm

DATA = Path(__file__).resolve().parent / "data"
RATINGS = DATA / "ratings" / "experiment-uncapped" / "player_ratings_2023_2026.json"
OUT = DATA / "world" / "decisions"
POINTS_PER_LOGIT = 7.0          # content Balance.LogitScale 700 / 100 (rating_model's reading of the game)
V2 = rm.BAND_KNOTS               # ((10, 30), (47, 54.5), (61, 73.5), (72, 82.5), (80, 89.5), (92, 93))
UNCAPPED = V2 + ((92.0 + (99.0 - 93.0) / ((93.0 - 89.5) / (92.0 - 80.0)), 99.0),)
TRANSFORMS = {
    "V2U": UNCAPPED,                                              # the uncapped experiment (the world pack's scale)
    "K96": V2[:-1] + ((92.0, 96.0), (104.0, 99.0)),               # elite segment re-knotted to 92 -> 96
    "K98": V2[:-1] + ((92.0, 98.0), (98.0, 99.0)),                # 92 -> 98
    "L1": V2[:-1] + ((89.5, 99.0),),                              # slope 1 above R 80: Overall moves with the Level
}
ROLE_LABEL = {"top": "Top", "jng": "Jungle", "mid": "Mid", "bot": "Bot", "sup": "Support"}


def t_of(knots, r):
    if r <= knots[0][0]:
        return knots[0][1]
    for (x0, y0), (x1, y1) in zip(knots, knots[1:]):
        if r <= x1:
            return y0 + (r - x0) * (y1 - y0) / (x1 - x0)
    return min(99.0, knots[-1][1])


def t_inverse(knots, y):
    if y <= knots[0][1]:
        return knots[0][0]
    for (x0, y0), (x1, y1) in zip(knots, knots[1:]):
        if y <= y1:
            return x0 + (y - y0) * (x1 - x0) / (y1 - y0)
    return knots[-1][0]


def elite_ok(r):
    return bool(r["elite_evidence"]) and (r["consistency_z"] if r["consistency_z"] is not None else 0.0) >= rm.ELITE_MIN_CONSISTENCY_Z


def gated(knots, r_value, ok):
    value = t_of(knots, r_value)
    return value if ok else min(value, rm.CAP_WITHOUT_ELITE_EVIDENCE)


def upper_growth(record):
    m = re.search(r"\+([0-9.]+) at the 80th percentile", record.get("potential_basis") or "")
    return float(m.group(1)) if m else None


def segment_slopes(knots):
    return [{"r": [x0, x1], "overall": [y0, y1], "slope": round((y1 - y0) / (x1 - x0), 3)} for (x0, y0), (x1, y1) in zip(knots, knots[1:])]


def tail(players):
    rated = [r for r in players if r["rating_evidence_class"] != "PRIOR_ONLY" and r["raw_quality"] is not None]
    rs = sorted((r["raw_quality"] for r in rated), reverse=True)
    levels = sorted((r["level_linear"] for r in rated if r["level_linear"] is not None), reverse=True)
    counts = {f"r_{t}_plus": sum(1 for x in rs if x >= t) for t in (75, 80, 84, 88, 90, 92)}
    gaps = {"r_rank1_minus_rank2": round(rs[0] - rs[1], 2), "r_rank1_minus_rank10": round(rs[0] - rs[9], 2),
            "r_rank10_minus_rank50": round(rs[9] - rs[49], 2), "r_rank50_minus_rank200": round(rs[49] - rs[199], 2)}
    return {"rated_players": len(rated), "r_top10": [round(x, 2) for x in rs[:10]], "level_top10": [round(x, 2) for x in levels[:10]],
            **counts, **gaps, "elite_evidence_players": sum(1 for r in rated if elite_ok(r))}


def counts_under(players, knots):
    out = Counter()
    for r in players:
        if r["rating_evidence_class"] == "PRIOR_ONLY" or r["raw_quality"] is None or r["aging_floor_applied"]:
            continue
        value = rm.round_half_even(gated(knots, r["raw_quality"], elite_ok(r)))
        if value >= 88:
            out[min(int(value), 99)] += 1
    bands = {"88-89": sum(v for k, v in out.items() if k <= 89), "90-93": sum(v for k, v in out.items() if 90 <= k <= 93),
             "94-96": sum(v for k, v in out.items() if 94 <= k <= 96), "97-99": sum(v for k, v in out.items() if k >= 97)}
    return {"by_value": dict(sorted(out.items())), "bands": bands, "max": max(out) if out else None}


def fit_logistic(dx, y):
    """Maximum-likelihood fit of P(blue wins) = logistic(a + b * dx). Returns (a, b, se_b, log loss)."""
    dx = np.asarray(dx, float); y = np.asarray(y, float)
    def nll(p):
        z = p[0] + p[1] * dx
        return float(np.sum(np.logaddexp(0, z) - y * z))
    res = optimize.minimize(nll, np.array([0.0, 1.0]), method="BFGS")
    a, b = res.x
    z = a + b * dx; q = 1 / (1 + np.exp(-z))
    x = np.column_stack([np.ones_like(dx), dx]); w = q * (1 - q)
    cov = np.linalg.inv(x.T @ (x * w[:, None]))
    return float(a), float(b), float(math.sqrt(cov[1, 1])), float(res.fun / len(y))


def calibration(players):
    by_ps = {r["player_id"]: r for r in players}
    db = sqlite3.connect(f"file:{DATA / 'staging.sqlite3'}?mode=ro", uri=True)
    ids = {k: v for k, v in db.execute("select oe_identity_key, ps_player_id from lpx_identity_map where status='HIGH_CONFIDENCE_MATCH'")}
    games = defaultdict(dict)
    rows = db.execute("""select g.game_key, p.side, p.role, p.identity_key, p.win from pf_games g join pf_player_games p on p.game_key = g.game_key
                         where g.year = 2026 and g.tournament_level = 'Primary'""")
    for key, side, role, identity, win in rows:
        games[key].setdefault(side, []).append((role, identity, win))
    seats_total = 0; seat_missing = Counter(); kept = []
    scales = ["V2U", "K96", "K98", "L1", "LEVEL"]
    for key, sides in games.items():
        if set(sides) != {"Blue", "Red"} or any(len(v) != 5 for v in sides.values()):
            seat_missing["not_five_a_side"] += 1
            continue
        means = {}; ok = True; result = None
        for side, seats in sides.items():
            values = defaultdict(list)
            for role, identity, win in seats:
                seats_total += 1
                ps = ids.get(identity)
                r = by_ps.get(ps) if ps else None
                if r is None or r["rating_evidence_class"] == "PRIOR_ONLY" or r["raw_quality"] is None or r["level_linear"] is None:
                    ok = False; seat_missing["unrated_seat"] += 1; break
                label = ROLE_LABEL.get(role)
                rr = (r.get("role_ratings") or {}).get(label)
                if rr is None:
                    ok = False; seat_missing["role_not_rated"] += 1; break
                if label == r["primary_role"]:
                    rq = r["raw_quality"]
                else:   # off-role seat: R read back from the role's Overall through the uncapped transform
                    rq = t_inverse(UNCAPPED, rr["overall"])
                values["V2U"].append(rr["overall"])
                for name in ("K96", "K98", "L1"):
                    values[name].append(gated(TRANSFORMS[name], rq, elite_ok(r)))
                values["LEVEL"].append(r["level_linear"] - (r["raw_quality"] - rq))
                if side == "Blue":
                    result = int(win)
            if not ok:
                break
            means[side] = {s: float(np.mean(values[s])) for s in scales}
        if ok:
            kept.append((means, result))
    out = {"games_2026_primary": len(games), "games_used": len(kept), "excluded": dict(seat_missing), "points_per_logit": POINTS_PER_LOGIT, "fits": {}}
    y = np.array([k[1] for k in kept])
    strata = {"all": lambda m: True}
    for lo, hi, name in ((85, 200, "both_sides_avg_85_plus"), (78, 85, "avg_78_85"), (0, 78, "avg_below_78")):
        strata[name] = (lambda lo, hi: (lambda m: lo <= (m["Blue"]["V2U"] + m["Red"]["V2U"]) / 2 < hi))(lo, hi)
    for stratum, keep in strata.items():
        index = [i for i, (m, _) in enumerate(kept) if keep(m)]
        if len(index) < 100:
            out["fits"][stratum] = {"games": len(index), "note": "too few games"}
            continue
        block = {"games": len(index)}
        for s in scales:
            dx = [(kept[i][0]["Blue"][s] - kept[i][0]["Red"][s]) / POINTS_PER_LOGIT for i in index]
            a, b, se, ll = fit_logistic(dx, y[index])
            block[s] = {"slope": round(b, 3), "slope_se": round(se, 3), "side_intercept": round(a, 3), "log_loss": round(ll, 4),
                        "mean_abs_diff_points": round(float(np.mean(np.abs(np.array(dx)))) * POINTS_PER_LOGIT, 2)}
        out["fits"][stratum] = block
    return out


def sidecar(players, name):
    knots = TRANSFORMS[name]
    pack_path = sorted((DATA / "world").glob("real_roster_snapshot_*.json"))[-1]
    pack = json.loads(pack_path.read_text(encoding="utf-8"))
    by_ps = {r["player_id"]: r for r in players}
    entries = {}; stats = Counter()
    for pid in sorted(pack["players"]):
        r = by_ps.get(pid)
        if r is None or r["rating_evidence_class"] == "PRIOR_ONLY" or r["raw_quality"] is None:
            stats["unchanged_not_rated"] += 1
            continue
        if r["aging_floor_applied"]:
            stats["unchanged_aging_floor"] += 1
            continue
        old_target = rm.round_half_even(gated(UNCAPPED, r["raw_quality"], elite_ok(r)))
        new_target = rm.round_half_even(gated(knots, r["raw_quality"], elite_ok(r)))
        delta = int(new_target - old_target)
        overall = int(r["overall_rating"] + delta)
        if r["age"] is None or r["age"] >= rm.GROWTH_END_AGE or r["potential"] is None:
            potential = overall
        else:
            upper = upper_growth(r)
            potential = r["potential"] if upper is None else int(min(99, max(overall, rm.round_half_even(t_of(knots, r["raw_quality"] + upper)))))
        if delta == 0 and potential == (r["potential"] if r["potential"] is not None else r["overall_rating"]):
            stats["unchanged"] += 1
            continue
        entries[pid] = {"delta": delta, "potential": potential}
        stats["changed"] += 1
        stats[f"delta_{delta}"] += 1
    path = OUT / f"elite_reknot_{name.lower()}.json"
    path.write_text(json.dumps({"transform": name, "knots": knots, "pack": pack_path.name, "players": entries}, indent=1) + "\n", encoding="utf-8")
    return {"file": path.name, **dict(sorted(stats.items()))}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--sidecar", default="K96", choices=sorted(TRANSFORMS))
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    players = json.loads(RATINGS.read_text(encoding="utf-8"))["players"]
    result = {
        "ratings": str(RATINGS.relative_to(DATA)), "rating_model": players[0]["rating_model_version"],
        "transforms": {k: {"knots": v, "segments": segment_slopes(v)} for k, v in TRANSFORMS.items()},
        "tail": tail(players),
        "counts": {k: counts_under(players, v) for k, v in TRANSFORMS.items()},
        "calibration": calibration(players),
        "sidecar": sidecar(players, args.sidecar),
    }
    (OUT / "elite_scale_analysis.json").write_text(json.dumps(result, indent=1) + "\n", encoding="utf-8")
    print(json.dumps({k: result[k] for k in ("tail", "counts", "calibration", "sidecar")}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
