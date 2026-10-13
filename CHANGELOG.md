# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Historical entries were reconstructed from local Git tags, commit diffs, and shared-component
history. Dates reflect tag history, not independently verified publication dates. Version
headings omit the trailing Windows package revision `.0`; links retain the original tags.
The `cert` tag contains installation-certificate instructions and is not a separate app version.

## [Unreleased]

### Added

- Added `mise run watch` using the .NET SDK's built-in watcher to rebuild and restart on C#, XAML, and manifest changes; kept `mise run dev` as a single build-and-run.
- Added a Reset button after Rotate right to restore zero rotation while rotating the crop selection back with the video and saving the result.
- Added rotate-left and rotate-right controls above the aspect-ratio options, rotating the preview and crop selection, swapping matching aspect-ratio presets, and rotating encoded frames before cropping.
- Added saved rotation and crop width/height alongside crop position, with geometry stored as percentages of the rotated canvas.
- Added a saved “Quit when done” option with a 30-second countdown, Quit now, and Cancel after a successful crop of the last video or when Load next is disabled.
- Added settings round-trip checks for crop geometry, rotation, preset clearing, and centering.

### Changed

- Enabled warnings-as-errors and updated nullable annotations, typed settings reads, and shared-component initialization.
- Debounced preview fitting, crop-frame rebuilding, and window-size persistence until resizing has paused for 500 ms; applied the final resize fit without animation.
- Renamed “Load next video after cropping” to “Load next” without changing its behavior.
- Saved crop geometry, rotation, preset, and centering together in one settings update.

### Fixed

- Applied timeline in/out points to encoded video and audio, calculated progress over the selected duration, and offset processing-preview seeks to the source in point.
- Copied FFmpeg binaries as runtime files instead of WinUI resources.
- Closed the attached cancellation flyout directly instead of traversing its visual parents.
- Corrected `mise run watch` argument forwarding so rebuilds use the unpackaged x64 configuration instead of AnyCPU MSIX packaging.
- Applied rotation-button enabled states to the buttons instead of their StackPanel, fixing the XAML compilation failure.
- Prevented intermediate layout callbacks from overwriting crop settings during restoration.
- Restored crop dimensions and rotation on subsequent video loads and app sessions.
- Allowed the Center Frame toggle to be switched off and saved that state.

## [2.2.0] - 2026-10-13

### Added

- Added a default-on “Preview while processing” setting that follows FFmpeg's processed timestamp in the paused source preview, at most twice per second, skipping stale updates and waiting for each seek to finish.
- Added a current/total video counter before the navigation arrows, updated when navigating or automatically advancing through loaded videos.
- Added per-session application logs covering startup, settings changes, video loading, media details, GPU discovery, crop requests, and FFmpeg commands, output, and exit codes.
- Added diagnostic reports containing exception details, the settings snapshot, and the session log for unhandled WinUI/CLR exceptions, unobserved task failures, media failures, and failed FFmpeg crops.
- Added a File-menu shortcut to open crash reports.
- Added integration checks for settings persistence, concurrent logging, report contents, and deliberate child-process crashes.
- Added `mise run dev` to build the current source and launch the unpackaged x64 Debug app using `dotnet run`.
- Added this changelog with backfilled version history.

### Changed

- Derived the compiled app version from the package manifest and displayed it in the title bar as `Video Cropper vX.Y.Z`.
- Pointed modified shared components to maintained forks so release source checkouts include their changes.
- Consolidated window, crop, auto-advance, and hardware preferences in `%APPDATA%\VideoCropper\settings.json`, loaded at startup and saved as changes occur using a flushed temporary file and replacement. Previous Windows settings stores were not imported.
- Moved application logs and crash reports from Documents to the app's roaming-data folder and updated the File-menu folder shortcuts.
- Replaced buffered FFmpeg logging with synchronous, thread-safe session logging to preserve completed entries before a crash.
- Allowed crop-operation exceptions to reach the application crash-report hooks instead of discarding their stack traces in an error dialog.
- Changed crop X/Y coordinates to percentages and persisted their values between sessions.
- Replaced the Batch crop button with a saved “Load next video after cropping” option.
- Resolved the FFmpeg executable relative to the application directory for both video-opening routes.

## [2.1.0] - 2026-08-24

### Added

- Added multi-video selection, previous/next video navigation, and batch cropping.
- Added persistent window dimensions, crop presets, crop behavior, and hardware-acceleration selection.
- Added a File menu with video-opening and app-data/log-folder shortcuts.
- Added basic application crash logs and FFmpeg file logging.
- Added resizable settings panels, mouse-wheel zoom, and pixel-aspect-ratio normalization.
- Added timeline range markers and FFmpeg speed and estimated-time-remaining feedback.
- Added GPU capability diagnostics and support for 10- and 12-bit video on NVIDIA and Intel GPUs.

### Changed

- Improved crop completion controls and command feedback.
- Refined zoom-into-frame margins and limited zoom-out to the video bounds.
- Updated asset installation and shared video-processing components.

### Fixed

- Corrected crop-bound mapping from the displayed frame to the source video.
- Fixed AMD cropping through an FFmpeg update and corrected Intel GPU crop failures.
- Corrected GPU indexing for systems with multiple graphics adapters.
- Fixed missing timeline previews and improved long-path handling.

## [2.0.0] - 2025-12-21

### Added

- Added hardware-accelerated cropping and GPU selection.
- Added timeline controls and a zoom-into-frame option.
- Added pause/cancel processing controls and live crop-coordinate updates.

### Changed

- Switched video output to x265 encoding with CRF 18 quality.
- Reworked crop processing around shared processing and progress controls.
- Replaced the bundled FFmpeg executable with a smaller custom build, then added hardware-acceleration support.

### Fixed

- Fixed a cropper stack-overflow issue.
- Cleared stale output-file state before processing and set the output only after successful processing.

## [1.0.1] - 2025-08-04

### Changed

- Documented the Go back button for returning to file selection. The tagged diff contained no application-code changes.

## [1.0.0] - 2025-08-04

### Added

- Released the initial Windows video-cropping application powered by FFmpeg.
- Added a draggable, resizable crop frame with aspect-ratio presets and frame centering.
- Added video preview controls and cropped output beside the source file.
- Added build, usage, and certificate-installation documentation.

[Unreleased]: https://github.com/electblake/VideoCropper/compare/v2.2.0.0...HEAD
[2.2.0]: https://github.com/electblake/VideoCropper/compare/v2.1.0.0...v2.2.0.0
[2.1.0]: https://github.com/electblake/VideoCropper/compare/v2.0.0.0...v2.1.0.0
[2.0.0]: https://github.com/electblake/VideoCropper/compare/v1.0.1.0...v2.0.0.0
[1.0.1]: https://github.com/electblake/VideoCropper/compare/v1.0.0.0...v1.0.1.0
[1.0.0]: https://github.com/electblake/VideoCropper/tree/v1.0.0.0
