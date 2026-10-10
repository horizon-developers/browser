namespace Horizon.Controls.Tabs;

public static class RecentTabs
{
    public static List<RecentTab> RecentTabsList { get; set; } = [];

    public static void AddTab(Tab tab)
    {
        string url = tab.WebContentInstance?.CurrentUrl;
        // Nothing to reopen, e.g. a new tab that was closed before anything was typed into it
        if (string.IsNullOrEmpty(url) || url == "about:blank")
        {
            return;
        }

        RecentTab newRecentTab = new RecentTab
        {
            Title = tab.Title,
            Url = url,
        };
        RecentTabsList.Add(newRecentTab);
    }
}
