using System.Collections.ObjectModel;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Rule for displaying content based on the value of a CV or a bit in a CV. 
/// If the CV and bit match the specified value, the content is displayed.
/// </summary>
public class CvConditionalRule
{
    /// <summary>
    /// CV that must match
    /// </summary>
    public ushort Cv { get; set; }

    /// <summary>
    /// Bit that must match.
    /// </summary>
    public byte? Bit { get; set; }

    /// <summary>
    /// Value that must match.
    /// </summary>
    public ushort Value { get; set; }

    /// <summary>
    /// The content (VCs) to display when the CV and bit match the specified value.
    /// </summary>
    public required ObservableCollection<CvItem> Content { get; set; }
}
