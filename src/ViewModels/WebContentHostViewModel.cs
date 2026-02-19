namespace Horizon.ViewModels;

public class WebContentHostViewModel
{
    public WebContentHostViewModel()
    {
    }

    public string LaunchUrl { get; set; }
    public Tab MyTab { get; set; }
    public bool IsInPrivate { get; set; }

    public byte[] QrCode { get; set; }
}
