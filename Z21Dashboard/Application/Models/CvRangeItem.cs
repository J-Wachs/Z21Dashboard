using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

public class CvRangeItem : CvItem
{
    [JsonPropertyName("cv_start")]
    public ushort CvStart { get; set; }

    [JsonPropertyName("cv_end")]
    public ushort CvEnd { get; set; }
}
