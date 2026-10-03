# RISK REGISTER

Status: ACTIVE — Discovery

Risk severity is provisional.

---

## R-01 — Unclear Product Center
Severity: Critical

Problem:
The concept can become an esports GM game, CEO simulator, tycoon, competitive strategy game, or organizational simulator.

Impact:
Strong individual systems may still produce an unfocused overall game.

Mitigation:
Lock player fantasy, authority, and strategic center before detailed system design.

---

## R-02 — Scope Explosion
Severity: Critical

Problem:
The long-term vision may expand too early into esports, traditional sports, media, advertising, events, merchandising, investment, and other industries.

Impact:
The core management loop may never be validated.

Mitigation:
Maintain a narrow first vertical slice and separate architectural extensibility from feature implementation.

---

## R-03 — Dynamic Numbers Without Dynamic Decisions
Severity: High

Problem:
Values may change while the optimal player response remains the same.

Impact:
The game appears dynamic but becomes repetitive.

Mitigation:
Variation must change opportunity cost, constraints, timing, information, or relationships between systems.

---

## R-04 — Excessive Simulation Fidelity
Severity: High

Problem:
Too many real-world details may be simulated because they exist rather than because they produce gameplay.

Impact:
Complexity, poor UX, balancing difficulty, and slow development.

Mitigation:
Only simulate detail that creates meaningful decisions or consequences.

---

## R-05 — Information Overload
Severity: High

Problem:
Management simulation may expose too much information simultaneously.

Impact:
Navigation burden, spreadsheet-like micromanagement, unclear priorities.

Mitigation:
Future UI/UX should prioritize:
"What does the player need to know now?"
over:
"What data exists?"

---

## R-06 — Content Production Dependency
Severity: Medium / High

Problem:
Replayability may become dependent on hundreds of manually authored events, dialogues, players, or scenarios.

Impact:
Production costs scale faster than system depth.

Mitigation:
Use persistent simulation state as the primary situation generator. Use authored content as enrichment.

---

## R-07 — Simulation Coupling
Severity: High

Problem:
Interacting systems may directly mutate each other's internal state.

Impact:
Fragile architecture and difficult testing.

Mitigation:
Later technical design should define explicit state ownership and system boundaries after gameplay requirements are known.

---

## R-08 — AI-Accelerated Inconsistency
Severity: High

Problem:
Multiple AI agents may rapidly create inconsistent terminology, duplicate abstractions, contradictory rules, architecture drift, and regressions.

Mitigation:
Use:
specification
→ ownership
→ implementation
→ verification

AI should execute clarified rules, not silently invent them.

---

# Review Rule

This register should be reviewed at each phase gate.

Risks may be:
- resolved,
- reduced,
- accepted,
- escalated,
- or converted into concrete tasks.
