using VideoCropper;

(string Tag, string Current, bool Expected)[] cases =
[
    ("v2.3.0.0", "2.3.0.0", false),
    ("v2.3.0", "2.3.0.0", false),
    ("v2.10.0.0", "2.9.0.0", true),
    ("v2.3.0.1", "2.3.0.0", true),
    ("v2.2.9.0", "2.3.0.0", false),
    ("v3.0.0-rc.1", "3.0.0", false),
    ("v3.0.0", "3.0.0-rc.1", true),
    ("v3.0.0-rc.10", "3.0.0-rc.2", true),
    ("v3.0.0+build.2", "3.0.0+build.1", false)
];
foreach (var item in cases)
    if (AppUpdates.IsNewer(item.Tag, item.Current) != item.Expected)
        throw new Exception($"Incorrect ordering: {item.Tag} vs {item.Current}");
Console.WriteLine($"Passed {cases.Length} version comparisons.");

if (args.Contains("--live"))
{
    var release = (await AppUpdates.CheckAsync("0.0.0"))!;
    Console.WriteLine($"Found {release.Tag}: {release.FileName} ({release.Size} bytes)");
    var path = await AppUpdates.DownloadAsync(release, new Progress<double>());
    if (new FileInfo(path).Length != release.Size) throw new Exception("Downloaded size differs from release asset.");
    if (await AppUpdates.CheckAsync(release.Tag.TrimStart('v')) is not null) throw new Exception("Equal version offered as update.");
    File.Delete(path);
    Directory.Delete(Path.GetDirectoryName(path)!);
    Console.WriteLine("Live release lookup, download, and up-to-date checks passed.");
}
