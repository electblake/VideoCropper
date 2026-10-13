using System.Diagnostics;
using System.Globalization;
using VideoCropperPage;

var ffmpeg = Path.GetFullPath(args[0]);
var root = Path.Combine(Path.GetTempPath(), "VideoCropper-range-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var raw = Path.Combine(root, "source.gray");
var source = Path.Combine(root, "source.mkv");
// Each frame has a distinct brightness, so a correct duration alone cannot pass.
File.WriteAllBytes(raw, Enumerable.Range(0, 80)
    .SelectMany(frame => Enumerable.Repeat((byte)(20 + frame * 2), 64 * 64)).ToArray());
Run($"-f rawvideo -pixel_format gray -video_size 64x64 -framerate 10 -i \"{raw}\" -f lavfi -i sine=frequency=440:sample_rate=48000:duration=8 -c:v ffv1 -c:a pcm_s16le -shortest \"{source}\"");

Verify("middle", new(TimeSpan.FromSeconds(2.3), TimeSpan.FromSeconds(5.7)), 23, 34);
Verify("start", new(TimeSpan.Zero, TimeSpan.FromSeconds(1.5)), 0, 15);
Verify("end", new(TimeSpan.FromSeconds(6.5), TimeSpan.FromSeconds(8)), 65, 15);
Verify("full", new(TimeSpan.Zero, TimeSpan.FromSeconds(8)), 0, 80);
Console.WriteLine($"PASS: actual FFmpeg in/out frame content, frame counts, audio duration, and preview source offsets. Artifacts: {root}");

void Verify(string name, CropTimeRange range, int firstFrame, int frameCount)
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    var output = Path.Combine(root, name + ".mkv");
    var decoded = Path.Combine(root, name + ".gray");
    var audio = Path.Combine(root, name + ".pcm");
    Run($"{range.InputArguments} -i \"{source}\" -c:v libx265 -crf 18 -c:a copy -fps_mode passthrough {range.OutputArguments} -vf \"crop=32:32:0:0\" \"{output}\"");
    Run($"-i \"{output}\" -map 0:v:0 -fps_mode passthrough -pix_fmt gray -f rawvideo \"{decoded}\"");
    Run($"-i \"{output}\" -map 0:a:0 -f s16le -acodec pcm_s16le \"{audio}\"");
    var frames = File.ReadAllBytes(decoded);
    Check(frames.Length == frameCount * 32 * 32, name + " frame count");
    Check(Math.Abs(frames[0] - (20 + firstFrame * 2)) <= 4, name + " first frame matches in point");
    Check(Math.Abs(frames[^1] - (20 + (firstFrame + frameCount - 1) * 2)) <= 4, name + " last frame precedes out point");
    var audioSeconds = new FileInfo(audio).Length / (48000.0 * 2);
    Check(Math.Abs(audioSeconds - range.Duration.TotalSeconds) < 0.05, name + " audio duration");
    Check(range.SourcePosition(TimeSpan.Zero) == range.In, name + " preview begins at in point");
    Check(range.SourcePosition(range.Duration / 2) == range.In + range.Duration / 2, name + " preview maps output time to source");
    Check(range.SourcePosition(range.Duration + TimeSpan.FromSeconds(1)) < range.Out, name + " preview stays before out point");
}

void Run(string arguments)
{
    using var process = Process.Start(new ProcessStartInfo(ffmpeg, "-hide_banner -loglevel error -y " + arguments)
    {
        UseShellExecute = false,
        RedirectStandardError = true,
        CreateNoWindow = true
    })!;
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    Check(process.ExitCode == 0, arguments + Environment.NewLine + error);
}

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
