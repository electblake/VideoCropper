using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VideoCropper
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string WindowWidthSetting = "MainWindow.Width";
        private const string WindowHeightSetting = "MainWindow.Height";
        private readonly DispatcherTimer resizeSettingsTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

        // Assigned when the WinUI window is constructed, before picker events can run.
        public static Window Window { get; private set; } = null!;
        public MainWindow()
        {
            InitializeComponent();
            var version = typeof(App).Assembly.GetName().Version!;
            Title = $"Video Cropper v{version.ToString(3)}";
            if (AppStorage.NoProfile) Title += " — No profile";
            Window = this;

            var settings = App.Settings;
            if (settings.ContainsKey(WindowWidthSetting))
            {
                AppWindow.Resize(new SizeInt32(
                    settings.Get<int>(WindowWidthSetting),
                    settings.Get<int>(WindowHeightSetting)));
            }
            else
            {
                AppWindow.Resize(new SizeInt32(1800, AppWindow.Size.Height));
            }

            AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "EncircledPIERound.ico"));
            WindowFrame.Navigate(typeof(MainPage));
            AppWindow.Closing += MainWindow_Closing;
            resizeSettingsTimer.Tick += (_, _) =>
            {
                resizeSettingsTimer.Stop();
                if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
                    App.Settings.SetWindowSize(AppWindow.Size.Width, AppWindow.Size.Height);
            };
            AppWindow.Changed += (_, args) =>
            {
                if (args.DidSizeChange &&
                    AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
                {
                    resizeSettingsTimer.Stop();
                    resizeSettingsTimer.Start();
                }
            };
        }

        private void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            resizeSettingsTimer.Stop();
            if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
                App.Settings.SetWindowSize(sender.Size.Width, sender.Size.Height);
            App.Diagnostics.Log("Main window closing");
        }

        private async void OpenVideo_Click(object sender, RoutedEventArgs e)
        {
            string[] allSupportedTypes = [".mkv", ".mp4"];
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            foreach (var supportedType in allSupportedTypes)
            {
                filePicker.FileTypeFilter.Add(supportedType);
            }
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            App.Diagnostics.Log("Opening video picker (menu)");
            var files = await filePicker.PickMultipleFilesAsync();
            App.Diagnostics.Log($"Video picker returned {files.Count} files");
            var ffmpegPath = Path.Join(AppContext.BaseDirectory, "Assets/ffmpeg.exe");
            WindowFrame.Navigate(typeof(VideoCropperPage), new CropperProps
            {
                FfmpegPath = ffmpegPath,
                VideoPaths = files.Select(file => file.Path).ToList(),
                TypeToNavigateTo = typeof(MainPage).FullName
            });
        }

        private async void NvidiaOverlayHelp_Click(object sender, RoutedEventArgs e)
        {
            NvidiaOverlayHelpDialog.XamlRoot = WindowFrame.XamlRoot;
            await NvidiaOverlayHelpDialog.ShowAsync();
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            var version = typeof(App).Assembly.GetName().Version!.ToString();
            var status = new TextBlock { Text = $"Running version {version}. Checking GitHub for updates…", TextWrapping = TextWrapping.Wrap };
            var progress = new ProgressBar { IsIndeterminate = true, Minimum = 0, Maximum = 100 };
            var download = new Button { Content = "Download installer", IsEnabled = false };
            var install = new Button { Content = "Quit and install new version", IsEnabled = false };
            var content = new StackPanel { Spacing = 12, MinWidth = 400, MaxWidth = 500 };
            content.Children.Add(status);
            content.Children.Add(progress);
            content.Children.Add(download);
            content.Children.Add(install);
            content.Children.Add(new TextBlock { Text = "Finish any active crops before installing. Windows will ask permission to run Setup.", TextWrapping = TextWrapping.Wrap });
            var dialog = new ContentDialog
            {
                Title = "Check for updates", XamlRoot = WindowFrame.XamlRoot,
                Content = content, CloseButtonText = "Close"
            };
            var busy = true;
            dialog.Closing += (_, args) => args.Cancel = busy;
            dialog.Opened += async (_, _) =>
            {
                var release = await AppUpdates.CheckAsync(version);
                progress.IsIndeterminate = false;
                busy = false;
                if (release is null)
                {
                    status.Text = $"You’re up to date. Running version {version}.";
                    return;
                }
                status.Text = $"Version {release.Tag} is available. You’re running {version}.";
                download.IsEnabled = true;
                download.Click += async (_, _) =>
                {
                    busy = true;
                    download.IsEnabled = false;
                    status.Text = "Downloading installer…";
                    var path = await AppUpdates.DownloadAsync(release, new Progress<double>(percent =>
                    {
                        progress.Value = percent;
                        status.Text = $"Downloading installer… {percent:F0}%";
                    }));
                    status.Text = $"Version {release.Tag} is downloaded and ready to install.";
                    progress.Value = 100;
                    busy = false;
                    install.IsEnabled = true;
                    install.Click += (_, _) =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                        dialog.Hide();
                        Close();
                    };
                };
            };
            await dialog.ShowAsync();
        }

        private async void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(AppStorage.Root);
        }

        private async void OpenCrashReportsFolder_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(AppStorage.CrashesFolder);
        }

        private async void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderPathAsync(AppStorage.LogsFolder);
        }
    }
}
