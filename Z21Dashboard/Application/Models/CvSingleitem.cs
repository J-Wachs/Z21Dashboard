using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

public class CvSingleItem : CvItem
{
    [JsonPropertyName("cv")]
    public ushort Cv { get; set; }
}
