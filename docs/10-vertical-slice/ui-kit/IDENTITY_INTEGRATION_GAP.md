# Player identity integration gap

No production identity, content, canonical hash or save code is changed in Stage 2.
This inspection describes the existing local working implementation, including
pre-existing uncommitted gameplay files. It does not claim those files were
published by this UI kit commit.

Current Company (`src/ManagementGame.Domain/Model.cs`) has stable Id and Name,
alongside People, Coach, Employment, Sponsors, resources and operating state.
Application Situation currently exposes a Company string and a revision, not a
complete organization identity record. Content.CompanyName supplies the development
default through CampaignFactory. That fixture name is not player-company canon.

Minimum future campaign contract (one source of truth on Company):

| Field | Intended ownership / mapping |
|---|---|
| Id | Existing stable company ID; unchanged by rename/rebrand |
| DisplayName | Existing Name can remain storage field; map explicitly to display name |
| Abbreviation | New bounded Unicode player text; projects to ShortName |
| EmblemReference | Optional portable asset/catalog key; not an arbitrary filesystem path |
| PrimaryColor | Optional opaque sRGB RGB value, raw choice |
| SecondaryColor | Optional opaque sRGB RGB value, raw choice |

Use existing campaign/projection revision for freshness; no new global identity
service is needed. IsDevelopmentFixture is presentation provenance and must never
be inferred from the company name. Input validation limits, permitted emblem
sources and the player-facing editing command require a separate approved task.
Do not save safe adjusted colors: theme changes must be free to resolve raw choices
again. Branding has no effect on competition, finance, sponsor or roster rules.

Read-model change: add a complete actor-safe organization identity to the current
situation/root context, carrying ID and revision. Keep navigation/selection keyed
by ID. Map the five identity fields into OrganizationIdentityView at the presenter
boundary; retain a compatibility Company display string until the existing internal
client is deliberately migrated. Do not return Domain objects to components.

Persistence: CompanyDto currently stores Id/Name explicitly and maps both ways.
Add the identity fields to DTO and From/ToDomain mapping together. SaveEnvelope
currently requires schema 1, exact rules/RNG/canonical/content versions, checksums
and gameplay hash. StrictJson disallows unknown fields and respects required
constructor parameters. Therefore this is not a safe implicit optional-field edit.
A future approved change must version the schema, validate fields and either
provide an explicit schema-1 migration (preserve Name/Id; absent optional colors/
emblem; documented abbreviation fallback) or reject old saves with a clear message.
Never silently invent a new brand or overwrite old saves. Add roundtrip, migration,
malformed-field, rollback/backup and deterministic canonical-hash checks then.

Canonical serialization currently covers campaign state. Persisting extra fields
can change canonical/gameplay hashes even when simulation outcomes are identical.
Decide and version that compatibility behavior explicitly; do not claim hashes
remain byte-identical across schemas. Presentation adjustments remain outside it.

Content: ContentLoader hashes the exact UTF-8 fixture JSON and checks its `.sha256`
manifest. Adding identity defaults to content changes its schema/digest; existing
saves demand the exact content hash. Prefer player input in campaign creation with
explicit neutral fallback when absent. If defaults are added to the content schema,
update digest and compatibility policy together. Stage 2 uses code fixtures and
does not load or modify that content or its digest.

Next integration should be a bounded campaign identity/read-model/save contract
task before a player-facing editor. The upcoming operating-map prototype can use
the existing presentation fixtures until that separate decision is made.
