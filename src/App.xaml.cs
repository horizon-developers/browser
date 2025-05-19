namespace Horizon;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        SettingsHelper.LoadSettingsOnStartup();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        string AppVersion = AppVersionHelper.GetAppVersion();
        string WV2RuntimeVersion;
        try
        {
            WV2RuntimeVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            WV2RuntimeVersion = "NOT INSTALLED";
        }
        string dotnetver = Environment.Version.ToString();
        string apparch = RuntimeInformation.ProcessArchitecture.ToString();
        string BRI =
            $"Horizon version: {AppVersion}\n" +
            $"WebView2Runtime version: {WV2RuntimeVersion}\n" +
            $"DotNetRuntime version: {dotnetver}\n" +
            $"Host architecture: {apparch}\n" +
            "\n\n" +
            e.Exception.Message +
            e.Exception.StackTrace;
        Win32Helper.ShowMessageBox("Horizon crash handler", $"An unhandled exception occured!\nWe prevented the app from crashing, however you should report this bug!\nBug report information (press Ctrl + C to copy):\n" + BRI);
        e.Handled = true;
    }

    /*protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var eventargs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (eventargs.Kind is ExtendedActivationKind.Protocol && eventargs.Data is ProtocolActivatedEventArgs)
        {
            ProtocolActivatedEventArgs ProtocolArgs = eventargs.Data as ProtocolActivatedEventArgs;
            string uri = ProtocolArgs.Uri.ToString();
            if (WindowHelper.ActiveWindows.Count > 0)
            {
                WindowHelper.MainWindow = WindowHelper.CreateWindow();
                WindowHelper.MainWindow.Activate();
            }
            System.Diagnostics.Debug.WriteLine(uri);
            ((WindowHelper.ActiveWindows[0].Content as Frame).Content as MainPage).CreateWebTab("New tab", "\uEC6C", uri);
        }
        else
        {
            
        }

        var eventargs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (eventargs.Kind is ExtendedActivationKind.Protocol)
        {
            //ProtocolActivatedEventArgs ProtocolArgs = eventargs.Data as ProtocolActivatedEventArgs;
            IProtocolActivatedEventArgs ProtocolArgs = eventargs.Data as IProtocolActivatedEventArgs;
            string uri = ProtocolArgs.Uri.ToString();
            if (WindowHelper.ActiveWindows.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("NEW WIND ON START!!");
                WindowHelper.MainWindow = WindowHelper.CreateWindow();
                WindowHelper.MainWindow.Activate();
            }
            //((WindowHelper.ActiveWindows[0].Content as Frame).Content as MainPage).CreateWebTab("New tab", uri);
            (WindowHelper.ActiveWindows[0].Content as MainPage).CreateWebTab("New tab", uri);
            System.Diagnostics.Debug.WriteLine(WindowHelper.ActiveWindows.Count);
            return;
        }

        if (!await CheckSec())
        {
            Application.Current.Exit();
            return;
        }
        WindowHelper.MainWindow = WindowHelper.CreateWindow();
        WindowHelper.MainWindow.Activate();
        //((WindowHelper.MainWindow.Content as Frame).Content as MainPage).CreateTab("New tab", typeof(NewTabPage));
        (WindowHelper.MainWindow.Content as MainPage).CreateTab("New tab", typeof(NewTabPage));
        //System.Diagnostics.Debug.WriteLine(WindowHelper.ActiveWindows.Count);



    }*/

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey("HorizonSingleInstance");

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            // Redirect activation to that instance
            await mainInstance.RedirectActivationToAsync(appArgs);

            // And exit our instance and stop
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return;
        }

        // Otherwise, register for activation redirection
        AppInstance.GetCurrent().Activated += App_Activated;

        if (SettingsHelper.GetSetting("IsAppLockEnabled") == "true")
        {
            if (await WindowsHelloHelper.CheckSec())
            {
                WindowHelper.CreateMainWindow();
                WindowHelper.ActivateMainWindow();
            }
            else
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }
        }
        else
        {
            WindowHelper.CreateMainWindow();
            WindowHelper.ActivateMainWindow();
        }
        
        HandleUriActivation(appArgs);
    }

    private void App_Activated(object sender, AppActivationArguments e)
    {
        HandleUriActivation(e);
        // starting from here, the Microsoft.Windows.CsWin32 package has to referenced, as well as the following methods to NativeMethods.txt
        // ShowWindow
        // SetForegroundWindow
        // otherwise the required source is not going to be generated
        var hwnd = (Windows.Win32.Foundation.HWND)WinRT.Interop.WindowNative.GetWindowHandle(WindowHelper.MainWindow);
        Windows.Win32.PInvoke.ShowWindow(hwnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_RESTORE);
        Windows.Win32.PInvoke.SetForegroundWindow(hwnd);
    }

    private static void HandleUriActivation(AppActivationArguments args)
    {
        string uri = "about:blank";
        if (args.Kind is ExtendedActivationKind.Protocol && args.Data != null)
        {
            WAMA.IProtocolActivatedEventArgs ProtocolArgs = args.Data.As<WAMA.IProtocolActivatedEventArgs>();
            uri = ProtocolArgs.Uri.ToString();
            System.Diagnostics.Debug.WriteLine(uri);

            _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                (WindowHelper.MainWindow.Content as MainPage).CreateWebTab("New tab", uri);
            });
            return;
        }

        _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            (WindowHelper.MainWindow.Content as MainPage).UrlBoxWrapper.Visibility = Visibility.Visible;
        });
    }
}
