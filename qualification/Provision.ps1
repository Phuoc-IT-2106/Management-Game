$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$toolsDir = Join-Path $root '.tools'
New-Item -ItemType Directory -Force $toolsDir | Out-Null
$downloads = @(
    @('godot.zip','https://downloads.godotengine.org/?version=4.7.2&flavor=stable&slug=mono_win64.zip&platform=windows.64','SHA256','A2A48473A7414C5F19FAB690518CAEBB738C09EF9601F6BD2388676A7F53B3C0'),
    @('templates.tpz','https://downloads.godotengine.org/?version=4.7.2&flavor=stable&slug=mono_export_templates.tpz&platform=templates','SHA256','92F8681E349EF1F90891B792DA95E3B2B0BD1ED610B78018C58FEB2D87E15A9D'),
    @('dotnet.zip','https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip','SHA512','24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430')
)
foreach ($download in $downloads) {
    $file = Join-Path $toolsDir $download[0]
    if (-not (Test-Path $file)) {
        & curl.exe -fL --max-time 300 -o $file $download[1]
        if ($LASTEXITCODE -ne 0) { throw "Download failed: $($download[0])" }
    }
    if ((Get-FileHash $file -Algorithm $download[2]).Hash -ne $download[3]) { throw "Hash mismatch: $file" }
}
if (-not (Test-Path "$toolsDir/godot")) { Expand-Archive "$toolsDir/godot.zip" "$toolsDir/godot" }
if (-not (Test-Path "$toolsDir/dotnet")) { Expand-Archive "$toolsDir/dotnet.zip" "$toolsDir/dotnet" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead("$toolsDir/templates.tpz")
try {
    foreach ($entry in $archive.Entries) {
        if ($entry.FullName -match '^templates/(windows_(debug|release)_x86_64(_console)?\.exe|version\.txt)$') {
            $destination = Join-Path "$toolsDir/templates" $entry.FullName
            New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
            if (-not (Test-Path $destination)) { [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$destination) }
        }
    }
} finally { $archive.Dispose() }
Write-Output 'Qualification archives verified and extracted. No machine-wide installation.'
