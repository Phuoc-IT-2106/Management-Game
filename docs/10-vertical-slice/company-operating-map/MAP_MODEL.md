# Bounded map model

Implementation: `game/Client/UI/OperatingMap/MapModel.cs`, `MapFixtures.cs`.
Immutable MapSnapshot contains session key, revision, Stage 2 OrganizationIdentityView,
fixture day/checkpoint, eight MapScopes, three ScopeLinks and at most four
MapSituations. Validation rejects missing/duplicate IDs, disconnected/cyclic
ancestry, missing references and bounds violations. No universal entity API.

Ownership: DEV_ORG_001 → scope:competitive → scope:esports → scope:development →
scope:primary-team. Functions function:business, function:talent, function:affairs
belong to company context but are shown as a separate capability lane. Typed kinds
Company/Portfolio/Division/Discipline/Team/Function appear in labels. Development
Discipline explicitly remains noncanonical. No empty future business entries.

Relationships: Business & Finance supports/constrains Primary Team; Talent &
Performance serves Primary Team; Affairs & Time references situations across the
company. Inspector exposes reciprocal direct links. These explain relationships,
not simulated departments, financial arrows or ownership transfers.

| Situation | Affected scope | Navigation target / workspace | Information |
| --- | --- | --- | --- |
| matter:sponsor | function:business | entry:sponsor / Commercial commitment review | Estimated effect; exact outcome unknown |
| matter:preparation | scope:primary-team | entry:preparation / Preparation review | Unknown opponent preparation/variance |
| matter:talent | scope:primary-team; linked from Talent function | entry:talent / Talent contract review | Estimated external ability; date unknown |
| matter:obligation | function:business | entry:finance / Obligation inspection | Known fixture date; amount unknown |

Each carries ID, scope, name/category, presentation priority, status, nullable due
day, reason, consequence, target, workspace kind, information state and evidence.
Priority is categorical; no gameplay value or urgency formula. At most two high/
critical matter markers receive emphasized badges. No time interrupt is implemented.

Data-source review: current Application Situation already supplies actor-safe
finance, offers, people and opponent estimates, but has a company display string
without complete identity, scope IDs, structured affairs or complete sponsor
terms. A live adapter would need session/identity scope contracts. This standalone
orientation experiment consistently uses Stage 2 presentation identity and bounded
fixtures instead of implying that synthetic matters are live observations. Current
commands and actual campaign path remain in Main. No Domain/Application edit.

MapNavigation owns only selected IDs, list mode, a return tuple and feedback text.
Return tuple: company, scope, situation, mode, focus key, scroll. The snapshot is
read-only evidence, not company state. Replace validates before mutation, rejects
older same-session revision, clears entry evidence, resets incompatible session,
or restores nearest surviving ancestor. A surviving matter relocated to another
scope updates its selected scope. No actual load/new-campaign subscription is
installed because this scene does not create a campaign; future integration must
call Replace at that boundary.
