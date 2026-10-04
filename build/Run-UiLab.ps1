param(
    [ValidateSet('identity','interaction','documents','branding')][string]$Page = 'identity',
    [ValidateSet('neutral','bright','dark','warning','low-contrast','identical','missing-emblem')][string]$Brand = 'neutral',
    [ValidateSet('Compact','Default','Comfortable')][string]$Density = 'Default',
    [ValidateRange(1,1.5)][double]$TextScale = 1,
    [switch]$NoBuild
)
. "$PSScriptRoot/Environment.ps1"
if (!$NoBuild) {
    & $Dotnet build "$RepoRoot/game/Client/Client.csproj" -c Debug -m:1 -p:RestoreLockedMode=true
    if ($LASTEXITCODE -ne 0) { throw 'UI Lab build failed' }
}
$scale = $TextScale.ToString('0.##', [Globalization.CultureInfo]::InvariantCulture)
# An explicitly interactive developer launcher. Does not change project.godot's main scene.
& $Godot --path "$RepoRoot/game/Client" --resolution 1280x720 --audio-driver Dummy res://UI/Lab/UiLab.tscn -- "--lab-page=$Page" "--lab-brand=$Brand" "--lab-density=$Density" "--lab-text-scale=$scale"
exit $LASTEXITCODE
