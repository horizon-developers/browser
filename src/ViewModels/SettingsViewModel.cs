namespace Horizon.ViewModels;

partial class SettingsViewModel : ObservableObject
{
    public static SettingsViewModel SettingsVM = new();

    [ObservableProperty]
    public partial ObservableCollection<FavoriteItem> FavoritesList { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Tab> Tabs { get; set; } = [];

    public SettingsViewModel()
    {
    }
}
