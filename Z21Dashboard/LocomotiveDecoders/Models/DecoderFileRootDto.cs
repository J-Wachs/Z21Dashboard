using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// DTO for the metadata of a decoder file.
/// </summary>
public class DecoderFileRootDto
{
    [JsonPropertyName("meta")]
    public required DecoderMetaDto Meta { get; set; }
}
