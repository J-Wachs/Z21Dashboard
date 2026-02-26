using Microsoft.Extensions.Logging;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Infrastructure;

namespace Z21Dashboard.Services;

internal class DecoderProgHelperService(ILogger<DecoderProgHelperService> logger, IDecoderProgService decoderProgService) : IDecoderProgHelperService
{
    #region Exposed methods

    /// <inheritdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage, List<CVValue> CvValues)> ReadCVValuesAsync(
    ProgrammingTarget progTarget,
    ushort? locoAddress,
    Queue<ushort> cvsToRequest)
    {
        if (progTarget is ProgrammingTarget.POM)
        {
            if (locoAddress is null or 0 or > Limits.HighestLongAddress)
            {
                return (false,
                    $"The locomotive address must have a value betweeen 1 and {Limits.HighestLongAddress} in order to read on the main track",
                    []);
            }
            return await decoderProgService.ReadCVAddressesPOMAsync((ushort)locoAddress, cvsToRequest);
        }
        else if (progTarget is ProgrammingTarget.ProgTrack)
        {
            return await decoderProgService.ReadCVAddressesProgTrackAsync(cvsToRequest);
        }
        else
        {
            return (false,
                "Unknown programming target",
                []);
        }
    }


    /// <inheritdoc />
    public async Task<ReadLocoAddressResponse> ReadLocoAddressAsync()
    {
        logger.LogError("GetLocoAddress");

        string errorMessage = string.Empty;
        ushort locoAddress = 0;

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ConfigurationData1);

        //var result = await decoderProgService.ReadCVAddressesProgTrackAsync(cvsToRequest);
        var result = await ReadCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToRequest);

        if (result.IsSuccess)
        {
            if (result.CvValues.Count > 1)
            {
                errorMessage = "More than one CV value was returned from Z21";
            }
            else if (result.CvValues.Count == 1)
            {
                cvsToRequest.Clear();

                var cv29Value = result.CvValues[0].Value;
                // Short or long address?
                if ((cv29Value & Cvs.CV29_Bit5_LongAddress) > 0)
                {
                    // Long address
                    cvsToRequest.Enqueue(Cvs.LongAddressHighByte);
                    cvsToRequest.Enqueue(Cvs.LongAddressLowByte);
                }
                else
                {
                    // Short address
                    cvsToRequest.Enqueue(Cvs.ShortAddress);
                }

                //result = await decoderProgService.ReadCVAddressesProgTrackAsync(cvsToRequest);
                result = await ReadCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToRequest);
                if (result.IsSuccess)
                {
                    // Check if short or long address:
                    if (result.CvValues[0].Cv is Cvs.ShortAddress)
                    {
                        locoAddress = result.CvValues[0].Value;
                    }
                    else if (result.CvValues[0].Cv is Cvs.LongAddressHighByte or Cvs.LongAddressLowByte)
                    {
                        // Calculate the long address:
                        var cv17 = result.CvValues.Find(x => x.Cv is Cvs.LongAddressHighByte);
                        var cv18 = result.CvValues.Find(x => x.Cv is Cvs.LongAddressLowByte);

                        // Part one of the workaround
                        if (cv17 is null || cv18 is null)
                        {
                            errorMessage = "Returned CV17 or CV18 does not contain a value";
                        }
                        else
                        {
                            locoAddress = (ushort)((cv17.Value - 192) * 256 + cv18.Value);
                        }
                    }
                }
            }
            else
            {
                errorMessage = "No CV values returned but no errors was reported";
            }
        }

        if (result.IsSuccess)
        {
            return new ReadLocoAddressResponse(
                errorMessage == string.Empty,
                errorMessage,
                locoAddress);
        }
        else
        {
            return new ReadLocoAddressResponse(
                false,
                result.ErrorMessage,
                0);
        }
    }

    /// <inheritdoc />
    public async Task<ReadLocoMotorCvsResponse> ReadLocoMotorCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress)
    {
        logger.LogError("GetLocoMotorCVs");

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ConfigurationData1);
        cvsToRequest.Enqueue(Cvs.AccelerationRate);
        cvsToRequest.Enqueue(Cvs.DeaccelerationRate);
        cvsToRequest.Enqueue(Cvs.PWMPeriod);
        cvsToRequest.Enqueue(Cvs.KickStart);

        //(bool IsSuccess, List<string> ErrorMessages, List<CVValue> CvValues) result;

        var result = await ReadCVValuesAsync(progTarget, locoAddress, cvsToRequest);
        if (result.IsSuccess)
        {
            if (result.CvValues.Count > 0)
            {
                CVMotorParameters cVMotorParameters = new()
                {
                    CV29_0_ForwardDirection = (result.CvValues.Find(x => x.Cv == Cvs.ConfigurationData1)?.Value & 0b00000001) > 0,
                    CV29_1_SpeedSteps = (result.CvValues.Find(x => x.Cv == Cvs.ConfigurationData1)?.Value & 0b00000010) > 0,
                    CV29_2_DCAnalog = (result.CvValues.Find(x => x.Cv == Cvs.ConfigurationData1)?.Value & 0b00000100) > 0,
                    CV3_AccRate = result.CvValues.Find(x => x.Cv == Cvs.AccelerationRate)?.Value ?? 0,
                    CV4_DecRate = result.CvValues.Find(x => x.Cv == Cvs.DeaccelerationRate)?.Value ?? 0,
                    CV9_PWMPeriod = result.CvValues.Find(x => x.Cv == Cvs.PWMPeriod)?.Value ?? 0,
                    CV65_KickStart = result.CvValues.Find(x => x.Cv == Cvs.KickStart)?.Value ?? 0
                };
                return new ReadLocoMotorCvsResponse(
                    true,
                    null,
                    cVMotorParameters
                    );
            }
            else
            {
                return new ReadLocoMotorCvsResponse(
                    false,
                    "No CV values returned but no errors was reported",
                    null
                    );
            }
        }

        return new ReadLocoMotorCvsResponse(
            false,
            result.ErrorMessage,
            null
            );
    }

    /// <inheritdoc />
    public async Task<ReadManufacturerResponse> ReadLocoDecoderManufacturerAsync(ProgrammingTarget progTarget, ushort? locoAddress)
    {
        logger.LogError("GetManufacturer");

        byte manufacturerCode = 0;
        string manufacturerName = string.Empty;
        byte manufacturerVersionNbr = 0;
        string statusMessage = string.Empty;

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ManufacturerVersionNbr);
        cvsToRequest.Enqueue(Cvs.Manufacturer);

        //(bool IsSuccess, List<string> ErrorMessages, List<CVValue> CvValues) result;
        var result = await ReadCVValuesAsync(progTarget, locoAddress, cvsToRequest);

        if (result.IsSuccess)
        {
            if (result.CvValues.Count > 0)
            {
                manufacturerCode = result.CvValues.Find(x => x.Cv == Cvs.Manufacturer)?.Value ?? 0;

                // Skal laves om til en DI klasse
                var getManufacturer = await ManufacturerService.GetManufacturerAsync(manufacturerCode);
                manufacturerName = getManufacturer is null ? "(Ukendt)" : getManufacturer.Name;
                manufacturerVersionNbr = result.CvValues.Find(x => x.Cv == Cvs.ManufacturerVersionNbr)?.Value ?? 0;
            }
        }

        return new ReadManufacturerResponse(
            result.IsSuccess,
            result.ErrorMessage,
                    manufacturerCode,
                    manufacturerName,
                    manufacturerVersionNbr
            );
    }

    /// <inheritdoc />
    public async Task<ReadLocoRailComCVsResponse> ReadLocoRailComCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress)
    {
        logger.LogInformation("Reading RailCom CVs");

        // Prepare CVs to read:
        // CV28: bits 0,1,7 (channel1, channel2, autologin)
        // CV29: bit 3 (RailCom enable)
        // CV127: RailCom delay
        // CV128: RailCom sensitivity
        Queue<ushort> cvsToRead = [];
        cvsToRead.Enqueue(Cvs.RailComConfiguration);  // CV28
        cvsToRead.Enqueue(Cvs.ConfigurationData1);    // CV29

        var result = await ReadCVValuesAsync(progTarget, locoAddress, cvsToRead);
        if (!result.IsSuccess)
            return new ReadLocoRailComCVsResponse(false, result.ErrorMessage, null);

        var cvValues = result.CvValues;

        var parameters = new CVRailComParameters
        {
            // --- CV28 bits ---
            CV28_Bit0_Channel1 = (cvValues.Find(x => x.Cv == Cvs.RailComConfiguration)?.Value & Cvs.CV28_Bit0_Channel1Broadcast) > 0,
            CV28_Bit1_Channel2 = (cvValues.Find(x => x.Cv == Cvs.RailComConfiguration)?.Value & Cvs.CV28_Bit1_DataTransmissionAllowed) > 0,
            CV28_Bit7_AutoLogin = (cvValues.Find(x => x.Cv == Cvs.RailComConfiguration)?.Value & Cvs.CV28_Bit7_AutoLogin) > 0,

            // --- CV29 bit 3 (RailCom enable) ---
            CV29_3_RailComEnabled = (cvValues.Find(x => x.Cv == Cvs.ConfigurationData1)?.Value & Cvs.CV29_Bit3_RailCom) > 0,
        };

        return new ReadLocoRailComCVsResponse(true, null, parameters);
    }

    /// <inheritdoc />
    public async Task<ReadLocoSpeedCurveResponse> ReadLocoSpeedCurveCVsAsync(ProgrammingTarget progTarget, ushort? locoAddress)
    {
        logger.LogError("GetLocoSpeedCurveCVs");

        // 1. Læs CV 29 først for at bestemme kurve-typen
        Queue<ushort> cv29Request = [];
        cv29Request.Enqueue(Cvs.ConfigurationData1);

        var cv29Result = await ReadCVValuesAsync(progTarget, locoAddress, cv29Request);

        if (!cv29Result.IsSuccess || cv29Result.CvValues.Count == 0)
        {
            return new ReadLocoSpeedCurveResponse(false, cv29Result.ErrorMessage, null);
        }

        byte cv29Value = cv29Result.CvValues[0].Value;
        bool isSpeedTableActive = (cv29Value & Cvs.CV29_Bit4_SpeedTable) > 0;

        // 2. Forbered næste læsning baseret på bit 4
        Queue<ushort> curveCvsRequest = [];

        if (isSpeedTableActive)
        {
            // Avanceret: Læs tabel (67-94) + Trim
            for (ushort i = Cvs.SpeedTableStart; i <= Cvs.SpeedTableEnd; i++)
            {
                curveCvsRequest.Enqueue(i);
            }
            curveCvsRequest.Enqueue(Cvs.ForwardTrim);
            curveCvsRequest.Enqueue(Cvs.ReverseTrim);
        }
        else
        {
            // Simpel: Læs VStart, VMid, VHigh
            curveCvsRequest.Enqueue(Cvs.VStart);
            curveCvsRequest.Enqueue(Cvs.VMid);
            curveCvsRequest.Enqueue(Cvs.VHigh);
        }

        var curveResult = await ReadCVValuesAsync(progTarget, locoAddress, curveCvsRequest);

        if (!curveResult.IsSuccess)
        {
            return new ReadLocoSpeedCurveResponse(false, curveResult.ErrorMessage, null);
        }

        // 3. Map resultatet til modellen
        var parameters = new CVSpeedCurveParameters
        {
            UseSpeedTable = isSpeedTableActive
        };

        if (isSpeedTableActive)
        {
            // Fyld arrayet
            for (int i = 0; i < 28; i++)
            {
                byte cvNum = (byte)(Cvs.SpeedTableStart + i);
                parameters.SpeedTable[i] = curveResult.CvValues.Find(x => x.Cv == cvNum)?.Value ?? 0;
            }
            parameters.CV66_ForwardTrim = curveResult.CvValues.Find(x => x.Cv == Cvs.ForwardTrim)?.Value ?? 128;
            parameters.CV95_ReverseTrim = curveResult.CvValues.Find(x => x.Cv == Cvs.ReverseTrim)?.Value ?? 128;
        }
        else
        {
            parameters.CV2_VStart = curveResult.CvValues.Find(x => x.Cv == Cvs.VStart)?.Value ?? 3;
            parameters.CV6_VMid = curveResult.CvValues.Find(x => x.Cv == Cvs.VMid)?.Value ?? 0; // 0 betyder ofte lineær
            parameters.CV5_VHigh = curveResult.CvValues.Find(x => x.Cv == Cvs.VHigh)?.Value ?? 255;
        }

        return new ReadLocoSpeedCurveResponse(true, null, parameters);
    }

    /// <inheritdoc />
    public async Task<(bool IsSuccess, string? ErrorMessage)> WriteCVValuesAsync(ProgrammingTarget progTarget,
        ushort? locoAddress,
        Queue<CVValue> cvsToWrite)
    {
        if (progTarget is ProgrammingTarget.POM)
        {
            if (locoAddress is null)
            {
                return (false, $"The locomotive address must not be 'null'");
            }
            return await decoderProgService.WriteCVAddressesPOMAsync((ushort)locoAddress, cvsToWrite);
        }
        else if (progTarget is ProgrammingTarget.ProgTrack)
        {
            return await decoderProgService.WriteCVAddressesProgTrackAsync(cvsToWrite);
        }
        else
        {
            return (false, "Unknown programming target");
        }
    }

    /// <inheritdoc />
    public async Task<WriteLocoAddressResponse> WriteLocoAddressAsync(ushort newLocoAddress)
    {
        logger.LogError("WriteLocoAddress");

        if (newLocoAddress is 0 or > Limits.HighestLongAddress)
        {
            return new WriteLocoAddressResponse(
                false,
                $"New locomotive address must be between 1 and {Limits.HighestLongAddress}");
        }
        else
        {
            Queue<ushort> cvsToRequest = [];
            cvsToRequest.Enqueue(Cvs.ConfigurationData1);
            var result = await ReadCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToRequest);
            if (result.IsSuccess)
            {
                if (result.CvValues.Count > 0)
                {
                    Queue<CVValue> cvsToWrite = [];

                    var cv29Value = result.CvValues[0].Value;

                    if (newLocoAddress >= 128)
                    {
                        // Long address
                        cv29Value |= 0b00100000;
                        cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));

                        var cv17 = 192 + (newLocoAddress / 256);
                        var cv18 = newLocoAddress % 256;

                        cvsToWrite.Enqueue(new(Cvs.LongAddressHighByte, (byte)cv17));
                        cvsToWrite.Enqueue(new(Cvs.LongAddressLowByte, (byte)cv18));
                    }
                    else
                    {
                        // Short address
                        cv29Value = (byte)(cv29Value & ~0b00100000);
                        cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));
                        cvsToWrite.Enqueue(new(Cvs.ShortAddress, (byte)newLocoAddress));
                    }

                    var resultWriteCV = await WriteCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToWrite);
                    if (!resultWriteCV.IsSuccess)
                    {
                        return new WriteLocoAddressResponse(
                            false,
                            resultWriteCV.ErrorMessage);
                    }

                }
            }
            if (result.IsSuccess)
            {
                return new WriteLocoAddressResponse(
                    true,
                    null);
            }
            else
            {
                return new WriteLocoAddressResponse(
                    false,
                    result.ErrorMessage);
            }
        }
    }

    /// <inheritdoc />
    public async Task<WriteLocoMotorCVsResponse> WriteLocoMotorCVsAsync(WriteLocoMotorCVsParams writeLocoMotorCVsParams)
    {
        logger.LogError("WriteLocoMotorCVs");

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ConfigurationData1);
        var result = await ReadCVValuesAsync(writeLocoMotorCVsParams.ProgTarget, writeLocoMotorCVsParams.LocoAddress, cvsToRequest);
        if (result.IsSuccess)
        {
            Queue<CVValue> cvsToWrite = [];

            var cv29Value = result.CvValues[0].Value;

            if (writeLocoMotorCVsParams.CVMotorParameters.CV29_0_ForwardDirection is true)
            {
                cv29Value |= Cvs.CV29_Bit0_ForwardDirection;
            }
            else if (writeLocoMotorCVsParams.CVMotorParameters.CV29_0_ForwardDirection is false)
            {
                cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit0_ForwardDirection);
            }

            if (writeLocoMotorCVsParams.CVMotorParameters.CV29_1_SpeedSteps is true)
            {
                cv29Value |= Cvs.CV29_Bit1_SpeedSteps;
            }
            else if (writeLocoMotorCVsParams.CVMotorParameters.CV29_1_SpeedSteps is false)
            {
                cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit1_SpeedSteps);
            }

            if (writeLocoMotorCVsParams.CVMotorParameters.CV29_2_DCAnalog is true)
            {
                cv29Value |= Cvs.CV29_Bit2_DCAnalog;
            }
            else if (writeLocoMotorCVsParams.CVMotorParameters.CV29_2_DCAnalog is false)
            {
                cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit2_DCAnalog);
            }

            cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));
            if (writeLocoMotorCVsParams.CVMotorParameters.CV3_AccRate is not null)
            {
                cvsToWrite.Enqueue(new(Cvs.AccelerationRate, (byte)writeLocoMotorCVsParams.CVMotorParameters.CV3_AccRate));
            }
            cvsToWrite.Enqueue(new(Cvs.DeaccelerationRate, (byte)writeLocoMotorCVsParams.CVMotorParameters.CV4_DecRate));
            cvsToWrite.Enqueue(new(Cvs.PWMPeriod, (byte)writeLocoMotorCVsParams.CVMotorParameters.CV9_PWMPeriod));
            cvsToWrite.Enqueue(new(Cvs.KickStart, (byte)writeLocoMotorCVsParams.CVMotorParameters.CV65_KickStart));

            var resultWriteCVValues = await WriteCVValuesAsync(
                writeLocoMotorCVsParams.ProgTarget,
                writeLocoMotorCVsParams.LocoAddress,
                cvsToWrite);

            if (resultWriteCVValues.IsSuccess is false)
            {
                return new WriteLocoMotorCVsResponse(
                false,
                resultWriteCVValues.ErrorMessage);

            }
        }

        if (result.IsSuccess)
        {
            return new WriteLocoMotorCVsResponse(
                true,
                null);
        }
        else
        {
            return new WriteLocoMotorCVsResponse(
                false,
                result.ErrorMessage);
        }
    }

    /// <inheritdoc />
    public async Task<WriteLocoRailComCVsResponse> WriteLocoRailComCVsAsync(WriteLocoRailComCVsParams parms)
    {
        // 1. Read CV29 first
        Queue<ushort> cv29Request = new();
        cv29Request.Enqueue(Cvs.ConfigurationData1);

        var readResult = await ReadCVValuesAsync(parms.ProgTarget, parms.LocoAddress, cv29Request);
        if (!readResult.IsSuccess) return new WriteLocoRailComCVsResponse(false, readResult.ErrorMessage);

        byte cv29Value = readResult.CvValues[0].Value;

        // Set/Clear bit 3 for RailCom
        if (parms.CVRailComParameters.CV29_3_RailComEnabled)
            cv29Value |= Cvs.CV29_Bit3_RailCom;
        else
            cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit3_RailCom);

        // 2. Read CV28 to update channel bits
        Queue<ushort> cv28Request = new();
        cv28Request.Enqueue(Cvs.RailComConfiguration);

        var readCV28 = await ReadCVValuesAsync(parms.ProgTarget, parms.LocoAddress, cv28Request);
        byte cv28Value = readCV28.IsSuccess && readCV28.CvValues.Count > 0
            ? readCV28.CvValues[0].Value
            : (byte)0;

        // Update bits from _cvRailComParameters
        cv28Value = (byte)((parms.CVRailComParameters.CV28_Bit0_Channel1
                            ? cv28Value | Cvs.CV28_Bit0_Channel1Broadcast
                            : cv28Value & ~Cvs.CV28_Bit0_Channel1Broadcast)
                           | (parms.CVRailComParameters.CV28_Bit1_Channel2
                            ? Cvs.CV28_Bit1_DataTransmissionAllowed
                            : 0)
                           | (parms.CVRailComParameters.CV28_Bit7_AutoLogin
                            ? Cvs.CV28_Bit7_AutoLogin
                            : 0));

        // 3. Write both CV28 and CV29
        Queue<CVValue> cvsToWrite = new();
        cvsToWrite.Enqueue(new CVValue(Cvs.ConfigurationData1, cv29Value));
        cvsToWrite.Enqueue(new CVValue(Cvs.RailComConfiguration, cv28Value));

        var writeResult = await WriteCVValuesAsync(parms.ProgTarget, parms.LocoAddress, cvsToWrite);
        return writeResult.IsSuccess
            ? new WriteLocoRailComCVsResponse(true, null)
            : new WriteLocoRailComCVsResponse(false, writeResult.ErrorMessage);
    }

    /// <inheritdoc />
    public async Task<WriteLocoSpeedCurveResponse> WriteLocoSpeedCurveCVsAsync(WriteLocoSpeedCurveParams parms)
    {
        logger.LogError("WriteLocoSpeedCurveCVs");

        // 1. Must read CV 29 first in order not to overwrite others bites (direction, steps etc.)
        Queue<ushort> cv29Request = [];
        cv29Request.Enqueue(Cvs.ConfigurationData1);

        var readResult = await ReadCVValuesAsync(parms.ProgTarget, parms.LocoAddress, cv29Request);
        if (!readResult.IsSuccess)
        {
            return new WriteLocoSpeedCurveResponse(false, readResult.ErrorMessage);
        }

        byte cv29Value = readResult.CvValues[0].Value;

        // 2. Update Bit 4
        if (parms.CurveParameters.UseSpeedTable)
        {
            cv29Value |= Cvs.CV29_Bit4_SpeedTable; // Set bit
        }
        else
        {
            cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit4_SpeedTable); // Clear bit
        }

        Queue<CVValue> cvsToWrite = [];
        cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));

        // 3. Add the needed CVs to the queue
        if (parms.CurveParameters.UseSpeedTable)
        {
            for (int i = 0; i < 28; i++)
            {
                cvsToWrite.Enqueue(new((ushort)(Cvs.SpeedTableStart + i), parms.CurveParameters.SpeedTable[i]));
            }
            cvsToWrite.Enqueue(new(Cvs.ForwardTrim, parms.CurveParameters.CV66_ForwardTrim));
            cvsToWrite.Enqueue(new(Cvs.ReverseTrim, parms.CurveParameters.CV95_ReverseTrim));
        }
        else
        {
            cvsToWrite.Enqueue(new(Cvs.VStart, parms.CurveParameters.CV2_VStart));
            cvsToWrite.Enqueue(new(Cvs.VMid, parms.CurveParameters.CV6_VMid));
            cvsToWrite.Enqueue(new(Cvs.VHigh, parms.CurveParameters.CV5_VHigh));
        }

        // 4. Perform write
        var writeResult = await WriteCVValuesAsync(parms.ProgTarget, parms.LocoAddress, cvsToWrite);

        if (writeResult.IsSuccess)
        {
            return new WriteLocoSpeedCurveResponse(
                true,
                null
                );
        }

        return new WriteLocoSpeedCurveResponse(
            false,
            writeResult.ErrorMessage
            );
    }

    #endregion Exposed methods
}
