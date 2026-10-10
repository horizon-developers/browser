using Horizon.Core;

namespace Horizon.Tests;

[TestClass]
public sealed class PublicSuffixListTests
{
    [TestMethod]
    [DataRow("com", "example.com")]
    [DataRow("com", "www.example.com")]
    [DataRow("co.uk", "example.co.uk")]
    [DataRow("co.uk", "co.uk")]
    [DataRow("com", "com")]
    [DataRow("github.io", "user.github.io")] // Private domains count too
    [DataRow("com.", "example.com.")] // A trailing dot is kept
    [DataRow("中国", "example.中国")] // Unicode TLDs
    [DataRow("xn--fiqs8s", "example.xn--fiqs8s")] // Same TLD in punycode
    public void FindsPublicSuffix(string expected, string domain) =>
        Assert.AreEqual(expected, PublicSuffixList.FindMatchingPublicSuffix(domain));

    [TestMethod]
    public void WildcardAndExceptionRules()
    {
        // *.ck and !www.ck
        Assert.AreEqual("foo.ck", PublicSuffixList.FindMatchingPublicSuffix("example.foo.ck"));
        Assert.AreEqual("ck", PublicSuffixList.FindMatchingPublicSuffix("www.ck"));
    }

    [TestMethod]
    [DataRow("example.def")]
    [DataRow("localhost")]
    [DataRow("intranet")]
    [DataRow("")]
    [DataRow(".")]
    public void UnknownSuffixesHaveNoMatch(string domain) =>
        Assert.IsNull(PublicSuffixList.FindMatchingPublicSuffix(domain));
}
