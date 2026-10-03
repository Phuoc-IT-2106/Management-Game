"""The only boundary allowed to read hidden truth to produce observations."""
from .state import Company, World, Observation, Estimate, CompetitiveView, POSTURES, draw
from .competition import lineups, profile, exposure


def estimate(truth: float, quality: float, seed: int, event: str, cfg: dict) -> Estimate:
    width = cfg["competition"]["information_error"] * (1-quality/100)
    value = truth + (draw(seed, "observation", event)*2-1)*width
    return Estimate(round(value, 6), round(value-width, 6), round(value+width, 6), quality/100)


def competitive_observation(company: Company, world: World, rival_id: str, match_id: str,
                            seed: int, cfg: dict) -> CompetitiveView:
    from .economy import efficiency
    rival = world.rivals[rival_id]
    quality = company.information["opponent"]
    rules=cfg["information_rules"]
    known = draw(seed, "posture_observation", match_id) < rules["posture_base_accuracy"]+rules["posture_quality_weight"]*quality/100
    alternatives = [p for p in POSTURES if p != rival.posture]
    observed = rival.posture if known else alternatives[int(draw(seed,"posture_error",match_id)*2)]
    return CompetitiveView(match_id, rival_id,
        tuple((lineup,profile(company,lineup,world.day,cfg)) for lineup in lineups(company)),
        estimate(rival.strength,quality,seed,match_id,cfg), observed,
        round(max(0,min(1,estimate(rival.adaptation*rival.information/100,quality,seed,match_id+":adaptation",cfg).value/100)),6),
        world.meta, abs(world.meta-world.previous_meta) if world.day-world.meta_changed_day < cfg["world_rules"]["meta_memory_days"] else 0,
        cfg["competition"]["preparation_days"], efficiency(company,cfg),
        tuple(sorted(company.coach.items())), tuple(sorted(exposure(company).items())))


def observe(company: Company, world: World, seed: int, cfg: dict,
            view: CompetitiveView | None = None) -> Observation:
    from .economy import outstanding, load, capacity, forecast_cash, commercial_value
    facts = {
        "cash": company.cash, "reputation": company.reputation, "audience": company.audience,
        "forecast": forecast_cash(company,world.day,cfg["distress"]["forecast_days"]),
        "liabilities": outstanding(company), "load": load(company), "capacity": capacity(company),
        "stage": company.stage, "has_star": "S" in company.players,
        "debt_open": any(company.financial[d.payment_id].status != "paid" for d in company.debts),
        "information": tuple(sorted(company.information.items())),
        "commercial_value": commercial_value(company,cfg),
        "sponsor_payment": company.sponsor.payment if company.sponsor.expiry>=world.day else 0,
        "sponsor_expiry":company.sponsor.expiry,"day":world.day,
        "sponsor_load":company.loads.get("sponsor_extra",0),
        "sponsor_requirement": company.sponsor.minimum_reputation,
        "star_cost": cfg["finance"]["star_fee"], "information_cost": cfg["finance"]["information_cost"],
        "talent": tuple((t.id,estimate(t.execution,company.information["talent"],seed,t.id,cfg),
                         t.cost,t.salary) for t in sorted(world.talent.values(),key=lambda t:t.id)
                        if t.claimed_by is None),
        "offers": tuple((o.id,o.payment,o.load,o.minimum_reputation) for o in
                        sorted(world.opportunities.values(),key=lambda o:o.id)
                        if o.claimed_by is None and o.expiry >= world.day)
    }
    return Observation(tuple(sorted(facts.items())), view)
