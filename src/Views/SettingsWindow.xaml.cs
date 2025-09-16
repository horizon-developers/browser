namespace Horizon.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        var HWND = WindowNative.GetWindowHandle(this);
        WindowId windowId = AppWindow.Id;

        if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
            DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            var workArea = displayArea.WorkArea;
            int newWidth = (int)(workArea.Width * 0.4);
            int newHeight = (int)(workArea.Height * 0.7);
            appWindow.Resize(new SizeInt32(newWidth, newHeight));

            PointInt32 centeredPosition = appWindow.Position;
            centeredPosition.X = (workArea.Width - appWindow.Size.Width) / 2;
            centeredPosition.Y = (workArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(centeredPosition);
        }

        OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();
        WindowHelper.SetWindowOwner(HWND);
        presenter.IsModal = true;

        AppWindow.SetPresenter(presenter);
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
        Frame SettingsHost = sender as Frame;
        SettingsHost.Navigate(typeof(SettingsView), null, new DrillInNavigationTransitionInfo());
    }
}
