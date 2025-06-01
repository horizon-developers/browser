namespace Horizon.Core;

public static class ClipboardHelper
{
    public static void CopyTextToClipboard(string text)
    {
        DataPackage package = new();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
