using Microsoft.Extensions.Logging;
using Z21Client;
using Z21Client.Helpers;
using Z21Client.Infrastructure;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Infrastructure;

namespace Z21Dashboard.Services;

internal class DecoderProgService(ILogger<DecoderProgService> logger, IZ21Client z21Client) : IDecoderProgService
{
    /// <inhericdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVAddressesPOMAsync(ushort locoAddress, Queue<ushort> cvsToRead)
    {
        if (locoAddress is 0 or > Limits.HighestLongAddress)
        {
            return (false,
                $"The locomotive address must have a value between 1 and {Limits.HighestLongAddress} in order to read on the main track",
                []);
        }

        // Check that RailCom is enabled before we start the POM reading, since without it we won't get any CV values back.

        var (sysStateSuccess, trackState) = await AsyncEventHelper.ExecuteAndWaitAsync<SystemState>(
                triggerAction: () => z21Client.GetSystemStateAsync(),
                subscribe: h => z21Client.OnSystemStateChanged += h,
                unsubscribe: h => z21Client.OnSystemStateChanged -= h,
                timeoutMs: 3000
            ); ;

        if (sysStateSuccess is false || trackState is null)
        {
            return new(
                false,
                "Failed to retrieve system state. Cannot proceed with POM CV reading.",
                []
                );
        }
        // When we come here, we need to check the configuration for RailCom is enabled.
        // Before FW 1.42 the Capabilities byte was not present. If < 1.42, we just
        // go ahead.
        if (z21Client.HardwareInfo?.FwVersion.Version >= Z21FirmwareVersions.V1_42 &&
            trackState.Capabilities?.RailComEnabled is false)
        {
            return new(
                false,
                "RailCom skal være aktiveret for at kunne læse CV værdier fra POM",
                []);
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

            if (!success || cvResult == null)
            {
                logger.LogError($"Timeout while reading CV {cvAddress} via POM.");
                return (false, 
                    $"Timeout: No response for CV {cvAddress} on the main track.",
                    []);
            }

            if (cvResult.Cv != cvAddress)
                return (false,
                    $"Sync error: Requested CV {cvAddress}, received {cvResult.Cv}",
                    []);

            resultList.Add(cvResult);
            await Task.Delay(100); // Ease the bus a bit
        }

        return (true, null, resultList);
    }

    /// <inhericdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVAddressesProgTrackAsync(Queue<ushort> cvsToRead)
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
                    timeoutMs: 5000 // Often a bit slower on the prog track
                );

                switch (status)
                {
                    case AsyncStatus.Success:
                        if (cvResult!.Cv != cvAddress)
                            return (false,
                                $"Sync error: Requested CV {cvAddress}, received {cvResult.Cv}",
                                []);
                        resultList.Add(cvResult);
                        break;

                    case AsyncStatus.FailureEvent:
                        // We received a NACK. 'nackShortCircuit' indicates whether it was a short circuit
                        string msg = nackShortCircuit
                            ? "Short circuit detected on the programming track!"
                            : "Read failed (NACK) – no contact with decoder.";
                        logger.LogWarning(msg);
                        return (false, msg, []);

                    case AsyncStatus.Timeout:
                        return (false,
                            "Timeout on the programming track.",
                            []);
                }

                await Task.Delay(100);
            }
            return (true,
                null,
                resultList);
        }
        finally
        {
            await z21Client.SetTrackPowerOnAsync();
        }
    }

    /// <inhericdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage)> WriteCVAddressesPOMAsync(ushort locoAddress, Queue<CVValue> cvsToWrite)
    {
        if (locoAddress is 0 or > Limits.HighestLongAddress)
        {
            return (false, $"The locomotive address must have a value between 1 and {Limits.HighestLongAddress} in order to write on the main track");
        }

        foreach (var item in cvsToWrite)
        {
            // For ProgTrack write, Z21 sends back the written value as confirmation (Success)
            // Or a NACK on failure.
            await z21Client.SetCVValueOnPOMAsync(locoAddress, item.Cv, item.Value);

            // Status must be Success here
            await Task.Delay(100);
        }

        return (true, null);
    }

    /// <inhericdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage)> WriteCVAddressesProgTrackAsync(Queue<CVValue> cvsToWrite)
    {
        try
        {
            foreach (var item in cvsToWrite)
            {
                // For ProgTrack write, Z21 sends back the written value as confirmation (Success)
                // Or a NACK on failure.
                var (status, cvResult, nackShortCircuit) = await AsyncEventHelper.ExecuteAndWaitWithFailureAsync<CVValue, bool>(
                    triggerAction: async () => await z21Client.SetCVValueOnProgTrackAsync(item.Cv, item.Value),
                    subscribeSuccess: h => z21Client.OnCVValueReceived += h,
                    unsubscribeSuccess: h => z21Client.OnCVValueReceived -= h,
                    subscribeFailure: h => z21Client.OnCVNAckReceived += h,
                    unsubscribeFailure: h => z21Client.OnCVNAckReceived -= h,
                    timeoutMs: 4000
                );

                if (status == AsyncStatus.FailureEvent)
                {
                    string msg = nackShortCircuit ? "Short circuit!" : "Write rejected (NACK).";
                    return (false, msg);
                }

                if (status == AsyncStatus.Timeout)
                {
                    return (false,
                        "Timeout during write – no response.");
                }

                // Status must be Success here
                await Task.Delay(100);
            }
            return (true,
                null);
        }
        finally
        {
            await z21Client.SetTrackPowerOnAsync();
        }
    }
}
