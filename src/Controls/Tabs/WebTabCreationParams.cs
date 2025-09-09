namespace Horizon.Controls.Tabs;

public class WebTabCreationParams
{
    public WebTabCreationParams() { }
    public string LaunchURL { get; set; }
    public Tab MyTab { get; set; }
    public bool IsInPrivate { get; set; } = false;
}
