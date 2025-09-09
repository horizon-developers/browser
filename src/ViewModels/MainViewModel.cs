namespace Horizon.ViewModels;

partial class MainViewModel : ObservableObject
{
    public static MainViewModel MainVM = new();

    [ObservableProperty]
    public partial ObservableCollection<FavoriteItem> FavoritesList { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Tab> Tabs { get; set; } = [];

    public MainViewModel()
    {
    }
}
