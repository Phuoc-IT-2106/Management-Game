param([ValidateSet('normal','sponsor','competitive','quiet','empty','long-name','branding','missing-identity','list','decision','return','affairs')][string]$Case = 'normal', [switch]$NoBuild)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'Operating Map build failed' }
}
& $Godot --path "$RepoRoot/game/Client" --audio-driver Dummy res://UI/OperatingMap/OperatingMap.tscn -- "--map-case=$Case"
if ($LASTEXITCODE -ne 0) { throw 'Operating Map failed' }
