using System.Text.Json.Serialization;

namespace ContentSplitter;

public class InputBlob
{
    [JsonPropertyName("content")] public string Content { get; set; }
    [JsonPropertyName("contentVector")] public float[] ContentVector { get; set; }
}