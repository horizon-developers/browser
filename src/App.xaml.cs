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
        UnhandledException += App_UnhandledException;

        // Warm the ad blocklist while the first WebView2 is still being created, so the first
        // page load is filtered from its very first request without deferring any of them.
        if (AppSettings.Current.BlockAds)
            _ = ABEDatabase.EnsureLoadedAsync();
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

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        AppInstance.GetCurrent().Activated += App_Activated;

        if (AppSettings.Current.IsAppLockEnabled)
        {
            if (await WindowsHelloHelper.CheckSec())
            {
                WindowHelper.CreateMainWindow();
                WindowHelper.ActivateMainWindow();
            }
            else
            {
                Process.GetCurrentProcess().Kill();
                return;
            }
        }
        else
        {
            WindowHelper.CreateMainWindow();
            WindowHelper.ActivateMainWindow();
        }
        
        HandleUriActivation(appArgs);
        RegisterAcceleratedLaunches();

        if (Environment.IsPrivilegedProcess)
        {
            Win32Helper.ShowMessageBox("Horizon - Warning", "Warning!\nThis instance of Horizon is running elevated, which isn't recommened due to possible security issues.", WindowHelper.HWND);
        }
    }

    private void App_Activated(object? sender, AppActivationArguments e)
    {
        HandleUriActivation(e);
        WindowHelper.RestoreMainWindow();
    }

    private AcceleratedLaunchHelper? _acceleratedLaunchHelper;

    // Tile and protocol launches get delivered straight to this instance where supported,
    // anything else still goes through the redirection in Program and ends up in App_Activated
    private void RegisterAcceleratedLaunches()
    {
        try
        {
            _acceleratedLaunchHelper = new();
            _ = _acceleratedLaunchHelper.TryRegisterForTileLaunch(() => OnAcceleratedLaunch(string.Empty));
            _ = _acceleratedLaunchHelper.TryRegisterForUris(["http", "https"], OnAcceleratedLaunch);
            WindowHelper.MainWindow.Closed += (_, _) => _acceleratedLaunchHelper?.Dispose();
        }
        catch (Exception ex)
        {
            // Purely an optimization, the regular redirection keeps working without it
            Logger.LogEvent(Logger.Severity.Warning, "AcceleratedLaunch", $"Registration failed: {ex}");
            _acceleratedLaunchHelper?.Dispose();
            _acceleratedLaunchHelper = null;
        }
    }

    // Same result as a redirected activation in App_Activated
    private static void OnAcceleratedLaunch(string uri)
    {
        WindowHelper.CreateNewTabInMainWindow("New tab", uri);
        WindowHelper.RestoreMainWindow();
    }

    private static void HandleUriActivation(AppActivationArguments args)
    {
        string uri = string.Empty;
        if (args.Data != null && args.Kind is ExtendedActivationKind.Protocol)
        {
            WAMA.IProtocolActivatedEventArgs ProtocolArgs = args.Data.As<WAMA.IProtocolActivatedEventArgs>();
            uri = ProtocolArgs.Uri.ToString();
        }
        WindowHelper.CreateNewTabInMainWindow("New tab", uri);
    }
}
