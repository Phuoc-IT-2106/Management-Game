# CONFLICTS AND SYNCHRONIZATION RECOMMENDATIONS

Status: ACTIVE review register — no conflict resolved by this file  
Phase: Phase 1 — Product Foundation  
Authority: Documentation review aid

## C-01 — Phase status is stale in supplied canonical files

**Sources:** `PROJECT_STATE.md` says Discovery and has no locked decisions; `PROJECT_CHARTER.md` says Phase 0. The project brief and `PROJECT_DIRECTOR_HANDOFF_PHASE1.md` place the work in Phase 1. `PHASE0_TO_P1_PLAN.md` and the source-package READMEs still speak from Phase 0.

**Authority:** Original project state and charter are foundation sources; Phase 1 handoff is a specialist report; the project brief establishes the current working phase for this repository task.

**Recommendation:** Director reviews `PROJECT_STATE_PHASE1_PROPOSED.md`, records the Phase 0 exit decision, then updates the canonical state and charter’s phase line as appropriate. Keep the historical originals available through source package provenance.

## C-02 — Claimed acceptance lacks a formal decision record

**Sources:** `PROJECT_DIRECTOR_HANDOFF_PHASE1.md` says ten directions were accepted; Phase 1 documents label some paragraphs **DECISION**. `DECISION_LOG.md` contains only `DEC-001` with Status: Proposed.

**Authority:** Accepted decision-log entries would rank first, but none were supplied. Handoff and specialist files do not independently prove formal approval.

**Recommendation:** Director verifies approvals against original Director records and uses `DECISION_LOG_UPDATES_PROPOSED.md` to record only verified decisions. Keep unverified items Proposed.

## C-03 — Completed worksheet still needs Director consolidation

**Sources:** `DISCOVERY_WORKSHEET_COMPLETED.md` contains the user's answers to all eight Discovery questions. Its status and closing instructions say Director review is required before converting answers into locked decisions. The older `DISCOVERY_WORKSHEET.md` from the Discovery ZIP was blank and is archived.

**Authority:** The completed worksheet is a user-answer source for Director consolidation. It supports the product direction but does not itself establish formal acceptance in `DECISION_LOG.md`.

**Recommendation:** Director checks the completed answers against the Phase 1 handoff and verifies which conclusions were approved. Keep the blank version only in the archive; use the completed worksheet for current Discovery reference.

## C-04 — Canonical vision has not absorbed Phase 1 direction

**Sources:** `GAME_VISION.md` emphasizes broader company evolution and does not clearly state the executive role, competitive leverage, delegation, medium-depth philosophy, or recovery window. `GAME_VISION_REVISIONS_PROPOSED.md` recommends targeted additions.

**Authority:** The existing vision is a foundation source; the revision file is explicitly a proposal.

**Recommendation:** Director decides whether to approve the proposed edits, with current product promise and future expansion separated. Preserve the existing vision until then.

## C-05 — Old question wording is broader than current direction

**Sources:** `OPEN_QUESTIONS.md` still asks whether competition or company growth is the main fantasy and whether the player controls tactics at all. Phase 1 documents offer a narrower executive/company-first direction while leaving exact identity and manual-control minimum open.

**Authority:** The question list is a foundation source; Phase 1 files are proposals with claimed inherited decisions.

**Recommendation:** Use `OPEN_QUESTIONS_PHASE1_REVIEW.md` to mark directionally answered questions as pending formal ratification, retain unresolved detail questions, and update the canonical question list after Director review.

## C-06 — Conceptual language could be read as architecture

**Sources:** `SYSTEM_LANDSCAPE.md` and `EXPANSION_PRINCIPLES.md` use “core,” “module,” and state terms. `ROADMAP.md` defers technical architecture until after prototype validation.

**Authority:** These are conceptual foundation sources, not technical decisions.

**Recommendation:** Read them as product context only. Do not derive engine, code modules, schema, or save design from them at this phase.

## C-07 — Rival timing differs between roadmap and Phase 1 scope

**Sources:** `ROADMAP.md` places competitor strategy and the dynamic world in P7, after the P5 vertical slice. `PRODUCT_BOUNDARIES.md` proposes several evolving rival organizations in its first vertical-slice boundary, and `CORE_LOOP.md` uses rival behavior in its first-slice acceptance signal.

**Authority:** The roadmap is a supplied foundation source; the first-slice details are Phase 1 proposals.

**Recommendation:** Director clarifies the minimum rival pressure needed for the first validation scope and distinguishes it from later deep competitor strategy. Do not silently move all dynamic-world work into the first slice or remove rival pressure from the proposal.

## C-08 — Risk register phase label is stale

**Sources:** `RISK_REGISTER.md` says “ACTIVE — Discovery,” while the project brief places current work in Phase 1. The risk entries have no Director-approved closure or severity revision.

**Authority:** The register is the inherited active risk source; the phase label is historical.

**Recommendation:** Director reviews the register at the Phase 1 gate and updates its phase label or risk statuses only with an explicit review outcome. Preserve all eight risks meanwhile.

## C-09 — Phase 2 handoff claims approvals absent from canonical records

**Sources:** `PROJECT_DIRECTOR_HANDOFF_PHASE2.md` calls Product Foundation approved and Phase 2 active. `TIME_MODEL.md` and other Phase 2 files call Phase 1 constraints inherited decisions. The supplied `DECISION_LOG.md` still contains no Accepted entry, and `PROJECT_STATE.md` still says Discovery.

**Authority:** The handoff and Phase 2 files are specialist sources; they report approval but are not the formal approval record.

**Recommendation:** Director verifies the Phase 0 and Phase 1 phase-gate approvals, then synchronizes the canonical decision log and project state. Do not treat the new Phase 2 model choices as Accepted during that synchronization.

## C-10 — Phase 2 proposes answers to earlier open questions

**Sources:** `CORE_LOOP.md` leaves the exact time model and rival updates open. `TIME_MODEL.md` proposes a daily authoritative tick with checkpoints; `RIVAL_SIMULATION.md` proposes compressed evolving rivals and a minimum rival count. `CORE_LOOP_UPDATE_PROPOSED.md` and `SYSTEM_LANDSCAPE_UPDATE_PROPOSED.md` offer targeted revisions.

**Authority:** The Phase 1 and foundation documents remain unchanged; Phase 2 files explicitly say Proposal.

**Recommendation:** Director reviews these model choices and the targeted update files before editing `CORE_LOOP.md`, `SYSTEM_LANDSCAPE.md`, or the canonical question register. Keep unapproved details as proposals.

## Review outcome needed

Project Director should verify prior approvals and synchronize the decision log, project state, canonical vision, and question status while reviewing the Phase 2 package. No risk severity or approval status has been changed by this repository organization.
