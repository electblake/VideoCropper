using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VideoCropper
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        internal static AppDiagnostics Diagnostics { get; private set; } = null!;
        internal static SettingsStore Settings { get; private set; } = null!;
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Diagnostics = new AppDiagnostics(AppStorage.LogsFolder, AppStorage.CrashesFolder, AppStorage.SettingsPath);
            UnhandledException += (_, args) => Diagnostics.Report($"WinUI unhandled exception: {args.Message}", args.Exception);
            Diagnostics.RegisterManagedExceptionHooks();
            Settings = new SettingsStore(AppStorage.SettingsPath, message => Diagnostics.Log(message));
            Diagnostics.Log("Settings loaded");
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Diagnostics.Log("Launching main window");
            _window = new MainWindow();
            if (!AppStorage.NoProfile)
            {
                AppNotificationManager.Default.NotificationInvoked += (_, _) =>
                    _window.DispatcherQueue.TryEnqueue(() => _window.Activate());
                AppNotificationManager.Default.Register();
                _window.Closed += (_, _) => AppNotificationManager.Default.Unregister();
            }
            _window.Activate();
            Diagnostics.Log("Main window activated");
        }

        internal static void NotifyCropCompleted(string outputFile)
        {
            var notification = new AppNotificationBuilder()
                .AddText("Crop complete")
                .AddText(System.IO.Path.GetFileName(outputFile))
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
    }
}
