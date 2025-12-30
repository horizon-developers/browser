#nullable enable
using CommunityToolkit.WinUI;

namespace Horizon.Controls.WebContent;

public sealed partial class WebContentHost : Page
{
    private string? LaunchUrl { get; set; }
    private Tab? MyTab { get; set; }
    private bool IsInPrivate { get; set; }

    public WebContentHost(TabCreationParams parameters)
    {
        this.InitializeComponent();
        ProcessParameters(parameters);
    }

    private void ProcessParameters(TabCreationParams parameters)
    {
        LaunchUrl = parameters.LaunchUrl;
        MyTab = parameters.MyTab;
        IsInPrivate = parameters.IsInPrivate;

        if (LaunchUrl == string.Empty)
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

                if (IsInPrivate)
                {
                    var options = environment.CreateCoreWebView2ControllerOptions();
                    options.IsInPrivateModeEnabled = true;
                    await (sender as WebView2)?.EnsureCoreWebView2Async(environment, options);
                    return;
                }

                await (sender as WebView2)?.EnsureCoreWebView2Async(environment);
            }
            catch (Exception ex)
            {
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
        string mainscript = "document.addEventListener(\"keydown\",function(e){e.ctrlKey&&\"l\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlL\")),e.ctrlKey&&\"t\"===e.key&&(e.preventDefault(),window.chrome.webview.postMessage(\"ControlT\"))});";
        await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(mainscript);
        sender.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
        if (LaunchUrl != string.Empty && LaunchUrl != null)
        {
            sender.Source = new Uri(LaunchUrl);
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

    private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        WindowHelper.CreateNewTabInMainWindow("New tab", args.Uri);
    }

    string? SelectionText;
    string? LinkUri;
    private void CoreWebView2_ContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        if (SettingsHelper.GetSetting("AdvancedCTX") != "true")
        {
            MenuFlyout? flyout;
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
            if (uri.Host.Contains("www"))
            {
                DomainText = uri.Host.Replace("www.", "");
            }
            MyTab?.Domain = DomainText;
            return;
        }
        
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (IsInPrivate)
        {
            MyTab?.Title = $"InPrivate: {sender.DocumentTitle}";
            return;
        }
        MyTab?.Title = sender.DocumentTitle;
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
                if (!string.IsNullOrEmpty(LinkUri))
                {
                    WindowHelper.CreateNewTabInMainWindow("New tab", LinkUri);
                }
                break;
            case "Copy":
                if (!string.IsNullOrEmpty(LinkUri))
                {
                    ClipboardHelper.CopyTextToClipboard(LinkUri);
                }
                break;
            case "CopyText":
                if (!string.IsNullOrEmpty(SelectionText))
                {
                    ClipboardHelper.CopyTextToClipboard(SelectionText);
                }
                break;
            // link context menu
            case "Search":
                if (!string.IsNullOrEmpty(SelectionText))
                {
                    string link = SettingsHelper.CurrentSearchUrl + SelectionText;
                    WindowHelper.CreateNewTabInMainWindow("New tab", link);
                }
                break;
            case "DevTools":
                WebContentControl.CoreWebView2.OpenDevToolsWindow();
                break;
            case "ViewSource":
                WindowHelper.CreateNewTabInMainWindow($"View source", $"view-source:{WebContentControl.CoreWebView2.Source}");
                break;
            case "TaskManager":
                WebContentControl.CoreWebView2.OpenTaskManagerWindow();
                break;
            case "ShareCTXLink":
                if (Uri.TryCreate(LinkUri, UriKind.Absolute, out Uri? uri))
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

    byte[]? QrCode;

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
            case "ReadingMode":
                string jscript = await Modules.Readability.ReadabilityHelper.GetReadabilityScriptAsync();
                await WebContentControl.CoreWebView2.ExecuteScriptAsync(jscript);
                break;
            case "Translate":
                string url = WebContentControl.CoreWebView2.Source;
                WebContentControl.CoreWebView2.Navigate("https://translate.google.com/translate?hl&u=" + url);
                break;
            case "AddFavoriteFlyout":
                FavoriteTitle.Text = WebContentControl.CoreWebView2.DocumentTitle;
                FavoriteUrl.Text = WebContentControl.CoreWebView2.Source;
                break;
            case "Downloads":
                WebContentControl.CoreWebView2.OpenDefaultDownloadDialog();
                break;
            case "GenQRCode":
                _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                {
                    QrCode = await QRCodeHelper.GenerateQRCodeFromUrlAsync(WebContentControl.CoreWebView2.Source);
                    if (QrCode != null)
                    {
                        BitmapImage QrCodeImage = await QRCodeHelper.ConvertBitmapBytesToImage(QrCode);
                        QRCodeImage.Source = QrCodeImage;
                        QRCodeFlyout.ShowAt(sender as Button);
                    }
                });
                break;
            case "Mute":
                WebContentControl.CoreWebView2.IsMuted = !WebContentControl.CoreWebView2.IsMuted;
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
        string QRFileName = $"QRCode - {WebContentControl.CoreWebView2.DocumentTitle}";
        string QRFileNameNor = FileNameHelper.ToValidFileName(QRFileName);
        await FileHelper.SaveBytesAsFileAsync(QRFileNameNor, QrCode, "Bitmap", ".bmp");
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

