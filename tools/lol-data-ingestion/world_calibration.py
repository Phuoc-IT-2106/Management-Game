"""Aggregate calibration for the 4,000-NPC world experiment (offline): no person-level data leaves this script.

EXPERIMENT / RESEARCH ONLY. Reads the research ratings (rated players: MEASURED_ESTIMATE or LIMITED_EVIDENCE with at
least MIN_GAMES verified 2026 games) and the real-roster summary, and writes data/world/npc_calibration_2026.json:
  - per PandaScore tier (a, b, c, d, unknown): the count, Overall quantiles 1..99, mean and SD, and the age histogram;
  - per role: the mean Overall (role balance check);
  - per age 16..30: the research potential headroom quantiles (potential - Overall), where potential exists;
  - the real verified squads' size histogram.
Every value is an aggregate over at least MIN_CELL players, or is dropped. The NPC generator reads only this file.

    .venv\\Scripts\\python.exe world_calibration.py
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import statistics
import sys

DATA_DIR = Path(__file__).resolve().parent / "data"
MIN_GAMES = 10
MIN_CELL = 20
VERSION = "npc-calibration 0.1"


def quantiles(values, points=range(1, 100)):
    xs = sorted(values)
    n = len(xs)
    out = {}
    for p in points:
        pos = (n - 1) * p / 100
        lo = int(pos)
        hi = min(n - 1, lo + 1)
        out[str(p)] = round(xs[lo] + (xs[hi] - xs[lo]) * (pos - lo), 3)
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--ratings", type=Path, default=DATA_DIR / "ratings" / "experiment-uncapped" / "player_ratings_2023_2026.json")
    parser.add_argument("--rosters", type=Path, default=DATA_DIR / "world" / "real_roster_validation_summary.json")
    parser.add_argument("--out", type=Path, default=DATA_DIR / "world" / "npc_calibration_2026.json")
    args = parser.parse_args()
    payload = json.loads(args.ratings.read_text(encoding="utf-8"))
    rated = [r for r in payload["players"] if r["rating_evidence_class"] != "PRIOR_ONLY" and (r.get("games_2026") or 0) >= MIN_GAMES]
    tiers = defaultdict(list)
    for r in rated:
        tiers[r["primary_tier"] or "unknown"].append(r)
    out_tiers = {}
    for tier, rows in sorted(tiers.items()):
        if len(rows) < MIN_CELL:
            continue
        overall = [r["overall_rating"] for r in rows]
        ages = Counter(r["age"] for r in rows if r["age"] is not None)
        out_tiers[tier] = {"players": len(rows), "overall_mean": round(statistics.mean(overall), 2),
                           "overall_sd": round(statistics.pstdev(overall), 2), "overall_quantiles": quantiles(overall),
                           "with_age": sum(ages.values()),
                           "age_histogram": {str(a): n for a, n in sorted(ages.items()) if n >= 3}}
    roles = defaultdict(list)
    for r in rated:
        roles[r["primary_role"]].append(r["overall_rating"])
    headroom = defaultdict(list)
    for r in rated:
        if r["potential"] is not None and r["age"] is not None:
            headroom[r["age"]].append(r["potential"] - r["overall_rating"])
    rosters = json.loads(args.rosters.read_text(encoding="utf-8")) if args.rosters.exists() else {}
    out = {
        "format": "npc-calibration", "version": VERSION, "as_of": payload.get("as_of_date"),
        "rating_model_version": payload.get("rating_model_version"),
        "ratings_sha256": hashlib.sha256(args.ratings.read_bytes()).hexdigest(),
        "population": {"rule": f"MEASURED_ESTIMATE or LIMITED_EVIDENCE, >= {MIN_GAMES} verified 2026 games", "players": len(rated)},
        "tiers": out_tiers,
        "role_mean_overall": {k: round(statistics.mean(v), 2) for k, v in sorted(roles.items()) if len(v) >= MIN_CELL},
        "potential_headroom_by_age": {str(a): {"players": len(v), "quantiles": quantiles(v, (10, 25, 50, 75, 90)),
                                               "mean": round(statistics.mean(v), 2)}
                                      for a, v in sorted(headroom.items()) if len(v) >= MIN_CELL},
        "real_playable_squad_sizes": rosters.get("playable_squad_sizes"),
        "note": "Aggregates only (cells of at least %d players). Local research file; do not redistribute." % MIN_CELL,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(out, indent=1) + "\n", encoding="utf-8")
    print(json.dumps({k: out[k] for k in ("version", "as_of", "population", "role_mean_overall")}, indent=1))
    for t, v in out_tiers.items():
        q = v["overall_quantiles"]
        print(t, v["players"], "mean", v["overall_mean"], "sd", v["overall_sd"], "p5/25/50/75/95", q["5"], q["25"], q["50"], q["75"], q["95"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
