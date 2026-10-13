#Requires -Version 7.4
param(
    [string]$OutputDirectory = 'ReleaseArtifacts/setup'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-Location (Join-Path $PSScriptRoot '..')

# Restore the pinned build tool into NuGet's user cache; no system install or PATH entry.
$toolsProject = Join-Path $PSScriptRoot '../installer/Installer.Tools.csproj'
dotnet restore $toolsProject --locked-mode
$compiler = (dotnet msbuild $toolsProject -getProperty:InnoSetupCompiler).Trim()
& $compiler --version

[xml]$manifest = Get-Content -LiteralPath 'VideoCropper/Package.appxmanifest' -Raw
$version = $manifest.Package.Identity.Version
$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$payload = (New-Item -ItemType Directory -Path (Join-Path $output "VideoCropper_${version}_x64_setup-payload")).FullName
dotnet publish VideoCropper/VideoCropper.csproj `
    --configuration Release --runtime win-x64 --self-contained false `
    -p:Platform=x64 -p:WindowsAppSDKSelfContained=false -p:DownloadFFmpeg=true `
    -p:WindowsPackageType=None -p:PublishProfile= `
    --output $payload
& $compiler "-dAppVersion=$version" "-dPayloadDir=$payload" "-dSetupOutput=$output" `
    (Join-Path $PSScriptRoot '../installer/VideoCropper.iss')

$name = "VideoCropper_${version}_x64_Setup.exe"
$setup = Join-Path $output $name
$digest = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = "$setup.sha256"
Set-Content -LiteralPath $checksum -Value "$digest  $name" -Encoding utf8NoBOM
Write-Output $setup
Write-Output $checksum
