using System.Diagnostics;
using VideoCropperPage;

if (args[0] == "-y")
{
    Console.Error.WriteLine("x265 [error]: memory allocation failure, aborting encode");
    Environment.Exit(-12);
}

var ffmpeg = Path.GetFullPath(args[0]);
var root = Path.Combine(Path.GetTempPath(), "VideoCropper-failure-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var source = Path.Combine(root, "source.mp4");
using var generator = Process.Start(new ProcessStartInfo(ffmpeg,
    $"-v error -f lavfi -i testsrc2=size=640x360:rate=30 -t 6 -c:v libx264 \"{source}\"")
{ UseShellExecute = false, CreateNoWindow = true })!;
await generator.WaitForExitAsync();
Check(generator.ExitCode == 0, "Source generation");
var crop = new CropProcessor(ffmpeg);
var errors = new List<string>();
var progress = new List<double>();
crop.SetProgressAndErrorReporters(progressPrimary: new InlineProgress(progress), error: errors.Add);
// Invalid expression fails before FFmpeg emits its usual 'Conversion failed!' line.
await crop.Crop(source, ffmpeg, "0", "0", "invalid_width", "180", false, 0, TimeSpan.Zero, TimeSpan.FromSeconds(6));
Check(!crop.Succeeded, "Failed encoding cannot be successful");
Check(errors.Count > 0, "Failed encoding reaches the process manager's error reporter");
Check(!progress.Contains(100), "Failed encoding cannot report completion");
errors.Clear();
progress.Clear();
var memoryCrop = new CropProcessor(Environment.ProcessPath!);
memoryCrop.SetProgressAndErrorReporters(progressPrimary: new InlineProgress(progress), error: errors.Add);
await memoryCrop.Crop(source, Environment.ProcessPath!, "0", "0", "320", "180", false, 0, TimeSpan.Zero, TimeSpan.FromSeconds(6));
Check(memoryCrop.MemoryExhausted && !memoryCrop.Succeeded, "Memory failure never completes the crop");
Check(errors.Single().Contains("could not allocate memory"), "Memory failure reports a known, actionable message");
Check(errors.Single().Contains("Exit code: -12") && errors.Single().Contains("memory allocation failure"), "Memory failure includes encoder diagnostics");
Check(!progress.Contains(100), "Memory failure never reports 100 percent");
errors.Clear();
progress.Clear();
await crop.Crop(source, ffmpeg, "0", "0", "320", "180", false, 0, TimeSpan.Zero, TimeSpan.FromSeconds(6));
Check(crop.Succeeded && errors.Count == 0, "A new CPU crop succeeds after failure");
Check(progress.Any(value => value > 0 && value < 100), "CPU crop reports intermediate progress");
Check(progress.Last() == 100, "Successful crop reports completion");
Console.WriteLine("PASS: failed crop reports failure, no false completion, subsequent CPU crop reports progress and completes.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class InlineProgress(List<double> values) : IProgress<double>
{
    public void Report(double value) => values.Add(value);
}

namespace VideoCropper
{
    internal static class App
    {
        internal static TestDiagnostics Diagnostics { get; } = new();
    }

    internal sealed class TestDiagnostics : FileLogger.IFileLogger
    {
        public void Activate(bool activate) { }
        public void Log(params object?[] messages) { }
        public void Report(string source, object detail) { }
    }
}
