namespace Horizon.Controls.Tabs;

public class TabCreationParams
{
    public TabCreationParams() { }
    public required string LaunchUrl { get; set; }
    public required Tab MyTab { get; set; }
    /// <summary>
    /// Set when the tab hosts a window opened by a page (window.open, target=_blank). Instead of navigating to
    /// LaunchUrl, the tab hands its CoreWebView2 back through this so it can become the opener's NewWindow.
    /// </summary>
    public TaskCompletionSource<CoreWebView2>? NewWindowTarget { get; set; }
}
