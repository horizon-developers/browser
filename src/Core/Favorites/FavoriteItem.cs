namespace Horizon.Core.Favorites;

[JsonSerializable(typeof(ObservableCollection<FavoriteItem>))]
//[JsonSerializable(typeof(List<FavoriteItem>))]
public partial class FavoriteItemSerializerContext : JsonSerializerContext
{
}

public class FavoriteItem
{
    public string Title { get; set; }
    public string Url { get; set; }
}
