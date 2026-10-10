namespace Horizon.Core;

public enum UserInputClassification
{
    Search,
    InternalUrl, // A url Horizon navigates to itself
    ExternalUrl, // A url with a scheme some other app handles (mailto:, tel:, steam://...)
}

public readonly record struct ClassifiedUserInput(UserInputClassification Classification, Uri? Url = null);

/// <summary>
/// Decides whether address bar input is a url or a web search, and what url it should open.
/// Port of Ladybird's classify_user_input / sanitize_url (Libraries/LibWebView/URL.cpp, BSD-2-Clause),
/// with System.Uri standing in for Ladybird's WHATWG URL parser.
/// </summary>
public static class UrlHelper
{
    // Schemes the WebView navigates to by itself. Ladybird's list plus the WebView2 specific ones.
    private static readonly string[] InternalSchemes = ["about", "blob", "data", "file", "http", "https", "edge", "chrome-extension"];

    // These known schemes do not use an authority, so they are recognized before testing whether the text could
    // instead be a host and port.
    private static readonly string[] KnownExternalSchemes = ["geo", "mailto", "sms", "tel"];

    // Schemes that are never opened from user input. Single letter schemes (drive letters) are denied as well.
    private static readonly string[] DeniedSchemes =
    [
        "afp", "data", "disk", "disks", "file", "hcp", "ie.http", "javascript", "mk", "ms-help",
        "nntp", "res", "shell", "vbscript", "view-source", "vnd.ms.radio",
    ];

    // https://url.spec.whatwg.org/#special-scheme
    private static readonly string[] SpecialSchemes = ["ftp", "file", "http", "https", "ws", "wss"];

    // https://datatracker.ietf.org/doc/html/rfc2606
    private static readonly string[] ReservedTlds = [".test", ".example", ".invalid", ".localhost"];

    public static ClassifiedUserInput ClassifyUserInput(string location, bool appendTld = false)
    {
        location = location.Trim();
        if (location.Length == 0)
            return default;

        if (TryGetLocalFile(location) is Uri fileUrl)
            return new(UserInputClassification.InternalUrl, fileUrl);

        bool httpsSchemeWasGuessed = false;
        bool inputHasExplicitPort = false;
        string schemelessLocation = "";

        bool hasScheme = TryParseUrl(location, out string scheme, out Uri? url);

        if (hasScheme && url is not null && IsUrlHandledInternally(scheme))
        {
            // Continue below to apply the domain and public suffix checks
        }
        else if (hasScheme && IsDeniedScheme(scheme))
        {
            return default;
        }
        else if (hasScheme && KnownExternalSchemes.Contains(scheme))
        {
            return url is null ? default : new(UserInputClassification.ExternalUrl, url);
        }
        else if (HasValidExplicitPort(location))
        {
            schemelessLocation = location;
            if (!Uri.TryCreate("https://" + location, UriKind.Absolute, out url))
                return default;

            httpsSchemeWasGuessed = true;
            inputHasExplicitPort = true;
        }
        else if (hasScheme)
        {
            return url is null ? default : new(UserInputClassification.ExternalUrl, url);
        }
        else
        {
            schemelessLocation = location;

            // A bare IPv6 address can't be a url host until it's wrapped in square brackets. Wrap it here, so that
            // e.g. "::1" becomes a navigation instead of falling through to a search, like Chromium does.
            if (location.IndexOfAny(['[', '%']) < 0 &&
                IPAddress.TryParse(location, out var ipv6Address) &&
                ipv6Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                schemelessLocation = $"[{ipv6Address}]";
            }
            // Horizon: The url parser accepts abbreviated IPv4 addresses, which would turn numbers like "3.14" or "88"
            // into a navigation to 3.0.0.14 or 0.0.0.88. Only recognize an IPv4 address with all four parts present.
            else if (IsAbbreviatedIPv4(location))
            {
                return default;
            }

            if (!Uri.TryCreate("https://" + schemelessLocation, UriKind.Absolute, out url) || url.Scheme != Uri.UriSchemeHttps)
                return default;

            httpsSchemeWasGuessed = true;
        }

        if (!IsUrlHandledInternally(url.Scheme))
            return default;

        if (url.HostNameType is UriHostNameType.Dns or UriHostNameType.Basic && url.Host.Length > 0)
        {
            string domain = url.Host;

            if (domain.Contains('"'))
                return default;

            bool hasReservedTld = ReservedTlds.Any(tld => domain.Length > tld.Length && domain.EndsWith(tld, StringComparison.OrdinalIgnoreCase));

            if (!hasReservedTld)
            {
                string? publicSuffix = PublicSuffixList.FindMatchingPublicSuffix(domain);
                if (publicSuffix is null || publicSuffix == domain)
                {
                    if (appendTld)
                        url = new UriBuilder(url) { Host = domain + ".com" }.Uri;
                    else if (httpsSchemeWasGuessed && !IsLoopbackOrLocalhost(url) && !inputHasExplicitPort)
                        return default;
                }
            }
        }

        // We guessed "https" above without knowing the host. Now that the host is known, guess "http" instead where
        // https can't reasonably work. Reparsing the input keeps intact a port that's default for one scheme but not
        // the other. An appendTld rewrite above never produces a host for which this branch is taken, so the rewrite
        // can't be lost by reparsing.
        if (httpsSchemeWasGuessed && ShouldGuessHttpSchemeForHost(url) &&
            Uri.TryCreate("http://" + schemelessLocation, UriKind.Absolute, out var httpUrl))
        {
            url = httpUrl;
        }

        return new(UserInputClassification.InternalUrl, url);
    }

