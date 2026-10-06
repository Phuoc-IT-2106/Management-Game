. "$PSScriptRoot/Environment.ps1"
$sponsorEvidence=Join-Path $RepoRoot 'artifacts/sponsor-workspace/verify'
New-Item -ItemType Directory -Force -Path $sponsorEvidence | Out-Null
& $Dotnet build "$RepoRoot/tests/SponsorWorkspace/SponsorWorkspace.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if($LASTEXITCODE -ne 0) { throw 'Sponsor test build failed' }
& $Dotnet "$RepoRoot/tests/SponsorWorkspace/bin/Debug/net10.0/SponsorWorkspace.dll"
if($LASTEXITCODE -ne 0) { throw 'Sponsor tests failed' }
& $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
if($LASTEXITCODE -ne 0) { throw 'Client build failed' }
& $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$sponsorEvidence/native.engine.log" res://UI/SponsorWorkspace/SponsorVerification.tscn -- --sponsor-verify --sponsor-viewport=1280x720
if($LASTEXITCODE -ne 0) { throw 'Sponsor native tests failed' }
