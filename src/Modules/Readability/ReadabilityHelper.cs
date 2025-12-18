namespace Horizon.Modules.Readability;

public class ReadabilityHelper
{
    public static string JScript { private set; get; }
    private static readonly string JScriptPath = $"{AppContext.BaseDirectory}\\Assets\\JS\\readability.js";

    public static async Task<string> GetReadabilityScriptAsync()
    {
        if (JScript == null)
        {
            JScript = await File.ReadAllTextAsync(JScriptPath);
        }
        return JScript;
    }
}