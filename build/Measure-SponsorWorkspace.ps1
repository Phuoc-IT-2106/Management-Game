param([string]$Output='artifacts/sponsor-workspace/acceptance/before', [ValidateSet('Debug','Release')][string[]]$Configurations=@('Debug','Release'))
. "$PSScriptRoot/Environment.ps1"
$root=[IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $root | Out-Null
$commit=(git -C $RepoRoot rev-parse HEAD).Trim()
$sources=Get-ChildItem "$RepoRoot/game/Client","$RepoRoot/src" -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\(obj|bin|\.godot)\\' } | Sort-Object FullName | ForEach-Object {
    $_.FullName.Substring($RepoRoot.Length+1).Replace('\','/')+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$sha=[Security.Cryptography.SHA256]::Create()
$digest=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($sources -join "`n")))).Replace('-','').ToLowerInvariant()
$sha.Dispose()
$sources | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $root 'source-files.txt')
foreach($configuration in $Configurations) {
    $restore=@('-p:RestoreLockedMode=true')
    # The SDK omits GodotSharpEditor outside Debug. Keep its separate lock in obj
    # instead of rewriting the checked-in Debug lock; package versions stay pinned.
    if($configuration -eq 'Release') { $restore=@('-p:RestoreLockedMode=false','-p:NuGetLockFilePath=obj/packages.Release.lock.json') }
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c $configuration -m:1 @restore > "$root/build-$configuration.log"
    if($LASTEXITCODE -ne 0) { Get-Content "$root/build-$configuration.log"; throw 'Build failed' }
    # Isolated native projects: Godot's editor engine loads the Debug location.
    # Stage the selected assembly there, then assert its real configuration in C#.
    $run=Join-Path $root $configuration
    $project=Join-Path $run 'game/Client'
    $assemblies=Join-Path $project '.godot/mono/temp/bin/Debug'
    New-Item -ItemType Directory -Force -Path $project,$assemblies,"$run/content" | Out-Null
    Copy-Item "$RepoRoot/game/Client/project.godot","$RepoRoot/game/Client/Main.cs","$RepoRoot/game/Client/Main.tscn" -Destination $project -Force
    Copy-Item "$RepoRoot/game/Client/UI" -Destination $project -Recurse -Force
    Copy-Item "$RepoRoot/content/fixture.*" -Destination "$run/content" -Force
    Copy-Item "$RepoRoot/game/Client/.godot/mono/temp/bin/$configuration/*" -Destination $assemblies -Recurse -Force
    $args=@('--path',('"'+$project+'"'),'--resolution','1280x720','--position','0,0','--audio-driver','Dummy',
        '--log-file',('"'+"$run/engine.log"+'"'),'res://UI/SponsorWorkspace/SponsorVerification.tscn','--',
        ('"--sponsor-performance='+$run+'"'),"--sponsor-configuration=$configuration","--sponsor-commit=$commit","--sponsor-source-digest=$digest")
    $process=Start-Process -FilePath $Godot -ArgumentList $args -PassThru -WindowStyle Hidden -RedirectStandardOutput "$run/stdout.log" -RedirectStandardError "$run/stderr.log"
    $null=$process.Handle
    $finished=$false
    for($attempt=0; $attempt -lt 6; $attempt++) {
        if($process.WaitForExit(30000)) { $finished=$true; break }
        Write-Output "Measuring $configuration..."
    }
    if(!$finished) { $process.Kill(); throw "Performance timeout $configuration" }
    $process.Refresh()
    if($process.ExitCode -ne 0 -or (Get-Item "$run/stderr.log").Length -gt 0) { Get-Content "$run/stdout.log"; Get-Content "$run/stderr.log"; throw 'Native measurement failed' }
    $data=Get-Content -Raw -Encoding utf8 "$run/performance.json" | ConvertFrom-Json
    if($data.Configuration -ne $configuration) { throw 'Loaded configuration mismatch' }
    Write-Output "$configuration PASS"
    $data.Summary | ConvertTo-Json
}
