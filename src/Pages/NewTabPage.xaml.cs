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
            var parameters = (XAMLTabCreationParams)e.Parameter;

            myTab = parameters.myTab;
        }
    }

    private void UrlBox_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as TextBox).Focus(FocusState.Programmatic);
    }

    private void UrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == WS.VirtualKey.Enter)
        {
            string input = (sender as TextBox).Text;
            if (!string.IsNullOrEmpty(input))
            {
                ProcessQueryAndGo(input);
            }
        }
    }

    private void NavigateToUrl(string query)
    {
        WebTabCreationParams parameters = new() { LaunchURL = query, myTab = myTab };
        Frame.Navigate(typeof(WebViewPage), parameters, new DrillInNavigationTransitionInfo());
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
}
