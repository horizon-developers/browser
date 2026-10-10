namespace Horizon.Core;

public enum BackdropKind
{
    Mica,
    MicaAlt,
    Acrylic,
    None
}

/// <summary>
/// Typed access to the app's settings, backed by ApplicationData.LocalSettings. Raises PropertyChanged when a value changes,
/// so the settings page can bind to it and the rest of the app can react to it.
/// Values are stored in the format earlier versions wrote ("true"/"false", "Mica Alt", ...) so existing settings carry over.
/// </summary>
public sealed partial class AppSettings : ObservableObject
{
    public static AppSettings Current { get; } = new();

    public const string DefaultSearchUrl = "https://www.duckduckgo.com?q=";

    // The keys are part of the stored data, renaming one resets that setting for every user
    private static class Keys
    {
        public const string SearchUrl = "SearchUrl";
        public const string Backdrop = "OverrideBackdropType";
        public const string UseAdvancedContextMenu = "AdvancedCTX";
        public const string DisableWebPageDarkening = "DisableWebPageDarkening";
        public const string BlockAds = "BlockAds";
        public const string IsScreencaptureBlocked = "IsScreencaptureBlocked";
        public const string IsAppLockEnabled = "IsAppLockEnabled";
        public const string TranslatePrivacyAccepted = "AccTranslatePrivacy";
    }

    private readonly IDictionary<string, object> _values = ApplicationData.Current.LocalSettings.Values;

    private AppSettings()
    {
    }

    public string SearchUrl
    {
        get => Get(Keys.SearchUrl) ?? DefaultSearchUrl;
        set => Set(Keys.SearchUrl, value);
    }

    public BackdropKind Backdrop
    {
        get => Get(Keys.Backdrop) switch
        {
            "Mica Alt" => BackdropKind.MicaAlt,
            "Acrylic" => BackdropKind.Acrylic,
            "None" => BackdropKind.None,
            _ => BackdropKind.Mica
        };
        set => Set(Keys.Backdrop, value == BackdropKind.MicaAlt ? "Mica Alt" : value.ToString());
    }

    /// <summary>
    /// Show WebView2's own context menu instead of Horizon's
    /// </summary>
    public bool UseAdvancedContextMenu
    {
        get => GetBool(Keys.UseAdvancedContextMenu);
        set => SetBool(Keys.UseAdvancedContextMenu, value);
    }

    public bool DisableWebPageDarkening
    {
        get => GetBool(Keys.DisableWebPageDarkening);
        set => SetBool(Keys.DisableWebPageDarkening, value);
    }

    public bool BlockAds
    {
        get => GetBool(Keys.BlockAds);
        set => SetBool(Keys.BlockAds, value);
    }

    public bool IsScreencaptureBlocked
    {
        get => GetBool(Keys.IsScreencaptureBlocked);
        set => SetBool(Keys.IsScreencaptureBlocked, value);
    }

    /// <summary>
    /// Ask for Windows Hello on startup
    /// </summary>
    public bool IsAppLockEnabled
    {
        get => GetBool(Keys.IsAppLockEnabled);
        set => SetBool(Keys.IsAppLockEnabled, value);
    }

    /// <summary>
    /// The user agreed to send pages to Google Translate and does not want to be asked again
    /// </summary>
    public bool TranslatePrivacyAccepted
    {
        get => GetBool(Keys.TranslatePrivacyAccepted);
        set => SetBool(Keys.TranslatePrivacyAccepted, value);
    }

    private string? Get(string key) => _values.TryGetValue(key, out object? value) ? value as string : null;

    private bool GetBool(string key) => Get(key) == "true";

    private void SetBool(string key, bool value, [CallerMemberName] string propertyName = "") => Set(key, value ? "true" : "false", propertyName);

    private void Set(string key, string value, [CallerMemberName] string propertyName = "")
    {
        if (Get(key) == value)
        {
            return;
        }
        _values[key] = value;
        OnPropertyChanged(propertyName);
    }
}
