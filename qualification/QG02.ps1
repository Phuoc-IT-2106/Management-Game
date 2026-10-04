param([ValidateSet('Debug','Release')][string]$Configuration='Release')
. "$PSScriptRoot/Environment.ps1"
if ($Configuration -eq 'Debug') {
    Invoke-Recorded 'qg02-build' $Dotnet @('build', "$HostProject/Host.csproj", '-c', 'Debug', '-m:1')
    Invoke-Recorded 'qg02-rendered-benchmark' $Godot @('--path', $HostProject, '--audio-driver', 'Dummy', '--', '--ui', '--benchmark')
} else {
    Invoke-Recorded 'qg02-export' $Godot @('--headless','--path',$HostProject,'--export-release','Windows x64',"$PSScriptRoot/artifacts/windows/Qualification.exe")
    if (Select-String -Path "$Evidence/qg02-export.log" -Pattern 'ERROR: Export|ERROR: System\.|Failed to build|Failed to export') { throw 'Managed export failed' }
    Invoke-Recorded 'qg02-rendered-benchmark' "$PSScriptRoot/artifacts/windows/Qualification.exe" @('--audio-driver','Dummy','--','--ui','--benchmark')
}
if (-not (Select-String -Path "$Evidence/qg02-rendered-benchmark.log" -SimpleMatch 'QG02_CHECKS_PASS')) { throw 'UI harness did not finish' }
$data = "$env:APPDATA/Godot/app_userdata/Phase 4B Foundation Qualification"
Copy-Item "$data/qg02.json" "$Evidence/qg02.json"
Copy-Item "$data/qg02-render.png" "$Evidence/qg02-render.png"
if ($Configuration -eq 'Release') {
    Get-ChildItem "$PSScriptRoot/artifacts/windows" -Recurse -File | ForEach-Object {
        [pscustomobject]@{Path=$_.FullName.Substring($PSScriptRoot.Length+1);Bytes=$_.Length;SHA256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}
    } | ConvertTo-Json | Out-File -Encoding utf8 "$Evidence/qg02-artifact-manifest.json"
}
