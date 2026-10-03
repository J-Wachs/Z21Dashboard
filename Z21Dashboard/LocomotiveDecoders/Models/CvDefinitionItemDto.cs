using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// The class uses JSON Polymorphisme to handle two types
/// of CV nodes:
/// - Information about a single CV
/// - Information about a range of CVs
/// 
/// The content of this class is the common variables
/// for the two type of CV information.
/// 
/// The class is used for reading the CV part of
/// a decoder definition file.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CvSingleDto), "cv_single")]
[JsonDerivedType(typeof(CvRangeDto), "cv_range")]
public abstract class CvDefinitionItemDto
{
    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string>? Name { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    [JsonPropertyName("readonly")]
    public bool? ReadOnly { get; set; }
}
