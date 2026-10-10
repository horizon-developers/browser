namespace Horizon.Controls.WebContent;

public sealed partial class WebContentHost : Page, IDisposable
{
    private WebContentHostViewModel WCHVM { get; set; } = new();
    private AdBlockEngine? _adBlockEngine;
    private TaskCompletionSource<CoreWebView2>? _newWindowTarget;
    private bool _isWebViewClosed;

    /// <summary>
    /// The page currently shown. Falls back to the url the tab was opened with while the WebView2 is still starting,
    /// failed to start or has been closed, so this never throws.
    /// </summary>
    public string? CurrentUrl => (_isWebViewClosed ? null : WebContentControl?.CoreWebView2?.Source) ?? WCHVM.LaunchUrl;

    public WebContentHost(TabCreationParams parameters)
    {
        this.InitializeComponent();
        ProcessParameters(parameters);
    }

    private void ProcessParameters(TabCreationParams parameters)
    {
        WCHVM.LaunchUrl = parameters.LaunchUrl;
        WCHVM.MyTab = parameters.MyTab;
        _newWindowTarget = parameters.NewWindowTarget;

        if (WCHVM.LaunchUrl == string.Empty)
        {
            UrlBoxWrapper.Visibility = Visibility.Visible;
        }
    }

    private async void WebContentControl_Loaded(object sender, RoutedEventArgs e)
    {
        if ((sender as WebView2)?.CoreWebView2 == null)
        {
            try
            {
                CoreWebView2Environment environment = await GlobalEnvironment.GetDefault();

                if (WCHVM.MyTab.IsInPrivate)
                {
                    var options = environment.CreateCoreWebView2ControllerOptions();
                    options.IsInPrivateModeEnabled = true;
                    await (sender as WebView2)?.EnsureCoreWebView2Async(environment, options);
                }
                else
                {
                    await (sender as WebView2)?.EnsureCoreWebView2Async(environment);
                }
            }
            catch (Exception ex)
            {
                _newWindowTarget?.TrySetException(ex);
                _isWebViewClosed = true;
                WebContentControl?.Close();
                TextBlock ErrorTextBlock = new()
                {
                    Text = $"A critical error occured while trying to load the content\n\n{ex.Message}\n\nStackTrace\n\n{ex.StackTrace}",
                    IsTextSelectionEnabled = true
                };
                Content = ErrorTextBlock;
            }
        }
    }

    private async void WebContentControl_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
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
        sender.CoreWebView2.WindowCloseRequested += CoreWebView2_WindowCloseRequested;

        // Attach before the first navigation below so the very first request is already filtered.
        // Applies to InPrivate tabs as well - tracker blocking matters most there.
        if (AppSettings.Current.BlockAds)
        {
            _adBlockEngine = new AdBlockEngine();
            _adBlockEngine.Attach(sender.CoreWebView2);
        }

        string mainscript = "document.addEventListener(\"keydown\",function(e){e.ctrlKey&&\"l\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlL\")),e.ctrlKey&&\"t\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlT\"))});";
        await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(mainscript);
        
        if (!AppSettings.Current.DisableWebPageDarkening)
        {
            sender.DefaultBackgroundColor = Colors.Transparent;
        }

        if (_newWindowTarget != null)
        {
            // The opener's NewWindowRequested navigates us once it receives this CoreWebView2,
            // navigating here first would make WebView2 reject it as the new window
            WebContentControl.Visibility = Visibility.Visible;
            _newWindowTarget.TrySetResult(sender.CoreWebView2);
            return;
        }

