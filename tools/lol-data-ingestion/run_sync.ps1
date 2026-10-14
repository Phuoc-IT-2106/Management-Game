param(
    [int]$MaxPages = 1,
    [ValidateSet('leagues','series','tournaments','teams','players','matches')]
    [string[]]$Collections = @('leagues','series','tournaments','teams','players'),
    # Preset crawls (core-2026, full-2026) replace -Collections; -MaxRequests and -Resume apply to them only.
    [ValidateSet('core-2026','full-2026')]
    [string]$Preset,
    [int]$MaxRequests,
    [string]$Resume
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$Python = Join-Path $PSScriptRoot '.venv\Scripts\python.exe'
if (-not (Test-Path $Python)) { throw 'Python venv missing: run py -3 -m venv .venv and pip install requirements.txt' }
if ($Preset) {
    $Arguments = @('sync_pandascore.py', '--preset', $Preset, '--max-pages', $MaxPages)
    if ($MaxRequests) { $Arguments += @('--max-requests', $MaxRequests) }
    if ($Resume) { $Arguments += @('--resume', $Resume) }
    & $Python @Arguments
    if ($LASTEXITCODE -eq 3) { Write-Warning "Collection stopped before the end; continue later with -Resume (see the manifest's stop_reason)"; exit 3 }
} else {
    & $Python 'sync_pandascore.py' --max-pages $MaxPages --collections $Collections
}
if ($LASTEXITCODE -ne 0) { throw "PandaScore sync failed with exit code $LASTEXITCODE" }
