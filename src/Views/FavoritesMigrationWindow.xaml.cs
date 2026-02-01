namespace Horizon.Views;

public sealed partial class FavoritesMigrationWindow : Window
{
    public FavoritesMigrationWindow()
    {
        InitializeComponent();
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        var HWND = WindowNative.GetWindowHandle(this);
        WindowId windowId = AppWindow.Id;

        if (DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            var workArea = displayArea.WorkArea;

            int newWidth = (int)(workArea.Width * 0.35);
            int newHeight = (int)(workArea.Height * 0.35);

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
        WindowHelper.MainWindow.Activate();
    }

    private async void ConvertBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        if (File.Exists(FavoritesHelper.FavoritesFilePath))
        {
            ContentDialog dialog = new()
            {
                Title = "Warning",
                Content = "Your favorites have already been converted!",
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
            this.Close();
            return;
        }

        button.Content = new ProgressRing { IsIndeterminate = true };
        button.IsEnabled = false;

        string convlist = await Task.Run(() =>
        {
            return FavoriteMigrationHelper.ConvertDatabase();
        });

        ConvertedFavsDisplay.Text = convlist;
        button.Content = "Done! Please check the logs";
    }
}