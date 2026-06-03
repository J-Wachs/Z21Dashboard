using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

// CV Definitions - polymorphic
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CvSingle), "cv_single")]
[JsonDerivedType(typeof(CvRange), "cv_range")]
public abstract class CvDefinitionItemDto
{
    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("default_value")]
    public byte? DefaultValue { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string>? Name { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }
}
