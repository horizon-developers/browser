using Microsoft.Data.Sqlite;

namespace Horizon.Core.Favorites;

public class FavoritesHelper
{
    public static readonly string FavoritesFilePath = $"{FolderHelper.LocalFolder.Path}\\Favorites.db";

    private static readonly string ConnectionString = $"Data Source={FavoritesFilePath}";

    // https://learn.microsoft.com/en-us/windows/apps/develop/data-access/sqlite-data-access
    static FavoritesHelper()
    {
        InitializeDatabase();
    }

    private static void InitializeDatabase()
    {
        try
        {
            // A new database file is automatically created if it doesn't exist
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Favorites (
                            Id TEXT PRIMARY KEY, 
                            Title TEXT, 
                            Url TEXT,
                            DateAdded INTEGER
                        )";

                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Error, "FavoritesHelper", $"Failed to initialize DB: {ex.Message}");
#endif
        }
    }

    public static void AddFavorite(string title, string url, bool noui = false)
    {
        try
        {
            var newId = Guid.NewGuid();

            FavoriteItem newFavoriteItem = new()
            {
                Title = title,
                Url = url,
                Id = newId
            };

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Favorites (Id, Title, Url) VALUES (@Id, @Title, @Url)";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", newFavoriteItem.Id.ToString());
                    command.Parameters.AddWithValue("@Title", newFavoriteItem.Title);
                    command.Parameters.AddWithValue("@Url", newFavoriteItem.Url);

                    command.ExecuteNonQuery();
                }
            }

#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "FavoritesHelper", "Added new favorite: " + newFavoriteItem.Title + ", " + newFavoriteItem.Id);
#endif
            WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                MainViewModel.MainVM.FavoritesList.Insert(0, newFavoriteItem);
            });
        }
        catch (Exception ex)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Error, "FavoritesHelper", $"Error adding favorite: {ex.Message}");
#endif
        }
    }

    public static void RemoveFavorite(FavoriteItem item)
    {
        if (item == null) return;

        try
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM Favorites WHERE Id = @Id";

                using (var command = new SqliteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", item.Id.ToString());
                    command.ExecuteNonQuery();
                }
            }

            WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                MainViewModel.MainVM.FavoritesList.Remove(item);
            });
        }
        catch (Exception ex)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Error, "FavoritesHelper", $"Error removing favorite: {ex.Message}");
#endif
        }
    }

    public static ObservableCollection<FavoriteItem> GetFavoritesList()
    {
        var items = new ObservableCollection<FavoriteItem>();

        if (!File.Exists(FavoritesFilePath))
        {
            return items;
        }

        try
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "FavoritesHelper", "Loading favorites from SQLite");
#endif
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Id, Title, Url FROM Favorites ORDER BY DateAdded DESC";

                using (var command = new SqliteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new FavoriteItem
                        {
                            Id = Guid.Parse(reader.GetString(0)), // GetString(index) is slightly faster than ["name"]
                            Title = reader.GetString(1),
                            Url = reader.GetString(2)
                        });
                    }
                }
            }
#if DEBUG
            Logger.LogEvent(Logger.Severity.Info, "FavoritesHelper", $"Loaded {items.Count} favorite(s)");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Error, "FavoritesHelper", $"Error loading favorites: {ex.Message}");
#endif
        }

        return items;
    }
}