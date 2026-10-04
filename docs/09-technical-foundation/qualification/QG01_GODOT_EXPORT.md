# QG-01 — Godot/.NET Windows export

Date: 2026-10-04 (Asia/Saigon). **Result: INCONCLUSIVE.**

## Objective and authority

Prove the selected Windows deployment path with one synthetic C# action and tiny
local file round trip. **DECISION:** Current Phase 4B request authorizes this
qualification; older PLAN/SPEC restrictions describe the previous assignment.
Phase 4 is not closed. ADR-TF-001 remains conditional for qualification.

## Environment and exact versions

**FACT:** Windows x64 10.0.19044, Intel i5-1135G7, Intel Iris Xe driver
32.0.101.7080, local C: NTFS. Hardware is observed, not an approved minimum PC.
Godot **4.7.2.stable.mono.official.ed1daf0bf**; template version
**4.7.2.stable.mono**. SDK **10.0.401**, MSBuild **18.9.11**, runtime
**10.0.12**, target **net10.0**, all qualification-local. Existing system SDK
9.0.300 and multiple runtimes mean this is expressly not a clean machine.

Official references checked before installation:
[candidate archive](https://godotengine.org/download/archive/4.7.2-stable/),
[GodotSharp target](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/GodotSharp.csproj),
[C# tooling](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html),
[runtime support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
The net8.0 GodotSharp library alone did not establish net10.0 host compatibility;
the executed fixture supplies that narrower evidence.

## Steps, commands and tests actually run

From repository root (PowerShell):

```powershell
powershell -NoProfile -File qualification/Provision.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File qualification/QG01.ps1
. ./qualification/Environment.ps1
Invoke-Recorded 'qg01-rendered-smoke' "$QualificationRoot/artifacts/windows/Qualification.exe" @('--', '--smoke', '--capture')
```

Provision verifies archive hashes and extracts only matching Windows x64
templates. The SDK SHA512 was checked against official release metadata.
Godot/template SHA256 values pin the HTTPS downloads, not independent signatures.
The provisioning script itself was also executed successfully against the caches.
QG01.ps1 records exact expanded build/import/export/run commands in `evidence/`.
`global.json` pins this experimentally working SDK for reproducibility, not a
production runtime decision. Bundled Godot packages are version-locked.

| Executed check | Measured result |
| --- | --- |
| Debug managed build | 0 errors; final build 0 warnings |
| Headless editor import | Exit 0; certificate-store error under sandbox |
| Debug host signal → C# → file round trip | Success marker, exit 0, .NET 10.0.12 |
| Windows x64 Release export | Artifact produced; managed output verified by execution |
| Exported headless interaction | Success marker and exit 0 |
| Exported rendered interaction | Intel OpenGL 3.3 Compatibility; success marker and exit 0 |
| Runtime location | Export uses `data_Host_windows_x86_64`, not installed runtime directory |
| Visual check | Captured 1280×720 UI inspected; status reads “Read-back verified” |
| Artifact inventory | 195 files, 195,768,757 bytes at QG-01 capture |

The synthetic harness emits the real button signal. It does not claim a physical
mouse/keyboard test. `user://qg01.txt` is local disposable fixture data.

## Failures and bounded fixes

**FACT:** First export lacked `Host.sln`. Godot logged an export exception yet
returned zero and left an unusable incomplete artifact. The solution is now
included; scripts check managed-export errors and require the runtime success
marker. Failed evidence is retained in `qg01-export-missing-solution.log`.

The console launcher remained after its engine child exited. Its owned process
was stopped; automation now starts the main executable with redirected output
and a timeout. See `qg01-export-wrapper-stall.log`. The earlier script also
misread a missing executable as the preceding exit code; it now validates the
executable path before launch. No false earlier result is used as pass evidence.

Initial restore could not reach NuGet vulnerability data (NU1900). Offline fixture
runs explicitly disable that audit; no vulnerability audit is claimed. The
rendered sandbox run reports unavailable WASAPI and uses dummy audio; it also
reports inaccessible root certificates. These did not prevent the fixture action
or rendering. Audio/network behavior is not qualified.

## Limitations and recommendation

**FACT:** No clean Windows VM/second PC was available; Windows Sandbox executable
was absent. No installed runtime was removed. Network was not physically
disconnected. Editor import and Debug execution were tested; interactive external
debugger attachment/breakpoint/step workflow was **NOT RUN**.

Therefore **INCONCLUSIVE**, not PASS. Local exported-app evidence supports
continuing independent qualification; there is no demonstrated engine blocker.
**PROPOSAL:** Recommend net10.0 / SDK 10.0.401 / runtime 10.0.12 provisionally based
on executed compatibility, subject to clean-machine acceptance and OS support
review. Do not hard-lock production runtime or close the engine ADR.

**OPEN QUESTION:** Assign a clean supported Windows machine with no Godot, SDK or
separately installed .NET. Copy the complete artifact, disconnect network, launch
`Qualification.exe`, activate write/read, verify status, exit, and retain OS,
input/render/exit observations plus the manifest. Also verify native runtime
prerequisites and debugger workflow before closing this gate.

## Retained evidence

- `evidence/qg01-*.log`: commands, tool identity, build/import/export and smoke output.
- `evidence/qg01-artifact-manifest.json`: every exported file size and SHA256.
- `evidence/qg01-render.png`: inspected rendered capture.
- `qualification/Host`, provisioning/environment/gate scripts and package lock.
- Local ignored binaries under `qualification/artifacts/windows`; downloads under
  `qualification/.tools`. Later gates may regenerate this output; the committed
  QG-01 manifest and gate commit identify this earlier artifact precisely.

## Director follow-up — 2026-10-04

This section supplements the historical evidence above. The user explicitly
limited this follow-up to the existing laptop; no external machine is available.
**QG-01 remains INCONCLUSIVE.**

### Clean-machine investigation

`evidence/followup-environment*.log` records Windows 10 Pro build 19044,
Dell Vostro 3400, i5-1135G7 (4 cores / 8 logical processors), 8,299,257,856 bytes
physical RAM, Iris Xe driver 32.0.101.7080 and 1920x1080 / 60 Hz display.
Windows Sandbox, VirtualBox and VMware executables were absent. HypervisorPresent
and a running vmcompute service do **not** demonstrate an available clean Windows
guest. System runtimes include .NET 8, 9 and 10; this host is not clean.

No artifact was copied to a separate machine, no network adapter was disconnected,
and no clean-machine launch, screenshot or exit result exists. Those fields are
**NOT RUN / INCONCLUSIVE**, not inferred from development-host execution. The
follow-up artifact inventory is recorded separately from historical manifests.

### Debugger investigation

The actual Debug build succeeded with zero warnings/errors; see
`evidence/followup-qg01-debug-build.log`. This is build evidence only.
Installed Visual Studio Community 2022 and Preview report 17.14.36119.2;
VS Code reports 1.140.0, commit 07f806f999227108933c2e30515b26eecc1fda74.
No installed VS Code C# / C# Dev Kit extension was found. Microsoft documents
that targeting net10.0 in Visual Studio requires version 18.0 or later; this
machine's VS 17 installation is not the supported IDE configuration for this
candidate. No unsupported compatibility override was attempted.

External debugger launch/attach, breakpoint hit, variable inspection, stepping
and continue were **NOT RUN**. All five debugger checks remain **INCONCLUSIVE**.
No debugger version is claimed as exercised. A supported VS Code C# debugger or
a matching Visual Studio installation and an actual recorded session remain
required; installation presence or a successful build cannot substitute for it.

### Runtime disposition

**Recommendation:** retain Godot 4.7.2.stable.mono.official.ed1daf0bf, matching
Windows x64 templates, SDK 10.0.401 / MSBuild 18.9.11, bundled runtime 10.0.12 and
net10.0 as the exact **qualification reproduction pin**. This recommendation is
based on executed Debug/Release fixture compatibility and the existing QG-03/04
evidence, not on .NET 10 being newer. No alternate target was executed in this
follow-up, so there is no comparative evidence to select one.
Microsoft's policy at review lists .NET 10 as LTS through November 14, 2028
(current patch 10.0.12), whereas .NET 8/9 end support in November 2026. That
servicing horizon is a secondary reason to continue qualifying this already-tested
candidate; it does not establish Godot-specific compatibility or a production pin.

**Production pinning is NOT recommended now.** Remaining risks are clean Windows
native prerequisites and deployment, external debugger support, and the supported
OS matrix. Local operation on Windows 10 Pro 19044 does not establish a supported
production OS/toolchain combination. Reconcile the selected OS edition/build
with Microsoft's current support matrix before acceptance; this laptop is not an
approved target PC. Published tool support and executed fixture compatibility are
separate claims. No SDK/runtime/build configuration has been changed.
The current .NET 10 matrix lists Windows 10 21H2 only for Enterprise/IoT editions,
not this Pro edition. A supported Windows 11 x64 target is a candidate for the
pending clean-machine test, not an environment verified in this follow-up.

Primary references checked during this follow-up:

- [Godot C# setup and external debugger workflows](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html).
- [Microsoft SDK / MSBuild / Visual Studio requirements](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- [.NET 10 supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

See [FOLLOWUP_SUMMARY.md](FOLLOWUP_SUMMARY.md) for the final artifact, local smoke
result, regression scope and Director disposition. None closes the clean-machine
or debugger sub-check.

The final local follow-up smoke executed
`qualification/artifacts/followup/Qualification.exe --audio-driver Dummy -- --smoke --capture`.
It rendered, loaded .NET 10.0.12 from its own `data_Host_windows_x86_64` directory,
emitted `QG01_INTERACTION_PASS` after the C# write/read, and exited 0. The inspected
`evidence/followup-reuse-smoke.png` displays **Read-back verified**. Exact command,
runtime directory and exit are in `followup-reuse-smoke.log`; 199 files totaling
195,918,925 bytes are inventoried in `followup-reuse-manifest.json`.
This additional development-host evidence does not change QG-01's INCONCLUSIVE
status. No separate-machine copied inventory, launch, offline run or debugger
breakpoint evidence is claimed.
