namespace Horizon.Modules.Favorites;

public class FavoritesHelper
{
    public static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

    public static async void LoadFavoritesOnStartup()
    {
        SettingsViewModel.SettingsVM.FavoritesList = await GetFavoritesListAsync();
    }

    public static async void CreateFirstFavorite(string title, string url)
    {
        // Generate json
        string json = "[{\"Title\":\"" + title + "\"," + "\"Url\":\"" + url + "\"}]";
        // create json file
        var file = await localFolder.CreateFileAsync("Favorites.json", CreationCollisionOption.ReplaceExisting);
        // write json to json file
        await FileIO.WriteTextAsync(file, json);
        // new historyitem
        FavoriteItem newFavoriteItem = new()
        {
            Title = title,
            Url = url
        };
        // add item to favorites list
        SettingsViewModel.SettingsVM.FavoritesList.Insert(0, newFavoriteItem);
    }

    public static async void AddFavorite(string title, string url)
    {
        var fileData = await localFolder.TryGetItemAsync("Favorites.json");
        if (fileData == null) CreateFirstFavorite(title, url);
        else
        {
            // new historyitem
            FavoriteItem newFavoriteItem = new()
            {
                Title = title,
                Url = url
            };
            // add item to favorites list
            SettingsViewModel.SettingsVM.FavoritesList.Insert(0, newFavoriteItem);
            SaveListChangesToDisk();
        }
    }

    public static void RemoveFavorite(FavoriteItem item)
    {
        SettingsViewModel.SettingsVM.FavoritesList.Remove(item);
        SaveListChangesToDisk();
    }

    public static async Task<ObservableCollection<FavoriteItem>> GetFavoritesListAsync()
    {
        var fileData = await localFolder.TryGetItemAsync("Favorites.json");
        if (fileData == null)
        {
            ObservableCollection<FavoriteItem> placeholderItems = new();
            return placeholderItems;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("LOADED FAVORITES");
            string filecontent = await FileIO.ReadTextAsync((IStorageFile)fileData);
            ObservableCollection<FavoriteItem> Items = JsonSerializer.Deserialize(filecontent, FavoriteItemSerializerContext.Default.ObservableCollectionFavoriteItem);
            foreach (FavoriteItem item in Items)
            {
                System.Diagnostics.Debug.WriteLine(item.Url);
            }
            return Items;
        }
    }

    private static async void SaveListChangesToDisk()
    {
        var fileData = await localFolder.GetFileAsync("Favorites.json");
        if (SettingsViewModel.SettingsVM.FavoritesList.Count < 1)
        {
            await fileData.DeleteAsync();
        }
        else
        {
            // Convert list to json
            string newJson = JsonSerializer.Serialize(SettingsViewModel.SettingsVM.FavoritesList, FavoriteItemSerializerContext.Default.ObservableCollectionFavoriteItem);
            // Write json to json file
            await FileIO.WriteTextAsync((IStorageFile)fileData, newJson);
        }
    }
}
