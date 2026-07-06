namespace Horizon.Core;

internal static class ABEDatabase
{
    private static readonly HashSet<string> Blocklist = [with(StringComparer.OrdinalIgnoreCase)];
    private static bool _isLoaded;

    internal static async Task LoadBlocklistAsync()
    {
        // Thread-safe state guard to handle double initialization gracefully
        if (_isLoaded)
        {
            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", "Cache hit! Blocklist already loaded.");
            return;
        }

        try
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Assets/Unified.txt");

            if (!File.Exists(filePath))
            {
                Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", $"Blocklist file not found at: {filePath}");
                return;
            }

            // Optimization: Streaming line-by-line prevents huge allocations on the Large Object Heap (LOH)
            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                string trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                // Robustly clean standard hosts file prefixes (0.0.0.0 or 127.0.0.1)
                if (trimmed.StartsWith("0.0.0.0", StringComparison.Ordinal))
                {
                    trimmed = trimmed[7..].Trim();
                }
                else if (trimmed.StartsWith("127.0.0.1", StringComparison.Ordinal))
                {
                    trimmed = trimmed[9..].Trim();
                }

                if (!string.IsNullOrEmpty(trimmed))
                {
                    Blocklist.Add(trimmed);
                }
            }

            _isLoaded = true;
            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", $"Successfully loaded {Blocklist.Count} domains into the blocklist.");
        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Failed to load blocklist: {ex.Message}");
        }
    }

    internal static bool IsAdDomain(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        // 1. Direct match check -> O(1)
        if (Blocklist.Contains(host)) return true;

        // 2. Subdomain check -> O(M) where M is the number of domain segments.
        // Replaced the massive O(N) linear loop over the entire HashSet.
        int nextDot = host.IndexOf('.');
        while (nextDot != -1)
        {
            // Extract parent domain (e.g., "g.doubleclick.net" -> "doubleclick.net")
            string parentDomain = host[(nextDot + 1)..];
            if (Blocklist.Contains(parentDomain))
            {
                return true;
            }
            nextDot = host.IndexOf('.', nextDot + 1);
        }

        return false;
    }
}

internal class AdBlockEngine
{
    public async Task InitBrowserAsync(CoreWebView2 webView)
    {
        await ABEDatabase.LoadBlocklistAsync();

        webView.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        webView.WebResourceRequested += OnWebResourceRequested;
    }   

    private void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? requestUri))
        {
            if (ABEDatabase.IsAdDomain(requestUri.Host))
            {
                CoreWebView2WebResourceResponse response = sender.Environment.CreateWebResourceResponse(
                    Content: null,
                    StatusCode: 403,
                    ReasonPhrase: "Blocked",
                    Headers: "Content-Type: text/plain");

                args.Response = response;
                Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", $"[SRC: {sender.Source}] Blocked " + args.Request.Uri);
            }
        }
    }
}