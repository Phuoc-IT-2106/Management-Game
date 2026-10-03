"""Measured evidence, including counterfactual probes; never presets a pass outcome."""
from __future__ import annotations
from copy import deepcopy
from dataclasses import asdict, replace
import math
from pathlib import Path
import json
import statistics

from .state import (VERSION, Decision, FinancialItem, Plan, Profile, Estimate, CompetitiveView,
                    digest, load_calibration)
from .simulation import Simulation, replay
from .fixtures import initialize
from . import competition, economy, information, coach
from .policies import choose


def seed_range(value: str) -> list[int]:
    if ":" not in value:
        return [int(value)]
    a,b=(int(x) for x in value.split(":"))
    if a>b or b-a>10000:
        raise ValueError("Seed range must be ordered and contain at most 10001 seeds")
    return list(range(a,b+1))


def write_json(path: Path, data):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2,sort_keys=True)+"\n",encoding="utf-8")


def summary(rows):
    keys=("wins","mean_win_probability","cash","committed_cash","reputation","audience",
          "approvals","autonomous_decisions","overdue","debt_due","post_horizon_obligations","rival_claims")
    n=len(rows)
    result={"runs":n,"viability_rate":sum(x["viable"] for x in rows)/n}
    result.update({key:round(statistics.mean(x[key] for x in rows),6) for key in keys})
    p=result["viability_rate"]
    z=1.96
    center=(p+z*z/(2*n))/(1+z*z/n)
    half=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n))/(1+z*z/n)
    result["viability_wilson_95"]=[round(center-half,6),round(center+half,6)]
    return result


def compare(seeds, cfg=None):
    cfg=cfg or load_calibration()
    contexts={}
    for context in ("baseline","liquidity","growth"):
        contexts[context]={}
        for policy in ("conservative","aggressive","adaptive"):
            rows=[]
            for seed in seeds:
                result=Simulation("A",seed,policy,cfg,context,keep_trace=False).finish()
                rows.append({"seed":seed,**result["metrics"],"state_hash":result["state_hash"]})
            contexts[context][policy]={"summary":summary(rows),"rows":rows}
    viable=[p for p in ("conservative","aggressive","adaptive") if any(
        contexts[c][p]["summary"]["viability_rate"]>=0.8 for c in contexts)]
    dominating=[]
    dimensions=("viability_rate","committed_cash","wins")
    for policy in ("conservative","aggressive","adaptive"):
        others=[p for p in ("conservative","aggressive","adaptive") if p!=policy]
        dominates=True
        strict=False
        for context in contexts:
            for other in others:
                a,b=contexts[context][policy]["summary"],contexts[context][other]["summary"]
                if any(a[k]<b[k] for k in dimensions):
                    dominates=False
                strict=strict or any(a[k]>b[k] for k in dimensions)
        if dominates and strict:
            dominating.append(policy)
    return {"version":VERSION,"calibration_hash":digest(cfg),"seeds":seeds,"contexts":contexts,
            "viable_policies":viable,"universally_dominating_policies":dominating,
            "ac01_pass":len(viable)>=2 and not dominating,
            "limitation":"Empirical results on three policies/contexts; not proof about every possible strategy."}


