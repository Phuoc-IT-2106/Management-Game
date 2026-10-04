# QG-01 clean Windows handoff — NOT EXECUTED

This procedure is pending. The current laptop is a development machine and is
not accepted clean-machine evidence. Use a Director-approved supported Windows
guest or physical machine; do not install prerequisites before recording the
pre-launch state.

1. Record Windows edition/version/build, CPU, installed RAM, GPU/driver, display
   scaling, whether Godot/SDK/development tools are installed, and the output of
   `dotnet --list-runtimes` if that command exists. Record absence explicitly.
2. Copy the **entire** `qualification/artifacts/followup/` directory, including
   `Qualification.exe`, `Qualification.pck` and `data_Host_windows_x86_64/`, plus
   `evidence/followup-reuse-manifest.json`. The manifest is the exact relative-path,
   byte-length and SHA256 inventory; copying the EXE alone is not sufficient.
3. Verify every copied relative path, size and SHA256 against that manifest and
   record any extra/missing file. Hash the manifest too. Record copy destination
   and exact copied inventory. Do not claim that a matching inventory proves that
   the application can run without installed dependencies.
4. If practical, disconnect network and record how/when it was disconnected.
   Otherwise record that it remained connected. Do not silently assume offline.
5. From the copied directory run `./Qualification.exe`. Record the launch command
   and process ID. Capture the rendered QG-01 UI. Click **Write and read tiny local
   fixture**, verify **Read-back verified**, capture it, then click **Exit**.
6. Retain stdout/stderr and exit code. The default `user://qg01.txt` is under the
   test user's Godot application data directory; record the actual resolved path
   and verify its contents are `managed-fixture-v1`. Test data is disposable.
7. Record startup, rendering, actual C# action, file round trip and clean exit
   separately. If a runtime/native-library prerequisite is missing, retain the
   error before attempting a remedy and return the finding to Director.

Automation may additionally run `./Qualification.exe -- --smoke --capture` with
redirected logs and an exit-code check. This exercises a synthetic button signal;
it does not replace observing the rendered application and real input.

Only actual execution on the recorded clean supported Windows environment can
close this sub-check. No such execution occurred in this follow-up.
