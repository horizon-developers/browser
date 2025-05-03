namespace Horizon.Controls.Tabs;

public partial class Tab : ObservableObject
{
    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /*private Uri _icon;
    public Uri Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }*/

    private UIElement _content;
    public UIElement Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }
}