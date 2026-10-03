using System.Text.Json.Serialization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CvSingleItem), "cv_single")]
[JsonDerivedType(typeof(CvWithBitItem), "cv_with_bit")]
[JsonDerivedType(typeof(CvRangeItem), "cv_range")]
public class CvItem
{
}
