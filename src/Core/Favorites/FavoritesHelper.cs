namespace Horizon.Core.Favorites;

public class FavoritesHelper
{
    private static readonly string FavoritesFilePath = $"{FolderHelper.LocalFolder.Path}\\Favorites.json";

    public static void AddFavorite(string title, string url)
    {
        FavoriteItem newFavoriteItem = new()
        {
            Title = title,
            Url = url
        };
        MainViewModel.MainVM.FavoritesList.Insert(0, newFavoriteItem);
        SaveListChangesToDisk();
    }

    public static void RemoveFavorite(FavoriteItem item)
    {
        MainViewModel.MainVM.FavoritesList.Remove(item);
        SaveListChangesToDisk();
    }

    public static ObservableCollection<FavoriteItem> GetFavoritesList()
    {
        bool DoFavsExist = File.Exists(FavoritesFilePath);
        if (!DoFavsExist)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Warning, "FavoritesHelper", "The favorites db does not exist!");
#endif
            ObservableCollection<FavoriteItem> placeholderItems = [];
            return placeholderItems;
        }
        else
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "FavoritesHelper", "Loading favorites");
#endif
            string filecontent = File.ReadAllText(FavoritesFilePath);
            ObservableCollection<FavoriteItem> Items = JsonSerializer.Deserialize(filecontent, FavoriteItemSerializerContext.Default.ObservableCollectionFavoriteItem);
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "FavoritesHelper", $"Loaded {Items.Count} favorite(s)");
#endif
            return Items;
        }
    }

    private static void SaveListChangesToDisk()
    {
        if (MainViewModel.MainVM.FavoritesList.Count < 1)
        {
            File.Delete(FavoritesFilePath);
        }
        else
        {
            string newJson = JsonSerializer.Serialize(MainViewModel.MainVM.FavoritesList, FavoriteItemSerializerContext.Default.ObservableCollectionFavoriteItem);
            File.WriteAllText(FavoritesFilePath, newJson);
        }
    }
}
