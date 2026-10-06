param([switch]$NoBuild)
. "$PSScriptRoot/Environment.ps1"
$shellRoot = Join-Path $RepoRoot 'artifacts/shell'
New-Item -ItemType Directory -Force -Path $shellRoot | Out-Null
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }
}
foreach ($size in '1280x720','1920x1080') {
    $saves = Join-Path $shellRoot ("verify-saves-" + [Guid]::NewGuid().ToString('N'))
    & $Godot --headless --path "$RepoRoot/game/Client" --audio-driver Dummy --log-file "$shellRoot/verify-$size.engine.log" res://UI/Shell/GameShell.tscn -- --shell-verify "--shell-viewport=$size" "--saves=$saves" "--content=$RepoRoot/content/fixture.json" > "$shellRoot/verify-$size.log" 2> "$shellRoot/verify-$size.stderr.log"
    if ($LASTEXITCODE -ne 0 -or (Get-Item "$shellRoot/verify-$size.stderr.log").Length -gt 0) { Get-Content "$shellRoot/verify-$size.stderr.log"; throw "Shell verification failed at $size" }
    Get-Content "$shellRoot/verify-$size.log" -Tail 1
}
