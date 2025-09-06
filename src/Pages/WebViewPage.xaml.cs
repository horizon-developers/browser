namespace Horizon.Pages;

public sealed partial class WebViewPage : Page
{
    string launchurl;
    private Tab MyTab { get; set; }

    public WebViewPage(WebTabCreationParams parameters)
    {
        this.InitializeComponent();
        ProcessParameters(parameters);
    }

    private void ProcessParameters(WebTabCreationParams parameters)
    {
        launchurl = parameters.LaunchURL;
        MyTab = parameters.MyTab;

        if (launchurl == string.Empty)
        {
            UrlBoxWrapper.Visibility = Visibility.Visible;
        }
    }

    private async void WebViewControl_Loaded(object sender, RoutedEventArgs e)
    {
        if ((sender as WebView2).CoreWebView2 == null)
        {
            try
            {
                await (sender as WebView2).EnsureCoreWebView2Async(await GlobalEnvironment.GetDefault());
            }
            catch (Exception ex)
            {
                WebViewControl?.Close();
                Frame.Navigate(typeof(WebViewErrorPage), new WebView2Error(ex.StackTrace), new DrillInNavigationTransitionInfo());
            }
        }
    }

    private async void WebViewControl_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        // WebViewEvents
        sender.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        sender.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        sender.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        sender.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
        sender.CoreWebView2.IsDocumentPlayingAudioChanged += CoreWebView2_IsDocumentPlayingAudioChanged;
        sender.CoreWebView2.IsMutedChanged += CoreWebView2_IsMutedChanged;
        sender.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
        sender.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
        //sender.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
        sender.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;
        sender.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        string mainscript = "document.addEventListener(\"keydown\",function(e){e.ctrlKey&&\"l\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlL\")),e.ctrlKey&&\"t\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlT\"))});";
        await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(mainscript);
        sender.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
        if (launchurl != string.Empty && launchurl != null)
        {
            sender.Source = new Uri(launchurl);
            WebViewControl.Visibility = Visibility.Visible;
            return;
        }
        UrlBox.Focus(FocusState.Keyboard);
    }

    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        LoadingBar.IsIndeterminate = true;
        LoadingBar.Visibility = Visibility.Visible;
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        LoadingBar.Visibility = Visibility.Collapsed;
        LoadingBar.IsIndeterminate = false;
    }

    private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        WindowHelper.CreateNewTabInMainWindow("New tab", args.Uri);
    }

    string SelectionText;
    string LinkUri;
    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        if (SettingsHelper.GetSetting("AdvancedCTX") != "true")
        {
            MenuFlyout flyout;
            if (args.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.SelectedText)
            {
                flyout = (MenuFlyout)Resources["TextContextMenu"];
                SelectionText = args.ContextMenuTarget.SelectionText;
            }

            else if (args.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.Image)
                flyout = null;

            else if (args.ContextMenuTarget.HasLinkUri)
            {
                flyout = (MenuFlyout)Resources["LinkContextMenu"];
                SelectionText = args.ContextMenuTarget.LinkText;
                LinkUri = args.ContextMenuTarget.LinkUri;
            }

            else if (args.ContextMenuTarget.IsEditable)
                flyout = null;

            else
                flyout = (MenuFlyout)Resources["ContextMenu"];

            if (flyout != null)
            {
                FlyoutBase.SetAttachedFlyout(WebViewControl, flyout);
                var wv2flyout = FlyoutBase.GetAttachedFlyout(WebViewControl);
                var options = new FlyoutShowOptions()
                {
                    Position = args.Location,
                };
                wv2flyout?.ShowAt(WebViewControl, options);
                args.Handled = true;
            }
        }
    }

    private void CoreWebView2_IsDocumentPlayingAudioChanged(CoreWebView2 sender, object args)
    {
        switch (sender.IsDocumentPlayingAudio)
        {
            case true:
                MuteBtn.Visibility = Visibility.Visible;
                break;
            case false:
                MuteBtn.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void CoreWebView2_IsMutedChanged(CoreWebView2 sender, object args)
    {
        if (sender.IsMuted)
        {
            MuteBtn.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE74F" };
            return;
        }
        if (!sender.IsMuted)
        {
            MuteBtn.Content = new FontIcon { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\uE767" };
        }
    }

    private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
    {
        MyTab.Domain = sender.DocumentTitle;
        if (Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri? uri))
        {
            MyTab.Domain = uri.Host;
            return;
        }
        
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        MyTab.Title = sender.DocumentTitle;
    }

    /*private void CoreWebView2_FaviconChanged(CoreWebView2 sender, object args)
    {
        MyTab.Icon = new Uri(sender.FaviconUri);
    }*/

    private void CoreWebView2_ContainsFullScreenElementChanged(CoreWebView2 sender, object args)
    {
        bool fs = WindowHelper.IsWindowInFullScreen();
        if (!fs)
        {
            WindowHelper.SetFullScreen(true);
        }
        else
        {
            WindowHelper.SetFullScreen(false);
        }
    }

    #region Keyboard shortcuts
    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (WindowHelper.IsWindowInFullScreen())
        {
            return;
        }
        // this input has been treated as VERY unsecure input
        // DO NOT add anything which could be slighly insecure
        if (args.TryGetWebMessageAsString() == "ControlL")
        {
            ToggleUrlBox();
            return;
        }
        if (args.TryGetWebMessageAsString() == "ControlT")
        {
            WindowHelper.CreateNewTabInMainWindow("New tab", string.Empty);
            return;
        }
    }
    #endregion

    private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem).Tag)
        {
            // general context menu
            case "Back":
                if (WebViewControl.CanGoBack)
                    WebViewControl.GoBack();
                break;
            case "Refresh":
                WebViewControl.Reload();
                break;
            case "Forward":
                if (WebViewControl.CanGoForward)
                    WebViewControl.GoForward();
                break;
            case "SelectAll":
                await WebViewControl.CoreWebView2.ExecuteScriptAsync("document.execCommand(\"selectAll\");");
                break;
            case "Print":
                WebViewControl.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                break;
            case "Save":
                await WebViewControl.CoreWebView2.ShowSaveAsUIAsync();
                break;
            // text context menu
            case "OpenLnkInNewTab":
                WindowHelper.CreateNewTabInMainWindow("New tab", LinkUri);
                break;
            case "Copy":
                ClipboardHelper.CopyTextToClipboard(LinkUri);
                break;
            case "CopyText":
                ClipboardHelper.CopyTextToClipboard(SelectionText);
                break;
            // link context menu
            case "Search":
                string link = SettingsHelper.CurrentSearchUrl + SelectionText;
                WindowHelper.CreateNewTabInMainWindow("New tab", link);
                break;
            case "DevTools":
                WebViewControl.CoreWebView2.OpenDevToolsWindow();
                break;
            case "ViewSource":
                WindowHelper.CreateNewTabInMainWindow($"View source", $"view-source:{WebViewControl.CoreWebView2.Source}");
                break;
            case "TaskManager":
                WebViewControl.CoreWebView2.OpenTaskManagerWindow();
                break;
        }
        var flyout = FlyoutBase.GetAttachedFlyout(WebViewControl);
        flyout?.Hide();
    }


    private void UrlBoxKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.KeyboardAccelerator.Key == WS.VirtualKey.Escape)
        {
            UrlBoxWrapper.Visibility = Visibility.Collapsed;
            WebViewControl.Focus(FocusState.Keyboard);
            args.Handled = true;
            return;
        }
        if (args.KeyboardAccelerator.Key == WS.VirtualKey.Enter)
        {
            ProcessQueryAndGo(UrlBox.Text);
            UrlBoxWrapper.Visibility = Visibility.Collapsed;
            args.Handled = true;
            return;
        }
    }

    private void ProcessQueryAndGo(string input)
    {
        if (WebViewControl.Visibility != Visibility.Visible)
            WebViewControl.Visibility = Visibility.Visible;
        string inputtype = UrlHelper.GetInputType(input);
        if (inputtype == "urlNOProtocol")
            NavigateToUrl("https://" + input.Trim());
        else if (inputtype == "url")
            NavigateToUrl(input.Trim());
        else
        {
            string query = SettingsHelper.CurrentSearchUrl + input;
            NavigateToUrl(query);
        }
    }

    private void UrlBox_GotFocus(object sender, RoutedEventArgs e)
    {
        UrlBox.SelectAll();
    }

    private void NavigateToUrl(string uri)
    {
        WebViewControl.CoreWebView2.Navigate(uri);
    }

    byte[] QrCode;

    private async void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Tag)
        {
            case "Back":
                WebViewControl.GoBack();
                break;
            case "Refresh":
                WebViewControl.Reload();
                break;
            case "ToggleUrlBox":
                ToggleUrlBox();
                break;
            case "Forward":
                WebViewControl.GoForward();
                break;
            case "ReadingMode":
                string jscript = await Modules.Readability.ReadabilityHelper.GetReadabilityScriptAsync();
                await WebViewControl.CoreWebView2.ExecuteScriptAsync(jscript);
                break;
            case "Translate":
                string url = WebViewControl.CoreWebView2.Source;
                WebViewControl.CoreWebView2.Navigate("https://translate.google.com/translate?hl&u=" + url);
                break;
            case "AddFavoriteFlyout":
                FavoriteTitle.Text = WebViewControl.CoreWebView2.DocumentTitle;
                FavoriteUrl.Text = WebViewControl.CoreWebView2.Source;
                break;
            case "Downloads":
                WebViewControl.CoreWebView2.OpenDefaultDownloadDialog();
                break;
            case "GenQRCode":
                _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    QrCode = await Modules.QRCodeGen.QRCodeHelper.GenerateQRCodeFromUrlAsync(WebViewControl.CoreWebView2.Source);
                    if (QrCode != null)
                    {
                        BitmapImage QrCodeImage = await Modules.QRCodeGen.QRCodeHelper.ConvertBitmapBytesToImage(QrCode);
                        QRCodeImage.Source = QrCodeImage;
                        QRCodeFlyout.ShowAt(sender as Button);
                    }
                });
                break;
            case "Mute":
                WebViewControl.CoreWebView2.IsMuted = !WebViewControl.CoreWebView2.IsMuted;
                break;
        }
    }

    private void AddFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        FavoritesHelper.AddFavorite(FavoriteTitle.Text, FavoriteUrl.Text);
        AddFavoriteFlyout.Hide();
    }

    private async void QRCodeButton_Click(object sender, RoutedEventArgs e)
    {
        await FileHelper.SaveBytesAsFileAsync("QRCode", QrCode, "Bitmap", ".bmp");
    }

    public void ToggleUrlBox()
    {
        switch(UrlBoxWrapper.Visibility)
        {
            case Visibility.Visible:
                UrlBoxWrapper.Visibility = Visibility.Collapsed;
                WebViewControl.Focus(FocusState.Keyboard);
                break;

            case Visibility.Collapsed:
                if (UrlBox.Text.Length < 1 && WebViewControl.CoreWebView2.Source != "about:blank")
                {
                    UrlBox.Text = WebViewControl.CoreWebView2.Source;
                }
                UrlBoxWrapper.Visibility = Visibility.Visible;
                UrlBox.Focus(FocusState.Keyboard);
                break;
        }
    }

    /*private async void OpenUrlFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        UrlBox.Text = await ClipboardHelper.PasteUriAsStringFromClipboardAsync();
        UrlBox.Focus(FocusState.Keyboard);
    }

    private void RestoreClosedTab_Click(object sender, RoutedEventArgs e)
    {

    }*/
}
