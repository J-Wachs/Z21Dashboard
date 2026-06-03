using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Values for the bit in question
/// </summary>
public class CvFlagBitDto
{
    [JsonPropertyName("bit")]
    public byte Bit { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string>? Name { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<string, Dictionary<string, string>>? Values { get; set; }
}
