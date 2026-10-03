# Headless company simulation prototype

## Governance status

**Phase 3 is closed — PASS PROTOTYPE GATE.** This code remains a bounded validation/reference asset. It is not automatically the production architecture or final balance model.

Python **3.14**, standard library only. Run commands from the repository root. No package installation is required.

## Run and verify

```powershell
python -m prototypes run --scenario A --seed 1000
python -m prototypes suite --seeds 1000:1127
python -m prototypes compare --seeds 1000:1127
python -m prototypes replay --input prototypes/reports/generated/run.json
python -m unittest discover -s prototypes/tests -v
```

`run` accepts `--policy conservative|aggressive|adaptive|fixed|financed|recovery`, `--context baseline|liquidity|growth`, `--calibration <json>`, `--actions <json>` and `--out <json>`. An actions file is a complete list of `{ "checkpoint_id": "...", "choice": "..." }` entries for required approvals. Export the `decisions` list from a run as a starting point; optional extra fields are ignored. Invalid, duplicate, missing and unused actions are rejected.

`suite` and `compare` accept inclusive `--seeds start:end`, `--calibration` and `--out <directory>`. The suite includes the comparison, A–M campaigns, repeated runs, representative replays and isolated mechanism probes. It can take several minutes on a local PC. Each command prints a short summary and writes machine-readable evidence. Full campaign artifacts include initial/final states, observations, decisions, metrics, event causes and hashes.

Exit codes: **0** for successful execution with applicable checks passed; **1** for execution/input errors; **2** for a replay mismatch or failed/inconclusive suite gate. `run` succeeding does not imply a viable company. `compare` writes its measured `ac01_pass` flag; the full suite evaluates acceptance.

## Review the evidence

- [Director handoff](reports/PROJECT_DIRECTOR_HANDOFF.md)
- [Acceptance and comparison tables](reports/verification/VERIFICATION.md)
- [Measured evidence](reports/verification/verification.json)
- [Trace review](reports/TRACE_REVIEW.json)
- [Build specification and authority](BUILD_SPEC.md)
- [Calibration and change record](data/CALIBRATION_LOG.md)

The suite leaves AC-12 **INCONCLUSIVE** until its exact representative traces have been inspected. A reviewer records findings, state hashes and trace hashes for A–M, then attaches the review:

```powershell
python -m prototypes review --report prototypes/reports/verification --review prototypes/reports/TRACE_REVIEW.json
```

That command validates review coverage and matching hashes; it cannot itself perform semantic review. Regenerating evidence invalidates a review when any relevant hash changes. Director approval is a separate governance decision.

## Implementation map

| Module | Responsibility |
| --- | --- |
| `src/state.py` | Company/World facts, immutable interfaces, calibration, deterministic random streams and hashing |
| `src/simulation.py` | Ordered daily cursor, checkpoints, common decision executor, artifacts and replay |
| `src/information.py` | Observation boundary; estimates and confidence |
| `src/competition.py` | Eligible lineups, derived capability/exposure and pure match resolution |
| `src/coach.py` | Finite plan evaluation, authority constraints and escalation |
| `src/economy.py` | Payments, contracts, commitments, commercial effects, overload and recovery |
| `src/world.py` | Calendar, rivals, bounded meta and finite markets |
| `src/fixtures.py`, `src/policies.py` | Scenario inputs and observation-only management policies |
| `src/verification.py` | Evidence, counterfactuals, scenario/AC gates and policy comparison |

Company owns financial items and contracts refer to their IDs. World owns match results; company and rival histories refer to result IDs. Derived quantities are calculated from owned facts. The runner's cursor and reports do not own a third gameplay state.

Pause/resume is available through `Simulation.advance()` and `submit(Decision(...))`; repeated `advance()` calls at a pending checkpoint are inert. An impossible authority envelope returns an escalation checkpoint with constraints and no legal choices. The batch CLI reports it as an execution error requiring a revised input; it does not invent a legal lineup. This prototype has no interactive play or production save system.

## Scope and reproducibility

One company, six players, one coach, four rivals, at most three talent candidates, one active sponsor, one bridge instrument, 84 days and eight player matches. No playoffs or match series. Development used seeds 0–31; the reported independent seed set is 1000–1127. Calibration values and pass thresholds are experimental conventions.

Replay embeds version, calibration, initial state, seed and decisions. It requires the matching simulation/fixture version. Match randomness is keyed separately from information and world randomness. Monetary units are integer CU; continuous mutations use fixed rounding. Python's ties-to-even `round` is used for offer prices. Runtime timing is excluded from gameplay hashes.

Only curated reports, per-seed metrics and 13 representative traces are tracked. Disposable runs under `reports/generated/` and Python caches are ignored. See the handoff for measured limits, simplification proposals and the gate recommendation. Technical Foundation is now authorized under DEC-021, but remains outside this prototype implementation.
