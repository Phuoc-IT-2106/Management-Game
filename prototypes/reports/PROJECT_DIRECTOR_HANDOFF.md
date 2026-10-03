# Project Director handoff — Prototype Build & Verification

Date: 2026-10-03

Simulation: `prototype-1.0`; calibration: `calibration-1`

Canonical input baseline: `a3ed01e3512f922aa4dde33684fe7a3cf5e20737`

Recommendation: **PASS PROTOTYPE GATE within the approved bounded validation scope.**

The engineering BUILD / VERIFY task is complete. A–M and AC-01–AC-13 have passing measured evidence, including inspection of the exact representative trace hashes. This is a recommendation to the Project Director, who retains authority to close the gate. Production engine selection and Technical Foundation have not started.

## 1. What was delivered

- A Python 3.14 standard-library CLI with `run`, `suite`, `compare`, `replay`, and a hash-bound `review` attachment command.
- Company/World state, a resumable daily runner, observation-only policies and coach, pure competition resolution, explicit financial commitments, commercial conversion, rival adaptation, bounded meta, overload and costly recovery.
- Input calibration, A–M scenario fixtures, 32 regression tests, 128 independent seeds per scenario, three policies across three contexts, mechanism ablations and controlled probes.
- [Verification tables](verification/VERIFICATION.md), [all per-seed metrics and probes](verification/verification.json), [13 representative artifacts](verification/representative/A.json), [semantic trace findings](TRACE_REVIEW.json), and [test execution record](TEST_RESULTS.txt).
- [Build contract](../BUILD_SPEC.md), [CLI instructions](../README.md), and [calibration provenance/change record](../data/CALIBRATION_LOG.md).

The 13 representative JSON files are named A–M under `verification/representative/`. Bulk reproducible development traces and caches are excluded from Git.

## 2. Authority and source reconciliation

The implementation began by fast-forwarding `main` to the canonical input baseline. DEC-001–DEC-020 and Project State v0.5 authorize BUILD / VERIFY. Stale “Proposed” labels, documentation-sync blockers, five-rival wording and an independently stored Strategy Exposure were superseded as specified by the user.

Three long specialist/prototype documents were truncated. Missing engineering details were supplemented from the user's pasted Prototype Engineering Lead request and their explicitly approved implementation plan. The supplement governs the CLI, A–M matrix, AC evidence conventions, determinism and delivery requirements; it does not silently create new product canon. The completed discovery worksheet remains the discovery source, rather than the blank worksheet.

Scope stayed at one company, six players with five roles, one coach, four rivals, two market candidates within the maximum of three, one active sponsor, one outstanding bridge instrument, 84 days, a double round-robin and eight player matches. Playoffs, Situational Pivot and series adaptation are omitted.

## 3. Ownership, dependencies and execution

| Owner / module | Implemented responsibility |
| --- | --- |
| Company / `state`, `economy` | People, contracts, financial items, current sponsor, information quality, capacity/load causes, preparation work, committed plans, authority and distress |
| World / `state`, `world` | Four compressed rivals, 20 world matches, result records, public meta and finite opportunities |
| `information` | Immutable observations containing known facts, noisy estimates and confidence; no world reference is passed to a decision policy |
| `coach`, `policies` | Finite legal plans and management choices based on observations; protected players, risk ceiling, manual/recommend/autonomous modes and uncertainty escalation |
| `competition` | Pure derived capability, exposure and factor calculation; emits CompetitiveOutcome without mutating cash, Reputation or Audience |
| `simulation` | Dispatch, exact day cursor, legal decision executor, causal trace and replay; owns execution metadata only |
| `verification` | Scenario/seed measurements, controlled counterfactuals, comparisons and acceptance reporting |

Financial items own remaining amounts and payment status. Contracts and loans reference their IDs. World stores results once; Company and rival histories reference them. Capacity, pressure, commercial value, capability and exposure are derived from owned causes. Preparation work records completed work and cannot be spent as a second currency.

Daily order is due receivables then obligations; availability/readiness and contract facts; preparation conversion with overload once; rival/world activity; meta; meaningful checkpoints; matches; company-owned delayed consequences; distress and review. The runner resumes a paused checkpoint without repeating the earlier daily phases. Same-day financing can settle an overdue bill, while `missed_day` preserves the earlier failure to pay on time.

