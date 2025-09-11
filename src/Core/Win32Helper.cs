namespace Horizon.Core;

/// <summary>
/// A helper class for Win32 api calls which have not yet been migrated to CsWin32
/// </summary>
internal partial class Win32Helper
{
    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static void ShowMessageBox(string title, string message, IntPtr hwnd = 0)
    {
        MessageBox(hwnd, message, title, 0);
    }
}
