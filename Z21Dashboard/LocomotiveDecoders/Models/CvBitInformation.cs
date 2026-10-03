namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Holds descriptive information about a specific bit within a CV.
/// </summary>
public  class CvBitInformation
{
    /// <summary>
    /// The name of the flag/function (e.g., "Direction"). Key = Language Code.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The values that the bit(s) can have.
    /// </summary>
    public Dictionary<byte, string>? Values { get; set; }
}
