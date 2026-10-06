param([string]$Configuration = 'Release')
. "$PSScriptRoot/Environment.ps1"
$evidence = Join-Path $RepoRoot 'docs/10-vertical-slice/evidence'
New-Item -ItemType Directory -Force -Path $evidence,(Join-Path $RepoRoot 'artifacts') | Out-Null
Push-Location $RepoRoot
try {
    & $Dotnet build ManagementGame.slnx -c $Configuration -m:1 -p:RestoreLockedMode=true 2>&1 | Tee-Object -FilePath "$evidence/build-$Configuration.log"
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    foreach ($name in @('Domain','Integration','UI')) {
        & $Dotnet "tests/$name/bin/$Configuration/net10.0/$name.dll" 2>&1 | Tee-Object -FilePath "$evidence/tests-$name-$Configuration.log"
        if ($LASTEXITCODE -ne 0) { throw "$name tests failed" }
    }
    & $Dotnet "tools/Headless/bin/$Configuration/net10.0/Headless.dll" "--output=$evidence/headless-$Configuration.json" --scale "--scale-output=$evidence/query-scale-$Configuration.json" 2>&1 | Tee-Object -FilePath "$evidence/headless-$Configuration.log"
    if ($LASTEXITCODE -ne 0) { throw 'Headless scenario failed' }
} finally { Pop-Location }
