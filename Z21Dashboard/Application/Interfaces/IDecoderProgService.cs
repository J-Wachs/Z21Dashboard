using Z21Client.Models;

namespace Z21Dashboard.Application.Interfaces;

internal interface IDecoderProgService
{
    // --------------------------------------------------------------------
    // OLD (callback-based API)
    // --------------------------------------------------------------------
    // Task ReadCVAddressesPOM(
    //     ushort locoAddress,
    //     Queue<ushort> cvsToRead,
    //     Func<bool, List<string>, List<CVValue>, Task> finishedReadMethod);
    //
    // Task ReadCVAddressesProgTrack(
    //     Queue<ushort> cvsToRead,
    //     Func<bool, List<string>, List<CVValue>, Task> finishedReadMethod);
    //
    // Task WriteCVAddressesPOM(
    //     ushort locoAddress,
    //     Queue<CVValue> cvsToWrite,
    //     Func<bool, List<string>, Task> finishedWriteMethod);
    //
    // Task WriteCVAddressesProgTrack(
    //     Queue<CVValue> cvsToWrite,
    //     Func<bool, List<string>, Task> finishedWriteMethod);


    // --------------------------------------------------------------------
    // NEW (async/await + result tuples)
    // --------------------------------------------------------------------

    Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVAddressesPOMAsync(ushort locoAddress, Queue<ushort> cvsToRead);

    Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVAddressesProgTrackAsync(Queue<ushort> cvsToRead);

    Task<(bool IsSuccess, string? ErrorMessage)> WriteCVAddressesPOMAsync(ushort locoAddress, Queue<CVValue> cvsToWrite);

    Task<(bool IsSuccess, string? ErrorMessage)> WriteCVAddressesProgTrackAsync(Queue<CVValue> cvsToWrite);
}
