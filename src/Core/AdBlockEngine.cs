namespace Horizon.Core;

/// <summary>
/// Holds the domain blocklist (StevenBlack's unified hosts file) and keeps it fresh.
/// The list is served from a local binary cache first so startup never waits on the network;
/// a missing or stale cache is refreshed in the background and swapped in atomically.
/// </summary>
internal static class ABEDatabase
{
    private const string SourceUrl = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";
    private const int Magic = 0x42444C4B; // "BDLK"
    private const int Version = 4;        // v4: loopback/bookkeeping hosts are no longer stored
    private const int HeaderSize = 20;    // magic(4) + version(4) + epoch(8) + count(4)
    private const int MaxHostLength = 253;
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    private static readonly HttpClient _http = new(new SocketsHttpHandler
    {
        // The raw list is ~2.4 MB, gzipped ~0.6 MB.
        AutomaticDecompression = DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly Lazy<Task> _load = new(LoadAsync);
    private static readonly object _gate = new();

    private static volatile FrozenSet<string> _blocklist = FrozenSet<string>.Empty;
    private static long _cachedEpoch = -1;   // unix time the in-memory list was fetched upstream, -1 = no list
    private static long _lastRefreshAttemptEpoch;
    private static Task? _refreshTask;

    private static string CacheFilePath => Path.Combine(FolderHelper.LocalFolder.Path, "Unified.bin");

    /// <summary>True once the on-disk cache has been consulted, whether or not it yielded a list.</summary>
    internal static bool IsReady => _load.IsValueCreated && _load.Value.IsCompleted;

    /// <summary>
    /// Loads the cached list (once, off the UI thread) and starts a background refresh if it is missing or stale.
    /// Safe to call from anywhere; every caller shares the same task.
    /// </summary>
    internal static Task EnsureLoadedAsync() => _load.Value;

    /// <summary>
    /// Starts a background refresh when the list is older than <see cref="MaxAge"/> or missing.
    /// Cheap to call often: at most one refresh runs at a time and failed attempts back off.
    /// </summary>
    internal static void RefreshIfStale()
    {
        if (IsReady) StartRefreshIfStale();
    }

    private static async Task LoadAsync()
    {
        await Task.Run(LoadFromDisk).ConfigureAwait(false);
        StartRefreshIfStale();
    }

    private static void StartRefreshIfStale()
    {
        lock (_gate)
        {
            if (_refreshTask is { IsCompleted: false }) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool missing = _cachedEpoch < 0;
            bool stale = missing || now - _cachedEpoch > MaxAge.TotalSeconds;
            if (!stale || now - _lastRefreshAttemptEpoch < RetryDelay.TotalSeconds) return;

            _lastRefreshAttemptEpoch = now;
            string reason = missing ? "No usable cache, fetching blocklist." : "Cache older than 24h, refreshing blocklist.";
            _refreshTask = Task.Run(() => RefreshAsync(reason));
        }
    }

    private static async Task RefreshAsync(string reason)
    {
        try
        {
            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", reason);
            var watch = Stopwatch.StartNew();

            string hostsText = await _http.GetStringAsync(SourceUrl).ConfigureAwait(false);
            long epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string[] domains = ParseHostsFile(hostsText);

            WriteBinAtomic(CacheFilePath, domains, epoch);
            Publish(domains, epoch);

            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",
                $"Fetched and cached {domains.Length} domains from upstream in {watch.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            // Keep whatever list is loaded (possibly stale, possibly empty); the next tab retries after RetryDelay.
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine",
                $"Blocklist fetch failed, keeping the current list ({_blocklist.Count} domains): {ex.Message}");
        }
    }

    private static void Publish(string[] domains, long epoch)
    {
        FrozenSet<string> set = domains.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            _blocklist = set;
            _cachedEpoch = epoch;
        }
    }

