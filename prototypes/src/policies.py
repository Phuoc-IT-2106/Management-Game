"""Scripted management policies. Inputs are observations, never simulator state."""
from .state import Decision, DecisionCheckpoint
from .competition import forecast


POLICIES=("conservative","aggressive","adaptive","fixed","financed","recovery")


def choose(cp: DecisionCheckpoint, policy: str, cfg: dict) -> Decision:
    if policy not in POLICIES:
        raise ValueError("Unknown policy")
    facts=cp.observation
    choice="retain" if "retain" in cp.choices else cp.choices[0]
    if cp.kind=="investment":
        if policy=="financed" and "finance_star" in cp.choices:
            choice="finance_star"
        elif policy=="aggressive":
            choice="star" if "star" in cp.choices else ("finance_star" if "finance_star" in cp.choices else choice)
        elif policy=="adaptive":
            if facts.get("sponsor_payment")>=cfg["policy_rules"]["growth_sponsor_threshold"] and "star" in cp.choices:
                choice="star"
            elif facts.get("cash")>=cfg["policy_rules"]["information_cash_threshold"] and "information" in cp.choices:
                choice="information"
    elif cp.kind=="competition":
        if policy=="fixed":
            options=[i for i,p in enumerate(cp.plans) if p.posture=="Aggressive" and p.allocation[0]>0.6]
            if options:
                choice=str(options[0])
            else:
                choice=cp.recommended
        elif policy=="conservative":
            options=[i for i,p in enumerate(cp.plans) if p.posture=="Conservative"]
            choice=str(max(options,key=lambda i:forecast(facts.competitive,cp.plans[i],cfg))) if options else cp.recommended
        else:
            choice=cp.recommended
    elif cp.kind=="sponsor":
        eligible=[]
        for offer_id,payment,extra_load,minimum_rep in facts.get("offers",()):
            if offer_id not in cp.choices:
                continue
            if payment<=facts.get("sponsor_payment") and facts.get("sponsor_expiry")-facts.get("day")>cfg["policy_rules"]["renewal_window"]:
                continue
            if policy=="conservative" and (minimum_rep>facts.get("reputation") or extra_load+facts.get("load")-facts.get("sponsor_load",0)>facts.get("capacity")):
                continue
            # Value includes an explicit workload/condition cost, not access to hidden future outcomes.
            score=payment-extra_load-(cfg["policy_rules"]["sponsor_condition_risk_cost"] if minimum_rep>facts.get("reputation") else 0)
            eligible.append((score,offer_id))
        choice=max(eligible)[1] if eligible else "decline"
    elif cp.kind=="recovery":
        if facts.get("stage")=="Warning" and policy=="recovery":
            choice="wait"  # Controlled fixture preserves an overdue state for K/L.
        elif "sell_star" in cp.choices and policy in ("recovery","conservative","adaptive"):
            choice="sell_star"
        elif "cancel_investment" in cp.choices:
            choice="cancel_investment"
        elif "bridge" in cp.choices:
            choice="bridge"
        else:
            choice="wait"
    return Decision(cp.id,choice)
