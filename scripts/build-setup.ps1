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
& (Join-Path $PSScriptRoot 'build-portable.ps1') -OutputDirectory $OutputDirectory

[xml]$manifest = Get-Content -LiteralPath 'VideoCropper/Package.appxmanifest' -Raw
$version = $manifest.Package.Identity.Version
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$payload = Join-Path $output "VideoCropper_${version}_x64"
& $compiler "-dAppVersion=$version" "-dPayloadDir=$payload" "-dSetupOutput=$output" `
    (Join-Path $PSScriptRoot '../installer/VideoCropper.iss')

$name = "VideoCropper_${version}_x64_Setup.exe"
$setup = Join-Path $output $name
$digest = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = "$setup.sha256"
Set-Content -LiteralPath $checksum -Value "$digest  $name" -Encoding utf8NoBOM
Write-Output $setup
Write-Output $checksum
