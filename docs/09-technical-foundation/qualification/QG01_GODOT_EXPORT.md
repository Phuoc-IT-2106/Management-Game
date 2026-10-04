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
