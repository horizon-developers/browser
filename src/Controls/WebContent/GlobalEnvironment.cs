namespace Horizon.Controls.WebContent;

internal static class GlobalEnvironment
{
    private static CoreWebView2Environment? Environment;

    public static async Task<CoreWebView2Environment> GetDefault()
    {
        return Environment ??= await CreateEnvironment();
    }

    private static async Task<CoreWebView2Environment> CreateEnvironment()
    {
        CoreWebView2EnvironmentOptions options = new()
        {
            AreBrowserExtensionsEnabled = true,
            ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay
        };
        var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);
#if DEBUG
        System.Diagnostics.Debug.WriteLine("WebView2 enviroment created!");
#endif
        return environment;
    }
}
