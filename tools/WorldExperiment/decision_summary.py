"""Summarize the decision-verification runs (HYBRID_WORLD_DECISION_VERIFICATION).

Reads data/world/decisions/runs/<variant>/seed-NNN.json (written by `WorldExperiment decide`) and writes
data/world/decisions/decision_summary.json and decision_tables.md: means with standard errors across seeds, paired
differences on shared seeds, and the integrity and determinism checks. Aggregates only.

    py -3.11 tools/WorldExperiment/decision_summary.py
"""
import json
import math
from pathlib import Path
import statistics as st
import sys

ROOT = Path(__file__).resolve().parents[2]
DECISIONS = ROOT / "tools" / "lol-data-ingestion" / "data" / "world" / "decisions"
RUNS = DECISIONS / "runs"
CHECKPOINTS = [0, 1, 3, 5, 10, 20]


def load():
    runs = {}
    for d in sorted(RUNS.iterdir()):
        if d.is_dir() and any(d.glob("seed-*.json")):
            runs[d.name] = {int(f.stem[5:]): json.loads(f.read_text(encoding="utf-8")) for f in sorted(d.glob("seed-*.json"))}
    return runs


def cp(run, season):
    return next(c for c in run["checkpoints"] if c["season"] == season)


def get(obj, path):
    for key in path.split("."):
        if key not in obj:
            return float("nan")   # an empty group (Reports.Summary writes only n = 0)
        obj = obj[key]
    return float(obj) if obj in ("NaN", "Infinity", "-Infinity") else obj   # System.Text.Json writes named literals as strings


def stat(values):
    v = [x for x in values if x is not None and not (isinstance(x, float) and math.isnan(x))]
    if not v:
        return {"n": 0}
    mean = st.fmean(v)
    se = st.stdev(v) / math.sqrt(len(v)) if len(v) > 1 else float("nan")
    return {"n": len(v), "mean": round(mean, 3), "se": round(se, 3), "min": round(min(v), 3), "max": round(max(v), 3)}


def fmt(s, digits=1):
    if s.get("n", 0) == 0:
        return "—"
    if s["n"] == 1 or math.isnan(s["se"]):
        return f"{s['mean']:.{digits}f}"
    return f"{s['mean']:.{digits}f} ± {s['se']:.{digits}f}"


def series_of(runs, variant, path, seasons=CHECKPOINTS, scale=1.0):
    return {c: stat([get(cp(r, c), path) * scale for r in runs[variant].values()]) for c in seasons}


def paired(runs, a, b, path, season):
    seeds = sorted(set(runs[a]) & set(runs[b]))
    diffs = [get(cp(runs[b][s], season), path) - get(cp(runs[a][s], season), path) for s in seeds]
    return stat(diffs)


def flows(run, season, key):
    return run_flows(run, season).get(key, 0)


def run_flows(run, season):
    return cp(run, season)["flows_cumulative"]


