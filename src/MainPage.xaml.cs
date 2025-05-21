namespace Horizon;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        GetFavorites();
    }

    public static async void GetFavorites()
    {
        SettingsViewModel.SettingsVM.FavoritesList = await FavoritesHelper.GetFavoritesListAsync();
#if DEBUG
        foreach (FavoriteItem item in SettingsViewModel.SettingsVM.FavoritesList)
        {
            System.Diagnostics.Debug.WriteLine(item.Url);
        }
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
                myTab = tab
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
            if ((tab.Content as Frame).Content is ExtensionsPage)
            {
                ((tab.Content as Frame).Content as ExtensionsPage).DisposeHeadless();
            }
            if ((tab.Content as Frame).Content is SettingsPage)
            {
                ((tab.Content as Frame).Content as SettingsPage).DisposeHeadless();
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

    public Tab SelectedTab
    {
        get
        {
            Tab item = (Tab)TabListView.SelectedItem;
            return item;
        }
        set
        {
            TabListView.SelectedItem = value;
        }
    }

    private void ToolbarButton_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Tag)
        {
            case "NewTab":
                CreateWebTab("New tab", string.Empty);
                break;
            case "NewSplitTab":
                CreateTab("New split tab", typeof(SplitTabPage));
                break;
            case "Downloads":
                CreateWebTab("Downloads", "edge://downloads");
                break;
            case "History":
                CreateWebTab("History", "edge://history");
                break;
        }
    }

    private void ToolbarFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as MenuFlyoutItem).Tag)
        {
            case "Extensions":
                CreateTab("Extensions", typeof(ExtensionsPage));
                break;
            case "Settings":
                CreateTab("Settings", typeof(SettingsPage));
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

    FavoriteItem selectedItem;
    private void FavoritesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        selectedItem = ((FrameworkElement)e.OriginalSource).DataContext as FavoriteItem;
    }

    private void FavContextItem_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as AppBarButton).Tag)
        {
            case "Copy":
                ClipboardHelper.CopyTextToClipboard(selectedItem.Url);
                break;
            case "Delete":
                FavoritesListView.SelectedItem = null;
                FavoritesHelper.RemoveFavorite(selectedItem);
                break;
            case "CopyText":
                ClipboardHelper.CopyTextToClipboard(selectedItem.Title);
                break;
        }
        FavoritesContextMenu.Hide();
    }
    #endregion
}
