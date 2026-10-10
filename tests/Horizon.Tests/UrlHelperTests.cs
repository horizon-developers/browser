using System.Net;
using Horizon.Core;

namespace Horizon.Tests;

// Mostly ported from Ladybird's Tests/LibWebView/TestWebViewURL.cpp, so the behaviour stays in line with it
[TestClass]
public sealed class UrlHelperTests
{
    private const string SearchUrl = "https://ecosia.org/search?q=";

    private static void ExpectUrl(string expected, string input, bool appendTld = false)
    {
        Assert.AreEqual(expected, UrlHelper.SanitizeUrl(input, appendTld)?.AbsoluteUri, $"input: {input}");
        Assert.AreEqual(expected, UrlHelper.ToNavigationUrl(input, SearchUrl, appendTld), $"input: {input}");
    }

    private static void ExpectSearch(string input)
    {
        Assert.IsNull(UrlHelper.SanitizeUrl(input), $"input: {input}");
        Assert.AreEqual(SearchUrl + Uri.EscapeDataString(input.Trim()), UrlHelper.ToNavigationUrl(input, SearchUrl), $"input: {input}");
    }

    private static void ExpectClassification(string input, UserInputClassification expected, string? expectedUrl = null)
    {
        var classified = UrlHelper.ClassifyUserInput(input);
        Assert.AreEqual(expected, classified.Classification, $"input: {input}");
        Assert.AreEqual(expectedUrl, classified.Url?.AbsoluteUri, $"input: {input}");
    }

    [TestMethod]
    [DataRow("hello")]
    [DataRow("hello world")]
    [DataRow("\"example.org\"")]
    [DataRow("\"example.org")]
    [DataRow("\"http://example.org\"")]
    [DataRow("example.org hello")]
    [DataRow("http://example.org and example sites")]
    [DataRow("ftp://example.org")]
    [DataRow("https://exa\"mple.com/what")]
    [DataRow("how to cook")]
    [DataRow("c# tips")]
    [DataRow("1+1")]
    [DataRow("3.14")] // Not an abbreviated IPv4 address
    [DataRow("88")]
    [DataRow("192.168")]
    [DataRow("192.168.0")]
    [DataRow("192.168.0.999")]
    [DataRow("")]
    [DataRow("   ")]
    public void PlainTextIsSearched(string input) => ExpectSearch(input);

    [TestMethod]
    public void UrlsWithSpacesAndQuotesAreEscaped()
    {
        ExpectUrl("https://example.com/%20some%20cool%20page", "https://example.com/ some cool page");
        ExpectUrl("https://example.com/some%20cool%20page", "https://example.com/some cool page");
        ExpectUrl("https://example.com/%22what%22", "https://example.com/\"what\"");
    }

    [TestMethod]
    [DataRow("https://example.org/", "example.org")] // Valid domain
    [DataRow("https://example.abc/", "example.abc")] // .abc is a recognized TLD
    [DataRow("https://www.example.co.uk/", "www.example.co.uk")]
    [DataRow("https://github.com/user/repo?tab=readme#install", "github.com/user/repo?tab=readme#install")]
    [DataRow("https://example.test/path", "example.test/path")] // Reserved TLDs
    [DataRow("https://example.example/path", "example.example/path")]
    [DataRow("https://example.invalid/path", "example.invalid/path")]
    [DataRow("http://example.localhost/path", "example.localhost/path")] // .localhost names get the http guess
    [DataRow("https://example.org/", "  example.org  ")]
    [DataRow("https://example.org/", "EXAMPLE.ORG")]
    public void DomainsWithoutSchemeGuessHttps(string expected, string input) => ExpectUrl(expected, input);

    [TestMethod]
    [DataRow("example.def")] // Invalid domain but no scheme: search (like Firefox or Chrome)
    [DataRow("co.uk")] // A public suffix alone is not a site
    [DataRow("com")]
    public void UnknownOrBareSuffixesAreSearched(string input) => ExpectSearch(input);

    [TestMethod]
    public void ExplicitSchemeIsRespected()
    {
        ExpectUrl("https://example.org/", "https://example.org");
        // Respect the user if the url has a valid scheme but not a public suffix (.def is not a recognized TLD)
        ExpectUrl("https://example.def/", "https://example.def");
        ExpectUrl("http://example.org/", "http://example.org");
        ExpectUrl("http://intranet/", "http://intranet");
    }

