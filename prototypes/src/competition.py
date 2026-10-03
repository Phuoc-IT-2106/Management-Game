"""Pure competitive calculations. No authoritative state is mutated here."""
from __future__ import annotations

from itertools import product
from .state import Company, CompetitiveOutcome, CompetitiveView, Plan, Profile, ROLES, draw


def lineups(company: Company) -> tuple[tuple[str, ...], ...]:
    candidates = [[p.id for p in company.players.values() if p.available and r in p.roles] for r in ROLES]
    return tuple(sorted(x for x in product(*candidates) if len(set(x)) == 5))


def profile(company: Company, lineup: tuple[str, ...], day: int, cfg: dict) -> Profile:
    if lineup not in lineups(company):
        raise ValueError("Lineup must cover five roles with five available distinct players")
    people = [company.players[x] for x in lineup]
    c = cfg["competition"]
    integration = sum(max(0, 1 - (day-p.joined)/c["familiarity_days"]) for p in people) / 5
    f=cfg["formula"]
    coordination = f["coordination_baseline"] - f["coordination_integration_loss"] * integration
    strength = sum(p.execution * (f["readiness_base"] + f["readiness_weight"]*p.readiness/100) for p in people)/5
    strength -= c["integration_penalty"] * integration
    return Profile(round(strength, 6), sum(p.adaptability for p in people)/5,
                   sum(p.consistency for p in people)/5, round(coordination, 6))


def exposure(company: Company) -> dict[str, float]:
    """Only completed public plans count; never inspect the forthcoming commitment."""
    history = [company.plans[x].posture for x in company.result_ids[-4:] if x in company.plans]
    return {p: history.count(p)/4 for p in ("Conservative", "Balanced", "Aggressive")}


def factors(own: Profile, rival_strength: float, rival_posture: str, rival_adaptation: float,
            plan: Plan, effort: float, meta: int, meta_change: float, exposed: float,
            cfg: dict) -> dict[str, float]:
    c = cfg["competition"]
    execution, opponent, adapt = plan.allocation
    f=cfg["formula"]
    execution_bonus = effort * execution * c["execution_weight"] * (1+(100-own.coordination)/f["execution_coordination_scale"])
    targeting = 1.0 if plan.target_posture == rival_posture else f["mistargeted_preparation"]
    opponent_bonus = effort * opponent * c["opponent_weight"] * targeting
    meta_bonus = effort * adapt * c["meta_weight"] * meta_change
    stance = {"Conservative": -1, "Balanced": 0, "Aggressive": 1}[plan.posture]
    enemy_stance = {"Conservative": -1, "Balanced": 0, "Aggressive": 1}[rival_posture]
    matchup = stance * (own.adaptability-f["posture_adaptability_center"])*f["posture_adaptability_weight"] + meta*stance*f["meta_posture_weight"]
    matchup += (own.coordination-f["posture_coordination_center"])*(-stance)*f["posture_coordination_weight"] - enemy_stance*stance*f["posture_interaction_weight"]
    counter = exposed * rival_adaptation * c["adaptation_weight"]
    if cfg.get("ablations", {}).get("rival_adaptation"):
        counter = 0
    return {"base_capability_difference": own.capability-rival_strength,
            "execution_preparation": execution_bonus,
            "opponent_preparation": opponent_bonus,
            "meta_preparation": meta_bonus,
            "rival_preparation": -c["rival_preparation"],
            "strategic_matchup": matchup, "rival_adaptation": -counter}


def probability(parts: dict[str, float], posture: str, cfg: dict) -> float:
    c = cfg["competition"]
    variance = c["variance_"+posture.lower()]
    return round(min(c["probability_ceiling"], max(c["probability_floor"],
                     0.5+sum(parts.values())/(2*variance))), 6)


def forecast(view: CompetitiveView, plan: Plan, cfg: dict) -> float:
    own = dict(view.profiles)[plan.lineup]
    effort = cfg["competition"]["preparation_scale"] * dict(view.coach)["preparation"]/100 * view.efficiency
    parts = factors(own, view.rival_strength.value, view.rival_posture, view.rival_adaptation,
                    plan, effort, view.meta, view.meta_change, dict(view.exposure)[plan.posture], cfg)
    return probability(parts, plan.posture, cfg)


def resolve(*, match_id: str, day: int, rival_id: str, own: Profile, rival_strength: float,
            rival_posture: str, rival_adaptation: float, plan: Plan, effort: float,
            meta: int, meta_change: float, exposed: float, seed: int, cfg: dict) -> CompetitiveOutcome:
    parts = factors(own, rival_strength, rival_posture, rival_adaptation, plan, effort,
                    meta, meta_change, exposed, cfg)
    p = probability(parts, plan.posture, cfg)
    roll = draw(seed, "match", match_id)
    variance = cfg["competition"]["variance_"+plan.posture.lower()]
    # Linear bounded-noise model. Clamp correction remains visible, not a hidden bonus.
    parts["probability_clamp_adjustment"] = (p-0.5)*2*variance-sum(parts.values())
    parts["bounded_execution_variance"] = (0.5-roll)*2*variance
    parts["random_draw"] = roll
    margin = (p-roll)*2*variance
    return CompetitiveOutcome(match_id, day, rival_id, roll < p, p, round(margin, 6),
                              1.0, 1.0, tuple((k, round(v, 6)) for k,v in parts.items()), plan)
