using Z21Client.Models;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

internal interface INmraCvService
{
    /// <summary>
    /// Get the name of a bit in a CV.
    /// </summary>
    /// <param name="cvNumber">The CV containing the bit</param>
    /// <param name="bit">The bit to get name of</param>
    /// <returns>A string containing only the name</returns>
    Task<string> GetCVBitName(ushort cvNumber, Bits bit);

    /// <summary>
    /// Get the description of a bit in a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <param name="bit"></param>
    /// <returns>A string containing the name of the bit and description of what value 0 and 1 means</returns>
    Task<string> GetCVBitDescription(ushort cvNumber, Bits bit);

    /// <summary>
    /// Get the description of values 0 and 1 for a bit in a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <param name="bit"></param>
    /// <returns>Two strings of the meaning of value 0 and 1</returns>
    Task<(string ZeroDescription, string OneDescription)> GetCVBitValuesDescription(ushort cvNumber, Bits bit);

    /// <summary>
    /// Gets the name of a CV, if available. If not, it returns an empty string.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <returns>A string with the description of the CV</returns>
    Task<string> GetCVName(ushort cvNumber);

    /// <summary>
    /// Gets the description of a CV, if available. If not, it returns an empty string.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <returns>A string with the description of the CV</returns>
    Task<string> GetCVDescription(ushort cvNumber);

    /// <summary>
    /// Gets the name and description (if any) of a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <returns>Two strings with the name and description</returns>
    Task<(string Name, string Description)> GetCVNameAndDescription(ushort cvNumber);
    
    Task<CvDefinition?> GetRawCvDefinition(ushort cv);
}
