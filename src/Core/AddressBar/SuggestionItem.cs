namespace Horizon.Core.AddressBar;

public enum SuggestionCommand { GoToUrl, SearchWeb, LocalFile, OpenFavorite }

[GeneratedBindableCustomProperty]
public partial class SuggestionItem
{
    public Symbol DisplayIcon { get; set; }
    public string DisplayText { get; set; }
    /// <summary>
    /// Optional second line, e.g. the url of a favorite
    /// </summary>
    public string Description { get; set; }
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public SuggestionCommand Command { get; set; }
    public string Value { get; set; }
}
