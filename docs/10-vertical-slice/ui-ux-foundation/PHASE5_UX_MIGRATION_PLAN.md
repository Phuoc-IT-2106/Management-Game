# Phase 5 UX migration plan

Status: PROPOSAL. This task completes only stage 1. Later stages are separate
bounded assignments; no UI demo or production redesign starts here.

| Stage | Scope / owner | Entry | Exit evidence / rollback |
| --- | --- | --- | --- |
| 0. Preserve functional slice | Technical Lead | Existing local gameplay/engineering path | Preserve files, command semantics, tests, save boundaries and historical evidence; classify current UI as internal |
| 1. Company UX foundation | UX lead + Director | DEC-022 and current brief | This reviewed document package, proposed DEC-023, traceable gaps and scope. No runtime change |
| 2. Design System + UI Kit | UI lead; Director approves aesthetic choices | Director disposition of foundation and bounded task brief | Approved semantic token values/typography, minimal components and incremental UI Lab with branding/state variants; no full framework |
| 3. Company Operating Map prototype | UX/UI lead | Approved context/identity blueprint and kit subset | Low-fidelity company orientation with supported scopes, keyboard list equivalent and no new simulation; compare hierarchy/matrix usability. Discard map variant if clarity/cost fails |
| 4. First contextual workspace | UI + Application leads | Approved blueprint and command/read-model gap plan | Recommend sponsor commitment review: company-level income/load/preparation trade-off; existing SponsorDecision. Correct stale/ID behavior, structured terms, consequences and return context verified |
| 5. Company-first UI demo | UI lead + QA + Director | Prior prototype/workspace accepted; identity prerequisites complete | One company, one development discipline/team; repeated existing management cycles, no-art path, branding variants, golden/input/performance evidence. Internal UI remains fallback |
| 6. Incremental production migration | Technical/UI leads | Per-workspace review approval | Migrate remaining finance, talent, preparation and affairs paths individually; retain command parity and save continuity within declared supported versions |

## Identity and read-model prerequisites

Before any player-facing company-first demo, replace the old named company fixture
with a technical identity carrying DEVELOPMENT FIXTURE — NONCANONICAL. Introduce
the minimal approved campaign identity fields and read projection, with explicit
DTO/version/content-digest effects and no new compatibility promise. Renaming a
company must not change entity IDs, routes or rules. Mocked UI Lab identity data
may precede campaign integration but must be visibly noncanonical and read-only.

Create structured projections only as demanded: sponsor terms and dates for the
first decision, scoped identity/context for navigation, and matter/cause IDs for
affairs. Retain existing observations and commands as adapters during transition.
Do not introduce new negotiation, finance, training or division mechanics to make
a screen appear richer. No need to redesign the Domain architecture.

## Coexistence and replacement gates

Keep the internal management UI reachable by an engineering entry/configuration
during migration. Both presentations call the same Application contracts; no
parallel gameplay state, screen-specific resolver or duplicate save system.
Commit presentation increments independently from gameplay changes whenever
possible. A failed visual or input review rolls back the new presentation while
the old functional path remains available.

Retire an old path only when its supported commands, save/load access, information
boundary, explanation and keyboard/ID correctness have equivalents; affected
behavior checks, visual review and measured workflow evidence pass. Remove only
the replaced presentation code; keep useful engineering harnesses outside player
flows. Never remove working code solely for visual age.

## Scope and governance gates

Phase 5 stays one company, one development esports discipline, one primary team,
limited rivals/competition, finance, sponsors, roster/coach/preparation, affairs
and results. Other corporate functions remain specification or embedded projections.
No additional sports, businesses, deep facilities, legal simulation, M&A, full
organization chart, broad negotiation, final balance or production portraits.

DEC-001–022 remain unchanged. DEC-023 acceptance is a Director decision; acceptance
of this package does not approve all future modules or final aesthetic choices.
Physical input/DPI, approved performance hardware and other DV obligations retain
their original owners/gates. A local UI demo does not authorize external public
distribution before DV-01. No Phase 4 reopening or QG-history alteration.

## Recommended next task

After review, authorize **a bounded Design System + UI Kit specification and
minimal Godot implementation** for NavigationContext, identity/status/value
primitives, DocumentView/ComparisonView and DecisionWorkspace needs. Agree final
token values/art direction through review, include minimal UI Lab fixtures, and
carry the identity/read-model gap list. Do not bundle the map, all ten modules or
full screen migration into that task.
