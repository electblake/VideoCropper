using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json;
using VideoCropper;

if (args.Length > 0 && args[0] == "profile")
{
    if (AppStorage.NoProfile)
    {
        var isolated = new SettingsStore(AppStorage.SettingsPath, _ => { });
        Check(!isolated.ContainsKey("Test.Marker"), "isolated profile starts fresh");
        isolated["Test.Marker"] = "isolated";
    }
    Console.WriteLine(JsonSerializer.Serialize(new { AppStorage.NoProfile, AppStorage.Root,
        AppStorage.SettingsPath, AppStorage.LogsFolder, AppStorage.CrashesFolder }));
    return;
}

void VerifyProfiles()
{
    var normalProfile = ReadProfile();
    Check(!normalProfile["NoProfile"]!.GetValue<bool>(), "profile isolation is opt-in");
    var expectedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoCropper");
    Check(normalProfile["Root"]!.GetValue<string>() == expectedRoot, "default profile location");
    var normalSettings = Path.Combine(expectedRoot, "settings.json");
    var beforeProfileTests = File.Exists(normalSettings) ? File.ReadAllBytes(normalSettings) : null;
    var isolatedProfile = ReadProfile("-NoProfile");
    var secondProfile = ReadProfile("-nOpRoFiLe");
    Check(isolatedProfile["Root"]!.GetValue<string>() != secondProfile["Root"]!.GetValue<string>(), "each isolated session gets a unique directory");
    foreach (var profile in new[] { isolatedProfile, secondProfile })
    {
        Check(profile["NoProfile"]!.GetValue<bool>(), "flag is case insensitive");
        var profileRoot = profile["Root"]!.GetValue<string>();
        Check(profileRoot.StartsWith(Path.Combine(Path.GetTempPath(), "VideoCropper") + Path.DirectorySeparatorChar), "isolated profile lives under temp");
        foreach (var key in new[] { "SettingsPath", "LogsFolder", "CrashesFolder" })
            Check(Path.GetDirectoryName(profile[key]!.GetValue<string>()) == profileRoot, key + " is isolated");
        Check(new SettingsStore(profile["SettingsPath"]!.GetValue<string>(), _ => { }).Get<string>("Test.Marker") == "isolated", "isolated settings can be saved");
    }
    Check(beforeProfileTests == null ? !File.Exists(normalSettings) : File.ReadAllBytes(normalSettings).SequenceEqual(beforeProfileTests), "normal settings remain untouched");
    Console.WriteLine("PASS: default profile, case-insensitive -NoProfile, fresh independent sessions, and normal settings unchanged.");
}

var root = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "VideoCropper-tests", Guid.NewGuid().ToString("N"));
var settingsPath = Path.Combine(root, "settings.json");
var diagnostics = new AppDiagnostics(Path.Combine(root, "Logs"), Path.Combine(root, "CrashReports"), settingsPath);

if (args.Length > 0 && args[0] == "crash")
{
    diagnostics.RegisterManagedExceptionHooks();
    diagnostics.Log("Last operation before deliberate crash");
    throw new InvalidOperationException("Deliberate smoke-test crash");
}
if (args.Length > 0 && args[0] == "invalid-settings")
{
    diagnostics.RegisterManagedExceptionHooks();
    _ = new SettingsStore(settingsPath, message => diagnostics.Log(message));
    return;
}

VerifyProfiles();
var store = new SettingsStore(settingsPath, message => diagnostics.Log(message));
var preview = new ProcessingPreviewPosition();
var duration = TimeSpan.FromMinutes(10);
Check(!preview.TryTake(TimeSpan.Zero, duration, out _), "no preview before encoder progress");
for (var i = 1; i <= 10000; i++) preview.Report(TimeSpan.FromMilliseconds(i));
Check(preview.TryTake(TimeSpan.Zero, duration, out var position) && position == TimeSpan.FromSeconds(10), "fast encoder progress coalesces to latest timestamp");
preview.Report(TimeSpan.FromSeconds(30));
Check(!preview.TryTake(TimeSpan.Zero, duration, out _), "no overlapping decoder seeks");
preview.CompleteSeek();
Check(preview.TryTake(TimeSpan.FromSeconds(10), duration, out position) && position == TimeSpan.FromSeconds(30), "slow seek skips stale progress");
preview.CompleteSeek();
Check(!preview.TryTake(TimeSpan.FromSeconds(10), duration, out _), "unchanged encoder progress does not repeat seeks");
preview.Report(TimeSpan.FromHours(1));
Check(preview.TryTake(TimeSpan.FromSeconds(30), duration, out position) && position < duration, "preview stays inside source duration");
preview.CompleteSeek();
preview.Reset();
Check(!preview.TryTake(TimeSpan.Zero, duration, out _), "next crop does not reuse old progress");
Console.WriteLine("PASS: preview coalescing, single in-flight seek, duplicate suppression, end bounds, and reset.");
store["Crop.AspectRatioPreset"] = "16:9";
store["Crop.PreviewWhileProcessing"] = false;
store["Crop.LockAspectRatio"] = true;
store["Crop.XPercent"] = 12.5;
store["HardwareAcceleration.GpuIndex"] = 2;
store.SetWindowSize(1400, 900);
store.SetCropState(12.5, 20, 50, 60, 3, "16:9", false);
var restored = new SettingsStore(settingsPath, _ => { });
Check(!(bool)restored["Crop.PreviewWhileProcessing"]!, "preview preference persists when disabled");
Check((string)restored["Crop.AspectRatioPreset"]! == "16:9", "string persistence");
Check((bool)restored["Crop.LockAspectRatio"]!, "boolean persistence");
Check((double)restored["Crop.XPercent"]! == 12.5, "fraction persistence");
Check((double)restored["Crop.YPercent"]! == 20 &&
      (double)restored["Crop.WidthPercent"]! == 50 &&
      (double)restored["Crop.HeightPercent"]! == 60 &&
      (int)restored["Crop.QuarterTurns"]! == 3 &&
      !(bool)restored["Crop.CenterPreset"]!, "crop geometry and rotation restore together");
