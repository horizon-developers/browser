namespace Horizon.Controls.Tabs;

public class TabCreationParams
{
    public TabCreationParams() { }
    public string LaunchUrl { get; set; }
    public Tab MyTab { get; set; }
    public bool IsInPrivate { get; set; } = false;
}
