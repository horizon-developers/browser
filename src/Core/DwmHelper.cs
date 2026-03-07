using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;

namespace Horizon.Core;

public static class DwmHelper
{
    internal static unsafe void ApplyMica(IntPtr hwnd, DWM_SYSTEMBACKDROP_TYPE backdropType)
    {
        HWND windowHandle = new(hwnd);

        PInvoke.DwmSetWindowAttribute(
            windowHandle,
            DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            &backdropType,
            sizeof(DWM_SYSTEMBACKDROP_TYPE));

        MARGINS margins = new()
        {
            cxLeftWidth = -1,
            cxRightWidth = -1,
            cyTopHeight = -1,
            cyBottomHeight = -1
        };

        PInvoke.DwmExtendFrameIntoClientArea(windowHandle, in margins);

        if (App.Current.RequestedTheme == ApplicationTheme.Dark)
        {
            SetDarkMode(windowHandle, true);
        }
    }

    private static unsafe void SetDarkMode(HWND windowHandle, bool isDark)
    {
        int useImmersiveDarkMode = isDark ? 1 : 0;

        PInvoke.DwmSetWindowAttribute(
            windowHandle,
            (DWMWINDOWATTRIBUTE)20,
            &useImmersiveDarkMode,
            sizeof(int));
    }
}