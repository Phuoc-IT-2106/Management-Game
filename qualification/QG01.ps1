. "$PSScriptRoot/Environment.ps1"
Invoke-Recorded 'qg01-sdk' $Dotnet @('--info')
Invoke-Recorded 'qg01-godot' $Godot @('--version')
Invoke-Recorded 'qg01-build' $Dotnet @('build', "$HostProject/Host.csproj", '-c', 'Debug')
Invoke-Recorded 'qg01-import' $Godot @('--headless', '--path', $HostProject, '--editor', '--quit')
Invoke-Recorded 'qg01-debug-smoke' $Godot @('--headless', '--path', $HostProject, '--', '--smoke')
New-Item -ItemType Directory -Force "$PSScriptRoot/artifacts/windows" | Out-Null
Invoke-Recorded 'qg01-export' $Godot @('--headless', '--path', $HostProject, '--export-release', 'Windows x64', "$PSScriptRoot/artifacts/windows/Qualification.exe")
if (Select-String -Path "$Evidence/qg01-export.log" -Pattern 'ERROR: Export|ERROR: System\.|Failed to build|Failed to export') { throw 'Managed export failed despite engine exit code' }
Invoke-Recorded 'qg01-export-smoke' "$PSScriptRoot/artifacts/windows/Qualification.exe" @('--headless', '--', '--smoke')
if (-not (Select-String -Path "$Evidence/qg01-export-smoke.log" -SimpleMatch 'QG01_INTERACTION_PASS')) { throw 'Export did not execute managed interaction' }
Get-ChildItem "$PSScriptRoot/artifacts/windows" -Recurse -File | ForEach-Object {
    [pscustomobject]@{ Path=$_.FullName.Substring($PSScriptRoot.Length + 1); Bytes=$_.Length; SHA256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash }
} | ConvertTo-Json -Depth 4 | Out-File -Encoding utf8 "$Evidence/qg01-artifact-manifest.json"
