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

        public static Window Window;
        public MainWindow()
        {
            InitializeComponent();
            var version = typeof(App).Assembly.GetName().Version!;
            Title = $"Video Cropper v{version.ToString(3)}";
            Window = this;

            var settings = App.Settings;
            if (settings.ContainsKey(WindowWidthSetting))
            {
                AppWindow.Resize(new SizeInt32(
                    (int)settings[WindowWidthSetting],
                    (int)settings[WindowHeightSetting]));
            }
            else
            {
                AppWindow.Resize(new SizeInt32(1800, AppWindow.Size.Height));
            }

            AppWindow.SetTitleBarIcon("Assets/EncircledPIERound.ico");
            WindowFrame.Navigate(typeof(MainPage));
            AppWindow.Closing += MainWindow_Closing;
            AppWindow.Changed += (_, args) =>
            {
                if (args.DidSizeChange &&
                    AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
                    App.Settings.SetWindowSize(AppWindow.Size.Width, AppWindow.Size.Height);
            };
        }

        private void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
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
