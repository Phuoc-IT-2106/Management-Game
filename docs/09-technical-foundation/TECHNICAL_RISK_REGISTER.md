# Technical Risk Register

Status: PROPOSED mitigations; all risks OPEN. Date: 2026-10-04.

**FACT:** Prototype evidence is bounded Python evidence. No production export, UI benchmark or filesystem qualification has been executed.

**PROPOSAL:** Likelihood is an engineering estimate. Referenced T/QG checks are in [testing](TESTING_DETERMINISM_DEBUGGING.md). Owners must attach evidence before closure; accepting a specification does not close its risks.

| ID | Risk / estimated likelihood / impact | Mitigation and concrete validation | Owner / deadline | Failure response |
| --- | --- | --- | --- | --- |
| R-01 | Godot/.NET 10 integration/export incompatibility; medium / high | Pin toolchain; QG-01 debugger, export and clean SDK/runtime-free Windows execution | Technical Lead / before substantial engine-dependent work | Bounded fix or revised runtime/candidate ADR; Unity fallback |
| R-02 | Dense tables require excessive custom infrastructure; medium / high | Pagination first; QG-02 latency, memory, control counts, focus and correct-ID actions at 1k/10k/100k rows | UI engineer / qualification | Adjust controls or compare Unity before building a framework |
| R-03 | RNG/order/arithmetic drift; medium / high | Versioned keyed RNG, numeric rules and hashes; T-01/03/05, QG-03 across machines/cultures/logging | Simulation engineer / core qualification | Block release; locate first divergent transition; version deliberate rule changes |
| R-04 | Cross-domain mutation or duplicate authority; medium / high | Ownership matrix and staged transaction; T-02/08/13 rollback, transfers and reference allowlist | Technical Lead / first integration | Reject boundary violation; document ownership correction |
| R-05 | Save interruption/migration loses progress; medium / critical | Validated temp, atomic publish and backups; T-04/09, QG-04 fault injection | Persistence engineer / before player-facing save reliance | Keep original/session; explicit backup recovery; block faulty build |
| R-06 | Content updates silently rewrite campaign facts; medium / high | Exact manifest pinning, frozen signed terms; T-09/10 missing pack/hash/schema and template-change tests | Content/persistence owners / integration | Reject mismatch or use declared tested migration |
| R-07 | Unknown PC/world scale makes performance unusable; high / high | Resolve Q-04/05; T-11 profiles tick/query/save/load and history growth at approved envelope | Director + Technical Lead / performance gate | Revise implementation within approved semantics; no skipped rules or invented scope |
| R-08 | Phases/effects repeat after load/retry; medium / high | Cursor/queues/receipts commit together; T-02/04/05 interruption at every legal boundary | Application engineer / save integration | Roll back incomplete transaction; block release until continuation matches |
| R-09 | Hidden truth leaks or observations reroll; medium / high | Observation-only interfaces; T-06/08 repeated queries, filtering/sorting and policy input inspection | UI + simulation owners / first decision workflow | Repair projections and retain diagnostic oracle isolation |
| R-10 | Histories/receipts/traces grow without bound; medium / high | Separate required history from disposable logs; T-11 measures growth slope; Q-06 retention/compaction review | Technical Lead / long campaign qualification | Propose semantics-preserving compaction; never drop pending effects/obligations |
| R-11 | Python calibration becomes accidental canon; medium / high | PB mapping, numeric-difference ledger and designer review; T-08/12 against approved contracts | QA + design / parity sign-off | Classify differences and obtain design approval where required |
| R-12 | Licensing/runtime lifecycle changes; medium / high | Recheck official terms/notices and support matrix before provisioning/release; QG-01 on relevant upgrades | Technical Lead + Director / provisioning and release | Rebudget, requalify or amend ADR; no automatic purchase/switch |
| R-13 | Asset/scene coupling makes migration expensive; medium / medium-high | Pure-core compilation, text content and explicit DTOs; T-13 plus representative adapter replacement review | Technical Lead / architecture integration | Remove core leakage before further UI investment; retain explicit UI rewrite cost |
| R-14 | AI code assumes APIs or bypasses invariants; medium / high | Review against pinned official APIs, analyzers and T-01/02/13; dependencies require review | Technical Lead / each PR | Reject unsupported code/dependencies; verify against contracts |
| R-15 | Stale/truncated sources treated as current authority; high / high | README discrepancy register, per-DEC audit, Director acknowledgment Q-07 | Director / spec review | Resolve substantive ambiguity explicitly; never invent missing gameplay requirements |

**OPEN QUESTION:** Q-04/05 block credible performance guarantees, not specification review. Owners are roles until Director assigns people; no dates or staffing commitments are invented.
