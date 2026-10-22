"""Real-team roster snapshot and validation (offline): verified starting fives, squads and ownership, as of one day.

EXPERIMENT / RESEARCH ONLY. Reads the staging database (PandaScore stage registrations, Leaguepedia participation as
normalized by build_performance.py) and the research ratings; writes git-ignored files under data/world/. Nothing is
fetched, and no source table is changed. The pack holds real names: never commit, publish or redistribute it
(PandaScore terms 6.3-6.4; DEC-031 proposal).

Rules (all AUTHORED and printed into the output):
  * A team enters when PandaScore registers it for a stage that began on or before the as-of day and ended in 2026.
    Its latest stage is the registered stage with the latest start (ties: later end, then id). Registration alone never
    counts as playing, and the provider's `active` flag and `current_team` are never read.
  * Evidence: the team's last EVIDENCE_GAMES primary games played on or before the as-of day and no earlier than its
    latest stage's start; when that stage has fewer than MIN_STAGE_GAMES games, the previous registered stage's start
    is used instead. A game counts for a team only through a HIGH team alias (pf_games team ids).
  * A starter for a role is the player with the most evidence games in that role (ties: the later appearance). The
    slot is verified only when: the seat's identity is a HIGH_CONFIDENCE_MATCH; the player played at least
    MIN_ROLE_GAMES games and MIN_ROLE_SHARE of the evidence games in that role; PandaScore registers the player on the
    team for the latest stage (or for the previous one when the evidence reaches it); and the player's authoritative
    employer is this team. Five verified slots with five distinct players make the team VERIFIED.
  * Authoritative employer (one per player): the team of the player's latest evidence appearance on or before the
    as-of day; a player who never appears is held by the team whose latest stage began last. Any other registration
    is kept as a secondary registration and gives no squad place.
  * Squad: the five starters and the other players registered on the team for its latest stage, authoritative here
    only. MAX_SQUAD registered players at most.
  * STALE: a verified team whose last evidence game is more than STALE_DAYS before the as-of day. It is reported, and
    kept out of the playable verified set.
  * National teams (leagues matching NATIONAL_TEAM_LEAGUE, e.g. the Asian Games) are not clubs: their registrations
    are kept as observations, their games never decide an employer, and they are never a playable team.
  * Home league: the league of most of the team's evidence-era primary games outside tier-s and national-team events
    (else the latest non-s stage's league); tier: that league's most common stage tier.
  * SUPERSEDED: a team that is not verified and whose players mostly (3 or more starters) are authoritative at one
    other team whose latest stage began later (a rebrand, a new provider entity, an event-only entity). Historical.
  * Academy affiliation: no source field (PandaScore has none; Leaguepedia team relations were not fetched). Name
    patterns are reported as INFERENCE candidates only and never become an affiliation.

    .venv\\Scripts\\python.exe build_real_rosters.py [--as-of 2026-10-13]
"""
import argparse
from collections import Counter, defaultdict
from datetime import date
import hashlib
import json
from pathlib import Path
import re
import sqlite3
import sys

DATA_DIR = Path(__file__).resolve().parent / "data"
AS_OF = date(2026, 10, 13)
EVIDENCE_GAMES = 10
MIN_STAGE_GAMES = 3
MIN_ROLE_GAMES = 3
MIN_ROLE_SHARE = 0.4
STALE_DAYS = 180
MAX_SQUAD = 16
HIGH = "HIGH_CONFIDENCE_MATCH"
ROLE_OF = {"top": "top", "jng": "jungle", "jun": "jungle", "mid": "mid", "bot": "bot", "adc": "bot", "sup": "support"}
ROLES = ("top", "jungle", "mid", "bot", "support")
VERSION = "real-roster-snapshot 0.1"
# National-team events: a national team is not an employer, and its games never decide a player's club.
NATIONAL_TEAM_LEAGUE = re.compile(r"asian games|nations|national", re.I)
ACADEMY_PATTERN = re.compile(r"\b(academy|challengers?|youth|rookies?|young|next|prime|juniors?)\b", re.I)


def rules():
    return {"version": VERSION, "evidence_games": EVIDENCE_GAMES, "min_stage_games": MIN_STAGE_GAMES,
            "min_role_games": MIN_ROLE_GAMES, "min_role_share": MIN_ROLE_SHARE, "stale_days": STALE_DAYS,
            "max_squad": MAX_SQUAD, "identity": HIGH}


