using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Information about a decoder, as listed in the decoder index (AllDecoders.json).
/// </summary>
public class DecoderIndexItemDto
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer_id")]
    public ushort ManufacturerId { get; set; } = 0;
}