Cash changes reconcile to explicit spending, receipts, obligation payments, asset sales, refunds or financing. The same executor applies manual and delegated plans. Independent event-keyed random streams prevent coach calls or extra logging from consuming match draws.

## 4. Verification execution and reproducibility

- Development/calibration seed set: 0–31. Independent verification set: **1000–1127**, inclusive.
- **1,664 scenario campaigns** (13 × 128), repeated a second time with identical final state hashes: **1,664/1,664 matches**.
- **13/13 representative full-trace replays** matched, including initial fixture/calibration validation.
- **1,152 comparison campaigns** (3 policies × 3 contexts × 128), plus mechanism ablations and recovery counterfactuals.
- **128/128 manual/coach pairs** produced identical World and Company gameplay facts after excluding the deliberately different authority-mode configuration.
- **32/32 regression tests passed**, including pause after due-finance settlement, invalid-action atomicity, eligibility, hidden information, idempotency, delayed effects, commercial cash separation and impossible-envelope escalation.
- No implementation error remained in the final execution. Suite status before attaching trace review was intentionally INCONCLUSIVE for AC-12; attaching the inspected hashes completed the gate evidence.

Replay captures simulation version, full calibration, initial Company/World state, seed, context, scenario overrides and decisions. Gameplay hashes exclude wall-clock timing and logs. Authority configuration is gameplay input, so E/F full hashes legitimately differ; their selected plans, competitive results and economic consequences are exactly equal. Cross-version replay is intentionally rejected.

## 5. A–M findings

Each scenario ran 128 seeds. PASS means its requested mechanism was demonstrated, not that its business strategy is recommended.

| Scenario | Result | Measured evidence and interpretation |
| --- | --- | --- |
| A — Conservative | PASS | 128/128 end-date viable; mean cash 531.56 CU, 5.25 wins, no overdue obligations. Day-112 obligations remain reported. |
| B — Star signing | PASS | Mean wins 6.58 versus A 5.25; cash falls to 84.79 CU and post-horizon obligations rise from 150 to 225 CU. Seed 1000 execution improves 67 → 85 while total commitments rise 600 → 900 CU. |
| C — Strong team loses | PASS | Controlled poor/better plans move win probability 0.392227 → 0.96. The representative day-70 loss has positive base advantage but negative preparation, matchup and adaptation contributions before variance. |
| D — Imperfect information | PASS | Beliefs and chosen-plan forecasts precede results. Seed 1000 initially forecasts 0.97 against resolved probability 0.675722 and loses; a later mistaken posture estimate reduces targeted preparation. |
| E — Coach | PASS | Mean approvals 5.34 versus manual 12; fewer approvals in 128/128 pairs. Uncertainty can still require approval. Plans remain inside the envelope. |
| F — Manual parity | PASS | 128/128 exact paired competitive/economic results; no manual or delegation strength bonus. |
| G — Exploitation | PASS | Repetition costs 0.057817 mean win probability with adaptation enabled. Adaptive choices change in 101/128 paired campaigns. Informed/uninformed isolated adaptation contributions are -5.4 and 0. |
| H — Meta | PASS | All 128 meta-off comparisons change preparation allocations. The representative changes to meta preparation on day 32 after the world transition. |
| I — Overload | PASS | Load 154/capacity 100 turns 2.04 raw daily work into 0.9384 once. All 128 adaptive overload-off comparisons change plans; base player capability is unchanged. |
| J — Financing | PASS | The timing-gap probe pays on time only with borrowing, at a real 22 CU cost. Integrated mean committed cash is **-213.19 CU**, with **459 CU** post-horizon obligations; end-date cash alone would conceal the problem. |
| K — Recovery | PASS | 128/128 recover versus 0/128 end-date viable without intervention. Seed 1000 goes Warning → Distress → Restructuring → Stabilized on days 1, 7, 8 and 11. |
| L — Restructuring | PASS | Same crisis fixture as K, inspected for sacrifice: execution 85 → 58, reduced future salary commitments, valid six-player roster and paid legacy obligation. |
| M — Snowball | PASS | Diminishing returns reduce raw positive Reputation growth by 17.009275 points on average before clamps. Payroll, finite market claims and future commitments remain active. Mean wins remain high at 7.73/8; long-horizon balance is unproven. |

