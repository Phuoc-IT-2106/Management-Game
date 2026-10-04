. "$PSScriptRoot/Environment.ps1"
Invoke-Recorded 'qg04-build' $Dotnet @('build', "$PSScriptRoot/SaveTests/SaveTests.csproj", '-c', 'Release', '-m:1')
$run = Join-Path "$PSScriptRoot/artifacts" ('save-tests-' + [Guid]::NewGuid().ToString('N'))
Invoke-Recorded 'qg04-tests' $Dotnet @("$PSScriptRoot/SaveTests/bin/Release/net10.0/SaveTests.dll",$run)
if (-not (Select-String -Path "$Evidence/qg04-tests.log" -SimpleMatch 'QG04_TESTS_PASS')) { throw 'Save tests did not complete' }
Copy-Item "$run/results.json" "$Evidence/qg04-results.json"
Get-ChildItem $run -Recurse -File | ForEach-Object {
    [pscustomobject]@{Path=$_.FullName.Substring($run.Length+1);Bytes=$_.Length;SHA256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}
} | ConvertTo-Json | Out-File -Encoding utf8 "$Evidence/qg04-fixture-manifest.json"
