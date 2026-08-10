using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace Horizon.Core;

internal static class ABEDatabase
{
    private const string SourceUrl = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";
    private const int Magic = 0x42444C4B; // "BDLK"
    private const int Version = 3;        // v3: header now embeds a unix epoch timestamp
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    private static FrozenSet<string> _blocklist = [];
    private static bool _isLoaded;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    private static string CacheFilePath => Path.Combine(GetWritableCacheDirectory(), "Unified.bin");

    private static string GetWritableCacheDirectory()
    {
        return ApplicationData.Current.LocalFolder.Path;
    }

    internal static async Task LoadBlocklistAsync()
    {
        if (_isLoaded) return;

        try
        {
            string cachePath = CacheFilePath;
            long cachedEpoch = TryReadCachedEpoch(cachePath);
            long nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool cacheMissing = cachedEpoch < 0;
            bool cacheStale = !cacheMissing && (nowEpoch - cachedEpoch) > MaxAge.TotalSeconds;

            if (cacheMissing || cacheStale)
            {
                Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",
                    cacheMissing ? "No local cache found, fetching blocklist." : "Cache older than 24h, refreshing blocklist.");

                bool refreshed = await TryRefreshCacheAsync(cachePath);

                if (!refreshed && cacheMissing)
                {
                    Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine",
                        "Fetch failed and no cache exists - ad blocking disabled for this session.");
                    _isLoaded = true;
                    return;
                }
                // If refresh failed but a stale cache exists, fall through and load it anyway -
                // stale blocking beats no blocking.
            }

            LoadFromDisk(cachePath);
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine", $"Failed to load blocklist: {ex.Message}");
        }
    }

    private static long TryReadCachedEpoch(string binPath)
    {
        if (!File.Exists(binPath)) return -1;

        try
        {
            using var fs = new FileStream(binPath, FileMode.Open, FileAccess.Read);
            Span<byte> header = stackalloc byte[16]; // magic(4) + version(4) + epoch(8)
            if (fs.Read(header) != 16) return -1;
            if (BinaryPrimitives.ReadInt32LittleEndian(header) != Magic) return -1;

            return BinaryPrimitives.ReadInt64LittleEndian(header[8..]);
        }
        catch
        {
            return -1;
        }
    }

    private static async Task<bool> TryRefreshCacheAsync(string cachePath)
    {
        try
        {
            string hostsText = await _http.GetStringAsync(SourceUrl);
            var domains = ParseHostsFile(hostsText);

            WriteBinAtomic(cachePath, domains, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",
                $"Fetched and cached {domains.Count} domains from upstream.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AdBlockEngine",
                $"Blocklist fetch failed, will use existing cache if present: {ex.Message}");
            return false;
        }
    }

    private static List<string> ParseHostsFile(string text)
    {
        var domains = new List<string>(capacity: 100_000);

        int start = 0;
        int len = text.Length;
        while (start < len)
        {
            int end = text.IndexOf('\n', start);
            if (end == -1) end = len;

            ReadOnlySpan<char> line = text.AsSpan(start, end - start).Trim();
            start = end + 1;

            if (line.IsEmpty || line[0] == '#') continue;

            // StevenBlack's list is "0.0.0.0 domain.tld" - we only want the domain.
            if (line.StartsWith("0.0.0.0"))
                line = line[7..].Trim();
            else if (line.StartsWith("127.0.0.1"))
                line = line[9..].Trim();
            else
                continue; // not a mapping line (stray localhost entries, etc.)

            if (line.IsEmpty) continue;

            // Strip trailing inline comments, e.g. "0.0.0.0 example.com # some note"
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash].Trim();

            if (!line.IsEmpty)
                domains.Add(line.ToString());
        }

        return domains;
    }

    private static void WriteBinAtomic(string path, List<string> domains, long epochSeconds)
    {
        // Write to temp + move-into-place so a crash mid-write never corrupts the cache
        // the next launch tries to read.
        string tempPath = path + ".tmp";

        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(epochSeconds);
            writer.Write(domains.Count);

            foreach (var domain in domains)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(domain);
                writer.Write((ushort)bytes.Length);
                writer.Write(bytes);
            }
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static void LoadFromDisk(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        var span = data.AsSpan();

        int offset = 0;
        _ = BinaryPrimitives.ReadInt32LittleEndian(span[offset..]); offset += 4; // magic
        _ = BinaryPrimitives.ReadInt32LittleEndian(span[offset..]); offset += 4; // version
        long epoch = BinaryPrimitives.ReadInt64LittleEndian(span[offset..]); offset += 8;
        int count = BinaryPrimitives.ReadInt32LittleEndian(span[offset..]); offset += 4;

        var domains = new string[count];
        for (int i = 0; i < count; i++)
        {
            ushort len = BinaryPrimitives.ReadUInt16LittleEndian(span[offset..]);
            offset += 2;
            domains[i] = Encoding.UTF8.GetString(span.Slice(offset, len));
            offset += len;
        }

        _blocklist = domains.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        long ageHours = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - epoch) / 3600;
        Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",
            $"Loaded {_blocklist.Count} domains from cache (age: {ageHours}h).");
    }

    internal static bool IsAdDomain(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        var lookup = _blocklist.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.Contains(host)) return true;

        int nextDot = host.IndexOf('.');
        while (nextDot != -1)
        {
            if (_blocklist.Contains(host[(nextDot + 1)..])) return true;
            nextDot = host.IndexOf('.', nextDot + 1);
        }
        return false;
    }
}

internal class AdBlockEngine
{
    public async Task InitBrowserAsync(CoreWebView2 webView)
    {
        Stopwatch watch = Stopwatch.StartNew();
        await ABEDatabase.LoadBlocklistAsync();
        watch.Stop();
        Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine",$"Init engine in {watch.ElapsedMilliseconds}ms");

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
                    StatusCode: 200,
                    ReasonPhrase: "OK",
                    Headers: "Content-Type: text/html");

                args.Response = response;
                Logger.LogEvent(Logger.Severity.Info, "AdBlockEngine", $"[SRC: {sender.Source}] Blocked " + args.Request.Uri);
            }
        }
    }
}