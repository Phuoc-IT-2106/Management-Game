# Prototype BUILD / VERIFY specification

Status: Implementation contract for disposable prototype validation; no production approval.

## Objective and sources

Test the management loop against scenarios A–M and AC-01–AC-13. Canonical baseline: GitHub commit `a3ed01e3512f922aa4dde33684fe7a3cf5e20737`, DEC-001–DEC-020, Project State v0.5, Minimal Prototype Spec, Director Review, Director Integration, and specialist specifications, in that order.

The three specialist/prototype specifications end abruptly around line 1000. The user explicitly authorized supplementing missing scenario and acceptance details with the Prototype Engineering Lead request and the approved implementation plan. This file records those supplements; it does not rewrite accepted design.

Higher-authority decisions resolve stale proposal labels and governance blockers in older files. Use four rivals, derive strategy exposure from history, and omit optional playoffs, situational pivots and series adaptation. Phase 3 is authorized; Technical Foundation is outside this work.

## Modules, ownership and dependencies

- `state`: Company/World dataclasses, immutable observations/plans/outcomes, stable IDs and canonical hashes.
- `simulation`: ordered daily runner, pause/resume cursor, decisions, causal trace, replay. Depends on all domain modules.
- `information`: observations from hidden truth; decision makers receive observations only.
- `competition`: pure bounded resolution; emits CompetitiveOutcome and never changes finance/reputation/audience.
- `coach`: finite plan evaluation within authority constraints; only consumes observations.
- `economy`: company financial schedules, commercial commitments, delayed consequences, derived pressure and recovery.
- `world`: lower-fidelity rivals, round-robin, scarce opportunities, bounded meta.
- `verification`: scenario comparisons, policy experiments, AC evidence and CLI reports.

Company owns its people, contracts, financial items, sponsor agreement, capacity causes/load, information capabilities, committed plans/preparation work, and distress. World owns rivals, calendar/results, meta and markets. Results are stored once in World; company histories reference match IDs. The runner cursor and trace are execution metadata, not a third gameplay authority.

Financial items own outstanding amounts; contracts/debt reference item IDs. Derived metrics are computed from causes. Preparation work is a record of performed work, not a spendable resource. Each domain applies only its own state changes; the runner dispatches consequences. IDs prevent duplicate settlement/consequence application.

## Interfaces and sequence

Observation exposes known company facts, estimates and confidence. DecisionCheckpoint names legal actions and reasons. Decision is validated before mutation. CompetitiveOutcome contains result, probability, importance, visibility and factor decomposition. Consequence records source ID, due day and owning process.

Daily order: advance date; settle existing due receivables then obligations; contract/availability facts; accumulate preparation work; apply process overload once; bounded world/rival activity; scheduled meta; resolve material checkpoints; competition; emit outcome; queue company consequences; apply due consequences; derive warnings; evaluate distress; review. A paused day resumes at its cursor, without rerunning settlement. New same-day rewards cannot retroactively erase a missed due payment.

Randomness is keyed by root seed, domain and stable event ID. No wall clock, Python hash(), global random generator or control-mode bonus affects gameplay. Money uses integer CU. Continuous state is rounded at mutation boundaries. Replay includes version, calibration, scenario, initial state, decisions, expected hash and seed.

## Constraints and calibration

Python 3.14 standard library only; CLI verification, not interactive play. One company, six players (five roles plus flex), one coach, four compressed rivals, up to three market candidates, one active sponsor, one bridge instrument, up to 84 days and eight player matches. No production engine, save architecture, tactical simulation or additional industries.

All ratings, prices, thresholds, coefficients and test gates are noncanonical. `data/calibration.json` is the explicit baseline. Development seeds 0–31; independent verification seeds 1000–1127. Calibration changes are recorded separately. Never loosen a gate to hide a failed result.

Information changes estimates and decisions, not asset ratings. Overload degrades preparation conversion once, not again as a team-strength multiplier. Coach is observation-only. Commercial value is opportunity leverage, not money. Unpaid obligations survive; outstanding debt and post-horizon commitments remain in reports. Recovery has a measured sacrifice; terminal conditions are prototype candidates only.

## Acceptance and tests

Scenarios: A sustainable; B star investment; C preparation/matchup upset; D rational decision under wrong information; E coherent delegation; F manual parity; G informed-rival adaptation; H meta-dependent choice; I overload; J financed investment; K recoverable distress; L costly restructuring; M snowball stress.

AC-01: at least two policies survive at least 80% of seeds in suitable contexts, with no single policy dominating all contexts in survival, committed liquidity and results. AC-02: at least three contextual choice reversals, including distinct useful contexts for each preparation priority. AC-03–04: trace competition into commercial choices, and no free cash from an offer/value. AC-05: lower estimate error with information, unchanged true capability. AC-06: fewer approvals with delegation, coherent choices, useful overrides, exact parity. AC-07: rivals change competitive choices or consume scarce opportunities. AC-08: commitments change future affordability, including beyond horizon. AC-09: recovery improves viability with sacrifice. AC-10: actual diminishing returns/scarcity/commitment effects in stress tests, not only clamps. AC-11: G/H/I arise from state. AC-12: material changes are causally traced and representative traces reviewed. AC-13: repeat runs, replay and pause/resume have identical gameplay results.

Unit/regression tests cover ownership, invalid action atomicity, information boundary, role eligibility, envelope constraints/escalation, payment/consequence idempotency, same-day ordering, delayed effects, derived-state semantics, and determinism. Compare three observation-only policies across baseline/liquidity/growth contexts. Report PASS/FAIL/INCONCLUSIVE with actual measurements, not expected outcomes.

## Risks and completion

Main risks: incomplete source tails, fitted calibration, hidden information, double counting, horizon debt exploits and too much state. Use provenance, held-out seed reports, immutable observations, causal IDs and explicit outstanding commitments to expose them.

Completion means running and reporting the full verification suite, including failed criteria. Correct implementation defects; record calibration/design failures honestly. Handoff includes implemented modules/ownership/sequence, A–M matrix, AC results, reproducibility, calibration changes, simplification proposals, limits, regression risks and open questions. Recommend ITERATE, PASS PROTOTYPE GATE or STOP / REDESIGN. Only Director closes the gate.
