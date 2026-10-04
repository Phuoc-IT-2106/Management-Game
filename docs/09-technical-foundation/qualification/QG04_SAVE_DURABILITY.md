# QG-04 — Save durability

Date: 2026-10-04 (Asia/Saigon). **Result: PASS for the tested Windows/NTFS interruption and failure scenarios.**

## Objective and exact environment

Verify that a minimal versioned snapshot can be safely published and explicitly
recovered without overwriting good history with corruption. Windows x64
10.0.19044, local C: NTFS, SDK 10.0.401, MSBuild 18.9.11, runtime 10.0.12.
Engine-independent Persistence and SaveTests projects; no new NuGet dependencies.
Godot 4.7.2 remains the overall spike host but is not required by this gate.

## Implementation and commands

**FACT:** The DTO stores schema 1, exact synthetic content identity, revision,
Company counter, World day and SHA256 checksum. Fixed-order UTF-8 JSON checksum
omits only its own field. Load bounds file size/depth, requires exactly the defined
fields, rejects duplicates, validates schema/content/checksum and revision/day
relationship before returning a separate immutable value. It cannot replace a
live session implicitly. No migrations, schema downgrades or latest-content
substitution exist. The content identity is a fixture tag, not a real content pack.

Write order: validate candidate → exclusive slot lock → validate previous primary
and advancing revision → same-directory temporary file → write → `Flush(true)` →
close → reread/validate → preserve validated older backup via flushed temp/rename →
`File.Replace(temp, primary, bak1)` or first-save `File.Move` → validate primary.
There is no delete-primary/copy fallback. Invalid backup generations never rotate
over valid older history. A corrupt primary stops a new save without changing
primary or backups. Temporary files are never load candidates. Recovery discovery
returns validated backup paths/revisions; choosing one is explicit.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File qualification/QG04.ps1
```

Exact expanded build and test commands are in `evidence/qg04-*.log`. Tests were
executed outside the restricted sandbox after its File.Replace permission denial.
All test data and ACL changes are limited to owned directories under
`qualification/artifacts/save-tests-*`. ACL denial is restored in `finally`.

## Tests actually run and results

**FACT:** Release build: zero warnings/errors. **166 assertions PASS**, exit 0.

| Executed scenario | Observation |
| --- | --- |
| Valid save/load; two backup generations | Exact revision-3 DTO round trip; backups 2 and 1 |
| Truncated and malformed JSON | Rejected; original corrupt bytes and valid backups preserved |
| Unsupported older/newer schema | Rejected; no downgrade/migration |
| Checksum, content mismatch, duplicate/missing fields, invalid day | Rejected before accepting a DTO |
| Corrupt primary + valid backup | LoadPrimary fails; explicit candidates 2/1; explicitly loading backup 2 succeeds |
| Write against corrupt primary | Rejected; cannot overwrite good backup |
| Actual primary file sharing lock | Write fails; prior primary/backup unchanged |
| Concurrent exclusive slot lock | Second writer rejected |
| Actual Windows ACL-denied temp write | Write fails; committed generations unchanged; original ACL restored |
| Simulated ERROR_DISK_FULL after 37 bytes | Partial temp remains non-authoritative; primary/backup unchanged; next save succeeds |
| Corrupt bak1 with good bak2 | Corruption does not rotate; bak2 bytes unchanged; validated primary becomes bak1 |
| Forced child-process termination | 9 checkpoints × 3 repeats = **27 actual kills** |

Each child waits at its named checkpoint. The parent observes the marker, kills
that owned process without allowing cleanup/finally recovery, waits for process
exit, then validates the exact primary, at least one backup, and another save.
Checkpoints: before-temp, partial-temp, after-temp-write, after-flush,
after-validation, before-backup-publish, after-backup, before-publish, after-publish.

Before publication the exact revision-3 primary survived; after publication the
exact revision-4 primary survived. All 27 cases retained at least one valid backup
and subsequently saved/loaded revision 5. No test treated an uncommitted temp as
a committed snapshot. The guarantee presupposes an existing valid committed
snapshot; there cannot be a previous snapshot before the first-ever commit.

## Failures and limits

**FACT:** Initial compile rejected a possibly-null process name under warnings as
errors; corrected to null-safe comparison. Initial sandbox execution denied
File.Replace during setup. Its log is retained as
`qg04-restricted-replace-failure.log`. The unmodified save algorithm passed outside
that sandbox. This is an environment restriction, not evidence to replace the
atomic publication strategy with a less safe fallback.

**FACT:** Disk full is injected after an actual partial file write; no disk was
physically filled. Interruption tests cover process termination around operations,
not power removal inside a kernel/filesystem write. Flush requests and observed
NTFS replacement do not prove storage-controller caches, hardware failure,
directory-metadata persistence or universal power-loss immunity. Network/sync/
removable filesystems, antivirus races, first-save power failure and supported
production migrations are **NOT QUALIFIED / INCONCLUSIVE**. There is no production
autosave/recovery UI, content pack or full Company/World save schema in this spike.

[Microsoft File.Replace documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-10.0)
supports the API's replacement/backup contract; it does not supply the untested
hardware guarantees above. This report claims only the executed fixture results.

## Retained evidence and recommendation

- `qualification/Persistence`, `qualification/SaveTests`, `qualification/QG04.ps1`.
- `evidence/qg04-build.log`, `qg04-tests.log`, `qg04-results.json`: all actual assertions.
- `evidence/qg04-fixture-manifest.json`: final fixture file sizes and SHA256 after
  recovery/subsequent-save checks, not a claim that every interruption image was
  archived before the recovery check.
- Local ignored test directories retain corrupt inputs, temporary files, primary,
  backups, kill checkpoint markers and result data.

**PROPOSAL:** The tested strategy is safe enough to continue technical review.
Keep Windows/local-NTFS qualification limits explicit; require further evidence
before broadening supported storage or claiming power-loss durability. No major
save-architecture blocker was found. This gate does not approve production saves
or close Phase 4.
