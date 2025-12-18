namespace Horizon.Controls.InfoBars;

public static class LinkCopied
{
    private static InfoBar InternalInfoBar;
    
    public static InfoBar Get()
    {
        if (InternalInfoBar == null)
        {
            InternalInfoBar = new InfoBar
            {
                Width = 150,
                IsOpen = true,
                Message = "Link copied!",
                IsClosable = false,
                Severity = InfoBarSeverity.Success
            };
        }
        return InternalInfoBar;
    }
}
