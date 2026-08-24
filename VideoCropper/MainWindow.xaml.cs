using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.Windows.Storage;
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
            Window = this;

            using var applicationData = ApplicationData.GetDefault();
            var settings = applicationData.LocalSettings.Values;
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
        }

        private void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            using var applicationData = ApplicationData.GetDefault();
            var settings = applicationData.LocalSettings.Values;
            settings[WindowWidthSetting] = sender.Size.Width;
            settings[WindowHeightSetting] = sender.Size.Height;
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
            var files = await filePicker.PickMultipleFilesAsync();
            var ffmpegPath = Path.Join(Package.Current.InstalledLocation.Path, "Assets/ffmpeg.exe");
            WindowFrame.Navigate(typeof(VideoCropperPage), new CropperProps
            {
                FfmpegPath = ffmpegPath,
                VideoPaths = files.Select(file => file.Path).ToList(),
                TypeToNavigateTo = typeof(MainPage).FullName
            });
        }

        private async void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
        {
            using var applicationData = ApplicationData.GetDefault();
            await Launcher.LaunchFolderAsync(applicationData.LocalFolder);
        }

        private async void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            var logPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ReelBox",
                "Crop");
            var logFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(logPath);
            await Launcher.LaunchFolderAsync(logFolder);
        }
    }
}
