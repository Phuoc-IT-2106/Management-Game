. "$PSScriptRoot/Environment.ps1"
Invoke-Recorded 'qg02-build' $Dotnet @('build', "$HostProject/Host.csproj", '-c', 'Debug')
Invoke-Recorded 'qg02-rendered-benchmark' $Godot @('--path', $HostProject, '--audio-driver', 'Dummy', '--', '--ui', '--benchmark')
if (-not (Select-String -Path "$Evidence/qg02-rendered-benchmark.log" -SimpleMatch 'QG02_CHECKS_PASS')) { throw 'UI harness did not finish' }
$data = "$env:APPDATA/Godot/app_userdata/Phase 4B Foundation Qualification"
Copy-Item "$data/qg02.json" "$Evidence/qg02.json"
Copy-Item "$data/qg02-render.png" "$Evidence/qg02-render.png"
