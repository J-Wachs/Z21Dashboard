using System.Text.Json.Serialization;

namespace Z21Dashboard.NmraDccStandard.Models;

/// <summary>
/// This class defines the min and value values allowed based on the 
/// bits stated. If no value is given, it is assumed that the min and 
/// max values are values thac can be calculated from the bits.
/// 
/// if no bits are given, but min and max values are, then the value
/// is assumes to use the 8 bits available even thought the high bis
/// might not be used. An example is CV 1 which uses all 8 bits but
/// the maximum allowed decimal value is 127. This means that bit 7
/// is not used.
/// </summary>
public class NmraDccStandardCvValueBitsDto
{
    [JsonPropertyName("min")]
    public ushort? Min { get; set; }

    [JsonPropertyName("max")]
    public ushort? Max { get; set; }

    [JsonPropertyName("from_bit")]
    public ushort? FromBit { get; set; }

    [JsonPropertyName("to_bit")]
    public ushort? ToBit { get; set; }
}
