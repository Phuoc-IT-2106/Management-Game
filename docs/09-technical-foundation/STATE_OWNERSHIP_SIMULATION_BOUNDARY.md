# State Ownership & Simulation Boundary

Status: PROPOSED specification. Date: 2026-10-04. Owner: Technical Lead.

## Accepted invariants

**DECISION:** DEC-009–DEC-019 govern chronology, ownership, resources, derived values, delegation, rivals, failure, outcomes, information and bounded competitive resolution. This document proposes technical contracts that implement those invariants; it does not approve new balance.

The five primary resource concepts remain Cash, Organizational Capacity, Reputation, Audience/Fandom and Information Quality. Preparation Capacity is derived and allocated among Team Execution, Opponent-Specific Preparation and Meta Adaptation (DEC-018). The Recovery owner implements the conceptual Stable → Warning → Distress → Restructuring → Stabilized or Terminal progression (DEC-016); no final numerical insolvency or terminal threshold is chosen here.

## Authority table

**PROPOSAL:**

| Owner | Authoritative stored facts | Derived or referenced information |
| --- | --- | --- |
| Company / People | Owned people, availability, readiness, persistent development | Capability and lineup summaries |
| Company / Finance | Cash, receivables, obligations, settlement records, unpaid amounts, missed dates | Cash forecasts, pressure, committed liquidity |
| Company / Contracts | All signed employment, sponsorship and debt terms, dates, rights, financial-item IDs | Outstanding amount comes from Finance only |
| Company / Commercial | Reputation, Audience, sponsor-delivery progress and agreement ID references | Commercial Value; World opportunity references; signed terms live in Contracts |
| Company / Organization | Support investments and standalone operational load/capacity commitments | Capacity and Load totals use People staff facts, Contracts commitments and local causes once; overload is derived |
| Company / Competitive Management | Committed plan, completed preparation work, Authority Envelope | Preparation Capacity, competitive capability, strategy exposure |
| Company / Information | Domain-specific information capability, acquired knowledge, observations' source facts and knowledge revisions | Decision-specific estimates/confidence from versioned rules |
| Company / Recovery | Distress stage, active recovery commitments, transition reasons | Viability forecasts, not a second cash ledger |
| World | External rivals and people, markets, opportunities, calendar, meta, result records | Company/opponent histories reference result IDs |
| Application | Session identity and transient request/UI coordination | No duplicate authoritative clock, roster, budget or result store |

World calendar owns day, phase and sequence. A checkpoint record belongs to its initiating owner and references the calendar cursor and knowledge revision. The runner does not create another gameplay root.

World is partitioned into Calendar (cursor and runtime ID allocator), Competition (schedule, results and explicit qualification rulings), Rivals (compressed private state), Markets (external people, offers and claims), and Environment (meta and market conditions). Standings are derived from results unless a non-derivable ruling is stored. Company People owns roster membership and staff capability; Competitive Management stores selected person IDs, never another roster. Each module owns its pending effects, consumer receipts and causal history. Application command receipts and campaign rules/content/RNG identity are persisted under World / Calendar execution records. UI preferences and filesystem slot metadata are outside gameplay authority.

## IDs, transfers and derived state

**PROPOSAL:** Stable Entity IDs identify runtime entities; Definition IDs identify static content. IDs never use engine instance IDs or wall-clock timestamps.

A recruitment transaction transfers a person from World to Company, removes the active listing and creates the contract/financial references together. External records retain ID references or historical snapshots explicitly dated as history, not a second current person record.

Within World, an explicitly modeled external person's current facts live in Markets/People records and rivals reference that ID. A compressed rival capability profile can represent unmodeled personnel, but cannot become a second mutable copy of modeled person attributes. Transfers update references through the owning modules. This distinction permits lower fidelity without duplicate person authority.

The application coordinates recruitment; Markets emits a transfer authorization, People imports the person, Contracts accepts terms, Finance creates or settles items, and each owner stages only its own changes. All commit together or all roll back. Release uses the reverse ownership transfer. A generated ID retains its identity across transfers, is never reused and is allocated from persisted deterministic counters. A definition ID never substitutes for a campaign person ID. Reference validation rejects duplicate active owners and dangling required references.

Each obligation owns its outstanding balance/status. Contract terms define the agreement and may create items, but do not maintain an independently mutable payment schedule. Loan principal/fee terms reference the resulting obligations.

Results have one World record. Cash changes have explicit transaction/settlement causes. An unpaid obligation retains the unpaid amount and original missed date; later settlement does not erase late-payment history.

