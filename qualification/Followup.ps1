param(
    [Parameter(Mandatory=$true)][ValidateSet('Export','Profile','Sustained','Layout','Smoke','Regression','Manifest')][string]$Mode,
    [ValidatePattern('^[a-z0-9-]+$')][string]$Name='candidate',
    [switch]$Light,
    [switch]$RebuildRows,
    [switch]$NoVsync
)
. "$PSScriptRoot/Environment.ps1"
$output = Join-Path $QualificationRoot 'artifacts/followup'
$exe = Join-Path $output 'Qualification.exe'
$data = "$env:APPDATA/Godot/app_userdata/Phase 4B Foundation Qualification"
$prefix = "followup-$Name"
switch ($Mode) {
    'Export' {
        New-Item -ItemType Directory -Force $output | Out-Null
        Invoke-Recorded "$prefix-export" $Godot @('--headless','--path',$HostProject,'--export-release','Windows x64',$exe)
        if (Select-String -Path "$Evidence/$prefix-export.log" -Pattern 'ERROR: Export|ERROR: System\.|Failed to build|Failed to export|Project export.*failed') { throw 'Managed export failed' }
        if (-not (Test-Path "$output/data_Host_windows_x86_64/Host.dll")) { throw 'Managed export missing' }
    }
    'Profile' {
        $destination="$Evidence/$prefix.json"
        if (Test-Path $destination) { throw "Evidence exists: $destination; choose a new Name" }
        $arguments=@('--audio-driver','Dummy','--','--ui','--profile',"--profile-output=$destination")
        if($Light) { $arguments+='--profile-light' }
        if($RebuildRows) { $arguments+='--rebuild-rows' }
        if($NoVsync) { $arguments+='--profile-no-vsync' }
        Invoke-Recorded $prefix $exe $arguments
        if (-not (Select-String -Path "$Evidence/$prefix.log" -SimpleMatch 'QG02_PROFILE_COMPLETE')) { throw 'Incomplete profile' }
        $result=Get-Content -Raw $destination | ConvertFrom-Json
        if($result.debugBuild -or $result.results.Count -ne 24) { throw 'Expected Release and 24 workflow results' }
    }
    'Sustained' {
        Invoke-Recorded $prefix $exe @('--audio-driver','Dummy','--','--ui','--benchmark')
        if (-not (Select-String -Path "$Evidence/$prefix.log" -SimpleMatch 'QG02_CHECKS_PASS 51')) { throw 'Incomplete correctness run' }
        Copy-Item "$data/qg02.json" "$Evidence/$prefix.json"
        Copy-Item "$data/qg02-render.png" "$Evidence/$prefix.png"
    }
    'Layout' {
        Invoke-Recorded $prefix $exe @('--audio-driver','Dummy','--','--ui','--layout-check')
        if (-not (Select-String -Path "$Evidence/$prefix.log" -SimpleMatch 'QG02_LAYOUT_PASS 84')) { throw 'Incomplete layout run' }
        Copy-Item "$data/qg02-layout-1280-2.png" "$Evidence/$prefix-1280-2.png"
    }
    'Smoke' {
        Invoke-Recorded $prefix $exe @('--audio-driver','Dummy','--','--smoke','--capture')
        if (-not (Select-String -Path "$Evidence/$prefix.log" -SimpleMatch 'QG01_INTERACTION_PASS')) { throw 'Smoke action missing' }
        Copy-Item "$data/qg01-render.png" "$Evidence/$prefix.png"
    }
    'Regression' {
        $destination="$Evidence/$prefix.json"
        Invoke-Recorded $prefix $exe @('--audio-driver','Dummy','--max-fps','30','--','--determinism',"--output=$destination")
        $actual=Get-Content -Raw $destination | ConvertFrom-Json
        $expected=Get-Content -Raw "$Evidence/qg03/host-Release-default.json" | ConvertFrom-Json
        if($actual.InitialHash -ne $expected.InitialHash -or $actual.FinalHash -ne $expected.FinalHash -or $actual.Hashes.Count -ne 32) { throw 'Replay identity changed' }
        for($i=0;$i -lt 32;$i++) { if($actual.Hashes[$i] -ne $expected.Hashes[$i]) { throw "Replay differs at $i" } }
        'REGRESSION_PASS: initial, all 32 transition hashes and final match retained QG-03 Release host.' | Out-File -Append -Encoding utf8 "$Evidence/$prefix.log"
    }
    'Manifest' {
        Get-ChildItem $output -Recurse -File | Sort-Object FullName | ForEach-Object {
            [pscustomobject]@{Path=$_.FullName.Substring($output.Length+1);Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
        } | ConvertTo-Json | Out-File -Encoding utf8 "$Evidence/$prefix-manifest.json"
        Get-ChildItem $HostProject -File | Where-Object Extension -In '.cs','.csproj','.godot','.cfg' | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{Path=$_.Name;SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
        } | ConvertTo-Json | Out-File -Encoding utf8 "$Evidence/$prefix-source.json"
    }
}