def controlled_probes(seeds, cfg):
    lineup=("P1","P2","P3","P4","P5")
    prep={}
    for name,coordination,change in (("new_lineup",77,0),("known_opponent",90,0),("meta_shift",90,2)):
        own=Profile(65,65,70,coordination)
        probabilities=[]
        for allocation in cfg["preparation_options"][:3]:
            plan=Plan(tuple(allocation),lineup,"Balanced","Balanced",1 if change else 0)
            parts=competition.factors(own,65,"Balanced",0.5,plan,6.12,plan.meta,change,0,cfg)
            probabilities.append(competition.probability(parts,plan.posture,cfg))
        prep[name]={"probabilities":probabilities,"preferred":max(range(3),key=lambda i:probabilities[i])}
    reversals=sum(prep[a]["preferred"]!=prep[b]["preferred"] for i,a in enumerate(prep) for b in list(prep)[i+1:])
    errors={"low":[],"high":[]}
    for seed in seeds:
        for name,q in (("low",10),("high",90)):
            errors[name].append(abs(information.estimate(70,q,seed,"probe",cfg).value-70))
    plan=Plan((0.7,0.15,0.15),lineup,"Aggressive","Conservative",0)
    own=Profile(70,60,70,70)
    weak=competition.factors(own,66,"Aggressive",0.85,plan,1,0,0,1,cfg)
    better_plan=replace(plan,allocation=(0.15,0.7,0.15),target_posture="Aggressive",posture="Balanced")
    better=competition.factors(own,66,"Aggressive",0.85,better_plan,8,0,0,0,cfg)
    upset={"stronger_roster_capability":70,"opponent_capability":66,
           "poor_plan_probability":competition.probability(weak,plan.posture,cfg),
           "better_plan_probability":competition.probability(better,better_plan.posture,cfg)}
    adaptation={}
    for name,ability in (("uninformed",0),("informed",0.9)):
        parts=competition.factors(own,66,"Aggressive",ability,plan,6,0,0,1,cfg)
        adaptation[name]=parts["rival_adaptation"]
    events=[]
    emit=lambda kind,source,**details:events.append({"kind":kind,"source":source,**details})
    bridge=[]
    for use_bridge in (False,True):
        c,w=initialize("A",cfg)
        c.cash=50
        c.financial={"due":FinancialItem("due",7,150,"out","contract","salary"),
                     "receipt":FinancialItem("receipt",14,300,"in","signed_sponsor","sponsor")}
        if use_bridge:
            economy.borrow(c,1,cfg,emit)
        economy.settle(c,7,emit)
        paid_on_time=c.financial["due"].status=="paid"
        for day in (14,57,84):
            economy.settle(c,day,emit)
        bridge.append({"borrowed":use_bridge,"paid_on_time":paid_on_time,"ending_cash":c.cash,
                       "outstanding":economy.outstanding(c)})
    debt_contexts={"bridge_over_gap":bridge}
    c,w=initialize("A",cfg)
    before=c.cash-economy.outstanding(c)
    economy.borrow(c,80,cfg,emit)
    debt_contexts["unneeded_loan_net_liquidity_change"]=c.cash-economy.outstanding(c)-before
    # Diagnostic oracle exists only here. It is never passed into coach or management policies.
    improvements=[]
    for seed in seeds:
        c,w=initialize("D",cfg)
        view=information.competitive_observation(c,w,"R1","diagnostic",seed,cfg)
        rec=coach.recommend(view,c.envelope,cfg)
        actual=[]
        for p in rec.plans:
            parts=competition.factors(dict(view.profiles)[p.lineup],w.rivals["R1"].strength,
                w.rivals["R1"].posture,view.rival_adaptation,p,6.12,0,0,0,cfg)
            actual.append(competition.probability(parts,p.posture,cfg))
        improvements.append(max(actual)-actual[rec.selected])
    return {"preparation_contexts":prep,"preparation_rank_reversals":reversals,
            "information_mae":{k:statistics.mean(v) for k,v in errors.items()},
            "upset":upset,"adaptation":adaptation,"debt":debt_contexts,
            "oracle_override_better_cases":sum(x>0.02 for x in improvements),
            "oracle_override_max_probability_gain":max(improvements)}


def status(passed, evidence):
    return {"status":"PASS" if passed else "FAIL","evidence":evidence}


