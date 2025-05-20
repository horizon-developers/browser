namespace Horizon.Pages;

public sealed partial class SplitTabPage : Page
{
    public SplitTabPage()
    {
        this.InitializeComponent();
        WebTabCreationParams parameters = new()
        {
            LaunchURL = string.Empty,
            IsSplitTab = true
        };
        LeftFrame.Navigate(typeof(WebViewPage), parameters);
        RightFrame.Navigate(typeof(WebViewPage), parameters);
    }

    public void CloseWebViews()
    {
        (LeftFrame.Content as WebViewPage).WebViewControl.Close();
        (RightFrame.Content as WebViewPage).WebViewControl.Close();
    }
}
