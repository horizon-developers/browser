namespace Horizon.Views;

public sealed partial class FavoritesManagerWindow : Window
{
    public FavoritesManagerWindow()
    {
        InitializeComponent();
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        var HWND = WindowNative.GetWindowHandle(this);
        WindowId windowId = AppWindow.Id;

        if (DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            var workArea = displayArea.WorkArea;

            int newWidth = (int)(workArea.Width * 0.4);
            int newHeight = (int)(workArea.Height * 0.4);

            int newX = workArea.X + (workArea.Width - newWidth) / 2;
            int newY = workArea.Y + (workArea.Height - newHeight) / 2;

            var newBounds = new RectInt32(newX, newY, newWidth, newHeight);

            AppWindow.MoveAndResize(newBounds);
        }

        OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();
        WindowHelper.SetWindowOwner(HWND);
        presenter.IsModal = true;

        AppWindow.SetPresenter(presenter);
        rootGrid.Focus(FocusState.Programmatic);
        AppWindow.Show();

        Closed += ModalWindow_Closed;
    }

    private void ModalWindow_Closed(object sender, WindowEventArgs args)
    {
        //(SettingsHost.Content as SettingsView)?.HeadlessWebViewInstance.Close();
        WindowHelper.MainWindow.Activate();
    }

    private void WindowKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        this.Close();
    }

    bool IsSearchActive { get; set; } = false;
    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        GenerateSearchResults();
    }

    private void GenerateSearchResults()
    {
        if (string.IsNullOrEmpty(SearchTextBox.Text))
        {
            FavoritesListView.ItemsSource = MainViewModel.MainVM.FavoritesList;
            IsSearchActive = false;
        }
        IsSearchActive = true;
        // Get all ListView items with the submitted search query
        var SearchResults = from s in MainViewModel.MainVM.FavoritesList where s.Title.Contains(SearchTextBox.Text, StringComparison.OrdinalIgnoreCase) select s;
        // Set SearchResults as ItemSource for HistoryListView
        FavoritesListView.ItemsSource = SearchResults;
    }

    private void FavoritesListView_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as ListView).ItemsSource = MainViewModel.MainVM.FavoritesList;
    }

    #region Favorites flyout
    FavoriteItem FavSelectedItem;
    private void FavoritesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        FavSelectedItem = ((FrameworkElement)e.OriginalSource).DataContext as FavoriteItem;
    }

    private void FavContextItem_Click(object sender, RoutedEventArgs e)
    {
        if (FavSelectedItem == null)
        {
            return;
        }
        switch ((sender as AppBarButton).Tag)
        {
            case "Copy":
                ClipboardHelper.CopyTextToClipboard(FavSelectedItem.Url);
                break;
            case "Delete":
                DeleteFavorite(FavSelectedItem.Id);
                break;
            case "CopyText":
                ClipboardHelper.CopyTextToClipboard(FavSelectedItem.Title);
                break;
        }
        FavoritesContextMenu.Hide();
    }
    #endregion

    private void DeleteFavorite(Guid favId)
    {
        foreach (FavoriteItem item in MainViewModel.MainVM.FavoritesList)
        {
            if (item.Id == favId)
            {
                FavoritesHelper.RemoveFavorite(item);
                if (IsSearchActive)
                {
                    GenerateSearchResults();
                }
                return;
            }
        }
    }
