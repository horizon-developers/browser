namespace Horizon.Modules.Readability;

public class ReadabilityHelper
{
    public static string JScript { private set; get; }
    private const string ResourceName = "Horizon.readability_module.js";

    public static async Task<string> GetReadabilityScriptAsync()
    {
        if (JScript == null)
        {
            using var stream = typeof(ReadabilityHelper).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Missing embedded resource {ResourceName}");
            using var reader = new StreamReader(stream);
            JScript = await reader.ReadToEndAsync();
        }
        return JScript;
    }
}
