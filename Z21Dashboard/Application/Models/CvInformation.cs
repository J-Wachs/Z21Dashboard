namespace Z21Dashboard.Application.Models;

/// <summary>
/// Holds descriptive information about a CV (Name, Description, Bit details).
/// </summary>
public class CvInformation
{
    public ushort Cv { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;

    // Information about specific bits (if applicable). Key = Bit Index (0-7).
    public Dictionary<int, CvBitInformation> BitDetails { get; set; } = [];
    public CvDefinition.CvValueBits? ValueBits { get; set; }
}
