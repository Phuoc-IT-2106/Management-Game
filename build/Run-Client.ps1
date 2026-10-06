param([switch]$Smoke,[switch]$Headless,[switch]$Benchmark,[int]$Tab=0,[string]$Capture='')
. "$PSScriptRoot/Environment.ps1"
$evidence = Join-Path $RepoRoot 'docs/10-vertical-slice/evidence'
New-Item -ItemType Directory -Force $evidence | Out-Null
$clientArgs = @('--path',"$RepoRoot/game/Client",'--audio-driver','Dummy')
if($Headless) { $clientArgs += '--headless' }
$clientArgs += @('--',"--content=$RepoRoot/content/fixture.json","--saves=$RepoRoot/artifacts/client-saves","--tab=$Tab")
if($Smoke) { $clientArgs += @('--smoke',"--hash=$evidence/host-Debug.hash") }
if($Benchmark) { $clientArgs += @('--benchmark',"--report=$evidence/roster-Debug.json") }
if($Capture) { $clientArgs += "--capture=$Capture" }
& $Godot @clientArgs 2>&1 | Tee-Object -FilePath "$evidence/host-Debug.log"
if($LASTEXITCODE -ne 0) { throw 'Godot client failed' }
