using System.Text.Json.Serialization;
using Z21Dashboard.NmraDccStandard.Models;

namespace Z21Dashboard.LocomotiveDecoders.Models;

public class CvValueBits
{
    [JsonPropertyName("min")]
    public ushort? Min { get; set; }

    [JsonPropertyName("max")]
    public ushort? Max { get; set; }

    [JsonPropertyName("from_bit")]
    public ushort? FromBit { get; set; }

    [JsonPropertyName("to_bit")]
    public ushort? ToBit { get; set; }

    public static implicit operator CvValueBits(NmraDccStandardCvValueBitsDto valueBits)
    {
        return new()
        {
            FromBit = valueBits.FromBit,
            Max = valueBits.Max,
            Min = valueBits.Min,
            ToBit = valueBits.ToBit
        };
    }
}
