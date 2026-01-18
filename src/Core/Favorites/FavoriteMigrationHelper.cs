namespace Horizon.Core.Favorites;

public static class FavoriteMigrationHelper
{
    private static readonly string FavoritesFilePath = $"{FolderHelper.LocalFolder.Path}\\Favorites.json";

    public static string ConvertDatabase()
    {
        string ConvertedLogList = string.Empty;
        bool DoOldFavsExist = File.Exists(FavoritesFilePath);
        if (!DoOldFavsExist)
        {
            return string.Empty;
        }
        string OldFavoritesFileContent = File.ReadAllText(FavoritesFilePath);
        List<LegacyFavoriteItem> OldFavorites = JsonSerializer.Deserialize(OldFavoritesFileContent, LegacyFavoriteItemSerializerContext.Default.ListLegacyFavoriteItem);
        foreach (LegacyFavoriteItem item in OldFavorites)
        {
            FavoritesHelper.AddFavorite(item.Title, item.Url);
            ConvertedLogList = ConvertedLogList + "\n" + item.Title + ", " + item.Url;
        }
        return ConvertedLogList;
    }
}
