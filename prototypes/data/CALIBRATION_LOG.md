# Noncanonical prototype calibration log

## calibration-1 — initial baseline

Source fixture values: 420 CU, Reputation 45, Audience 35, capacity 100/load 74, six named players and the supplied coach ratings from the approved prototype specification. These remain test inputs.

Additional baseline choices: 28-day settlements; 150 CU total initial salary/operations per period; 140 CU sponsor receipts; star acquisition 230 CU with 100 CU recurring salary replacing a 25 CU salary; bridge principal 180 CU with 22 CU financing cost (54 in distress), repayable after 56 days. The contract horizon extends to day 112 so ending at day 84 does not hide the next commitment.

Preparation conversion, match variance, information error, adaptation, meta, reputation/audience response, opportunity terms and all economic thresholds are in `calibration.json`. Formula parameters were extracted from the initial implementation into that file without tuning them to held-out results. Fixed five-role eligibility and same-domain event identity are semantic rules, not balance parameters.

Development seeds: 0–31. Independent verification seeds: 1000–1127. No threshold or starting-value tuning based on the verification seeds is permitted under this baseline.

Implementation corrections before verification (not balance changes): rival adaptation capability is estimated instead of passed as exact hidden truth; overdue settlement retains its original missed date; recovery can settle obligations with newly raised cash without rewriting that history; expired sponsorship releases load; repeat calls cannot repeat settlement or consequences. These are recorded as implementation corrections, not changes to accepted design.

Controlled probes use additional declared input states to isolate relationships. Their numbers are displayed in the verification JSON and never replace the integrated campaign calibration. The oracle used to diagnose better alternatives is confined to verification and is not a management policy.

## Units and conventions

| Values | Unit / convention |
| --- | --- |
| Cash, fees, salaries, sponsor payments, obligations | Integer CU; salary and sponsor amounts are per 28-day settlement period |
| Dates, terms, duration, delay, horizon | Integer simulation days; contracts include obligations through day 112 |
| Player/coach ratings, information, Reputation, Audience | Points on a nominal 0–100 test scale |
| Capacity, Load and workload causes | Comparable organizational workload units, not currency |
| Preparation allocations, probability, efficiency, exposure | Fractions; allocations sum to one |
| Preparation scale/work and competitive factors | Abstract competitive contribution units |
| Meta | Discrete state -1, 0 or 1; change magnitude is absolute state difference |
| Viability threshold | At least 80% of observed seeds; confidence intervals are reported separately |

## Verification-stage corrections

No calibration values or acceptance thresholds were changed after inspecting independent seeds. Trace inspection did identify an implementation defect: replacing a sponsor counted existing sponsor load twice and could trigger two agreements from the same offer round. The correction subtracts replaced load and permits one decision per round; a regression test covers both. Independent evidence was regenerated afterward.

Evidence checks were strengthened to require causal IDs linking commercial agreements to prior results, decisions changed by adaptation/overload ablations, and improved recovery viability against a no-intervention campaign. Impossible coach envelopes now remain explicit escalation checkpoints. These are implementation/evidence corrections, not changes to approved design or balance.
