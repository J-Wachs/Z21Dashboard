using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.LocomotiveDecoders.Interfaces;

/// <summary>
/// Interface for service responsible for providing decoder programming
/// helper services.
/// </summary>
internal interface IDecoderProgHelperService
{
    /// <summary>
    /// From a page definition, extract the CVs and bits that need to be read,
    /// and then perform the read operations against the decoder. This is intended
    /// to be a helper method for the UI, where the page definition contains the CVs
    /// and bits that are relevant for that page, and this method will handle the
    /// logic of extracting those CVs and bits, performing the read/write operations,
    /// and returning the results in a way that can be easily consumed by the UI.
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="pageId"></param>
    /// <param name="decoderSettings"></param>
    /// <returns></returns>
    Task<Result> ExtractCvsAndBitsAndReadFromDecoderAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        string pageId,
        DecoderSettings decoderSettings);

    /// <summary>
    /// From a page definition, extract the CVs and bits that need to be written,
    /// and then perform the write operations against the decoder. This is intended
    /// to be a helper method for the UI, where the page definition contains the CVs
    /// and bits that are relevant for that page, and this method will handle the
    /// logic of extracting those CVs and bits, performing the read/write operations,
    /// and returning the results in a way that can be easily consumed by the UI.
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="pageId"></param>
    /// <param name="decoderSettings"></param>
    /// <returns></returns>
    Task<Result> ExtractCvsAndBitsAndWriteToDecoderAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        string pageId,
        DecoderSettings decoderSettings);

    /// <summary>
    /// Read a number of CVs from the locomotive. The CVs to read are passed in as
    /// a queue, and the method will read them in order and return the results in a list.
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="cvsToRequest"></param>
    /// <param name="turnOnTrackPowerAfterReadPT">Whether to turn on the track power after reading from the programming track</param>
    /// <returns>The state of the reading</returns>
    Task<Result<List<CVValue>>> ReadCVValuesAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        Queue<ushort> cvsToRequest,
        bool turnOnTrackPowerAfterReadPT = true
        );

    /// <summary>
    /// Read the locomotive address from the decoder. The locomotive must be placed
    /// on the programming track (if one exists on the connected central station).
    /// </summary>
    /// <returns></returns>
    Task<Result<ushort>> ReadLocoAddressAsync();

    /// <summary>
    /// Reads the manufacturer information from the decoder. The id from the decoder
    /// is then used to look up information about the manufacturer.
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <returns></returns>
    Task<Result<DecoderManufacturerInfo>> ReadLocoDecoderManufacturerAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress
        );

    /// <summary>
    /// Writes a number of CVs to the locomotive. The CVs to write are passed in as
    /// a queue, and the method will write them in order.
    /// </summary>
    /// <param name="progTarget"></param>
    /// <param name="locoAddress"></param>
    /// <param name="cvsToWrite"></param>
    /// <returns></returns>
    Task<Result> WriteCVValuesAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        Queue<CVValue> cvsToWrite
        );

    /// <summary>
    /// Writes the (new) address to the locomotive, taking into account if
    /// the address is a short or long address.
    /// </summary>
    /// <param name="newLocoAddress"></param>
    /// <returns></returns>
    Task<Result> WriteLocoAddressAsync(ushort newLocoAddress);
}
