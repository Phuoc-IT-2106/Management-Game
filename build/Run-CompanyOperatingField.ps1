param(
    [ValidateSet('1280x720','1920x1080')][string]$Size = '1280x720',
    [switch]$Capture,
    [switch]$Verify,
    [switch]$NoBuild
)
. "$PSScriptRoot/Environment.ps1"
$fieldRoot = Join-Path $RepoRoot 'artifacts/company-operating-field'
New-Item -ItemType Directory -Force -Path $fieldRoot | Out-Null
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Company Operating Field build failed' }
}
$fieldArgs = @('--path',('"'+"$RepoRoot/game/Client"+'"'),'--resolution',$Size,'--position','0,0','--audio-driver','Dummy',
    '--log-file',('"'+"$fieldRoot/field-$Size.engine.log"+'"'),'res://UI/Lab/CompanyOperatingField.tscn','--',"--field-viewport=$Size")
if ($Capture) {
    $captureRoot = Join-Path $RepoRoot 'docs/10-vertical-slice/company-ui-demo/operating-field-evidence'
    New-Item -ItemType Directory -Force -Path $captureRoot | Out-Null
    $fieldSources = Get-ChildItem "$RepoRoot/game/Client","$RepoRoot/src" -Recurse -File | Where-Object {
        $_.FullName -notmatch '\\(obj|bin|\.godot)\\' -and $_.Extension -in '.cs','.tscn','.godot','.csproj'
    }
    $fieldSources += Get-Item "$RepoRoot/content/fixture.json"
    $sourceLines = $fieldSources | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($RepoRoot.Length+1).Replace('\','/')+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    $digest = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($sourceLines -join "`n")))).Replace('-','').ToLowerInvariant()
    $sha.Dispose()
    $sourceLines | Set-Content -Encoding UTF8 -LiteralPath "$fieldRoot/source-files.txt"
    $head = (git -C $RepoRoot rev-parse HEAD).Trim()
    $fieldArgs += ('"--field-output='+$captureRoot+'"'),"--field-commit=$head","--field-source-digest=$digest"
}
if ($Verify) { $fieldArgs += '--field-verify' }
if ($Capture -or $Verify) {
    $outLog = "$fieldRoot/field-$Size.stdout.log"; $errLog = "$fieldRoot/field-$Size.stderr.log"
    $process = Start-Process -FilePath $Godot -ArgumentList $fieldArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog
    $null = $process.Handle
    if (!$process.WaitForExit(60000)) { $process.Kill(); throw 'Company Operating Field timed out' }
    $process.Refresh()
    Get-Content -LiteralPath $outLog
    if ($process.ExitCode -ne 0 -or (Get-Item -LiteralPath $errLog).Length -gt 0) {
        Get-Content -LiteralPath $errLog; throw 'Company Operating Field capture/verification failed'
    }
} else {
    # Explicitly interactive launcher; production main_scene is unchanged.
    $process = Start-Process -FilePath $Godot -ArgumentList $fieldArgs -PassThru
    Write-Output "Company Operating Field opened (process $($process.Id), $Size)."
}
