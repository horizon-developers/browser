#nullable enable

namespace Horizon.Core;

public static class WindowHelper
{
    public static void SetFullScreen(bool fs)
    {
        switch (fs)
        {
            case true:
                MainAppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                break;
            case false:
                MainAppWindow.SetPresenter(AppWindowPresenterKind.Default);
                break;
        }
    }

    public static bool IsWindowInFullScreen()
    {
        if (MainAppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            return true;
        else
            return false;
    }

    static public void CreateMainWindow()
    {
        MainWindow = new();
        MainWindow.Title = "Horizon";
        MainWindow.ExtendsContentIntoTitleBar = true;
        SetMinWindowSize();
        string Backdrop = SettingsHelper.GetSetting("OverrideBackdropType");
        switch (Backdrop)
        {
            case "Acrylic":
                MainWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
                break;
            case "Mica Alt":
                MainWindow.SystemBackdrop = new MicaBackdrop
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
                break;
            default:
                MainWindow.SystemBackdrop = new MicaBackdrop();
                break;
        }

        MainAppWindow = MainWindow.AppWindow;
        //MainAppWindow.SetIcon("Horizon.ico");
        //MainAppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        WindowMainPage = new MainPage();
        MainWindow.Content = WindowMainPage;
        MainWindow.SetTitleBar(WindowMainPage.TitleBarControl);



    }

    static public void ActivateMainWindow()
    {
        MainWindow.Activate();
    }

    static public void CloseMainWindow()
    {
        MainWindow.Close();
    }

    static public void CreateNewTabInMainWindow(string title, string uri)
    {
        _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            (MainWindow.Content as MainPage).CreateWebTab(title, uri);
        });
    }

    static public void CreateNativeTabInMainWindow(string title, Type page)
    {
        _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            (MainWindow.Content as MainPage).CreateTab(title, page);
        });
    }

    static public void SetMinWindowSize()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 500;
        }
    }

    public static Window MainWindow { get; set; }
    public static AppWindow MainAppWindow { get; private set; }
    public static MainPage WindowMainPage { get; private set; }
}
