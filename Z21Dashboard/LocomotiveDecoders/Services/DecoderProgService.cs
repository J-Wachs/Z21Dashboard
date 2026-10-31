using Microsoft.Extensions.Logging;
using Z21Client;
using Z21Client.Helpers;
using Z21Client.Infrastructure;
using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Interfaces;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.LocomotiveDecoders.Services;

/// <summary>
/// Implementation of <see cref="IDecoderProgService"/> that uses the Z21Client to perform CV reading and writing
/// operations on both the main track (POM) and programming track, with enhanced error handling and logging.
/// </summary>
/// <param name="logger"></param>
/// <param name="z21Client"></param>
internal class DecoderProgService(
    ILogger<DecoderProgService> logger,
    IZ21Client z21Client
    ) : IDecoderProgService
{
    /// <inheritdoc />
    public async Task<Result<List<CVValue>>> ReadCVAddressesPOMAsync(
        ushort locoAddress,
        Queue<ushort> cvsToRead
        )
    {
        if (locoAddress is 0 or > Limits.HighestLongAddress)
        {
            // "The locomotive address must have a value between 1 and {0} in order to communicate on the main track"
            return Result<List<CVValue>>.Failure(string.Format(SharedResources.LocoAddressOutOfRange, Limits.HighestLongAddress));
        }

        // Check that RailCom is enabled before we start the POM reading, since without it we won't get any CV values back.
        var (sysStateSuccess, trackState) = await AsyncEventHelper.ExecuteAndWaitAsync<SystemState>(
                triggerAction: () => z21Client.GetSystemStateAsync(),
                subscribe: h => z21Client.OnSystemStateChanged += h,
                unsubscribe: h => z21Client.OnSystemStateChanged -= h,
                timeoutMs: 3000
            );

        if (sysStateSuccess is false || trackState is null)
        {
            // "Failed to retrieve system state. Cannot proceed with POM CV reading"
            return Result<List<CVValue>>.Failure(SharedResources.FailedToRetrieveSystemStatus);
        }

        // When we come here, we need to check the configuration for RailCom is enabled.
        // Before FW 1.42 the Capabilities byte was not present. If < 1.42, we just
        // go ahead.
        if (z21Client.HardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 &&
            trackState.Capabilities?.RailComEnabled is false)
        {
            // "RailCom must be enabled in Z21 and decoder in order to read CV values from POM"
            return Result<List<CVValue>>.Failure(SharedResources.RailComMustBeTurnedOn);
        }

        // Go on with the POM reading as before, since we know RailCom is enabled if we got here.
        var resultList = new List<CVValue>();

        foreach (var cvAddress in cvsToRead)
        {
            // We use the SIMPLE method here
            var (success, cvResult) = await AsyncEventHelper.ExecuteAndWaitAsync<CVValue>(
                triggerAction: async () => await z21Client.GetCVValueFromPOMAsync(locoAddress, cvAddress),
                subscribe: h => z21Client.OnCVValueReceived += h,
                unsubscribe: h => z21Client.OnCVValueReceived -= h,
                timeoutMs: 3000
            );

            if (success is false || cvResult is null)
            {
                // "Timeout during read of CV {0}. No response from decoder on main track"
                return Result<List<CVValue>>.Failure(string.Format(SharedResources.TimeoutDuringReadPOM, cvAddress));
            }

            if (cvResult.Cv != cvAddress)
            {
                // "Sync error: Requested CV {0}, received {1}"
                return Result<List<CVValue>>.Failure(string.Format(SharedResources.CVSyncError, cvAddress, cvResult.Cv));

            }

            resultList.Add(cvResult);
            await Task.Delay(100); // Ease the bus a bit
        }

        return Results.Success(resultList);
    }

    /// <inheritdoc />
    public async Task<Result<List<CVValue>>> ReadCVAddressesProgTrackAsync(Queue<ushort> cvsToRead, bool turnOnTrackPowerAfterRead = true)
    {
        List<CVValue> resultList = [];

        try
        {
            foreach (var cvAddress in cvsToRead)
            {
                // We use the ADVANCED method here (WithFailure)
                var (status, cvResult, nackShortCircuit) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<CVValue, bool>(
                    triggerAction: async () => await z21Client.GetCVValueFromProgTrackAsync(cvAddress),
                    subscribeSuccess: h => z21Client.OnCVValueReceived += h,
                    unsubscribeSuccess: h => z21Client.OnCVValueReceived -= h,
                    subscribeFailure: h => z21Client.OnCVNAckReceived += h,
                    unsubscribeFailure: h => z21Client.OnCVNAckReceived -= h,
                    timeoutMs: 10000 // Much slower on the prog track, than RailCom.
                );

                switch (status)
                {
                    case AsyncStatus.Success:
                        if (cvResult!.Cv != cvAddress)
                        {
                            // "Sync error: Requested CV {0}, received {1}"
                            return Result<List<CVValue>>.Failure(string.Format(SharedResources.CVSyncError, cvAddress, cvResult.Cv));
                        }

                        resultList.Add(cvResult);
                        break;

                    case AsyncStatus.FailureEvent:
                        // We received a NACK. 'nackShortCircuit' indicates whether it was a short circuit
                        //  ? "Short circuit detected"
                        //  : "Read failed (NACK) – no contact with decoder.";

                        string msg = nackShortCircuit
                            ? SharedResources.ShortCircuit
                            : SharedResources.ReadRejected;
                        logger.LogWarning(msg);
                        return Result<List<CVValue>>.Failure(msg);

                    case AsyncStatus.Timeout:
                        // "Timeout: No response for CV {0} on the programming track"
                        return Result<List<CVValue>>.Failure(string.Format(SharedResources.TimeoutDuringReadProgTrack, cvAddress));
                }

                await Task.Delay(100);
            }
            return Results.Success(resultList);
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(SharedResources.ErrorReadingFromDecoder, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result<List<CVValue>>.Failure(errorMessage);
        }
        finally
        {
            if (turnOnTrackPowerAfterRead)
            {
                await z21Client.SetTrackPowerOnAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteCVBitAddressesPOMAsync(
        ushort locoAddress,
        Queue<CVBitValue> cvBitsToWrite
        )
    {
        if (locoAddress is 0 or > Limits.HighestLongAddress)
        {
            // "The locomotive address must have a value between 1 and {0} in order to communicate on the main track"
            return Result.Failure(string.Format(SharedResources.LocoAddressOutOfRange, Limits.HighestLongAddress));
        }

        foreach (var item in cvBitsToWrite)
        {
            await z21Client.SetCVBitValueOnPOMAsync(locoAddress, new (item.Cv, item.Bit, item.Value));

            // Status must be Success here
            await Task.Delay(100);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> WriteCVAddressesPOMAsync(
        ushort locoAddress,
        Queue<CVValue> cvsToWrite
        )
    {
        if (locoAddress is 0 or > Limits.HighestLongAddress)
        {
            // "The locomotive address must have a value between 1 and {0} in order to communicate on the main track"
            return Result.Failure(string.Format(SharedResources.LocoAddressOutOfRange, Limits.HighestLongAddress));
        }

        foreach (var item in cvsToWrite)
        {
            // For ProgTrack write, Z21 sends back the written value as confirmation (Success)
            // Or a NACK on failure.
            await z21Client.SetCVValueOnPOMAsync(locoAddress, new(item.Cv, item.Value));

            // Status must be Success here
            await Task.Delay(100);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> WriteCVAddressesProgTrackAsync(Queue<CVValue> cvsToWrite)
    {
        try
        {
            foreach (var item in cvsToWrite)
            {
                // For ProgTrack write, Z21 sends back the written value as confirmation (Success)
                // Or a NACK on failure.
                var (status, cvResult, nackShortCircuit) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<CVValue, bool>(
                    triggerAction: async () => await z21Client.SetCVValueOnProgTrackAsync(new(item.Cv, item.Value)),
                    subscribeSuccess: h => z21Client.OnCVValueReceived += h,
                    unsubscribeSuccess: h => z21Client.OnCVValueReceived -= h,
                    subscribeFailure: h => z21Client.OnCVNAckReceived += h,
                    unsubscribeFailure: h => z21Client.OnCVNAckReceived -= h,
                    timeoutMs: 4000
                );

                if (status == AsyncStatus.FailureEvent)
                {
                    // "Short circuit!" : "Write rejected (NACK)"
                    string msg = nackShortCircuit ? SharedResources.ShortCircuit : SharedResources.WriteRejected;
                    return Result.Failure(msg);
                }

                if (status == AsyncStatus.Timeout)
                {
                    // "Timeout during write – no response"
                    return Result.Failure(SharedResources.TimeoutDuringWrite);
                }

                // Status must be Success here
                await Task.Delay(100);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(SharedResources.ErrorWritingToDecoder, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
        finally
        {
            await z21Client.SetTrackPowerOnAsync();
        }
    }
}
