param([string]$Output='artifacts/sponsor-workspace/acceptance/checks')
. "$PSScriptRoot/Environment.ps1"
$evidence=[IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
Push-Location $RepoRoot
try {
    & $Dotnet build ManagementGame.slnx -c Debug -m:1 -p:RestoreLockedMode=true > "$evidence/build-solution.log"
    if($LASTEXITCODE -ne 0) { throw 'Solution build failed' }
    foreach($name in @('SponsorWorkspace','UiKit','OperatingMap')) {
        & $Dotnet build "tests/$name/$name.csproj" -c Debug -m:1 -p:RestoreLockedMode=true > "$evidence/build-$name.log"
        if($LASTEXITCODE -ne 0) { throw "$name build failed" }
    }
    foreach($name in @('SponsorWorkspace','Domain','Integration','UI','UiKit','OperatingMap')) {
        & $Dotnet "tests/$name/bin/Debug/net10.0/$name.dll" > "$evidence/tests-$name.log"
        if($LASTEXITCODE -ne 0) { throw "$name checks failed" }
        Get-Content "$evidence/tests-$name.log" -Tail 1
    }
    & $Dotnet tools/Headless/bin/Debug/net10.0/Headless.dll "--output=$evidence/headless.json" > "$evidence/headless.log"
    if($LASTEXITCODE -ne 0) { throw 'Headless failed' }
    & $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$evidence/kit.engine.log" res://UI/Lab/UiLab.tscn -- --lab-verify --lab-viewport=1280x720 > "$evidence/kit.log" 2> "$evidence/kit.stderr.log"
    if($LASTEXITCODE -ne 0 -or (Get-Item "$evidence/kit.stderr.log").Length -gt 0) { throw 'Native kit failed' }
    & $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$evidence/map.engine.log" res://UI/OperatingMap/OperatingMap.tscn -- --map-verify --map-viewport=1280x720 > "$evidence/map.log" 2> "$evidence/map.stderr.log"
    if($LASTEXITCODE -ne 0 -or (Get-Item "$evidence/map.stderr.log").Length -gt 0) { throw 'Native map failed' }
    $saves=Join-Path $evidence ('smoke-saves-'+[Guid]::NewGuid().ToString('N'))
    & $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$evidence/main.engine.log" res://Main.tscn -- --smoke "--saves=$saves" "--hash=$evidence/main.hash" > "$evidence/main.log" 2> "$evidence/main.stderr.log"
    if($LASTEXITCODE -ne 0 -or (Get-Item "$evidence/main.stderr.log").Length -gt 0) { throw 'Main smoke failed' }
    $headless=Get-Content -Raw -Encoding utf8 "$evidence/headless.json" | ConvertFrom-Json
    if((Get-Content -Raw "$evidence/main.hash").Trim() -ne $headless.FinalHash) { throw 'Main/headless mismatch' }
    Write-Output "MAIN_HEADLESS_PARITY_PASS $($headless.FinalHash)"
    & "$PSScriptRoot/Capture-SponsorWorkspace.ps1" -NoBuild -Output (Join-Path $Output '../candidates')
    if($LASTEXITCODE -ne 0) { throw 'Sponsor captures failed' }
    Write-Output 'SPONSOR_ACCEPTANCE_REGRESSION_PASS'
} finally { Pop-Location }
