param([string]$Output = 'artifacts/ui-kit/candidates', [switch]$NoBuild, [switch]$Quick)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'UI Lab build failed' }
}
$captureRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $captureRoot | Out-Null
$commit = (git -C $RepoRoot rev-parse HEAD).Trim()
$dirty = [bool](git -C $RepoRoot status --porcelain)
$sourceLines = Get-ChildItem -LiteralPath "$RepoRoot/game/Client/UI" -Recurse -File | Where-Object { $_.Extension -in '.cs','.tscn' } | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring($RepoRoot.Length + 1).Replace('\','/') + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$sourceBytes = [Text.Encoding]::UTF8.GetBytes(($sourceLines -join "`n"))
$sha = [Security.Cryptography.SHA256]::Create()
$digest = [BitConverter]::ToString($sha.ComputeHash($sourceBytes)).Replace('-','').ToLowerInvariant()
$sha.Dispose()
$sourceLines | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $captureRoot 'source-files.txt')
# Fixed cases: page, brand, density, text scale, viewport, scroll fraction.
$cases = @(
    @('identity','neutral','Default','1','1280x720','0'),
    @('interaction','warning','Default','1','1920x1080','0'),
    @('identity','bright','Compact','1','1920x1080','0'),
    @('identity','dark','Comfortable','1.5','1280x720','0'),
    @('identity','low-contrast','Default','1','2560x1440','0'),
    @('identity','identical','Default','1','1280x720','1'),
    @('identity','missing-emblem','Default','1','1920x1080','0'),
    @('interaction','neutral','Default','1','1280x720','1'),
    @('documents','neutral','Default','1','1920x1080','0'),
    @('documents','neutral','Default','1','1920x1080','0.5'),
    @('documents','neutral','Default','1','1920x1080','1'),
    @('branding','neutral','Default','1','1920x1080','0'),
    @('branding','neutral','Default','1','1920x1080','0.5'),
    @('branding','neutral','Default','1','1920x1080','1')
)
if ($Quick) { $cases = ,$cases[0] }
foreach ($case in $cases) {
    $key = $case -join '-'
    $log = Join-Path $captureRoot "$key.stdout.log"
    $errLog = Join-Path $captureRoot "$key.stderr.log"
    # Quote only filesystem args; fixture args are fixed literals above. No shell eval.
    $args = @('--path', ('"' + "$RepoRoot/game/Client" + '"'), '--resolution', $case[4], '--position','0,0', '--audio-driver','Dummy',
        '--log-file', ('"' + (Join-Path $captureRoot "$key.engine.log") + '"'), 'res://UI/Lab/UiLab.tscn', '--',
        '--lab-verify', "--lab-viewport=$($case[4])", "--lab-page=$($case[0])", "--lab-brand=$($case[1])", "--lab-density=$($case[2])", "--lab-text-scale=$($case[3])",
        "--lab-scroll=$($case[5])", ('"--lab-output=' + $captureRoot + '"'), "--lab-commit=$commit", "--lab-source-digest=$digest", "--lab-dirty=$dirty")
    $process = Start-Process -FilePath $Godot -ArgumentList $args -PassThru -WindowStyle Hidden -RedirectStandardOutput $log -RedirectStandardError $errLog
    $null = $process.Handle
    if (!$process.WaitForExit(60000)) { $process.Kill(); throw "UI Lab timed out: $key" }
    $process.Refresh()
    if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $log; Get-Content -LiteralPath $errLog; throw "UI Lab failed: $key ($($process.ExitCode))" }
    $manifestName = "$($case[0])-$($case[1])-$($case[2])-$($case[3])-$($case[4])-scroll$($case[5]).json"
    $manifest = Get-Content -Raw -LiteralPath (Join-Path $captureRoot $manifestName) | ConvertFrom-Json
    if ("$($manifest.Viewport.Width)x$($manifest.Viewport.Height)" -ne $case[4] -or $manifest.Checks.Count -lt 20) { throw "Capture metadata mismatch: $key" }
    if ((Get-Item -LiteralPath $errLog).Length -gt 0) { throw "Review non-empty engine stderr: $errLog" }
    Write-Output "PASS candidate $manifestName"
}
Write-Output "UI_KIT_CAPTURE_MATRIX_PASS $($cases.Count) source=$digest"
