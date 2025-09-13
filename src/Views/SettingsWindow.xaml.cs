namespace Horizon.Views;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(800, 600));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        var HWND = WindowNative.GetWindowHandle(this);
        WindowId windowId = AppWindow.Id;

        if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
            DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
        {
            PointInt32 CenteredPosition = appWindow.Position;
            CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(CenteredPosition);
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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsHost.GoBack();
    }
}
