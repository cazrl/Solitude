$ErrorActionPreference = 'Stop'
$pinRoot = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Building Pinball requires Visual Studio 2022 C++ Build Tools and a Windows SDK.' }
$start=[Diagnostics.ProcessStartInfo]::new($msbuild)
$start.UseShellExecute=$false
$start.CreateNoWindow=$true
$start.RedirectStandardOutput=$true
$start.RedirectStandardError=$true
foreach($arg in @((Join-Path $pinRoot 'third_party/SpaceCadetPinball/Solitude.Pinball.vcxproj'),'/p:Configuration=Release','/p:Platform=x64','/nologo','/verbosity:minimal')) {$start.ArgumentList.Add($arg)}
# Some host environments contain both PATH and Path. Desktop MSBuild rejects those.
$clean=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($entry in [Environment]::GetEnvironmentVariables().GetEnumerator()) {$clean[$entry.Key]=$entry.Value}
$start.Environment.Clear()
foreach($entry in $clean.GetEnumerator()) {$start.Environment[$entry.Key]=$entry.Value}
$build=[Diagnostics.Process]::Start($start)
$stdout=$build.StandardOutput.ReadToEndAsync()
$stderr=$build.StandardError.ReadToEndAsync()
$build.WaitForExit()
Write-Output $stdout.Result
Write-Output $stderr.Result
if ($build.ExitCode -ne 0) { throw 'Pinball native build failed.' }