store.SetCropState(12.5, 20, 50, 60, 0, null, true);
var customCrop = new SettingsStore(settingsPath, _ => { });
Check(!customCrop.ContainsKey("Crop.AspectRatioPreset") &&
      (int)customCrop["Crop.QuarterTurns"]! == 0 &&
      (bool)customCrop["Crop.CenterPreset"]!, "custom crop clears preset and persists rotation reset and centering");
Check((int)restored["HardwareAcceleration.GpuIndex"]! == 2, "integer persistence");
Check((int)restored["MainWindow.Width"]! == 1400 && (int)restored["MainWindow.Height"]! == 900, "window persistence");
store.Remove("Crop.AspectRatioPreset");
Check(!new SettingsStore(settingsPath, _ => { }).ContainsKey("Crop.AspectRatioPreset"), "removal persistence");
Check(Directory.GetFiles(root, "*.tmp").Length == 0, "atomic writes completed");

Parallel.For(0, 100, i => diagnostics.Log($"Concurrent marker {i:D3}"));
var log = File.ReadAllText(diagnostics.LogPath);
Check(log.Split('\n').Count(line => line.Contains("Concurrent marker")) == 100, "concurrent logs are immediately persisted");
var reportPath = diagnostics.Report("test report", new InvalidOperationException("Outer", new IOException("Inner")));
var report = File.ReadAllText(reportPath);
Check(report.Contains("Outer") && report.Contains("Inner") && report.Contains("Concurrent marker 099") && report.Contains("12.5"), "report includes exception chain, log, and settings");

RunFailure("crash", root, "Deliberate smoke-test crash", "Last operation before deliberate crash");
var invalidRoot = Path.Combine(root, "invalid");
Directory.CreateDirectory(invalidRoot);
File.WriteAllText(Path.Combine(invalidRoot, "settings.json"), "{invalid json");
RunFailure("invalid-settings", invalidRoot, "JsonReaderException", "{invalid json");
Check(File.ReadAllText(Path.Combine(invalidRoot, "settings.json")) == "{invalid json", "invalid configuration is not replaced");
Console.WriteLine($"PASS: settings round trips, atomic replacement, concurrent logging, report contents, real unhandled crash, invalid settings fail fast. Artifacts: {root}");

static void RunFailure(string mode, string root, string expectedError, string expectedContext)
{
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
    start.ArgumentList.Add(mode);
    start.ArgumentList.Add(root);
    using var child = Process.Start(start)!;
    var error = child.StandardError.ReadToEnd();
    child.WaitForExit();
    Check(child.ExitCode != 0 && error.Contains(expectedError), mode + " fails naturally");
    Check(Directory.GetFiles(Path.Combine(root, "CrashReports")).Select(File.ReadAllText)
        .Any(text => text.Contains("CLR unhandled exception") && text.Contains(expectedError) && text.Contains(expectedContext)), mode + " writes a complete crash report");
}

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("Failed: " + name);
}

static JsonObject ReadProfile(string? flag = null)
{
    var start = new ProcessStartInfo(Environment.ProcessPath!)
    { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
    start.ArgumentList.Add("profile");
    if (flag != null) start.ArgumentList.Add(flag);
    using var child = Process.Start(start)!;
    var output = child.StandardOutput.ReadToEnd();
    var error = child.StandardError.ReadToEnd();
    child.WaitForExit();
    Check(child.ExitCode == 0, "profile child: " + error);
    return JsonNode.Parse(output)!.AsObject();
}
