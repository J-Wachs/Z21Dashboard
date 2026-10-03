using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Abstraction for the content of a page, which can be either conditional content or simple content.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CvConditionalContent), "conditional_content")]
[JsonDerivedType(typeof(CvSimpleContent), "cvs_content")]
public abstract class PageContentBase
{
}
