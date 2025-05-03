namespace Horizon.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // Get settings and display them in the UI
        SearchEngineSelector.ItemsSource = SearchEngineHelper.SearchEngines;
        string SearchEngine = SettingsHelper.GetSetting("EngineFriendlyName");

        foreach (SearchEngine engine in SearchEngineHelper.SearchEngines)
        {
            if (engine.SearchUrl == SettingsHelper.CurrentSearchUrl)
            {
                SearchEngineSelector.SelectedItem = engine;
            }
        }

        BackdropTypeSelector.ItemsSource = Backdrops.BackdropsList;
        string Backdrop = SettingsHelper.GetSetting("OverrideBackdropType");
        foreach (string Backdp in Backdrops.BackdropsList)
        {
            if (Backdp == SettingsHelper.CurrentBackdrop)
            {
                BackdropTypeSelector.SelectedItem = Backdp;
            }
        }

        if (SettingsHelper.GetSetting("AdvancedCTX") == "true")
        {
            AdvancedCTXToggle.IsOn = true;
        }

        // Set event handlers
        SearchEngineSelector.SelectionChanged += SearchEngineSelector_SelectionChanged;
        BackdropTypeSelector.SelectionChanged += BackdropTypeSelector_SelectionChanged;
        AdvancedCTXToggle.Toggled += AdvancedCTXToggle_Toggled;
    }

    private void SetAsDefaultButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void SearchEngineSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SearchEngine engine = e.AddedItems[0] as SearchEngine;
        SearchEngineHelper.SetSearchEngine(engine);
    }

    private void BackdropTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SettingsHelper.SetSetting("OverrideBackdropType", e.AddedItems[0].ToString());
    }

    private void AdvancedCTXToggle_Toggled(object sender, RoutedEventArgs e)
    {
        switch ((sender as ToggleSwitch).IsOn)
        {
            case true:
                SettingsHelper.SetSetting("AdvancedCTX", "true");
                break;
            case false:
                SettingsHelper.SetSetting("AdvancedCTX", "false");
                break;
        }
    }

    /*private async void ClearUserDataButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await UI.ShowDialogWithAction($"Question", "Do you really want to clear all user data?", "Yes", "No");
        if (result == ContentDialogResult.Primary)
        {
            ClearUserDataProgressRing.IsActive = true;
            ClearUserDataBtn.IsEnabled = false;
            await WebView2ProfileDataHelper.ClearAllProfileDataAsync();
            ClearUserDataProgressRing.IsActive = false;
            ClearUserDataBtn.IsEnabled = true;
            ContentDialog dialog = new()
            {
                Title = "Info",
                Content = "User data was cleared",
                PrimaryButtonText = "Ok & restart app"
            };

            ContentDialogResult contentDialogResult = await dialog.ShowAsync();
            if (contentDialogResult == ContentDialogResult.Primary)
            {
                var appRestart = await CoreApplication.RequestRestartAsync(string.Empty);
                if (appRestart == AppRestartFailureReason.NotInForeground || appRestart == AppRestartFailureReason.RestartPending || appRestart == AppRestartFailureReason.Other)
                {
                    NotificationHelper.NotifyUser("Error", "Please restart Horizon manually");
                }
            }
        }
    }*/

    private void VersionTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        string appversion = AppVersionHelper.GetAppVersion();
        string apparch = RuntimeInformation.ProcessArchitecture.ToString();
        (sender as TextBlock).Text = $"Version {appversion} ({apparch})";
    }

    private async void SettingsCardClickHandler(object sender, RoutedEventArgs e)
    {
        switch ((sender as SettingsCard).Tag)
        {
            case "Extensions":
                WindowHelper.CreateNativeTabInMainWindow("Extensions", typeof(ExtensionsPage));
                break;
            case "GNU":
                await WS.Launcher.LaunchUriAsync(new Uri("https://raw.githubusercontent.com/horizon-developers/browser/refs/heads/main/LICENSE"));
                break;
            case "GitHub":
                await WS.Launcher.LaunchUriAsync(new Uri("https://github.com/Horizon-developers/browser"));
                break;
            case "DevSanx":
                await WS.Launcher.LaunchUriAsync(new Uri("https://discord.com/invite/windows-apps-hub-714581497222398064"));
                break;
            case "Donate":
                await WS.Launcher.LaunchUriAsync(new Uri("https://paypal.me/julianhasreiter"));
                break;
            case "BuildInfo":
                string dotnetver = Environment.Version.ToString();
                string appver = AppVersionHelper.GetAppVersion();
                string apparch = RuntimeInformation.ProcessArchitecture.ToString();
                string sysarch = RuntimeInformation.OSArchitecture.ToString();
                string sysversion = Environment.OSVersion.VersionString;

                string wv2version = CoreWebView2Environment.GetAvailableBrowserVersionString();

                string appsdkversion = $"{Microsoft.WindowsAppSDK.Release.Major}.{Microsoft.WindowsAppSDK.Release.Minor}.{Microsoft.WindowsAppSDK.Release.Patch}";
                string appsdkchannel = Microsoft.WindowsAppSDK.Release.Channel;

                string debugCombinedString = $"Horizon Version {appver}\n.NET Version: {dotnetver}\nAppArch: {apparch}\nSys: {sysversion}\nSysArch: {sysarch}\nWebView2Runtime version: {wv2version}\nWindowsAppSdk: {appsdkversion} ({appsdkchannel})\n";


                Win32Helper.ShowMessageBox("Horizon", $"Build information (press Ctrl + C to copy):\n" + debugCombinedString);
                break;
        }
    }
}