def load_ratings(path):
    if not path.exists():
        return {}, None
    payload = json.loads(path.read_text(encoding="utf-8"))
    return {r["player_id"]: r for r in payload["players"] if r.get("player_id")}, payload.get("rating_model_version")


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None


def build(db, as_of, ratings):
    as_of_s = as_of.isoformat()
    stages = {}
    for tid, league, serie, name, tier, begin, end, kind in db.execute(
            "SELECT tournament_id, league_id, serie_id, name, tier, begin_at, end_at, type FROM ps_tournaments"):
        stages[tid] = {"id": tid, "league_id": league, "serie_id": serie, "name": name, "tier": tier,
                       "begin": (begin or "")[:10], "end": (end or "")[:10], "type": kind}
    leagues = dict(db.execute("SELECT league_id, name FROM ps_leagues"))
    national_leagues = {lid for lid, n in leagues.items() if n and NATIONAL_TEAM_LEAGUE.search(n)}
    teams_meta = {tid: {"name": n, "acronym": a, "location": loc} for tid, n, a, loc in db.execute(
        "SELECT team_id, name, acronym, location FROM ps_teams")}
    players_meta = {pid: {"handle": n, "nationality": nat, "provider_role": role} for pid, n, nat, role in db.execute(
        "SELECT player_id, name, nationality, role FROM ps_players")}

    registrations = defaultdict(lambda: defaultdict(dict))   # team -> stage -> player -> role
    national_teams = set()
    history = []
    for tid, team, pid, role in db.execute("SELECT tournament_id, team_id, player_id, role FROM ps_registrations"):
        s = stages.get(tid)
        if not s or not s["begin"] or s["begin"] > as_of_s or (s["end"] or "9999") < "2026-01-01":
            continue
        national = s["league_id"] in national_leagues
        if not national:
            registrations[team][tid][pid] = ROLE_OF.get(role or "", "")
        else:
            national_teams.add(team)
        history.append({"national_team_event": national, "team_id": team, "player_id": pid, "stage_id": tid, "stage": s["name"], "league_id": s["league_id"],
                        "tier": s["tier"], "begin": s["begin"], "end": s["end"], "registered_role": ROLE_OF.get(role or "", ""),
                        "source": "pandascore:ps_registrations"})

    # Primary 2026 games on or before the as-of day, through HIGH team aliases only.
    games = {}
    team_games = defaultdict(list)
    for key, gdate, ts, blue, red, stage, tier in db.execute(
            "SELECT game_key, game_date, game_ts, blue_ps_team_id, red_ps_team_id, ps_stage_id, ps_tier FROM pf_games "
            "WHERE is_primary = 1 AND year = 2026 AND game_date <= ?", (as_of_s,)):
        games[key] = {"date": gdate, "ts": ts or gdate, "Blue": blue, "Red": red, "stage": stage, "tier": tier}
        for side, team in (("Blue", blue), ("Red", red)):
            if team:
                team_games[team].append(key)
    seats = defaultdict(list)   # (game, side) -> seats
    for key, side, role, status, ps_id, ident, handle in db.execute(
            "SELECT pg.game_key, pg.side, pg.role, pg.identity_status, pg.ps_player_id, pg.identity_key, pg.handle "
            "FROM pf_player_games pg JOIN pf_games g USING(game_key) WHERE g.is_primary = 1 AND g.year = 2026 AND g.game_date <= ?",
            (as_of_s,)):
        seats[(key, side)].append({"role": ROLE_OF.get(role, role), "status": status, "ps": ps_id if status == HIGH else None,
                                   "identity": ident, "handle": handle})

    def team_seats(team, key):
        g = games[key]
        side = "Blue" if g["Blue"] == team else "Red"
        return seats.get((key, side), [])

    # Latest appearance per player (HIGH identities), for the authoritative employer.
    last_seen = {}
    for team, keys in team_games.items():
        if team in national_teams:
            continue
        for key in keys:
            for s in team_seats(team, key):
                if s["ps"]:
                    stamp = (games[key]["ts"], key)
                    if s["ps"] not in last_seen or stamp > last_seen[s["ps"]][0]:
                        last_seen[s["ps"]] = (stamp, team)

    def ordered_stages(team):
        return sorted(registrations[team], key=lambda t: (stages[t]["begin"], stages[t]["end"], t))

    latest_begin = {team: stages[ordered_stages(team)[-1]]["begin"] for team in registrations}
    employer = {}
    for team in sorted(registrations):
        latest = ordered_stages(team)[-1]
        for pid in registrations[team][latest]:
            if pid in last_seen:
                employer[pid] = last_seen[pid][1]
            elif pid not in employer or (latest_begin[team], team) > (latest_begin[employer[pid]], employer[pid]):
                employer[pid] = team

    out_teams, conflicts = [], []
    for team in sorted(registrations, key=lambda t: (len(t), t)):
        order = ordered_stages(team)
        latest = order[-1]
        previous = order[-2] if len(order) > 1 else None
        stage = stages[latest]
        domestic = next((stages[t] for t in reversed(order) if stages[t]["tier"] != "s"), stage)
        home_counts = Counter(stages[games[k]["stage"]]["league_id"] for k in team_games.get(team, [])
                              if games[k]["stage"] in stages and stages[games[k]["stage"]]["tier"] != "s"
                              and stages[games[k]["stage"]]["league_id"] not in national_leagues)
        home_league = max(home_counts, key=lambda lid: (home_counts[lid], lid)) if home_counts else domestic["league_id"]
        home_tiers = Counter(stages[t]["tier"] for t in order if stages[t]["league_id"] == home_league)
        home_tier = max(home_tiers, key=lambda x: (home_tiers[x], x or "")) if home_tiers else domestic["tier"]
        played = sorted(team_games.get(team, []), key=lambda k: (games[k]["ts"], k))
        window_start = stage["begin"]
        in_stage = [k for k in played if games[k]["date"] >= window_start]
        used_previous = False
        if len(in_stage) < MIN_STAGE_GAMES and previous:
            window_start, used_previous = stages[previous]["begin"], True
            in_stage = [k for k in played if games[k]["date"] >= window_start]
        evidence = in_stage[-EVIDENCE_GAMES:]
        registered = dict(registrations[team][latest])
        allowed = set(registered) | (set(registrations[team][previous]) if used_previous and previous else set())
        slots, reasons = [], []
        for role in ROLES:
            counts, latest_at, unresolved = Counter(), {}, Counter()
            for key in evidence:
                for s in team_seats(team, key):
                    if s["role"] != role:
                        continue
                    if s["ps"]:
                        counts[s["ps"]] += 1
                        latest_at[s["ps"]] = max(latest_at.get(s["ps"], ""), games[key]["ts"])
                    else:
                        unresolved[s["identity"] or s["handle"]] += 1
            slot = {"role": role, "player_id": None, "games": 0, "share": 0.0, "status": "NO_EVIDENCE",
                    "unresolved_seats": sum(unresolved.values())}
            if counts or unresolved:
                best = max(counts, key=lambda p: (counts[p], latest_at[p], p)) if counts else None
                top_unresolved = max(unresolved.values()) if unresolved else 0
                if best is None or top_unresolved > counts[best]:
                    slot["status"] = "IDENTITY_UNRESOLVED"
                else:
                    n = counts[best]
                    slot.update(player_id=best, games=n, share=round(n / len(evidence), 3))
                    if n < MIN_ROLE_GAMES or n / len(evidence) < MIN_ROLE_SHARE:
                        slot["status"] = "INSUFFICIENT_PARTICIPATION"
                    elif best not in allowed:
                        slot["status"] = "NOT_REGISTERED"
                    elif employer.get(best, team) != team:
                        slot["status"] = "OWNERSHIP_CONFLICT"
                        conflicts.append({"player_id": best, "team_id": team, "authoritative_team_id": employer.get(best),
                                          "role": role})
                    else:
                        slot["status"] = "VERIFIED"
                    slot["registered_role"] = registered.get(best, "")
            slots.append(slot)
        starters = [s["player_id"] for s in slots if s["status"] == "VERIFIED"]
        if len(starters) == 5 and len(set(starters)) == 5:
            status = "VERIFIED"
        elif not evidence:
            status = "NO_PARTICIPATION_EVIDENCE"
        else:
            status = "INCOMPLETE"
            if len(set(starters)) < len(starters):
                reasons.append("DUPLICATE_STARTER")
        reasons += sorted({s["status"] for s in slots if s["status"] != "VERIFIED"})
        last_game = games[played[-1]]["date"] if played else None
        stale = bool(last_game and (as_of - date.fromisoformat(last_game)).days > STALE_DAYS)
        starter_set = set(starters) if status == "VERIFIED" else set()
        substitutes = sorted(p for p in registered if p not in starter_set and employer.get(p) == team)
        secondary = sorted(p for p in registered if employer.get(p) not in (team, None))
        squad = (len(starter_set) + len(substitutes)) if status == "VERIFIED" else None
        out_teams.append({
            "team_id": team, **teams_meta.get(team, {"name": None, "acronym": None, "location": None}),
            "latest_stage": {k: stage[k] for k in ("id", "name", "tier", "begin", "end", "league_id")},
            "league_id": home_league, "league": leagues.get(home_league), "tier": home_tier,
            "latest_domestic_league": leagues.get(domestic["league_id"]),
            "evidence_window_start": window_start, "evidence_used_previous_stage": used_previous,
            "evidence_games": len(evidence), "last_game_date": last_game, "stale": stale,
            "status": status, "reasons": reasons, "playable": status == "VERIFIED" and not stale,
            "starters": slots, "substitutes": substitutes, "secondary_registrations": secondary,
            "registered_latest_stage": len(registered), "squad_size": squad,
            "squad_over_limit": bool(squad and squad > MAX_SQUAD),
            "academy_affiliation": "UNKNOWN_NO_SOURCE_FIELD",
            "academy_name_pattern": bool(ACADEMY_PATTERN.search(teams_meta.get(team, {}).get("name") or "")),
        })

    latest_of = {t["team_id"]: t["latest_stage"]["begin"] for t in out_teams}
    for t in out_teams:
        if t["status"] == "VERIFIED":
            continue
        moved = Counter(employer.get(s["player_id"]) for s in t["starters"] if s["status"] == "OWNERSHIP_CONFLICT")
        target = max(moved, key=lambda x: (moved[x], x or "")) if moved else None
        if target and moved[target] >= 3 and latest_of.get(target, "") >= t["latest_stage"]["begin"]:
            t["status"], t["superseded_by"] = "SUPERSEDED", target

    # Squad players with ratings (research model) for verified teams.
    people = {}
    for t in out_teams:
        if t["status"] != "VERIFIED":
            continue
        for pid in [s["player_id"] for s in t["starters"]] + t["substitutes"]:
            r = ratings.get(pid, {})
            people[pid] = {"player_id": pid, **players_meta.get(pid, {}), "team_id": t["team_id"],
                           "squad_role": "STARTER" if pid in {s["player_id"] for s in t["starters"]} else "SUBSTITUTE",
                           "starter_role": next((s["role"] for s in t["starters"] if s["player_id"] == pid), None),
                           "rating": {k: r.get(k) for k in ("primary_role", "overall_rating", "potential", "band", "mechanics",
                                                            "early_game", "teamfight", "macro", "champion_pool", "age",
                                                            "rating_evidence_class", "confidence_label", "role_ratings",
                                                            "games_2026")} if r else None}
    return out_teams, people, conflicts, history


