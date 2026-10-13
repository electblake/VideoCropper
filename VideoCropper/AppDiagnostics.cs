using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FileLogger;

namespace VideoCropper;

internal sealed class AppDiagnostics : IFileLogger
{
    private readonly object gate = new();
    private readonly string crashesFolder;
    private readonly string settingsPath;
    public string SessionId { get; } = $"{DateTime.UtcNow:yyyyMMddTHHmmssfffffffZ}-{Environment.ProcessId}-{Guid.NewGuid():N}";
    public string LogPath { get; }
    private bool active = true;

    public AppDiagnostics(string logsFolder, string crashesFolder, string settingsPath)
    {
        this.crashesFolder = crashesFolder;
        this.settingsPath = settingsPath;
        Directory.CreateDirectory(logsFolder);
        Directory.CreateDirectory(crashesFolder);
        LogPath = Path.Combine(logsFolder, SessionId + ".log");
        Log($"Session started; version={Assembly.GetExecutingAssembly().GetName().Version}; " +
            $"OS={RuntimeInformation.OSDescription}; runtime={RuntimeInformation.FrameworkDescription}; " +
            $"architecture={RuntimeInformation.ProcessArchitecture}; base={AppContext.BaseDirectory}; settings={settingsPath}");
    }

    public void Activate(bool activate) { lock (gate) active = activate; }

    public void RegisterManagedExceptionHooks()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Report($"CLR unhandled exception; terminating={args.IsTerminating}", args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) =>
            Report("Unobserved task exception", args.Exception);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log("Process exiting");
    }

    public void Log(params object?[] messages)
    {
        lock (gate)
        {
            if (!active) return;
            // Synchronous writes leave no in-memory queue to lose during a crash.
            File.AppendAllText(LogPath,
                $"{DateTimeOffset.UtcNow:O} [thread {Environment.CurrentManagedThreadId}] {string.Join(", ", messages)}{Environment.NewLine}");
        }
    }

    public string Report(string source, object exception)
    {
        lock (gate)
        {
            Log($"FAILURE [{source}]", exception);
            var reportPath = Path.Combine(crashesFolder, $"{SessionId}-{Guid.NewGuid():N}.txt");
            using var report = new StreamWriter(reportPath);
            report.WriteLine($"VideoCropper diagnostic report\nUTC: {DateTimeOffset.UtcNow:O}\nSession: {SessionId}\nSource: {source}\nException:\n{exception}");
            report.WriteLine("\n--- Settings on disk ---");
            // A startup failure can happen before first-launch settings creation.
            if (File.Exists(settingsPath)) report.WriteLine(File.ReadAllText(settingsPath));
            report.WriteLine("\n--- Application session log ---");
            using var log = File.OpenText(LogPath);
            report.Write(log.ReadToEnd());
            return reportPath;
        }
    }
}