def report_tables(runs, out):
    """Curated tables for HYBRID_WORLD_DECISION_VERIFICATION.md (report_tables.md)."""
    L = []

    def tab(title, header, rows):
        L.append(f"### {title}")
        L.append("")
        L.append("| " + " | ".join(header) + " |")
        L.append("|" + "|".join("---" if i == 0 else "---:" for i in range(len(header))) + "|")
        for r in rows:
            L.append("| " + " | ".join(str(x) for x in r) + " |")
        L.append("")

    def n(v):
        return len(runs.get(v, {}))

    def m(s, dg=1):
        if s.get("n", 0) == 0 or (isinstance(s.get("mean"), float) and math.isnan(s["mean"])):
            return "—"
        if s["n"] < 2 or math.isnan(s["se"]):
            return f"{s['mean']:.{dg}f}"
        return f"{s['mean']:.{dg}f} ± {s['se']:.{dg}f}"

    def j(values):
        return " / ".join(values)

    d = out["dh1"]
    pol = [v for v in ("A", "B", "BO", "C") if v in d]
    rows = []
    for key, label in (("fielding_five", "fielding five starters"), ("authentic_five", "fielding five real starters"), ("active", "still competing")):
        for v in pol:
            rows.append([f"{v} ({n(v)} seeds): {label}"] + [m(d[v][key][c]) for c in CHECKPOINTS])
    tab("D-H1: real teams of 210 (mean ± SE across seeds)", ["Variant / measure"] + [f"S{c}" for c in CHECKPOINTS], rows)
    tab("D-H1: real teams that have had an incomplete lineup (% of team-runs, cumulative)",
        ["Variant", "S1", "S3", "S5", "S10", "S20", "Never in 20 seasons", "Median first season"],
        [[v] + [d[v]["incomplete_by_season_pct"][c] for c in (1, 3, 5, 10, 20)]
         + [d[v]["never_incomplete_teams_pct"], d[v]["median_first_incomplete"] if d[v]["median_first_incomplete"] < 99 else "> 20"] for v in pol])
    rows = []
    for v in pol:
        for t in "abcd":
            rows.append([v, t] + [d[v]["incomplete_by_tier_pct"][t][c] for c in (1, 3, 5, 10, 20)])
    tab("D-H1: ever incomplete by tier (%; tier a 47, b 7, c 70, d 86 teams)", ["Variant", "Tier", "S1", "S3", "S5", "S10", "S20"], rows)

    def rec(v, k, dg=0):
        return m(d[v]["recruitment_20"][k], dg)

    def why(v, k):
        return m(d[v]["unfilled_reasons_20"].get(k, {"n": 0}), 0)

    tab("D-H1: recruitment and consequences over 20 seasons (per run)", ["Measure"] + pol, [
        ["Real vacancies filled with a real player"] + [rec(v, "real_starter_filled_real") for v in pol],
        ["Real vacancies filled with an NPC (market signing)"] + [rec(v, "real_starter_filled_npc") for v in pol],
        ["Real vacancies left unfilled (team-role-windows)"] + [rec(v, "real_starter_unfilled") for v in pol],
        ["  no allowed candidate exists"] + [why(v, "none") for v in pol],
        ["  candidates held by teams it may not buy from"] + [why(v, "protected") for v in pol],
        ["  candidates unwilling (level or reservation wage)"] + [why(v, "unwilling") for v in pol],
        ["  over the wage cap / fee above funds"] + [f"{why(v, 'wage')} / {why(v, 'fee')}" for v in pol],
        ["Upgrades (real / NPC)"] + [f"{rec(v, 'real_upgrades_real')} / {rec(v, 'real_upgrades_npc')}" for v in pol],
        ["Real-to-real transfers"] + [rec(v, "transfer_real") for v in pol],
        ["Transfer fees paid by real teams (x100 minor units)"] + [rec(v, "real_buyer_fees") for v in pol],
        ["Organizations withdrawn"] + [rec(v, "real_teams_withdrawn", 1) for v in pol],
        ["Real players retired"] + [rec(v, "retired_real") for v in pol],
        ["Series won by the stronger real team (%)"] + [m(d[v]["stronger_team_won_pct"], 1) for v in pol]])
    tab("D-H1: state at S5 / S10 / S20", ["Measure"] + pol, [
        ["Real players active"] + [j([m(d[v]["real_players_active"][c], 0) for c in (5, 10, 20)]) for v in pol],
        ["NPC starters on real teams"] + [j([m(d[v]["npc_starters"][c], 0) for c in (5, 10, 20)]) for v in pol],
        ["Mean strength, real tier a (an empty slot counts 25)"] + [j([m(d[v]["strength_a"][c]) for c in (5, 10, 20)]) for v in pol],
        ["Mean strength, all real teams"] + [j([m(d[v]["mean_strength"][c]) for c in (5, 10, 20)]) for v in pol],
        ["Mean strength, fictional professional teams"] + [j([m(d[v]["fictional_pro_strength"][c]) for c in (5, 10, 20)]) for v in pol],
        ["Real leagues (of 31) with 4+ active teams"] + [j([m(d[v]["leagues_4_plus"][c], 1) for c in (5, 10, 20)]) for v in pol],
        ["Real teams with negative funds"] + [j([m(d[v]["negative_funds"][c], 1) for c in (5, 10, 20)]) for v in pol],
        ["Real teams: season wages / revenue"] + [j([m(d[v]["wage_to_revenue"][c], 2) for c in (5, 10, 20)]) for v in pol],
        ["NPCs rated 75+ employed by real teams"] + [j([m(d[v]["npc_75_on_real"][c], 0) for c in (5, 10, 20)]) for v in pol],
        ["NPC free agents after the window (%)"] + [j([m(d[v]["npc_fa_pct"][c], 1) for c in (5, 10, 20)]) for v in pol],
        ["Pre-window free agents rated 70+"] + [j([m(d[v]["pre_window_70"][c], 0) for c in (5, 10, 20)]) for v in pol]])
    if "A" in d:
        tab("D-H1: real vacancies by role under A (S5 / S10)", ["Role", "Vacancies"],
            [[r, j([m(d["A"]["vacancy_by_role"][c][r], 0) for c in (5, 10)])] for r in ("top", "jungle", "mid", "bot", "support")])

    e = out["e2"]
    ev = [v for v in ("A", "B", "B-all", "B-bounded", "B-fixed", "B-path", "C") if v in e]

    def es(v, k, dg=1, seasons=(1, 5, 10, 20)):
        return j([m(e[v][k][c], dg) for c in seasons])

    tab("E2/F1: population and market (S1 / S5 / S10 / S20)", ["Measure"] + [f"{v} ({n(v)})" for v in ev], [
        ["Active NPCs"] + [es(v, "npc_active", 0) for v in ev],
        ["Entrants = NPC retirements, every season"] + [str(e[v]["intake_equals_retirements"]) for v in ev],
        ["NPC retirements per season"] + [j([m(e[v]["retired_per_season"][c], 0) for c in (1, 5, 10, 20)]) for v in ev],
        ["Entrants per season"] + [j([m(e[v]["intake_per_season"][c], 0) for c in (1, 5, 10, 20)]) for v in ev],
        ["NPC free agents after the window (%)"] + [es(v, "npc_fa_pct") for v in ev],
        ["  range over every season and seed (%)"] + [f"{e[v]['fa_pct_range'][0]}–{e[v]['fa_pct_range'][1]}" for v in ev],
        ["  seasons above the 12% warning (per run)"] + [m(e[v]["warnings_over_12"], 2) for v in ev],
        ["NPC free agents as the window opens (%)"] + [es(v, "npc_fa_pct_pre") for v in ev],
        ["Pre-window free agents rated 70+"] + [es(v, "pre_70", 0) for v in ev],
        ["Pre-window free agents with potential 85+"] + [es(v, "pre_pot85", 0) for v in ev],
        ["Smallest role pool after the window"] + [es(v, "fa_min_role", 0) for v in ev],
        ["Free agents unemployed 2+ seasons"] + [es(v, "fa_long", 0) for v in ev],
        ["Mean asking wage of free agents (per day)"] + [es(v, "asking_wage", 0) for v in ev],
        ["Vacant squad seats, fictional layers"] + [es(v, "vacant_seats", 0) for v in ev],
        ["Fictional teams without five starters"] + [es(v, "teams_without_five", 0) for v in ev],
        ["  of which fictional professional"] + [es(v, "pro_without_five", 2) for v in ev],
        ["Starter vacancies filled (% cumulative)"] + [j([m(e[v]["hiring"][c], 1) for c in (1, 5, 10, 20)]) for v in ev],
        ["NPC mean age"] + [es(v, "age_mean", 2) for v in ev],
        ["NPC aged 16-21 (%)"] + [es(v, "young_share") for v in ev],
        ["NPC aged 28+ (%)"] + [es(v, "veteran_share") for v in ev],
        ["NPC mean Overall"] + [es(v, "npc_overall_mean", 2) for v in ev],
        ["NPC top 50 by role"] + [es(v, "npc_top50", 2) for v in ev],
        ["Fictional professional starters, mean Overall"] + [es(v, "fp_starter_overall", 2) for v in ev],
        ["Semi-professional starters, mean Overall"] + [es(v, "sp_starter_overall", 2) for v in ev]])

    r1 = out["d1"]
    p1 = out["d1_paired"]
    dv = [v for v in ("B", "D1t", "D1bt", "D1a", "D1b") if v in r1]

    def ds(v, k, c, dg=2):
        return m(r1[v][k][c], dg)

    # display names: D1t is the D1a candidate (the tail), D1a/D1b the wider-room sensitivity
    show = {"B": "B baseline", "D1t": "D1a: tail", "D1bt": "D1a tail + D1b K96", "D1a": "Sensitivity: room 14", "D1b": "Room 14 + D1b K96"}
    tab("D1: ratings in the world (mean per run)", ["Measure"] + [f"{show.get(v, v)} ({n(v)})" for v in dv], [
        ["NPC potential 95+ / 97+ / 99 at creation"] + [j([ds(v, k, 0) for k in ("npc_potential_95_plus", "npc_potential_97_plus", "npc_potential_99")]) for v in dv],
        ["Every NPC created by S20 (cohort + entrants): potential 97+ / 99"] + [j([ds(v, "created_npc_initial_potential_97_plus", 20), ds(v, "created_npc_initial_potential_99", 20)]) for v in dv],
        ["Max potential, S0 / S20"] + [j([ds(v, "max_potential", 0, 1), ds(v, "max_potential", 20, 1)]) for v in dv],
        ["Players 90+, S0 / S5 / S20"] + [j([ds(v, "count_90_plus", c, 1) for c in (0, 5, 20)]) for v in dv],
        ["Players 94+, S0 / S5 / S10 / S20"] + [j([ds(v, "count_94_plus", c) for c in (0, 5, 10, 20)]) for v in dv],
        ["Players 97+, S5 / S20"] + [j([ds(v, "count_97_plus", c) for c in (5, 20)]) for v in dv],
        ["Real players 94+, S0 / S5 / S10"] + [j([ds(v, "real_94_plus", c) for c in (0, 5, 10)]) for v in dv],
        ["Ever 94+ by S20: anyone / NPCs"] + [j([ds(v, "ever_94_plus", 20), ds(v, "npc_ever_94_plus", 20)]) for v in dv],
        ["Ever 97+ by S20"] + [ds(v, "ever_97_plus", 20) for v in dv],
        ["Max Overall, S5 / S20"] + [j([ds(v, "max_overall", 5, 1), ds(v, "max_overall", 20, 1)]) for v in dv],
        ["NPCs that ever passed 82, by S20"] + [ds(v, "npc_ever_above_82", 20, 0) for v in dv],
        ["NPCs created above 82 (cohort + every entrant)"] + [ds(v, "npc_initial_above_82", 20, 0) for v in dv],
        ["Young prospects (<= 21, potential 85+) that reached potential - 1 by S20 (%)"] + [ds(v, "prospect_cohort.reached_potential_pct", 20, 1) for v in dv],
        ["Overall p99 of active players, S0 / S20"] + [j([ds(v, "overall_all.p99", 0, 0), ds(v, "overall_all.p99", 20, 0)]) for v in dv],
        ["Top 50 by role, all players, S0 / S20"] + [j([m(r1[v]["top50_all"][0], 2), m(r1[v]["top50_all"][20], 2)]) for v in dv],
        ["Strongest real tier-a team, S0 / S5"] + [j([ds(v, "real_a_top_strength", 0, 1), ds(v, "real_a_top_strength", 5, 1)]) for v in dv],
        ["Mean contract wage of 90+ players, S1 (per day)"] + [ds(v, "salary_90_plus_mean", 1, 0) for v in dv],
        ["Mean fee of a transfer of an 88+ player (20 seasons)"] + [m(r1[v]["transfer_88_fee_mean"], 0) for v in dv],
        ["Series won by the stronger real team (%)"] + [m(r1[v]["stronger_won_pct"], 2) for v in dv]])
    keys = (("npc_potential_95_plus", 0), ("npc_potential_97_plus", 0), ("count_90_plus", 5), ("count_94_plus", 5), ("count_94_plus", 20),
            ("ever_94_plus", 20), ("npc_ever_94_plus", 20), ("real_94_plus", 5), ("prospect_cohort.reached_potential_pct", 20), ("salary_90_plus_mean", 1))
    names = {"npc_potential_95_plus": "NPCs with potential 95+", "npc_potential_97_plus": "NPCs with potential 97+",
             "count_90_plus": "Players 90+", "count_94_plus": "Players 94+", "ever_94_plus": "Players ever 94+",
             "npc_ever_94_plus": "NPCs ever 94+", "real_94_plus": "Real players 94+",
             "prospect_cohort.reached_potential_pct": "Young prospects reaching potential (%)", "salary_90_plus_mean": "Mean wage of 90+ players"}
    pair = {"D1t-B": "D1a tail − B", "D1bt-D1t": "D1b K96 (on the tail)", "D1a-B": "Room 14 − B", "D1b-D1a": "D1b K96 (on room 14)"}
    tab("D1: paired differences on shared seeds (mean ± SE)", ["Measure (season)"] + [pair.get(x, x) for x in p1],
        [[f"{names.get(k, k)} (S{c})"] + [m(p1[x][k][c], 2) for x in p1] for k, c in keys])
    (DECISIONS / "report_tables.md").write_text("\n".join(L) + "\n", encoding="utf-8")


