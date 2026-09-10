param([switch]$SkipTests)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES=Join-Path $env:USERPROFILE '.nuget\packages' }
if (-not $SkipTests) {
    dotnet run --project tests/Solitude.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Game verification failed.' }
    dotnet run --project tests/Solitude.UiChecks.csproj -c Release -p:OutputPath=../artifacts/ui-check-build/
    if ($LASTEXITCODE -ne 0) { throw 'Historical window and interaction verification failed.' }
}
[xml]$project=Get-Content -LiteralPath 'src/Solitude.csproj'
$version=$project.Project.PropertyGroup.Version
$release=Join-Path $PSScriptRoot ('artifacts/release-v'+$version.Replace('.',''))
dotnet publish src/Solitude.csproj -c Release -r win-x64 --self-contained true -p:OutputPath=../artifacts/package-build/ -o $release
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$files=@(Get-ChildItem -LiteralPath $release -File)
if($files.Count -ne 1 -or $files[0].Name -ne 'Solitude.exe'){throw 'Expected exactly one distributed EXE.'}
$dist=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$target=Join-Path $dist 'Solitude.exe'
$temporary=Join-Path $dist 'Solitude.exe.new'
Copy-Item -LiteralPath $files[0].FullName -Destination $temporary
if(Test-Path -LiteralPath $target){
    $backup=Join-Path $PSScriptRoot ('artifacts/Solitude-before-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.exe')
    try{[System.IO.File]::Replace($temporary,$target,$backup)}
    catch{throw "The verified new EXE is at $release. The dist copy could not be replaced; close that copy and rerun the build. $($_.Exception.Message)"}
}else{Move-Item -LiteralPath $temporary -Destination $target}
$releaseHash=(Get-FileHash -LiteralPath $files[0].FullName -Algorithm SHA256).Hash
$distHash=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if($releaseHash -ne $distHash){throw 'Delivered EXE differs from the retained release.'}
Get-Item -LiteralPath $target | Select-Object FullName,Length
Get-FileHash -LiteralPath $target -Algorithm SHA256
