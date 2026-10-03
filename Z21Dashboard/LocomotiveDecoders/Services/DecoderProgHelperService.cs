using System.Collections.ObjectModel;
using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Interfaces;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.NmraDccStandard;
using Z21Dashboard.NmraDccStandard.Interfaces;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Ui.Models;

namespace Z21Dashboard.LocomotiveDecoders.Services;

/// <summary>
/// Implementation of <see cref="IDecoderProgHelperService"/>. This class contain methods
/// that will perform 'all the hard work' and call the lowlevel metods in DecoderProgService 
/// to do the actual reading and writing to the decoder.
/// 
/// One example is ReadLocoAddress that reads the configuration to determine if the address
/// is short or long, and then read the relevant CVs for the type of address.
/// </summary>
/// <param name="decoderProgService"></param>
/// <param name="manufacturerService"></param>
internal class DecoderProgHelperService(
    IDecoderProgService decoderProgService,
    IManufacturerService manufacturerService
    ) : IDecoderProgHelperService
{
    #region Exposed methods

    /// <inheritdoc />
    public async Task<Result> ExtractCvsAndBitsAndReadFromDecoderAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        string pageId,
        DecoderSettings decoderSettings
        )
    {
        var pageContent = decoderSettings.Pages.FirstOrDefault(p => p.Id == pageId);
        if (pageContent is null)
        {
            // "Cannot find page with id {0}"
            return Result.Failure(string.Format(SharedResources.MissingPageId, pageId));
        }

        var cvBitsInPage = GetCvsAndBits(pageContent.PageContent, decoderSettings.CvValues, getForWrite: false);

        var cvsToRead = new Queue<ushort>(cvBitsInPage.Select(x => x.Cv).Distinct());

        var result = await ReadCVValuesAsync(progTarget, locoAddress, cvsToRead);
        if (result.IsSuccess)
        {
            foreach (var cv in result.Data!)
            {
                Result resultSetCv;
                // We need to be carefull and set either by bit or by whole value:
                var listCvInfo = cvBitsInPage.Where(x => x.Cv == cv.Cv);
                foreach (var entry in listCvInfo)
                {
                    if (entry.FromBit is null)
                    {
                        resultSetCv = decoderSettings.SetCvValue(cv.Cv, cv.Value);
                        if (resultSetCv.IsSuccess is false)
                        {
                            return Result.Failure(result.Messages);
                        }
                    }
                    else if (entry.ToBit is null || entry.ToBit == entry.FromBit)
                    {
                        resultSetCv = decoderSettings.SetCvBitValue(cv.Cv, (Bits)entry.FromBit, (cv.Value & (1 << (int)entry.FromBit)) != 0);
                        if (resultSetCv.IsSuccess is false)
                        {
                            return Result.Failure(result.Messages);
                        }
                    }
                    else
                    {
                        for (var i = (int)entry.FromBit; i <= (int)entry.ToBit; i++)
                        {
                            resultSetCv = decoderSettings.SetCvBitValue(cv.Cv, (Bits)i, (cv.Value & (1 << i)) != 0);
                            if (resultSetCv.IsSuccess is false)
                            {
                                return Result.Failure(resultSetCv.Messages);
                            }
                        }
                    }
                }
            }
            return Result.Success();
        }

        return Result.Failure(result.Messages);
    }

    /// <inheritdoc />
    public async Task<Result> ExtractCvsAndBitsAndWriteToDecoderAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        string pageId,
        DecoderSettings decoderSettings
    )
    {
        var pageContent = decoderSettings.Pages.FirstOrDefault(p => p.Id == pageId);
        if (pageContent is null)
        {
            // "Cannot find page with id {0}"
            return Result.Failure(string.Format(SharedResources.MissingPageId, pageId));
        }

        var cvBitsInPage = GetCvsAndBits(pageContent.PageContent, decoderSettings.CvValues, getForWrite: true);

        // This method build up a structure that contains the needed
        // information to be able to update the CV values in the decoder, by either setting a bit or a whole value, depending on the CV and bit information from the page content, and the current values in the decoder settings.
        var result = BuildCvValuesFromDecoderSettings(cvBitsInPage, decoderSettings);
        if (result.IsSuccess is false)
        {
            return Result.Failure(result.Messages);
        }

        var cvStates = result.Data!;
        Queue<CVValue> cvValuesToWrite = [];

        // For each cv that has null bits (flags that are not set) read the value:
        foreach (var entry in cvStates)
        {
            if (entry.Value.AnyBitNotSet() is true)
            {
                // When a bitwise CV has some flags set, and other not we need to read
                // the current value from the decoder, patch it with the new bit values
                // and write the whole value back.

                // Get the decoder value:
                Queue<ushort> cvToRead = new([entry.Key]);
                var resultReadCVValuesAsync = await ReadCVValuesAsync(progTarget, locoAddress, cvToRead);
                if (resultReadCVValuesAsync.IsSuccess is false)
                {
                    if (resultReadCVValuesAsync.Messages is not null)
                    {
                        return Result.Failure(resultReadCVValuesAsync.Messages);
                    }
                    return Result.Failure(string.Empty);
                }
                var decoderValue = resultReadCVValuesAsync.Data!.First().Value;
                var patchedValue = entry.Value.PatchWithWholeValue(decoderValue);
                if (patchedValue is not null)
                {
                    cvValuesToWrite.Enqueue(new(entry.Key, (byte)patchedValue));
                }
            }
            else
            {
                // We either have a bitwise CV with all bits set, or a value CV,
                // in both cases we can just write the whole value:
                var wholeValue = entry.Value.GetWholeValue();
                if (wholeValue is not null)
                {
                    cvValuesToWrite.Enqueue(new(entry.Key, (byte)wholeValue));
                }
            }
        }

        var resultWriteCvValues = await WriteCVValuesAsync(progTarget, locoAddress, cvValuesToWrite);

        return resultWriteCvValues.Match(
            onSuccess: () => Result.Success(),
            onFailure: error => Result.Failure(error)
            );
    }

    /// <inheritdoc />
    public async Task<Result<List<CVValue>>> ReadCVValuesAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        Queue<ushort> cvsToRequest,
        bool turnOnTrackPowerAfterReadPT = true
        )
    {
        if (progTarget is ProgrammingTarget.POM)
        {
            if (locoAddress is null or 0 or > Limits.HighestLongAddress)
            {
                // "The locomotive address must have a value betweeen 1 and {0} in order to read on the main track"
                return Result<List<CVValue>>.Failure(string.Format(SharedResources.LocoAddressOutOfRange, Limits.HighestLongAddress));
            }
            return await decoderProgService.ReadCVAddressesPOMAsync((ushort)locoAddress, cvsToRequest);
        }
        else if (progTarget is ProgrammingTarget.ProgTrack)
        {
            return await decoderProgService.ReadCVAddressesProgTrackAsync(cvsToRequest, turnOnTrackPowerAfterReadPT);
        }
        else
        {
            // "Unknown programming target"
            return Result<List<CVValue>>.Failure(SharedResources.UnknownProgTarget);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ushort>> ReadLocoAddressAsync()
    {
        string errorMessage = string.Empty;
        ushort locoAddress = 0;

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ConfigurationData1);

        var result = await ReadCVValuesAsync(
            ProgrammingTarget.ProgTrack,
            null,
            cvsToRequest
            );

        if (result.IsSuccess)
        {
            if (result.Data!.Count > 1)
            {
                // "More than one CV value was returned from Z21"
                errorMessage = SharedResources.MoreThanOneCV;
            }
            else if (result.Data!.Count is 1)
            {
                cvsToRequest.Clear();

                var cv29Value = result.Data![0].Value;
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

                result = await ReadCVValuesAsync(
                    ProgrammingTarget.ProgTrack,
                    null,
                    cvsToRequest
                    );
                if (result.IsSuccess)
                {
                    // Check if short or long address:
                    if (result.Data![0].Cv is Cvs.ShortAddress)
                    {
                        locoAddress = result.Data![0].Value;
                    }
                    else if (result.Data![0].Cv is Cvs.LongAddressHighByte or Cvs.LongAddressLowByte)
                    {
                        // Calculate the long address:
                        var cv17Value = result.Data!.Find(x => x.Cv is Cvs.LongAddressHighByte);
                        var cv18Value = result.Data!.Find(x => x.Cv is Cvs.LongAddressLowByte);

                        // Part one of the workaround
                        if (cv17Value is null || cv18Value is null)
                        {
                            // "Returned CV17 or CV18 does not contain a value"
                            errorMessage = SharedResources.CV1718NoValue;
                        }
                        else
                        {
                            locoAddress = (ushort)((cv17Value.Value - 192) * 256 + cv18Value.Value);
                        }
                    }
                }
            }
            else
            {
                // "No CV values returned but no errors was reported"
                errorMessage = SharedResources.NoCVValueNoError;
            }
        }

        return result.Match(
            onSuccess: () => Results.Success(locoAddress),
            onFailure: errorMessages => Result<ushort>.Failure(errorMessages)
            );
    }
    
    /// <inheritdoc />
    public async Task<Result<DecoderManufacturerInfo>> ReadLocoDecoderManufacturerAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress
        )
    {
        byte manufacturerCode = 0;
        string manufacturerName = string.Empty;
        byte manufacturerVersionNbr = 0;
        string statusMessage = string.Empty;

        Queue<ushort> cvsToRequest = [];
        cvsToRequest.Enqueue(Cvs.ManufacturerVersionNbr);
        cvsToRequest.Enqueue(Cvs.Manufacturer);

        var result = await ReadCVValuesAsync(progTarget, locoAddress, cvsToRequest);

        if (result.IsSuccess)
        {
            if (result.Data!.Count > 0)
            {
                manufacturerCode = result.Data!.Find(x => x.Cv == Cvs.Manufacturer)?.Value ?? 0;
                var getManufacturer = await manufacturerService.GetManufacturerAsync(manufacturerCode);
                // "(Unknown)"
                manufacturerName = getManufacturer is null ? SharedResources.UnknownManufacturer : getManufacturer.Name;
                manufacturerVersionNbr = result.Data!.Find(x => x.Cv == Cvs.ManufacturerVersionNbr)?.Value ?? 0;
            }
        }

        return result.IsSuccess ? Results.Success(new DecoderManufacturerInfo(manufacturerCode, manufacturerName, manufacturerVersionNbr)) :
            Result<DecoderManufacturerInfo>.Failure(result.Messages);
    }

    /// <inheritdoc />
    public async Task<Result> WriteCVValuesAsync(
        ProgrammingTarget progTarget,
        ushort? locoAddress,
        Queue<CVValue> cvsToWrite
        )
    {
        if (progTarget is ProgrammingTarget.POM)
        {
            if (locoAddress is null)
            {
                // "The locomotive address must not be 'null'"
                return Result.Failure(SharedResources.LocoAddressMustNotBeNull);
            }
            return await decoderProgService.WriteCVAddressesPOMAsync((ushort)locoAddress, cvsToWrite);
        }
        else if (progTarget is ProgrammingTarget.ProgTrack)
        {
            return await decoderProgService.WriteCVAddressesProgTrackAsync(cvsToWrite);
        }
        else
        {
            // "Unknown programming target"
            return Result.Failure(SharedResources.UnknownProgTarget);
        }
    }

    /// <inheritdoc />
    public async Task<Result> WriteLocoAddressAsync(ushort newLocoAddress)
    {
        if (newLocoAddress is 0 or > Limits.HighestLongAddress)
        {
            // "New locomotive address must be between 1 and {0}"
            return Result.Failure(string.Format(SharedResources.NewLocoAddressOutOfRange, Limits.HighestLongAddress));
        }
        else
        {
            Queue<ushort> cvsToRequest = [];
            cvsToRequest.Enqueue(Cvs.ConfigurationData1);
            var result = await ReadCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToRequest);
            if (result.IsSuccess)
            {
                if (result.Data!.Count > 0)
                {
                    Queue<CVValue> cvsToWrite = [];

                    var cv29Value = result.Data![0].Value;
                    if (newLocoAddress > Limits.HighestShortAddress2)
                    {
                        // Long address: Set bit 5 to 1
                        cv29Value |= Cvs.CV29_Bit5_LongAddress;
                        cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));

                        var cv17 = 192 + (newLocoAddress / 256);
                        var cv18 = newLocoAddress % 256;

                        cvsToWrite.Enqueue(new(Cvs.LongAddressHighByte, (byte)cv17));
                        cvsToWrite.Enqueue(new(Cvs.LongAddressLowByte, (byte)cv18));
                    }
                    else
                    {
                        // Short address: Set bit 5 to 0
                        cv29Value = (byte)(cv29Value & ~Cvs.CV29_Bit5_LongAddress);
                        cvsToWrite.Enqueue(new(Cvs.ConfigurationData1, cv29Value));
                        cvsToWrite.Enqueue(new(Cvs.ShortAddress, (byte)newLocoAddress));
                    }

                    var resultWriteCV = await WriteCVValuesAsync(ProgrammingTarget.ProgTrack, null, cvsToWrite);
                    if (!resultWriteCV.IsSuccess)
                    {
                        return Result.Failure(resultWriteCV.Messages);
                    }
                }
            }
            if (result.IsSuccess)
            {
                return Result.Success();
            }
            else
            {
                return Result.Failure(result.Messages);
            }
        }
    }

    #endregion Exposed methods

    /// <summary>
    /// Adds a CV and bit to the local structure that contains the CVs and bits 
    /// on the page, if it does not already exists.
    /// </summary>
    /// <param name="cv"></param>
    /// <param name="bit"></param>
    /// <param name="cvBitsInPage"></param>
    private static void AddToCvBitsInPageIfNotExists(
        ushort cv,
        Bits? fromBit,
        Bits? toBit,
        ref HashSet<CvBitInPage> cvBitsInPage
        )
    {
        var cvState = new CvBitInPage()
        {
            Cv = cv,
            FromBit = fromBit,
            ToBit = toBit
        };

        if (cvBitsInPage.Contains(cvState) is false)
        {
            cvBitsInPage.Add(cvState);
        }
    }

    /// <summary>
    /// Evaluate a conditional rule, by checking the actual value of the CV
    /// and bit defined in the rule against the expected value in the rule.
    /// </summary>
    /// <param name="rule">The rule to test.</param>
    /// <returns>true/false for if the rule matches the actual value in
    /// DecoderSettings.CvValues</returns>
    private static bool DoesRuleMatch(
        Dictionary<ushort, CvState> cvValues,
        CvConditionalRule rule
        )
    {
        if (cvValues.TryGetValue(rule.Cv, out var cvValue))
        {
            if (rule.Bit is null)
            {
                // We are looking for at Value:
                return (rule.Value == cvValue.GetValue());
            }
            else
            {
                // We are looking for a bit value
                // The compare value (from decoder file, cannot be null)
                var theValue = cvValue.GetBit((Bits)rule.Bit);
                return (theValue is true && rule.Value == 1 ||
                        theValue is false && rule.Value == 0);
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the CVs from the PageContent fields and if they are a
    /// value or/and bits. This is stored in a local structure, in
    /// order to know which cvs and bits are on this page.
    /// The getForWrite flag, if true, only get the CVs/bits that are relevant for write (rules that match),
    /// otherwise get all CVs/bits on the page to be able to manage potential change between values as they all
    /// will be read from the decoder.
    /// </summary>
    /// <parm name="getForWrite">Flag for are we getting CVs/bits for write operations (or read).</parm>
    /// <returns></returns>
    private static HashSet<CvBitInPage> GetCvsAndBits(
        ObservableCollection<PageContentBase> pageContent,
        Dictionary<ushort, CvState> cvValues,
        bool getForWrite
        )
    {
        // The combination of CV and possibly bit that are on the current page.
        // This is used to know which CVs and bits to read/write when the user
        // clicks the buttons.
        HashSet<CvBitInPage> cvBitsInPage = [];

        foreach (var item in pageContent)
        {
            switch (item)
            {
                case CvSimpleContent sc:
                    foreach (var subItem in sc.Content)
                    {
                        switch (subItem)
                        {
                            case CvSingleItem csi:
                                AddToCvBitsInPageIfNotExists(csi.Cv, null, null, ref cvBitsInPage);
                                break;

                            case CvWithBitItem cwbi:
                                AddToCvBitsInPageIfNotExists(cwbi.Cv, (Bits?)cwbi.Bit, (Bits?)cwbi.BitTo, ref cvBitsInPage);
                                break;

                            case CvRangeItem cri:
                                for (ushort cv = cri.CvStart; cv <= cri.CvEnd; cv++)
                                {
                                    AddToCvBitsInPageIfNotExists(cv, null, null, ref cvBitsInPage);
                                }
                                break;
                        }
                    }
                    break;

                case CvConditionalContent cc:
                    foreach (var subItem in cc.Content)
                    {
                        // if for write, only take those that match:
                        bool addToLocal = !getForWrite || DoesRuleMatch(cvValues, subItem);

                        if (addToLocal)
                        {
                            foreach (var subSubItem in subItem.Content)
                            {
                                switch (subSubItem)
                                {
                                    case CvSingleItem csi:
                                        AddToCvBitsInPageIfNotExists(csi.Cv, null, null, ref cvBitsInPage);
                                        break;

                                    case CvWithBitItem cwbi:
                                        AddToCvBitsInPageIfNotExists(cwbi.Cv, (Bits?)cwbi.Bit, (Bits?)cwbi.BitTo, ref cvBitsInPage);
                                        break;

                                    case CvRangeItem cri:
                                        for (ushort cv = cri.CvStart; cv <= cri.CvEnd; cv++)
                                        {
                                            AddToCvBitsInPageIfNotExists(cv, null, null, ref cvBitsInPage);
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    break;
            }
        }

        return cvBitsInPage;
    }

    /// <summary>
    /// Copy values over, so it is easy for us to set values.
    /// </summary>
    private static Result<Dictionary<ushort, CvState>> BuildCvValuesFromDecoderSettings(
        HashSet<CvBitInPage> cvBitsInPage,
        DecoderSettings decoderSettings
        )
    {
        // Just in case there should be no bits in the page, but that is unlikely
        Result result = Result.Success();

        Dictionary<ushort, CvState> cvStates = [];
        foreach (var entry in cvBitsInPage)
        {
            if (cvStates.ContainsKey(entry.Cv) is false)
            {
                cvStates[entry.Cv] = new CvState(decoderSettings.CvMetadata[entry.Cv]);
            }

            if (entry.FromBit is null)
            {
                result = cvStates[entry.Cv].SetValue(decoderSettings.GetCvValue(entry.Cv));
            }
            else if (entry.ToBit is null || entry.FromBit == entry.ToBit)
            {
                result = cvStates[entry.Cv].SetBit((Bits)entry.FromBit, decoderSettings.GetCvBit(entry.Cv, (Bits)entry.FromBit));
            }
            else
            {
                for (var i = (int)entry.FromBit; i <= (int)entry.ToBit; i++)
                {
                    result = cvStates[entry.Cv].SetBit((Bits)i, decoderSettings.GetCvBit(entry.Cv, (Bits)i));
                }
            }
        }

        if (result.IsSuccess)
        { 
            return Results.Success(cvStates);
        }
        else
        {
            return Result<Dictionary<ushort, CvState>>.Failure(result.Messages);
        }
    }
}
