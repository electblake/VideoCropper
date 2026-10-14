# Video Cropper
This repo provides a Windows desktop application for cropping videos. Only supports Windows 10 and 11 (not tested on other versions of Windows). Powered by FFMPEG.

<img width="1791" height="1023" alt="Screenshot 2025-07-28 103546" src="https://github.com/user-attachments/assets/e40e106c-9399-4eb4-87e7-f93264d6ee8b" />

## How to build
You need to have at least .NET 9 runtime installed to build the software. Download the latest runtime [here](https://dotnet.microsoft.com/en-us/download). If you're not sure which one to download, try [.NET 9.0 Version 9.0.203](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-9.0.203-windows-x64-installer)

In the project folder, run the below
```
dotnet publish -c Release -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None
```
When that completes, go to `\bin\Release\net<version>-windows\win-x64` and you'll find the **VideoCropper.exe**.

## Development

### Portable release build

From Windows with PowerShell 7.4+ (`pwsh`) and the .NET 9 SDK
(`dotnet`) on `PATH`, run:

```powershell
pwsh -NoProfile -File scripts/build-portable.ps1
# Optional output directory, relative to the repository root or absolute:
pwsh -NoProfile -File scripts/build-portable.ps1 ReleaseArtifacts/ci-build
```

Initialize the Git submodules and supply `VideoCropper/Assets/ffmpeg.exe` and its
runtime DLLs before building. The script does not install tools or invoke mise.
It reads the version from the package manifest and produces a self-contained x64
folder, ZIP, and `.sha256` file under `ReleaseArtifacts/portable` by default.
Use a fresh output directory for another build of the same version; existing build
folders and ZIPs are not overwritten. Any failed command stops the script.
The script does not bump versions, commit, tag, or publish to GitHub.

### Setup installer build

The setup script restores the pinned `Tools.InnoSetup` 7.1.0 NuGet dependency from
`installer/Installer.Tools.csproj` into your user NuGet cache. No system-wide
Inno Setup installation or compiler entry on `PATH` is needed. With PowerShell
7.4+, the .NET 9 SDK, and initialized Git submodules, run on Windows (local
FFmpeg binaries are not needed for this build):

```powershell
pwsh -NoProfile -File scripts/build-setup.ps1
# Optional fresh output directory:
pwsh -NoProfile -File scripts/build-setup.ps1 ReleaseArtifacts/ci-setup
```

This publishes a framework-dependent app and compiles `installer/VideoCropper.iss`.
The default output directory is `ReleaseArtifacts/setup`; it contains the app payload,
`VideoCropper_<version>_x64_Setup.exe`, and the setup executable's `.sha256` file.
The setup script restores its compiler using the committed NuGet lock file.
Neither build script invokes mise or publishes a release.

Setup requires administrator access and installs under `%ProgramFiles%\VideoCropper`,
adds a Start Menu shortcut and an optional desktop shortcut, and registers an
uninstaller in Windows Settings. A stable application ID makes later setups reuse
the same installation. Uninstall the previous per-user bundled setup before switching
to this all-users installer. Close running crops before upgrading. User settings, logs,
and crash reports in `%APPDATA%\VideoCropper` are retained on uninstall.

The small setup executable embeds only the framework-dependent application.
During Setup, Inno downloads checksum-pinned Visual C++ x64, .NET 8 x64, Windows
App Runtime 1.7 x64, and FFmpeg dependencies. Setup runs the Microsoft runtime
installers and installs FFmpeg into `Assets/ffmpeg.exe`; users do not run commands
or configure PATH. Internet access is required during installation. Microsoft
runtimes remain installed on uninstall; the app-local FFmpeg executable is removed.
Dependency URLs and SHA-256 hashes are pinned in `installer/VideoCropper.iss` and
must be updated together when servicing dependencies. The portable build remains
self-contained. See Microsoft's [framework-dependent deployment guidance](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deploy-unpackaged-apps)
and Inno's [download and archive support](https://jrsoftware.org/ishelp/topic_filessection.htm).

The current setup build is **unsigned**. Before public distribution, configure
Inno Setup's [SignTool](https://jrsoftware.org/ishelp/topic_setup_signtool.htm)
with a trusted signing identity to sign Setup and its uninstaller. The scripts do
not create certificates, modify certificate trust, or store signing credentials.
The legacy MSIX manifest is used for the version; this installer installs the
unpackaged application and does not replace or migrate an existing MSIX install.

### Run locally

Launch `VideoCropper.exe -NoProfile` for a fresh, isolated session. The flag is
case-insensitive and off by default. Each launch uses a unique folder under
`%TEMP%\VideoCropper` for settings, logs, and crash reports, leaving your normal
profile untouched. Temporary files remain available after exit for debugging.
Windows notification registration and completion notifications are disabled in
this mode; the title bar shows “No profile”. This isolates app preferences, not
video files: crop outputs still go beside the selected input, so use test videos.

For a development session, run `mise run dev -- -NoProfile`.

Run `mise run dev` to build and launch the app once in unpackaged x64 Debug mode
using `dotnet run`.

Run `mise run watch` for automatic rebuilding with the .NET SDK's built-in
`dotnet watch`. Saving C# or XAML files (including shared
components), the project file, or the package manifest rebuilds and restarts the app.
This uses full restarts, not in-place Hot Reload; a restart interrupts active crops.
Close any separately launched instance before starting the task. Press Ctrl+C to
stop watching. Build failures prevent the updated app from launching.

