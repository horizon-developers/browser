namespace Horizon.Pages;

public sealed partial class SettingsPage : Page
{
    private WebView2 HeadlessWebViewInstance = new();

    public SettingsPage()
    {
        this.InitializeComponent();
        InitHeadless();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        SearchEngineSelector.ItemsSource = SearchEngineHelper.SearchEngines;
        foreach (SearchEngine engine in SearchEngineHelper.SearchEngines)
        {
            if (engine.SearchUrl == SettingsHelper.CurrentSearchUrl)
            {
                SearchEngineSelector.SelectedItem = engine;
                break;
            }
        }

        BackdropTypeSelector.ItemsSource = Backdrops.BackdropsList;
        foreach (string Backdp in Backdrops.BackdropsList)
        {
            if (Backdp == SettingsHelper.CurrentBackdrop)
            {
                BackdropTypeSelector.SelectedItem = Backdp;
                break;
            }
        }

        AdvancedCTXToggle.IsOn = SettingsHelper.GetSetting("AdvancedCTX") == "true";
        BlockCaptureToggle.IsOn = SettingsHelper.GetSetting("IsScreencaptureBlocked") == "true";
        WindowsHelloToggle.IsOn = SettingsHelper.GetSetting("IsAppLockEnabled") == "true";
        AlwaysOnTopToggle.IsOn = SettingsHelper.GetSetting("IsAlwaysOnTopEnabled") == "true";

        // Set event handlers
        SearchEngineSelector.SelectionChanged += SearchEngineSelector_SelectionChanged;
        BackdropTypeSelector.SelectionChanged += BackdropTypeSelector_SelectionChanged;
        AdvancedCTXToggle.Toggled += AdvancedCTXToggle_Toggled;
        BlockCaptureToggle.Toggled += BlockCaptureToggle_Toggled;
        WindowsHelloToggle.Toggled += WindowsHelloLockToggle_Toggled;
        AlwaysOnTopToggle.Toggled += AlwaysOnTopToggle_Toggled;
    }

    private async void InitHeadless()
    {
        await HeadlessWebViewInstance.EnsureCoreWebView2Async(await GlobalEnvironment.GetDefault());
        UpdateSetDownloadFolderSettingsCardDescription();
    }

    public void DisposeHeadless()
    {
        HeadlessWebViewInstance.Close();
    }

