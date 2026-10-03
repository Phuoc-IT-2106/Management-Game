# ADR-TF-001 — Engine / Framework and Runtime

Status: PROPOSED. Date: 2026-10-04. Owner: Technical Lead.
Approval owner: Project Director. No DEC entry is created by this ADR.

## Context and requirements

**DECISION:** DEC-021 authorizes technical evaluation, not production BUILD. Windows-first, UI + 2D, offline single-player and local saves are explicit user requirements.

**FACT:** The [engine evaluation](ENGINE_EVALUATION.md) separates official capabilities from project-fit judgments. No engine feasibility build has occurred.

The foundation needs dense management UI, structured content, simulation isolation, headless tests, reproducibility, versioned saves and maintainability for a small team.

## Proposed choice

**PROPOSAL:**

- Godot 4.7.2 .NET as presentation/desktop host; Compatibility renderer initially.
- C# across host, application and domain; no second gameplay implementation in GDScript.
- .NET 10 LTS as the target runtime, conditional on verified editor/export integration.
- A standalone core and headless runner with no Godot references.
- Built-in Controls and project view models; no mandatory third-party UI framework.
- Windows x64 export first. Other desktop releases remain future scope.

Patch versions and package hashes must be pinned when implementation is separately authorized. A later patch upgrade requires the same compatibility checks.

## Alternatives and trade-offs

**PROPOSAL:**

| Alternative | Benefit | Reason not preferred now |
| --- | --- | --- |
| Unity 6.3 LTS + UI Toolkit | Strong table/virtualization/binding foundation | Managed API profile differs from modern standalone .NET; commercial/editor integration costs |
| Unreal Engine | Powerful UI/editor/profiling and C++ capabilities | Expected maintenance/build burden exceeds present UI + 2D needs |
| Godot with GDScript domain | Close editor integration | Standalone typed C# core better matches the proposed external testing/tooling workflow |

Godot shifts some table/query/binding work into project code. Unity is the first reconsideration candidate if that work becomes disproportionate. These are engineering judgments, not measured productivity facts.

## Consequences and affected modules

**PROPOSAL:** UI and engine adapters depend on Godot. Domain, content semantics, commands, save DTOs and behavioral tests do not. Do not use engine object identities, resources or scene serialization as gameplay authority.

Affected modules: Client, Application, Domain, Infrastructure, Headless Runner, tests and build automation.

**FACT:** GodotSharp's net8.0 library target does not prove the exact .NET 10 editor/debugger/export combination. The current local SDK is not the proposed target. See source G7/N1 in the evaluation.

## Qualification and reversal

**PROPOSAL:** QG-01 must verify editor debugging, CLI export and a Windows build running without an installed SDK. QG-02 must verify dense table interaction and scaling. QG-03 verifies host/headless reproducibility. Definitions are in the [testing strategy](TESTING_DETERMINISM_DEBUGGING.md).

If a gate fails, record the failure and cost of a bounded fix. If fixing it requires a large custom framework or an unsupported runtime path, return the ADR to Director with Unity as the first alternative. Do not silently change technology.

Switching to Unity preserves concepts and much C# source, but requires UI/host rewrites and API-profile retargeting. Switching to Unreal also entails a language port. No binary portability is promised.

## Approval boundary

**OPEN QUESTION — Q-01:** Director acceptance of the candidate and qualification plan remains pending. Accepting this proposal is not evidence that qualification passed.

The [handoff](PROJECT_DIRECTOR_HANDOFF.md) requests explicit approval. Any technical experiment or production build needs subsequent implementation authority.
