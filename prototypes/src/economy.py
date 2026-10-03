"""Company-owned money, commitments, commercial consequences and recovery."""
from __future__ import annotations
from .state import (Company, FinancialItem, Contract, Debt, Player, Sponsor, Consequence,
                    Opportunity, Talent, CompetitiveOutcome)


def capacity(c: Company) -> float:
    return sum(c.capacity_inputs.values())


def load(c: Company) -> float:
    return sum(c.loads.values())


def efficiency(c: Company, cfg: dict) -> float:
    if cfg.get("ablations", {}).get("overload"):
        return 1.0
    ratio = load(c)/max(1,capacity(c))
    return max(cfg["organization"]["minimum_efficiency"],
               1-cfg["organization"]["overload_slope"]*max(0,ratio-1))


def outstanding(c: Company) -> int:
    return sum(x.amount for x in c.financial.values() if x.direction=="out" and x.status in ("pending","overdue"))


def forecast_cash(c: Company, day: int, window: int) -> int:
    return c.cash+sum(x.amount*(1 if x.direction=="in" else -1) for x in c.financial.values()
                      if x.status in ("pending","overdue") and x.due <= day+window)


def commercial_value(c: Company, cfg: dict | None=None) -> float:
    if cfg is None:
        from .state import load_calibration
        cfg=load_calibration()
    weights=cfg["economy_rules"]
    return round(c.reputation*weights["commercial_reputation_weight"]+c.audience*weights["commercial_audience_weight"],6)


def schedule_contract(c: Company, contract: Contract, cfg: dict) -> None:
    period = cfg["finance"]["period"]
    for due in range(period,contract.end+1,period):
        if due <= contract.start:
            continue
        item_id = f"{contract.id}:pay:{due}"
        c.financial[item_id] = FinancialItem(item_id,due,contract.recurring,"out",contract.id,"salary")
        contract.payment_ids.append(item_id)


def settle(c: Company, day: int, emit) -> None:
    """Existing receipts first; no retroactive removal of an earlier missed-day fact."""
    items = sorted(c.financial.values(),key=lambda x:(x.direction != "in",x.due,x.id))
    for item in items:
        if item.due > day or item.status not in ("pending","overdue"):
            continue
        before = c.cash
        if item.direction == "in":
            c.cash += item.amount
        elif c.cash >= item.amount:
            c.cash -= item.amount
        else:
            if item.status != "overdue":
                item.status = "overdue"
                item.missed_day = day
                emit("missed_obligation",item.id,before=before,after=c.cash,amount=item.amount,
                     cause=item.source,due=item.due)
            continue
        item.status = "paid"
        item.paid_day = day
        emit("cash_settlement",item.id,before=before,after=c.cash,amount=item.amount,
             direction=item.direction,cause=item.source,due=item.due)


def spend(c: Company, amount: int, source: str, emit) -> None:
    if amount < 0 or c.cash < amount:
        raise ValueError("Insufficient liquidity")
    before = c.cash
    c.cash -= amount
    emit("cash_spending",source,before=before,after=c.cash,amount=amount,cause=source)


def end_contract(c: Company, player_id: str, day: int) -> None:
    for contract in c.contracts.values():
        if contract.entity != player_id or not contract.active:
            continue
        contract.active = False
        for item_id in contract.payment_ids:
            item = c.financial[item_id]
            if item.due > day and item.status == "pending":
                item.status = "waived"


def replace_player(c: Company, talent: Talent, day: int, cfg: dict, emit,
                   old_id: str = "P1") -> None:
    if talent.claimed_by is not None or old_id not in c.players or talent.cost > c.cash:
        raise ValueError("Unavailable or unaffordable roster replacement")
    old = c.players[old_id]
    if old.roles != "A":
        raise ValueError("Prototype acquisition replaces Role A only")
    before_liabilities = outstanding(c)
    spend(c,talent.cost,f"sign:{talent.id}:{day}",emit)
    end_contract(c,old_id,day)
    del c.players[old_id]
    c.players[talent.id] = Player(talent.id,"A",talent.execution,talent.adaptability,
                                cfg["economy_rules"]["new_player_consistency"],cfg["economy_rules"]["new_player_readiness"],day)
    contract = Contract(f"contract:{talent.id}:{day}",talent.id,talent.salary,day)
    c.contracts[contract.id] = contract
    schedule_contract(c,contract,cfg)
    if talent.id == "S":
        c.loads["star"] = cfg["organization"]["star_load"]
    for match_id in list(c.plans):
        if match_id not in c.result_ids and old_id in c.plans[match_id].lineup:
            del c.plans[match_id]
    emit("roster_replacement",f"sign:{talent.id}:{day}",before=old.execution,after=talent.execution,
         removed=old_id,added=talent.id,liabilities_before=before_liabilities,
         liabilities_after=outstanding(c),cause=contract.id)


