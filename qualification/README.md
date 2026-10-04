# Phase 4B qualification fixtures

Disposable, synthetic foundation experiments only. No gameplay or Python port.
Current Director authority is the 2026-10-04 Phase 4B request: ADR-TF-001
conditionally accepted for qualification, ADR-TF-002 accepted, Phase 4 exit
not passed, Phase 5 not authorized. Historical specification labels are retained.

Run PowerShell scripts from this directory. Tool downloads and generated outputs
stay in ignored `.tools/` and `artifacts/`. Gate reports and curated raw evidence
live in `../docs/09-technical-foundation/qualification/`.

The SDK/runtime is an experimental candidate until QG-01 evidence supports a
recommendation. No production runtime choice is made by these fixtures.

Entry point: [Director qualification summary](../docs/09-technical-foundation/qualification/QUALIFICATION_SUMMARY.md).

```powershell
./Provision.ps1
./QG01.ps1
./QG02.ps1
./QG03.ps1
./QG04.ps1
```

Run in order. Scripts capture expanded commands and process exits. QG-02 performs
five minutes of rendered interaction. QG-03 exports/runs the shared core. QG-04
changes only owned fixture ACLs (restored), simulates disk-full and kills only
owned child test processes. On a restricted execution sandbox, Godot export or
NTFS File.Replace may need an ordinary Windows execution context. Do not treat
sandbox failure as proof of an engine/filesystem defect.

These scripts verify the stated local fixtures; they do not replace the clean
Windows machine, external debugger and physical/DPI follow-up in the gate reports.
