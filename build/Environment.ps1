$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
# Reuse installed pinned binaries, never qualification gameplay assemblies.
$env:DOTNET_ROOT = Join-Path $RepoRoot 'qualification/.tools/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $RepoRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $RepoRoot 'qualification/.tools/packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:NuGetAudit = 'false'
$Dotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
$Godot = Join-Path $RepoRoot 'qualification/.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
if ((& $Dotnet --version) -ne '10.0.401') { throw 'SDK pin mismatch' }
if ((& $Godot --version) -ne '4.7.2.stable.mono.official.ed1daf0bf') { throw 'Godot pin mismatch' }
