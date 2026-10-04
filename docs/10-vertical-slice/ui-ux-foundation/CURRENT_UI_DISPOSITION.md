# Current UI and code disposition

Status: FACT findings; disposition column is PROPOSAL. Inspected local main
`985b98d` plus the pre-existing uncommitted implementation on 2026-10-04.
This task changes no runtime code, content, tests, build files or evidence.

## Current presentation identified

`Main.Build` constructs six TabContainer sections: Dashboard, Team & people,
Competition & coach, Finance, Sponsors, Review & results. A shared top toolbar
holds seed/new campaign, save/load/recovery and advance. `Refresh` builds long
text summaries from one `Situation` projection; confirmation dialogs wrap material
sign/release/sponsor commitments. Roster uses a native Tree with reused bounded
rows, metadata IDs, filter/search/sort and page controls.

The dashboard already says “YOUR NEXT DECISION” and exposes load/commitments, so
the implementation is not devoid of decision context. Its next situation is still
primarily the next match; functional tabs and text dumps do not express a company
portfolio or a coherent return-to-decision path. Navigation, view construction,
formatting, commands, smoke and benchmark code share one large class. Colors,
font size and spacing are local literals. There is no canonical component library.

## File / symbol matrix

| Code or artifact | Disposition | Retain / adapt contract and retirement condition |
| --- | --- | --- |
| [Domain Model](../../../src/ManagementGame.Domain/Model.cs): Company, World, Campaign | RETAIN | Company/World authority and immutable records. Identity extension later; no new gameplay root or blanket model rewrite |
| Domain Simulation.cs, Competition.cs, Finance.cs, Determinism.cs | RETAIN | Existing ordered execution, outcome consumption, keyed randomness, obligations and mechanics; presentation change is no reason to replace them |
| [CampaignFactory](../../../src/ManagementGame.Domain/CampaignFactory.cs): Create | ADAPT | Currently seeds Company.Name from Content.CompanyName with technical ID `company:player`. Future initialization accepts approved player identity; preserve stable IDs and deterministic creation |
| [Application contracts](../../../src/ManagementGame.Application/Contracts.cs): Decisions, Request, Response, IGameSession | RETAIN | Existing preparation, coach, signing, release, sponsor and advance command semantics; save/load port remains |
| Contracts.cs: Situation, PersonRow, CandidateRow, SponsorRow, BillRow, ResultRow | ADAPT | Preserve observation-safe fields; add scoped company identity, structured terms/affairs and source/revision metadata only where blueprint requires. Do not split into a large speculative API |
| [Observations](../../../src/ManagementGame.Application/Observations.cs): Build | ADAPT | Keep knowledge filtering and query independence. Replace UI-ready concatenated sponsors/review/pending strings with bounded structured projections when needed |
| Observations.cs: CoachPolicy.Recommend | RETAIN | Actor-safe recommendation and same manual/delegated executor. Five-role assumptions belong to this development discipline, not shared corporate UI |
| [RosterTable](../../../src/ManagementGame.Application/RosterTable.cs): Query | RETAIN / ADAPT | Retain stable tie-breaks, numeric sorts, ID selection and bounded page size (default 25, clamped to 100). Adapt column/field semantics only with real additional scope |
| [Session](../../../src/ManagementGame.Application/Session.cs): Submit, ApplyDecision, Save, Load; Canonical.cs | RETAIN | Revision rejection, receipts, one-writer transitions, safe replacement and deterministic hashing; no speculative core refactor |
| Infrastructure ContentLoader.cs, SnapshotStore.cs, SnapshotValidation.cs | RETAIN | Content validation, bounded save identity and safe load behavior; future identity migration requires explicit versioning and targeted verification |
| Infrastructure SnapshotDtos.cs: CompanyDto | ADAPT | Current Id/Name persist. Additional identity fields require reviewed schema/version/compatibility handling; not a UI-local cache |
| [Main.cs](../../../game/Client/Main.cs): Build/Refresh and tab shell | DEBUG / INTERNAL; REPLACE LATER | Keep working management path until company-first alternative is verified. Later replace presentation incrementally; no deletion based on appearance |
| Main.cs: heading.Text = view.Company | RETAIN / ADAPT | Already reads campaign-derived name. Broader identity read model needed; no hard-coded name should replace this binding |
| Main.cs: A–E filters/lineup, 5v5 label and fixed day-28/cadence text | ADAPT | Keep development fixture rules isolated; future discipline adapter and projected contract terms replace shell assumptions/literals |
| Main.cs: local Theme/colors/font/spacing helpers | REPLACE LATER | Replace with approved tokens/components during affected workspace migration, not a simultaneous screen rewrite |
| Main.cs: Send, Ask, Report | ADAPT | Preserve typed Request/revision/ID/confirmation behavior; extract shared presenter lifecycle and stale-draft handling when components are introduced |
| Main.cs: BindRoster, SelectId, UpdateRelease | ADAPT | Reuse bounded row/ID behavior inside canonical DataTable; test selection/callback reset and focus under refresh |
| Main.cs: RunSmoke, benchmark/_Process/capture; Composition.Hash/LastUiCommand | DEBUG / INTERNAL | Preserve useful verification hooks. Keep engineering counts/seed/receipt diagnostics outside ordinary player decision flow in future presentation |
| [Composition.cs](../../../game/Client/Composition.cs), Main.tscn, project.godot, Client.csproj | RETAIN | Composition boundary and pinned host; replace scene entry only after parallel/internal fallback is proven |
| [content/fixture.json](../../../content/fixture.json), fixture.sha256 | ADAPT (future) | Replace existing company-name fixture with technical noncanonical identity through a deliberate content-identity revision; exact evidence/saves must not silently change |
| tests/Domain, tests/Integration, tests/UI; tools/Headless | RETAIN | Preserve behavioral, determinism, save/continuation, observation and stable-selection evidence; extend only for affected contracts |
| build scripts, solution and lockfiles | RETAIN | No toolchain/engine change justified by presentation direction |
| docs/10-vertical-slice/evidence | DEBUG / INTERNAL | Existing local evidence retained with original limits; dashboard image is not a future golden or authority |
| prototypes/ and qualification/ | RETAIN reference | No changes to prototype canon boundaries, QG outcomes or Phase 4 evidence |
| Any current working file | REMOVE: none | Future deletion only after equivalent behavior, golden/input/performance evidence and reviewed migration; this task removes nothing |

