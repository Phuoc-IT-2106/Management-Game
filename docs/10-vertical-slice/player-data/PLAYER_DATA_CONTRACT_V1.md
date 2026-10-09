# Player Data Contract v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

Dependencies: [authority and source index](README.md), [ownership](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [content](../../09-technical-foundation/DATA_DRIVEN_CONTENT.md), [persistence](../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md), [catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md), [formulas](PLAYER_RATING_FORMULAS_V1.md), [NPC](NPC_GENERATION_V1.md).

## Boundary and lifecycle

**FACT:** current Person is an eight-field runtime record, not a source profile. **PROPOSAL:** the following are separate contracts owned at separate boundaries, not one mutable mega-entity. The example envelope packages them for review only.

~~~mermaid
flowchart TD
  E[External public data] --> S[Source profile and provenance]
  S --> O[Immutable player-game observations]
  O --> F[Versioned feature extraction]
  F --> R[Derived rating artifact]
  R --> D[Immutable player definition]
  G[Generation rules plus world seed] --> D
  D --> C[Company-owned player OR World-owned candidate]
  C --> V[Actor-safe observations]
  H[Historical source roster references] --> S
  X[Company employment and lineup references] --> C
~~~

Source profiles/observations/benchmarks belong to an offline content preparation boundary; they have no runtime mutation authority. A definition is immutable within its content version. Initialization copies approved capabilities to the campaign's authoritative player state. Transfer changes its owning collection and employment references atomically, retaining entity identity and progression. Public transfer history never performs this transaction.

**DECISION — preserved:** one active owner per campaign entity, Company or World, not both. Execution metadata is technical bookkeeping, not a third gameplay owner. Competition receives a capability snapshot and emits an outcome; it cannot edit employment, development or money.

## Representation rules

**PROPOSAL:** UTF-8, case-sensitive snake_case keys, NFC strings, finite JSON numbers, no duplicate/unknown keys. R = required non-null, N = required key allowing null, O = optional key, D = derived; U = unavailable/unknown value represented by null plus reason. O does not mean a provider must supply the field. Numeric zero is observed zero, never missing. Empty arrays mean no recorded entries, not proof that none ever existed. All examples include every top-level key.

IDs are lowercase ASCII [a-z0-9:_-], 1–128 characters. Display strings are 1–120 characters; source URIs up to 2048; external identifiers are opaque provider strings, 1–256, case preserved. Dates use YYYY-MM-DD, timestamps UTC RFC3339 with Z. Intervals use inclusive start, exclusive end; null end means open/unknown with an explicit status. Public calendar dates and integer campaign days never share a field. Unknown enum values fail; future additions require a contract version. No numeric sentinel, NaN, infinity or untyped extension bag.

## Example envelope dictionary

| Field | Type / presence | Meaning / owner |
|---|---|---|
| format | string R, player-data-example-v1 | Documentation envelope only; not the current content/save format. |
| spec_version | string R, v0.1 | This proposed contract. |
| example_only | boolean R, true | Prohibits production admission. |
| origin_path | enum R: public_data_example, generated_npc | Acquisition path; not a simulation rule selector. |
| definition | object R | Shared immutable internal PlayerDefinition below. |
| source_profile | object N | Offline public identity evidence; null for generated NPC. |
| observation_excerpt | array R of PlayerGameObservation | Synthetic excerpt or empty; never fabricated as actual provider data. |
| rating_evidence | object N | Offline measured-rating trace; null for generated NPC. |
| generation_evidence | object N | Generator trace; null for public pathway. |
| campaign_state_example | object R | Separate hypothetical runtime owner/fields, not an initialized save. |
| reference_manifest | object R | Example-only lists of organization_ids, competition_ids, champion_ids, metric_definition_ids, employment_ids (string arrays); production references resolve from their own catalogs. |

## Shared PlayerDefinition dictionary

| Field | Type / presence / units | Rules |
|---|---|---|
| definition_id | ID R | Stable biography identity; unique in catalog. Not a team, handle or account ID. |
| definition_version | positive integer R | Immutable revision for that definition ID. |
| identity | object R | Fields below; public facts require provenance, fictional fields require generated origin. |
| primary_role | enum R: top, jungle, mid, bot, support | bot means Bot/ADC slot, not a guarantee of champion class. LoL candidate only. |
| secondary_roles | distinct role array R | Excludes primary; eligibility does not grant unmeasured proficiency. |
| active_status | enum R: active, inactive, retired, unknown | Source-as-of status; does not cancel campaign employment. |
| capabilities | object R | Exactly the eight catalog IDs, each a RatingValue. Core/deferred is a model decision, not inferred from presence. |
| development_projection | object N | Deferred: null in v0.1. A later version must define type/semantics before use. |
| fictional_traits | array R | Empty in v0.1; no unapproved personality mechanic. |
| provenance | object R | Provenance dictionary below. |

identity fields: handle (string R), public_name (string N), nationality_codes (distinct ISO alpha-2 strings, array N), birth_date (date N), birth_year (integer 1900–as-of year N), birth_precision (enum R: day, year, unknown). day requires a valid date and matching year; year requires birth_date null; unknown requires both null. No inference of an exact birthday from a year. Generated examples may use fictional birth years; unknown nationality is null rather than an inferred ethnicity. Handle/name changes do not change IDs. No private contact/location data is included.

RatingValue fields, identical for real pathway and NPC:

| Field | Type / units | Rule |
|---|---|---|
| value | integer N, rating points 1–99 | null for unknown, deferred or not_applicable. No implicit average. |
| status | enum R: estimated, generated, unknown, deferred, not_applicable | estimated means a proxy-derived game value, not public fact. |
| method | enum N: performance_proxy_v01, latent_generation_v01 | Non-null iff value exists. |
| evidence_confidence | number N, 0–1 | Heuristic measurement adequacy, not a probability of truth; null for generated capability. |
| reason | string N | Required nonempty for null value; null for released value. |

Persistent capability is separate from recent form, readiness, public reputation, development projection and actor knowledge. NPC truth can be exact inside the simulation without having measured-data confidence 1.0. Both origins feed the same future resolution contract; no origin-based bonus, special accuracy or separate match rules.

## Source profile and career dictionary

| Field | Type / presence | Meaning |
|---|---|---|
| source_profile_id | ID R | Offline record identity, never campaign entity ID. |
| external_identities | array R | Entries: provider (string), namespace (string), value (opaque string), source_ref (URI), verified_at (timestamp), all R. Unique provider + namespace + value; account and human namespaces distinct. |
| public_current_team_ref | ID N | A dated source assertion; not the campaign employer. |
| career_history | array R | Entries: organization_ref R, from_date N, to_date N, role N, status R (reported or uncertain), source_ref R. Conflicting intervals retained as disputed evidence, not authoritative ownership. |
| participation_refs | ID array R | References competitions; statistics remain in observations. |
| handle_history | array R | Entries: handle R, valid_from N, valid_to N, source_ref R. |
| provenance | object R | Provider/as-of/rights evidence. |

Transfer history is derived from dated career assertions; do not invent a fee or contract. Contract values, current lineup, payroll, release costs and employment status belong to campaign Company/Employment. A later source catalog may reference documented public contract evidence separately, but it cannot populate private campaign terms automatically.

## PlayerGameObservation dictionary

**PROPOSAL:** one completed player appearance in one game, never a series aggregate. Raw provider payload retention is separate and subject to rights. All listed keys are required in the normalized row; N fields may be unavailable and must appear in unavailable_fields. Structural failure rejects/quarantines the row rather than guessing.

| Fields | Type / presence / units | Definition |
|---|---|---|
| observation_id, source_profile_id, match_id, game_id | ID R | match_id groups a series; game_id identifies one game. |
| played_at | timestamp R | Game end time for age calculation; uncertain timing must be resolved before time-weighted release. |
| patch, season, competition_ref, tier | string N | patch is actual played patch, not retrieval/client version; tier is a versioned catalog label. Unknown blocks an applicable benchmark. |
| team_ref, opponent_ref | ID R | Distinct; reference source organizations. |
| role | role enum R | Actual role played, not current profile role. |
| champion_ref | ID N | Versioned static champion key; no name-based fuzzy join. |
| side | enum N: blue, red | Not inferred from team order. |
| duration_seconds | integer R, 1–14400 seconds | Game clock duration, excluding pauses; remake/aborted games quarantined for v0.1. |
| kills, deaths, assists, cs_lane, cs_jungle | integer N, nonnegative counts | Lane CS and jungle CS must be disjoint and consistently defined. |
| gold_total | integer N, nonnegative game gold | Cumulative acquired gold, including starting/passive gold; not current wallet or earned-only gold. |
| damage_to_champions | integer N, nonnegative damage | Damage to enemy champions only. |
| vision_score, wards_placed, wards_killed | number N nonnegative score; integer N nonnegative counts | Distinct measures; wards cannot silently substitute for vision_score. |
| team_kills, team_gold_total, team_damage_to_champions | integer N, nonnegative same-unit totals | Same game's five participants, same definitions/time boundary. |
| gold_at_15, opponent_gold_at_15, xp_at_15, opponent_xp_at_15, cs_at_15, opponent_cs_at_15 | integer N, nonnegative gold/XP/CS | Exact game-clock 900-second paired-role snapshot. No interpolation or inferred final-game totals. |
| result | enum R: win, loss | Outcome; not a player skill label. |
| team_context | object R | pregame_strength_z (number N, -3..3), model_version (string N); both null if not externally calibrated on prior data. |
| unavailable_fields | map R | Key = nullable field path; value enum unavailable, not_collected, unknown, not_applicable. Only this explicitly typed map allows dynamic keys. Nulls must be explained. |
| provenance | object R | Observation-level source and metric semantics. |

Per-count technical cap 1,000,000; gold/XP/damage technical cap 1,000,000,000; vision_score cap 1,000,000. These detect malformed inputs, not plausible balance. Reject negative values, totals below individual contributions, kills+assists greater than team_kills, inconsistent pair timestamps, or mutually inconsistent duplicates. Check all five rows when present; partial excerpts cannot claim team-total verification. Games shorter than 900 seconds have null early snapshots with not_applicable. Features and outlier handling are specified in [formulas](PLAYER_RATING_FORMULAS_V1.md).

## Provenance and rating evidence

Provenance has required keys: provider (string), external_id (string N), source_ref (URI N), retrieved_at (timestamp N), source_updated_at (timestamp N), as_of (timestamp R), use_status (enum: example_only, pending_review, approved_for_named_use, prohibited), license_ref (URI N), metric_definition_id (ID N), dataset_version (string N), rating_model_version (string N), generator_version (string N). A production public artifact needs an external source reference, retrieval timestamp, reviewed license/use record and immutable dataset version; source_updated_at may genuinely be unknown. approved_for_named_use alone is insufficient without its referenced permitted-use record. Examples always use example_only. Generated provenance uses provider internal-generation, no external ID/source/retrieval, and a generator version. No fabricated provider assertions.

rating_evidence fields are R unless marked N: example_basis (string), observation_excerpt_complete (boolean), benchmark_id (string), comparability_scope (string), window_start/window_end (dates), n_games/n_series (nonnegative integers), effective_series (number ≥0), mean_age_weight (number 0–1), feature_coverage (number 0–1), context_quality (number 0–1), observed_attribute_z (map of numeric laning/combat/vision), consistency_observed_variance (number N ≥0), notes (string). These are explanatory aggregates for the synthetic example, not a full production audit log. Production additionally requires immutable input-row IDs/hash, per-feature valid sets, clipping decisions, per-series weights, benchmark coefficients, arithmetic profile and output hash. No release can claim reproducibility from aggregates alone.

generation_evidence uses the exact schema in [NPC generation](NPC_GENERATION_V1.md). Input hashes, source rights records and benchmark artifacts are future pack dependencies, not guessed hashes in these examples.

## Runtime dictionary and ownership

campaign_state_example fields:

| Field | Type / presence / units | Authoritative owner / behavior |
|---|---|---|
| campaign_id, entity_id, definition_id | ID R | Technical campaign identity and persistent player identity; definition ref resolves exact version. |
| definition_version | integer R ≥1 | Frozen reference. |
| owner_kind | enum R: company, world_candidate | Exactly one owning collection. |
| current_employer_ref, employment_ref | ID N | Company + Employment references when employed; null for free candidate. Roster membership is the collection, not a duplicated player list. |
| readiness | integer R, 1–100 points | People state; current simulator consumes readiness. Initial value is campaign setup, not external measurement. |
| availability | enum R: available | Proposed explicit representation of this example state only; other conditions require later rules. |
| form, development_state, condition | N, null in v0.1 | Reserved concepts, not implemented mechanics or free-form objects. |
| current_role | role enum R | Campaign assignment, distinct from historical roles. Role-switch behavior requires approval. |
| capability_overrides | object R, empty in v0.1 | Future progression cannot mutate immutable definitions; any later nonempty shape requires a versioned contract. |

This example shape is not the current PersonDto. In a future implementation the effective capability state must be defined explicitly (initial definition plus typed campaign changes, or a fully persisted snapshot); do not create two writable truths. Proposed growth, decline, injury and role familiarity belong to People state under its current Company/World owner. Scouting records belong to the observing company's information boundary and are estimates with observation dates, not backdoors to hidden capability.

## Identity, deduplication and history rules

1. DefinitionId identifies an enduring person within the catalog; EntityId identifies that person within one campaign; ExternalIdentity identifies a source's human/account record. Two campaigns may instantiate the same definition with different entity IDs. A single campaign must not instantiate the same biography twice as active people.
2. Existing player:generated is a reusable template ID, not proof of duplicate people. Migrating it requires the saved EntityId and a mapping table; do not merge every generated person.
3. External keys are namespaced. A player may have multiple accounts and renamed handles. Matching handles, names, nationality or birth year alone cannot merge profiles. Missing external ID uses an internal staging ID plus evidence; unresolved identity blocks publication, not inspection.
4. Deduplicate observation (provider, game, source player identity, revision); revised source rows supersede with retained lineage. Cross-provider same-game observations need an explicit verified mapping and deterministic preferred source; never count both as independent evidence or average incompatible definitions.
5. Rename updates versioned display/history fields. Transfer preserves DefinitionId/EntityId and changes only the campaign ownership/contract transaction or the separately dated source timeline. Resolved identity merges record aliases/tombstones; never rewrite existing campaign IDs automatically.
6. Source refresh creates a new definition revision and rating artifact for future campaigns. Live campaigns retain pinned data/rules and runtime facts until an explicit validated migration, with before/after evidence and rollback policy, is accepted.

## Example interpretation

[real_player.example.json](examples/real_player.example.json) uses the invented handle Example Meridian and internal synthetic evidence. Its one observation is an excerpt; forty-series summary values are hand-constructed worked inputs, not claimed to follow from that row alone. [npc_player.example.json](examples/npc_player.example.json) uses invented Lantern Finch, no public identity/statistics, and a hand-worked latent trace. Neither represents an actual human, a seeded golden output, a licensed player pack or a loadable save. Their identical definition/runtime keys demonstrate shared contracts without conflating provenance or ownership.
