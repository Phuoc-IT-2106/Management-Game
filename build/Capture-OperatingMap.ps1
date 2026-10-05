param([string]$Output = 'artifacts/operating-map/candidates', [switch]$NoBuild, [switch]$Quick, [switch]$Refinement)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Map build failed' }
}
$mapCaptureRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $mapCaptureRoot | Out-Null
$mapCommit = (git -C $RepoRoot rev-parse HEAD).Trim()
$sourceLines = Get-ChildItem -LiteralPath "$RepoRoot/game/Client/UI" -Recurse -File | Where-Object { $_.Extension -in '.cs','.tscn' } | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring($RepoRoot.Length + 1).Replace('\','/') + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$sha = [Security.Cryptography.SHA256]::Create()
$digest = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($sourceLines -join "`n")))).Replace('-','').ToLowerInvariant()
$sha.Dispose()
$sourceLines | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $mapCaptureRoot 'source-files.txt')
$cases = @('normal','sponsor','competitive','quiet','empty','long-name','branding','missing-identity','list','decision','return','affairs')
$sizes = @('1280x720','1920x1080')
if ($Quick) { $cases = @('normal'); $sizes = @('1280x720') }
$modes = @('')
if ($Refinement) { $cases = @('normal','sponsor','competitive','quiet','long-name'); $modes = @('map','list') }
if ($Quick -and $Refinement) { $cases = @('normal'); $sizes = @('1280x720') }
foreach ($size in $sizes) {
  foreach ($mode in $modes) {
    foreach ($case in $cases) {
        $key = if ($mode) { "$mode-$case-$size" } else { "$case-$size" }
        $stdout = Join-Path $mapCaptureRoot "$key.stdout.log"
        $stderr = Join-Path $mapCaptureRoot "$key.stderr.log"
        $mapArgs = @('--path', ('"' + "$RepoRoot/game/Client" + '"'), '--resolution', $size, '--position','0,0','--audio-driver','Dummy',
            '--log-file', ('"' + (Join-Path $mapCaptureRoot "$key.engine.log") + '"'), 'res://UI/OperatingMap/OperatingMap.tscn', '--',
            '--map-verify', "--map-case=$case", "--map-viewport=$size", ('"--map-output=' + $mapCaptureRoot + '"'), "--map-commit=$mapCommit", "--map-source-digest=$digest")
        if ($mode) { $mapArgs += "--map-mode=$mode" }
        # Full synthetic workflows once per viewport; every candidate checks its actual layout.
        $fullVerify = !$Refinement -or ($case -eq 'normal' -and $mode -eq 'map')
        if (!$fullVerify) { $mapArgs = @($mapArgs | Where-Object { $_ -ne '--map-verify' }) }
        $process = Start-Process -FilePath $Godot -ArgumentList $mapArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $null = $process.Handle
        if (!$process.WaitForExit(60000)) { $process.Kill(); throw "Map capture timed out: $key" }
        $process.Refresh()
        if ($process.ExitCode -ne 0 -or (Get-Item -LiteralPath $stderr).Length -gt 0) { Get-Content $stdout; Get-Content $stderr; throw "Map capture failed: $key" }
        $manifest = Get-Content -Raw -LiteralPath (Join-Path $mapCaptureRoot "$key.json") | ConvertFrom-Json
        if ("$($manifest.Viewport.Width)x$($manifest.Viewport.Height)" -ne $size -or ($fullVerify -and $manifest.Checks.Count -lt 25)) { throw "Map capture metadata mismatch: $key" }
        Write-Output "PASS candidate $key"
    }
  }
}
Write-Output "OPERATING_MAP_CAPTURE_PASS $($cases.Count * $sizes.Count * $modes.Count) source=$digest"
