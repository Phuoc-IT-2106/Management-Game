param([string]$Output = 'artifacts/shell/candidates', [switch]$NoBuild)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }
}
$captureRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot $Output))
New-Item -ItemType Directory -Force -Path $captureRoot | Out-Null
# Window captures need an interactive desktop session.
foreach ($size in '1280x720','1920x1080') {
    $saves = Join-Path $captureRoot ("saves-" + [Guid]::NewGuid().ToString('N'))
    $captureArgs = @('--path',('"'+"$RepoRoot/game/Client"+'"'),'--resolution',$size,'--position','0,0','--audio-driver','Dummy','res://UI/Shell/GameShell.tscn','--',
        "--shell-viewport=$size",('"--shell-output='+$captureRoot+'"'),('"--saves='+$saves+'"'),('"--content='+"$RepoRoot/content/fixture.json"+'"'))
    $stdout = Join-Path $captureRoot "$size.stdout.log"; $stderr = Join-Path $captureRoot "$size.stderr.log"
    $process = Start-Process -FilePath $Godot -ArgumentList $captureArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $null = $process.Handle
    if (!$process.WaitForExit(90000)) { $process.Kill(); throw "Shell capture timeout $size" }
    $process.Refresh()
    if ($process.ExitCode -ne 0 -or (Get-Item $stderr).Length -gt 0) { Get-Content $stderr; throw "Shell capture failed $size" }
    Write-Output "PASS shell captures $size"
}