    [TestMethod]
    // Respect localhost, and guess http (not https) for it: loopback and localhost hosts can't generally obtain a
    // publicly-trusted TLS cert, and their traffic never leaves the machine.
    [DataRow("http://localhost/", "localhost")]
    [DataRow("http://localhost:8000/", "localhost:8000")]
    [DataRow("http://localhost/hello", "localhost/hello")]
    [DataRow("http://localhost/hello.world", "localhost/hello.world")]
    [DataRow("http://localhost/hello.world?query=123", "localhost/hello.world?query=123")]
    [DataRow("http://localhost:5173/", "LOCALHOST:5173")]
    [DataRow("http://127.0.0.1/", "127.0.0.1")]
    [DataRow("http://127.0.0.1:8000/", "127.0.0.1:8000")]
    [DataRow("http://127.0.0.1:8000/api/v1?x=1", "127.0.0.1:8000/api/v1?x=1")]
    [DataRow("http://[::1]/", "::1")] // A bare IPv6 address gets bracketed
    [DataRow("http://[::1]:8000/", "[::1]:8000")]
    [DataRow("http://0.0.0.0:8000/", "0.0.0.0:8000")] // The unspecified address reaches loopback in practice
    public void LocalhostAndLoopbackGuessHttp(string expected, string input) => ExpectUrl(expected, input);

    [TestMethod]
    // Private-use and link-local addresses also get the http guess. Like loopback hosts, they can't obtain
    // publicly-trusted TLS certs.
    [DataRow("http://192.168.0.42:8000/", "192.168.0.42:8000")]
    [DataRow("http://192.168.1.1/", "192.168.1.1")]
    [DataRow("http://10.0.0.5:3000/", "10.0.0.5:3000")]
    [DataRow("http://172.16.0.1/", "172.16.0.1")]
    [DataRow("http://172.31.255.254/", "172.31.255.254")]
    [DataRow("http://169.254.1.1/", "169.254.1.1")]
    [DataRow("http://[fe80::1]/", "[fe80::1]")]
    [DataRow("http://[fd00::1]:8080/", "[fd00::1]:8080")]
    public void PrivateAddressesGuessHttp(string expected, string input) => ExpectUrl(expected, input);

    [TestMethod]
    // Addresses just outside those ranges keep the https guess
    [DataRow("https://172.32.0.1/", "172.32.0.1")]
    [DataRow("https://11.0.0.1/", "11.0.0.1")]
    [DataRow("https://100.64.0.1/", "100.64.0.1")] // Shared space (CGNAT) is not local
    [DataRow("https://8.8.8.8/", "8.8.8.8")]
    [DataRow("https://[2001:db8::1]/", "[2001:db8::1]")]
    public void PublicAddressesGuessHttps(string expected, string input) => ExpectUrl(expected, input);

    [TestMethod]
    public void ExplicitSchemeIsNeverRewritten()
    {
        ExpectUrl("https://localhost:8000/", "https://localhost:8000");
        ExpectUrl("https://127.0.0.1/", "https://127.0.0.1");
    }

    [TestMethod]
    public void PortsSurviveTheHttpGuess()
    {
        // Reparsing with the http guess keeps a port that's default for https only, and drops a port that's default for http
        ExpectUrl("http://localhost:443/", "localhost:443");
        ExpectUrl("http://localhost/", "localhost:80");
    }

    [TestMethod]
    public void UnbracketedIPv6PortIsPartOfTheAddress()
    {
        // Without brackets, ":8000" parses as part of the IPv6 address, not as a port.
        // System.Uri serializes ::1:8000 as the equivalent ::0.1.128.0, so compare the address instead of the text.
        var url = UrlHelper.SanitizeUrl("::1:8000");
        Assert.IsNotNull(url);
        Assert.AreEqual(Uri.UriSchemeHttps, url.Scheme);
        Assert.IsTrue(url.IsDefaultPort);
        Assert.AreEqual(IPAddress.Parse("::1:8000"), IPAddress.Parse(url.Host.Trim('[', ']')));
    }

    [TestMethod]
    // Single-label hostnames other than "localhost" still search, like in other browsers, even if the system resolver
    // might know them (e.g. from the hosts file)
    [DataRow("localhost4")]
    [DataRow("localhost6")]
    [DataRow("intranet")]
    public void SingleLabelHostsAreSearched(string input) => ExpectSearch(input);

    [TestMethod]
    [DataRow("https://example.com/", "example")] // User holds down the Ctrl key
    [DataRow("https://example.def.com/", "example.def")]
    [DataRow("https://com.com/", "com")]
    [DataRow("https://localhost.com/", "localhost")] // localhost.com is a public site
    [DataRow("http://127.0.0.1/", "127.0.0.1")] // No TLD is appended to an IP address
    [DataRow("https://example.com/index.html", "example/index.html")]
    public void AppendTld(string expected, string input) => ExpectUrl(expected, input, appendTld: true);

    [TestMethod]
    public void AppendTldStillSearchesPlainText()
    {
        Assert.AreEqual(SearchUrl + "hello%20world", UrlHelper.ToNavigationUrl("hello world", SearchUrl, appendTld: true));
    }

