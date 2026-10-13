#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-Location (Join-Path $PSScriptRoot '..')

$testBuild = Join-Path $env:TEMP ('VideoCropper-regression-' + [guid]::NewGuid().ToString('N'))
dotnet build VideoCropper/VideoCropper.csproj --configuration Debug -p:Platform=x64 `
    -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None "-p:OutputPath=$testBuild/" -warnaserror --nologo
$env:PATH = "$testBuild;$env:PATH"
dotnet run --project Tests/DiagnosticsSmoke -p:TreatWarningsAsErrors=true
dotnet run --project Tests/CropRangeSmoke -p:TreatWarningsAsErrors=true -- VideoCropper/Assets/ffmpeg.exe samples
dotnet run --project Tests/StopSmoke -p:TreatWarningsAsErrors=true "-p:AppBuildDir=$testBuild" -- VideoCropper/Assets/ffmpeg.exe
