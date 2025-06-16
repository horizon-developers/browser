namespace Horizon.Core;

[JsonSerializable(typeof(ReleaseNotes))]
public partial class ReleaseNotesSerializerContext : JsonSerializerContext
{
}
public class ReleaseNotes
{
    [JsonPropertyName("tag_name")]
    public string VersionTag { get; set; }
    [JsonPropertyName("name")]
    public string VersionName { get; set; }
    [JsonPropertyName("body")]
    public string Body { get; set; }
}
