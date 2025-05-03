namespace Horizon.Core;

internal class Win32Helper
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static void ShowMessageBox(string title, string message, IntPtr hwnd = 0)
    {
        MessageBox(hwnd, message, title, 0);
    }
}
