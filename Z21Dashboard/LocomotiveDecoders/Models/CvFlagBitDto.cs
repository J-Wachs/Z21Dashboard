using System.Text.Json.Serialization;
using Z21Dashboard.NmraDccStandard.Models;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Values for the bit in question
/// </summary>
public class CvFlagBitDto
{
    [JsonPropertyName("bit")]
    public byte Bit { get; set; }

    [JsonPropertyName("bit_to")]
    public byte? BitTo { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string>? Name { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<byte, Dictionary<string, string>>? Values { get; set; }

    public static implicit operator CvFlagBitDto(NmraDccStandardCvFlagBitDto flagBits)
    {
        return new()
        {
            Bit = flagBits.Bit,
            BitTo = flagBits.BitTo,
            Name = flagBits.Name,
            Values = flagBits.Values
        };
    }
}