def suite(seeds, cfg=None, out=None):
    cfg=cfg or load_calibration()
    experiments=compare(seeds,cfg)
    probes=controlled_probes(seeds,cfg)
    scenario_results={}
    representatives={}
    repeat_matches=0
    for name in "ABCDEFGHIJKLM":
        rows=[]
        for seed in seeds:
            result=Simulation(name,seed,cfg=cfg,keep_trace=seed==seeds[0]).finish()
            other=Simulation(name,seed,cfg=cfg,keep_trace=False).finish()
            repeat_matches+=result["state_hash"]==other["state_hash"]
            rows.append({"seed":seed,**result["metrics"],"state_hash":result["state_hash"]})
            if seed==seeds[0]:
                representatives[name]=result
                if out:
                    write_json(Path(out)/"representative"/f"{name}.json",result)
        scenario_results[name]={"summary":summary(rows),"rows":rows}
    replay_results={name:replay(value) for name,value in representatives.items()}
    paired=0
    reduced=0
    meta_changes=0
    adaptation_choice_changes=0
    overload_choice_changes=0
    recovery_without_intervention=0
    adaptation_reduction=[]
    diminishing=[]
    for seed in seeds:
        untreated=Simulation("K",seed,cfg=cfg,keep_trace=False)
        while (checkpoint:=untreated.advance()) is not None:
            decision=Decision(checkpoint.id,"wait") if checkpoint.kind=="recovery" else choose(checkpoint,"recovery",cfg)
            untreated.submit(decision)
        recovery_without_intervention+=untreated.metrics()["viable"]
        e=Simulation("E",seed,cfg=cfg,keep_trace=False).finish()
        f=Simulation("F",seed,cfg=cfg,keep_trace=False).finish()
        ec,fc=deepcopy(e["final_state"]["company"]),deepcopy(f["final_state"]["company"])
        ec.pop("envelope");fc.pop("envelope")
        paired+=ec==fc and e["final_state"]["world"]==f["final_state"]["world"]
        reduced+=e["metrics"]["approvals"]<f["metrics"]["approvals"]
        off=deepcopy(cfg);off["ablations"]={"meta":True}
        h=Simulation("H",seed,cfg=cfg,keep_trace=False).finish()
        no_meta=Simulation("H",seed,cfg=off,keep_trace=False).finish()
        allocations=lambda r:[x["plan"]["allocation"] for x in r["decisions"] if x["kind"]=="competition"]
        meta_changes+=allocations(h)!=allocations(no_meta)
        off=deepcopy(cfg);off["ablations"]={"rival_adaptation":True}
        g=Simulation("G",seed,cfg=cfg,keep_trace=False).finish()
        no_adaptation=Simulation("G",seed,cfg=off,keep_trace=False).finish()
        adaptation_reduction.append(no_adaptation["metrics"]["mean_win_probability"]-g["metrics"]["mean_win_probability"])
        adaptive_g=Simulation("G",seed,policy="adaptive",cfg=cfg,keep_trace=False).finish()
        adaptive_g_off=Simulation("G",seed,policy="adaptive",cfg=off,keep_trace=False).finish()
        plans=lambda r:[x["plan"] for x in r["decisions"] if x["kind"]=="competition"]
        adaptation_choice_changes+=plans(adaptive_g)!=plans(adaptive_g_off)
        off=deepcopy(cfg);off["ablations"]={"overload":True}
        adaptive_i=Simulation("I",seed,policy="adaptive",cfg=cfg,keep_trace=False).finish()
        adaptive_i_off=Simulation("I",seed,policy="adaptive",cfg=off,keep_trace=False).finish()
        overload_choice_changes+=plans(adaptive_i)!=plans(adaptive_i_off)
        off=deepcopy(cfg);off["ablations"]={"diminishing_returns":True}
        m=Simulation("M",seed,cfg=cfg).finish()
        no_diminishing=Simulation("M",seed,cfg=off).finish()
        raw=lambda r:sum(x["reputation_delta"] for x in r["trace"] if x["kind"]=="consequences_queued" and x["reputation_delta"]>0)
        diminishing.append(raw(no_diminishing)-raw(m))
    probes.update({"manual_coach_parity_pairs":paired,"delegation_reduced_approval_cases":reduced,
                   "meta_changed_allocation_cases":meta_changes,
                   "rival_adaptation_changed_plan_cases":adaptation_choice_changes,
                   "overload_changed_plan_cases":overload_choice_changes,
                   "recovery_without_intervention_viability":recovery_without_intervention/len(seeds),
                   "mean_rival_adaptation_probability_cost":statistics.mean(adaptation_reduction),
                   "mean_raw_reputation_growth_reduction":statistics.mean(diminishing)})
    trace=representatives
    kind=lambda name,k:[x for x in trace[name]["trace"] if x["kind"]==k]
    valid_trace=all(all(x.get("cause") and "before" in x and "after" in x for x in r["trace"])
                    for r in representatives.values())
    # Isolated commercial-value probe and exact process conversion are regression tested separately too.
    c,w=initialize("A",cfg)
    cash=c.cash
    c.reputation=95;c.audience=95;w.day=14
    from .world import update
    update(w,economy.commercial_value(c),seeds[0],cfg,lambda *a,**k:None)
    free_cash_absent=c.cash==cash
    saturation_on=statistics.mean(diminishing)>0
    work=kind("I","preparation_work")
    single_overload=bool(work) and all(abs(x["after"]-x["before"]-x["raw"]*x["efficiency"])<1e-5 for x in work)
    sacrifices=[x for x in kind("L","roster_replacement") if x["removed"]=="S" and x["after"]<x["before"]]
    applied={x["source"]:x["day"] for x in trace["A"]["trace"] if x["kind"] in ("reputation","audience")}
    linked_offers=[x["source"] for x in kind("A","sponsor_opportunity")
                   if any(cause in applied and applied[cause]<x["day"] for cause in x["cause"])]
    signed={x["source"] for x in kind("A","sponsor_agreement")}
    paid={x["cause"] for x in kind("A","cash_settlement") if x["direction"]=="in"}
    commercial_chain=sorted(set(linked_offers)&signed&paid)
    n=len(seeds)
    ac={
      "AC-01":status(experiments["ac01_pass"],{"viable_policies":experiments["viable_policies"],"universal_dominators":experiments["universally_dominating_policies"]}),
      "AC-02":status(probes["preparation_rank_reversals"]>=3 and meta_changes>0,probes["preparation_contexts"]),
      "AC-03":status(bool(commercial_chain),{"representative":"A","linked_agreements":commercial_chain,"chain":"Outcome -> delayed reputation/audience -> offer -> agreement -> scheduled cash"}),
      "AC-04":status(free_cash_absent,{"cash_before_and_after_unsigned_offer":cash,"no_competition_cash_mutation":"pure resolver regression test"}),
      "AC-05":status(probes["information_mae"]["high"]<probes["information_mae"]["low"],probes["information_mae"]),
      "AC-06":status(paired==n and reduced>0 and probes["oracle_override_better_cases"]>0,{"parity_pairs":paired,"pairs":n,"fewer_approvals":reduced,"diagnostic_override_better":probes["oracle_override_better_cases"]}),
      "AC-07":status(probes["mean_rival_adaptation_probability_cost"]>0 and adaptation_choice_changes>0,{"mean_adaptation_probability_cost":probes["mean_rival_adaptation_probability_cost"],"changed_plan_cases":adaptation_choice_changes,"representative_claims":len(kind("G","rival_claim"))}),
      "AC-08":status(probes["debt"]["unneeded_loan_net_liquidity_change"]<0 and scenario_results["J"]["summary"]["post_horizon_obligations"]>0,probes["debt"]),
      "AC-09":status(scenario_results["K"]["summary"]["viability_rate"]>=0.8 and scenario_results["K"]["summary"]["viability_rate"]>recovery_without_intervention/n and bool(sacrifices),{"recovery_viability":scenario_results["K"]["summary"]["viability_rate"],"without_intervention_viability":recovery_without_intervention/n,"sacrifice":sacrifices}),
      "AC-10":status(saturation_on and bool(kind("M","rival_claim")) and any(x["direction"]=="out" for x in kind("M","cash_settlement")),{"mean_raw_reputation_growth_reduction":probes["mean_raw_reputation_growth_reduction"],"stress":scenario_results["M"]["summary"],"limit":"Evidence for this bounded horizon, not global balance proof"}),
      "AC-11":status(meta_changes>0 and adaptation_choice_changes>0 and overload_choice_changes>0 and single_overload,{"meta_changes":meta_changes,"adaptation_choice_changes":adaptation_choice_changes,"overload_choice_changes":overload_choice_changes,"single_overload_conversion":single_overload,"scripted_narrative_events":0}),
      "AC-12":{"status":"INCONCLUSIVE" if valid_trace else "FAIL","evidence":{"representative_traces":len(representatives),"machine_check":"all emitted changes have cause and before/after" if valid_trace else "missing fields","manual_review":"Pending review of exact representative trace hashes"}},
      "AC-13":status(repeat_matches==13*n and all(x["matched"] for x in replay_results.values()),{"repeated_runs_matched":repeat_matches,"repeated_pairs":13*n,"full_trace_replays":replay_results})
    }
    scenario_checks={
      "A":scenario_results["A"]["summary"]["viability_rate"]>=0.8,
      "B":scenario_results["B"]["summary"]["cash"]<scenario_results["A"]["summary"]["cash"] and bool(kind("B","roster_replacement")),
      "C":probes["upset"]["poor_plan_probability"]<0.5 and probes["upset"]["better_plan_probability"]>probes["upset"]["poor_plan_probability"]+0.05,
      "D":probes["oracle_override_better_cases"]>0 and scenario_results["D"]["summary"]["wins"]<8,
      "E":reduced>0,"F":paired==n,
      "G":probes["mean_rival_adaptation_probability_cost"]>0,"H":meta_changes>0,
      "I":single_overload and any(x["efficiency"]<1 for x in work),
      "J":probes["debt"]["bridge_over_gap"][1]["paid_on_time"] and not probes["debt"]["bridge_over_gap"][0]["paid_on_time"],
      "K":scenario_results["K"]["summary"]["viability_rate"]>=0.8,"L":bool(sacrifices),
      "M":ac["AC-10"]["status"]=="PASS"}
    for name,passed in scenario_checks.items():
        scenario_results[name]["status"]="PASS" if passed else "FAIL"
    result={"simulation_version":VERSION,"calibration_hash":digest(cfg),"seeds":seeds,
            "scenarios":scenario_results,"acceptance":ac,"probes":probes,"comparison":experiments,
            "recommendation":"PASS PROTOTYPE GATE" if all(x["status"]=="PASS" for x in ac.values()) and all(scenario_checks.values()) else "ITERATE",
            "limitations":["Finite policies, fixtures, seeds and horizon; no production balance claim.",
                           "Counterfactual true-state oracle is diagnostic only; never a policy input.",
                           "AC-12 semantic trace review remains a human/agent inspection step."]}
    if out:
        write_json(Path(out)/"verification.json",result)
        write_report(Path(out)/"VERIFICATION.md",result)
    return result


