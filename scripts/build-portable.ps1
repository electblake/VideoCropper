#Requires -Version 7.4
param(
    [string]$OutputDirectory = 'ReleaseArtifacts/portable'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-Location (Join-Path $PSScriptRoot '..')

[xml]$manifest = Get-Content -LiteralPath 'VideoCropper/Package.appxmanifest' -Raw
$version = $manifest.Package.Identity.Version
$name = "VideoCropper_${version}_x64"
$output = New-Item -ItemType Directory -Path $OutputDirectory -Force
$payload = New-Item -ItemType Directory -Path (Join-Path $output.FullName $name)

dotnet publish VideoCropper/VideoCropper.csproj `
    --configuration Release --runtime win-x64 --self-contained true `
    -p:Platform=x64 -p:WindowsAppSDKSelfContained=true `
    -p:WindowsPackageType=None -p:PublishProfile= `
    --output $payload.FullName

$archive = Join-Path $output.FullName "$name.zip"
[System.IO.Compression.ZipFile]::CreateFromDirectory($payload.FullName, $archive)
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = Join-Path $output.FullName "$name.sha256"
Set-Content -LiteralPath $checksum -Value "$digest  $name.zip" -Encoding utf8NoBOM
Write-Output $archive
Write-Output $checksum
