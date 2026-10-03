# DOCUMENT AUTHORITY

Status: ACTIVE — repository governance guide  
Phase: Phase 1 — Product Foundation  
Authority: Explains source use; creates no gameplay decisions

## Purpose

Help contributors identify what a document establishes and what still needs Project Director approval.

## Source hierarchy

1. Accepted entries in `DECISION_LOG.md`.
2. Current, approved `PROJECT_STATE.md`.
3. Approved canonical product documents, including `PROJECT_CHARTER.md`, `GAME_VISION.md`, and later approved specifications.
4. Phase and Discovery source documents.
5. Specialist proposals.
6. Chat brainstorming.

Authority depends on **approval status and recency**, not filename alone. A proposal’s internal **DECISION** label does not make it an accepted project decision. A stale canonical file does not silently cancel a newer direction; report the discrepancy for Director review.

## Canonical and current-state sources

- `docs/01-governance/DECISION_LOG.md` is the formal decision record. Its supplied `DEC-001` entry is **Proposed**; no Accepted entry was supplied.
- `docs/00-project/PROJECT_CHARTER.md`, `GAME_VISION.md`, `ROADMAP.md`, and `GLOSSARY.md` are the supplied foundation sources. Their existing text is preserved. The charter and original project state still say Discovery.
- `docs/00-project/PROJECT_STATE.md` is the supplied original state, but its Phase 0 status is stale relative to the Phase 1 brief and handoff. Read it with `PROJECT_STATE_PHASE1_PROPOSED.md` until Director synchronization.
- A Phase 2 specialist package has since been received. `PROJECT_STATE_PHASE2_PROPOSED.md` is the latest candidate state, but neither it nor the Phase 2 handoff supersedes the formal record without Director approval.
- `docs/01-governance/RISK_REGISTER.md` is the active inherited risk list. Severity and status have not been reapproved here.

## Proposal sources

`docs/03-product-foundation/` contains specialist deliverables and proposed vision revisions. `docs/06-core-loop-simulation/` contains Phase 2 specialist proposals, including proposed updates to the core loop and system landscape. `PROJECT_STATE_PHASE1_PROPOSED.md`, `PROJECT_STATE_PHASE2_PROPOSED.md`, `DECISION_LOG_UPDATES_PROPOSED.md`, and `OPEN_QUESTIONS_PHASE1_REVIEW.md` are review aids. They do not supersede canonical files on their own.

## Historical and Discovery sources

`docs/02-discovery/` preserves Phase 0 synthesis, the **completed user worksheet**, product direction, preliminary pillars, pending decisions, source index, and transition plan. These explain provenance and reasoning. The completed answers are input for Director consolidation, not approved decisions. The earlier blank worksheet is archived at `docs/90-archive/discovery/DISCOVERY_WORKSHEET_BLANK.md`.

## Handoff sources

`docs/05-handoffs/` records specialist and Director handoffs. A handoff can report that a direction was accepted, but the formal decision log and approved state must be checked before treating it as a recorded decision.

## Statement labels

- **FACT:** verifiable source or project condition.
- **DECISION:** explicitly accepted choice with an authoritative record.
- **ASSUMPTION:** working premise awaiting validation.
- **PROPOSAL:** recommended choice awaiting approval.
- **OPEN QUESTION:** unresolved point requiring a later decision or investigation.

## Conflict handling

1. Identify the exact claims and both sources.
2. Record each source’s approval status and place in the hierarchy.
3. Preserve the original text; do not silently rewrite history or promote a proposal.
4. Escalate material conflicts and stale canonical status to Project Director.
5. Once approved, update the decision log and current state, then link the superseded source.

Current issues are listed in [Conflicts and synchronization recommendations](CONFLICTS_AND_SYNC_RECOMMENDATIONS.md).
