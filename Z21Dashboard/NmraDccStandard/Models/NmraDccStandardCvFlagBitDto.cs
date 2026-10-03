using System.Text.Json.Serialization;

namespace Z21Dashboard.NmraDccStandard.Models;

public class NmraDccStandardCvFlagBitDto
{
    [JsonPropertyName("bit")]
    public required byte Bit { get; set; }

    [JsonPropertyName("bit_to")]
    public byte? BitTo { get; set; }

    [JsonPropertyName("name")]
    public required Dictionary<string, string> Name { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<byte, Dictionary<string, string>>? Values { get; set; }
}
