namespace Horizon.Core;

public static class ClipboardHelper
{
    public static void CopyTextToClipboard(string text)
    {
        DataPackage package = new();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    public static async Task<string> PasteUriAsStringFromClipboardAsync()
    {
        var package = Clipboard.GetContent();
        if (package.Contains(StandardDataFormats.Text))
        {
            var text = await package.GetTextAsync();
            return text.ToString();
        }
        return string.Empty;
    }
}