## 6. Acceptance matrix

| Criterion | Result | Evidence |
| --- | --- | --- |
| AC-01 | PASS | All three policies exceed 80% end-date viability in suitable contexts; none dominates every context on survival, committed liquidity and wins. See comparison below. |
| AC-02 | PASS | Three pairwise ranking reversals across a newly integrated lineup, a known opponent and a meta shift. Execution, opponent and meta preparation each rank first in a distinct reachable input context. |
| AC-03 | PASS | A trace links match IDs → delayed Reputation/Audience → offers 35/56 → accepted agreements → scheduled receipts and later choices. Exact causal IDs are checked. |
| AC-04 | PASS | Changing Commercial Value and creating an unsigned offer leave Cash unchanged; accepting an offer schedules future receipts. Pure competition has no cash mutation. |
| AC-05 | PASS | High-information mean absolute estimate error 0.740330 versus low-information 6.662971; true profiles and keyed match draw do not change. |
| AC-06 | PASS | Exact parity 128/128; fewer approvals 128/128. A diagnostic oracle finds a better alternative in 108/128 low-information cases. This establishes possible override value, not human override skill. |
| AC-07 | PASS | Rival adaptation changes choices in 101/128 campaigns, and claims remove finite opportunities. The plan-change evidence is stronger than the market-count evidence. |
| AC-08 | PASS | Borrowing changes near-term affordability and adds repayment above principal. All future obligations are included; J exposes day-140 debt rather than treating it as profit. |
| AC-09 | PASS | Recovery improves viability 0% → 100% with a measured execution/salary tradeoff. No obligation is silently deleted. |
| AC-10 | PASS | Growth saturation reduces positive Reputation increments before any clamp; real payroll and commitments also constrain cash. This is bounded mechanism evidence, not proof that a strong start is globally balanced. |
| AC-11 | PASS | G/H/I choice changes occur under adaptation/meta/overload ablations with the same seeds. No narrative script grants wins or directly selects plans. |
| AC-12 | PASS | All A–M representative material paths inspected and cash transitions reconciled; findings are bound to exact state/trace hashes in TRACE_REVIEW.json. |
| AC-13 | PASS | 1,664 repeated state matches, 13 complete trace replays, and regression checks at paused decision/finance boundaries. |

## 7. Policy comparison

`committed_cash` = current Cash + signed remaining receivables − all outstanding obligations, including beyond day 84. It is an undiscounted commitment indicator, not net worth or a success score. Viability means no overdue obligation and no prototype terminal state **at the observation endpoint**; it does not claim all future commitments are affordable or that no payment was ever late.

| Context | Policy | End-date viability | Mean wins / 8 | Cash after commitments (CU) |
| --- | --- | ---: | ---: | ---: |
| Baseline | Conservative | 100% | 5.25 | 516.64 |
| Baseline | Aggressive | 100% | 6.58 | 32.81 |
| Baseline | Adaptive | 100% | 5.97 | 473.05 |
| Liquidity | Conservative | 100% | 5.25 | 346.64 |
| Liquidity | Aggressive | 100% | 6.58 | **-213.19** |
| Liquidity | Adaptive | 100% | 5.86 | 368.14 |
| Growth | Conservative | 100% | 5.25 | 583.44 |
| Growth | Aggressive | 100% | 6.71 | 67.84 |
| Growth | Adaptive | 100% | 6.71 | 67.84 |

The 95% Wilson interval for each observed 128/128 viability rate is approximately [97.09%, 100%]. No composite success score is used. Reputation, Audience, approval pressure, obligations and rival activity are also reported per seed. Organizational load/capacity and explicit decision history are available in full run artifacts. These finite policies do not prove there is no dominant strategy outside the tested choices.

## 8. Calibration and defect classification

