namespace Horizon.Pages;

public sealed partial class NewTabPage : Page
{
    private Tab myTab;

    public NewTabPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter != null)
        {
            myTab = (Tab)e.Parameter;
        }
    }

    private void UrlBox_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as TextBox).Focus(FocusState.Programmatic);
    }

    private void UrlBoxKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.KeyboardAccelerator.Key == WS.VirtualKey.Enter)
        {
            string input = UrlBox.Text;
            if (!string.IsNullOrEmpty(input))
            {
                ProcessQueryAndGo(input);
            }
        }
    }

    private void ProcessQueryAndGo(string input)
    {
        string inputtype = UrlHelper.GetInputType(input);
        if (inputtype == "urlNOProtocol")
            NavigateToUrl("https://" + input.Trim());
        else if (inputtype == "url")
            NavigateToUrl(input.Trim());
        else
        {
            string query = SettingsHelper.CurrentSearchUrl + input;
            NavigateToUrl(query);
        }
    }

    private void NavigateToUrl(string query)
    {
        WebTabCreationParams parameters = new() { LaunchURL = query, myTab = myTab };
        Frame.Navigate(typeof(WebViewPage), parameters, new DrillInNavigationTransitionInfo());
    }
}
