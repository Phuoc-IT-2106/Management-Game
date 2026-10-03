# Prototype verification results

Simulation: prototype-1.0

Calibration SHA-256: `1c0de0bece63bc5d0fc977ed58708c87c83e4de71995c6e28e06ee234cbb60f3`

Seeds: 1000–1127 (128)

Recommendation from measured evidence and attached reviews: **PASS PROTOTYPE GATE**

## Scenario matrix

| Scenario | Result | Viability | Mean wins | Mean cash | Committed cash |
| --- | --- | ---: | ---: | ---: | ---: |
| A | PASS | 100.0% | 5.25 | 531.56 | 516.64 |
| B | PASS | 100.0% | 6.58 | 84.79 | 32.81 |
| C | PASS | 100.0% | 5.16 | 536.38 | 543.70 |
| D | PASS | 100.0% | 5.62 | 464.73 | 469.70 |
| E | PASS | 100.0% | 6.21 | 467.30 | 481.06 |
| F | PASS | 100.0% | 6.21 | 467.30 | 481.06 |
| G | PASS | 100.0% | 4.35 | 530.72 | 517.21 |
| H | PASS | 100.0% | 5.97 | 466.16 | 473.05 |
| I | PASS | 100.0% | 3.39 | 390.00 | 240.00 |
| J | PASS | 100.0% | 6.58 | 72.79 | -213.19 |
| K | PASS | 100.0% | 5.25 | 182.51 | 185.06 |
| L | PASS | 100.0% | 5.25 | 182.51 | 185.06 |
| M | PASS | 100.0% | 7.73 | 654.15 | 739.88 |

## Acceptance

| Criterion | Result | Evidence |
| --- | --- | --- |
| AC-01 | PASS | {"universal_dominators":[],"viable_policies":["conservative","aggressive","adaptive"]} |
| AC-02 | PASS | {"known_opponent":{"preferred":1,"probabilities":[0.640608,0.654633,0.444258]},"meta_shift":{"preferred":2,"probabilities":[0.770658,0.784683,0.97]},"new_lineup":{"preferred":0,"probabilities":[0.733428,0.674523,0.464148]}} |
| AC-03 | PASS | {"chain":"Outcome -> delayed reputation/audience -> offer -> agreement -> scheduled cash","linked_agreements":["offer:35:premium","offer:56:premium"],"representative":"A"} |
| AC-04 | PASS | {"cash_before_and_after_unsigned_offer":420,"no_competition_cash_mutation":"pure resolver regression test"} |
| AC-05 | PASS | {"high":0.7403300859375003,"low":6.662970781250001} |
| AC-06 | PASS | {"diagnostic_override_better":108,"fewer_approvals":128,"pairs":128,"parity_pairs":128} |
| AC-07 | PASS | {"changed_plan_cases":101,"mean_adaptation_probability_cost":0.05781742968749999,"representative_claims":4} |
| AC-08 | PASS | {"bridge_over_gap":[{"borrowed":false,"ending_cash":200,"outstanding":0,"paid_on_time":false},{"borrowed":true,"ending_cash":178,"outstanding":0,"paid_on_time":true}],"unneeded_loan_net_liquidity_change":-22} |
| AC-09 | PASS | {"recovery_viability":1.0,"sacrifice":[{"added":"RPL","after":58,"before":85,"cause":"contract:RPL:8","day":8,"kind":"roster_replacement","liabilities_after":698,"liabilities_before":1050,"removed":"S","source":"sign:RPL:8"}],"without_intervention_viability":0.0} |
| AC-10 | PASS | {"limit":"Evidence for this bounded horizon, not global balance proof","mean_raw_reputation_growth_reduction":17.0092753515625,"stress":{"approvals":12,"audience":86.528385,"autonomous_decisions":0,"cash":654.148438,"committed_cash":739.882812,"debt_due":0,"mean_win_probability":0.96822,"overdue":0,"post_horizon_obligations":150,"reputation":86.989655,"rival_claims":4.171875,"runs":128,"viability_rate":1.0,"viability_wilson_95":[0.970862,1.0],"wins":7.726562}} |
| AC-11 | PASS | {"adaptation_choice_changes":101,"meta_changes":128,"overload_choice_changes":128,"scripted_narrative_events":0,"single_overload_conversion":true} |
| AC-12 | PASS | Full measured evidence in verification.json; {"review":"TRACE_REVIEW.json","trace_hashes":{"A":"52ba9562665b49940d10910fa8a5b7b66e20bd3ab1c7ad7ac5508a7727b5d1e5","B":"3d228f9edb4689027d7cd7d5ebcb2bec271a96630c8f6fdec8e2281b3a47f143","C":"bc8f72939fc54e67c57d140a974420344a8907ee2d96bc64c5a4c6ca6b6808ac","D":"025b343515daa4fba8b6bd529d6402ee1cf030950ea19cf43ba61810db075837","E":"b5e885ad639354d68b750dcc56355022ebbcf46884189725a06fbc2faee58446","F":"c8f34fa962caa2547d150bf45f6926a5bcecbc5ca24e… |
| AC-13 | PASS | Full measured evidence in verification.json; {"full_trace_replays":{"A":{"matched":true,"state_hash":"52953c490c4c4b1180953dc250fcdfb3bf3393aae695f46d6d4c669e04909be5","trace_hash":"52ba9562665b49940d10910fa8a5b7b66e20bd3ab1c7ad7ac5508a7727b5d1e5"},"B":{"matched":true,"state_hash":"d16361c34f67df987d486cf98f48909c0a71c6eb8eb4ef4180621abc863eb656","trace_hash":"3d228f9edb4689027d7cd7d5ebcb2bec271a96630c8f6fdec8e2281b3a47f143"},"C":{"matched":true,"state_hash":"d607b8bd436733f8a5ded84dc61778a… |

## Policy comparison

| Context | Policy | Viability | Wins | Cash after commitments |
| --- | --- | ---: | ---: | ---: |
| baseline | adaptive | 100.0% | 5.97 | 473.05 |
| baseline | aggressive | 100.0% | 6.58 | 32.81 |
| baseline | conservative | 100.0% | 5.25 | 516.64 |
| growth | adaptive | 100.0% | 6.71 | 67.84 |
| growth | aggressive | 100.0% | 6.71 | 67.84 |
| growth | conservative | 100.0% | 5.25 | 583.44 |
| liquidity | adaptive | 100.0% | 5.86 | 368.14 |
| liquidity | aggressive | 100.0% | 6.58 | -213.19 |
| liquidity | conservative | 100.0% | 5.25 | 346.64 |

## Limitations

- Finite policies, fixtures, seeds and horizon; no production balance claim.
- Counterfactual true-state oracle is diagnostic only; never a policy input.