## Identity gap

FACT: `content/fixture.json` currently sets `CompanyName` to “Northstar Esports.”
This occurrence is audit evidence only, not a proposed identity or canonical name.
The data is labelled a noncanonical development fixture, but the literal does not
meet the new technical-placeholder policy. It flows via CampaignFactory →
Company.Name → Situation.Company → heading. Company ID/Name are persisted by
CompanyDto; abbreviation/emblem/colors and a player identity entry path are absent.

PROPOSAL: Before a company-first player-facing demo, use technical fixture
identity, update the content digest and save/test identity intentionally, and
implement the smallest reviewed campaign/read-model identity contract. Do not
rewrite existing fixtures, hashes or historical screenshots as part of a spec
commit. No code rule may test the display name.

## Other presentation gaps for migration

Structured timeline matter IDs/classes and full contract timing are not exposed
by current read records. `Request` has revision/ID but no general checkpoint/actor
fields. Portfolio/division/team routes are conceptual; the current model has no
separate general portfolio entity schema. Project only supported scope first;
do not manufacture new authoritative records to satisfy a diagram.

Current selection remains by ID even if a filter hides the selected row. A future
table must visibly explain retained hidden selection or clear it deliberately;
actions cannot ambiguously target a row the player thinks is selected. This is
a blueprint/verification requirement, not a claim a new test has passed.

## Evidence limitations

Existing local logs report Domain 4, Integration 97 and UI presentation 3 passes;
the test source has additional checks beyond that log snapshot. Existing query
reports are Debug data queries, not proof of current Release rendering or physical
input. `VERIFICATION.md` still says production checks not run. Preserve these
facts without promoting stale logs into fresh gate evidence. This task performs
documentation checks, not a gameplay acceptance run.