def main():
    runs = load()
    out = {"variants": {k: sorted(v) for k, v in runs.items()}}
    lines = ["# Decision verification: run summary", "", "Means ± standard error across seeds. Generated by decision_summary.py.", ""]
    def table(title, header, rows):
        lines.append(f"## {title}"); lines.append("")
        lines.append("| " + " | ".join(header) + " |"); lines.append("|" + "---|" * len(header))
        for row in rows:
            lines.append("| " + " | ".join(str(x) for x in row) + " |")
        lines.append("")

    # ---------------- integrity and determinism
    integrity = {}
    for v, rs in runs.items():
        integrity[v] = {
            "seeds": len(rs),
            "initial_invariant_failures": sum(r["initial_invariant_failures"] for r in rs.values()),
            "checkpoint_invariant_failures": sum(c["invariant_failures"] for r in rs.values() for c in r["checkpoints"]),
            "history_violations_final": sum(r["checkpoints"][-1].get("history_violations", 0) for r in rs.values()),
            "conservation_violations": sum(r["conservation_violations"] for r in rs.values()),
            "max_registered": max(c["max_registered"] for r in rs.values() for c in r["checkpoints"]),
            "npc_initial_above_82": sum(r["checkpoints"][-1]["npc_initial_above_82"] for r in rs.values()),
            "max_initial_npc_overall": max(r["checkpoints"][-1]["npc_created_max_initial_overall"] for r in rs.values()),
            "potential_below_overall": sum(c["potential_below_overall"] for r in rs.values() for c in r["checkpoints"]),
            "real_rating_mismatches": sum(r["real_pack"]["rating_mismatches"] for r in rs.values()),
            "unique_ids": all(c["unique_person_ids"] for r in rs.values() for c in r["checkpoints"]),
            "season_seconds_mean": stat([r["season_seconds_mean"] for r in rs.values()]),
            "season_seconds_max": max(r["season_seconds_max"] for r in rs.values()),
            # medians and p95 over every simulated season: robust to the laptop's sleep (a few seasons took minutes)
            "season_seconds_median": round(st.median(x["seconds"] for r in rs.values() for x in r["series"]), 2),
            "season_seconds_p95": sorted(x["seconds"] for r in rs.values() for x in r["series"])[int(0.95 * sum(len(r["series"]) for r in rs.values()))],
            "seasons_over_60s": sum(1 for r in rs.values() for x in r["series"] if x["seconds"] > 60),
            "create_seconds_median": round(st.median(r["create_seconds"] for r in rs.values()), 2),
            "create_seconds": stat([r["create_seconds"] for r in rs.values()]),
            "total_seconds": stat([r["total_seconds"] for r in rs.values()]),
            "peak_working_set_mb_max": round(max(r["peak_working_set_bytes"] for r in rs.values()) / 1e6, 1),
        }
    out["integrity"] = integrity
    det = {}
    if "B-replay" in runs:
        det["replay_equal"] = {s: runs["B-replay"][s]["digest"] == runs["B"][s]["digest"] for s in runs["B-replay"] if s in runs["B"]}
    if "B-bounded" in runs:
        det["bounded_final_equal_to_B"] = {s: runs["B-bounded"][s]["digest"] == runs["B"][s]["digest"] for s in runs["B-bounded"] if s in runs["B"]}
    same0 = {}
    for v in runs:
        if v in ("B",):
            continue
        shared = sorted(set(runs[v]) & set(runs["B"]))
        same0[v] = sum(runs[v][s]["digest_s0"] == runs["B"][s]["digest_s0"] for s in shared), len(shared)
    det["season0_identical_to_B"] = same0
    out["determinism"] = det
    table("Integrity, determinism and run cost", ["Variant", "Seeds", "Invariant failures (start / checkpoints)", "History violations (end)",
          "Conservation violations", "Max registered", "NPC start > 82", "Max initial NPC", "Creation s (median)", "Season s (median / p95)", "Seasons > 60 s (sleep)", "Peak MB"],
          [[v, i["seeds"], f"{i['initial_invariant_failures']} / {i['checkpoint_invariant_failures']}", i["history_violations_final"], i["conservation_violations"],
            i["max_registered"], i["npc_initial_above_82"], i["max_initial_npc_overall"], i["create_seconds_median"],
            f"{i['season_seconds_median']} / {i['season_seconds_p95']}", i["seasons_over_60s"], i["peak_working_set_mb_max"]] for v, i in integrity.items()])
    lines.append(f"Determinism: {json.dumps(det)}"); lines.append("")

    # ---------------- D-H1
    dh1 = [v for v in ("A", "B", "C", "BO") if v in runs]
    d = {}
    for v in dh1:
        rs = runs[v]
        d[v] = {
            "active": series_of(runs, v, "real_teams.active"), "authentic_five": series_of(runs, v, "real_teams.authentic_five"),
            "fielding_five": series_of(runs, v, "real_teams.fielding_five"), "with_vacancy": series_of(runs, v, "real_teams.with_vacancy"),
            "never_incomplete": series_of(runs, v, "real_never_incomplete"), "npc_starters": series_of(runs, v, "real_teams.npc_starters"),
            "real_starter_share": series_of(runs, v, "real_starter_share_real"), "real_players_active": series_of(runs, v, "real_players_active"),
            "real_free_agents": series_of(runs, v, "real_free_agents"),
            "mean_strength": series_of(runs, v, "real_teams.mean_strength"), "real_a_mean_strength": series_of(runs, v, "real_a_mean_strength"),
            "fictional_pro_strength": series_of(runs, v, "fictional_pro_mean_strength"),
            "leagues_4_plus": series_of(runs, v, "real_leagues_with_4_plus"), "leagues_2_plus": series_of(runs, v, "real_leagues_with_2_plus"),
            "negative_funds": series_of(runs, v, "real_teams.negative_funds"), "wage_to_revenue": series_of(runs, v, "real_teams.wage_to_revenue"),
            "mean_funds_a": series_of(runs, v, "real_by_tier.a.mean_funds"),
            "npc_75_on_real": series_of(runs, v, "npc_75_plus_on_real_teams"), "pre_window_70": series_of(runs, v, "pre_window_70_plus"),
            "npc_fa_pct": series_of(runs, v, "npc_fa_pct"), "npc_in_real_squads": series_of(runs, v, "real_teams.npc_in_squads"),
            "mean_squad": series_of(runs, v, "real_teams.mean_squad"),
        }
        for tier in ("a", "b", "c", "d"):
            d[v][f"fielding_five_{tier}"] = series_of(runs, v, f"real_by_tier.{tier}.fielding_five")
            d[v][f"authentic_five_{tier}"] = series_of(runs, v, f"real_by_tier.{tier}.authentic_five")
            d[v][f"active_{tier}"] = series_of(runs, v, f"real_by_tier.{tier}.active")
            d[v][f"strength_{tier}"] = series_of(runs, v, f"real_by_tier.{tier}.mean_strength")
        # recruitment, cumulative over 20 seasons
        last = max(CHECKPOINTS)
        rec = {k: stat([run_flows(r, last).get(k, 0) for r in rs.values()]) for k in
               ("real_starter_filled_real", "real_starter_filled_npc", "real_starter_unfilled", "real_npc_fallback_signed", "real_upgrades_real",
                "real_upgrades_npc", "transfer_real", "real_buyer_fees", "real_teams_withdrawn", "real_withdrawal_players", "retired_real")}
        reasons = {}
        for r in rs.values():
            for k, n in run_flows(r, last).items():
                if k.startswith("real_unfilled_"):
                    _, _, role, why = k.split("_", 3)
                    reasons.setdefault(why, []).append(0)
        for why in list(reasons):
            reasons[why] = stat([sum(n for k, n in run_flows(r, last).items() if k.startswith("real_unfilled_") and k.endswith("_" + why)) for r in rs.values()])
        roles = {c: {role: stat([cp(r, c)["real_vacancy_by_role"][role] for r in rs.values()]) for role in ("top", "jungle", "mid", "bot", "support")} for c in (5, 10, 20)}
        stronger = stat([run_flows(r, last).get("real_series_stronger_won", 0) / max(1, run_flows(r, last).get("real_series_unequal", 0)) * 100 for r in rs.values()])
        first = [x for r in rs.values() for x in r["first_incomplete_season"]]
        tiers = [t for r in rs.values() for t in r["real_team_tiers"]]
        incomplete_by = {c: round(100 * sum(1 for x in first if 0 < x <= c) / len(first), 1) for c in (1, 3, 5, 10, 20)}
        by_tier = {tier: {c: round(100 * sum(1 for x, t in zip(first, tiers) if t == tier and 0 < x <= c) / max(1, sum(1 for t in tiers if t == tier)), 1)
                          for c in (1, 3, 5, 10, 20)} for tier in ("a", "b", "c", "d")}
        never = [x for x in first if x == 0]
        median_first = st.median([x if x > 0 else 99 for x in first])
        d[v]["recruitment_20"] = rec; d[v]["unfilled_reasons_20"] = reasons; d[v]["vacancy_by_role"] = roles
        d[v]["stronger_team_won_pct"] = stronger; d[v]["incomplete_by_season_pct"] = incomplete_by; d[v]["incomplete_by_tier_pct"] = by_tier
        d[v]["never_incomplete_teams_pct"] = round(100 * len(never) / len(first), 1); d[v]["median_first_incomplete"] = median_first
        fn = [x for r in rs.values() for x in r["first_npc_starter_season"]]
        d[v]["npc_starter_by_season_pct"] = {c: round(100 * sum(1 for x in fn if 0 < x <= c) / len(fn), 1) for c in (1, 3, 5, 10, 20)}
    out["dh1"] = d
    for key, title in (("fielding_five", "Real teams fielding five starters (of 210)"), ("authentic_five", "Real teams fielding five REAL starters (of 210)"),
                       ("active", "Real teams still competing (of 210)"), ("npc_starters", "NPC starters on real teams"),
                       ("real_starter_share", "Share of real-team starting slots held by real players (%)"), ("mean_strength", "Mean real-team strength (vacancy = 25)"),
                       ("real_a_mean_strength", "Mean strength of real tier-a teams"), ("leagues_4_plus", "Real leagues (of 31) with 4+ active teams"),
                       ("real_players_active", "Real players active"), ("negative_funds", "Real teams with negative funds"),
                       ("npc_fa_pct", "NPC free agents after the window (%)"), ("npc_75_on_real", "NPCs rated 75+ employed by real teams"),
                       ("wage_to_revenue", "Real teams: season wages / revenue")):
        table(title, ["Variant"] + [f"S{c}" for c in CHECKPOINTS], [[v] + [fmt(d[v][key][c], 1 if key not in ("wage_to_revenue",) else 3) for c in CHECKPOINTS] for v in dh1])
    table("Real teams ever incomplete by season (% of team-runs; pooled over seeds)", ["Variant", "S1", "S3", "S5", "S10", "S20", "Never in 20", "Median first season"],
          [[v] + [d[v]["incomplete_by_season_pct"][c] for c in (1, 3, 5, 10, 20)] + [d[v]["never_incomplete_teams_pct"], d[v]["median_first_incomplete"]] for v in dh1])
    for v in dh1:
        table(f"{v}: ever incomplete by tier (%)", ["Tier", "S1", "S3", "S5", "S10", "S20"], [[t] + [d[v]["incomplete_by_tier_pct"][t][c] for c in (1, 3, 5, 10, 20)] for t in "abcd"])
        table(f"{v}: by tier at S5 / S10 / S20 (fielding five; authentic five; active; strength)", ["Tier", "Fielding five", "Authentic five", "Active", "Strength"],
              [[t, " / ".join(fmt(d[v][f'fielding_five_{t}'][c]) for c in (5, 10, 20)), " / ".join(fmt(d[v][f'authentic_five_{t}'][c]) for c in (5, 10, 20)),
                " / ".join(fmt(d[v][f'active_{t}'][c]) for c in (5, 10, 20)), " / ".join(fmt(d[v][f'strength_{t}'][c]) for c in (5, 10, 20))] for t in "abcd"])
    table("Recruitment by real teams over 20 seasons (cumulative per run)", ["Variant", "Vacancies filled (real)", "Filled (NPC)", "Unfilled", "Upgrades real / NPC",
          "Real-to-real transfers", "Fees paid by real teams (x100)", "Withdrawn", "Stronger team won (%)"],
          [[v, fmt(d[v]["recruitment_20"]["real_starter_filled_real"], 0), fmt(d[v]["recruitment_20"]["real_starter_filled_npc"], 0), fmt(d[v]["recruitment_20"]["real_starter_unfilled"], 0),
            f"{fmt(d[v]['recruitment_20']['real_upgrades_real'], 0)} / {fmt(d[v]['recruitment_20']['real_upgrades_npc'], 0)}", fmt(d[v]["recruitment_20"]["transfer_real"], 0),
            fmt(d[v]["recruitment_20"]["real_buyer_fees"], 0), fmt(d[v]["recruitment_20"]["real_teams_withdrawn"], 1), fmt(d[v]["stronger_team_won_pct"], 1)] for v in dh1])
    table("Why real vacancies stayed open (20 seasons, cumulative per run)", ["Variant"] + ["none", "protected", "fee", "wage", "unwilling", "squad_full"],
          [[v] + [fmt(d[v]["unfilled_reasons_20"].get(k, {"n": 0}), 0) for k in ("none", "protected", "fee", "wage", "unwilling", "squad_full")] for v in dh1])
    table("Real vacancies by role (S5 / S10 / S20)", ["Variant", "top", "jungle", "mid", "bot", "support"],
          [[v] + [" / ".join(fmt(d[v]["vacancy_by_role"][c][role], 0) for c in (5, 10, 20)) for role in ("top", "jungle", "mid", "bot", "support")] for v in dh1])

    # ---------------- E2/F1
    e2 = [v for v in ("B", "B-all", "B-bounded", "B-fixed", "B-path", "A", "C", "BO") if v in runs]
    e = {}
    for v in e2:
        rs = runs[v]
        def ser(key):
            return {s: stat([r["series"][s - 1][key] for r in rs.values()]) for s in (1, 3, 5, 10, 20)}
        e[v] = {"npc_active": series_of(runs, v, "npc_active"), "npc_fa_pct": series_of(runs, v, "npc_fa_pct"),
                "npc_fa_pct_pre": series_of(runs, v, "npc_fa_pct_pre_window"), "npc_fa": series_of(runs, v, "npc_fa"),
                "retired_per_season": ser("npc_retired"), "intake_per_season": ser("intake"),
                "fa_70": {c: stat([sum(x["70_plus"] for x in cp(r, c)["fa_by_role"].values()) for r in rs.values()]) for c in CHECKPOINTS},
                "fa_min_role": {c: stat([min(x["all"] for x in cp(r, c)["fa_by_role"].values()) for r in rs.values()]) for c in CHECKPOINTS},
                "fa_pot85": series_of(runs, v, "fa_potential_85_plus"), "pre_70": series_of(runs, v, "pre_window_70_plus"),
                "pre_pot85": series_of(runs, v, "pre_window_potential_85_plus"),
                "fa_long": {c: stat([cp(r, c)["fa_unemployed_seasons"].get("2", 0) + cp(r, c)["fa_unemployed_seasons"].get("3+", 0) for r in rs.values()]) for c in CHECKPOINTS},
                "asking_wage": series_of(runs, v, "fa_mean_asking_wage"),
                "vacant_seats": {c: stat([sum(cp(r, c)["vacant_squad_seats"].values()) for r in rs.values()]) for c in CHECKPOINTS},
                "teams_without_five": {c: stat([sum(cp(r, c)["teams_without_five"].values()) for r in rs.values()]) for c in CHECKPOINTS},
                "amateur_without_five": series_of(runs, v, "teams_without_five.Amateur"),
                "pro_without_five": series_of(runs, v, "teams_without_five.FictionalPro"),
                "age_mean": series_of(runs, v, "npc_age.mean"), "age_p90": series_of(runs, v, "npc_age.p90"), "age_p10": series_of(runs, v, "npc_age.p10"),
                "young_share": series_of(runs, v, "npc_stage_share.16-21"), "veteran_share": series_of(runs, v, "npc_stage_share.28+"),
                "npc_overall_mean": series_of(runs, v, "overall_npc.mean"), "fp_starter_overall": series_of(runs, v, "fictional_pro_starter_overall_mean"),
                "fp_starter_age": series_of(runs, v, "fictional_pro_starter_age_mean"), "sp_starter_overall": series_of(runs, v, "semi_pro_starter_overall_mean"),
                "npc_top50": {c: stat([st.fmean(cp(r, c)["top50_by_role_npc"].values()) for r in rs.values()]) for c in CHECKPOINTS},
                "hiring": {c: stat([100 * run_flows(r, c).get("starter_filled", 0) / max(1, run_flows(r, c).get("starter_filled", 0) + run_flows(r, c).get("starter_unfilled", 0) + run_flows(r, c).get("real_starter_unfilled", 0)) for r in rs.values()]) for c in (1, 5, 10, 20)},
                "reserve_fill": {c: stat([100 * run_flows(r, c).get("reserve_filled", 0) / max(1, run_flows(r, c).get("reserve_filled", 0) + run_flows(r, c).get("reserve_unfilled", 0)) for r in rs.values()]) for c in (1, 5, 10, 20)},
                "warnings_over_12": stat([sum(1 for x in r["series"] if x["npc_fa_pct_post"] > 12) for r in rs.values()]),
                "fa_pct_range": [round(min(x["npc_fa_pct_post"] for r in rs.values() for x in r["series"]), 2), round(max(x["npc_fa_pct_post"] for r in rs.values() for x in r["series"]), 2)],
                "intake_equals_retirements": all(x["intake"] == x["npc_retired"] for r in rs.values() for x in r["series"]),
                "intake_equals_all_retirements": all(x["intake"] == x["npc_retired"] + x["real_retired"] for r in rs.values() for x in r["series"]),
                }
        if v == "B-path":
            e[v]["pathways_20"] = {k: stat([run_flows(r, 20).get(k, 0) for r in rs.values()]) for k in ("intake_UNATTACHED_PROSPECT", "intake_AMATEUR_SCENE", "intake_SEMI_PRO_SCENE")}
    out["e2"] = e
    for key, title, dg in (("npc_active", "Active NPCs", 0), ("npc_fa_pct", "NPC free agents after the window (%)", 2), ("npc_fa_pct_pre", "NPC free agents as the window opens (%)", 1),
                           ("fa_70", "Unsigned NPC free agents rated 70+ (after the window)", 1), ("pre_70", "NPC free agents rated 70+ as the window opens", 1),
                           ("pre_pot85", "NPC free agents with potential 85+ as the window opens", 1), ("fa_min_role", "Smallest role pool of unsigned free agents", 1),
                           ("fa_long", "Free agents unemployed 2+ seasons", 1), ("asking_wage", "Mean asking wage of free agents (per day)", 0),
                           ("vacant_seats", "Vacant squad seats (fictional layers)", 1), ("teams_without_five", "Fictional teams without five starters", 1),
                           ("pro_without_five", "Fictional professional teams without five", 2),
                           ("age_mean", "NPC mean age", 2), ("age_p90", "NPC age p90", 1), ("young_share", "NPC aged 16-21 (%)", 1), ("veteran_share", "NPC aged 28+ (%)", 1),
                           ("npc_overall_mean", "NPC mean Overall", 2), ("npc_top50", "NPC top 50 by role (mean of roles)", 2),
                           ("fp_starter_overall", "Fictional professional starters: mean Overall", 2), ("fp_starter_age", "Fictional professional starters: mean age", 2),
                           ("sp_starter_overall", "Semi-professional starters: mean Overall", 2)):
        table(title, ["Variant"] + [f"S{c}" for c in CHECKPOINTS], [[v] + [fmt(e[v][key][c], dg) for c in CHECKPOINTS] for v in e2])
    table("Retirements and intake per season (NPC)", ["Variant", "Retired S1/S5/S10/S20", "Intake S1/S5/S10/S20", "Intake = retirements every season", "FA % range (all seasons, all seeds)", "Seasons above 12% (per run)"],
          [[v, " / ".join(fmt(e[v]["retired_per_season"][s], 0) for s in (1, 5, 10, 20)), " / ".join(fmt(e[v]["intake_per_season"][s], 0) for s in (1, 5, 10, 20)),
            e[v]["intake_equals_retirements"], e[v]["fa_pct_range"], fmt(e[v]["warnings_over_12"], 2)] for v in e2])
    table("Hiring: starter vacancies filled / reserve seats filled (% cumulative)", ["Variant", "Starters S1/S5/S10/S20", "Reserves S1/S5/S10/S20"],
          [[v, " / ".join(fmt(e[v]["hiring"][c], 1) for c in (1, 5, 10, 20)), " / ".join(fmt(e[v]["reserve_fill"][c], 1) for c in (1, 5, 10, 20))] for v in e2])

    # ---------------- D1
    # D1t: the D1a candidate (tail reaches 99, ceilings below 96 unchanged); D1bt: D1t + the K96 elite re-knot.
    # D1a: sensitivity (wider soft room 14); D1b: D1a + K96.
    d1 = [v for v in ("B", "D1t", "D1bt", "D1a", "D1b") if v in runs]
    keys = ["count_90_plus", "count_94_plus", "count_97_plus", "real_90_plus", "real_94_plus", "npc_90_plus", "npc_94_plus", "npc_97_plus", "ever_94_plus",
            "ever_97_plus", "npc_ever_94_plus", "npc_ever_above_82", "npc_initial_above_82", "max_overall", "max_potential", "npc_potential_90_plus", "npc_potential_95_plus",
            "npc_potential_97_plus", "npc_potential_99", "created_npc_initial_potential_95_plus", "created_npc_initial_potential_97_plus",
            "created_npc_initial_potential_99", "near_potential_85_plus", "prospect_cohort.reached_potential_pct", "prospect_cohort.mean_gain_best",
            "prospect_cohort.best_max", "salary_90_plus_mean", "asking_wage_90_plus_mean", "real_a_top_strength", "real_a_mean_strength", "overall_all.p99",
            "overall_all.mean", "overall_real.max", "overall_npc.max", "potential_npc.p99"]
    r1 = {v: {k: series_of(runs, v, k) for k in keys} for v in d1}
    for v in d1:
        rs = runs[v]
        r1[v]["top50_all"] = {c: stat([st.fmean(cp(r, c)["top50_by_role_all"].values()) for r in rs.values()]) for c in CHECKPOINTS}
        r1[v]["transfer_88_fee_mean"] = stat([run_flows(r, 20).get("transfer_88_plus_fees", 0) * 100 / max(1, run_flows(r, 20).get("transfer_88_plus", 0)) for r in rs.values()])
        r1[v]["stronger_won_pct"] = stat([run_flows(r, 20).get("real_series_stronger_won", 0) / max(1, run_flows(r, 20).get("real_series_unequal", 0)) * 100 for r in rs.values()])
    pairs = {}
    for a, b in (("B", "D1t"), ("D1t", "D1bt"), ("B", "D1a"), ("D1a", "D1b")):
        if a in runs and b in runs:
            pairs[f"{b}-{a}"] = {k: {c: paired(runs, a, b, k, c) for c in CHECKPOINTS} for k in keys}
    out["d1"] = r1; out["d1_paired"] = pairs
    for key in keys:
        table(f"D1: {key}", ["Variant"] + [f"S{c}" for c in CHECKPOINTS], [[v] + [fmt(r1[v][key][c], 2) for c in CHECKPOINTS] for v in d1]
              + [[f"paired {p}"] + [fmt(pairs[p][key][c], 2) for c in CHECKPOINTS] for p in pairs])
    table("D1: top 50 by role, all players (mean of roles)", ["Variant"] + [f"S{c}" for c in CHECKPOINTS], [[v] + [fmt(r1[v]["top50_all"][c], 2) for c in CHECKPOINTS] for v in d1])
    table("D1: market and competition (20 seasons)", ["Variant", "Mean fee of a transfer of an 88+ player", "Stronger real team won (%)"],
          [[v, fmt(r1[v]["transfer_88_fee_mean"], 0), fmt(r1[v]["stronger_won_pct"], 2)] for v in d1])

    report_tables(runs, out)
    (DECISIONS / "decision_summary.json").write_text(json.dumps(out, indent=1, default=str) + "\n", encoding="utf-8")
    (DECISIONS / "decision_tables.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{sum(len(v) for v in runs.values())} runs in {len(runs)} variants -> {DECISIONS / 'decision_tables.md'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
