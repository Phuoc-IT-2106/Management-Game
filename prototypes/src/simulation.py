"""Ordered resumable daily simulation and replayable decision execution."""
from __future__ import annotations

from copy import deepcopy
from dataclasses import asdict
from . import economy, competition, information, coach, world as world_logic
from .state import (VERSION, Decision, DecisionCheckpoint, CompetitiveOutcome, digest,
                    load_calibration, state_hash)
from .fixtures import initialize, scenarios
from .policies import choose


class Simulation:
    def __init__(self, scenario="A", seed=1000, policy=None, cfg=None, context="baseline",
                 overrides=None, keep_trace=True):
        if scenario not in scenarios():
            raise ValueError("Scenario must be A–M")
        self.scenario,self.seed,self.context=scenario,int(seed),context
        self.policy=policy or scenarios()[scenario]["policy"]
        self.cfg=deepcopy(cfg or load_calibration())
        self.overrides=deepcopy(overrides or {})
        self.company,self.world=initialize(scenario,self.cfg,context,self.overrides)
        self.initial={"company":asdict(self.company),"world":asdict(self.world)}
        self.cursor=0
        self.pending=None
        self.finished=False
        self.trace=[]
        self.decisions=[]
        self.keep_trace=keep_trace
        self.review_history=[]
        self.checkpoint_count=0
        self.emit("initial_state","initial_company",before=None,after=digest(self.initial),
                  cause="canonical_fixture_and_noncanonical_calibration")

    def emit(self, kind, source, **details):
        if self.keep_trace:
            self.trace.append({"day":self.world.day,"kind":kind,"source":source,
                               "cause":details.pop("cause",source),**details})

    def _investment_checkpoint(self):
        c,w=self.company,self.world
        if "investment:initial" in c.decisions_done:
            return None
        choices=["retain"]
        f=self.cfg["finance"]
        if "P1" in c.players:
            star=w.talent["S"]
            prospect=w.talent["T"]
            if star.claimed_by is None:
                if c.cash>=star.cost:
                    choices.append("star")
                if c.cash+f["bridge_principal"]>=star.cost and not c.debts:
                    choices.append("finance_star")
            if prospect.claimed_by is None and c.cash>=prospect.cost:
                choices.append("prospect")
        if c.cash>=f["information_cost"]:
            choices.append("information")
        if c.cash>=f["support_cost"]:
            choices.append("support")
        return DecisionCheckpoint("investment:initial",w.day,"investment",
            "Allocate cash or preserve flexibility before the first competition cycle",tuple(choices),
            information.observe(c,w,self.seed,self.cfg))

    def _checkpoint(self):
        c,w=self.company,self.world
        initial=self._investment_checkpoint()
        if initial:
            return initial
        # New stage / new overdue item can produce a material recovery decision, not a daily prompt.
        unpaid=tuple(sorted(x.id for x in c.financial.values() if x.status=="overdue"))
        key=f"recovery:{c.stage}:{'|'.join(unpaid)}"
        pressure=bool(unpaid) or economy.forecast_cash(c,w.day,self.cfg["distress"]["forecast_days"])<0
        if pressure and c.stage in ("Warning","Distress","Restructuring") and key not in c.decisions_done:
            choices=["wait"]
            if "S" in c.players:
                choices.append("sell_star")
            if not any(c.financial[d.payment_id].status!="paid" for d in c.debts):
                choices.append("bridge")
            if any(not x.applied and x.kind in c.investments for x in c.consequences.values()):
                choices.append("cancel_investment")
            if len(choices)>1:
                return DecisionCheckpoint(key,w.day,"recovery","Committed cash coverage or unpaid obligations",
                                          tuple(choices),information.observe(c,w,self.seed,self.cfg))
        offers=[o for o in w.opportunities.values() if o.claimed_by is None and o.expiry>w.day]
        offer_key="sponsor:"+"|".join(sorted({o.id.rsplit(":",1)[0] for o in offers}))
        if offers and offer_key not in c.decisions_done:
            return DecisionCheckpoint(offer_key,w.day,"sponsor","Finite offers with payment, workload and conditions",
                                      tuple(["decline"]+sorted(o.id for o in offers)),
                                      information.observe(c,w,self.seed,self.cfg))
        for match in w.schedule:
            if "C" not in (match.home,match.away) or match.id in c.plans or match.id in w.results:
                continue
            if not 0<=match.day-w.day<=self.cfg["competition"]["preparation_days"]:
                continue
            rival_id=match.away if match.home=="C" else match.home
            view=information.competitive_observation(c,w,rival_id,match.id,self.seed,self.cfg)
            rec=coach.recommend(view,c.envelope,self.cfg)
            constraints=(("risk_ceiling",c.envelope.risk_ceiling),("protected",c.envelope.protected),
                         ("eligible_lineups",tuple(lineup for lineup,_ in view.profiles)))
            if not rec.plans:
                return DecisionCheckpoint(f"escalation:{match.id}:{w.day}",w.day,"escalation",rec.reason,(),
                    information.observe(c,w,self.seed,self.cfg,view),context_id=match.id,constraints=constraints)
            plans=list(rec.plans)
            best=rec.plans[rec.selected]
            modes=(c.envelope.preparation,c.envelope.lineup,c.envelope.posture)
            if not rec.escalation:
                for mode,attribute in zip(modes,("allocation","lineup","posture")):
                    if mode=="autonomous":
                        plans=[p for p in plans if getattr(p,attribute)==getattr(best,attribute)]
            approval=rec.escalation or any(m!="autonomous" for m in modes)
            return DecisionCheckpoint(f"plan:{match.id}:{w.day}",w.day,"competition",rec.reason,
                       tuple(str(i) for i in range(len(plans))),information.observe(c,w,self.seed,self.cfg,view),
                       tuple(plans),str(plans.index(best)),match.id,approval,constraints)
        return None

    def advance(self):
        """Run to a meaningful checkpoint, retaining an exact within-day cursor."""
        if self.pending:
            return self.pending
        c,w,cfg=self.company,self.world,self.cfg
        while not self.finished:
            if self.cursor==0:
                if w.day>=cfg["horizon"] or c.stage=="Terminal":
                    self.finished=True
                    break
                w.day+=1
                self.cursor=1
            elif self.cursor==1:
                economy.sponsor_conditions(c,w.day,cfg,self.emit)
                economy.settle(c,w.day,self.emit)
                self.cursor=2
            elif self.cursor==2:
                if w.day==c.sponsor.expiry+1:
                    before=c.loads.pop("sponsor_extra",0)
                    self.emit("sponsor_expiry",c.sponsor.id,before=before,after=0,cause=c.sponsor.id)
                for p in c.players.values():
                    before=p.readiness
                    p.readiness=round(min(100,p.readiness+cfg["competition"]["readiness_recovery"]),6)
                    if before!=p.readiness:
                        self.emit("readiness",f"readiness:{p.id}:{w.day}",before=before,after=p.readiness,
                                  cause="daily_people_recovery")
                self.cursor=3
            elif self.cursor==3:
                factor=economy.efficiency(c,cfg)
                for match in w.schedule:
                    if match.id not in c.plans or match.id in w.results or w.day>match.day:
                        continue
                    work=c.preparation_work.setdefault(match.id,[])
                    raw=cfg["competition"]["preparation_scale"]*c.coach["preparation"]/100/cfg["competition"]["preparation_days"]
                    before=sum(x[1] for x in work)
                    converted=round(raw*factor,6)
                    work.append((w.day,converted))
                    self.emit("preparation_work",f"{match.id}:work:{w.day}",before=before,
                              after=before+converted,raw=raw,efficiency=factor,
                              load=economy.load(c),capacity=economy.capacity(c),cause=f"plan:{match.id}")
                self.cursor=4
            elif self.cursor==4:
                commercial_context={"reputation":c.reputation,"audience":c.audience,
                    "causes":[x.id for x in c.consequences.values() if x.applied and
                              x.kind in ("reputation","audience")][-4:] or ["initial_company"]}
                world_logic.update(w,economy.commercial_value(c,cfg),self.seed,cfg,self.emit,commercial_context)
                self.cursor=5
            elif self.cursor==5:
                world_logic.update_meta(w,cfg,self.emit)
                self.cursor=6
            elif self.cursor==6:
                self.pending=self._checkpoint()
                if self.pending:
                    return self.pending
                self.cursor=7
            elif self.cursor==7:
                self._resolve_matches()
                self.cursor=8
            elif self.cursor==8:
                economy.apply_due(c,w.day,cfg,self.emit)
                self.cursor=9
            elif self.cursor==9:
                economy.update_distress(c,w.day,cfg,self.emit)
                if w.day%7==0 or c.stage in ("Distress","Restructuring"):
                    self.review_history.append({"day":w.day,"cash":c.cash,"reputation":c.reputation,
                        "audience":c.audience,"forecast":economy.forecast_cash(c,w.day,14),"stage":c.stage})
                self.cursor=0
        return None

    def _resolve_matches(self):
        c,w,cfg=self.company,self.world,self.cfg
        world_logic.resolve_rival_matches(w,self.seed,cfg,self.emit)
        for match in w.schedule:
            if match.day!=w.day or "C" not in (match.home,match.away) or match.id in w.results:
                continue
            plan=c.plans[match.id]
            own=competition.profile(c,plan.lineup,w.day,cfg)
            rival_id=match.away if match.home=="C" else match.home
            rival=w.rivals[rival_id]
            effort=sum(x[1] for x in c.preparation_work.get(match.id,[]))
            outcome=competition.resolve(match_id=match.id,day=w.day,rival_id=rival_id,own=own,
                rival_strength=rival.strength,rival_posture=rival.posture,
                rival_adaptation=rival.adaptation*rival.information/10000,plan=plan,effort=effort,
                meta=w.meta,meta_change=abs(w.meta-w.previous_meta) if w.day-w.meta_changed_day<cfg["world_rules"]["meta_memory_days"] else 0,
                exposed=competition.exposure(c)[plan.posture],seed=self.seed,cfg=cfg)
            w.results[match.id]=outcome
            c.result_ids.append(match.id)
            rival.result_ids.append(match.id)
            self.emit("competitive_outcome",outcome.id,before="scheduled",after="win" if outcome.won else "loss",
                      probability=outcome.probability,factors=dict(outcome.factors),cause=f"plan:{match.id}")
            # Owning People process consumes the completed participation signal.
            for player_id in plan.lineup:
                p=c.players[player_id]
                before=p.readiness
                p.readiness=round(max(0,p.readiness-cfg["competition"]["readiness_cost"]),6)
                self.emit("readiness",f"{match.id}:{player_id}",before=before,after=p.readiness,cause=match.id)
            economy.queue_outcome(c,outcome,cfg,self.emit)

    def submit(self, decision: Decision, actor="player"):
        cp=self.pending
        if cp is None or decision.checkpoint_id!=cp.id or decision.choice not in cp.choices:
            raise ValueError("Decision does not match a pending checkpoint and legal choice")
        c,w,cfg=self.company,self.world,self.cfg
        action=decision.choice
        # All possible failure checks precede mutations, including compound borrowing/signing.
        if cp.kind=="competition":
            plan=cp.plans[int(action)]
            if plan.lineup not in competition.lineups(c):
                raise ValueError("Lineup eligibility changed")
            c.plans[cp.context_id]=plan
            c.preparation_work.setdefault(cp.context_id,[])
            self.emit("plan_committed",f"plan:{cp.context_id}",before=None,after=asdict(plan),cause=cp.id)
        elif cp.kind=="investment":
            if action in ("star","prospect","finance_star"):
                talent=w.talent["T" if action=="prospect" else "S"]
                if talent.claimed_by is not None or "P1" not in c.players:
                    raise ValueError("Talent is no longer available")
                if action=="finance_star":
                    if any(c.financial[d.payment_id].status!="paid" for d in c.debts) or c.cash+cfg["finance"]["bridge_principal"]<talent.cost:
                        raise ValueError("Financed signing cannot be completed")
                    economy.borrow(c,w.day,cfg,self.emit)
                economy.replace_player(c,talent,w.day,cfg,self.emit)
                talent.claimed_by="C"
            elif action in ("information","support"):
                economy.invest(c,"information:opponent" if action=="information" else action,w.day,cfg,self.emit)
        elif cp.kind=="sponsor" and action!="decline":
            offer=w.opportunities[action]
            economy.accept_sponsor(c,offer,w.day,cfg,self.emit)
            offer.claimed_by="C"
        elif cp.kind=="recovery":
            if action=="bridge":
                economy.borrow(c,w.day,cfg,self.emit)
                if c.stage in ("Distress","Restructuring"):
                    before=c.stage
                    c.stage="Restructuring"
                    c.restructured_day=w.day
                    self.emit("restructuring",f"bridge_restructure:{w.day}",before=before,after=c.stage,cause=f"bridge:{w.day}")
            elif action!="wait":
                economy.restructure(c,action,w.day,cfg,self.emit)
            if action!="wait":
                economy.settle(c,w.day,self.emit)
        c.decisions_done.append(cp.id)
        if cp.requires_approval:
            self.checkpoint_count+=1
        record={"checkpoint_id":cp.id,"day":w.day,"kind":cp.kind,"choice":action,
                "actor":actor,"requires_approval":cp.requires_approval,"reason":cp.reason,
                "observation":asdict(cp.observation),"constraints":dict(cp.constraints),
                "recommended_choice":cp.recommended}
        if cp.kind=="competition":
            record["plan"]=asdict(c.plans[cp.context_id])
            record["forecast"]=competition.forecast(cp.observation.competitive,c.plans[cp.context_id],cfg)
        self.decisions.append(record)
        self.emit("decision",cp.id,before="pending",after=action,cause=cp.reason,actor=actor)
        self.pending=None

    def finish(self, actions=None):
        supplied={x["checkpoint_id"]:x["choice"] for x in (actions or [])}
        if actions is not None and len(supplied)!=len(actions):
            raise ValueError("Action file contains duplicate checkpoint IDs")
        used=set()
        while (cp:=self.advance()) is not None:
            if cp.kind=="escalation":
                raise ValueError(f"Coach escalation at {cp.id}: {cp.reason}; authority or roster needs revision")
            if cp.id in supplied:
                decision=Decision(cp.id,supplied[cp.id])
                used.add(cp.id)
            elif not cp.requires_approval:
                decision=Decision(cp.id,cp.recommended)
            elif actions is not None:
                raise ValueError(f"Action file lacks required checkpoint {cp.id}")
            else:
                decision=choose(cp,self.policy,self.cfg)
            self.submit(decision,"player" if cp.requires_approval else "coach")
        if supplied.keys()-used:
            raise ValueError("Action file contains unused checkpoint IDs")
        return self.artifact()

    def metrics(self):
        c,w=self.company,self.world
        outcomes=[w.results[x] for x in c.result_ids]
        pending=[x for x in c.financial.values() if x.status in ("pending","overdue")]
        return {"day":w.day,"wins":sum(x.won for x in outcomes),"matches":len(outcomes),
            "mean_win_probability":round(sum(x.probability for x in outcomes)/max(1,len(outcomes)),6),
            "cash":c.cash,"committed_cash":c.cash+sum(x.amount*(1 if x.direction=="in" else -1) for x in pending),
            "outstanding_obligations":economy.outstanding(c),
            "post_horizon_obligations":sum(x.amount for x in pending if x.direction=="out" and x.due>self.cfg["horizon"]),
            "debt_due":sum(c.financial[d.payment_id].amount for d in c.debts if c.financial[d.payment_id].status!="paid"),
            "overdue":sum(x.amount for x in pending if x.status=="overdue"),
            "stage":c.stage,"viable":c.stage!="Terminal" and not any(x.status=="overdue" for x in pending),
            "reputation":c.reputation,"audience":c.audience,"commercial_value":economy.commercial_value(c,self.cfg),
            "load":economy.load(c),"capacity":economy.capacity(c),"approvals":self.checkpoint_count,
            "autonomous_decisions":sum(not x["requires_approval"] for x in self.decisions),
            "rival_claims":sum(len(r.acquisitions) for r in w.rivals.values())}

    def artifact(self):
        return {"simulation_version":VERSION,"calibration":deepcopy(self.cfg),"calibration_hash":digest(self.cfg),
            "scenario":self.scenario,"seed":self.seed,"policy":self.policy,"context":self.context,
            "overrides":self.overrides,"initial_state":self.initial,"decisions":self.decisions,
            "final_state":{"company":asdict(self.company),"world":asdict(self.world)},
            "state_hash":state_hash(self.company,self.world),"metrics":self.metrics(),
            "reviews":self.review_history,"trace":self.trace,"trace_hash":digest(self.trace)}


def replay(artifact):
    if artifact["simulation_version"]!=VERSION:
        raise ValueError("Replay simulation version differs")
    if digest(artifact["calibration"])!=artifact["calibration_hash"]:
        raise ValueError("Replay calibration hash differs")
    sim=Simulation(artifact["scenario"],artifact["seed"],artifact["policy"],artifact["calibration"],
                   artifact["context"],artifact["overrides"])
    if digest(sim.initial)!=digest(artifact["initial_state"]):
        raise ValueError("Initial fixture state differs; simulation version must be updated")
    result=sim.finish(artifact["decisions"])
    return {"matched":result["state_hash"]==artifact["state_hash"] and result["trace_hash"]==artifact["trace_hash"],
            "state_hash":result["state_hash"],"trace_hash":result["trace_hash"]}
