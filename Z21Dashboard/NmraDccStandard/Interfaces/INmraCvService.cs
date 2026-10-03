using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.NmraDccStandard.Models;

namespace Z21Dashboard.NmraDccStandard.Interfaces;

internal interface INmraCvService
{
    /// <summary>
    /// Get the name of a bit in a CV.
    /// </summary>
    /// <param name="cvNumber">The CV containing the bit</param>
    /// <param name="bit">The bit to get name of</param>
    /// <returns>A string containing only the name</returns>
    Task<Result<string>> GetCVBitNameAsync(
        ushort cvNumber,
        Bits bit
        );

    /// <summary>
    /// Get the description of a bit in a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <param name="bit"></param>
    /// <returns>A string containing the name of the bit and description of what value 0 and 1 means</returns>
    Task<Result<string>> GetCVBitDescriptionAsync(
        ushort cvNumber,
        Bits bit
        );

    /// <summary>
    /// Get the description of values 0 and 1 for a bit in a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <param name="bit"></param>
    /// <returns>Two strings of the meaning of value 0 and 1</returns>
    Task<Result<(string ZeroDescription, string OneDescription)>> GetCVBitValuesDescriptionAsync(
        ushort cvNumber,
        Bits bit
        );

    /// <summary>
    /// Gets the name and description (if any) of a CV.
    /// </summary>
    /// <param name="cvNumber"></param>
    /// <returns>Two strings with the name and description</returns>
    Task<Result<(string Name, string Description)>> GetCVNameAndDescriptionAsync(ushort cvNumber);
    
    /// <summary>
    /// Gets the data about a CV from the master NMRA definition file.
    /// </summary>
    /// <param name="cv"></param>
    /// <returns></returns>
    Task<Result<NmraDccStandardCvDefinitionDto>> GetRawCvDefinitionAsync(ushort cv);
}
