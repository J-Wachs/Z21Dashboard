using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

public class CvSingle : CvDefinitionItemDto
{
    [JsonPropertyName("cv")]
    public ushort Cv { get; set; }

    [JsonPropertyName("flag_bits")]
    public List<CvFlagBitDto>? FlagBits { get; set; }

    [JsonPropertyName("value_bits")]
    public CvValueBits? ValueBits { get; set; }
}

