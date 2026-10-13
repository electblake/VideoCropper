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