    private static void LoadFromDisk()
    {
        string path = CacheFilePath;
        if (!File.Exists(path)) return;

        try
        {
            var watch = Stopwatch.StartNew();
            byte[] data = File.ReadAllBytes(path);

            if (!TryParseCache(data, out string[] domains, out long epoch))
            {
                // Corrupt or written by an older build - drop it so the refresh rebuilds it from upstream.
                Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", "Cached blocklist is invalid, discarding it.");
                File.Delete(path);
                return;
            }

            Publish(domains, epoch);

            long ageHours = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - epoch) / 3600;
            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",
                $"Loaded {domains.Length} domains from cache (age: {ageHours}h) in {watch.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Failed to load cached blocklist: {ex.Message}");
        }
    }

    private static bool TryParseCache(ReadOnlySpan<byte> data, out string[] domains, out long epoch)
    {
        domains = [];
        epoch = -1;

        if (data.Length < HeaderSize) return false;
        if (BinaryPrimitives.ReadInt32LittleEndian(data) != Magic) return false;
        if (BinaryPrimitives.ReadInt32LittleEndian(data[4..]) != Version) return false;
        epoch = BinaryPrimitives.ReadInt64LittleEndian(data[8..]);
        int count = BinaryPrimitives.ReadInt32LittleEndian(data[16..]);
        if (count < 0 || count > (data.Length - HeaderSize) / 2) return false; // every entry is at least a 2-byte length

        var result = new string[count];
        int offset = HeaderSize;
        for (int i = 0; i < count; i++)
        {
            if (offset + 2 > data.Length) return false;
            int len = BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
            offset += 2;
            if (offset + len > data.Length) return false;
            result[i] = Encoding.UTF8.GetString(data.Slice(offset, len));
            offset += len;
        }

        domains = result;
        return true;
    }

    private static string[] ParseHostsFile(string text)
    {
        var domains = new List<string>(capacity: 100_000);

        ReadOnlySpan<char> remaining = text;
        while (!remaining.IsEmpty)
        {
            int newline = remaining.IndexOf('\n');
            ReadOnlySpan<char> line = newline < 0 ? remaining : remaining[..newline];
            remaining = newline < 0 ? default : remaining[(newline + 1)..];

            // Drop comments (whole-line and trailing), then read "<address> <host> [<host>...]".
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            line = line.Trim();
            if (line.IsEmpty) continue;

            int separator = line.IndexOfAny(' ', '\t');
            if (separator < 0) continue;

            // Only the sinkhole mappings are blocklist entries; IPv6 loopback lines etc. are hosts-file bookkeeping.
            ReadOnlySpan<char> address = line[..separator];
            if (!address.SequenceEqual("0.0.0.0") && !address.SequenceEqual("127.0.0.1")) continue;

            ReadOnlySpan<char> hosts = line[(separator + 1)..];
            while (!hosts.IsEmpty)
            {
                hosts = hosts.TrimStart();
                int end = hosts.IndexOfAny(' ', '\t');
                ReadOnlySpan<char> host = end < 0 ? hosts : hosts[..end];
                hosts = end < 0 ? default : hosts[(end + 1)..];

                if (IsBlockableHost(host))
                    domains.Add(host.ToString());
            }
        }

        return domains.ToArray();
    }

    /// <summary>
    /// Filters out the entries every hosts file carries for the OS's own benefit. Blocking "localhost"
    /// or "local" would break every local dev server opened in the browser.
    /// </summary>
    private static bool IsBlockableHost(ReadOnlySpan<char> host)
    {
        if (host.IsEmpty || host.Length > MaxHostLength) return false;
        if (host.IndexOf('.') < 0) return false; // localhost, local, broadcasthost, ip6-* ... never public ad hosts
        return !host.Equals("localhost.localdomain", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("0.0.0.0", StringComparison.Ordinal);
    }

    private static void WriteBinAtomic(string path, string[] domains, long epochSeconds)
    {
        // Write to temp + move-into-place so a crash mid-write never corrupts the cache
        // the next launch tries to read.
        string tempPath = path + ".tmp";

        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 16))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(epochSeconds);
            writer.Write(domains.Length);

            Span<byte> buffer = stackalloc byte[MaxHostLength * 3]; // worst-case UTF-8 expansion
            foreach (string domain in domains)
            {
                int length = Encoding.UTF8.GetBytes(domain, buffer);
                writer.Write((ushort)length);
                writer.Write(buffer[..length]);
            }
        }

        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>Whether <paramref name="host"/> or any of its parent domains is blocklisted.</summary>
    internal static bool IsAdDomain(string host)
    {
        FrozenSet<string> set = _blocklist;
        if (set.Count == 0 || string.IsNullOrEmpty(host)) return false;

        var lookup = set.GetAlternateLookup<ReadOnlySpan<char>>();

        // "ads.example.com." (fully-qualified, trailing dot) must hit the same entries as "ads.example.com".
        ReadOnlySpan<char> remaining = host.AsSpan().TrimEnd('.');
        while (!remaining.IsEmpty)
        {
            if (lookup.Contains(remaining)) return true;

            int dot = remaining.IndexOf('.');
            if (dot < 0) break;
            remaining = remaining[(dot + 1)..];
        }
        return false;
    }
}

/// <summary>
/// Per-WebView request filter. Requests to blocklisted hosts are answered with a harmless, successful-looking
/// response instead of a network error, so pages - and their anti-adblock scripts - keep working.
/// <see cref="Attach"/> and <see cref="Dispose"/> must be called on the UI thread that owns the CoreWebView2.
/// </summary>
internal sealed class AdBlockEngine : IDisposable
{
    // 1x1 transparent GIF: blocked images decode fine instead of firing onerror / showing a broken-image icon.
    private static readonly byte[] TransparentGif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    // Service/shared workers are not tied to one document, so WebView2 raises their requests on every
    // CoreWebView2 that registered a worker filter. Keep that filter on exactly one live engine at a time.
    private static readonly List<AdBlockEngine> _live = [];
    private static AdBlockEngine? _workerFilterOwner;

