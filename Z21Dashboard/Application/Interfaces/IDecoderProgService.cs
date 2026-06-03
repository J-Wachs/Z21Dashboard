using Z21Client.Models;
using Z21Dashboard.Helpers;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Service responsible for reading and writing decoder CV (Configuration Variable) values
/// using either Programming on Main (POM) or an isolated programming track.
/// Implementations perform the operations asynchronously and return Result wrappers
/// indicating success or failure and any read CV values.
/// </summary>
public interface IDecoderProgService
{
    /// <summary>
    /// Reads a sequence of CV addresses from a locomotive using Programming on Main (POM).
    /// </summary>
    /// <param name="locoAddress">The locomotive address on the main where POM reads are directed.</param>
    /// <param name="cvsToRead">A queue containing CV addresses to read. Implementations are expected
    /// to consume the queue (sequentially) and read each address.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="CVValue"/> entries for the
    /// requested CV addresses on success, or an error Result on failure.
    /// </returns>
    Task<Result<List<CVValue>>> ReadCVAddressesPOMAsync(ushort locoAddress, Queue<ushort> cvsToRead);

    /// <summary>
    /// Reads a sequence of CV addresses using the programming track (isolated programming environment).
    /// </summary>
    /// <param name="cvsToRead">A queue containing CV addresses to read. Implementations are expected
    /// to consume the queue (sequentially) and read each address from the programming track.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="CVValue"/> entries for the
    /// requested CV addresses on success, or an error Result on failure.
    /// </returns>
    Task<Result<List<CVValue>>> ReadCVAddressesProgTrackAsync(Queue<ushort> cvsToRead);

    /// <summary>
    /// Writes a sequence of CV values to a locomotive using Programming on Main (POM).
    /// The method processes the provided queue of <see cref="CVValue"/> items (cvsToWrite)
    /// and writes each CV to the target locomotive identified by <paramref name="locoAddress"/>.
    /// </summary>
    /// <param name="locoAddress">The locomotive address on the main where POM writes are directed.</param>
    /// <param name="cvsToWrite">A queue containing CV values to write. Implementations are expected
    /// to consume the queue (sequentially) and write each value.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the write operations.
    /// On failure, the Result should include diagnostic information where available.
    /// </returns>
    Task<Result> WriteCVAddressesPOMAsync(ushort locoAddress, Queue<CVValue> cvsToWrite);

    /// <summary>
    /// Writes a sequence of CV values using the programming track (isolated programming environment).
    /// The method processes the provided queue of <see cref="CVValue"/> items (cvsToWrite)
    /// and writes each CV to the decoder on the programming track.
    /// </summary>
    /// <param name="cvsToWrite">A queue containing CV values to write. Implementations are expected
    /// to consume the queue (sequentially) and write each value to the programming track.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the write operations.
    /// On failure, the Result should include diagnostic information where available.
    /// </returns>
    Task<Result> WriteCVAddressesProgTrackAsync(Queue<CVValue> cvsToWrite);
}
