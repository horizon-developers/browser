namespace Horizon.Controls.Tabs;

public partial class Tab : ObservableObject
{
    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Fixed when the tab is created, a tab never switches between InPrivate and regular browsing
    /// </summary>
    public bool IsInPrivate { get; init; }

    private string _domain;
    public string Domain
    {
        get => _domain;
        set => SetProperty(ref _domain, value);
    }

    /*private Uri _icon;
    public Uri Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }*/

    private WebContentHost _webcontentinstance;
    public WebContentHost WebContentInstance
    {
        get => _webcontentinstance;
        set => SetProperty(ref _webcontentinstance, value);
    }
}