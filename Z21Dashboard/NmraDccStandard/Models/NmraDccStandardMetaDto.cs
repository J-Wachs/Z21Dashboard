using System.Text.Json.Serialization;

namespace Z21Dashboard.NmraDccStandard.Models;

/// <summary>
/// Definition of metadata for the version of the NMRA standard.
/// </summary>
public class NmraDccStandardMetaDto
{
    [JsonPropertyName("standard")]
    public required string Standard { get; set; }

    [JsonPropertyName("document")]
    public required string Document { get; set; }

    [JsonPropertyName("scope")]
    public required string Scope { get; set; }

    [JsonPropertyName("languages")]
    public required List<string> Languages { get; set; }
}
