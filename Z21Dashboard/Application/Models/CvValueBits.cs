using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

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
}
