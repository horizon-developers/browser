namespace Horizon.ViewModels;

public class WebContentHostViewModel
{
    public WebContentHostViewModel()
    {
    }

    public string LaunchUrl { get; set; } = string.Empty;
    public Tab MyTab { get; set; } = null!; // Assigned by WebContentHost's constructor

    #region Context menu
    public string? SelectionText { get; set; }
    public string? LinkUri { get; set; }
    #endregion

    public byte[]? QrCode { get; set; }
}
