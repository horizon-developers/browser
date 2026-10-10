namespace Horizon.Views;

public sealed partial class SettingsView : Page
{
    public readonly WebView2 HeadlessWebViewInstance = new();

    public SettingsView()
    {
        this.InitializeComponent();
        InitHeadless();
    }

    // The toggles bind to this two-way
    public AppSettings Settings => AppSettings.Current;

    // The backdrop picker lists these names, in this order
    private static readonly (BackdropKind Kind, string Name)[] BackdropOptions =
    [
        (BackdropKind.Acrylic, "Acrylic"),
        (BackdropKind.Mica, "Mica"),
        (BackdropKind.MicaAlt, "Mica Alt"),
        (BackdropKind.None, "None")
    ];

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        SearchEngineSelector.ItemsSource = SearchEngineHelper.SearchEngines;
        SearchEngineSelector.SelectedItem = SearchEngineHelper.SearchEngines.FirstOrDefault(engine => engine.SearchUrl == Settings.SearchUrl);

        BackdropTypeSelector.ItemsSource = BackdropOptions.Select(option => option.Name).ToList();
        BackdropTypeSelector.SelectedIndex = Array.FindIndex(BackdropOptions, option => option.Kind == Settings.Backdrop);

        // Attached after the initial selection, so showing the current values doesn't write them back
        SearchEngineSelector.SelectionChanged += SearchEngineSelector_SelectionChanged;
        BackdropTypeSelector.SelectionChanged += BackdropTypeSelector_SelectionChanged;
    }

    private async void InitHeadless()
    {
        await HeadlessWebViewInstance.EnsureCoreWebView2Async(await GlobalEnvironment.GetDefault());
        UpdateSetDownloadFolderSettingsCardDescription();
    }

    private async void UserPictureDisplay_Loaded(object sender, RoutedEventArgs e)
    {
        var pic = await UserHelper.GetUserPicture();
        ((PersonPicture)sender).ProfilePicture = pic;
    }

    private async void UserNameDisplay_Loaded(object sender, RoutedEventArgs e)
    {
        var name = await UserHelper.GetUserName();
        ((TextBlock)sender).Text = name;
    }

    private void SearchEngineSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchEngineSelector.SelectedItem is SearchEngine engine)
        {
            Settings.SearchUrl = engine.SearchUrl;
        }
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
        var senderButton = (Button)sender;
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
        if (BackdropTypeSelector.SelectedIndex >= 0)
        {
            // WindowHelper applies the new backdrop when the setting changes
            Settings.Backdrop = BackdropOptions[BackdropTypeSelector.SelectedIndex].Kind;
        }
    }

    // Not bound two-way: App Lock may only be turned on once Windows Hello is known to work
    private async void WindowsHelloToggle_Toggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn == Settings.IsAppLockEnabled)
        {
            // The one-way binding showing the current value, or our own reset below
            return;
        }
        if (!toggle.IsOn)
        {
            Settings.IsAppLockEnabled = false;
            return;
        }
        if (await WindowsHelloHelper.CheckAvailability() == UserConsentVerifierAvailability.Available)
        {
            Settings.IsAppLockEnabled = true;
            return;
        }
        Win32Helper.ShowMessageBox("Horizon", "An error occured while trying to setup App Lock using Windows Hello.\n\nPlease setup it in Windows Settings");
        toggle.IsOn = false;
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
            switch (extension.Name)
            {
                case "Microsoft Clipboard Extension":
                    continue;
                case "Microsoft Edge PDF Viewer":
                    continue;
                default:
                    ExtensionsListView.Items.Add(extension);
                    continue;
            }
            
        }
    }

    CoreWebView2BrowserExtension? selectedItem;
    private void ExtensionsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        selectedItem = ((FrameworkElement)e.OriginalSource).DataContext as CoreWebView2BrowserExtension;
    }
    #endregion

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (selectedItem == null)
        {
            return;
        }

        switch (((MenuFlyoutItem)sender).Tag)
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
        ((TextBlock)sender).Text = AppVersionHelper.GetAppVersion();
    }

    private async void SettingsCardClickHandler(object sender, RoutedEventArgs e)
    {
        switch (((SettingsCard)sender).Tag)
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
                string appver = AppVersionHelper.GetAppVersion();
                string apparch = RuntimeInformation.ProcessArchitecture.ToString();
                string dotnetver = Environment.Version.ToString();
                string sysarch = RuntimeInformation.OSArchitecture.ToString();
                string sysversion = Environment.OSVersion.VersionString;
                string appsdkversion = $"{Microsoft.WindowsAppSDK.Release.Major}.{Microsoft.WindowsAppSDK.Release.Minor}.{Microsoft.WindowsAppSDK.Release.Patch} ({Microsoft.WindowsAppSDK.Release.Channel})";
                string wv2version = CoreWebView2Environment.GetAvailableBrowserVersionString();

                string debugCombinedString = $"Horizon Version {appver} ({apparch})\n.NET Version {dotnetver}\nWinAppSdk Version {appsdkversion}\nSystem {sysversion} ({sysarch})\nWebView2Runtime Version {wv2version}\n";

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
