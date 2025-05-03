namespace Horizon.Core;

class FaviconHelper
{
    public static IconSource ConvFavURLToIconSource(string url)
    {
        try
        {
            Uri faviconUrl = new(url);
            BitmapIconSource iconsource = new() { UriSource = faviconUrl, ShowAsMonochrome = false };
            return iconsource;
        }
        catch
        {
            return null;
        }
    }
    public static IconSource GetFallbackFavicon()
    {
        IconSource iconsource = new SymbolIconSource() { Symbol = Symbol.Document };
        return iconsource;
    }

}
