# Engine / Framework Evaluation

Status: PROPOSAL — recommendation awaiting Project Director approval.
Evidence reverified: 2026-10-04.
Repository baseline: `6ee4df3`. Owner: Technical Lead.

## Basis and method

**DECISION:** DEC-021 opens Technical Foundation PLAN / SPEC; production BUILD remains unauthorized. See the [authority and provenance record](README.md#authority-and-provenance).

**DECISION — user requirements:** Windows first, management UI + 2D, single-player offline, local saves, and explicitly bounded save compatibility. These are session requirements, not new DEC entries.

**ASSUMPTION:** A small AI-assisted team has no existing engine-specialist constraint. Most early effort will be simulation, structured content and management interfaces.

**FACT:** The Python prototype supplies behavioral evidence, not production architecture. No engine benchmark or export experiment was performed in this assignment.

Evidence is presented before judgments. No numerical score is used: measured project performance and team productivity are not yet available. No additional candidate is necessary for this bounded evaluation.

## Official capability and licensing evidence

Source IDs below refer to the linked official pages in the source register.

| Candidate | FACT: capabilities verified | FACT: commercial and lifecycle constraints |
| --- | --- | --- |
| Godot 4.7.2 .NET | Current Windows download includes .NET; C# desktop export covers Windows, Linux and macOS [G1/G3]. Control/container UI and multi-column Tree controls are available [G4/G5]. CLI supports headless execution and exports [G6]. | MIT permits commercial use subject to notices [G2]. GodotSharp 4.7.2 targets net8.0 [G7]; that does not establish this project's .NET 10 export compatibility. |
| Unity 6.3 LTS | Desktop Windows/macOS/Linux [U5]; UI Toolkit MultiColumnListView supports virtualization, and runtime binding supports C# objects [U2/U3]. Batch mode and no-graphics automation are documented [U6]. API profiles are .NET Standard 2.1 / .NET Framework, not a .NET 10 runtime [U4]. | LTS support through December 2027 [U1]. 2026 Personal eligibility extends to USD 200,000 revenue/funding; Pro lists USD 2,310/seat/year prepaid. Runtime Fee was cancelled [U7]. Terms, eligibility and taxes still apply. |
| Unreal Engine, current UE 5.8 documentation | Desktop packaging, UMG, DataTables, SaveGame, Low-Level Tests and Insights are documented [E1–E6]. UMG Viewmodel is marked Beta [E2]. | Standard royalty-game terms are 5% on applicable lifetime gross revenue above USD 1 million per product, with EULA exclusions. A qualifying program can reduce the rate to 3.5%; this is conditional [E7/E8]. |

**FACT:** .NET 10 LTS support ends November 14, 2028; .NET 8 and 9 support ends November 10, 2026 [N1]. Local inspection found SDK 9.0.300. No engine installation or initialization was performed.

**FACT:** Godot's built-in C# editor is limited compared with an external IDE; exported properties/tool changes require rebuilding and general hot-reload state preservation is limited [G3]. Desktop support does not imply C# web export. Unity documents command-line Test Framework execution [U8]. Unreal's C++ Automation Framework depends on engine systems and directs pure unit tests toward Low-Level Tests [E5]. The separate Low-Level Tests page exposed no usable body text in this review; exact standalone/headless invocation remains unverified. Unreal command-line editor/client automation is documented [E11]. No project-specific headless engine test was run.

## Project-specific assessment

All judgments in this table are **PROPOSAL**, inferred from documented capabilities rather than measured project benchmarks.

| Requirement | Godot .NET | Unity | Unreal |
| --- | --- | --- | --- |
| Windows and 2D | Desktop export and dedicated 2D workflow [G3/G8]; Controls suit panels | Windows export and documented 2D workflow [U5/U9] | Desktop packaging and Paper 2D plugin [E1/E10]; extra workflow surface |
| Language / editor implications | Modern .NET C# core, external IDE and rebuild discipline | C# gameplay; Unity runtime/API profile requires compatibility testing | C++/Blueprint gameplay [E9]; C# integration would add an unqualified bridge |
| Dense tables, forms, comparisons | Controls fit; implement project binding, filtering, sorting and pagination | Strongest documented ready-made table foundation among these candidates | UMG/Slate can support the workflow; integration conventions require care |
| Structured data / simulation | Plain C# objects; avoid a Node per entity | Plain C# objects; avoid a MonoBehaviour per entity | Plain C++ objects; avoid an Actor/UObject per record |
| Determinism | Independent core can own arithmetic, ordering and RNG | Same approach; API profile limits matter | Same approach in C++; engine physics is unnecessary here |
| Headless tests | Standalone .NET runner plus engine headless smoke tests | Standalone compatible core plus batch/editor integration | C++ core plus low-level and engine integration tests |
| Save/load/versioning | Explicit DTOs and project migration rules | Explicit DTOs; scene/asset serialization does not define save policy | SaveGame offers integration; project schema and migration remain necessary |
| Content iteration | Text JSON and validators, presentation scenes separate | JSON plus editor tooling; constrain reliance on engine assets | DataTables offer authoring; control asset-format coupling |
| UI iteration | Reusable native controls and themes; more custom binding work | Runtime UI Toolkit offers relevant reusable infrastructure | Designer tooling is useful; C++/Blueprint boundaries add coordination |
| AI-assisted coding | C# and text assets support reviewable diffs and external tests | C# is reviewable; asset/editor lifecycle requires discipline | C++ is reviewable; Blueprint-heavy domain changes are harder to inspect as text |
| Debugging / observability | C# debugger and application causal traces | Editor tooling plus domain traces | Insights plus domain traces; powerful profiling does not replace causality |
| Build / small-team maintenance | Proposed simplest fit for this UI + 2D scope, subject to .NET/export verification | Credible alternative; editor automation and licensing need setup | Expected larger C++/asset/build burden for current needs |
| Plugins / ecosystem | Use built-in controls first; critical third-party UI plugins would need review | Relevant built-in UI reduces need for table plugins | Use built-in tooling; do not assume Beta MVVM is a mandatory dependency |
| Performance | No measured ranking; bound visible controls and profile C# allocations/interop | No measured ranking; virtualization is useful | No measured ranking; additional rendering capability is not a present requirement |
| Expansion / future modding | Domain and data-pack boundaries can remain engine-independent | Same, subject to managed API compatibility | Same conceptually; C++ and asset tooling increase reversal cost |
| Migration / lock-in | Rebuild UI/host when changing engine; preserve domain semantics | Rebuild UI/host; retarget incompatible .NET APIs | Language migration adds cost beyond replacing UI |

No candidate provides this game's deterministic semantics, save compatibility or information boundary automatically. Those remain project contracts.

## Recommendation and reversal

**PROPOSAL:** Prefer Godot .NET + C# with an engine-independent .NET 10 core. Prefer native UI controls, bounded tables and a thin engine adapter. MIT terms, a modern standalone runtime and text-based development fit the small team's scope.

Unity is the priority alternative because dense management UI is central and its documented table/binding features directly address that need. Unreal remains technically capable, but current scope does not justify its expected integration burden.

**OPEN QUESTION:** Can the exact Godot/.NET combination export reliably, and can the proposed UI meet interaction requirements without a large custom framework? Resolve through QG-01/QG-02 in the [testing strategy](TESTING_DETERMINISM_DEBUGGING.md), only after implementation authority is granted.

Do not silently switch engines if a gate fails. Return evidence and an amended ADR to Director. See [ADR-TF-001](ENGINE_FRAMEWORK_ADR.md).

## Official source register

FACT: These sources were reverified on 2026-10-04. Version-specific sources take precedence over moving stable pages for implementation. Recheck licensing before purchase/release and API compatibility before an upgrade.

| ID | Official source | Claim supported |
| --- | --- | --- |
| G1 | [Godot Windows download](https://godotengine.org/download/windows/) | 4.7.2 .NET download and export templates |
| G2 | [Godot license](https://godotengine.org/license/) | MIT and notice obligations |
| G3 | [Godot C# basics](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html) | C# desktop support and tooling |
| G4 | [Godot UI documentation](https://docs.godotengine.org/en/stable/tutorials/ui/index.html) | Control/container UI |
| G5 | [Godot Tree](https://docs.godotengine.org/en/stable/classes/class_tree.html) | Multi-column management controls |
| G6 | [Godot command line](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html) | Headless and export operations |
| G7 | [GodotSharp 4.7.2 project](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/GodotSharp.csproj) | Library target framework |
| U1 | [Unity release support](https://unity.com/releases/unity-6/support) | 6.3 LTS lifecycle |
| U2 | [Unity MultiColumnListView](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-uxml-element-MultiColumnListView.html) | Tables and virtualization |
| U3 | [Unity runtime binding](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-runtime-binding.html) | Binding to C# data |
| U4 | [Unity .NET profiles](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html) | Managed API compatibility |
| U5 | [Unity system requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html) | Desktop support |
| U6 | [Unity editor CLI](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/editor) | Batch/no-graphics automation |
| U7 | [Unity pricing updates](https://unity.com/products/pricing-updates) | 2026 plans and Runtime Fee cancellation |
| E1 | [Unreal packaging](https://dev.epicgames.com/documentation/unreal-engine/packaging-your-project) | Desktop deployment |
| E2 | [Unreal UMG Viewmodel](https://dev.epicgames.com/documentation/unreal-engine/API/PluginIndex/ModelViewViewModel) | MVVM capability and Beta status |
| E3 | [Unreal data-driven gameplay](https://dev.epicgames.com/documentation/en-us/unreal-engine/data-driven-gameplay-elements-in-unreal-engine) | DataTables and data authoring |
| E4 | [Unreal SaveGame](https://dev.epicgames.com/documentation/en-us/unreal-engine/saving-and-loading-your-game-in-unreal-engine) | Save/load integration |
| E5 | [Unreal Automation Framework](https://dev.epicgames.com/documentation/en-us/unreal-engine/automation-test-framework-in-unreal-engine) | C++ tests, engine dependency and referral to Low-Level Tests |
| E6 | [Unreal Insights](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-insights-in-unreal-engine) | Profiling and debugging |
| E7 | [Unreal licensing](https://www.unrealengine.com/license) | Royalty-game terms |
| E8 | [Unreal EULA](https://www.unrealengine.com/eula/unreal) | Exclusions and conditional reduced rate |
| N1 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | Runtime support dates |
| G8 | [Godot 2D](https://docs.godotengine.org/en/stable/tutorials/2d/index.html) | Dedicated 2D workflow |
| U8 | [Unity command-line tests](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/test-framework-introduction/running-tests/run-tests-from-command-line) | Test Framework automation |
| U9 | [Unity 2D](https://docs.unity3d.com/6000.3/Documentation/Manual/Unity2D.html) | 2D workflow |
| E9 | [Unreal C++ programming](https://dev.epicgames.com/documentation/en-us/unreal-engine/programming-with-cplusplus-in-unreal-engine) | C++ and Blueprint model |
| E10 | [Unreal Paper 2D](https://dev.epicgames.com/documentation/en-us/unreal-engine/paper-2d-overview-in-unreal-engine) | Sprite-based 2D plugin |
| E11 | [Unreal test execution](https://dev.epicgames.com/documentation/en-us/unreal-engine/run-automation-tests-in-unreal-engine) | Editor/client command-line tests and reports |

**FACT:** Sources were rechecked on 2026-10-04, using official indexed excerpts where a direct page body was unavailable. Some C# documentation retains earlier-version examples; it does not certify the proposed .NET 10 combination. Unity vertical list virtualization does not imply horizontal virtualization [U2]. Unreal royalty-game licensing differs from its non-game seat model [E7/E8].

**PROPOSAL:** All three ecosystems have documented first-party UI, content and testing foundations; this does not establish plugin maintenance quality or team productivity. Godot has sufficient native features to justify qualification, Unity's first-party table/binding stack makes it the strongest fallback, and Unreal's broad C++ tooling does not offset its expected build burden here. Future modding would begin with validated data packs only if separately approved; no executable mods or workshop are authorized. Text content reduces data lock-in, while scenes/widgets/imported assets and editor extensions remain engine-specific.