def borrow(c: Company, day: int, cfg: dict, emit) -> None:
    if any(c.financial[d.payment_id].status != "paid" for d in c.debts):
        raise ValueError("Only one outstanding bridge financing is allowed")
    f = cfg["finance"]
    fee = f["distress_bridge_fee"] if c.stage in ("Distress","Restructuring") else f["bridge_fee"]
    debt_id = f"bridge:{day}"
    item_id = debt_id+":repay"
    item = FinancialItem(item_id,day+f["bridge_term"],f["bridge_principal"]+fee,"out",debt_id,"debt")
    c.financial[item_id] = item
    c.debts.append(Debt(debt_id,f["bridge_principal"],fee,item_id))
    before = c.cash
    c.cash += f["bridge_principal"]
    emit("financing",debt_id,before=before,after=c.cash,obligation=item.amount,due=item.due,
         financing_cost=fee,cause=debt_id)


def invest(c: Company, kind: str, day: int, cfg: dict, emit) -> None:
    if kind in c.investments:
        raise ValueError("Investment already committed")
    f = cfg["finance"]
    cost = f["information_cost"] if kind.startswith("information") else f["support_cost"]
    spend(c,cost,f"investment:{kind}:{day}",emit)
    c.investments[kind] = day
    c.loads[kind] = cfg["organization"]["information_load"]
    event_id = f"investment:{kind}:{day}"
    c.consequences[event_id] = Consequence(event_id,day+f["investment_delay"],kind,event_id,1)


