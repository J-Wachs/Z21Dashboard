using System.Text.Json.Serialization;

namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// Top level class for holding settings for a locomotive
/// internally.
/// </summary>
public class LocomotiveDefinitionDto
{
    [JsonPropertyName("meta")]
    public required LocomotiveMetaDto Meta { get; set; }

    [JsonPropertyName("decoder")]
    public required LocomotiveDecoderDto Decoder { get; set; }
}
