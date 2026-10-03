"""Explicit owned facts and immutable domain interfaces."""
from __future__ import annotations

from dataclasses import asdict, dataclass, field, is_dataclass
import hashlib
import json
from pathlib import Path
import random
from typing import Any

VERSION = "prototype-1.0"
ROLES = ("A", "B", "C", "D", "E")
POSTURES = ("Conservative", "Balanced", "Aggressive")


def canonical(value: Any) -> str:
    if is_dataclass(value):
        value = asdict(value)
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False, allow_nan=False)


def digest(value: Any) -> str:
    return hashlib.sha256(canonical(value).encode("utf-8")).hexdigest()


def draw(seed: int, domain: str, event: str) -> float:
    """Independent event streams: observation calls cannot consume match randomness."""
    key = hashlib.sha256(f"{VERSION}|{seed}|{domain}|{event}".encode()).digest()
    return random.Random(int.from_bytes(key, "big")).random()


def load_calibration(path: str | None = None) -> dict:
    source = Path(path) if path else Path(__file__).parents[1] / "data" / "calibration.json"
    cfg = json.loads(source.read_text(encoding="utf-8"))
    if not 1 <= cfg["horizon"] <= 84:
        raise ValueError("Prototype horizon must be 1–84 days")
    if any(isinstance(v,(float,int)) and v<0 for v in cfg["finance"].values()):
        raise ValueError("Financial calibration must be nonnegative")
    if not 0 < cfg["competition"]["probability_floor"] < cfg["competition"]["probability_ceiling"] < 1:
        raise ValueError("Competitive probability bounds must be ordered within (0, 1)")
    if any(len(x)!=3 or min(x)<0 or abs(sum(x)-1)>1e-8 for x in cfg["preparation_options"]):
        raise ValueError("Preparation allocations must be three nonnegative fractions summing to one")
    if len(cfg["calendar"]["round_days"])!=10 or sorted(set(cfg["calendar"]["round_days"]))!=cfg["calendar"]["round_days"]:
        raise ValueError("Five-organization double round-robin needs ten ordered distinct round dates")
    return cfg


@dataclass
class Player:
    id: str
    roles: str
    execution: float
    adaptability: float
    consistency: float
    readiness: float
    joined: int = -30
    available: bool = True


@dataclass
class Contract:
    id: str
    entity: str
    recurring: int
    start: int
    end: int = 112
    active: bool = True
    payment_ids: list[str] = field(default_factory=list)


@dataclass
class FinancialItem:
    id: str
    due: int
    amount: int
    direction: str
    source: str
    kind: str
    status: str = "pending"
    paid_day: int | None = None
    missed_day: int | None = None


@dataclass
class Debt:
    id: str
    principal: int
    financing_cost: int
    payment_id: str


@dataclass
class Envelope:
    preparation: str = "recommend"
    lineup: str = "recommend"
    posture: str = "recommend"
    risk_ceiling: str = "Aggressive"
    protected: tuple[str, ...] = ()


@dataclass(frozen=True)
class Plan:
    allocation: tuple[float, float, float]
    lineup: tuple[str, ...]
    posture: str
    target_posture: str
    meta: int


@dataclass(frozen=True)
class Profile:
    capability: float
    adaptability: float
    consistency: float
    coordination: float


@dataclass(frozen=True)
class Estimate:
    value: float
    low: float
    high: float
    confidence: float


@dataclass(frozen=True)
class CompetitiveView:
    match_id: str
    rival_id: str
    profiles: tuple[tuple[tuple[str, ...], Profile], ...]
    rival_strength: Estimate
    rival_posture: str
    rival_adaptation: float
    meta: int
    meta_change: float
    days: int
    efficiency: float
    coach: tuple[tuple[str, float], ...]
    exposure: tuple[tuple[str, float], ...]


@dataclass(frozen=True)
class Observation:
    facts: tuple[tuple[str, Any], ...]
    competitive: CompetitiveView | None = None

    def get(self, name: str, default: Any = None) -> Any:
        return dict(self.facts).get(name, default)


@dataclass(frozen=True)
class DecisionCheckpoint:
    id: str
    day: int
    kind: str
    reason: str
    choices: tuple[str, ...]
    observation: Observation
    plans: tuple[Plan, ...] = ()
    recommended: str | None = None
    context_id: str = ""
    requires_approval: bool = True
    constraints: tuple[tuple[str, Any], ...] = ()


@dataclass(frozen=True)
class Decision:
    checkpoint_id: str
    choice: str


@dataclass(frozen=True)
class CompetitiveOutcome:
    id: str
    day: int
    rival_id: str
    won: bool
    probability: float
    margin: float
    importance: float
    visibility: float
    factors: tuple[tuple[str, float], ...]
    plan: Plan


@dataclass
class Consequence:
    id: str
    due: int
    kind: str
    source: str
    amount: float
    applied: bool = False


@dataclass
class Sponsor:
    id: str
    payment: int
    expiry: int
    minimum_reputation: float = 0
    load: float = 0


@dataclass
class Company:
    cash: int
    reputation: float
    audience: float
    players: dict[str, Player]
    coach: dict[str, float]
    contracts: dict[str, Contract]
    financial: dict[str, FinancialItem]
    information: dict[str, float]
    capacity_inputs: dict[str, float]
    loads: dict[str, float]
    sponsor: Sponsor
    envelope: Envelope = field(default_factory=Envelope)
    debts: list[Debt] = field(default_factory=list)
    consequences: dict[str, Consequence] = field(default_factory=dict)
    plans: dict[str, Plan] = field(default_factory=dict)
    preparation_work: dict[str, list[tuple[int, float]]] = field(default_factory=dict)
    result_ids: list[str] = field(default_factory=list)
    stage: str = "Stable"
    restructured_day: int | None = None
    investments: dict[str, int] = field(default_factory=dict)
    decisions_done: list[str] = field(default_factory=list)


@dataclass
class Rival:
    id: str
    name: str
    strength: float
    adaptation: float
    information: float
    posture: str
    result_ids: list[str] = field(default_factory=list)
    acquisitions: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Match:
    id: str
    day: int
    home: str
    away: str


@dataclass
class Talent:
    id: str
    execution: float
    adaptability: float
    cost: int
    salary: int
    claimed_by: str | None = None


@dataclass
class Opportunity:
    id: str
    payment: int
    expiry: int
    duration: int
    load: float
    minimum_reputation: float
    claimed_by: str | None = None


@dataclass
class World:
    day: int
    rivals: dict[str, Rival]
    schedule: list[Match]
    talent: dict[str, Talent]
    opportunities: dict[str, Opportunity] = field(default_factory=dict)
    results: dict[str, CompetitiveOutcome | dict] = field(default_factory=dict)
    meta: int = 0
    previous_meta: int = 0
    meta_changed_day: int = -30


def state_hash(company: Company, world: World) -> str:
    return digest({"company": asdict(company), "world": asdict(world)})
