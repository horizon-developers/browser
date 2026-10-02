namespace Horizon.Controls.Tabs;

public static class RecentTabs
{
    public static List<RecentTab> RecentTabsList { get; set; } = [];

    public static void AddTab(Tab tab)
    {
        RecentTab newRecentTab = new RecentTab
        {
            Title = tab.Title,
            Url = tab.WebContentInstance.WebContentControl.CoreWebView2.Source,
        };
        RecentTabsList.Add(newRecentTab);
    }
}
