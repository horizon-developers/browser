namespace Horizon.ViewModels;

public class WebContentHostViewModel
{
    public WebContentHostViewModel()
    {
    }

    public string LaunchUrl { get; set; }
    public Tab MyTab { get; set; }
    public bool IsInPrivate { get; set; }

    #region Context menu
    public string SelectionText { get; set; }
    public string LinkUri { get; set; }
    #endregion

    public byte[] QrCode { get; set; }
}