    /// <summary>
    /// Returns the url to navigate to, or null when the input should be searched for instead.
    /// </summary>
    public static Uri? SanitizeUrl(string location, bool appendTld = false)
    {
        var classifiedInput = ClassifyUserInput(location, appendTld);
        return classifiedInput.Classification == UserInputClassification.InternalUrl ? classifiedInput.Url : null;
    }

    /// <summary>
    /// Returns what the address bar should open for the input: the url it names, or a web search for it.
    /// </summary>
    public static string ToNavigationUrl(string location, string searchUrl, bool appendTld = false)
    {
        location = location.Trim();
        return SanitizeUrl(location, appendTld)?.AbsoluteUri ?? FormatSearchQuery(searchUrl, location);
    }

    public static string FormatSearchQuery(string searchUrl, string query) => searchUrl + Uri.EscapeDataString(query.Trim());

    private static bool IsUrlHandledInternally(string scheme) => InternalSchemes.Contains(scheme);

    private static bool IsDeniedScheme(string scheme) => scheme.Length == 1 || DeniedSchemes.Contains(scheme);

    /// <summary>
    /// Mimics the WHATWG parser's decision whether the input is an absolute url: it needs a syntactically valid
    /// scheme, and for special schemes a valid authority. Non-special urls ("site:example.com") always parse there,
    /// so <paramref name="url"/> may be null for the rare ones System.Uri rejects.
    /// </summary>
    private static bool TryParseUrl(string input, out string scheme, out Uri? url)
    {
        url = null;
        scheme = "";

        int colon = input.IndexOf(':');
        if (colon <= 0 || !char.IsAsciiLetter(input[0]))
            return false;
        for (int i = 1; i < colon; i++)
        {
            char c = input[i];
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('+' or '-' or '.'))
                return false;
        }
        scheme = input[..colon].ToLowerInvariant();

        // System.Uri would turn "C:\path" into a file url, so its result only counts when the scheme agrees
        if (Uri.TryCreate(input, UriKind.Absolute, out var parsed) && parsed.Scheme == scheme)
            url = parsed;

