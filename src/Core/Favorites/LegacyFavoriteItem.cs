namespace Horizon.Core.Favorites;

[JsonSerializable(typeof(List<LegacyFavoriteItem>))]
public partial class LegacyFavoriteItemSerializerContext : JsonSerializerContext
{
}

public class LegacyFavoriteItem
{
    public string Title { get; set; }
    public string Url { get; set; }
}