    [TestMethod]
    [DataRow("whatever:example.com")]
    [DataRow("site:example.com")]
    [DataRow("steam://launch/1536610")]
    [DataRow("mailto:hello@example.com")]
    [DataRow("geo:23.4,12.4")]
    [DataRow("javascript:alert(1)")]
    [DataRow("javascript://alert")]
    [DataRow("vbscript:msgbox")]
    [DataRow("view-source:https://example.com")]
    public void NonNavigableSchemesAreSearched(string input) => ExpectSearch(input);

    [TestMethod]
    public void HostWithPortGuessesHttps()
    {
        ExpectUrl("https://host:8080/", "host:8080");
    }

    [TestMethod]
    public void ClassifyUserInput()
    {
        ExpectClassification("example.org", UserInputClassification.InternalUrl, "https://example.org/");
        ExpectClassification("localhost:8080", UserInputClassification.InternalUrl, "http://localhost:8080/");
        ExpectClassification("example.com:8080", UserInputClassification.InternalUrl, "https://example.com:8080/");
        ExpectClassification("host:8080/path?query#fragment", UserInputClassification.InternalUrl, "https://host:8080/path?query#fragment");
        ExpectClassification("host:0", UserInputClassification.InternalUrl, "https://host:0/");
        ExpectClassification("host:65535", UserInputClassification.InternalUrl, "https://host:65535/");

        ExpectClassification("tel:123", UserInputClassification.ExternalUrl, "tel:123");
        ExpectClassification("sms:123", UserInputClassification.ExternalUrl, "sms:123");
        ExpectClassification("mailto:hello@example.com", UserInputClassification.ExternalUrl, "mailto:hello@example.com");
        ExpectClassification("geo:23.4,12.4", UserInputClassification.ExternalUrl, "geo:23.4,12.4");
        ExpectClassification("steam://launch/1536610", UserInputClassification.ExternalUrl, "steam://launch/1536610");
        ExpectClassification("site:example.com", UserInputClassification.ExternalUrl, "site:example.com");
        ExpectClassification("host:65536", UserInputClassification.ExternalUrl, "host:65536");

        ExpectClassification("hello", UserInputClassification.Search);
        ExpectClassification("hello world", UserInputClassification.Search);
        ExpectClassification("javascript:alert(1)", UserInputClassification.Search);
        ExpectClassification(@"C:\does\not\exist.html", UserInputClassification.Search); // Single letter schemes are denied
    }

    [TestMethod]
    public void AboutAndDataUrls()
    {
        ExpectUrl("about:blank", "about:blank");
        ExpectUrl("about:version", "about:version");
        ExpectUrl("data:text/html", "data:text/html");

        ExpectSearch("about");
        ExpectSearch("about blabla:");
        ExpectSearch("blabla about:");
        ExpectSearch("data text/html");
        ExpectSearch("text/html data:");
    }

    [TestMethod]
    [DataRow("edge://settings/", "edge://settings")]
    [DataRow("edge://flags/", "edge://flags/")]
    [DataRow("chrome-extension://abcdefghijklmnop/popup.html", "chrome-extension://abcdefghijklmnop/popup.html")]
    [DataRow("file:///C:/Users/test/index.html", "file:///C:/Users/test/index.html")]
    public void WebViewSchemes(string expected, string input) => ExpectUrl(expected, input);

    [TestMethod]
    public void ExistingLocalPathsOpenAsFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Horizon Tests " + Guid.NewGuid().ToString("N"));
        string file = Path.Combine(directory, "page.html");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(file, "<p>hi</p>");
            string expected = new Uri(file).AbsoluteUri;

            ExpectUrl(expected, file);
            ExpectUrl(expected, $"\"{file}\""); // Explorer's "Copy as path" adds quotes
            ExpectUrl(new Uri(directory).AbsoluteUri, directory);
            Assert.IsTrue(UrlHelper.SanitizeUrl(file)!.IsFile);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void RelativePathsAreNotTreatedAsFiles()
    {
        // A file in the working directory must not hijack a domain typed into the address bar
        string name = "horizon-test-" + Guid.NewGuid().ToString("N") + ".com";
        File.WriteAllText(name, "");
        try
        {
            ExpectUrl($"https://{name}/", name);
        }
        finally
        {
            File.Delete(name);
        }
    }

    [TestMethod]
    public void SearchQueriesAreEscaped()
    {
        Assert.AreEqual(SearchUrl + "c%23%20tips%20%26%20tricks", UrlHelper.FormatSearchQuery(SearchUrl, "c# tips & tricks"));
        Assert.AreEqual(SearchUrl + "hello%20world", UrlHelper.ToNavigationUrl("  hello world ", SearchUrl));
    }
}
