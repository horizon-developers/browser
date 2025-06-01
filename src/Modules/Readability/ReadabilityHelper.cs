namespace Horizon.Modules.Readability;

public class ReadabilityHelper
{
    public static string JScript { private set; get; }

    public static async Task<string> GetReadabilityScriptAsync()
    {
        if (JScript == null)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/JS/readability.js"));
            JScript = await FileIO.ReadTextAsync(file);
        }
        return JScript;
    }
}