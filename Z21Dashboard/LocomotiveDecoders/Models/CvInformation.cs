namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Holds descriptive information about a CV (Name, Description, Bit details).
/// </summary>
public class CvInformation
{
    public ushort Cv { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public byte? DefaultValue { get; set; } = null;
    public bool? ReadOnly { get; set;  } = false;

    public Dictionary<(byte Bit, byte? BitTo), CvBitInformation> BitDetails { get; set; } = [];

    public CvValueBits? ValueBits { get; set; }
}
