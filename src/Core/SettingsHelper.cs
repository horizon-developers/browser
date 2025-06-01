namespace Horizon.Core;

public static class SettingsHelper
{
    private static readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

    public static string GetSetting(string Setting)
    {
        string SettingValue = localSettings.Values[Setting] as string;
        return SettingValue;
    }

    public static void SetSetting(string Setting, string SettingValue)
    {
        localSettings.Values[Setting] = SettingValue;
    }

    public static void LoadSettingsOnStartup()
    {
        CurrentSearchUrl = GetSetting("SearchUrl") ?? DefaultSearchUrl;
        CurrentBackdrop = GetSetting("OverrideBackdropType") ?? DefaultBackdrop;
    }

    // Settings
    public static readonly string DefaultSearchUrl = "https://www.duckduckgo.com?q=";
    public static readonly string DefaultBackdrop = "Mica";

    public static string CurrentSearchUrl { get; set; }
    public static string CurrentBackdrop { get; set; }
}
