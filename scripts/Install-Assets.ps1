$ErrorActionPreference = 'Stop'

Write-Host 'Installing FFmpeg asset...'
mise install
$ffmpegPath = (mise which ffmpeg).Trim()
$destinationPath = Join-Path $PSScriptRoot '../VideoCropper/Assets/ffmpeg.exe'

Copy-Item -LiteralPath $ffmpegPath -Destination $destinationPath -Force | Out-Null
Write-Host "FFmpeg installed: $destinationPath"

Write-Host 'Asset installation complete.'
