# Rules v3 measurement (docs/10-vertical-slice/RULES_V3_INCENTIVES.md): four owner policies x 8 seeds x 5 seasons through the
# Session API, a JSON report, and a failing exit when any metric of roadmap gates 1..Gate is not met.
param([string]$Output = 'artifacts/rules-v3', [int]$Gate = 1, [int]$Seeds = 8, [int]$Seasons = 5, [string]$Configuration = 'Debug', [switch]$NoBuild)
. "$PSScriptRoot/Environment.ps1"
$out = [IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $out | Out-Null
Push-Location $RepoRoot
try {
    if (!$NoBuild) {
        & $Dotnet build tools/Headless/Headless.csproj -c $Configuration -m:1 -p:RestoreLockedMode=true | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Headless build failed' }
    }
    & $Dotnet "tools/Headless/bin/$Configuration/net10.0/Headless.dll" --measure "--measure-seeds=$Seeds" "--measure-seasons=$Seasons" "--measure-gate=$Gate" "--measure-output=$out/measure.json" 2>&1 | Tee-Object -FilePath "$out/measure.log"
    if ($LASTEXITCODE -ne 0) { throw "Measurement gate $Gate is not met" }
} finally { Pop-Location }
