using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

public class CvWithBitItem : CvItem
{
    [JsonPropertyName("cv")]
    public ushort Cv { get; set; }

    [JsonPropertyName("bit")]
    public byte Bit { get; set; }
}
