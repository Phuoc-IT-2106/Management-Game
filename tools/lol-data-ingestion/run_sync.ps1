param(
    [int]$MaxPages = 1,
    [ValidateSet('leagues','series','tournaments','teams','players','matches')]
    [string[]]$Collections = @('leagues','series','tournaments','teams','players')
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$Python = Join-Path $PSScriptRoot '.venv\Scripts\python.exe'
if (-not (Test-Path $Python)) { throw 'Python venv missing: run py -3 -m venv .venv and pip install requirements.txt' }
& $Python 'sync_pandascore.py' --max-pages $MaxPages --collections $Collections
if ($LASTEXITCODE -ne 0) { throw "PandaScore sync failed with exit code $LASTEXITCODE" }
