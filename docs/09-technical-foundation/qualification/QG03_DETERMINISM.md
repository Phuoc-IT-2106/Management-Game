# QG-03 — Core / host determinism

Date: 2026-10-04 (Asia/Saigon). **Result: PASS for the requested bounded fixture on this Windows x64 machine.**

## Objective and environment

Prove a plain C# Domain and Application path produces identical authoritative
results in a standalone runner and Godot host. Same Windows 10.0.19044 / i5-1135G7 /
Iris Xe environment, Godot/template 4.7.2 .NET, SDK 10.0.401 and runtime 10.0.12
as QG-01. No production gameplay or Python prototype rules were implemented.

## Implemented contract

**FACT:** `qualification/Domain` has no package/project reference or Godot type.
The assembly-reference check found only System.Collections, Immutable, Linq,
Memory, Runtime, Security.Cryptography and Text.Json. Domain uses memory streams,
not filesystem I/O. Application references Domain; both hosts call `Session.Submit`.

Company owns an artificial counter; World owns day and three artificial entity
counters. Immutable staged transitions commit both together. Seed, revision and
command receipts reside in an explicitly technical execution envelope, not a
third gameplay authority. Typed commands reject stale revisions/conflicting IDs;
exact retries are idempotent. Checked arithmetic rejects overflow before commit.

Fixture: initial Company counter 0; World day 1; entities A/B/C with counters
3/7/11. Seed 123456789. Journal has 32 commands `cmd-000` through `cmd-031`, cycling
A/B/C, expected revision i, delta i%7-3. These are disposable artificial rules.

`rng-v1` hashes NFC UTF-8 length-prefixed protocol, invariant seed, domain, event,
purpose, draw index and retry; lengths and first 64 hash bits are big-endian.
Range draws use rejection sampling. `fixture-canonical-v1` fixes JSON field order,
integer representation, ordinal entity/receipt order and includes identity,
Company/World and technical execution fields. It excludes logs, host timing and
machine diagnostics. Golden vector independently calculated with Python
hashlib/struct: SHA256 `da0aff2c38a20cb5d820e1ccb8ae896ad3b8d2ce89457808a0918127f132f168`,
first uint64 **15711650815429184693**. This is protocol checking, not prototype parity.

## Commands, tests and measured results

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File qualification/QG03.ps1
```

Exact expanded commands are in `evidence/qg03-*.log`; replay hashes are in
`evidence/qg03/`. Script compares initial/final identity, sequence length and every
transition, reporting the first divergent index on failure.
The final audit changed output collection to a unique ignored directory per run,
requires exactly eight freshly produced files, and copies evidence only after
comparison. That full workflow was executed again successfully; no stale result
file is needed to establish the reported matches.

| Executed lane | Variants | Result |
| --- | --- | --- |
| Standalone Debug | en-US/log off/normal insertion; tr-TR/log on/reversed insertion | 32/32 matching per run |
| Standalone Release | Same variants | 32/32 matching per run |
| Godot Debug, headless | Same variants, frame caps 15 / 144 | 32/32 matching per run |
| Exported Godot Release, rendered OpenGL | Same variants, caps 30 / 120 | 32/32 matching per run |
| Contract assertions, Debug | RNG vector, ordering/logging/locale, changed seed, idempotency, conflicting/stale rejection, overflow atomicity, 33 immutable resume boundaries, assembly references | 43 PASS |
| Contract assertions, Release | Same | 43 PASS |

All eight runs have identical initial and **all 32** transition hashes, not only
the final hash. Final: `a3561cf23f1ddf7abedc0052a789db2f27d017b8ea87c2fe4e209f92135000fa`.
Godot executes one typed command per actual `_Process` call; delta is never passed
to Domain/Application. Frame caps are requested settings, not measured sustained
FPS claims. Builds completed with zero compiler warnings/errors.

## Failures, limitations and recommendation

**FACT:** Restricted multi-project MSBuild initially exited 1 with zero compiler
diagnostics; an in-sandbox single-node build succeeded. Godot's multi-project
publish subsequently failed in that sandbox. The exact same QG03.ps1 workflow
completed outside it without source changes. Initial failure logs are retained;
this supports an execution-environment explanation, not a proven MSBuild root cause.
The export error scan caught Godot's misleading zero exit on managed publish
failure and prevented testing stale output.

Final build audit found `Optimize` unset for plain Microsoft.NET.Sdk projects
under Godot's custom `ExportRelease` configuration (the Godot host SDK already
sets it). `Directory.Build.props` now explicitly enables it for that configuration.
`qg03-domain-export-optimization.log` records `true`; the full parity matrix and
contract checks were rerun after this correction, retaining the same hashes.

**FACT:** No second qualified Windows machine, CPU architecture matrix or runtime
upgrade was tested; those checks remain INCONCLUSIVE. Resume tests use immutable
in-memory boundaries, not filesystem saves. This tiny fixture does not claim
PB-01–PB-10 production gameplay coverage, all seeds, long campaigns, arbitrary
concurrency, or Python/C# bitwise parity. The current Phase 4B request deliberately
authorizes only this small architectural fixture; broader specification checks
remain future work.

**PROPOSAL:** Retain the plain-core/shared-Application architecture. No host/hash
divergence or engine-independence blocker was found. Proceed to QG-04; preserve
these fixtures as replaceable evidence, not production gameplay implementation.

## Retained artifacts

Domain/Application/Runner sources and QG03.ps1; eight replay result JSON files;
`qg03-comparison.json`; `qg03-rng-vector.json`; contract/build/export/host logs;
failure logs. The regenerated local export under `qualification/artifacts/windows`
now includes Domain and Application assemblies; `qg03-artifact-manifest.json`
identifies it independently of the historical QG-01 artifact manifest.
