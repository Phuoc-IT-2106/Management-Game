import unittest
from copy import deepcopy
from dataclasses import asdict, replace

from prototypes.src.state import (Decision, FinancialItem, Envelope, Plan, Profile, digest,
                                  draw, load_calibration, state_hash)
from prototypes.src.simulation import Simulation, replay
from prototypes.src.fixtures import initialize
from prototypes.src import economy, competition, information, coach, world
from prototypes.src.policies import choose


class SimulationTests(unittest.TestCase):
    def setUp(self):
        self.cfg=load_calibration()
        self.events=[]
        self.emit=lambda kind,source,**kw:self.events.append({"kind":kind,"source":source,**kw})

    def test_round_robin_has_eight_player_and_twenty_world_matches(self):
        schedule=world.schedule()
        self.assertEqual(20,len(schedule))
        self.assertEqual(8,sum("C" in (m.home,m.away) for m in schedule))
        for team in ["C","R1","R2","R3","R4"]:
            self.assertEqual(8,sum(team in (m.home,m.away) for m in schedule))

    def test_invalid_decision_is_atomic(self):
        sim=Simulation()
        cp=sim.advance()
        before=state_hash(sim.company,sim.world)
        trace=digest(sim.trace)
        with self.assertRaises(ValueError):
            sim.submit(Decision(cp.id,"invent_money"))
        self.assertEqual(before,state_hash(sim.company,sim.world))
        self.assertEqual(trace,digest(sim.trace))

    def test_duplicate_decision_rejected(self):
        sim=Simulation()
        cp=sim.advance()
        decision=Decision(cp.id,"retain")
        sim.submit(decision)
        with self.assertRaises(ValueError):
            sim.submit(decision)

    def test_payments_idempotent_and_receipts_first(self):
        c,w=initialize("A",self.cfg)
        c.cash=0
        c.financial={"bill":FinancialItem("bill",1,100,"out","contract","salary"),
                     "receipt":FinancialItem("receipt",1,100,"in","sponsor","sponsor")}
        economy.settle(c,1,self.emit)
        self.assertEqual("paid",c.financial["bill"].status)
        self.assertEqual(0,c.cash)
        n=len(self.events)
        economy.settle(c,1,self.emit)
        self.assertEqual(n,len(self.events))

    def test_late_cash_does_not_erase_missed_due_fact(self):
        c,w=initialize("A",self.cfg)
        c.cash=0
        c.financial={"bill":FinancialItem("bill",1,10,"out","contract","salary")}
        economy.settle(c,1,self.emit)
        self.assertEqual("overdue",c.financial["bill"].status)
        c.financial["prize"]=FinancialItem("prize",1,10,"in","result:late","prize")
        economy.settle(c,1,self.emit)
        self.assertEqual(1,c.financial["bill"].missed_day)
        self.assertEqual("paid",c.financial["bill"].status)

    def test_expensive_financing_has_real_obligation_beyond_horizon(self):
        c,w=initialize("A",self.cfg)
        before=c.cash
        economy.borrow(c,80,self.cfg,self.emit)
        debt=c.debts[0]
        item=c.financial[debt.payment_id]
        self.assertEqual(before+debt.principal,c.cash)
        self.assertGreater(item.amount,debt.principal)
        self.assertGreater(item.due,84)
        with self.assertRaises(ValueError):
            economy.borrow(c,81,self.cfg,self.emit)

    def test_competition_is_pure_and_emits_no_cash(self):
        c,w=initialize("A",self.cfg)
        view=information.competitive_observation(c,w,"R1","test",0,self.cfg)
        plan=coach.recommend(view,c.envelope,self.cfg).plans[0]
        before=state_hash(c,w)
        out=competition.resolve(match_id="test",day=1,rival_id="R1",own=dict(view.profiles)[plan.lineup],
            rival_strength=70,rival_posture="Balanced",rival_adaptation=0.5,plan=plan,effort=6,
            meta=0,meta_change=0,exposed=0,seed=0,cfg=self.cfg)
        self.assertEqual(before,state_hash(c,w))
        self.assertNotIn("cash",asdict(out))

    def test_delayed_consequences_apply_once(self):
        sim=Simulation("A",0)
        sim.finish()
        before=state_hash(sim.company,sim.world)
        economy.apply_due(sim.company,84,self.cfg,self.emit)
        self.assertEqual(before,state_hash(sim.company,sim.world))

    def test_information_does_not_change_true_capability_or_resolution_draw(self):
        c,w=initialize("A",self.cfg)
        lineup=competition.lineups(c)[0]
        p=competition.profile(c,lineup,0,self.cfg)
        before=draw(3,"match","test")
        for quality in (0,50,100):
            c.information["opponent"]=quality
            information.competitive_observation(c,w,"R1","test",3,self.cfg)
            self.assertEqual(p,competition.profile(c,lineup,0,self.cfg))
        self.assertEqual(before,draw(3,"match","test"))

    def test_observation_contains_estimate_not_true_opponent_or_future(self):
        c,w=initialize("D",self.cfg)
        view=information.competitive_observation(c,w,"R1","test",0,self.cfg)
        self.assertNotEqual(w.rivals["R1"].strength,view.rival_strength.value)
        self.assertFalse(hasattr(view,"world"))
        self.assertFalse(hasattr(view,"true_opponent"))
        self.assertFalse(hasattr(view,"future_results"))

    def test_information_narrows_errors_across_development_seeds(self):
        low=high=0
        for seed in range(32):
            low+=abs(information.estimate(70,10,seed,"x",self.cfg).value-70)
            high+=abs(information.estimate(70,90,seed,"x",self.cfg).value-70)
        self.assertLess(high,low)

    def test_envelope_blocks_disallowed_posture_and_protects_player(self):
        c,w=initialize("A",self.cfg)
        view=information.competitive_observation(c,w,"R1","test",0,self.cfg)
        rec=coach.recommend(view,Envelope(risk_ceiling="Conservative",protected=("P2",)),self.cfg)
        self.assertTrue(rec.plans)
        self.assertTrue(all(p.posture=="Conservative" and "P2" in p.lineup for p in rec.plans))

    def test_impossible_envelope_and_low_confidence_escalate(self):
        c,w=initialize("D",self.cfg)
        view=information.competitive_observation(c,w,"R1","test",0,self.cfg)
        self.assertTrue(coach.recommend(view,c.envelope,self.cfg).escalation)
        result=coach.recommend(view,Envelope(protected=("missing",)),self.cfg)
        self.assertTrue(result.escalation)
        self.assertFalse(result.plans)

    def test_lineup_eligibility(self):
        c,w=initialize("A",self.cfg)
        with self.assertRaises(ValueError):
            competition.profile(c,("P1",)*5,1,self.cfg)
        c.players["P1"].available=False
        self.assertEqual((),competition.lineups(c))

    def test_impossible_envelope_pauses_runner_without_illegal_plan(self):
        sim=Simulation("A",0)
        sim.company.envelope.protected=("missing",)
        while (cp:=sim.advance()).kind!="escalation":
            sim.submit(choose(cp,"conservative",self.cfg))
        before=state_hash(sim.company,sim.world)
        self.assertTrue(cp.requires_approval)
        self.assertFalse(cp.choices)
        self.assertEqual(("missing",),dict(cp.constraints)["protected"])
        self.assertEqual(cp,sim.advance())
        self.assertEqual(before,state_hash(sim.company,sim.world))
        with self.assertRaisesRegex(ValueError,"Coach escalation"):
            sim.finish()

    def test_replacement_preserves_six_players_and_old_due_obligations(self):
        c,w=initialize("A",self.cfg)
        c.financial["contract:P1:pay:28"].status="overdue"
        c.financial["contract:P1:pay:28"].missed_day=28
        economy.replace_player(c,w.talent["S"],29,self.cfg,self.emit)
        self.assertEqual(6,len(c.players))
        self.assertTrue(competition.lineups(c))
        self.assertEqual("overdue",c.financial["contract:P1:pay:28"].status)
        self.assertEqual("waived",c.financial["contract:P1:pay:56"].status)

    def test_rival_update_consumes_finite_opportunity(self):
        c,w=initialize("A",self.cfg)
        w.day=14
        world.update(w,economy.commercial_value(c),0,self.cfg,self.emit)
        cash=c.cash
        w.day=21
        world.update(w,economy.commercial_value(c),0,self.cfg,self.emit)
        offer=next(iter(w.opportunities.values()))
        self.assertIsNotNone(offer.claimed_by)
        with self.assertRaises(ValueError):
            economy.accept_sponsor(c,offer,w.day,self.cfg,self.emit)
        self.assertEqual(cash,c.cash)

    def test_commercial_value_and_unsigned_offers_are_not_cash(self):
        c,w=initialize("A",self.cfg)
        before=c.cash
        c.reputation=95
        c.audience=95
        w.day=14
        world.update(w,economy.commercial_value(c),0,self.cfg,self.emit)
        self.assertEqual(before,c.cash)
        offer=next(iter(w.opportunities.values()))
        economy.accept_sponsor(c,offer,14,self.cfg,self.emit)
        self.assertEqual(before,c.cash)
        self.assertTrue(any(x.source==offer.id for x in c.financial.values()))

    def test_sponsor_replacement_load_and_one_decision_per_offer_round(self):
        sim=Simulation("A",1000)
        result=sim.finish()
        agreements=[x for x in result["trace"] if x["kind"]=="sponsor_agreement"]
        self.assertEqual(len(agreements),len({x["day"] for x in agreements}))
        from prototypes.src.state import Observation, DecisionCheckpoint
        facts=Observation(tuple({"sponsor_payment":170,"sponsor_expiry":70,"day":56,
            "load":92,"capacity":100,"sponsor_load":18,"reputation":50,
            "offers":(("premium",195,18,45),)}.items()))
        cp=DecisionCheckpoint("offer",56,"sponsor","renew",("decline","premium"),facts)
        self.assertEqual("premium",choose(cp,"conservative",self.cfg).choice)

    def test_manual_and_autonomous_same_plans_same_competition(self):
        a=Simulation("E",1).finish()
        b=Simulation("F",1).finish()
        self.assertEqual(a["final_state"]["world"],b["final_state"]["world"])
        ca,cb=deepcopy(a["final_state"]["company"]),deepcopy(b["final_state"]["company"])
        ca.pop("envelope"); cb.pop("envelope")
        self.assertEqual(ca,cb)
        self.assertLess(a["metrics"]["approvals"],b["metrics"]["approvals"])

    def test_pause_resume_and_replay_are_identical(self):
        sim=Simulation("B",2)
        cp=sim.advance()
        before=state_hash(sim.company,sim.world)
        self.assertEqual(cp,sim.advance())
        self.assertEqual(before,state_hash(sim.company,sim.world))
        clone=deepcopy(sim)
        a,b=sim.finish(),clone.finish()
        self.assertEqual(a["state_hash"],b["state_hash"])
        self.assertEqual(a["trace_hash"],b["trace_hash"])
        self.assertTrue(replay(a)["matched"])

    def test_replay_detects_tampering_and_missing_actions(self):
        result=Simulation("A",3).finish()
        result["calibration"]["cash"]+=1
        with self.assertRaises(ValueError):
            replay(result)
        with self.assertRaises(ValueError):
            Simulation().finish([])

    def test_pause_after_due_finance_does_not_repeat_settlement(self):
        sim=Simulation("J",1000)
        while (cp:=sim.advance()) is not None:
            before=state_hash(sim.company,sim.world)
            trace=digest(sim.trace)
            self.assertEqual(cp,sim.advance())
            self.assertEqual(before,state_hash(sim.company,sim.world))
            self.assertEqual(trace,digest(sim.trace))
            if cp.day==84 and cp.kind=="recovery":
                clone=deepcopy(sim)
                self.assertEqual(clone.finish()["state_hash"],sim.finish()["state_hash"])
                break
            sim.submit(choose(cp,sim.policy,self.cfg))
        else:
            self.fail("Expected the fixture's due-finance recovery checkpoint")
        self.assertEqual(Simulation("J",1000).finish()["state_hash"],sim.artifact()["state_hash"])

    def test_overload_affects_process_not_base_capability(self):
        normal=Simulation("A",4)
        overloaded=Simulation("I",4)
        lineup=competition.lineups(normal.company)[0]
        self.assertEqual(competition.profile(normal.company,lineup,0,self.cfg),
                         competition.profile(overloaded.company,lineup,0,self.cfg))
        a,b=normal.finish(),overloaded.finish()
        work=[x for x in b["trace"] if x["kind"]=="preparation_work"]
        self.assertTrue(work)
        for x in work:
            self.assertAlmostEqual(x["after"]-x["before"],x["raw"]*x["efficiency"],places=5)

    def test_recovery_has_a_cost_and_retains_valid_roster(self):
        sim=Simulation("K",5)
        result=sim.finish()
        self.assertEqual("Stabilized",result["metrics"]["stage"])
        self.assertTrue(any(x["kind"]=="missed_obligation" for x in result["trace"]))
        sale=[x for x in result["trace"] if x["kind"]=="roster_replacement" and x["removed"]=="S"]
        self.assertTrue(sale)
        self.assertLess(sale[0]["after"],sale[0]["before"])
        self.assertEqual(6,len(sim.company.players))
        self.assertTrue(competition.lineups(sim.company))

    def test_every_representative_scenario_runs_and_replays(self):
        for scenario in "ABCDEFGHIJKLM":
            with self.subTest(scenario=scenario):
                result=Simulation(scenario,6).finish()
                self.assertTrue(replay(result)["matched"])
                self.assertLessEqual(result["metrics"]["day"],84)
                self.assertTrue(all("cause" in x for x in result["trace"]))


if __name__=="__main__":
    unittest.main()
