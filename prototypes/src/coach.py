"""Observation-only coach; no Company/World reference is accepted."""
from dataclasses import dataclass
from .state import CompetitiveView, Plan, Envelope, POSTURES
from .competition import forecast


@dataclass(frozen=True)
class Recommendation:
    plans: tuple[Plan, ...]
    scores: tuple[float, ...]
    selected: int
    escalation: bool
    reason: str


def recommend(view: CompetitiveView, envelope: Envelope, cfg: dict) -> Recommendation:
    options = []
    for lineup, own in view.profiles:
        if not set(envelope.protected).issubset(lineup):
            continue
        for allocation in cfg["preparation_options"]:
            for posture in POSTURES[:POSTURES.index(envelope.risk_ceiling)+1]:
                options.append(Plan(tuple(allocation),lineup,posture,view.rival_posture,view.meta))
    if not options:
        return Recommendation((),(),-1,True,"No eligible lineup satisfies protected-player constraints")
    quality = dict(view.coach)["judgment"]/100
    # A bounded coach tendency can overvalue a familiar, balanced plan. It never buffs resolution.
    scores = tuple(round(forecast(view,p,cfg)+(1-quality)*cfg["coach_rules"]["bias_weight"]*
                         (1 if p.posture == "Balanced" else 0),6) for p in options)
    chosen = max(range(len(options)),key=lambda i:scores[i])
    unique = sorted(set(scores),reverse=True)
    near = len(unique)>1 and unique[0]-unique[1] < cfg["coach_rules"]["close_score"]
    low = view.rival_strength.confidence < cfg["coach_rules"]["confidence_threshold"]
    reason = (f"Highest estimated win probability under risk ceiling {envelope.risk_ceiling}; "
              f"estimated score={scores[chosen]:.3f}, confidence={view.rival_strength.confidence:.2f}")
    if low or near:
        reason += "; escalate uncertainty/close alternatives"
    return Recommendation(tuple(options),scores,chosen,low or near,reason)
