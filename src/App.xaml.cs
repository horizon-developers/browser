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

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

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

        if (Environment.IsPrivilegedProcess)
        {
            Win32Helper.ShowMessageBox("Horizon - Warning", "Warning!\nThis instance of Horizon is running elevated, which isn't recommened due to possible security issues.", WindowHelper.HWND);
        }
    }

    private void App_Activated(object sender, AppActivationArguments e)
    {
        HandleUriActivation(e);
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
