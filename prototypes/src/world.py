"""Lower-fidelity external state and a shared finite opportunity pool."""
from .state import Match, World, Opportunity, draw


def schedule(cfg=None) -> list[Match]:
    if cfg is None:
        from .state import load_calibration
        cfg=load_calibration()
    ring=["C","R1","R2","R3","R4",None]
    first=[]
    for round_index in range(5):
        pairs=[(ring[i],ring[-1-i]) for i in range(3)]
        first.append([(a,b) for a,b in pairs if a is not None and b is not None])
        ring=[ring[0],ring[-1],*ring[1:-1]]
    out=[]
    for i,pairs in enumerate(first+[[(b,a) for a,b in r] for r in first]):
        for j,(home,away) in enumerate(pairs):
            out.append(Match(f"match:{i+1}:{j+1}",cfg["calendar"]["round_days"][i],home,away))
    return out


def update(w: World, commercial: float, seed: int, cfg: dict, emit, commercial_context=None) -> None:
    if w.day in cfg["calendar"]["development_days"]:
        for rival in sorted(w.rivals.values(),key=lambda x:x.id):
            before=rival.strength
            delta=cfg["world_rules"]["developer_gain"] if rival.id=="R3" else (draw(seed,"rival_development",f"{rival.id}:{w.day}")-0.5)*cfg["world_rules"]["other_development_range"]
            rival.strength=round(before+delta,6)
            emit("rival_development",f"{rival.id}:{w.day}",before=before,after=rival.strength,
                 cause="bounded_development_not_player_results")
    if w.day in cfg["commercial"]["offer_days"]:
        rules=cfg["commercial"]
        payment=min(rules["offer_ceiling"],round(rules["offer_base"]+commercial*rules["value_coefficient"]))
        for premium in (False,True):
            offer_id=f"offer:{w.day}:{'premium' if premium else 'standard'}"
            offer=Opportunity(offer_id,payment+(rules["premium_increment"] if premium else 0),
                              w.day+rules["offer_ttl"],rules["offer_duration"],
                              rules["premium_load"] if premium else rules["standard_load"],
                              rules["premium_rep_requirement"] if premium else 0)
            w.opportunities[offer_id]=offer
            emit("sponsor_opportunity",offer_id,before=0,after=offer.payment,
                 cause=(commercial_context or {}).get("causes",["initial_company"]),
                 inputs=commercial_context or {},
                 commercial_value=commercial,expiry=offer.expiry,load=offer.load,
                 minimum_reputation=offer.minimum_reputation)
    for offer in sorted(w.opportunities.values(),key=lambda o:o.id):
        if offer.claimed_by is None and w.day>=offer.expiry:
            rival=sorted(w.rivals)[int(draw(seed,"sponsor_competition",offer.id)*len(w.rivals))]
            offer.claimed_by=rival
            w.rivals[rival].acquisitions.append(offer.id)
            emit("rival_claim",offer.id,before="available",after=rival,cause="finite_sponsor_market")
    if w.day==cfg["calendar"]["talent_claim_day"]:
        available=[t for t in w.talent.values() if t.claimed_by is None]
        if available:
            chosen=sorted(available,key=lambda t:t.id)[0]
            chosen.claimed_by="R1"
            w.rivals["R1"].acquisitions.append(chosen.id)
            emit("rival_claim",chosen.id,before="available",after="R1",cause="finite_talent_market")


def update_meta(w: World, cfg: dict, emit) -> None:
    key=str(w.day)
    if key in cfg["meta_schedule"] and not cfg.get("ablations",{}).get("meta"):
        before=w.meta
        w.previous_meta=w.meta
        w.meta=cfg["meta_schedule"][key]
        w.meta_changed_day=w.day
        emit("meta_change",f"meta:{w.day}",before=before,after=w.meta,cause="bounded_external_calendar")


def resolve_rival_matches(w: World, seed: int, cfg: dict, emit) -> None:
    for match in w.schedule:
        if match.day!=w.day or "C" in (match.home,match.away) or match.id in w.results:
            continue
        a,b=w.rivals[match.home],w.rivals[match.away]
        p=min(cfg["competition"]["probability_ceiling"],max(cfg["competition"]["probability_floor"],0.5+(a.strength-b.strength)/cfg["world_rules"]["rival_match_scale"]))
        winner=a.id if draw(seed,"rival_match",match.id)<p else b.id
        w.results[match.id]={"id":match.id,"day":w.day,"home":a.id,"away":b.id,"winner":winner}
        a.result_ids.append(match.id)
        b.result_ids.append(match.id)
        emit("rival_result",match.id,before="scheduled",after=winner,cause=match.id,probability=round(p,6))
