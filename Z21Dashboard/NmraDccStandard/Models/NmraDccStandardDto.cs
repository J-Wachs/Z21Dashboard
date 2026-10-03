using System.Text.Json.Serialization;

namespace Z21Dashboard.NmraDccStandard.Models;

/// <summary>
/// This class is the root of the Nmra Dcc Standard file.
/// </summary>
public class NmraDccStandardDto
{
    [JsonPropertyName("meta")]
    public required NmraDccStandardMetaDto Meta { get; set; }

    [JsonPropertyName("cv_definitions")]
    public required List<NmraDccStandardCvDefinitionDto> CvDefinitions { get; set; }
}