def finalize_trace_review(directory, review_file):
    """Accept a review only for the exact version, calibration and representative traces."""
    folder=Path(directory)
    result=json.loads((folder/"verification.json").read_text(encoding="utf-8"))
    review=json.loads(Path(review_file).read_text(encoding="utf-8"))
    if review["simulation_version"]!=result["simulation_version"] or review["calibration_hash"]!=result["calibration_hash"]:
        raise ValueError("Trace review version/calibration mismatch")
    reviewed=review["scenarios"]
    if set(reviewed)!=set("ABCDEFGHIJKLM"):
        raise ValueError("Trace review must cover A–M")
    for name,item in reviewed.items():
        artifact=json.loads((folder/"representative"/f"{name}.json").read_text(encoding="utf-8"))
        if item["trace_hash"]!=artifact["trace_hash"] or digest(artifact["trace"])!=item["trace_hash"]:
            raise ValueError(f"Trace review does not match {name}")
        if item["state_hash"]!=artifact["state_hash"] or not item.get("finding"):
            raise ValueError(f"Review lacks matching state/finding for {name}")
    passed=all(item["status"]=="PASS" for item in reviewed.values())
    result["acceptance"]["AC-12"]=status(passed,{"review":Path(review_file).name,"trace_hashes":{k:v["trace_hash"] for k,v in reviewed.items()},
                                               "scope":"Material decisions, competition, finances and recovery inspected across A–M"})
    result["recommendation"]="PASS PROTOTYPE GATE" if all(x["status"]=="PASS" for x in result["acceptance"].values()) and all(x["status"]=="PASS" for x in result["scenarios"].values()) else "ITERATE"
    result["limitations"]=[x for x in result["limitations"] if not x.startswith("AC-12")]
    write_json(folder/"verification.json",result)
    write_report(folder/"VERIFICATION.md",result)
    return result


