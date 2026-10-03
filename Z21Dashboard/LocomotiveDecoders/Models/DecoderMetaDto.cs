using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Metadata for a locomotive decoder.
/// </summary>
public class DecoderMetaDto
{
    [JsonPropertyName("manufacturer")]
    public required string Manufacturer { get; set; }

    [JsonPropertyName("manufacturer_id")]
    public ushort ManufacturerId { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("sound")]
    public bool? Sound { get; set; }

    [JsonPropertyName("protocols")]
    public required string Protocols { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
