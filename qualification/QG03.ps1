. "$PSScriptRoot/Environment.ps1"
$resultsDir = Join-Path $Evidence 'qg03'
New-Item -ItemType Directory -Force $resultsDir | Out-Null
foreach ($configuration in @('Debug','Release')) {
    Invoke-Recorded "qg03-build-$configuration" $Dotnet @('build', "$PSScriptRoot/Runner/Runner.csproj", '-c', $configuration, '-m:1')
    $runner = "$PSScriptRoot/Runner/bin/$configuration/net10.0/Runner.dll"
    Invoke-Recorded "qg03-tests-$configuration" $Dotnet @($runner,'--tests')
    foreach ($variant in @('default','varied')) {
        $options = @($runner,"--output=$resultsDir/runner-$configuration-$variant.json")
        if ($variant -eq 'varied') { $options += @('--tr','--log','--reverse') }
        Invoke-Recorded "qg03-runner-$configuration-$variant" $Dotnet $options
    }
}
Invoke-Recorded 'qg03-host-build' $Dotnet @('build', "$HostProject/Host.csproj", '-c', 'Debug', '-m:1')
foreach ($variant in @('default','varied')) {
    $options = @('--headless','--path',$HostProject,'--max-fps',$(if($variant -eq 'default') {'15'} else {'144'}),'--','--determinism',"--output=$resultsDir/host-Debug-$variant.json")
    if ($variant -eq 'varied') { $options += @('--tr','--log','--reverse') }
    Invoke-Recorded "qg03-host-Debug-$variant" $Godot $options
}
Invoke-Recorded 'qg03-export' $Godot @('--headless','--path',$HostProject,'--export-release','Windows x64',"$PSScriptRoot/artifacts/windows/Qualification.exe")
if (Select-String -Path "$Evidence/qg03-export.log" -Pattern 'ERROR: Export|ERROR: System\.|Failed to build|Failed to export') { throw 'Managed export failed' }
foreach ($variant in @('default','varied')) {
    # Rendered Release host at a different cap, versus headless Debug above.
    $options = @('--audio-driver','Dummy','--max-fps',$(if($variant -eq 'default') {'30'} else {'120'}),'--','--determinism',"--output=$resultsDir/host-Release-$variant.json")
    if ($variant -eq 'varied') { $options += @('--tr','--log','--reverse') }
    Invoke-Recorded "qg03-host-Release-$variant" "$PSScriptRoot/artifacts/windows/Qualification.exe" $options
}
$baseline=Get-Content -Raw "$resultsDir/runner-Debug-default.json" | ConvertFrom-Json
$comparison = foreach ($file in Get-ChildItem $resultsDir -Filter '*.json') {
    $result=Get-Content -Raw $file.FullName | ConvertFrom-Json
    if ($result.InitialHash -ne $baseline.InitialHash -or $result.FinalHash -ne $baseline.FinalHash -or $result.Hashes.Count -ne 32) { throw "Replay identity mismatch: $($file.Name)" }
    for ($i=0;$i -lt 32;$i++) { if ($result.Hashes[$i] -ne $baseline.Hashes[$i]) { throw "First divergence $($file.Name) transition $i" } }
    [pscustomobject]@{Run=$file.Name;Transitions=32;Match=$true;FinalHash=$result.FinalHash}
}
$comparison | ConvertTo-Json | Out-File -Encoding utf8 "$Evidence/qg03-comparison.json"
$comparison | Format-Table