        if (WCHVM.LaunchUrl != string.Empty && WCHVM.LaunchUrl != null)
        {
            sender.Source = new Uri(WCHVM.LaunchUrl);
            WebContentControl.Visibility = Visibility.Visible;
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

    private async void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        // Hand the page the new tab's CoreWebView2 instead of just opening the url in it, so window.opener survives.
        // Login popups (Sign in with Google/Microsoft/Apple) post their result back to the opener through it.
        args.Handled = true;
        Windows.Foundation.Deferral deferral = args.GetDeferral();
        try
        {
            TaskCompletionSource<CoreWebView2> newWindowTarget = new(TaskCreationOptions.RunContinuationsAsynchronously);
            WindowHelper.CreateNewTabInMainWindow("New tab", args.Uri, WCHVM.MyTab, WCHVM.MyTab.IsInPrivate, newWindowTarget);
            args.NewWindow = await newWindowTarget.Task;
        }
        catch (Exception ex)
        {
            // The new tab failed to create its WebView2 (it shows the error itself) or was closed before it could
            Logger.LogEvent(Logger.Severity.Warning, "WebContentHost", $"Could not open new window for {args.Uri}: {ex.Message}");
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void CoreWebView2_WindowCloseRequested(CoreWebView2 sender, object args)
    {
        // window.close(), e.g. a login popup that is done. Deferred, closing the WebView2 inside its own event is unsafe
        if (WCHVM.MyTab != null)
        {
            WindowHelper.CloseTabInMainWindow(WCHVM.MyTab);
        }
    }

    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        if (AppSettings.Current.UseAdvancedContextMenu)
        {
            return;
        }
        MenuFlyout? flyout;
        if (args.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.SelectedText)
        {
            flyout = (MenuFlyout)Resources["TextContextMenu"];
            WCHVM.SelectionText = args.ContextMenuTarget.SelectionText;
        }

        else if (args.ContextMenuTarget.Kind == CoreWebView2ContextMenuTargetKind.Image)
            flyout = null;

        else if (args.ContextMenuTarget.HasLinkUri)
        {
            flyout = (MenuFlyout)Resources["LinkContextMenu"];
            WCHVM.SelectionText = args.ContextMenuTarget.LinkText;
            WCHVM.LinkUri = args.ContextMenuTarget.LinkUri;
        }

        else if (args.ContextMenuTarget.IsEditable)
            flyout = null;

        else
            flyout = (MenuFlyout)Resources["ContextMenu"];

        if (flyout != null)
        {
            FlyoutBase.SetAttachedFlyout(WebContentControl, flyout);
            var wv2flyout = FlyoutBase.GetAttachedFlyout(WebContentControl);
            var options = new FlyoutShowOptions()
            {
                Position = args.Location,
            };
            wv2flyout?.ShowAt(WebContentControl, options);
            args.Handled = true;
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
        if (Uri.TryCreate(sender.Source, UriKind.Absolute, out Uri? uri))
        {
            string DomainText = uri.Host;
            if (uri.Host.StartsWith("www"))
            {
                DomainText = uri.Host.Replace("www.", "");
            }
            WCHVM.MyTab?.Domain = DomainText;
            return;
        }
        
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (WCHVM.MyTab.IsInPrivate)
        {
            WCHVM.MyTab?.Title = $"InPrivate: {sender.DocumentTitle}";
            return;
        }
        WCHVM.MyTab?.Title = sender.DocumentTitle;
    }

    /*private void CoreWebView2_FaviconChanged(CoreWebView2 sender, object args)
    {
        MyTab.Icon = new Uri(sender.FaviconUri);
    }*/

    private void CoreWebView2_ContainsFullScreenElementChanged(CoreWebView2 sender, object args)
    {
        // Follow what the page actually requested instead of toggling, otherwise window and page can drift apart
        bool fs = sender.ContainsFullScreenElement;
        WindowHelper.SetFullScreen(fs);
        SetFullScreenLayout(fs);
    }

    /// <summary>
    /// Hides the toolbar and strips the rounded border around the WebView2 so the page can cover the whole screen,
    /// or brings the regular layout back
    /// </summary>
    private void SetFullScreenLayout(bool fullScreen)
    {
        if (fullScreen)
        {
            ToolbarHost.Visibility = Visibility.Collapsed;
            UrlBoxWrapper.Visibility = Visibility.Collapsed;
            WebContentBorder.Margin = new Thickness(0);
            WebContentBorder.BorderThickness = new Thickness(0);
            WebContentBorder.CornerRadius = new CornerRadius(0);
        }
        else
        {
            ToolbarHost.Visibility = Visibility.Visible;
            // Clearing the local values hands these back to the PageBorder style
            WebContentBorder.ClearValue(MarginProperty);
            WebContentBorder.ClearValue(Grid.BorderThicknessProperty);
            WebContentBorder.ClearValue(Grid.CornerRadiusProperty);
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
            WindowHelper.CreateNewTabInMainWindow("New tab", string.Empty, WCHVM.MyTab, WCHVM.MyTab.IsInPrivate);
            return;
        }
    }
    #endregion

    private async void ContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem)?.Tag)
        {
            // general context menu
            case "Back":
                if (WebContentControl.CanGoBack)
                    WebContentControl.GoBack();
                break;
            case "Refresh":
                WebContentControl.Reload();
                break;
            case "Forward":
                if (WebContentControl.CanGoForward)
                    WebContentControl.GoForward();
                break;
            case "SelectAll":
                await WebContentControl.CoreWebView2.ExecuteScriptAsync("document.execCommand(\"selectAll\");");
                break;
            case "Print":
                WebContentControl.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                break;
            case "Share":
                ShareHelper.Share(WebContentControl.CoreWebView2.DocumentTitle, WebContentControl.Source);
                break;
            case "Save":
                await WebContentControl.CoreWebView2.ShowSaveAsUIAsync();
                break;
            // text context menu
            case "OpenLnkInNewTab":
                if (!string.IsNullOrEmpty(WCHVM.LinkUri))
                {
                    WindowHelper.CreateNewTabInMainWindow("New tab", WCHVM.LinkUri, WCHVM.MyTab, WCHVM.MyTab.IsInPrivate);
                }
                break;
            case "Copy":
                if (!string.IsNullOrEmpty(WCHVM.LinkUri))
                {
                    ClipboardHelper.CopyTextToClipboard(WCHVM.LinkUri);
                }
                break;
            case "CopyText":
                if (!string.IsNullOrEmpty(WCHVM.SelectionText))
                {
                    ClipboardHelper.CopyTextToClipboard(WCHVM.SelectionText);
                }
                break;
            // link context menu
            case "Search":
                if (!string.IsNullOrEmpty(WCHVM.SelectionText))
                {
                    string link = UrlHelper.FormatSearchQuery(AppSettings.Current.SearchUrl, WCHVM.SelectionText);
                    WindowHelper.CreateNewTabInMainWindow("New tab", link, WCHVM.MyTab, WCHVM.MyTab.IsInPrivate);
                }
                break;
            case "DevTools":
                WebContentControl.CoreWebView2.OpenDevToolsWindow();
                break;
            case "ViewSource":
                WindowHelper.CreateNewTabInMainWindow($"View source", $"view-source:{WebContentControl.CoreWebView2.Source}", WCHVM.MyTab, WCHVM.MyTab.IsInPrivate);
                break;
            case "TaskManager":
                WebContentControl.CoreWebView2.OpenTaskManagerWindow();
                break;
            case "ShareCTXLink":
                if (Uri.TryCreate(WCHVM.LinkUri, UriKind.Absolute, out Uri? uri))
                {
                    ShareHelper.Share("Shared link", uri);
                }
                break;
        }
        var flyout = FlyoutBase.GetAttachedFlyout(WebContentControl);
        flyout?.Hide();
    }


    private void UrlBoxKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.KeyboardAccelerator.Key == WS.VirtualKey.Escape)
        {
            UrlBoxWrapper.Visibility = Visibility.Collapsed;
            WebContentControl.Focus(FocusState.Keyboard);
            args.Handled = true;
            return;
        }
    }

    private void NavigateToUrl(string uri)
    {
        if (WebContentControl.Visibility != Visibility.Visible)
            WebContentControl.Visibility = Visibility.Visible;
        WebContentControl.CoreWebView2.Navigate(uri);
    }

    private async void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button)?.Tag)
        {
            case "Back":
                WebContentControl.GoBack();
                break;
            case "Refresh":
                WebContentControl.Reload();
                break;
            case "ToggleUrlBox":
                ToggleUrlBox();
                break;
            case "Forward":
                WebContentControl.GoForward();
                break;
            
            case "Mute":
                WebContentControl.CoreWebView2.IsMuted = !WebContentControl.CoreWebView2.IsMuted;
                break;
        }
    }

    public async void ForwardedEvent(string EventString)
    {
        switch (EventString)
        {
            case "ReadingMode":
                string jscript = await Modules.Readability.ReadabilityHelper.GetReadabilityScriptAsync();
                await WebContentControl.CoreWebView2.ExecuteScriptAsync(jscript);
                break;
            case "Translate":
#if DEBUG
                System.Diagnostics.Debug.WriteLine(WebContentControl.CoreWebView2.Source);
#endif
                if (WebContentControl.CoreWebView2.Source == "about:blank")
                    break;

                if (!AppSettings.Current.TranslatePrivacyAccepted)
                {
                    StackPanel contentPanel = new();
                    contentPanel.Children.Add(new TextBlock
                    {
                        Text = "Do you really want to translate this site using Google?\nAll data from this website will be transmitted to Google for translation.",
                        TextWrapping = TextWrapping.Wrap
                    });

                    HyperlinkButton privacyLink = new()
                    {
                        Content = "Verify Google Privacy Policy",
                        NavigateUri = new Uri("https://policies.google.com/privacy"),
                        Margin = new Thickness(-11, 10, 0, 0)
                    };
                    contentPanel.Children.Add(privacyLink);

                    ContentDialog dialog = new()
                    {
                        Title = "Translate?",
                        Content = contentPanel,
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        PrimaryButtonText = "Translate",
                        SecondaryButtonText = "Translate & Don't show again",
                        XamlRoot = this.XamlRoot
                    };
                    var result = await dialog.ShowAsync();
                    switch (result)
                    {
                        case ContentDialogResult.Primary:
                            Translate();
                            break;
                        case ContentDialogResult.Secondary:
                            Translate();
                            AppSettings.Current.TranslatePrivacyAccepted = true;
                            break;
                        default:
                            break;
                    }
                    break;
                }
                Translate();
                break;
            case "AddFavoriteFlyout":
                FavoriteTitle.Text = WebContentControl.CoreWebView2.DocumentTitle;
                FavoriteUrl.Text = WebContentControl.CoreWebView2.Source;
                AddFavoriteFlyout.ShowAt(FlyoutHostBtn);
                break;
            case "Downloads":
                WebContentControl.CoreWebView2.OpenDefaultDownloadDialog();
                break;
            case "GenQRCode":
                _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    WCHVM.QrCode = await QRCodeHelper.GenerateQRCodeFromUrlAsync(WebContentControl.CoreWebView2.Source);
                    if (WCHVM.QrCode != null)
                    {
                        BitmapImage QrCodeImage = await QRCodeHelper.ConvertBitmapBytesToImage(WCHVM.QrCode);
                        QRCodeImage.Source = QrCodeImage;
                        QRCodeFlyout.ShowAt(FlyoutHostBtn);
                    }
                });
                break;
        }
    }

    private void Translate()
    {
        WebContentControl.CoreWebView2.Navigate("https://translate.google.com/translate?hl&u=" + Uri.EscapeDataString(WebContentControl.CoreWebView2.Source));
    }

    private void AddFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        FavoritesHelper.AddFavorite(FavoriteTitle.Text, FavoriteUrl.Text);
        AddFavoriteFlyout.Hide();
    }

    private async void QRCodeButton_Click(object sender, RoutedEventArgs e)
    {
        string QRFileName = $"QRCode - {WebContentControl.CoreWebView2.DocumentTitle}";
        string QRFileNameNor = FileNameHelper.ToValidFileName(QRFileName);
        if (WCHVM.QrCode == null)
        {
            return;
        }
        await FileHelper.SaveBytesAsFileAsync(QRFileNameNor, WCHVM.QrCode, "Bitmap", ".bmp");
        /*_ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
        {
            DevWinUI.Growl.Success(new DevWinUI.GrowlInfo
            {
                ShowDateTime = false,
                Title = "Success",
                Message = $"Saved QRCode to\n{path}"
            });
        });*/

    }

    public void ToggleUrlBox()
    {
        switch(UrlBoxWrapper.Visibility)
        {
            case Visibility.Visible:
                UrlBoxWrapper.Visibility = Visibility.Collapsed;
                WebContentControl.Focus(FocusState.Keyboard);
                break;

            case Visibility.Collapsed:
                if (UrlBox.Text.Length < 1 && WebContentControl.CoreWebView2.Source != "about:blank")
                {
                    UrlBox.Text = WebContentControl.CoreWebView2.Source;
                }
                UrlBoxWrapper.Visibility = Visibility.Visible;
                UrlBox.Focus(FocusState.Keyboard);
                UrlBox.FindDescendant<TextBox>()?.SelectAll();
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

    private System.Threading.CancellationTokenSource? _cts;
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Mark members as static
    private async void AddressBar_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var rawText = sender.Text;

        if (string.IsNullOrWhiteSpace(rawText))
        {
            _cts?.Cancel();
            sender.ItemsSource = null;
            return;
        }

        var oldCts = _cts;
        _cts = new System.Threading.CancellationTokenSource();
        var token = _cts.Token;

        oldCts?.Cancel();
        oldCts?.Dispose();

        

        try
        {
            await Task.Delay(100, token);

            var query = rawText.Trim();

            var suggestions = new List<SuggestionItem>(7);

            Uri? url = UrlHelper.SanitizeUrl(query);

            _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                if (url?.IsFile == true)
                {
                    suggestions.Add(new SuggestionItem
                    {
                        DisplayIcon = Symbol.Folder,
                        DisplayText = $"Open local file {query}",
                        Command = SuggestionCommand.LocalFile,
                        Value = query
                    });
                }
                else if (url is not null)
                {
                    suggestions.Add(new SuggestionItem
                    {
                        DisplayIcon = Symbol.Globe,
                        DisplayText = $"Visit {query}",
                        Command = SuggestionCommand.GoToUrl,
                        Value = query
                    });
                }

                // Address matches sit above the web search because that is what Enter opens for plain text.
                // A typed url still gets the "Visit" entry first, so the top row always shows what Enter does.
                suggestions.AddRange(FavoriteSuggestions.FindByAddress(query));

                suggestions.Add(new SuggestionItem
                {
                    DisplayIcon = Symbol.Find,
                    DisplayText = $"Search the web for \"{query}\"",
                    Command = SuggestionCommand.SearchWeb,
                    Value = query
                });

                // Title-only matches go below the search so they have to be picked explicitly
                suggestions.AddRange(FavoriteSuggestions.FindByTitle(query));

                // Ensure we aren't updating the UI if a newer request came in during processing
                if (!token.IsCancellationRequested)
                {
                    sender.ItemsSource = suggestions;
                }
            });

            
        }
        catch (TaskCanceledException)
        {
            // Expected behavior when user types fast
        }
        catch (ObjectDisposedException)
        {
            // Handle race condition where CTS is disposed
        }
    }

    /*private void AddressBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SuggestionItem item)
        {
            ExecuteSuggestion(item);
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "AddressBar", $"Executed suggestion: {item.DisplayText} {item.Command}");
#endif
        }
    }*/

    private void AddressBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        UrlBoxWrapper.Visibility = Visibility.Collapsed;
        if (args.ChosenSuggestion is SuggestionItem item)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "AddressBar", $"Query submitted, thus executed suggestion: {item.DisplayText} {item.Command}");