    private CoreWebView2? _webView;
    private CoreWebView2Environment? _environment;

    public void Attach(CoreWebView2 webView)
    {
        _webView = webView;
        _environment = webView.Environment;

        // Register before the first navigation so no request slips through while the list loads;
        // requests that arrive before the cache has been read are deferred, not passed.
        webView.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.Document);
        webView.WebResourceRequested += OnWebResourceRequested;

        _live.Add(this);
        if (_workerFilterOwner == null)
            TryTakeWorkerFilter(this);

        _ = ABEDatabase.EnsureLoadedAsync();
        ABEDatabase.RefreshIfStale();
    }

    public void Dispose()
    {
        if (_webView == null) return;

        try
        {
            _webView.WebResourceRequested -= OnWebResourceRequested;
        }
        catch (Exception ex)
        {
            // The CoreWebView2 may already be gone; nothing left to unhook then.
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Detach failed: {ex.Message}");
        }

        _live.Remove(this);
        if (_workerFilterOwner == this)
        {
            // Hand the worker filter to another open tab so worker traffic stays filtered.
            _workerFilterOwner = null;
            foreach (AdBlockEngine next in _live)
            {
                if (TryTakeWorkerFilter(next)) break;
            }
        }

        _webView = null;
        _environment = null;
    }

    private static bool TryTakeWorkerFilter(AdBlockEngine engine)
    {
        try
        {
            engine._webView!.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All,
                CoreWebView2WebResourceRequestSourceKinds.ServiceWorker | CoreWebView2WebResourceRequestSourceKinds.SharedWorker);
            _workerFilterOwner = engine;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Could not register worker filter: {ex.Message}");
            return false;
        }
    }

    private async void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        Windows.Foundation.Deferral? deferral = null;
        try
        {
            if (!ABEDatabase.IsReady)
            {
                deferral = args.GetDeferral();
                await ABEDatabase.EnsureLoadedAsync(); // resumes on the UI thread via the DispatcherQueue sync context
            }

            if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? requestUri) || !ABEDatabase.IsAdDomain(requestUri.Host))
                return;

            args.Response = CreateBlockedResponse(args);
            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", $"[SRC: {sender.Source}] Blocked {args.Request.Uri}");
        }
        catch (Exception ex)
        {
            // Never let a filter failure take the tab down - the request simply goes through.
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Failed to filter {args.Request.Uri}: {ex.Message}");
        }
        finally
        {
            try
            {
                deferral?.Complete();
            }
            catch (Exception)
            {
                // The WebView was closed while the request was deferred.
            }
        }
    }

    private CoreWebView2WebResourceResponse CreateBlockedResponse(CoreWebView2WebResourceRequestedEventArgs args)
    {
        // Answer with the kind of empty content each consumer accepts silently: an empty script or stylesheet
        // parses fine and a 1x1 GIF decodes, whereas an empty text/html body trips Chromium's strict MIME
        // checks (module scripts, stylesheets) or the image decoder and fires the onerror handlers
        // anti-adblock scripts listen for.
        IRandomAccessStream? content = null;
        string contentType;
        switch (args.ResourceContext)
        {
            case CoreWebView2WebResourceContext.Script:
                contentType = "application/javascript";
                break;
            case CoreWebView2WebResourceContext.Stylesheet:
                contentType = "text/css";
                break;
            case CoreWebView2WebResourceContext.Image:
                contentType = "image/gif";
                content = new MemoryStream(TransparentGif, writable: false).AsRandomAccessStream();
                break;
            case CoreWebView2WebResourceContext.Document:
                contentType = "text/html"; // blank page for blocked (i)frames
                break;
            default:
                contentType = "text/plain";
                break;
        }

        var headers = new StringBuilder(192)
            .Append("Content-Type: ").Append(contentType)
            .Append("\nCache-Control: no-store"); // keep synthetic responses out of the HTTP cache

        // Cross-origin fetch()/XHR only resolves when the response carries CORS headers; without them the
        // promise rejects exactly as if the request had been blocked.
        CoreWebView2HttpRequestHeaders requestHeaders = args.Request.Headers;
        if (requestHeaders.Contains("Origin"))
        {
            headers.Append("\nAccess-Control-Allow-Origin: ").Append(requestHeaders.GetHeader("Origin"))
                   .Append("\nAccess-Control-Allow-Credentials: true");

            if (requestHeaders.Contains("Access-Control-Request-Headers"))
                headers.Append("\nAccess-Control-Allow-Headers: ").Append(requestHeaders.GetHeader("Access-Control-Request-Headers"));
            if (requestHeaders.Contains("Access-Control-Request-Method"))
                headers.Append("\nAccess-Control-Allow-Methods: ").Append(requestHeaders.GetHeader("Access-Control-Request-Method"));
        }
        else
        {
            headers.Append("\nAccess-Control-Allow-Origin: *");
        }

        return _environment!.CreateWebResourceResponse(content, 200, "OK", headers.ToString());
    }
}
