namespace Horizon.Core;

public static class WebView2ProfileHelper
{
    public static async Task ClearAllProfileDataAsync(WebView2 wv2)
    {
        CoreWebView2Profile profile = wv2.CoreWebView2.Profile;
        await profile.ClearBrowsingDataAsync();
        await FileHelper.DeleteLocalFile("Favorites.json");
        SettingsViewModel.SettingsVM.FavoritesList.Clear();
    }
}