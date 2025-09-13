namespace Horizon.Core.AddressBar;

public enum SuggestionCommand { GoToUrl, SearchWeb }

[GeneratedBindableCustomProperty]
public partial class SuggestionItem
{
    public Symbol DisplayIcon { get; set; }
    public string DisplayText { get; set; }
    public SuggestionCommand Command { get; set; }
    public string Value { get; set; }
}