# Source review and conflict register

Status: FACT unless explicitly labelled otherwise. Reviewed 2026-10-04.

## Baseline and provenance

`git ls-remote origin refs/heads/main` verified remote main at
`1aa3d6ee4d4553eef51cf9fc958cb3ad5d71d73a`. Local main is
`985b98df4ed1b6b9c18b9c5547c5cc3a79f4dffe`, a descendant containing the Phase 5
skeleton/specification. The dirty workspace additionally contains the working
implementation, content, tests and local evidence. Findings about those files
are workspace observations, not claims that they exist on remote main.

The requested `docs/02-product/` does not exist. Product material is under
`docs/03-product-foundation/`; its role, boundaries, core decisions, loop and vision
proposals were reviewed against the accepted log and current project state.

| Source read | Relevant finding / use |
| --- | --- |
| [Project state](../../00-project/PROJECT_STATE.md), [roadmap](../../00-project/ROADMAP.md), [DEC-001–022](../../01-governance/DECISION_LOG.md) | Company-first, CEO authority, esports first; one company/discipline/team; Phase 5 authorized under deferred gates |
| [Document authority](../../01-governance/DOCUMENT_AUTHORITY.md) | Approval and recency outrank filenames; internal DECISION labels do not approve proposals |
| [Product foundation](../../03-product-foundation/README.md), PLAYER_ROLE, PRODUCT_BOUNDARIES, CORE_DECISIONS, CORE_LOOP, proposed vision | Meaningful trade-offs and executive decisions; proposals use esports-heavy wording but already contain company ambition |
| [Core simulation](../../06-core-loop-simulation/README.md), COMPANY_STATE, RESOURCE_MODEL, TIME_MODEL, DELEGATION_MODEL, SIMULATION_CYCLE, CONSEQUENCE_MAP | One fact owner; five resource concepts; daily ticks with meaningful checkpoints; explainable delayed effects; bounded authority |
| [Specialist integration](../../07-specialist-specs/DIRECTOR_INTEGRATION.md), SIMULATION_ECONOMY_SPEC §§6–9, ESPORTS_COMPETITION_SPEC §9 | Commercial opportunity is not Cash; uncertainty improves knowledge; own facts versus opponent estimates; accepted for prototype validation |
| [Prototype review](../../08-minimal-prototype/DIRECTOR_REVIEW.md), MINIMAL_SIMULATION_PROTOTYPE_SPEC stored/derived state and information sections | Fictional discipline, counts, horizon and calibration are disposable validation choices |
| [Technical index](../../09-technical-foundation/README.md), [closure](../../09-technical-foundation/PHASE4_CLOSURE.md) | Foundation accepted through DEC-022; historical QG outcomes and DV-01–10 preserved |
| PRODUCTION_ARCHITECTURE, STATE_OWNERSHIP_SIMULATION_BOUNDARY, UI_TECHNICAL_FOUNDATION under technical foundation | Engine-independent core, immutable observations, typed commands, stable IDs, bounded rows, input/focus and performance obligations |
| DATA_DRIVEN_CONTENT, PROTOTYPE_MIGRATION_DISPOSITION under technical foundation | Campaign facts separate from definitions; no automatic prototype port; content identity and save compatibility matter |
| All eight top-level Phase 5 Markdown documents | Scope, architecture, implementation sequence, data, tests, verification and unresolved production decisions remain bounded |
| Local Phase 5 evidence logs and query-scale report | Existing logs report local passes; VERIFICATION.md still says tests not run. Evidence predates this task and is not re-certified here |
| Main.cs, Composition.cs, Contracts.cs, Observations.cs, RosterTable.cs, Session.cs; Domain model/factory; snapshot DTO/store; fixture and tests | Concrete UI, identity, command and persistence migration findings in disposition |

## Conflicts and resolutions

| ID | Sources / issue | Disposition |
| --- | --- | --- |
| UX-C01 | DEC-003/008 company-first versus current dashboard/team tab hierarchy | Presentation gap, not contradictory gameplay. PROPOSAL: DEC-023 formalizes the replacement presentation root |
| UX-C02 | UI foundation mentions active tabs; current slice scope says inspect dashboard | These are implementation/layout details, not accepted permanent navigation. Retain technical contracts; propose incremental presentation replacement |
| UX-C03 | Requested universal corporate UX versus DEC-018's three preparation priorities | Preserve Team Execution, Opponent-Specific Preparation and Meta Adaptation in the current discipline workspace. Generalization does not repeal DEC-018 or authorize changed preparation rules |
| UX-C04 | Brief says only blocking matters stop time; DEC-009 requires meaningful checkpoints and code stops after results/distress | Separate a command's normal checkpoint completion from an automatic interruption. Application owns checkpoint legality; UI salience never changes simulation stops |
| UX-C05 | Configurable identity brief versus `Content.CompanyName` fixture seed and missing brand fields | Current heading reads campaign data but campaign creation prescribes a fixture name. PROPOSAL: technical fixture identity and campaign identity contract before player-facing migration; no silent content/hash/save rewrite now |
| UX-C06 | DOCUMENT_AUTHORITY historical commentary says no Accepted decisions; technical files retain PROPOSED labels | Preserve historical text; DEC-001–022 and current PROJECT_STATE/closure control. This package does not reopen Phase 4 |
| UX-C07 | Specialist/prototype source truncations and old gate labels documented by existing Phase 5 questions | Do not reconstruct missing mechanics or treat old Phase-5-blocked wording as current |
| UX-C08 | New framework could become disproportionate Godot infrastructure | Native controls first, measured reuse; escalate framework cost under DEC-022 rather than building a generic graph/table engine |

No accepted decision requires supersession for this presentation proposal.
DEC-023 warrants a project-level review because it would bind future screens,
identity handling and component governance across domains. The brief authorizes
drafting; it does not unambiguously approve these detailed contracts as Accepted.

## Focused interaction research

**FACT:** Sources checked on 2026-10-04; they inform design, not project authority.

- [NN/g: Progressive disclosure](https://www.nngroup.com/articles/progressive-disclosure/)
  supports making uncommon detail available after the essentials. **PROPOSAL:**
  the four information layers preserve a short executive path and optional depth.
- [NN/g: Recognition and recall](https://www.nngroup.com/articles/recognition-and-recall/)
  supports visible context cues. **PROPOSAL:** company/scope labels and return to
  the originating decision reduce the need to remember navigation history.
- [Godot: theme editor](https://docs.godotengine.org/en/stable/tutorials/ui/gui_using_theme_editor.html)
  documents reusable themes. **PROPOSAL:** shared semantic theme resources and
  reviewed variations implement the visual system. Stable docs are reference
  material; validate exact APIs against the pinned engine during implementation.
- [W3C: accessibility principles](https://www.w3.org/WAI/fundamentals/accessibility-principles/)
  supports readable contrast and information that does not depend on color.
  **PROPOSAL:** brand colors remain separate from system status and focus.

**ASSUMPTION:** A restrained company/function map with a list/matrix equivalent
will improve orientation in this game. This is a design inference, not a proven
usability result. Validate it against a simple contextual navigation baseline
before investing in a relationship graph.
