param([switch]$IncludeCaptures)
. "$PSScriptRoot/Environment.ps1"
$mapEvidence = Join-Path $RepoRoot 'artifacts/operating-map'
New-Item -ItemType Directory -Force -Path $mapEvidence | Out-Null
& $Dotnet build "$RepoRoot/tests/OperatingMap/OperatingMap.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Map pure build failed' }
& $Dotnet "$RepoRoot/tests/OperatingMap/bin/Debug/net10.0/OperatingMap.dll" | Tee-Object -FilePath "$mapEvidence/pure-tests.log"
if ($LASTEXITCODE -ne 0) { throw 'Map pure checks failed' }
& $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }
& $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$mapEvidence/native.engine.log" res://UI/OperatingMap/OperatingMap.tscn -- --map-verify --map-viewport=1280x720 | Tee-Object -FilePath "$mapEvidence/native.log"
if ($LASTEXITCODE -ne 0) { throw 'Map native checks failed' }
if ($IncludeCaptures) { & "$PSScriptRoot/Capture-OperatingMap.ps1" -NoBuild }
