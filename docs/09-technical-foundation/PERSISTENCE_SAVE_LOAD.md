# Persistence / Save-Load Foundation

Status: PROPOSED. Date: 2026-10-04. Owner: Technical Lead.

## Authority and compatibility

**DECISION:** Local saves, offline single-player and versioned, bounded compatibility are current user requirements. DEC-010/012 require one owner per fact and derived-state discipline.

**PROPOSAL:** Experimental builds may break saves with explicit release notes and a clear load-time explanation. Each released build publishes an explicit supported-save-version matrix. Initially support its current schema only; add a migration only when Director declares an older version supported. Do not promise all development saves, downgrade compatibility or cross-ruleset replay. This policy detail remains Q-02, not an accepted decision.

**FACT:** Prototype pause/resume and replay are evidence for execution semantics. They do not verify filesystem saves, crash recovery or production migrations.

## Snapshot contract

**PROPOSAL:** Use explicit versioned UTF-8 JSON DTOs initially, distinct from domain objects and engine serialization. Keep the transport replaceable; do not serialize Nodes, object graphs, reflection type names or executable objects. Compression is deferred until measured need.

| Section | Persisted content and purpose |
| --- | --- |
| Envelope | Format identifier, save schema version, writer build, simulation/rules version, RNG algorithm version, canonical hash version, campaign ID, snapshot revision, content manifest and checksum |
| Company | All owned people/readiness, roster, signed terms, financial items and settlement history, Reputation/Audience, capacity/load causes, information/knowledge, plans/completed work, authority envelopes, recovery stage/commitments |
| World | Calendar day/next phase/item/sequence, competition schedule/results/rulings, rivals, external people, finite opportunities/claims, meta/market state, deterministic ID counters |
| Execution records within owners | Pending checkpoints, delayed effects, consumer receipts, command receipts/payload digests and referenced causal records; seed and any explicitly stateful RNG counters |
| Optional presentation metadata | Save label, created-at display time, thumbnail and UI preferences, separated from gameplay hashes and discarded safely if unavailable |

The detailed authority table in [state ownership](STATE_OWNERSHIP_SIMULATION_BOUNDARY.md) controls where each field lives; these sections do not create new owners. Terms reference financial items rather than duplicating remaining balances. Historical snapshots are dated reporting evidence, never the current value.

The file checksum covers the canonical envelope and payload with only the checksum field omitted; optional metadata is covered for file-integrity purposes even though excluded from gameplay hashes. The save schema defines this encoding explicitly. A separate gameplay hash inside the envelope supports unchanged-identity round-trip/replay checks.

**PROPOSAL:** Snapshot only a complete committed boundary. A match plus required immediate consumers is indivisible; future effects may remain queued. Freeze one immutable revision, release simulation execution, then serialize that revision. UI receives the exact saved day/revision on success. Concurrent saves to one slot serialize; cancellation never publishes a partial file. A pending checkpoint is saved with its ID, actor knowledge revision and next execution cursor. Reload must not rerun earlier phases or discard unfulfilled commitments.

## Static definitions and campaign identity

**PROPOSAL:** Save runtime changes and Definition IDs; pin the complete gameplay content manifest by category/schema/pack version and content hash. Retain the required immutable packs with the build or campaign cache. Loading must locate the exact manifest, or use a declared migration; never substitute the newest pack silently. Signed contract terms are campaign facts and remain frozen even if their template later changes. Initial player attributes copied from content become runtime facts; they are not reset from definitions on load.

Save schema, content schema, rules version and build number have distinct meanings. A presentation-only patch can retain the same gameplay identity. A content or rounding change can change gameplay identity without changing JSON structure. Version labels alone are insufficient: verify hashes. See [content foundation](DATA_DRIVEN_CONTENT.md).

## Load and migration sequence

**PROPOSAL:**

1. Read bounded bytes into a separate load context. Reject invalid format, oversized structures, duplicate keys and unsupported versions before constructing gameplay objects. Limit lengths/counts/depth with versioned tooling limits approved alongside Q-05.
2. Verify file checksum, version support and exact content availability. A checksum detects accidental corruption; it is not an anti-tamper security claim.
3. If supported, run a pure DTO-to-DTO migration chain on a copy. Each step declares source/target schemas, any rules/content transition, required ID mappings and postconditions. No gameplay advancement, current clock or random invention is allowed.
4. Validate unique IDs, exactly one current owner, required references, valid dates/cursors, legal roster/contract relationships, financial reconciliation, effect/receipt consistency, numeric ranges and checkpoint resumability. Negative forecasts are legitimate; malformed amounts or impossible payment states are not. Do not silently clamp corrupted facts.
5. Construct Company/World, rebuild derived caches and actor observations, then compare the stored canonical gameplay hash for an unchanged rules/schema identity. Migrated saves receive a new hash after migration validation, retaining source provenance; equality to the old hash is not expected.
6. Atomically replace the active session only after all checks succeed. Failure leaves the current session and original file intact. No automatic advance occurs after loading.

No migration framework is implemented in Phase 4. Future tests cover every declared supported edge, missing edges, corrupt input and immutable originals. Unsupported newer/older versions return a precise version message, never a best-effort partial campaign.

## Safe writes and recovery

**PROPOSAL:** Resolve slots under the user's local application-data save directory, separate from installation files. Slot IDs are validated names, never arbitrary content-provided paths. Use a same-directory temporary file, exclusive slot lock, complete serialization, checksum, flush-to-storage request, close, then reread/validate the temporary file. Preserve the previous validated primary as a backup and publish by a platform-supported atomic replace (or atomic rename for a new slot). QG-04 must verify the exact Windows/filesystem implementation; API success does not prove power-loss durability on every device.

If replacement, flush, backup creation or disk space fails, report failure and preserve the previous valid primary. Do not implement a delete-primary-then-copy fallback. Keep two last-known-good backup generations per slot initially; prune only after the new primary validates. Never rotate a corrupt primary over a valid backup. Manual saves and autosave slots are distinct. Backup count/cadence remain Q-03.

On startup, ignore incomplete temporary files as primary saves. If the primary fails validation, offer a validated backup with its date/revision and explain potential progress loss. Do not silently load an older campaign, overwrite corrupt evidence or silently start a new game. Read-only, full-disk, sharing-lock, truncated-file and process-kill cases are QG-04 tests. Sync folders/removable/network filesystems are not presumed equivalent to tested local storage.

## Rebuild and growth controls

**PROPOSAL:** Rebuild standings, forecasts, Commercial Value, capability, capacity/load totals, overload, exposure and UI projections. Preserve knowledge source facts so rebuilding does not reveal truth or reroll estimates. Keep histories needed by gameplay and all outstanding causal references; optional diagnostic traces may rotate independently. Until an approved compaction rule exists, do not prune receipt/history data that affects replay or idempotency. Q-06 determines bounded retention after measuring growth.

Validation: T-04/T-05/T-09 and QG-04 in [testing](TESTING_DETERMINISM_DEBUGGING.md). Reversal cost is DTO/migration work, not engine scene migration.