Derived caches are keyed by state/content revision, excluded from authoritative saves and rebuilt after load. Historical values may be stored with their timestamp and source revision for reporting.

Preparation work records completed work, not spendable preparation currency. Overload is applied once in each affected process, with no extra aggregate match-strength penalty for the same lost preparation.

## Observation, command and outcome contracts

**PROPOSAL:**

- Observation contains allowed facts, estimates, uncertainty, source age and knowledge revision. Reading it again does not reroll knowledge.
- DecisionCheckpoint contains its ID, reason, initiating owner, constraints and observed context. Legal choices are validated again when submitted.
- Decision links to checkpoint and revision. Validate authority, eligibility, availability, money and reservation state before mutation.
- CompetitiveOutcome contains result, expectation, importance, visibility and capability/preparation/matchup/adaptation/variance breakdown.
- Consequence contains event ID, cause ID, target owner, effect type, creation time and due boundary.

Manual, recommended and autonomous choices use the same executor. A duplicate command ID returns its prior receipt. A stale revision or illegal choice changes nothing.

Commands are player/staff intent requesting a transition; outcomes are immutable domain results for other owners to interpret; observations are read-only actor-visible information. A command carries actor ID, kind/schema version, payload, expected revision and checkpoint ID where required. Store its canonical payload digest with the receipt: reuse of an ID with different intent is rejected, while an exact retry returns the original result. Rejections consume no gameplay RNG or IDs.

Competition reads inputs and emits an outcome. Finance, Commercial and other owners interpret outcomes in deterministic order. Receipts are keyed by event ID and consumer; a due-effect removal and receipt commit together. Retry cannot double-pay or double-apply Reputation.

A future consequence queue is owned by the receiving module. Queues and receipts persist; no receipt pruning is allowed until references and replay/retention rules prove it safe.

## Daily progression and transaction boundaries

**PROPOSAL:** Persist the exact phase cursor; each phase may contain ordered atomic transactions. One day remains the smallest calendar tick; phases are execution order, not a finer gameplay clock.

1. Settle committed due receivables, then due obligations.
2. Process contracts and availability.
3. Convert preparation work.
4. Apply other process-specific overload effects; preparation's own reduction was already applied once.
5. Advance bounded rival/world activity.
6. Advance meta.
7. Stop at meaningful checkpoints.
8. Resolve scheduled competition.
9. Apply immediate outcomes and due consequences.
10. Evaluate warnings/distress.
11. Review and commit day completion.

Within a phase, order by due day, stable entity/event ID and explicit rule priority. Post-match revenue cannot retroactively pay a bill at the beginning of that day. Later settlement can clear arrears while retaining the missed date.

Resume starts at the stored boundary. It must not repeat finance, preparation or rival actions already committed. Empty days advance without a 'continue' decision.

At campaign creation the cursor is the first day's phase 1. Completing phase 11 moves it to the next day's phase 1 atomically. A cursor records the next transaction, including phase, stable item key and sequence; processing updates state and cursor in one commit. Saves are allowed only between complete transactions, never between an outcome and its required immediate consumers. Financial distress or an impossible authority envelope can interrupt at the first safe boundary before further work; resumption preserves the next phase/item. New immediate effects are dispatched by a versioned acyclic consumer order; an effect targeting an already completed phase is explicitly deferred to its next legal boundary, never applied retroactively.

## Staff and rival boundaries

**DECISION:** All three control modes operate under the same rules; better information improves decisions rather than underlying strength.

**PROPOSAL:** Staff receive Observation and finite legal plans, never the full World or hidden definition catalog. Evaluate plans within the Authority Envelope; escalate if no plan is legal or uncertainty exceeds a versioned rule threshold. Record the forecast before the outcome.

Each rival receives its own observation plus its own compressed private state. Rival cadence and maximum planning work are bounded by deterministic operation counts, not elapsed CPU time. Rival-versus-rival resolution can be lower fidelity under documented rules.

A shared finite opportunity is claimed atomically. Rival pressure must arise from budgets, information, commitments and public history, not player-performance compensation. Production rival count, horizon and coefficients remain Q-05 in [open questions](PHASE4_OPEN_QUESTIONS.md).

## Determinism boundary

**PROPOSAL:** Same authoritative initial snapshot, rules/content/RNG versions, seed and accepted decisions must produce the same results. All ordering, arithmetic and random contracts are specified in [testing/determinism](TESTING_DETERMINISM_DEBUGGING.md). UI frame rate, logging and thread scheduling must not influence gameplay.
