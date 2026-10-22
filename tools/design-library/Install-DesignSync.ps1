# Installs a per-user scheduled task after an actual verified Drive upload.
# Run from an interactive Windows PowerShell session on the machine with rclone OAuth.
[CmdletBinding()]
param([string]$TaskName = "ManagementGameDesignDriveSync")

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$scriptPath = Join-Path $PSScriptRoot "asset_sync.py"
$statePath = Join-Path $repo "artifacts\design-library-sync\state.json"

$python = (Get-Command python.exe -ErrorAction Stop).Source
if (!(Get-Command rclone.exe -ErrorAction SilentlyContinue)) {
    throw "rclone not installed. Install rclone and run rclone config (remote: gdrive)."
}
& $python $scriptPath doctor
if ($LASTEXITCODE -ne 0) { throw "Design sync doctor failed" }

if (!(Test-Path $statePath)) {
    throw "Pilot not verified. Upload a test via: python tools/design-library/asset_sync.py once --apply"
}
$state = Get-Content $statePath -Raw | ConvertFrom-Json
$verified = @($state.records.PSObject.Properties | Where-Object { $_.Value.status -eq "VERIFIED" })
if ($verified.Count -eq 0) {
    throw "No checksum-verified Drive upload found. Run the pilot before installing automatic sync."
}

$arguments = ('"{0}" watch --apply --remove-verified --interval 8' -f $scriptPath)
$action = New-ScheduledTaskAction -Execute $python -Argument $arguments -WorkingDirectory $repo
$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Seconds 0) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description "Upload design-inbox images to Google Drive, MD5-verify, then remove only verified inbox copies" -Force | Out-Null
Write-Host "Installed scheduled task: $TaskName"
Write-Host "Images placed in $repo\artifacts\design-inbox\<collection> are synced automatically after next logon."
Write-Host "Run Start-ScheduledTask -TaskName '$TaskName' to start now."