#endif
            ExecuteSuggestion(item);
            return;
        }
        var query = sender.Text.Trim();

        // Ctrl+Enter turns a bare name into a .com address ("example" opens https://example.com/)
        if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(WS.VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            ProcessQueryAndGo(query, appendTld: true);
            return;
        }

        // Only plain text that would otherwise become a web search - a typed url always wins.
        // Re-evaluated here instead of reading ItemsSource, which lags behind the debounce when Enter follows typing quickly.
        if (UrlHelper.SanitizeUrl(query) is null &&
            FavoriteSuggestions.NavigationTarget(query) is SuggestionItem favorite)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "AddressBar", $"Query submitted, opening matching favorite: {favorite.DisplayText} {favorite.Value}");
#endif
            ExecuteSuggestion(favorite);
            return;
        }
        ProcessQueryAndGo(query);
    }

    private void ExecuteSuggestion(SuggestionItem item)
    {
        switch (item.Command)
        {
            case SuggestionCommand.GoToUrl:
                ProcessQueryAndGo(item.Value);
                break;
            case SuggestionCommand.SearchWeb:
                string query = UrlHelper.FormatSearchQuery(AppSettings.Current.SearchUrl, item.Value);
#if DEBUG
                Logger.LogEvent(Logger.Severity.Info, "AddressBar", $"Searching the web for {item.Value}");
#endif
                NavigateToUrl(query);
                break;
            case SuggestionCommand.LocalFile:
                ProcessQueryAndGo(item.Value);
                break;
            case SuggestionCommand.OpenFavorite:
                // ProcessQueryAndGo adds a scheme to favorites saved without one
                ProcessQueryAndGo(item.Value);
                break;
        }
    }

    private void ProcessQueryAndGo(string input, bool appendTld = false)
    {
        NavigateToUrl(UrlHelper.ToNavigationUrl(input, AppSettings.Current.SearchUrl, appendTld));
    }

    public void Dispose()
    {
        if (WebContentControl?.CoreWebView2 != null)
        {
            WebContentControl.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
            WebContentControl.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            WebContentControl.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            WebContentControl.CoreWebView2.ContextMenuRequested -= CoreWebView2_ContextMenuRequested;
            WebContentControl.CoreWebView2.IsDocumentPlayingAudioChanged -= CoreWebView2_IsDocumentPlayingAudioChanged;
            WebContentControl.CoreWebView2.IsMutedChanged -= CoreWebView2_IsMutedChanged;
            WebContentControl.CoreWebView2.SourceChanged -= CoreWebView2_SourceChanged;
            WebContentControl.CoreWebView2.DocumentTitleChanged -= CoreWebView2_DocumentTitleChanged;
            WebContentControl.CoreWebView2.ContainsFullScreenElementChanged -= CoreWebView2_ContainsFullScreenElementChanged;
            WebContentControl.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            WebContentControl.CoreWebView2.WindowCloseRequested -= CoreWebView2_WindowCloseRequested;
        }
        // Closed before the WebView2 was ready, let the opener stop waiting for us
        _newWindowTarget?.TrySetCanceled();
        _adBlockEngine?.Dispose();
        _adBlockEngine = null;
        _isWebViewClosed = true;
        WebContentControl?.Close();
    }
}
