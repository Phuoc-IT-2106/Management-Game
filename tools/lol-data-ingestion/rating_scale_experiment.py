"""Rating-scale experiment (offline): the research ratings without the practical ceiling at 93.

EXPERIMENT ONLY. It reruns `rate_players.run` with two constants of `rating_model` changed and writes to its own
directory, so the v2 outputs in data/ratings/ stay as they are:
  - the banded transform T keeps every v2 knot, and its last segment (80 -> 89.5, 92 -> 93) continues at the same slope
    up to 99 instead of stopping at 93 (one extra knot at R = 112.57). T is unchanged for every R <= 92, so no player
    below the old clamp moves;
  - the scale maximum (Overall and potential) is 99 instead of 93, and a band EXCEPTIONAL 94-99 is named above ELITE.
    It needs the same elite evidence: without it the Overall still stops at 89.

Then it compares the experiment with the v2 outputs and prints aggregate counts only (no player names).

    .venv\\Scripts\\python.exe rating_scale_experiment.py            # run the variant and compare
    .venv\\Scripts\\python.exe rating_scale_experiment.py --compare  # compare existing outputs only
"""
import argparse
from collections import Counter
import json
from pathlib import Path
import sys

import rating_model as rm
import rate_players as rp

EXPERIMENT_DIR = rp.DATA_DIR / "ratings" / "experiment-uncapped"
EXPERIMENT_VERSION = rm.MODEL_VERSION + " + scale-uncapped-experiment 0.1"
LAST_SLOPE = (93.0 - 89.5) / (92.0 - 80.0)
EXTRA_KNOT = (92.0 + (99.0 - 93.0) / LAST_SLOPE, 99.0)


def patch():
    """Apply the experiment's scale to the imported rating_model module (process-local)."""
    rm.BAND_KNOTS = rm.BAND_KNOTS + (EXTRA_KNOT,)
    rm.SCALE_MAX = 99.0
    rm.BANDS = (("EXCEPTIONAL", 94, 99),) + tuple(b if b[0] != "ELITE" else ("ELITE", 90, 93) for b in rm.BANDS)
    rm.MODEL_VERSION = EXPERIMENT_VERSION


def load(path):
    return {r["player_id"]: r for r in json.loads(path.read_text(encoding="utf-8"))["players"]}


def compare(base_dir, exp_dir):
    base = load(base_dir / f"{rp.OUTPUT_STEM}.json")
    exp = load(exp_dir / f"{rp.OUTPUT_STEM}.json")
    same_ids = set(base) == set(exp)
    changed = [pid for pid in base if pid in exp and (base[pid]["overall_rating"], base[pid]["potential"]) !=
               (exp[pid]["overall_rating"], exp[pid]["potential"])]
    def dist(records, field):
        return dict(sorted(Counter(r[field] for r in records.values() if r[field] is not None and r[field] >= 88).items()))
    out = {
        "experiment_version": EXPERIMENT_VERSION,
        "extra_knot": [round(EXTRA_KNOT[0], 3), EXTRA_KNOT[1]],
        "same_player_set": same_ids,
        "players": len(exp),
        "records_changed": len(changed),
        "overall_changed": sum(1 for p in changed if base[p]["overall_rating"] != exp[p]["overall_rating"]),
        "potential_changed": sum(1 for p in changed if base[p]["potential"] != exp[p]["potential"]),
        "overall_max": [max(r["overall_rating"] for r in base.values()), max(r["overall_rating"] for r in exp.values())],
        "potential_max": [max(r["potential"] or 0 for r in base.values()), max(r["potential"] or 0 for r in exp.values())],
        "overall_88_plus": [dist(base, "overall_rating"), dist(exp, "overall_rating")],
        "potential_88_plus": [dist(base, "potential"), dist(exp, "potential")],
        "bands": [dict(Counter(r["band"] for r in base.values())), dict(Counter(r["band"] for r in exp.values()))],
        "raw_quality_above_92": sum(1 for r in exp.values() if (r["raw_quality"] or 0) > 92),
        "max_overall_change": max([abs(base[p]["overall_rating"] - exp[p]["overall_rating"]) for p in changed] or [0]),
        "potential_below_overall": sum(1 for r in exp.values() if r["potential"] is not None and r["potential"] < r["overall_rating"]),
    }
    (exp_dir / "scale_experiment_comparison.json").write_text(json.dumps(out, indent=1) + "\n", encoding="utf-8")
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=rp.DATA_DIR / "staging.sqlite3")
    parser.add_argument("--out", type=Path, default=EXPERIMENT_DIR)
    parser.add_argument("--compare", action="store_true", help="only compare existing outputs")
    args = parser.parse_args()
    if not args.compare:
        if args.out.resolve() == rp.OUT_DIR.resolve():
            raise SystemExit("refusing to overwrite the v2 outputs")
        args.out.mkdir(parents=True, exist_ok=True)
        patch()
        rp.run(args.db, rp.AS_OF, args.out, write_db=False)
    print(json.dumps(compare(rp.OUT_DIR, args.out), indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
