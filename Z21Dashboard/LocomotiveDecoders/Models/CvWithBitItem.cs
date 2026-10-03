using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

public class CvWithBitItem : CvItem
{
    [JsonPropertyName("cv")]
    public ushort Cv { get; set; }

    [JsonPropertyName("bit")]
    public byte Bit { get; set; }

    [JsonPropertyName("bit_to")]
    public byte? BitTo { get; set; }
}
