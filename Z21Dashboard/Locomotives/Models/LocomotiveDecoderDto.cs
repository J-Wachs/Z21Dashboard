using Z21Dashboard.LocomotiveDecoders.Models;

namespace Z21Dashboard.Locomotives.Models;

/// <summary>
/// Class to hold the information about the locomotive decoder and the values set in the
/// decoder for this specific locomotive.
/// </summary>
public class LocomotiveDecoderDto
{
    /// <summary>
    /// The address is used to make it simple to get the address
    /// as to fin the actual address it is nessecary to look at CV 29 bit 5,
    /// CV 1 and/or Cv 17 and 18.
    /// </summary>
    public ushort Address { get; set; } = 0;

    /// <summary>
    /// Information about what decoder is selected for the locomotive, if any.
    /// </summary>
    public string? DecoderFile { get; set; }

    /// <summary>
    /// The location of the decoder file (in application or in a local folder).
    /// </summary>
    public DecoderFileLocation? DecoderFileLocation { get; set; }

    /// <summary>
    /// The specific values for this locomotive, which may be different
    /// from the default values defined in the decoder file, if any.
    /// </summary>
    public Dictionary<ushort, byte> CvValues { get; set; } = [];
}
