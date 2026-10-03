# Repository / Build / Tooling Proposal

Status: PROPOSED. Date: 2026-10-04. Owner: Technical Lead / build engineer.

**FACT:** The inspected repository is `Phuoc-IT-2106/Management-Game`, baseline `6ee4df3`. Production projects, scripts and CI described below do not exist as deliverables of this task. Existing prototype assets remain in place.

**DECISION:** No repository reorganization, engine installation, C# initialization or production BUILD in this assignment.

## Proposed boundaries

**PROPOSAL:** Adopt this layout only after Director acceptance and separate implementation authority. Names are proposed, not current paths.

```text
game/Client/                 Godot project, UI scenes, presentation models, composition root
src/ManagementGame.Domain/   Plain C# Company/World modules and immutable content contracts
src/ManagementGame.Application/ Commands, observations, orchestration and I/O ports
src/ManagementGame.Infrastructure/ JSON/content/save adapters
tools/Headless/              Engine-free scenario/replay host
tools/ContentValidator/      Category/manifest validator
tools/SaveValidator/         Read-only save checks
tests/Domain/                Pure rules and ownership tests
tests/Integration/           Transactions, saves, replays and parity scenarios
tests/UI/                   Presentation-model and Godot component fixtures
content/                    Versioned gameplay definitions, schemas and manifests
assets/                     Source presentation assets and notices
build/                      Windows build/export/qualification scripts
docs/                       Governance and specifications
prototypes/                 Existing Python reference and curated verification evidence
artifacts/                  Ignored build/test outputs
```

Domain references platform base libraries only. Application references Domain. Infrastructure references Application ports and Domain immutable contracts. Client/Headless composition roots construct adapters; UI screen code sees Application contracts only. No production assembly references `prototypes/`. Domain category contracts avoid a circular Domain ↔ Content project dependency. Assembly-reference checks enforce the allowed graph and reject engine packages in core tests. Module-internal mutation methods are not public UI APIs.

## Versioning and analysis

**PROPOSAL:** Pin accepted engine/template versions and checksums, the qualified SDK in `global.json`, package versions and lockfiles, formatter/analyzer versions and gameplay manifests. Review upgrades as reproducibility changes with QG-01/QG-03 reruns where relevant. Use EditorConfig, nullable-reference checking, .NET analyzers, explicit numeric conversions and warnings-as-errors for first-party core code. Time-box exceptions with justification; generated engine bindings are not rewritten to satisfy local style.

Review AI-assisted changes by the same standards: approved module boundary, real API reference, test evidence, no unexplained dependency and no hidden gameplay expansion. No tooling choice guarantees AI output correctness. Prefer text scenes and small components; isolate binary assets and avoid generated cache diffs.

## CI and Windows export

**PROPOSAL:** Every PR validates docs links, domain/content boundaries, formatting, content manifests, core tests and deterministic fixtures. A Windows job builds the qualified .NET projects, imports presentation assets through the pinned .NET engine, runs component smoke tests and exports a Windows x64 candidate. Run expanded deterministic scenarios, save fault injection and scale checks nightly or on relevant changes. No backend service, cloud save or online runtime dependency is needed.

A candidate pipeline checks out a clean commit; verifies tool versions; restores locked dependencies; builds/tests core; validates content; imports Godot assets; builds managed assemblies; runs engine smoke/component checks; exports with matching .NET templates and named preset; collects complete runtime files, notices and manifest; computes artifact hashes; then runs QG-01 on a clean SDK-free Windows machine. Capture failure exit codes and reports, not only successful process launch. Headless engine tests cannot establish visual/input correctness; rendered smoke testing is a separate lane.

## Commands as future interface contracts

**PROPOSAL:** These commands specify the future developer experience. Project files, presets and wrapper scripts must be created and verified only during authorized implementation. They are not runnable completion instructions for this Phase 4 task.

```powershell
dotnet restore ManagementGame.sln --locked-mode
dotnet format ManagementGame.sln --verify-no-changes --no-restore
dotnet build ManagementGame.sln -c Release --no-restore
dotnet test ManagementGame.sln -c Release --no-build --logger trx
dotnet run --project tools/ContentValidator -- --manifest content/manifest.json
dotnet run --project tools/Headless -- scenario --fixture parity --seed 1000
dotnet run --project tools/Headless -- replay --input artifacts/repro.json
dotnet run --project tools/SaveValidator -- --input artifacts/fixture.save.json
pwsh -File build/Test-GodotUI.ps1
pwsh -File build/Export-Windows.ps1
```

The future export wrapper resolves an explicitly pinned Godot .NET executable, verifies templates, imports assets and invokes the documented `--headless --path <project> --export-release <preset> <output>` flow; build ordering and managed artifact inclusion must be demonstrated in QG-01. The UI wrapper uses a project-owned test harness, not an invented built-in Godot unit-test switch. Tool exits: 0 success, nonzero for validation/test/build failure, with machine-readable details.

**FACT:** Current reference-only commands are documented in [prototype README](../../prototypes/README.md), including `python -m unittest discover -s prototypes/tests -v` and `python -m prototypes replay --input <artifact>`. Running them cannot qualify the future C# implementation.

## Artifact policy

**PROPOSAL:** Commit source, approved schemas, manifests, small fixtures, lockfiles and curated reviewed evidence. Ignore `.godot/`, `bin/`, `obj/`, temporary saves, generated traces and exports. Preserve existing curated Phase 3 reports. CI retains successful candidate manifests/results and failure reproduction bundles for a proposed 30 days; release artifacts and gate evidence have durable tagged retention. Final retention is Q-06. Distribute complete export output, content pack and required notices; signing credentials stay outside source control. Signing/store upload is a later release concern, not Phase 4 work.

**OPEN QUESTION:** Minimum OS/hardware and the production scale budget remain Q-04/Q-05. CI cannot label performance qualified before these are agreed. Engine licensing and SDK support must be rechecked before provisioning or release.
