$ErrorActionPreference = 'Stop'

$ffmpegPath = (mise which ffmpeg).Trim()
$destinationPath = Join-Path $PSScriptRoot '../VideoCropper/Assets/ffmpeg.exe'

$ffmpegPath
Copy-Item -LiteralPath $ffmpegPath -Destination $destinationPath -Force -PassThru
