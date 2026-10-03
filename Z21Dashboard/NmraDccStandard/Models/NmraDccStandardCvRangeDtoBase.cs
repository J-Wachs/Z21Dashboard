using System.Text.Json.Serialization;

namespace Z21Dashboard.NmraDccStandard.Models;

/// <summary>
/// Definition of a range of CVs, e.g. the reserved range CV 96 to CV 104.
/// </summary>
public class NmraDccStandardCvRangeDto
{
    [JsonPropertyName("from")]
    public required int CvStart { get; set; }

    [JsonPropertyName("to")]
    public required int CvEnd { get; set; }
}
