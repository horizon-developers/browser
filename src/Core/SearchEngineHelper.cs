namespace Horizon.Core;

public class SearchEngineHelper
{
    public static void SetSearchEngine(SearchEngine engine)
    {
        SettingsHelper.SetSetting("EngineFriendlyName", engine.EngineFriendlyName);
        SettingsHelper.SetSetting("SearchUrl", engine.SearchUrl);
        SettingsHelper.CurrentSearchUrl = engine.SearchUrl;
        return;
    }

    public static IReadOnlyList<SearchEngine> SearchEngines = new SearchEngine[]
    {
        new("Ask", "https://www.ask.com/web?q="),
        new("Baidu", "https://www.baidu.com/s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&tn=baidu&wd="),
        new("Bing", "https://www.bing.com?q="),
        new("Brave Search", "https://search.brave.com/search?q="),
        new("DuckDuckGo", "https://www.duckduckgo.com?q="),
        new("Ecosia", "https://www.ecosia.org/search?q="),
        new("Google", "https://www.google.com/search?q="),
        new("Startpage", "https://www.startpage.com/search?q="),
        new("Qwant", "https://www.qwant.com/?q="),
        new("Unduck", "https://unduck.link?q="),
        new("Yahoo!", "https://search.yahoo.com/search?p="),
        new("Yandex", "https://yandex.com/search/?text=")
    };
}
