param([string]$Output='artifacts/sponsor-workspace/candidates', [switch]$Quick, [switch]$NoBuild)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if($LASTEXITCODE -ne 0) { throw 'Client build failed' }
}
$captureRoot=[IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $captureRoot | Out-Null
$commit=(git -C $RepoRoot rev-parse HEAD).Trim()
$sources=Get-ChildItem "$RepoRoot/game/Client/UI","$RepoRoot/src" -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring($RepoRoot.Length+1).Replace('\','/')+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$sha=[Security.Cryptography.SHA256]::Create()
$digest=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($sources -join "`n")))).Replace('-','').ToLowerInvariant()
$sha.Dispose()
$sources | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $captureRoot 'source-files.txt')
$cases=@(@('review','1280x720'),@('confirmation','1280x720'),@('success','1280x720'),@('stale','1280x720'),@('long','1280x720'),@('review','1920x1080'),@('confirmation','1920x1080'))
if($Quick) { $cases=,@('review','1280x720') }
foreach($case in $cases) {
    $scenario=$case[0]; $size=$case[1]; $key="$scenario-$size"
    $stdout=Join-Path $captureRoot "$key.stdout.log"; $stderr=Join-Path $captureRoot "$key.stderr.log"
    $captureArgs=@('--path',('"'+"$RepoRoot/game/Client"+'"'),'--resolution',$size,'--position','0,0','--audio-driver','Dummy',
        '--log-file',('"'+(Join-Path $captureRoot "$key.engine.log")+'"'),'res://UI/SponsorWorkspace/SponsorVerification.tscn','--',
        "--sponsor-case=$scenario","--sponsor-viewport=$size",('"--sponsor-output='+$captureRoot+'"'),"--sponsor-commit=$commit","--sponsor-source-digest=$digest")
    if($scenario -eq 'review') { $captureArgs+='--sponsor-verify' }
    $process=Start-Process -FilePath $Godot -ArgumentList $captureArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $null=$process.Handle
    if(!$process.WaitForExit(60000)) { $process.Kill(); throw "Capture timeout $key" }
    $process.Refresh()
    if($process.ExitCode -ne 0 -or (Get-Item -LiteralPath $stderr).Length -gt 0) { Get-Content $stdout; Get-Content $stderr; throw "Capture failed $key" }
    $manifest=Get-Content -Raw -LiteralPath (Join-Path $captureRoot "$key.json") | ConvertFrom-Json
    if("$($manifest.Viewport.Width)x$($manifest.Viewport.Height)" -ne $size) { throw 'Wrong render dimensions' }
    Write-Output "PASS $key"
}
Write-Output "SPONSOR_CAPTURE_PASS $($cases.Count) source=$digest"
