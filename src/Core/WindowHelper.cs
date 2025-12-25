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
    public static nint HWND { get; private set; } = 0;

    static public void CreateMainWindow()
    {
        MainWindow = new()
        {
            Title = "Horizon",
            ExtendsContentIntoTitleBar = true
        };
        HWND = WindowNative.GetWindowHandle(MainWindow);
        MainWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        //RestoreTabs();
        //RestoreWindowState();
        SetMinWindowSize();
        SetupBackdrop();
        //MainWindow.SetTitleBar(MainWindow.TitleBarControl);
        //MainWindow.AppWindow.Closing += AppWindow_Closing;
        ApplyWindowSettings();
    }

    /*private static void RestoreTabs()
    {
        RestoreSessionTabs.RestoreTabsOnStartup();
    }

    private static async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        await RestoreSessionTabs.SaveTabsOnExit().WaitAsync(new System.Threading.CancellationToken());
        SaveWindowState();
    }*/

    public static void SetFullScreen(bool fs)
    {
        switch (fs)
        {
            case true:
                MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.TabContentHost.Margin = new Thickness(-44, -8, -194, -8);
                    MainWindow.Sidebar.Visibility = Visibility.Collapsed;
                });
                break;
            case false:
                MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                _ = MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.TabContentHost.Margin = new Thickness(0);
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
            MainWindow.CreateTab(title, uri);
        });
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OverlappedPresenter))]
    static public void SetMinWindowSize()
    {
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 500;
            presenter.Maximize();
        }
    }

    static public void BlockScreencaptureForMainWindow(bool b)
    {
        switch (b)
        {
            case true:
                Windows.Win32.PInvoke.SetWindowDisplayAffinity((Windows.Win32.Foundation.HWND)HWND, WINDOW_DISPLAY_AFFINITY.WDA_MONITOR);
                break;
            case false:
                Windows.Win32.PInvoke.SetWindowDisplayAffinity((Windows.Win32.Foundation.HWND)HWND, WINDOW_DISPLAY_AFFINITY.WDA_NONE);
                break;
        }
    }

    static public void SetWindowOwner(nint OwnedHWND)
    {
#if Win64
        _ = Windows.Win32.PInvoke.SetWindowLongPtr((Windows.Win32.Foundation.HWND)OwnedHWND, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, HWND);
#else
        _ = Windows.Win32.PInvoke.SetWindowLong((Windows.Win32.Foundation.HWND)OwnedHWND, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, (int)HWND);
#endif
    }

    /*static public void SaveWindowState()
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
    }*/

    /*private static void RestoreWindowState()
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
    }*/
}
