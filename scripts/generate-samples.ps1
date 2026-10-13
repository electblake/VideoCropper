#requires -Version 7.4
param(
    [string]$Ffmpeg = "$PSScriptRoot/../VideoCropper/Assets/ffmpeg.exe"
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$sampleDirectory = Join-Path $PSScriptRoot '../samples'
New-Item -ItemType Directory -Force -Path $sampleDirectory | Out-Null

# Exact source aspect ratios with even dimensions and chroma-aligned crop offsets.
$samples = @(
    @{ Name = 'portrait-in-hd.mp4'; Width = 1920; Height = 1080; ContentWidth = 576; ContentHeight = 1024 },
    @{ Name = 'portrait-in-ultrawide.mkv'; Width = 2560; Height = 1080; ContentWidth = 576; ContentHeight = 1024 },
    @{ Name = 'landscape-in-portrait.mp4'; Width = 1080; Height = 1920; ContentWidth = 1024; ContentHeight = 576 },
    @{ Name = 'ultrawide-in-portrait.mov'; Width = 1080; Height = 1920; ContentWidth = 1024; ContentHeight = 432 }
)
foreach ($sample in $samples) {
    $filter = "[0:v][1:v]overlay=x='mod(t*120,main_w-overlay_w)':y=main_h*0.7:shortest=1,pad=$($sample.Width):$($sample.Height):(ow-iw)/2:(oh-ih)/2:black,setsar=1[v]"
    & $Ffmpeg -hide_banner -loglevel error -y `
        -f lavfi -i "smptebars=size=$($sample.ContentWidth)x$($sample.ContentHeight):rate=24:duration=4" `
        -f lavfi -i 'color=white:size=32x32:rate=24:duration=4' `
        -filter_complex_threads 1 -filter_complex $filter -map '[v]' -an -c:v libx264 -threads 2 -preset slow -crf 18 -pix_fmt yuv420p `
        (Join-Path $sampleDirectory $sample.Name)
}
