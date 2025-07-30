namespace Horizon.Controls.WebContent;

internal static class GlobalEnvironment
{
    private static CoreWebView2Environment Environment;

    public static async Task<CoreWebView2Environment> GetDefault()
    {
        if (Environment == null)
        {
            await CreateEnvironment();
        }
        return Environment;
    }

    private static async Task CreateEnvironment()
    {
        CoreWebView2EnvironmentOptions options = new()
        {
            AreBrowserExtensionsEnabled = true,
            ScrollBarStyle = CoreWebView2ScrollbarStyle.FluentOverlay
        };
        Environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);
#if DEBUG
        System.Diagnostics.Debug.WriteLine("WebView2 enviroment created!");
#endif
    }
}
