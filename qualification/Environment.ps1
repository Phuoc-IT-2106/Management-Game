$ErrorActionPreference = 'Stop'
$QualificationRoot = $PSScriptRoot
$env:DOTNET_ROOT = Join-Path $QualificationRoot '.tools/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $QualificationRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $QualificationRoot '.tools/packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
# This offline fixture uses bundled Godot packages. Vulnerability auditing is
# not qualification evidence; the initial unavailable-audit warning is retained.
$env:NuGetAudit = 'false'
$env:APPDATA = Join-Path $QualificationRoot '.tools/appdata'
$env:LOCALAPPDATA = Join-Path $QualificationRoot '.tools/localappdata'
$Godot = Join-Path $QualificationRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64.exe'
$Dotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
$HostProject = Join-Path $QualificationRoot 'Host'
$Evidence = Join-Path $QualificationRoot '../docs/09-technical-foundation/qualification/evidence'
New-Item -ItemType Directory -Force -Path $env:APPDATA,$env:LOCALAPPDATA,$Evidence | Out-Null

function Invoke-Recorded {
    param([string]$Name, [string]$Executable, [string[]]$Arguments)
    if (-not (Test-Path -LiteralPath $Executable)) { throw "Executable missing: $Executable" }
    $log = Join-Path $Evidence "$Name.log"
    "COMMAND: $Executable $($Arguments -join ' ')" | Out-File -Encoding utf8 $log
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $Executable
    $start.Arguments = ($Arguments | ForEach-Object { '"' + $_.Replace('"','\"') + '"' }) -join ' '
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(600000)) {
        $process.Kill()
        throw "$Name exceeded ten-minute process timeout"
    }
    $stdout.Result | Out-File -Encoding utf8 -Append $log
    $stderr.Result | Out-File -Encoding utf8 -Append $log
    $code = $process.ExitCode
    $process.Dispose()
    "EXIT: $code" | Out-File -Encoding utf8 -Append $log
    Get-Content $log | Select-Object -Last 12
    if ($code -ne 0) { throw "$Name failed with exit $code (see $log)" }
}
