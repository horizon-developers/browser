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
#if DEBUG
        Logger.LogEvent(Logger.Severity.Info, "WindowChromeView", $"OnPropertyChanged, PropertyName: { propertyName}");
#endif
    }

    public WindowChrome()
    {
        InitializeComponent();
        GetFavorites();
    }

    public static void GetFavorites()
    {
        MainViewModel.MainVM.FavoritesList = FavoritesHelper.GetFavoritesList();
    }

    public void CreateTab(string title, string launchurl, bool isinprivate = false, bool insertaftercurrent = false, int indexofrequester = -1)
    {
        Tab tab = new();
        TabCreationParams parameters = new()
        {
            LaunchUrl = launchurl,
            MyTab = tab,
            IsInPrivate = isinprivate
        };

        WebContentHost NewWCI = new(parameters);

        tab.Title = title;
        tab.WebContentInstance = NewWCI;
        if (insertaftercurrent && indexofrequester != -1)
        {
            MainViewModel.MainVM.Tabs.Insert(indexofrequester + 1, tab);
            TabListView.SelectedItem = tab;
            return;
        }
        MainViewModel.MainVM.Tabs.Add(tab);
        TabListView.SelectedItem = tab;
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListView listView = sender as ListView;
        Tab item = (Tab)listView.SelectedItem;
        if (item != null)
        {
            SelectedTab = item;
            TabContentHost.Content = item.WebContentInstance;
        }
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var tab = (Tab)button.DataContext;
        CloseTab(tab);
    }

    private void CloseTab(Tab tab)
    {
        if (MainViewModel.MainVM.Tabs.Count > 1)
        {
            int index = MainViewModel.MainVM.Tabs.IndexOf(tab);
            tab.WebContentInstance.WebContentControl.Close();
            tab.WebContentInstance = null;
            if (index == 0)
            {
                TabListView.SelectedIndex = 1;
            }
            else
            {
                TabListView.SelectedIndex = index - 1;
            }
            MainViewModel.MainVM.Tabs.Remove(tab);
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
                ClipboardHelper.CopyTextToClipboard(SelectedTab.WebContentInstance.WebContentControl.CoreWebView2.Source);
                DevWinUI.Growl.Success(new DevWinUI.GrowlInfo
                {
                    ShowDateTime = false,
                    Title = "Success",
                    Message = "Copied link!"
                });
                break;
            case "NewTab":
                CreateTab("New tab", string.Empty);
                break;
            case "NewPrivateTab":
                CreateTab("New InPrivate tab", string.Empty, true);
                break;
        }
    }

    private void ToolbarFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "Downloads":
                CreateTab("Downloads", "edge://downloads");
                break;
            case "History":
                CreateTab("History", "edge://history");
                break;
            case "Crashes":
                CreateTab("Crashes", "edge://crashes");
                break;
            case "Flags":
                CreateTab("Flags", "edge://flags");
                break;
            case "GPU":
                CreateTab("GPU Internals", "edge://gpu");
                break;
            case "Inspect":
                CreateTab("Inspect", "edge://inspect");
                break;
            case "Modules":
                CreateTab("Inspect", "edge://modules");
                break;
            case "WhatsNew":
                CreateTab("Release notes", "https://github.com/horizon-developers/browser/releases/latest");
                break;
            case "Settings":
                _ = new Views.SettingsWindow();
                break;
        }
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //System.Diagnostics.Debug.WriteLine(sender.GetType().Name);
        var pointer = e.GetCurrentPoint(sender as Grid);
        if (pointer.Properties.IsMiddleButtonPressed)
        {
            var button = (Grid)sender;
            var tab = (Tab)button.DataContext;
            CloseTab(tab);
        }
    }

    Tab CTXSelectedTab;
    private void TabListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        CTXSelectedTab = ((FrameworkElement)e.OriginalSource).DataContext as Tab;
    }

    private void TabCTXItem_Click(object sender, RoutedEventArgs e)
    {
        if (CTXSelectedTab == null)
        {
            return;
        }

        var ThisWCI = CTXSelectedTab.WebContentInstance;
        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "Duplicate":
                string URL = ThisWCI.WebContentControl.CoreWebView2.Source;
                CreateTab("New tab", URL);
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
            CreateTab(item.Title, item.Url);
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
        SelectedTab.WebContentInstance.ToggleUrlBox();
    }
}
