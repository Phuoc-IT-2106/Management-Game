"""Offline exports for cache_logos.py, using provider IDs and normalized roster evidence only.

This module owns no asset store or schema. Paths come exclusively from verified image_cache entries.
Source: PandaScore. Provider data exports stay in the ignored data/assets/mapping directory.
"""
from collections import Counter, defaultdict
from datetime import datetime
from itertools import combinations
from pathlib import PurePosixPath
from urllib.parse import urlsplit

IN_2026 = {"begins_in_2026", "ends_in_2026", "spans_2026", "scheduled_in_2026"}


def rows(db, table):
    if not db.execute("SELECT 1 FROM sqlite_master WHERE name=? AND type='table'", (table,)).fetchone():
        return []
    cursor = db.execute(f"SELECT * FROM {table}")
    keys = [c[0] for c in cursor.description]
    return [dict(zip(keys, row)) for row in cursor]


def select_team_logo(team, dark=False):
    """Presentation choice only: a missing dark variant falls back to this exact team's regular logo."""
    if dark and team.get("dark_logo_path"):
        return team["dark_logo_path"]
    return team.get("logo_path")


def combined_status(*statuses):
    for status in ("INVALID_IMAGE", "DOWNLOAD_FAILED", "MISSING_URL", "LICENSE_REVIEW_REQUIRED", "AVAILABLE"):
        if status in statuses:
            return status
    return "CACHED"


def timestamp(value):
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00")) if value else None
    except ValueError:
        return None


def registration_conflicts(registrations):
    """Potential overlapping team registrations, not evidence of contracts or actual participation."""
    groups = defaultdict(list)
    for row in registrations:
        groups[row["player_id"]].append(row)
    conflicts, uncertain = [], []
    for player, group in groups.items():
        for left, right in combinations(group, 2):
            if left["team_id"] == right["team_id"]:
                continue
            starts = [timestamp(r["begin_at"]) for r in (left, right)]
            ends = [timestamp(r["end_at"]) for r in (left, right)]
            same_stage = left["tournament_id"] == right["tournament_id"]
            unknown = not all(starts + ends)
            if not same_stage and not unknown and max(starts) > min(ends):
                continue
            item = {"player_id": player, "left_tournament_id": left["tournament_id"],
                    "right_tournament_id": right["tournament_id"], "left_team_id": left["team_id"],
                    "right_team_id": right["team_id"], "review_required": True,
                    "reason": "same_stage_multiple_teams" if same_stage else
                              "unknown_period" if unknown else "overlapping_stage_periods"}
            (uncertain if unknown and not same_stage else conflicts).append(item)
    return conflicts, uncertain


