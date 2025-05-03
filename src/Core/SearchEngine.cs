namespace Horizon.Core;

public class SearchEngine(string EngineFriendlyName, string SearchUrl)
{
    public string EngineFriendlyName { get; set; } = EngineFriendlyName;
    public string SearchUrl { get; set; } = SearchUrl;
}