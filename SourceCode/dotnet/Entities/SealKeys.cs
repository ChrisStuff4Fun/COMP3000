using System.Text.Json.Serialization;

public class SealKeys
{
    [JsonPropertyName("public")]
    public string? Public { get; set; }

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    [JsonPropertyName("relin")]
    public string? Relin { get; set; }

    [JsonPropertyName("context")]
    public string? Context { get; set; }
}