#nullable enable

namespace Horizon.Core;

/// <summary>
/// A common helper for Window operations
/// It allows for easy creation of new windows, modifing, accessing and closing them
/// </summary>
public static class WindowHelper
{
    public static void SetFullScreen(bool fs)
    {
        switch (fs)
        {
            case true:
                MainAppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    (MainWindow.Content as MainPage).TabContentPresenter.Margin = new Thickness(-42, -34, -192, -7);
                    (MainWindow.Content as MainPage).Sidebar.Visibility = Visibility.Collapsed;
                });
                break;
            case false:
                MainAppWindow.SetPresenter(AppWindowPresenterKind.Default);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    (MainWindow.Content as MainPage).TabContentPresenter.Margin = new Thickness(0);
                    (MainWindow.Content as MainPage).Sidebar.Visibility = Visibility.Visible;
                });
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
        MainAppWindow = MainWindow.AppWindow;
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
        WindowMainPage = new MainPage();
        MainWindow.Content = WindowMainPage;
        MainWindow.SetTitleBar(WindowMainPage.TitleBarControl);
        if (SettingsHelper.GetSetting("IsScreencaptureBlocked") == "true")
        {
            BlockScreencaptureForMainWindow(true);
        }
        if (SettingsHelper.GetSetting("IsAlwaysOnTopEnabled") == "true")
        {
            SetMainWindowAlwaysOnTop(true);
        }
    }

    static public void ActivateMainWindow()
    {
        MainWindow.Activate();
    }

    static public void RestoreMainWindow()
    {
        if (MainAppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }
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
        if (MainAppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 500;
        }
    }

    static public void BlockScreencaptureForMainWindow(bool b)
    {
        var hwnd = (Windows.Win32.Foundation.HWND)WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
        switch (b)
        {
            case true:
                Windows.Win32.PInvoke.SetWindowDisplayAffinity(hwnd, WINDOW_DISPLAY_AFFINITY.WDA_MONITOR);
                break;
            case false:
                Windows.Win32.PInvoke.SetWindowDisplayAffinity(hwnd, WINDOW_DISPLAY_AFFINITY.WDA_NONE);
                break;
        }
    }

    static public void SetMainWindowAlwaysOnTop(bool t)
    {
        if (MainAppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = t;
        }
    }

    public static Window MainWindow { get; set; }
    public static AppWindow MainAppWindow { get; private set; }
    public static MainPage WindowMainPage { get; private set; }
}
