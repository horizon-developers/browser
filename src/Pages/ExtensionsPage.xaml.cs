namespace Horizon.Pages;

public sealed partial class ExtensionsPage : Page
{
    private WebView2 HeadlessWebViewInstance = new();
    public ExtensionsPage()
    {
        this.InitializeComponent();
        InitHeadless();

    }

    private async void InitHeadless()
    {
        CoreWebView2EnvironmentOptions options = new()
        {
            AreBrowserExtensionsEnabled = true,
            ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay
        };
        CoreWebView2Environment environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);
        await HeadlessWebViewInstance.EnsureCoreWebView2Async(environment);
        /*HeadlessWebViewInstance.CoreWebView2.NewWindowRequested += (sender, args) =>
        {
            MainPage page = WindowHelper.GetMainPageContentForWindow(App.WindowHelper.MainWindow);
            page.CreateWebTab("Tab", args.Uri);
            args.Handled = true;
        };*/
    }

    private async Task<IReadOnlyList<CoreWebView2BrowserExtension>> GetExtensionListAsync()
    {
        IReadOnlyList<CoreWebView2BrowserExtension> extensions = await HeadlessWebViewInstance.CoreWebView2.Profile.GetBrowserExtensionsAsync();
        foreach (CoreWebView2BrowserExtension extension in extensions)
        {
            System.Diagnostics.Debug.WriteLine(extension.Name + ": " + extension.Id);
        }
        return extensions;
    }

    private async void InstallExButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker
        FolderPicker openPicker = new();

        // See the sample code below for how to make the window accessible from the App class.
        var window = WindowHelper.MainWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the folder picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            //PickFolderOutputTextBlock.Text = "Picked folder: " + folder.Name;
            try
            {
                await HeadlessWebViewInstance.CoreWebView2.Profile.AddBrowserExtensionAsync(folder.Path);
            }
            catch (Exception ex)
            {
                Win32Helper.ShowMessageBox("Horizon", "Installation failed because:\n" + ex.Message);
            }
            RefreshExtensionsList();
        }
        else
        {
            //PickFolderOutputTextBlock.Text = "Operation cancelled.";
        }


    }

    private void ListExtension_Click(object sender, RoutedEventArgs e)
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
}
