#nullable enable
namespace Horizon.Core.AddressBar;

/// <summary>
/// Turns favorites into address bar suggestions. Reads MainVM.FavoritesList directly (an ObservableCollection),
/// so every method must be called on the UI thread.
/// </summary>
public static class FavoriteSuggestions
{
    /// <summary>
    /// Favorites whose address starts with what was typed ("git" -> github.com). These are the ones Enter opens.
    /// </summary>
    public static List<SuggestionItem> FindByAddress(string query, int max = 3)
    {
        string q = Normalize(query);
        // "" and "https://" would otherwise match every favorite
        if (q.Length == 0)
            return [];

        return [.. MainViewModel.MainVM.FavoritesList
            .Where(f => Normalize(f.Url).StartsWith(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Url.Length) // the plain "julianhasreiter.eu" favorite beats a deep link into it
            .Take(max)
            .Select(ToSuggestion)];
    }

    /// <summary>
    /// Favorites that only match by title. Listed below the web search, so they have to be picked explicitly.
    /// </summary>
    public static List<SuggestionItem> FindByTitle(string query, int max = 2)
    {
        string q = Normalize(query);
        if (q.Length == 0)
            return [];

        return MainViewModel.MainVM.FavoritesList
            .Where(f => !Normalize(f.Url).StartsWith(q, StringComparison.OrdinalIgnoreCase)
                     && f.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(max)
            .Select(ToSuggestion)
            .ToList();
    }

    /// <summary>
    /// The favorite Enter should open for the given query, or null if no address matches.
    /// </summary>
    public static SuggestionItem? NavigationTarget(string query) => FindByAddress(query, 1).FirstOrDefault();

    // e.g. "https://www.github.com/x" -> "github.com/x"; applied to both sides so "www.git" still matches
    private static string Normalize(string s)
    {
        int schemeEnd = s.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd >= 0)
            s = s[(schemeEnd + 3)..];
        if (s.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            s = s[4..];
        return s;
    }

    private static SuggestionItem ToSuggestion(FavoriteItem f) => new()
    {
        DisplayIcon = Symbol.Favorite,
        DisplayText = f.Title,
        Description = f.Url,
        Command = SuggestionCommand.OpenFavorite,
        Value = f.Url
    };
}