def summarize(teams, people, conflicts, as_of, rating_version):
    by_tier = defaultdict(Counter)
    reasons = Counter()
    for t in teams:
        by_tier[t["tier"] or "unknown"][t["status"]] += 1
        by_tier[t["tier"] or "unknown"]["playable"] += int(t["playable"])
        by_tier[t["tier"] or "unknown"]["stale"] += int(t["status"] == "VERIFIED" and t["stale"])
        for r in t["reasons"]:
            reasons[r] += 1
    verified = [t for t in teams if t["status"] == "VERIFIED"]
    playable = [t for t in verified if t["playable"]]
    squads = Counter(t["squad_size"] for t in playable)
    starters = [p for p in people.values() if p["squad_role"] == "STARTER"]
    slot_status = Counter(s["status"] for t in teams for s in t["starters"])
    role_mismatch = sum(1 for t in verified for s in t["starters"] if s.get("registered_role") and s["registered_role"] != s["role"])
    rated = [p for p in people.values() if p["rating"] and p["rating"]["overall_rating"] is not None]
    return {
        "as_of": as_of.isoformat(), "rules": rules(), "rating_model_version": rating_version,
        "teams": len(teams), "status": dict(Counter(t["status"] for t in teams)),
        "verified": len(verified), "playable_verified": len(playable),
        "stale_verified": sum(1 for t in verified if t["stale"]),
        "by_tier": {k: dict(v) for k, v in sorted(by_tier.items())},
        "incomplete_reasons": dict(reasons.most_common()),
        "slot_status": dict(slot_status),
        "playable_squad_sizes": dict(sorted(squads.items())),
        "max_playable_squad": max(squads) if squads else None,
        "squads_over_limit": sum(1 for t in teams if t["squad_over_limit"]),
        "starters_in_playable": sum(1 for t in playable for s in t["starters"]),
        "substitutes_in_playable": sum(len(t["substitutes"]) for t in playable),
        "starter_registered_role_differs": role_mismatch,
        "ownership_conflicts": len(conflicts),
        "players_with_ratings": len(rated), "players_without_rating": len(people) - len(rated),
        "starters_by_evidence_class": dict(Counter((p["rating"] or {}).get("rating_evidence_class") for p in starters)),
        "academy_name_pattern_teams": sum(1 for t in teams if t["academy_name_pattern"]),
        "squad_memberships_in_verified": sum(len(t["starters"]) + len(t["substitutes"]) for t in verified),
        "unique_people_in_verified": len(people),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--db", type=Path, default=DATA_DIR / "staging.sqlite3")
    parser.add_argument("--as-of", type=date.fromisoformat, default=AS_OF)
    parser.add_argument("--ratings", type=Path, default=DATA_DIR / "ratings" / "experiment-uncapped" / "player_ratings_2023_2026.json")
    parser.add_argument("--out", type=Path, default=DATA_DIR / "world")
    args = parser.parse_args()
    ratings_path = (args.ratings if args.ratings.exists() else DATA_DIR / "ratings" / "player_ratings_2023_2026.json").resolve()
    ratings, rating_version = load_ratings(ratings_path)
    db = sqlite3.connect(f"file:{args.db.as_posix()}?mode=ro", uri=True)
    teams, people, conflicts, history = build(db, args.as_of, ratings)
    summary = summarize(teams, people, conflicts, args.as_of, rating_version)
    summary["ratings_file_sha256"] = sha256(ratings_path)
    summary["ratings_file"] = ratings_path.relative_to(DATA_DIR).as_posix()
    args.out.mkdir(parents=True, exist_ok=True)
    pack = {"format": "real-roster-snapshot", "version": VERSION, "as_of": args.as_of.isoformat(), "rules": rules(),
            "rating_model_version": rating_version, "teams": teams, "players": people, "ownership_conflicts": conflicts,
            "historical_memberships": sorted(history, key=lambda h: (h["team_id"], h["begin"], h["stage_id"], h["player_id"])),
            "note": "LOCAL RESEARCH PACK: real names; never commit or redistribute."}
    name = f"real_roster_snapshot_{args.as_of.isoformat()}.json"
    (args.out / name).write_text(json.dumps(pack, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    summary["pack_file"] = name
    summary["pack_sha256"] = sha256(args.out / name)
    # Every real handle known to the staging database (PandaScore catalogue and Leaguepedia scoreboards), normalized to
    # lower-case letters and digits: generated NPC handles must avoid them all.
    handles = {re.sub(r"[^a-z0-9]", "", (n or "").lower()) for (n,) in db.execute("SELECT name FROM ps_players")}
    handles |= {re.sub(r"[^a-z0-9]", "", (n or "").lower()) for (n,) in db.execute("SELECT DISTINCT name FROM lp_player_games")}
    handles.discard("")
    team_names = sorted({re.sub(r"[^a-z0-9]", "", (n or "").lower()) for (n,) in db.execute("SELECT name FROM ps_teams")} - {""})
    (args.out / "real_handle_exclusions.json").write_text(json.dumps(
        {"format": "real-handle-exclusions", "normalization": "lower-case, letters and digits only",
         "handles": sorted(handles), "team_names": team_names, "note": "LOCAL RESEARCH FILE: real names; never commit or redistribute."}) + "\n",
        encoding="utf-8")
    summary["real_handle_exclusions"] = len(handles)
    (args.out / "real_roster_validation_summary.json").write_text(json.dumps(summary, indent=1) + "\n", encoding="utf-8")
    print(json.dumps(summary, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
