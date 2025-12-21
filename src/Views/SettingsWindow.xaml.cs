namespace Horizon.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        
        var HWND = WindowNative.GetWindowHandle(this);
        WindowId windowId = AppWindow.Id;

        if (DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            var workArea = displayArea.WorkArea;

            int newWidth = (int)(workArea.Width * 0.4);
            int newHeight = (int)(workArea.Height * 0.7);

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
        (SettingsHost.Content as SettingsView)?.HeadlessWebViewInstance.Close();
        WindowHelper.MainWindow.Activate();
    }

    private void SettingsHost_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsHost.Navigate(typeof(SettingsView), null, new DrillInNavigationTransitionInfo());
    }

    private void WindowKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        this.Close();
    }
}
