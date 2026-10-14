using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace VideoCropper;

internal sealed record InstallerRelease(string Tag, string Url, string FileName, long Size);

internal static class AppUpdates
{
    private static readonly HttpClient Client = new();

    internal static bool IsNewer(string tag, string currentVersion) =>
        VersionComparer.VersionRelease.Compare(
            NuGetVersion.Parse(tag.TrimStart('v')), NuGetVersion.Parse(currentVersion)) > 0;

    internal static async Task<InstallerRelease?> CheckAsync(string currentVersion)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/electblake/VideoCropper/releases/latest");
        request.Headers.UserAgent.ParseAdd("VideoCropper/" + currentVersion);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var release = json.RootElement;
        var tag = release.GetProperty("tag_name").GetString()!;
        if (!IsNewer(tag, currentVersion)) return null;
        var asset = release.GetProperty("assets").EnumerateArray().Single(asset =>
            asset.GetProperty("name").GetString()!.EndsWith("_x64_Setup.exe", StringComparison.Ordinal));
        return new InstallerRelease(tag, asset.GetProperty("browser_download_url").GetString()!,
            asset.GetProperty("name").GetString()!, asset.GetProperty("size").GetInt64());
    }

    internal static async Task<string> DownloadAsync(InstallerRelease release, IProgress<double> progress)
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "VideoCropper", Guid.NewGuid().ToString("N")));
        var path = Path.Combine(directory.FullName, Path.GetFileName(release.FileName));
        using var response = await Client.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync();
        await using (var destination = File.Create(path))
        {
            var buffer = new byte[81920];
            long downloaded = 0;
            int count;
            while ((count = await source.ReadAsync(buffer)) != 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, count));
                downloaded += count;
                progress.Report(100d * downloaded / release.Size);
            }
        }
        return path;
    }
}