## Run without building
You can also just download the release builds if you don't wish to build manually. Unfortunately packages created in WinUI 3 have to be signed with a certificate, and certificates sourced from trusted companies cost hundreds of dollars. If you wish to install the package, you'll have to install a certificate signed by myself, as described [here](https://github.com/PeteJobi/VideoCropper/releases/tag/cert). You only need to do this once - future updates will not require different certificates.

## How to use
When you open the program, you will be prompted to upload a video. Once that is done, you will be taken to the crop interface page. Drag and resize the crop frame as required. By default, the aspect ratio is not locked and you can resize freely, but you can select the "**Lock aspect ratio**" checkbox to preserve it while resizing. You can choose from a bunch of predefined aspect ratios or reset to the original. Center the frame within the video with the "**Center Frame**" button. Selected crop presets, crop behavior, and performance settings roam between sessions and are applied automatically when the next video loads.

As you drag and resize, the coordinates of the frame will reflect in the number boxes labelled **X**, **Y**, **Width** and **Height**. If you want to be exact, you can manually change any of these values and the frame will update to reflect your input. There are simple video controls that can help you see how parts of the video look within the frame.

If you want to see more clearly what's in the crop frame, especially if you intend to crop a small portion, you can tick the **Zoom into frame** checkbox.

When you're done setting up the crop frame, click the "**Crop!**" button to start cropping. A progress bar showing the progress of the crop operation will show up. While the process is on-going, you can choose to pause or cancel it. Once the process is done, you can click the "**View**" button to view the output. 

**Settings → Preview while processing** is enabled by default and saved between sessions.
Set the timeline's **In** and **Out** markers to choose the segment to export. Cropping
uses that range for video and audio; progress and ETA cover the selected duration.
The initial range covers the whole video.

During cropping, the paused source preview follows FFmpeg's latest processed timestamp
at most twice per second, retaining the crop overlay. Slow seeks skip intermediate updates
instead of building a queue. Playback controls are disabled while the preview follows
processing; turn the setting off to stop automatic seeking. Video navigation is disabled
during a crop. Completion, cancellation, and leaving the page stop preview updates.
This previews the source at the reported processing position, not the encoded output.
Seeking still uses decoder resources, especially for high-resolution or long-GOP video;
disable the setting when maximum encoding throughput matters. It does not change the crop
dimensions automatically when the video's composition changes.

The output file will be created in the same folder and it will have the same file name with "__CROPPED_" appended to it. Be warned that if such a file exists, it will be deleted.

Click the **Go back** button to return to the file selection page.

## Settings and diagnostics

Use **Help → Check for updates** to compare the running version with the latest
stable GitHub release from `electblake/VideoCropper`. Version comparison supports
the fork's four-part versions as well as semantic prerelease ordering. The dialog
shows when the app is up to date. When an update is available, choose **Download
installer** to download the x64 Setup release asset with percentage progress, then
**Quit and install new version** to launch Setup and close Video Cropper. Finish
active crops first. Setup requests Windows administrator permission.

Setup retains the existing installation directory and shortcut choices using its
stable application ID. Inno Setup's Windows Restart Manager integration checks
files in use and presents its standard close-app prompt before replacing them.
The final installer page offers to launch Video Cropper again.

Run `dotnet run --project Tests/UpdatesSmoke -- --live` to check version ordering,
the real GitHub release lookup, installer download, and the up-to-date result.
This check downloads and deletes a temporary installer without executing it.

The cropper restores the last rotation, crop position and size, aspect-ratio preset,
aspect-ratio lock, centering, zoom-into-frame, mouse-wheel zoom, pixel-aspect
normalization, processing preview, auto-advance, and hardware selection on the next
load. Crop position and size are saved as percentages of the rotated video canvas;
a selected aspect-ratio preset is reapplied when the next video's proportions differ.
These preferences apply across videos and app sessions.

Window resizing waits for a 500 ms pause before refitting the preview and rebuilding
the crop frame when needed. The final fit is applied without an additional animation.
Window-size settings are saved after the same delay and again when the app closes.

**Load next** opens the next video after a successful crop. **Quit when done**
shows a 30-second countdown after a successful crop when the current video is the
last in the list (including a single video), or when **Load next** is off. Choose
**Quit now** to close immediately or **Cancel** to stay in the app. Canceling the
dialog leaves the preference enabled for later crops. Failed or canceled crops
do not start the countdown. Both checkbox preferences are saved between sessions.

The app resolves the current user's Windows roaming application-data folder using
`Environment.SpecialFolder.ApplicationData` and stores its data under `VideoCropper`
(normally `%APPDATA%\VideoCropper`). It uses this store for both packaged and unpackaged
launches without requiring a package identity. Windows may virtualize the physical
path for an MSIX installation; **File → Open App Data Folder** opens the app's store.

- `settings.json`: window dimensions, crop presets and coordinates, crop behavior,
  auto-advance, and hardware-acceleration preferences. Changes are saved immediately
  by writing and flushing a complete temporary document, then replacing the settings
  file. Settings are loaded before the main window is created. Invalid configuration
  fails visibly and is not silently reset. Old Windows settings stores are not imported.
- `Logs\`: a unique timestamped log per process session, including startup/runtime
  information, saved settings, video loading, media details, GPU discovery, crop
  requests, and external-process commands, stdout, stderr, and exit codes. Each entry
  is written synchronously, including entries from background threads.
- `CrashReports\`: unique text reports for unhandled WinUI/CLR exceptions, unobserved
  task failures, media failures, and unsuccessful FFmpeg crop processes. Each includes
  the exception details available from the runtime, settings on disk, and a copy of
  the session log. Exceptions are not marked handled or suppressed.

Use **File → Open Logs Folder** or **File → Open Crash Reports Folder** to inspect or
share diagnostics. Reports contain video paths, commands, and configuration values.
For debugging, provide the report and the steps that triggered the failure.

These are application-level reports, not native memory dumps. Native fail-fast
termination, process kills, and failures before diagnostics initialization may leave
only the last persisted session log. WinUI may provide less detail than the original
exception; see [Microsoft's UnhandledException documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception).

Run the diagnostics integration checks with:

```powershell
dotnet run --project Tests/DiagnosticsSmoke/DiagnosticsSmoke.csproj
```

The checks use an isolated temporary folder and deliberately crash a child process
to verify that crash reports preserve the final log entries and exception details.
