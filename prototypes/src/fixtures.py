"""Initial conditions only; fixtures never force simulation outcomes."""
import json
from pathlib import Path
from .state import (Company, World, Player, Contract, FinancialItem, Sponsor, Rival, Talent, Envelope)
from .economy import schedule_contract
from .world import schedule


def scenarios() -> dict:
    return json.loads((Path(__file__).parents[1]/"scenarios"/"scenarios.json").read_text(encoding="utf-8"))


def initialize(scenario: str, cfg: dict, context: str="baseline", overrides: dict | None=None):
    definition=scenarios()[scenario]
    initial=dict(definition["initial"])
    if context=="liquidity":
        initial["cash"]=250
    elif context=="growth":
        initial["sponsor_pay"]=196
    elif context!="baseline":
        raise ValueError("Unknown experiment context")
    initial.update(overrides or {})
    players={row[0]:Player(*row[:6]) for row in cfg["players"]}
    contracts={f"contract:{row[0]}":Contract(f"contract:{row[0]}",row[0],row[6],0) for row in cfg["players"]}
    f=cfg["finance"]
    contracts["contract:coach"]=Contract("contract:coach","coach",f["coach_pay"],0)
    contracts["contract:operations"]=Contract("contract:operations","operations",f["operations"],0)
    mode=initial.get("mode","recommend")
    sponsor_pay=initial.get("sponsor_pay",f["sponsor_pay"])
    c=Company(cash=initial.get("cash",cfg["cash"]),reputation=initial.get("reputation",cfg["reputation"]),
              audience=initial.get("audience",cfg["audience"]),players=players,coach=dict(cfg["coach"]),
              contracts=contracts,financial={},information=dict(cfg["information"]),
              capacity_inputs={"baseline":cfg["capacity"]},loads={"baseline":cfg["load"]},
              sponsor=Sponsor("sponsor:initial",sponsor_pay,84),envelope=Envelope(mode,mode,mode))
    c.loads["fixture_load"]=initial.get("extra_load",0)
    for p in c.players.values():
        p.execution+=initial.get("execution_bonus",0)
    c.information["opponent"]=initial.get("opponent_information",c.information["opponent"])
    c.coach["preparation"]=initial.get("coach_preparation",c.coach["preparation"])
    c.coach["judgment"]=initial.get("coach_judgment",c.coach["judgment"])
    if initial.get("crisis"):
        del c.players["P1"]
        c.players["S"]=Player("S","A",cfg["talent"]["star_execution"],cfg["talent"]["star_adaptability"],70,92,-30)
        del c.contracts["contract:P1"]
        c.contracts["contract:S"]=Contract("contract:S","S",f["star_salary"],0)
        c.loads["star"]=cfg["organization"]["star_load"]
    for contract in c.contracts.values():
        schedule_contract(c,contract,cfg)
    for day in (28,56,84):
        item_id=f"sponsor:initial:{day}"
        c.financial[item_id]=FinancialItem(item_id,day,sponsor_pay,"in","sponsor:initial","sponsor")
    if initial.get("crisis"):
        c.financial["legacy:due:7"]=FinancialItem("legacy:due:7",7,150,"out","legacy_commitment","legacy")
    rivals={row[0]:Rival(*row) for row in cfg["rivals"]}
    talent={"S":Talent("S",cfg["talent"]["star_execution"],cfg["talent"]["star_adaptability"],f["star_fee"],f["star_salary"]),
            "T":Talent("T",cfg["talent"]["prospect_execution"],cfg["talent"]["prospect_adaptability"],f["prospect_fee"],f["prospect_salary"])}
    if initial.get("crisis"):
        talent["S"].claimed_by="C"
    w=World(0,rivals,schedule(cfg),talent)
    return c,w
