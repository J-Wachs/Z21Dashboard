namespace Z21Dashboard.Application.Models;

/// <summary>
/// Holds descriptive information about a specific bit within a CV.
/// </summary>
public  class CvBitInformation
{
    // The name of the flag/function (e.g., "Direction"). Key = Language Code.
    public  string Name { get; set; } = string.Empty;

    // Text displayed when value is 0 (False). E.g., "Normal". Key = Language Code.
    public  string OffLabel { get; set; } = string.Empty;

    // Text displayed when value is 1 (True). E.g., "Reversed". Key = Language Code.
    public  string OnLabel { get; set; } = string.Empty;
}
