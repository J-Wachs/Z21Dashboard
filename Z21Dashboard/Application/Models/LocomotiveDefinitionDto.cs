using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

public class LocomotiveDefinitionDto
{
    [JsonPropertyName("meta")]
    public required LocomotiveMetaDto Meta { get; set; }

    [JsonPropertyName("decoder")]
    public required LocomotiveDecoderDto Decoder { get; set; }
}
