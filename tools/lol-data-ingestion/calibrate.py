"""Aggregate calibration report for the game's generated world (rules v4), from raw PandaScore snapshots.

Output: reports/calibration_2026.json (+ .md). Aggregate statistics only: distributions, shares and quantiles.
No person names, handles, birthdays or provider person ids; no team names (team-level Elo stays local in data/).
The report feeds world-generation parameters (rival strength spread, roster sizes, series formats, nationality and
age mixes, career exits and entries). It never makes a real person or organization part of the game.

Ages come from birthdays embedded in tournament rosters; their licence status is unconfirmed (API discovery report
section 3), so the report marks them `licence_unconfirmed` and consumers must be able to fall back to authored curves.
"""
import argparse
from collections import Counter, defaultdict
from datetime import datetime
import json
import math
from pathlib import Path

from build_rosters import roster_rows
from snapshots import SNAPSHOT_ROOT, completed_runs, load_records, provenance

REPORTS = Path(__file__).resolve().parent / "reports"
TIERS = ("s", "a", "b", "c", "d")
ROLES = ("top", "jun", "mid", "adc", "sup")
PLAYOFF_WORDS = ("playoff", "final", "knockout", "bracket", "elimination", "semifinal", "quarterfinal")
ELO_START, ELO_K, MIN_TEAM_MATCHES = 1500.0, 32.0, 8


def r4(x):
    return None if x is None else round(float(x), 4)


def quantile(values, q):
    """Linear-interpolated quantile of a non-empty list (type 7)."""
    v = sorted(values)
    if not v:
        return None
    pos = (len(v) - 1) * q
    lo, hi = math.floor(pos), math.ceil(pos)
    return v[lo] + (v[hi] - v[lo]) * (pos - lo)


def summary(values):
    if not values:
        return {"n": 0}
    mean = sum(values) / len(values)
    sd = math.sqrt(sum((x - mean) ** 2 for x in values) / len(values))
    return {"n": len(values), "mean": r4(mean), "sd": r4(sd), "p10": r4(quantile(values, .1)), "p50": r4(quantile(values, .5)),
            "p90": r4(quantile(values, .9)), "min": r4(min(values)), "max": r4(max(values))}


def shares(counter, top=None):
    total = sum(counter.values())
    if total == 0:
        return {}
    items = sorted(counter.items(), key=lambda kv: (-kv[1], str(kv[0])))
    out = {str(k): r4(v / total) for k, v in (items[:top] if top else items)}
    if top and len(items) > top:
        out["other"] = r4(sum(v for _, v in items[top:]) / total)
    return out


def playable(match):
    """A finished, decided two-team match with scores; forfeits and draws carry no strength evidence."""
    teams = [o.get("opponent", {}).get("id") for o in match.get("opponents") or []]
    scores = {r.get("team_id"): r.get("score") for r in match.get("results") or []}
    return (match.get("status") == "finished" and not match.get("forfeit") and not match.get("draw") and len(teams) == 2
            and None not in teams and teams[0] != teams[1] and match.get("winner_id") in teams
            and all(isinstance(scores.get(t), int) for t in teams))


def stage_kind(name):
    lowered = (name or "").lower()
    return "playoffs" if any(w in lowered for w in PLAYOFF_WORDS) else "regular"


def elo(matches):
    """Series-level Elo in chronological order. Returns final ratings, match counts, per-team tier tallies and the
    pre-match expectations (for a calibration check)."""
    ratings, counts, tiers, predictions = {}, Counter(), defaultdict(Counter), []
    ordered = sorted((m for m in matches if playable(m)), key=lambda m: (m.get("begin_at") or "", m["id"]))
    for m in ordered:
        a, b = (o["opponent"]["id"] for o in m["opponents"])
        ra, rb = ratings.get(a, ELO_START), ratings.get(b, ELO_START)
        expected = 1 / (1 + 10 ** ((rb - ra) / 400))
        won = 1.0 if m["winner_id"] == a else 0.0
        predictions.append((expected, won))
        ratings[a], ratings[b] = ra + ELO_K * (won - expected), rb - ELO_K * (won - expected)
        counts[a] += 1
        counts[b] += 1
        tier = (m.get("tournament") or {}).get("tier")
        if tier in TIERS:
            tiers[a][tier] += 1
            tiers[b][tier] += 1
    return ratings, counts, tiers, predictions, ordered


def team_tier(tally):
    """The tier a team mostly plays at; ties go to the higher tier."""
    if not tally:
        return None
    return min(tally, key=lambda t: (-tally[t], TIERS.index(t)))