    private void SearchEngineSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SearchEngine engine = e.AddedItems[0] as SearchEngine;
        SearchEngineHelper.SetSearchEngine(engine);
    }

    private async void OpenProfileFolder_Click(object sender, RoutedEventArgs e)
    {
        await WS.Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
    }

    private void UpdateSetDownloadFolderSettingsCardDescription()
    {
        CoreWebView2Profile profile = HeadlessWebViewInstance.CoreWebView2.Profile;
        SetDownloadFolderSettingsCard.Description = profile.DefaultDownloadFolderPath;
    }

    private async void SetDownloadFolderButton_Click(object sender, RoutedEventArgs e)
    {
        //disable the button to avoid double-clicking
        var senderButton = sender as Button;
        senderButton.IsEnabled = false;

        // Create a folder picker
        FolderPicker openPicker = new();

        // Initialize the folder picker with the window handle (HWND).
        InitializeWithWindow.Initialize(openPicker, WindowHelper.HWND);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            CoreWebView2Profile profile = HeadlessWebViewInstance.CoreWebView2.Profile;
            profile.DefaultDownloadFolderPath = folder.Path;
        }

        //re-enable the button
        senderButton.IsEnabled = true;

        UpdateSetDownloadFolderSettingsCardDescription();

    }

    private void BackdropTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string Selection = e.AddedItems[0].ToString();
        SettingsHelper.SetSetting("OverrideBackdropType", e.AddedItems[0].ToString());
        SettingsHelper.CurrentBackdrop = Selection;
        switch (Selection)
        {
            case "Acrylic":
                WindowHelper.MainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
                break;
            case "Mica":
                WindowHelper.MainWindow.SystemBackdrop = new MicaBackdrop();
                break;
            case "Mica Alt":
                WindowHelper.MainWindow.SystemBackdrop = new MicaBackdrop
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
                break;
            case "None":
                WindowHelper.MainWindow.SystemBackdrop = null;
                break;
        }
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

    private void BlockCaptureToggle_Toggled(object sender, RoutedEventArgs e)
    {
        switch ((sender as ToggleSwitch).IsOn)
        {
            case true:
                SettingsHelper.SetSetting("IsScreencaptureBlocked", "true");
                WindowHelper.BlockScreencaptureForMainWindow(true);
                break;
            case false:
                SettingsHelper.SetSetting("IsScreencaptureBlocked", "false");
                WindowHelper.BlockScreencaptureForMainWindow(false);
                break;
        }
    }

    private async void WindowsHelloLockToggle_Toggled(object sender, RoutedEventArgs e)
    {
        switch ((sender as ToggleSwitch).IsOn)
        {
            case true:
                UserConsentVerifierAvailability IsWinHelloAvailable = await WindowsHelloHelper.CheckAvailability();
                if (IsWinHelloAvailable == UserConsentVerifierAvailability.Available)
                {
                    SettingsHelper.SetSetting("IsAppLockEnabled", "true");
                    return;
                }
                if (IsWinHelloAvailable == UserConsentVerifierAvailability.NotConfiguredForUser)
                {
                    Win32Helper.ShowMessageBox("Horizon", "An error occured while trying to setup App Lock using Windows Hello.\n\nPlease setup it in Windows Settings");
                    (sender as ToggleSwitch).IsOn = false;
                    break;
                }
                break;
            case false:
                SettingsHelper.SetSetting("IsAppLockEnabled", "false");
                break;
        }
    }

    private void AlwaysOnTopToggle_Toggled(object sender, RoutedEventArgs e)
    {
        switch ((sender as ToggleSwitch).IsOn)
        {
            case true:
                WindowHelper.SetMainWindowAlwaysOnTop(true);
                SettingsHelper.SetSetting("IsAlwaysOnTopEnabled", "true");
                break;
            case false:
                WindowHelper.SetMainWindowAlwaysOnTop(false);
                SettingsHelper.SetSetting("IsAlwaysOnTopEnabled", "false");
                break;
        }
    }

    /*private async void ClearUserDataButton_Click(object sender, RoutedEventArgs e)
    {
        // ShowDialogWithAction is no longer available, as PenguinApps.Core has never been updated to support WinAppSdk / WinUI
        var result = await UI.ShowDialogWithAction($"Question", "Do you really want to clear all user data?", "Yes", "No");
        if (result == ContentDialogResult.Primary)
        {
            ClearUserDataProgressRing.IsActive = true;
            ClearUserDataBtn.IsEnabled = false;
            await WebView2ProfileDataHelper.ClearAllProfileDataAsync(HeadlessWebViewInstance);
            ClearUserDataProgressRing.IsActive = false;
            ClearUserDataBtn.IsEnabled = true;
            ContentDialog dialog = new()
            {
                Title = "Info",
                Content = "User data was cleared",
                PrimaryButtonText = "Ok & restart app"
            };

            ContentDialogResult contentDialogResult = await dialog.ShowAsync();
            // This doesn't work for WinAppSdk apps, we have to find another way
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

                string debugCombinedString = $"Horizon Version {appver}\n.NET Version: {dotnetver}\nAppArch: {apparch}\nSys: {sysversion}\nSysArch: {sysarch}\nWebView2Runtime version: {wv2version}\n";


                Win32Helper.ShowMessageBox("Horizon", $"Build information (press Ctrl + C to copy):\n" + debugCombinedString);
                break;
        }
    }
}
