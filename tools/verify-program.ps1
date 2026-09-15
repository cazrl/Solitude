param([string]$OutputRoot = 'artifacts/verification', [switch]$Focused)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repository
$output = [IO.Path]::GetFullPath((Join-Path $repository $OutputRoot))
if (-not $output.StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Verification output must stay inside the repository.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable failed with exit code $LASTEXITCODE" }
}
$ui = Join-Path $output 'ui'
$package = Join-Path $output 'package'
Invoke-Checked dotnet @('run', '--project', 'tests/Solitude.Tests.csproj', '-c', 'Release')
Invoke-Checked dotnet @('build', 'tests/Solitude.UiChecks.csproj', '-c', 'Release', '-o', $ui)
$uiExe = Join-Path $ui 'Solitude.UiChecks.exe'
if ($Focused) { Invoke-Checked $uiExe @('--program-fixes-only') } else { Invoke-Checked $uiExe @() }
Invoke-Checked $uiExe @('--orbit-native-only')
Invoke-Checked dotnet @('publish', 'src/Solitude.csproj', '-c', 'Release', '--no-restore', '-o', $package)
Invoke-Checked dotnet @('build','tests/Solitude.PinballChecks.csproj','-c','Release','-o',(Join-Path $output 'pinball-check-app'))
Invoke-Checked (Join-Path $output 'pinball-check-app/Solitude.PinballChecks.exe') @((Join-Path $output 'pinball-checks'),(Join-Path $package 'Solitude.exe'))
function Render-Application([string]$Executable, [string]$Directory) {
    $render = Start-Process -FilePath $Executable -ArgumentList @('--render', ('"' + $Directory + '"'), '--data-dir', ('"' + (Join-Path $output 'isolated-state') + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($render.ExitCode -ne 0) { throw "Render failed: $Executable" }
}
$sourceFrames = Join-Path $output 'source-render'
$packageFrames = Join-Path $output 'package-render'
Render-Application (Join-Path $ui 'Solitude.exe') $sourceFrames
Render-Application (Join-Path $package 'Solitude.exe') $packageFrames
$sourceImages = @(Get-ChildItem -LiteralPath $sourceFrames -Filter '*.png' -File -Recurse)
$packageImages = @(Get-ChildItem -LiteralPath $packageFrames -Filter '*.png' -File -Recurse)
if ($sourceImages.Count -eq 0 -or $sourceImages.Count -ne $packageImages.Count) { throw 'Render image counts differ or are empty.' }
foreach ($source in $sourceImages) {
    $relative = [IO.Path]::GetRelativePath($sourceFrames, $source.FullName)
    $published = Join-Path $packageFrames $relative
    if (-not (Test-Path -LiteralPath $published) -or (Get-FileHash -LiteralPath $source.FullName).Hash -ne (Get-FileHash -LiteralPath $published).Hash) { throw "Package differs from source: $relative" }
}
$exe = Get-Item -LiteralPath (Join-Path $package 'Solitude.exe')
[ordered]@{version=$exe.VersionInfo.FileVersion;bytes=$exe.Length;sha256=(Get-FileHash -LiteralPath $exe.FullName).Hash;matchingImages=$sourceImages.Count;scope= $(if($Focused){'focused plus engine and native'}else{'full UI, engine and native'})} | ConvertTo-Json | Set-Content (Join-Path $output 'verification.json')
Write-Output "Verified $($exe.VersionInfo.FileVersion): $($sourceImages.Count) matching source/package images."