def elo_section(matches):
    ratings, counts, tiers, predictions, ordered = elo(matches)
    eligible = {t for t, n in counts.items() if n >= MIN_TEAM_MATCHES}
    by_tier = defaultdict(list)
    for team in eligible:
        if (tier := team_tier(tiers[team])) is not None:
            by_tier[tier].append(ratings[team])
    medians = {t: quantile(v, .5) for t, v in by_tier.items()}
    gaps = {f"{TIERS[i]}-{TIERS[i + 1]}": r4(medians[TIERS[i]] - medians[TIERS[i + 1]])
            for i in range(len(TIERS) - 1) if TIERS[i] in medians and TIERS[i + 1] in medians}
    # Spread inside a league: what a single 8-team league looks like from top to bottom.
    league_teams = defaultdict(set)
    league_tier = {}
    for m in ordered:
        league = m.get("league_id")
        for o in m["opponents"]:
            league_teams[league].add(o["opponent"]["id"])
        league_tier.setdefault(league, (m.get("tournament") or {}).get("tier"))
    spreads = defaultdict(list)
    for league, teams in league_teams.items():
        values = sorted(ratings[t] for t in teams if t in eligible)
        if len(values) >= 6 and league_tier.get(league) in TIERS:
            spreads[league_tier[league]].append(values[-1] - values[0])
    # Calibration check on the later half: Brier score of the expectations versus outcomes.
    half = predictions[len(predictions) // 2:]
    brier = sum((e - w) ** 2 for e, w in half) / len(half) if half else None
    within = {t: {"top_minus_bottom": summary(v), "top_vs_bottom_win_probability": r4(1 / (1 + 10 ** (-quantile(v, .5) / 400)))}
              for t, v in sorted(spreads.items())}
    return {"method": f"series-level Elo, start {ELO_START:.0f}, K {ELO_K:.0f}, chronological; teams with >= {MIN_TEAM_MATCHES} decided matches",
            "matches_used": len(ordered), "teams_rated": len(eligible),
            "by_tier": {t: summary(sorted(v)) for t, v in sorted(by_tier.items(), key=lambda kv: TIERS.index(kv[0]))},
            "median_gap_between_tiers": gaps, "within_league_spread": within, "brier_later_half": r4(brier)}


def formats_section(matches):
    by_tier, sweeps, lengths = defaultdict(Counter), defaultdict(Counter), defaultdict(list)
    for m in matches:
        if not playable(m):
            continue
        tier = (m.get("tournament") or {}).get("tier") or "unknown"
        kind = stage_kind((m.get("tournament") or {}).get("name"))
        n = m.get("number_of_games")
        by_tier[f"{tier}:{kind}"][f"bo{n}"] += 1
        scores = sorted((r["score"] for r in m["results"]), reverse=True)
        if n in (3, 5):
            sweeps[f"bo{n}"][f"{scores[0]}-{scores[1]}"] += 1
        for g in m.get("games") or []:
            if g.get("finished") and isinstance(g.get("length"), int) and g["length"] > 0:
                lengths[tier].append(g["length"] / 60)
    return {"series_format_share_by_tier_and_stage": {k: shares(v) for k, v in sorted(by_tier.items())},
            "series_score_share": {k: shares(v) for k, v in sorted(sweeps.items())},
            "game_length_minutes_by_tier": {k: summary(v) for k, v in sorted(lengths.items())}}


AGE_BANDS = (("<=18", 0, 19), ("19-21", 19, 22), ("22-24", 22, 25), ("25-27", 25, 28), ("28+", 28, 200))


def age_at(row):
    """Age in years at the stage's start, from the embedded birthday; None when unknown or implausible."""
    if not row["birthday"] or not row["begin_at"]:
        return None
    try:
        age = (datetime.fromisoformat(row["begin_at"][:10]) - datetime.fromisoformat(row["birthday"][:10])).days / 365.2425
    except ValueError:
        return None
    return age if 13 <= age <= 45 else None


def team_splits(rows):
    """Each team's splits in start order: its first tournament stage in each series, with the players listed there."""
    first = {}
    for r in sorted(rows, key=lambda r: (r["team_id"], r["serie_id"] or 0, r["begin_at"] or "", r["tournament_id"])):
        split = first.setdefault((r["team_id"], r["serie_id"]), {"team": r["team_id"], "begin": r["begin_at"] or "", "tournament": r["tournament_id"],
                                                                 "tier": r["tier"], "players": {}})
        if r["tournament_id"] == split["tournament"]:
            split["players"][r["player_id"]] = r
    teams = defaultdict(list)
    for split in first.values():
        teams[split["team"]].append(split)
    for splits in teams.values():
        splits.sort(key=lambda s: s["begin"])
    return teams


def roster_section(rows):
    teams = defaultdict(set)
    roles = defaultdict(Counter)
    for r in rows:
        teams[(r["tournament_id"], r["team_id"], r["tier"])].add(r["player_id"])
        roles[(r["tournament_id"], r["team_id"])][r["role"] or "unknown"] += 1
    sizes = defaultdict(Counter)
    for (_, _, tier), players in teams.items():
        n = len(players)
        sizes[tier or "unknown"][str(n) if n <= 7 else "8+"] += 1
    per_role = defaultdict(list)
    for counter in roles.values():
        for role in ROLES:
            per_role[role].append(counter.get(role, 0))
    # Turnover between consecutive series (splits) of one team: share of the later roster not on the earlier one.
    turnover = defaultdict(list)
    for splits in team_splits(rows).values():
        for before, after in zip(splits, splits[1:]):
            if after["players"]:
                turnover[after["tier"] or "unknown"].append(len(after["players"].keys() - before["players"].keys()) / len(after["players"]))
    return {"roster_size_share_by_tier": {t: shares(c) for t, c in sorted(sizes.items())},
            "players_per_role_per_team": {role: summary(v) for role, v in per_role.items()},
            "turnover_between_splits_by_tier": {t: summary(v) for t, v in sorted(turnover.items())},
            "team_stages": len(teams)}


def careers_section(rows):
    """Career flow between a team's consecutive splits (rules v4 lifecycle and rival transfers), aggregates only.

    exit_after_split_by_age: of the players on a split the team followed with another, the share listed on no later
    roster in the data, by age band at the earlier split. It is exit from the observed rosters within one year: a
    proxy for retirement and for dropping below the observed tiers, not a measured retirement rate.
    incoming_source_share: players new to a team's later split, by whether the data saw them earlier with another team
    (a transfer) or not at all (new to the observed rosters); new_entrant_age summarises the latter's ages.
    """
    teams = team_splits(rows)
    starts = defaultdict(list)
    for splits in teams.values():
        for split in splits:
            for player in split["players"]:
                starts[player].append((split["begin"], split["team"]))
    exits, totals = Counter(), Counter()
    for splits in teams.values():
        for split in splits[:-1]:
            for player, row in split["players"].items():
                age = age_at(row)
                band = "unknown" if age is None else next(name for name, low, high in AGE_BANDS if low <= age < high)
                totals[band] += 1
                if not any(begin > split["begin"] for begin, _ in starts[player]):
                    exits[band] += 1
    transfers, entrants, entrant_ages = 0, 0, []
    for splits in teams.values():
        for before, after in zip(splits, splits[1:]):
            for player, row in after["players"].items():
                if player in before["players"]:
                    continue
                if any(begin < after["begin"] and team != after["team"] for begin, team in starts[player]):
                    transfers += 1
                else:
                    entrants += 1
                    if age_at(row) is not None:
                        entrant_ages.append(age_at(row))
    incoming = transfers + entrants
    return {"exit_after_split_by_age": {band: {"players": totals[band], "exit_share": r4(exits[band] / totals[band])}
                                        for band, _, _ in AGE_BANDS + (("unknown", 0, 0),) if totals[band]},
            "incoming_source_share": {"players": incoming, "from_another_team": r4(transfers / incoming) if incoming else None,
                                      "new_to_observed_rosters": r4(entrants / incoming) if incoming else None},
            "new_entrant_age": summary(entrant_ages) if entrant_ages else {"n": 0},
            "note": "Exit from the observed rosters within one year: a proxy for retirement and for leaving the observed tiers."}


def demographics_section(rows):
    latest = {}
    for r in sorted(rows, key=lambda r: (r["begin_at"] or "", r["tournament_id"])):
        latest[r["player_id"]] = r
    nationality = defaultdict(Counter)
    ages = defaultdict(list)
    with_birthday = 0
    for r in latest.values():
        nationality[r["region"] or "unknown"][r["nationality"] or "unknown"] += 1
        if r["birthday"] and r["begin_at"]:
            try:
                born = datetime.fromisoformat(r["birthday"][:10])
                at = datetime.fromisoformat(r["begin_at"][:10])
            except ValueError:
                continue
            age = (at - born).days / 365.2425
            if 13 <= age <= 45:
                with_birthday += 1
                ages[f"{r['tier']}:{r['role'] or 'unknown'}"].append(age)
                ages[f"all:{r['role'] or 'unknown'}"].append(age)
    histogram = Counter(int(a) for key, values in ages.items() if key.startswith("all:") for a in values)
    return {"players": len(latest),
            "nationality_share_by_region": {region: shares(c, top=10) for region, c in sorted(nationality.items())},
            "age": {"licence_status": "licence_unconfirmed: birthdays come from embedded roster objects; aggregate use only",
                    "coverage": r4(with_birthday / len(latest)) if latest else None,
                    "by_tier_and_role": {k: summary(v) for k, v in sorted(ages.items())},
                    "histogram_all_roles": {str(k): v for k, v in sorted(histogram.items())}}}


def density_section(matches):
    by_team_serie = defaultdict(list)
    for m in matches:
        if not playable(m) or not m.get("begin_at"):
            continue
        tier = (m.get("tournament") or {}).get("tier") or "unknown"
        for o in m["opponents"]:
            by_team_serie[(o["opponent"]["id"], m.get("serie_id"), tier)].append(m["begin_at"][:10])
    weekly = defaultdict(list)
    for (_, _, tier), days in by_team_serie.items():
        if len(days) >= 4:
            first, last = datetime.fromisoformat(min(days)), datetime.fromisoformat(max(days))
            weeks = max(1.0, (last - first).days / 7)
            weekly[tier].append(len(days) / weeks)
    return {"matches_per_week_by_tier": {t: summary(v) for t, v in sorted(weekly.items())}}


def build_report(tournaments, matches, sources):
    rows = roster_rows(tournaments)
    window = sorted(m["begin_at"][:10] for m in matches if m.get("begin_at"))
    return {"format": "lol-calibration-report", "version": 1,
            "status": "AGGREGATE STATISTICS ONLY: no person names, handles, birthdays, provider person ids or team names",
            "source": "PandaScore (Free plan): tournaments with expected rosters, past matches",
            "observed_window": {"from": window[0] if window else None, "to": window[-1] if window else None},
            "counts": {"tournaments": len(tournaments), "matches": len(matches), "roster_rows": len(rows)},
            "elo": elo_section(matches), "formats": formats_section(matches), "rosters": roster_section(rows),
            "demographics": demographics_section(rows), "careers": careers_section(rows), "density": density_section(matches), "sources": sources}


def markdown(report):
    e = report["elo"]
    lines = [f"# LoL calibration report {report['observed_window']['from']} – {report['observed_window']['to']}", "",
             report["status"] + ".", "", f"Counts: {report['counts']}.", "", "## Team Elo by tier", "",
             "| Tier | Teams | Median | P10 | P90 | SD |", "|---|---|---|---|---|---|"]
    for tier, s in e["by_tier"].items():
        lines.append(f"| {tier.upper()} | {s['n']} | {s.get('p50')} | {s.get('p10')} | {s.get('p90')} | {s.get('sd')} |")
    lines += ["", f"Median gaps between tiers: {e['median_gap_between_tiers']}.", "",
              f"Within-league spread (top minus bottom, median) and top-vs-bottom win probability: "
              f"{ {t: (v['top_minus_bottom'].get('p50'), v['top_vs_bottom_win_probability']) for t, v in e['within_league_spread'].items()} }.",
              "", f"Brier score of pre-match expectations, later half: {e['brier_later_half']}.", "",
              "## Formats", "", f"Series scores: {report['formats']['series_score_share']}.", "",
              "## Rosters", "", f"Roster size share by tier: {report['rosters']['roster_size_share_by_tier']}.", "",
              f"Turnover between splits by tier: { {t: v.get('p50') for t, v in report['rosters']['turnover_between_splits_by_tier'].items()} }.",
              "", "## Demographics", "", f"Players: {report['demographics']['players']}; age coverage {report['demographics']['age']['coverage']} "
              f"({report['demographics']['age']['licence_status']}).", "",
              f"Age histogram (all roles): {report['demographics']['age']['histogram_all_roles']}.", "",
              "## Careers", "", report["careers"]["note"], "",
              f"Exit after a split, by age band: { {b: v['exit_share'] for b, v in report['careers']['exit_after_split_by_age'].items()} }.", "",
              f"Players new to a team's later split: {report['careers']['incoming_source_share']}.", ""]
    return "\n".join(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--snapshot-root", type=Path, default=SNAPSHOT_ROOT)
    parser.add_argument("--year", type=int, default=2026, help="Tournaments running in, and matches starting in, this year")
    parser.add_argument("--output", type=Path, default=REPORTS / "calibration_2026.json")
    args = parser.parse_args()
    runs = completed_runs(args.snapshot_root)
    if not runs:
        raise SystemExit("No completed snapshot runs; run sync_pandascore.py --preset core-2026 first")
    year = str(args.year)
    tournaments = [t for t in load_records(runs, "tournaments") if year in ((t.get("begin_at") or "")[:4], (t.get("end_at") or "")[:4])]
    matches = [m for m in load_records(runs, "matches") if (m.get("begin_at") or "")[:4] == year]
    report = build_report(tournaments, matches, provenance(runs))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    # LF on every platform: the repository stores text with LF, and the content fixture pins this file's SHA-256.
    args.output.write_text(json.dumps(report, indent=2, sort_keys=True, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    args.output.with_suffix(".md").write_text(markdown(report) + "\n", encoding="utf-8", newline="\n")
    print(f"Calibration report: {args.output}")
