namespace Horizon.Core;

public static partial class UrlHelper
{
    private static readonly Regex UrlMatch = UrlRegex();
    private static readonly Regex IPMatch = IPRegex();
    public static string GetInputType(string input)
    {
        string type;

        if (input.StartsWith("http://") || input.StartsWith("https://") || input.StartsWith("edge://") || input.StartsWith("file:///"))
        {
            type = "url";
        }
        else if (UrlMatch.IsMatch(input) || IPMatch.IsMatch(input))
        {
            type = "urlNOProtocol";
        }
        else
        {
            type = "searchquery";
        }
        return type;
    }

    [GeneratedRegex("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?::([0-9]{1,5}))?$", RegexOptions.Singleline)]
    public static partial Regex IPRegex();
    [GeneratedRegex(@"^(http(s)?:\/\/)?(www.)?([a-zA-Z0-9])+([\-\.]{1}[a-zA-Z0-9]+)*\.[a-zA-Z]{2,63}(:[0-9]{1,5})?(\/[^\s]*)?$", RegexOptions.Singleline)]
    public static partial Regex UrlRegex();
}