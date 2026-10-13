using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VideoCropper;

internal static class AppStorage
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoCropper");
    public static string SettingsPath => Path.Combine(Root, "settings.json");
    public static string LogsFolder => Path.Combine(Root, "Logs");
    public static string CrashesFolder => Path.Combine(Root, "CrashReports");
}

internal sealed class SettingsStore
{
    private readonly string path;
    private readonly JsonObject values;
    private readonly Action<string> log;
    private readonly object gate = new();

    public SettingsStore(string path, Action<string> log)
    {
        this.path = path;
        this.log = log;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) File.WriteAllText(path, "{}"); // First launch.
        values = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    public bool ContainsKey(string key)
    {
        lock (gate) return values.ContainsKey(key);
    }

    public JsonNode? this[string key]
    {
        get { lock (gate) return values[key]; }
        set
        {
            lock (gate)
            {
                values[key] = value;
                Save();
            }
        }
    }

    public void SetWindowSize(int width, int height)
    {
        lock (gate)
        {
            values["MainWindow.Width"] = width;
            values["MainWindow.Height"] = height;
            Save();
        }
    }

    public void Remove(string key)
    {
        lock (gate)
        {
            values.Remove(key);
            Save();
        }
    }

    private void Save()
    {
        // Replace only after a complete, flushed document has been written.
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
        {
            JsonSerializer.Serialize(stream, values, new JsonSerializerOptions { WriteIndented = true });
            stream.Flush(true);
        }
        File.Move(temporaryPath, path, overwrite: true);
        log("Settings saved: " + values.ToJsonString());
    }
}