    private static System.Threading.CancellationTokenSource? _cts;
    private static readonly char[] UrlIndicators = ['.', ':'];
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

            var suggestions = new List<SuggestionItem>(3);

            // 7. Regex Guard Clauses (CRITICAL OPTIMIZATION)
            // Regex is CPU expensive. Don't run it for plain text queries like "how to cook".
            // Only check if the string contains URL-like characters ('.' or ':')
            bool isUrlCandidate = query.IndexOfAny(UrlIndicators) >= 0;

            _ = WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                if (isUrlCandidate)
                {

                    // Use OrdinalIgnoreCase for faster string comparison
                    if (query.StartsWith("edge://", StringComparison.OrdinalIgnoreCase) ||
                        UrlHelper.IPRegex().IsMatch(query) ||
                        UrlHelper.UrlRegex().IsMatch(query))
                    {
                        suggestions.Add(new SuggestionItem
                        {
                            DisplayIcon = Symbol.Globe,
                            DisplayText = $"Visit {query}",
                            Command = SuggestionCommand.GoToUrl,
                            Value = query
                        });
                    }
                    else if (query.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(new SuggestionItem
                        {
                            DisplayIcon = Symbol.Folder,
                            DisplayText = $"Open local file {query}",
                            Command = SuggestionCommand.LocalFile,
                            Value = query
                        });
                    }
                }

                suggestions.Add(new SuggestionItem
                {
                    DisplayIcon = Symbol.Find,
                    DisplayText = $"Search the web for \"{query}\"",
                    Command = SuggestionCommand.SearchWeb,
                    Value = query
                });

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
        ProcessQueryAndGo(query);
    }

    private void ExecuteSuggestion(SuggestionItem item)
    {
        switch (item.Command)
        {
            case SuggestionCommand.GoToUrl:
                ProcessQueryAndGo(item.Value.ToLowerInvariant());
                break;
            case SuggestionCommand.SearchWeb:
                string query = SettingsHelper.CurrentSearchUrl + item.Value;
#if DEBUG
                Logger.LogEvent(Logger.Severity.Info, "AddressBar", $"Searching the web for {item.Value}");
#endif
                NavigateToUrl(query);
                break;
            case SuggestionCommand.LocalFile:
                ProcessQueryAndGo(item.Value.Replace("\"", ""));
                break;
        }
    }

    private void ProcessQueryAndGo(string input)
    {
        string inputtype = UrlHelper.GetInputType(input);
        if (inputtype == "urlNOProtocol")
            NavigateToUrl("https://" + input.Trim());
        else if (inputtype == "url")
        {
            if (input.StartsWith("file:///"))
            {
                NavigateToUrl(input.Replace("\"", ""));
                return;
            }
            NavigateToUrl(input.Trim());
        }
        else
        {
            string query = SettingsHelper.CurrentSearchUrl + input;
            NavigateToUrl(query);
        }
    }
}
