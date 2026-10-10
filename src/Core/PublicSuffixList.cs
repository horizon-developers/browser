#nullable enable
using System.Globalization;
using System.Reflection;

namespace Horizon.Core;

/// <summary>
/// Lookups against the Public Suffix List (https://publicsuffix.org), embedded from Assets/Data/public_suffix_list.dat.
/// Mirrors what Ladybird gets from libpsl with PSL_TYPE_ANY | PSL_TYPE_NO_STAR_RULE: ICANN and private rules both count,
/// but there is no implicit "*" rule, so an unknown TLD has no public suffix at all.
/// To update the list, replace the .dat file with https://publicsuffix.org/list/public_suffix_list.dat
/// </summary>
public static class PublicSuffixList
{
    private const string ResourceName = "Horizon.public_suffix_list.dat";

    private sealed class Rules
    {
        public readonly HashSet<string> Exact = new(StringComparer.Ordinal);
        public readonly HashSet<string> Wildcard = new(StringComparer.Ordinal); // "*.ck" is stored as "ck"
        public readonly HashSet<string> Exception = new(StringComparer.Ordinal); // "!www.ck" is stored as "www.ck"
    }

    private static readonly Lazy<Rules> s_rules = new(Load);
    private static readonly IdnMapping s_idn = new();

    /// <summary>
    /// Returns the longest public suffix that the domain ends with, or null if it has none (e.g. "example.def").
    /// A trailing dot on the domain is kept on the result, like libpsl does.
    /// </summary>
    public static string? FindMatchingPublicSuffix(string domain)
    {
        domain = domain.TrimStart('.');
        string trailingDot = "";
        if (domain.EndsWith('.'))
        {
            trailingDot = ".";
            domain = domain[..^1];
        }
        if (domain.Length == 0)
            return null;

        string? lookup = ToAscii(domain);
        if (lookup is null)
            return null;

        var rules = s_rules.Value;
        var remaining = lookup.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (IsPublicSuffix(rules, remaining.ToString()))
            {
                // Hand back the suffix in the same form (unicode or punycode) the caller passed in
                int labels = remaining.Count('.') + 1;
                return LastLabels(domain, labels) + trailingDot;
            }

            int separator = remaining.IndexOf('.');
            if (separator < 0)
                return null;
            remaining = remaining[(separator + 1)..];
        }
        return null;
    }

    private static bool IsPublicSuffix(Rules rules, string name)
    {
        if (rules.Exception.Contains(name))
            return false;
        // A wildcard rule makes its parent a public suffix too ("*.ck" covers "ck"), as in libpsl
        if (rules.Exact.Contains(name) || rules.Wildcard.Contains(name))
            return true;

        int separator = name.IndexOf('.');
        return separator >= 0 && rules.Wildcard.Contains(name[(separator + 1)..]);
    }

    private static string LastLabels(string domain, int count)
    {
        int index = domain.Length;
        for (int i = 0; i < count; i++)
        {
            index = domain.LastIndexOf('.', index - 1);
            if (index < 0)
                return domain;
        }
        return domain[(index + 1)..];
    }

    private static string? ToAscii(string domain)
    {
        try
        {
            return s_idn.GetAscii(domain).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Rules Load()
    {
        var rules = new Rules();
        using var stream = typeof(PublicSuffixList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource {ResourceName}");
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is string line)
        {
            // Each rule is the first whitespace-delimited token on its line
            var rule = line.AsSpan().Trim();
            int whitespace = rule.IndexOfAny(' ', '\t');
            if (whitespace >= 0)
                rule = rule[..whitespace];
            if (rule.IsEmpty || rule.StartsWith("//"))
                continue;

            if (rule.StartsWith("!"))
                Add(rules.Exception, rule[1..]);
            else if (rule.StartsWith("*."))
                Add(rules.Wildcard, rule[2..]);
            else
                Add(rules.Exact, rule);
        }
        return rules;
    }

    private static void Add(HashSet<string> set, ReadOnlySpan<char> rule)
    {
        if (ToAscii(rule.ToString()) is string ascii)
            set.Add(ascii);
    }
}
