using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

public class CvSingleDto : CvDefinitionItemDto
{
    [JsonPropertyName("cv")]
    public ushort Cv { get; set; }

    [JsonPropertyName("default_value")]
    public byte? DefaultValue { get; set; }

    [JsonPropertyName("flag_bits")]
    public List<CvFlagBitDto>? FlagBits { get; set; }

    [JsonPropertyName("value_bits")]
    public CvValueBits? ValueBits { get; set; }
}