def build_mappings(db, entries):
    assets = {(r["entity_type"], r["entity_id"], r["field"]): r for r in entries}
    catalogs = {entity: {r[f"{entity}_id"]: r for r in rows(db, table)}
                for entity, table in (("player", "ps_players"), ("team", "ps_teams"), ("league", "ps_leagues"))}
    stages = {r["tournament_id"]: r for r in rows(db, "ps_tournaments")}
    provenance_keys = ("source_run", "source_query", "source_file", "observed_at", "payload_sha256")

    def asset(entity, entity_id, field="image_url"):
        return assets.get((entity, entity_id, field), {"status": "MISSING_URL", "local_path": None})

    def path(a):
        return a.get("local_path") if a["status"] == "CACHED" else None

    def provenance(record):
        return {key: record.get(key) for key in provenance_keys}

    memberships = defaultdict(list)
    for row in rows(db, "team_membership_observations"):
        memberships[row["player_id"]].append(row)
    players, teams = {}, {}
    for player_id, record in sorted(catalogs["player"].items()):
        a = asset("player", player_id)
        current = record.get("current_team_id")
        players[player_id] = {"provider": "pandascore", "entity_type": "player", "entity_id": player_id,
                              "image_status": a["status"], "image_path": path(a),
                              "asset_identity": a.get("asset_identity"),
                              "provider_current_team_id": current,
                              "provider_current_team_exists": current in catalogs["team"] if current else None,
                              "current_team_source": "ps_players.current_team_id",
                              "provenance": provenance(record),
                              "historical_team_observations": memberships.get(player_id, [])}
    for team_id, record in sorted(catalogs["team"].items()):
        regular, dark = asset("team", team_id), asset("team", team_id, "dark_mode_image_url")
        teams[team_id] = {"provider": "pandascore", "entity_type": "team", "entity_id": team_id,
                          "logo_path": path(regular), "dark_logo_path": path(dark),
                          "image_status": regular["status"], "dark_image_status": dark["status"],
                          "provenance": provenance(record)}

    observations = defaultdict(list)
    for row in rows(db, "tournament_registrations"):
        observations[(row["tournament_id"], row["team_id"], row["player_id"])].append(row)
    registrations = rows(db, "ps_registrations")
    roster = []
    for row in sorted(registrations, key=lambda r: (r["tournament_id"], r["team_id"], r["player_id"])):
        stage = stages.get(row["tournament_id"], {})
        if stage.get("relevance_2026") not in IN_2026:
            continue
        player_id, team_id = row["player_id"], row["team_id"]
        portrait, logo = asset("player", player_id), asset("team", team_id)
        broken = (["player"] if player_id not in players else []) + (["team"] if team_id not in teams else [])
        roster.append({"provider": "pandascore", "tournament_id": row["tournament_id"], "team_id": team_id,
                       "player_id": player_id, "player_image_path": path(portrait) if player_id in players else None,
                       "team_logo_path": path(logo) if team_id in teams else None,
                       "team_dark_logo_path": teams.get(team_id, {}).get("dark_logo_path"),
                       "player_image_status": portrait["status"], "team_logo_status": logo["status"],
                       "image_status": combined_status(portrait["status"], logo["status"]),
                       "registration_source": "pandascore", "registration_status": "broken_reference" if broken else "verified",
                       "registration_evidence": "ps_registrations: expected_roster; not match participation or contract",
                       "broken_references": broken, "source_run": row["source_run"], "observed_at": row["observed_at"],
                       "begin_at": stage.get("begin_at"), "end_at": stage.get("end_at"),
                       "tournament_provenance": provenance(stage),
                       "provider_current_team_id": players.get(player_id, {}).get("provider_current_team_id"),
                       "historical_registration_observations": observations.get((row["tournament_id"], team_id, player_id), [])})
    conflicts, uncertain = registration_conflicts(roster)
    flagged = {(r["player_id"], r[side + "_tournament_id"], r[side + "_team_id"])
               for r in conflicts + uncertain for side in ("left", "right")}
    for row in roster:
        row["association_review_required"] = (row["player_id"], row["tournament_id"], row["team_id"]) in flagged

    registered_players = {r["player_id"] for r in roster}
    competition_teams = {r["team_id"] for r in rows(db, "ps_tournament_teams")
                         if stages.get(r["tournament_id"], {}).get("relevance_2026") in IN_2026}
    competition_teams.update(r["team_id"] for r in roster)
    for match in rows(db, "ps_matches"):
        if match.get("relevance_2026") in IN_2026:
            competition_teams.update(match[k] for k in ("opponent1_id", "opponent2_id") if match.get(k))

    def counts(entity, field="image_url", ids=None):
        ids = set(catalogs[entity]) if ids is None else ids
        selected = [asset(entity, i, field) for i in ids]
        states = Counter(a["status"] for a in selected)
        return {"total": len(ids), "with_url": sum(bool(a.get("source_url")) for a in selected),
                "cached": states["CACHED"], "missing_url": states["MISSING_URL"],
                "failed": states["DOWNLOAD_FAILED"], "invalid": states["INVALID_IMAGE"],
                "license_review_required": states["LICENSE_REVIEW_REQUIRED"], "statuses": dict(states),
                "cached_coverage_percent": round(100 * states["CACHED"] / len(ids), 2) if ids else 0}

    broken = {"image_entities": sum(r["entity_id"] not in catalogs[r["entity_type"]] for r in entries),
              "registration_tournaments": sum(r["tournament_id"] not in stages for r in registrations),
              "registration_players": sum(r["player_id"] not in players for r in registrations),
              "registration_teams": sum(r["team_id"] not in teams for r in registrations),
              "current_teams": sum(bool(p["provider_current_team_id"]) and not p["provider_current_team_exists"] for p in players.values()),
              "competition_teams": len(competition_teams - teams.keys())}
    player_index = {r["player_id"] for r in rows(db, "ps_player_index_2026") if r["registered_in_2026"] == "verified"}
    versions = defaultdict(set)
    for (stage, team, player), evidence in observations.items():
        versions[(stage, player)].add(team)
    revision_reviews = [{"tournament_id": stage, "player_id": player, "observed_team_ids": sorted(ids),
                         "reason": "different teams observed across snapshots; observation date is not transfer date"}
                        for (stage, player), ids in sorted(versions.items()) if len(ids) > 1]
    coverage = {"source": "Source: PandaScore", "players": counts("player"), "teams": counts("team"),
                "dark_logos": counts("team", "dark_mode_image_url"), "leagues": counts("league"),
                "players_registered_2026": counts("player", ids=registered_players),
                "teams_competing_2026": counts("team", ids=competition_teams),
                "associations": {"player_mappings": len(players), "team_mappings": len(teams),
                                 "valid_player_image_references": counts("player")["with_url"],
                                 "valid_team_logo_references": counts("team")["with_url"],
                                 "roster_rows": len(roster),
                                 "valid_roster_entity_links": sum(not r["broken_references"] for r in roster),
                                 "roster_links_with_both_images_cached": sum(r["image_status"] == "CACHED" and not r["broken_references"] for r in roster),
                                 "roster_rows_missing_player_url": sum(r["player_image_status"] == "MISSING_URL" for r in roster),
                                 "roster_rows_missing_team_url": sum(r["team_logo_status"] == "MISSING_URL" for r in roster),
                                 "broken_references": broken, "overlapping_registration_pairs": len(conflicts),
                                 "overlapping_registration_players": len({r["player_id"] for r in conflicts}),
                                 "unknown_period_review_pairs": len(uncertain), "historical_revision_reviews": len(revision_reviews),
                                 "player_index_disagreement": len(player_index ^ registered_players),
                                 "absent_image_reference_rows": sum(r.get("url_state") == "absent_reference" for r in entries)},
                "verified_cache_files": sum(r["status"] == "CACHED" for r in entries),
                "verified_cache_bytes": sum(r.get("bytes") or 0 for r in entries if r["status"] == "CACHED")}
    urls = [r["source_url"] for r in entries if r.get("source_url")]
    def url_metadata(value):
        try:
            parts = urlsplit(value)
            return parts.hostname or "invalid", PurePosixPath(parts.path).suffix.lower() or "no_extension"
        except ValueError:
            return "invalid", "unknown"
    url_parts = [url_metadata(u) for u in urls]
    coverage["cdn"] = {"url_count": len(urls), "hosts": dict(Counter(p[0] for p in url_parts)),
                       "path_extensions": dict(Counter(p[1] for p in url_parts))}
    coverage["authorization"] = {"references_with_recorded_approval": sum(bool(r.get("license_note", "").strip()) for r in entries),
                                 "third_party_licence_inferred": False}
    coverage["cached_formats"] = dict(Counter(r["format"] for r in entries if r["status"] == "CACHED"))
    pending = sum(bool(r.get("source_url")) and r["status"] != "CACHED" for r in entries)
    coverage["remaining_url_assets"] = pending
    coverage["status"] = ("READY_FOR_APPROVED_DOWNLOAD" if any(r["status"] == "LICENSE_REVIEW_REQUIRED" for r in entries)
                          else "PARTIAL" if pending or any(broken.values()) else "COMPLETE")
    return {"player_images.json": list(players.values()), "team_images.json": list(teams.values()),
            "roster_image_index_2026.json": roster, "association_reviews_2026.json": conflicts + uncertain,
            "historical_registration_reviews.json": revision_reviews}, coverage
