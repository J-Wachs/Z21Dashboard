using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Metadata for a locomotive decoder.
/// </summary>
public class DecoderMetaDto
{
    [JsonPropertyName("manufacturer")]
    public required string Manufacturer { get; set; }

    [JsonPropertyName("manufacturer_id")]
    public int ManufacturerId { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("protocol")]
    public required string Protocol { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