        return url is not null || !SpecialSchemes.Contains(scheme);
    }

    // Borrowed from Ladybird's url_from_text
    private static bool IsAbbreviatedIPv4(string input)
    {
        int hostnameEnd = input.IndexOfAny([':', '/', '?', '#']);
        var hostname = hostnameEnd < 0 ? input : input[..hostnameEnd];
        if (!hostname.All(c => char.IsAsciiDigit(c) || c == '.'))
            return false;

        var parts = hostname.Split('.');
        return parts.Length != 4 || parts.Any(part => part.Length == 0);
    }

    private static bool HasValidExplicitPort(string input)
    {
        int authorityEnd = input.IndexOfAny(['/', '?', '#']);
        var authority = authorityEnd < 0 ? input : input[..authorityEnd];

        int portSeparator;
        if (authority.StartsWith('['))
        {
            int closingBracket = authority.IndexOf(']');
            if (closingBracket < 0 || closingBracket + 1 >= authority.Length || authority[closingBracket + 1] != ':')
                return false;
            portSeparator = closingBracket + 1;
        }
        else
        {
            portSeparator = authority.LastIndexOf(':');
            if (portSeparator < 0)
                return false;
        }

        var port = authority[(portSeparator + 1)..];
        if (port.Length == 0 || !port.All(char.IsAsciiDigit))
            return false;

        return Uri.TryCreate("https://" + input, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps;
    }

    // https://w3c.github.io/webappsec-secure-contexts/#is-origin-trustworthy
    private static bool IsLoopbackOrLocalhost(Uri url)
    {
        // 127.0.0.0/8 or ::1/128
        if (TryGetIPAddress(url, out var address))
            return address.AddressFamily == AddressFamily.InterNetwork ? address.GetAddressBytes()[0] == 127 : address.Equals(IPAddress.IPv6Loopback);

        string host = url.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("localhost.", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost.", StringComparison.OrdinalIgnoreCase);
    }

    // When guessing the scheme for schemeless input, we prefer "https", except for hosts that can't generally obtain
    // publicly-trusted TLS certs, and whose traffic never leaves the machine: loopback addresses and localhost names
    // (which the Secure Contexts spec treats as "potentially trustworthy", even over plain http), as well as the
    // unspecified addresses 0.0.0.0 and [::] (which reach loopback in practice), and private-use and link-local
    // addresses. Gecko and Blink likewise exempt all of those hosts from their HTTPS-upgrade machinery.
    private static bool ShouldGuessHttpSchemeForHost(Uri url)
    {
        if (IsLoopbackOrLocalhost(url))
            return true;

        if (!TryGetIPAddress(url, out var address))
            return false;

        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 0                                    // 0.0.0.0/8: "this network" (RFC 791)
                || bytes[0] == 10                                   // 10.0.0.0/8: private use (RFC 1918)
                || (bytes[0] == 172 && (bytes[1] & 0xf0) == 16)     // 172.16.0.0/12: private use (RFC 1918)
                || (bytes[0] == 192 && bytes[1] == 168)             // 192.168.0.0/16: private use (RFC 1918)
                || (bytes[0] == 169 && bytes[1] == 254);            // 169.254.0.0/16: link-local (RFC 3927)
        }

        return address.Equals(IPAddress.IPv6Any)                    // [::]: the unspecified address
            || (bytes[0] & 0xfe) == 0xfc                            // fc00::/7: unique local (RFC 4193)
            || (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80);     // fe80::/10: link-local (RFC 4291)
    }

    private static bool TryGetIPAddress(Uri url, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IPAddress? address)
    {
        address = null;
        if (url.HostNameType is not (UriHostNameType.IPv4 or UriHostNameType.IPv6))
            return false;
        return IPAddress.TryParse(url.Host.Trim('[', ']'), out address);
    }

    // Lets an existing local file or folder be opened by typing (or pasting a quoted) absolute path
    private static Uri? TryGetLocalFile(string location)
    {
        if (location.Length >= 2 && location.StartsWith('"') && location.EndsWith('"'))
            location = location[1..^1];

        try
        {
            if (!Path.IsPathFullyQualified(location) || !(File.Exists(location) || Directory.Exists(location)))
                return null;
            return new Uri(Path.GetFullPath(location));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UriFormatException)
        {
            return null;
        }
    }
}