def accept_sponsor(c: Company, offer: Opportunity, day: int, cfg: dict, emit) -> None:
    if offer.claimed_by is not None or offer.expiry < day:
        raise ValueError("Sponsor opportunity is no longer available")
    previous = c.sponsor
    for item in c.financial.values():
        if item.direction == "in" and item.source == previous.id and item.due > day and item.status=="pending":
            item.status="waived"
    expiry = day+offer.duration
    c.sponsor = Sponsor(offer.id,offer.payment,expiry,offer.minimum_reputation,offer.load)
    c.loads["sponsor_extra"] = offer.load
    period = cfg["finance"]["period"]
    for due in range(((day//period)+1)*period,expiry+1,period):
        item_id=f"{offer.id}:receipt:{due}"
        c.financial[item_id]=FinancialItem(item_id,due,offer.payment,"in",offer.id,"sponsor")
    emit("sponsor_agreement",offer.id,before=previous.payment,after=offer.payment,
         load=offer.load,minimum_reputation=offer.minimum_reputation,cause=offer.id,
         receipt_dates=[x.due for x in c.financial.values() if x.source==offer.id])


def sponsor_conditions(c: Company, day: int, cfg: dict, emit) -> None:
    if c.reputation >= c.sponsor.minimum_reputation:
        return
    for item in list(c.financial.values()):
        if item.direction=="in" and item.source==c.sponsor.id and item.due==day:
            penalty_id=item.id+":condition"
            if penalty_id not in c.financial:
                c.financial[penalty_id]=FinancialItem(penalty_id,day,cfg["commercial"]["breach_penalty"],
                                                     "out",c.sponsor.id,"sponsor_condition")
                emit("commercial_condition",penalty_id,before=0,after=cfg["commercial"]["breach_penalty"],
                     reputation=c.reputation,required=c.sponsor.minimum_reputation,cause=c.sponsor.id)


def queue_outcome(c: Company, outcome: CompetitiveOutcome, cfg: dict, emit) -> None:
    key=f"{outcome.id}:reputation"
    if key in c.consequences:
        return
    rules=cfg["consequences"]
    sign=1 if outcome.won else -1
    base=rules["reputation_win"] if outcome.won else rules["reputation_loss"]
    surprise=abs(int(outcome.won)-outcome.probability)
    saturation=(1-c.reputation/100) if outcome.won else c.reputation/100
    if cfg.get("ablations",{}).get("diminishing_returns"):
        saturation=1
    delta=round(sign*(base+rules["surprise_weight"]*surprise)*saturation*outcome.importance,6)
    c.consequences[key]=Consequence(key,outcome.day+rules["reputation_delay"],"reputation",outcome.id,delta)
    audience_id=f"{outcome.id}:audience"
    c.consequences[audience_id]=Consequence(audience_id,outcome.day+rules["audience_delay"],
                                           "audience",key,sign*outcome.visibility)
    emit("consequences_queued",outcome.id,before=0,after=2,cause=outcome.id,
         reputation_delta=delta,reputation_due=c.consequences[key].due,
         audience_due=c.consequences[audience_id].due)


def apply_due(c: Company, day: int, cfg: dict, emit) -> None:
    for event in sorted(c.consequences.values(),key=lambda x:(x.due,x.id)):
        if event.applied or event.due > day:
            continue
        if event.kind=="reputation":
            before=c.reputation
            c.reputation=round(max(0,min(100,c.reputation+event.amount)),6)
            after=c.reputation
        elif event.kind=="audience":
            before=c.audience
            delta=cfg["consequences"]["audience_rate"]*(c.reputation-c.audience)+event.amount*cfg["economy_rules"]["audience_visibility_weight"]
            c.audience=round(max(0,min(100,c.audience+delta)),6)
            after=c.audience
        elif event.kind.startswith("information"):
            domain=event.kind.split(":")[1]
            before=c.information[domain]
            c.information[domain]=min(cfg["information_rules"]["maximum_quality"],before+cfg["information_rules"]["investment_gain"])
            after=c.information[domain]
            c.loads.pop(event.kind,None)
        elif event.kind=="support":
            before=capacity(c)
            c.capacity_inputs["support"]=cfg["organization"]["support_gain"]
            c.loads.pop("support",None)
            contract=Contract(f"support:{day}","support",cfg["finance"]["support_cost_per_period"],day)
            c.contracts[contract.id]=contract
            schedule_contract(c,contract,cfg)
            after=capacity(c)
        else:
            raise ValueError(f"Unknown consequence {event.kind}")
        event.applied=True
        emit(event.kind,event.id,before=before,after=after,cause=event.source)


def restructure(c: Company, action: str, day: int, cfg: dict, emit) -> None:
    if action=="sell_star":
        if "S" not in c.players:
            raise ValueError("No star contract to sell")
        before=c.cash
        c.cash+=cfg["finance"]["star_sale"]
        emit("asset_sale",f"sale:S:{day}",before=before,after=c.cash,cause="contract:S")
        replacement=Talent("RPL",cfg["economy_rules"]["replacement_execution"],cfg["economy_rules"]["replacement_adaptability"],cfg["finance"]["replacement_fee"],cfg["finance"]["prospect_salary"])
        replace_player(c,replacement,day,cfg,emit,old_id="S")
        c.loads.pop("star",None)
    elif action=="cancel_investment":
        pending=[x for x in c.consequences.values() if not x.applied and x.kind in c.investments]
        if not pending:
            raise ValueError("No unfinished investment")
        event=sorted(pending,key=lambda x:x.id)[0]
        event.applied=True
        c.loads.pop(event.kind,None)
        before=c.cash
        cost=cfg["finance"]["information_cost"] if event.kind.startswith("information") else cfg["finance"]["support_cost"]
        refund=cost//cfg["economy_rules"]["cancellation_refund_divisor"]
        c.cash+=refund
        emit("investment_cancelled",event.id,before=before,after=c.cash,cause=event.id,
             sacrifice="Forfeit planned capability improvement and most acquisition cost")
    else:
        raise ValueError("Unknown restructuring action")
    before=c.stage
    c.stage="Restructuring"
    c.restructured_day=day
    emit("restructuring",f"restructure:{day}",before=before,after=c.stage,cause=action)


def update_distress(c: Company, day: int, cfg: dict, emit) -> None:
    unpaid=[x for x in c.financial.values() if x.status=="overdue"]
    f=forecast_cash(c,day,cfg["distress"]["forecast_days"])
    before=c.stage
    if unpaid:
        exhausted=("S" not in c.players and any(c.financial[d.payment_id].status!="paid" for d in c.debts) and
                   not any(not x.applied and x.kind in c.investments for x in c.consequences.values()))
        old=day-min(x.missed_day for x in unpaid)
        c.stage="Terminal" if old>=cfg["distress"]["grace_days"] and exhausted else (
            "Restructuring" if c.restructured_day is not None else "Distress")
    elif c.restructured_day is not None:
        if (f>=0 and day-c.restructured_day>=cfg["distress"]["stabilization_days"] and
            load(c)/max(1,capacity(c))<=cfg["distress"]["maximum_recovery_load_ratio"]):
            c.stage="Stabilized"
    else:
        c.stage="Warning" if f<0 else "Stable"
    if c.stage!=before:
        emit("distress_stage",f"distress:{day}",before=before,after=c.stage,forecast=f,
             cause=[x.id for x in unpaid] or "committed_cash_forecast")