def write_report(path: Path, result):
    lines=["# Prototype verification results","",f"Simulation: {result['simulation_version']}","",
           f"Calibration SHA-256: `{result['calibration_hash']}`","",
           f"Seeds: {result['seeds'][0]}–{result['seeds'][-1]} ({len(result['seeds'])})","",
           f"Recommendation from measured evidence and attached reviews: **{result['recommendation']}**","",
           "## Scenario matrix","","| Scenario | Result | Viability | Mean wins | Mean cash | Committed cash |","| --- | --- | ---: | ---: | ---: | ---: |"]
    for name,item in result["scenarios"].items():
        s=item["summary"]
        lines.append(f"| {name} | {item['status']} | {s['viability_rate']:.1%} | {s['wins']:.2f} | {s['cash']:.2f} | {s['committed_cash']:.2f} |")
    lines += ["","## Acceptance","","| Criterion | Result | Evidence |","| --- | --- | --- |"]
    for key,item in result["acceptance"].items():
        evidence=json.dumps(item["evidence"],ensure_ascii=False,separators=(",", ":"))
        if len(evidence)>600:
            evidence="Full measured evidence in verification.json; "+evidence[:450]+"…"
        lines.append(f"| {key} | {item['status']} | {evidence.replace('|','/')} |")
    lines += ["","## Policy comparison","","| Context | Policy | Viability | Wins | Cash after commitments |","| --- | --- | ---: | ---: | ---: |"]
    for context,policies in result["comparison"]["contexts"].items():
        for policy,item in policies.items():
            s=item["summary"]
            lines.append(f"| {context} | {policy} | {s['viability_rate']:.1%} | {s['wins']:.2f} | {s['committed_cash']:.2f} |")
    lines += ["","## Limitations",""]+["- "+x for x in result["limitations"]]
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text("\n".join(lines)+"\n",encoding="utf-8")
