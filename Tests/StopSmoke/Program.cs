using System.Diagnostics;
using FileLogger;
using WinUIShared.Helpers;

var ffmpeg = Path.GetFullPath(args[0]);
var root = Path.Combine(Path.GetTempPath(), "VideoCropper-stop-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var processor = new TestProcessor(ffmpeg);
foreach (var mode in new[] { "stop", "paused-stop", "cancel", "paused-cancel", "resume", "complete" })
{
    var output = Path.Combine(root, mode + ".mp4");
    var complete = mode is "complete" or "resume";
    var encoding = processor.Encode(output, complete ? 3 : 30);
    await processor.FramesWritten.Task.WaitAsync(TimeSpan.FromSeconds(20));
    if (mode is "paused-stop" or "paused-cancel" or "resume")
    {
        processor.Pause();
        await Task.Delay(300);
        Check(!encoding.IsCompleted, mode + " remains active while paused");
    }
    if (mode == "resume") processor.Resume();
    if (mode is "stop" or "paused-stop") processor.Stop();
    if (mode is "cancel" or "paused-cancel") await processor.Cancel();
    var success = await encoding.WaitAsync(TimeSpan.FromSeconds(20));
    if (mode is "cancel" or "paused-cancel")
    {
        Check(!success, mode + " did not complete successfully");
        Check(!File.Exists(output), "Cancel deletes partial output");
        Console.WriteLine($"PASS: {mode}, partial output deleted");
        continue;
    }
    Check(success, mode + " exits successfully");
    Check(processor.StopRequested == !complete, mode + " stop state");
    var decoded = Path.Combine(root, mode + ".gray");
    using var decoder = Process.Start(new ProcessStartInfo(ffmpeg,
        $"-v error -xerror -i \"{output}\" -map 0:v:0 -pix_fmt gray -fps_mode passthrough -f rawvideo \"{decoded}\"")
    { UseShellExecute = false, CreateNoWindow = true })!;
    await decoder.WaitForExitAsync();
    Check(decoder.ExitCode == 0, mode + " output fully decodes");
    var frames = new FileInfo(decoded).Length / (160 * 90);
    Check(complete ? frames == 90 : frames > 0 && frames < 900,
        mode + " contains the expected complete/partial frame count");
    Console.WriteLine($"PASS: {mode}, {frames} playable frames");
}
Console.WriteLine($"PASS: stop, paused stop/cancel, resume, destructive cancel, and normal completion. Artifacts: {root}");

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

sealed class TestProcessor(string ffmpeg) : Processor(ffmpeg, new TestLogger())
{
    public TaskCompletionSource FramesWritten { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<bool> Encode(string output, int seconds)
    {
        StopRequested = false;
        gracefulStopEnabled = true;
        outputFile = output;
        FramesWritten = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return StartFfmpegProcess(
            $"-y -re -f lavfi -i testsrc2=size=160x90:rate=30 -t {seconds} -c:v libx265 -threads 2 -x265-params pools=2 -preset ultrafast -nostats -progress pipe:2 \"{output}\"",
            (_, e) =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(e.Data ?? "", @"^frame=(\d+)$");
                if (match.Success && int.Parse(match.Groups[1].Value) >= 30)
                    FramesWritten.TrySetResult();
            });
    }
}

sealed class TestLogger : IFileLogger
{
    public void Activate(bool activate) { }
    public void Log(params object?[] messages) { }
}