Starting Cash 420 CU, Reputation 45, Audience 35, roster, coach and information values came from the supplied prototype fixture. Additional amounts, coefficients and thresholds are explicitly experimental in `calibration.json` and its [unit/rationale record](../data/CALIBRATION_LOG.md). No acceptance gate or calibration value was loosened after observing independent seeds.

Implementation corrections covered estimated rival adaptation instead of hidden truth, preserving missed payment dates, releasing expired sponsor load, one-time settlement/consequences, explicit impossible-envelope escalation, and the sponsor replacement defect discovered during trace review. That last defect counted existing sponsor load twice and allowed two agreements in a single offer round; it was fixed with a focused regression and regenerated independent evidence.

Evidence checks were strengthened to test actual choice changes under G/I ablations, causal commercial IDs, and recovery against no intervention. A Windows Python temporary-directory permission issue was fixed in the test harness using scoped workspace directories. These were implementation or evidence defects, not balance changes. No approved design change was made.

## 9. Simplifications, limits and regression risks

1. **Horizon debt risk:** J can end without current arrears by replacing a paid bridge with an expensive day-140 commitment. Reports correctly show negative committed cash. Extending the observation horizon or adding a sustainability measure should be a separate validation iteration; current end-date viability must not be marketed as solvency.
2. **Strong-start ceiling:** M still wins almost every match. Growth saturation is active, but the short horizon cannot establish long-term competitive balance. A passing mechanism gate does not endorse current strength, variance or commercial coefficients.
3. **Market scarcity:** Two candidates and finite sponsor offers are implemented; talent acquisition is an initial allocation choice, and rivals claim unused talent later. Long-term talent bidding and scarcity-driven investment timing have not been demonstrated. AC-07 rests on measured rival-induced plan changes.
4. **Simplified people/coach:** Role eligibility is discrete. Consistency and several recorded coach ratings are not active resolution dimensions; preparation and judgment drive the implemented coach. Availability is an explicit supported fact, without injury generation. This avoids claiming untested depth.
5. **Information domains:** Opponent information has measured behavioral effects. Talent estimates are exposed, but the small scripted policy set does not establish investment value for talent/commercial information. Commercial information is retained as a fixture fact without a separate pricing-information mechanism.
6. **Recovery breadth:** Selling commitments, cancelling unfinished investment, accepting adverse sponsor terms and bridging exist. The integrated recovery evidence uses star disposal; K/L intentionally share a fixture. The other paths need broader outcome validation before production design relies on them equally.
7. **Coach/override evidence:** The true-state oracle is restricted to verification. Its better alternatives prove the coach can be wrong; they do not show that a real player can consistently identify the better action from the same observation. Confidence is a quality indicator, not a calibrated posterior probability.
8. **World fidelity:** Rival development is bounded and independent of player results. Rival-versus-rival matches are coarse; rivals do not have full mirrored company economies. Importance and visibility are fixed for regular-season matches.
9. **Trace granularity:** Traces cover material transitions and referenced commitments, not a production event-sourcing contract for every field write. The saved initial/final states and replay support deeper inspection. The recorded competition checkpoint reason describes the coach recommendation; a fixed/manual override can select another plan, whose forecast is recorded separately.
10. **Runtime scope:** Only Python 3.14 on the current Windows environment was exercised. Pause/resume is in memory, replay requires matching fixtures/version, and there is no interactive UI or general save migration. Terminal is a prototype conclusion only.

Recommended simplifications for Director consideration: keep one financial-item authority; retain three preparation directions and three postures; preserve derived exposure and capacity; consider removing inactive coach/information fields until a test demonstrates that they improve decisions. These proposals are not silently applied as design revisions.

## 10. Director decision and handoff boundary

The implemented loop demonstrates the intended tradeoffs: preparation and information alter decisions; delegation reduces approval burden without a strength bonus; star investment buys performance with cash/load/commitment costs; competitive results flow through reputation and commercial agreements; recovery preserves the company at a competitive cost.

**Recommend PASS PROTOTYPE GATE for this bounded experiment.** The Director should acknowledge the horizon/market/strong-start limitations above when deciding whether to close the gate or request another focused validation iteration. No STOP / REDESIGN condition was established by this run. This task ends with engineering evidence and repository synchronization, before Technical Foundation.
