param([switch]$IncludeCaptures)
. "$PSScriptRoot/Environment.ps1"
$evidenceRoot = Join-Path $RepoRoot 'artifacts/ui-kit'
New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
& $Dotnet build "$RepoRoot/tests/UiKit/UiKit.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Pure UI Kit build failed' }
& $Dotnet "$RepoRoot/tests/UiKit/bin/Debug/net10.0/UiKit.dll" | Tee-Object -FilePath "$evidenceRoot/pure-tests.log"
if ($LASTEXITCODE -ne 0) { throw 'Pure UI Kit checks failed' }
& $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Godot client build failed' }
& $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$evidenceRoot/native-headless.engine.log" res://UI/Lab/UiLab.tscn -- --lab-verify --lab-viewport=1280x720 | Tee-Object -FilePath "$evidenceRoot/native-headless.log"
if ($LASTEXITCODE -ne 0) { throw 'Native UI checks failed' }
if ($IncludeCaptures) { & "$PSScriptRoot/Capture-UiKit.ps1" -NoBuild }
