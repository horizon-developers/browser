#nullable enable
// This is fine here, there is no scenario where these should lead to a crash
// at least that's what I hope lmao
#pragma warning disable CS8618

namespace Horizon.Core;

/// <summary>
/// A common helper for Window operations
/// It allows for easy creation of new windows, modifing, accessing and closing them
/// </summary>
public static class WindowHelper
{
    public static WindowChrome MainWindow { get; private set; }
    public static nint HWND { get; private set; }

    static public void CreateMainWindow()
    {
        MainWindow = new()
        {
            Title = "Horizon",
            ExtendsContentIntoTitleBar = true
        };
        HWND = WindowNative.GetWindowHandle(MainWindow);
        //RestoreWindowState();
        SetMinWindowSize();
        SetupBackdrop();
        MainWindow.SetTitleBar(MainWindow.TitleBarControl);
        //MainWindow.AppWindow.Closing += AppWindow_Closing;
        ApplyWindowSettings();
    }

    public static void SetFullScreen(bool fs)
    {
        switch (fs)
        {
            case true:
                MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.TabContentPresenter.Margin = new Thickness(-42, -34, -192, -7);
                    MainWindow.Sidebar.Visibility = Visibility.Collapsed;
                });
                break;
            case false:
                MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.TabContentPresenter.Margin = new Thickness(0);
                    MainWindow.Sidebar.Visibility = Visibility.Visible;
                });
                break;
        }
    }

    public static bool IsWindowInFullScreen()
    {
        if (MainWindow.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            return true;
        else
            return false;
    }

    static private void SetupBackdrop()
    {
        string Backdrop = SettingsHelper.GetSetting("OverrideBackdropType");
        MainWindow.SystemBackdrop = Backdrop switch
        {
            "Acrylic" => new DesktopAcrylicBackdrop(),
            "Mica Alt" => new MicaBackdrop
            {
                Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
            },
            "None" => null,
            _ => new MicaBackdrop(),
        };
    }

    static private void ApplyWindowSettings()
    {
        if (SettingsHelper.GetSetting("IsScreencaptureBlocked") == "true")
        {
            BlockScreencaptureForMainWindow(true);
        }
        if (SettingsHelper.GetSetting("IsAlwaysOnTopEnabled") == "true")
        {
            SetMainWindowAlwaysOnTop(true);
        }
    }

    private static void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        SaveWindowState();
    }

    static public void ActivateMainWindow()
    {
        MainWindow.Activate();
    }

    static public void RestoreMainWindow()
    {
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
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
            MainWindow.CreateWebTab(title, uri);
        });
    }

    static public void CreateNativeTabInMainWindow(string title, Type page)
    {
        _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            MainWindow.CreateTab(title, page);
        });
    }

    static public void SetMinWindowSize()
    {
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 500;
        }
    }

    static public void BlockScreencaptureForMainWindow(bool b)
    {
        var hwnd = (Windows.Win32.Foundation.HWND)WindowNative.GetWindowHandle(MainWindow);
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
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = t;
        }
    }

    static public void SaveWindowState()
    {
        WindowState state = new()
        {
            X = MainWindow.AppWindow.Position.X,
            Y = MainWindow.AppWindow.Position.Y,
            Width = MainWindow.AppWindow.Size.Width,
            Height = MainWindow.AppWindow.Size.Height
        };
        bool IsMaximized = false;
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                IsMaximized = true;
            }
        }
        state.IsMaximized = IsMaximized;

        var json = JsonSerializer.Serialize(state, WindowStateSerializerContext.Default.WindowState);

        if (!string.IsNullOrEmpty(json))
        {
            SettingsHelper.SetSetting("WindowSettings", json);
        }
#if DEBUG
        System.Diagnostics.Debug.WriteLine(json);
#endif
    }

    private static void RestoreWindowState()
    {
        string WindowSettingsJson = SettingsHelper.GetSetting("WindowSettings");
        if (string.IsNullOrEmpty(WindowSettingsJson))
        {
            return;
        }

        WindowState? Settings = JsonSerializer.Deserialize(WindowSettingsJson, WindowStateSerializerContext.Default.WindowState);

        if (Settings == null)
        {
            return;
        }

        if (Settings.IsMaximized)
        {
            if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
            return;
        }

        RectInt32 windowRect = new(
            Settings.X,
            Settings.Y,
            Settings.Width,
            Settings.Height
        );

        MainWindow.AppWindow.MoveAndResize(windowRect);
    }
}
