namespace Horizon;

/// <summary>
/// The main WindowChrome of the app. It displays the titlebar, the tab bar and the sidebar, as well as the the WebContent
/// </summary>
public sealed partial class WindowChrome : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public WindowChrome()
    {
        InitializeComponent();
        GetFavorites();
    }

    public static void GetFavorites()
    {
        SettingsViewModel.SettingsVM.FavoritesList = FavoritesHelper.GetFavoritesList();
#if DEBUG
        System.Diagnostics.Debug.WriteLine("Favorites:" + SettingsViewModel.SettingsVM.FavoritesList.Count);
#endif
    }

    public void CreateTab(string title, Type page, string launchurl = null)
    {
        Frame frame = new();

        Tab tab = new()
        {
            Title = title,
            Content = frame
        };

        if (launchurl != null)
        {
            WebTabCreationParams parameters = new()
            {
                LaunchURL = launchurl,
                MyTab = tab
            };

            frame.Navigate(page, parameters, new DrillInNavigationTransitionInfo());
        }
        else
        {
            frame.Navigate(page, tab, new DrillInNavigationTransitionInfo());
        }
        SettingsViewModel.SettingsVM.Tabs.Add(tab);
        TabListView.SelectedItem = tab;
    }

    public void CreateWebTab(string title, string launchurl)
    {
        CreateTab(title, typeof(WebViewPage), launchurl);
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListView listView = sender as ListView;
        Tab item = (Tab)listView.SelectedItem;
        SelectedTab = item;
        UIElement tabContent = item?.Content;
        TabContentPresenter.Content = tabContent;
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsViewModel.SettingsVM.Tabs.Count > 1)
        {
            var button = (Button)sender;
            var tab = (Tab)button.DataContext;
            int index = SettingsViewModel.SettingsVM.Tabs.IndexOf(tab);
            if ((tab.Content as Frame).Content is WebViewPage)
            {
                ((tab.Content as Frame).Content as WebViewPage).WebViewControl.Close();
            }
            if ((tab.Content as Frame).Content is SplitTabPage)
            {
                ((tab.Content as Frame).Content as SplitTabPage).CloseWebViews();
            }
            tab.Content = null;
            if (index == 0)
                TabListView.SelectedIndex = 1;
            else
                TabListView.SelectedIndex = SettingsViewModel.SettingsVM.Tabs.Count - 2;
            SettingsViewModel.SettingsVM.Tabs.Remove(tab);
        }
        else
        {
            WindowHelper.CloseMainWindow();
        }
    }

    private Tab _selectedTab;

    public Tab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab != value)
            {
                _selectedTab = value;
                OnPropertyChanged();
            }
        }
    }

    private void ToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Tag)
        {
            case "CopyLink":
                ClipboardHelper.CopyTextToClipboard(((SelectedTab.Content as Frame).Content as WebViewPage).WebViewControl.CoreWebView2.Source);
                break;
            case "NewTab":
                CreateWebTab("New tab", string.Empty);
                break;
            case "NewSplitTab":
                CreateTab("New split tab", typeof(SplitTabPage));
                break;
        }
    }

    private void ToolbarFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "Downloads":
                CreateWebTab("Downloads", "edge://downloads");
                break;
            case "History":
                CreateWebTab("History", "edge://history");
                break;
            case "Crashes":
                CreateWebTab("Crashes", "edge://crashes");
                break;
            case "Flags":
                CreateWebTab("Flags", "edge://flags");
                break;
            case "GPU":
                CreateWebTab("GPU Internals", "edge://gpu");
                break;
            case "Inspect":
                CreateWebTab("Inspect", "edge://inspect");
                break;
            case "Modules":
                CreateWebTab("Inspect", "edge://modules");
                break;
            case "WhatsNew":
                CreateWebTab("Release notes", "https://github.com/horizon-developers/browser/releases/latest");
                break;
            case "Settings":
                //CreateTab("Settings", typeof(SettingsPage));
                _ = new Modules.Settings.SettingsWindow();
                break;
        }
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //System.Diagnostics.Debug.WriteLine(sender.GetType().Name);
        var pointer = e.GetCurrentPoint(sender as Grid);
        if (pointer.Properties.IsMiddleButtonPressed)
        {
            if (SettingsViewModel.SettingsVM.Tabs.Count > 1)
            {
                var button = (Grid)sender;
                var tab = (Tab)button.DataContext;
                int index = SettingsViewModel.SettingsVM.Tabs.IndexOf(tab);
                if ((tab.Content as Frame).Content is WebViewPage)
                {
                    ((tab.Content as Frame).Content as WebViewPage).WebViewControl.Close();
                }
                tab.Content = null;
                if (index == 0)
                    TabListView.SelectedIndex = 1;
                else
                    TabListView.SelectedIndex = SettingsViewModel.SettingsVM.Tabs.Count - 2;
                SettingsViewModel.SettingsVM.Tabs.Remove(tab);
            }
            else
            {
                WindowHelper.CloseMainWindow();
            }
        }


    }

    Tab CTXSelectedTab;
    private void TabListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        CTXSelectedTab = ((FrameworkElement)e.OriginalSource).DataContext as Tab;
    }

    private void TabCTXItem_Click(object sender, RoutedEventArgs e)
    {
        object TabContent = (CTXSelectedTab.Content as Frame).Content;
        if (TabContent is not WebViewPage)
        {
            return;
        }

        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "Duplicate":
                string URL = (TabContent as WebViewPage).WebViewControl.CoreWebView2.Source;
                CreateWebTab("New tab", URL);
                break;
        }
    }

    #region Favorites flyout
    private void FavoritesFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        FavoritesFlyout.ShowAt(FavoritesBtn);
        FavoritesListView.SelectedItem = null;
    }

    private void FavoritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListView listView = sender as ListView;
        if (listView.SelectedItem != null)
        {
            FavoriteItem item = (FavoriteItem)listView.SelectedItem;
            CreateWebTab(item.Title, item.Url);
            FavoritesFlyout.Hide();
        }
    }

    FavoriteItem FavSelectedItem;
    private void FavoritesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        FavSelectedItem = ((FrameworkElement)e.OriginalSource).DataContext as FavoriteItem;
    }

    private void FavContextItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as AppBarButton).Tag)
        {
            case "Copy":
                ClipboardHelper.CopyTextToClipboard(FavSelectedItem.Url);
                break;
            case "Delete":
                FavoritesListView.SelectedItem = null;
                FavoritesHelper.RemoveFavorite(FavSelectedItem);
                break;
            case "CopyText":
                ClipboardHelper.CopyTextToClipboard(FavSelectedItem.Title);
                break;
        }
        FavoritesContextMenu.Hide();
    }
    #endregion

    private void UrlBoxButton_Click(object sender, RoutedEventArgs e)
    {
        if ((SelectedTab.Content as Frame)?.Content is WebViewPage webViewPage)
        {
            webViewPage.ToggleUrlBox();
        }
    }
}
