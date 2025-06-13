namespace Horizon.Controls.Tabs;

public class WebTabCreationParams
{
    public WebTabCreationParams() { }
    public string LaunchURL { get; set; }
    public Tab MyTab { get; set; }
    public bool IsSplitTab { get; set; } = false;
}
