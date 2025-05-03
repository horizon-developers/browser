namespace Horizon.Modules.Readability;

public class ReadabilityHelper
{
    public static string jscript { private set; get; }

    public static async Task<string> GetReadabilityScriptAsync()
    {
        if (jscript == null)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/JS/readability.js"));
            jscript = await FileIO.ReadTextAsync(file);
        }
        return jscript;
    }
}