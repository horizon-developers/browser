namespace Horizon.Views;

public sealed partial class SettingsView : Page
{
    public readonly WebView2 HeadlessWebViewInstance = new();

    public SettingsView()
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
        DisableWebPageDarkening.IsOn = SettingsHelper.GetSetting("DisableWebPageDarkening") == "true";
        BlockCaptureToggle.IsOn = SettingsHelper.GetSetting("IsScreencaptureBlocked") == "true";
        WindowsHelloToggle.IsOn = SettingsHelper.GetSetting("IsAppLockEnabled") == "true";

        // Set event handlers
        SearchEngineSelector.SelectionChanged += SearchEngineSelector_SelectionChanged;
        BackdropTypeSelector.SelectionChanged += BackdropTypeSelector_SelectionChanged;
        AdvancedCTXToggle.Toggled += AdvancedCTXToggle_Toggled;
        DisableWebPageDarkening.Toggled += DisableWebPageDarkening_Toggled;
        BlockCaptureToggle.Toggled += BlockCaptureToggle_Toggled;
        WindowsHelloToggle.Toggled += WindowsHelloLockToggle_Toggled;
    }

    private async void InitHeadless()
    {
        await HeadlessWebViewInstance.EnsureCoreWebView2Async(await GlobalEnvironment.GetDefault());
        UpdateSetDownloadFolderSettingsCardDescription();
    }

    private async void UserPictureDisplay_Loaded(object sender, RoutedEventArgs e)
    {
        var pic = await UserHelper.GetUserPicture();
        (sender as PersonPicture).ProfilePicture = pic;
    }

    private async void UserNameDisplay_Loaded(object sender, RoutedEventArgs e)
    {
        var name = await UserHelper.GetUserName();
        (sender as TextBlock).Text = name;
    }

    private void SearchEngineSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SearchEngine engine = e.AddedItems[0] as SearchEngine;
        SearchEngineHelper.SetSearchEngine(engine);
    }

    private async void OpenProfileFolder_Click(object sender, RoutedEventArgs e)
    {
        await WS.Launcher.LaunchFolderAsync(FolderHelper.LocalFolder);
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
        FolderPicker openPicker = new(WindowHelper.MainWindow.AppWindow.Id)
        {
            // Set options for your folder picker
            SuggestedStartLocation = PickerLocationId.Desktop
        };

        // Open the picker for the user to pick a folder
        var folder = await openPicker.PickSingleFolderAsync();
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

    private void DisableWebPageDarkening_Toggled(object sender, RoutedEventArgs e)
    {
        switch ((sender as ToggleSwitch).IsOn)
        {
            case true:
                SettingsHelper.SetSetting("DisableWebPageDarkening", "true");
                break;
            case false:
                SettingsHelper.SetSetting("DisableWebPageDarkening", "false");
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

    #region Extensions
    private async Task<IReadOnlyList<CoreWebView2BrowserExtension>> GetExtensionListAsync()
    {
        IReadOnlyList<CoreWebView2BrowserExtension> extensions = await HeadlessWebViewInstance.CoreWebView2.Profile.GetBrowserExtensionsAsync();
#if DEBUG
        foreach (CoreWebView2BrowserExtension extension in extensions)
        {
            System.Diagnostics.Debug.WriteLine(extension.Name + ": " + extension.Id);
        }
#endif
        return extensions;
    }

    private async void InstallExButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker
        FolderPicker openPicker = new(WindowHelper.MainWindow.AppWindow.Id)
        {
            // Set options for your folder picker
            SuggestedStartLocation = PickerLocationId.Desktop
        };

        // Open the picker for the user to pick a folder
        var folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            try
            {
                await HeadlessWebViewInstance.CoreWebView2.Profile.AddBrowserExtensionAsync(folder.Path);
            }
            catch (Exception ex)
            {
                ContentDialog InstallFailedDialog = new()
                {
                    Title = "Error",
                    Content = "Installation failed because:\n" + ex.Message,
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };
                await InstallFailedDialog.ShowAsync();
            }
            RefreshExtensionsList();
        }
    }

    private void ExtensionsExpander_Expanded(object sender, EventArgs e)
    {
        RefreshExtensionsList();
    }

    private async void RefreshExtensionsList()
    {
        ExtensionsListView.SelectedItem = null;
        ExtensionsListView.Items.Clear();
        IReadOnlyList<CoreWebView2BrowserExtension> list = await GetExtensionListAsync();
        foreach (CoreWebView2BrowserExtension extension in list)
        {
            ExtensionsListView.Items.Add(extension);
        }
    }

    CoreWebView2BrowserExtension selectedItem;
    private void ExtensionsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        selectedItem = ((FrameworkElement)e.OriginalSource).DataContext as CoreWebView2BrowserExtension;
    }
    #endregion

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "CopyID":
                ClipboardHelper.CopyTextToClipboard(selectedItem.Id);
                break;
            case "Delete":
                ContentDialog dialog = new()
                {
                    Title = "Remove?",
                    PrimaryButtonText = "Remove",
                    CloseButtonText = "Cancel",
                    XamlRoot = XamlRoot,
                    Content = "Do you really want to remove " + selectedItem.Name + "?"
                };

                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    IReadOnlyList<CoreWebView2BrowserExtension> list = await GetExtensionListAsync();
                    foreach (CoreWebView2BrowserExtension extension in list)
                    {
                        if (extension.Id == selectedItem.Id)
                        {
                            await extension.RemoveAsync();
                        }
                    }
                    RefreshExtensionsList();
                }
                break;
        }
    }

    private void VersionTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as TextBlock).Text = AppVersionHelper.GetAppVersion();
    }

    private async void SettingsCardClickHandler(object sender, RoutedEventArgs e)
    {
        switch ((sender as SettingsCard).Tag)
        {
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

                ContentDialog BuildInfoDialog = new()
                {
                    Title = "Horizon debug info",
                    Content = new TextBlock { Text = debugCombinedString, IsTextSelectionEnabled = true },
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };
                await BuildInfoDialog.ShowAsync();
                break;
        }
    }
}
