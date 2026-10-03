# PROJECT DIRECTOR INTEGRATION — SIMULATION & ECONOMY × ESPORTS COMPETITION

Status: ACCEPTED FOR PROTOTYPE VALIDATION
Date: 2026-10-03

## Outcome
The two specialist specifications integrate without a critical contradiction and are approved as inputs to the Minimal Simulation Prototype.

## Accepted Cross-System Model
Company/World state → player decisions → economy and/or competitive commitments → competition resolution → CompetitiveOutcome → company/economy consequence resolution → Reputation / Audience / commercial opportunity / financial pressure → changed future options → Review / Adapt.

## State Ownership
### Company State
Owns player-company entities and durable facts: roster, staff, contracts, readiness, liquidity/obligations/debt, Reputation, Audience/Fandom, organization load/capability inputs, information capabilities, sponsor commitments, competition preparation/lineup/posture/delegation, and distress stage.

### World State
Owns external facts needed by the current scope: rivals, competition environment/schedule, meta, talent opportunities, sponsor opportunities, and bounded market context.

## Cross-System Rule
Systems emit outcomes/signals instead of directly mutating state owned by another domain.

Competition therefore emits `CompetitiveOutcome`. It does not directly add Cash, Reputation, Audience, or Company Value.

## Integrated Decisions
- Preparation Capacity is derived, not a sixth primary resource.
- Information Quality is domain-aware and produces estimates/confidence rather than hidden truth.
- Readiness has one authoritative source in People/roster state; Competition consumes it.
- Organizational Capacity affects process effectiveness, not generic team-strength penalties.
- Commercial Value is leverage/opportunity quality, not automatic revenue.
- Structural failure emerges through accumulated company state, obligations, liquidity, risk and recovery options, not from a single match result.
- Rival organizations remain World State and may use lower fidelity than the player company.
- Manual and delegated competitive control use the same underlying resolution rules; manual control has no hidden bonus.

## Gate
Integration status: PASS.

Next approved artifact: Minimal Simulation Prototype Specification.