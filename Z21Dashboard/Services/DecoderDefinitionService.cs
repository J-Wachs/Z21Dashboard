using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Text.Json;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Shared.Dashboard.Components.ProgrammingDecoder.AdvTabs;

namespace Z21Dashboard.Services;

/// <summary>
/// Service responsible for loading decoder definitions from JSON files (both app resources and local storage),
/// handling inheritance from NMRA standards, and populating the runtime settings with Values AND Metadata.
/// </summary>
internal class DecoderDefinitionService(
    IDashboardStateService dashboardStateService,
    ILanguageSupportService languageSupportService,
    ILogger<DecoderDefinitionService> logger,
    INmraCvService nmraCvService
    ) : IDecoderDefinitionService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    #region Exposed Methods

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DecoderListItemDto>?>> GetLocoDecodersAsync()
    {
        List<DecoderListItemDto> decoderList = [];

        // Step 1: Load from Application Resources
        var resultLoadFromAppRess = await LoadLocoDecoderIndexFromAppResourceAsync();
        if (resultLoadFromAppRess.IsSuccess is false || resultLoadFromAppRess.Data is null)
        {
            return Result<IReadOnlyList<DecoderListItemDto>?>.Failure(resultLoadFromAppRess.Messages);
        }
        var appDecoders = resultLoadFromAppRess.Data;
        decoderList.AddRange(appDecoders);

        // Step 2: Load from Local File System
        var resultLoadFromLocalFileSys = await LoadLocoDecoderIndexFromLocalFileSystemAsync();
        if (resultLoadFromLocalFileSys.IsSuccess is false || resultLoadFromLocalFileSys.Data is null)
        {
            return Result<IReadOnlyList<DecoderListItemDto>?>.Failure(resultLoadFromAppRess.Messages);
        }
        var localDecoders = resultLoadFromLocalFileSys.Data;
        decoderList.AddRange(localDecoders);

        return Result<IReadOnlyList<DecoderListItemDto>?>.Success([.. decoderList]);
    }

    /// <inheritdoc />
    public async Task<DecoderFileRootDto?> GetMetaInfoFromLocalLocoDecoderFile(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        return JsonSerializer.Deserialize<DecoderFileRootDto>(content, _jsonOptions);
    }


    /// <inheritdoc />
    public async Task<Result> SaveValuesToUserDefinedLocoDecoderAsync(DecoderListItemDto decoder, DecoderSettings decoderSettings)
    {
        if (decoder.Location is not DecoderFileLocation.LocalStorage)
        {
            return Result.Failure(SharedResources.CannotSaveDecoderDef);
        }

        var resultDecoderDefinition = await GetDecoderDefinitionAsync(decoder.FileName, decoder.Location);
        if (resultDecoderDefinition.IsSuccess is false || resultDecoderDefinition.Data is null)
        {
            return Result.Failure(resultDecoderDefinition.Messages);
        }

        var decoderDefinition = resultDecoderDefinition.Data;

        foreach (var cvValue in decoderSettings.CvValues.Where(x => x.Value.IsChanged))
        {
            // Look at metadata, if contains bits, get old value
            if (decoderSettings.CvMetadata.TryGetValue(cvValue.Key, out var metadata))
            {
                var foundCv = decoderDefinition.CvDefinitions.FirstOrDefault(x => x is CvSingle && (x as CvSingle)?.Cv == cvValue.Key);
                if (foundCv is CvSingle cvSingleObject)
                {
                    // If all bits are set, then just write the value if it was changed
                    if (cvValue.Value.AnyBitNotSet() is false)
                    {
                        // Save CV, WholeValue
                        cvSingleObject.DefaultValue = cvValue.Value.GetWholeValue();
                    }
                    else
                    {
                        // Some bits are set, others not. We need to read the old value, patch the bits and then write back the whole value (only if changed)
                        // Read value

                        // Patch value with new bit values from cvValue.Value
                        cvSingleObject.DefaultValue = cvValue.Value.PatchWithWholeValue(cvSingleObject.DefaultValue ?? 0);
                    }
                }
            }
            else
            {
                // "Information about cv-address {0} was not found. Operation aborted"
                return Result.Failure(string.Format(SharedResources.CvMetadataNotFound, cvValue.Key));
            }
        }

        var resultSaveDecoderDefinition = await SaveDecoderDefinitionAsync(decoder, decoderDefinition);
        if (resultSaveDecoderDefinition.IsSuccess is false)
        {
            return Result.Failure(resultSaveDecoderDefinition.Messages);
        }

        decoderSettings.ClearChanged();

        await Task.CompletedTask;

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> GetLocoDecoderDefAndSetCVsIntoSettings(DecoderListItemDto decoder, DecoderSettings decoderSettings)
    {
        // 0. Prepare settings object
        decoderSettings.Clear();
        decoderSettings.SetDecoder(decoder.FileName, decoder.Location, decoder.Name, decoder.Manufacturer);

        Dictionary<ushort, short> finalCvValues = [];

        DecoderDefinitionFileDto? specificDecoder = null;
        DecoderDefinitionFileDto? nmraDecoder = null;
        ObservableCollection<UiPageDto>? pagesToUse = null;

        try
        {
            // 1. Load the specific decoder file
            var resultGetDecoderDefinition = await GetDecoderDefinitionAsync(decoder.FileName, decoder.Location);
            if (resultGetDecoderDefinition.IsSuccess is false || resultGetDecoderDefinition.Data is null)
            {
                return Result.Failure(resultGetDecoderDefinition.Messages);
            }

            specificDecoder = resultGetDecoderDefinition.Data;

            if (specificDecoder?.CvDefinitions is null)
            {
                // "Decoder file {0} from {1} has no CV definitions"
                var errorMessage = string.Format(SharedResources.DecoderFileNoCvDefinitions, decoder.FileName, decoder.Location);
                return Result.Failure(errorMessage);
            }

            // Prioritize UI pages from specific file
            if (specificDecoder.UiPages is not null && specificDecoder.UiPages.Count > 0)
            {
                pagesToUse = specificDecoder.UiPages;
            }

            // 2. Check for NMRA inheritance
            bool needsNmra = specificDecoder.CvDefinitions
                .Any(x => string.Equals(x.Source, "nmra", StringComparison.OrdinalIgnoreCase));

            if (needsNmra)
            {
                // Load NMRA data (full DTO)
                var resultLoadNmraDataDto = await LoadNmraDataDtoAsync();
                if (resultLoadNmraDataDto.IsSuccess is false)
                {
                    return Result.Failure(resultLoadNmraDataDto.Messages);
                }
                nmraDecoder = resultLoadNmraDataDto.Data;

                // Fallback: If specific file has no pages, use NMRA pages
                if (pagesToUse is null && nmraDecoder?.UiPages is not null)
                {
                    pagesToUse = nmraDecoder.UiPages;
                }
            }

            // 2.1. We cannot go-on if no pages:
            if (pagesToUse is null)
            {
                // "The selected decoder '{0}', file '{1}' from manufacturer '{2}', does not have any pages defined. Pages are needed"
                return Result.Failure(string.Format(SharedResources.NoPagesInDecoderFiles, decoderSettings.DecoderName, decoderSettings.DecoderFile, decoderSettings.DecoderMfg));
            }

            // 3. Identify ALL unique CV numbers involved (Specific + NMRA)
            HashSet<ushort> allCvNumbers = [];

            // From Specific
            foreach (var def in specificDecoder.CvDefinitions)
                foreach (var cv in ExtractCvNumbers(def))
                    allCvNumbers.Add(cv);

            // From NMRA
            if (nmraDecoder?.CvDefinitions is not null)
            {
                foreach (var def in nmraDecoder.CvDefinitions)
                    foreach (var cv in ExtractCvNumbers(def))
                        allCvNumbers.Add(cv);
            }

            // 4. Iterate all CVs to populate Values AND Metadata
            foreach (var cv in allCvNumbers)
            {
                // Find definition objects (can be null)
                var specificDef = FindDefinitionForCv(specificDecoder.CvDefinitions, cv);
                var nmraDef = (nmraDecoder is not null) ? FindDefinitionForCv(nmraDecoder.CvDefinitions, cv) : null;

                // A. Set Value (Specific wins over NMRA)
                byte? val = null;
                if (specificDef is not null)
                {
                    val = specificDef.DefaultValue;
                }
                else if (nmraDef is not null)
                {
                    val = nmraDef.DefaultValue;
                }

                // B. Build Metadata (Specific -> NMRA -> Service)
                var metadata = await BuildCvMetadataAsync(cv, specificDef, nmraDef);
                if (metadata is not null)
                {
                    decoderSettings.CvMetadata[cv] = metadata;
                }

                // This updates CvValues in settings (and handles bits)
                decoderSettings.SetCvWholeValue(cv, val);
            }

            // 5. Write UI Pages to settings
            foreach (var pageDto in pagesToUse)
            {
                var pageModel = new DecoderPage
                {
                    Id = pageDto.Id ?? Guid.NewGuid().ToString(),
                    Type = pageDto.Type ?? DecoderUIPageTypes.DynamicList,
                    Title = GetLocalizedText(pageDto.Title) ?? string.Empty,
                    Description = GetLocalizedText(pageDto.Description),
                    PageContent = pageDto.PageContent
                };
                decoderSettings.Pages.Add(pageModel);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            // "Error loading decoder definition: {ex.Message}"
            // "Failed to load decoder definition for {0} from {1}. Error: '{2}'"
            var errorMessage = string.Format(SharedResources.ErrorLoadingDecoderDef, decoder.Name, decoder.Location, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
    }

    #endregion Exposed Methods

    #region Metadata Building Logic

    /// <summary>
    /// Saves the specified decoder definition to local storage if the decoder's location is set to LocalStorage.
    /// </summary>
    /// <param name="decoder">The decoder list item that determines the target file name and storage location. The location must be
    /// LocalStorage.</param>
    /// <param name="decoderDefinition">The decoder definition data to be serialized and saved.</param>
    /// <returns>A Result indicating whether the save operation succeeded. Returns a failure result if saving is not permitted or
    /// an error occurs.</returns>
    private async Task<Result> SaveDecoderDefinitionAsync(DecoderListItemDto decoder, DecoderDefinitionFileDto decoderDefinition)
    {
        try
        {
            string jsonContent = JsonSerializer.Serialize(decoderDefinition, _jsonOptions);
            if (decoder.Location == DecoderFileLocation.LocalStorage)
            {
                var dashboardSettings = await dashboardStateService.GetSettingsAsync();
                string filePath = Path.Combine(dashboardSettings.LocalLocoDecoderFolder!, decoder.FileName);
                await File.WriteAllTextAsync(filePath, jsonContent);

                return Result.Success();
            }
            else
            {
                // This should never ever happen as the UI should not allow saving to app resources, but we put this here for safety and completeness:
                // "Cannot save decoder definition to application resources"
                return Result.Failure(SharedResources.CannotSaveDecoderDef);
            }
        }
        catch (Exception ex)
        {
            // "Error saving decoder definition '{0}' to location '{1}'. Error: '{2}'"
            var errorMessage = string.Format(SharedResources.ErrorSavingDecoderDef, decoder.Name, decoder.Location, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Build the metadata for a given CV by looking at the specific decoder
    /// definition first, then falling back to NMRA definition,
    /// </summary>
    /// <param name="cv"></param>
    /// <param name="specificDef"></param>
    /// <param name="nmraDef"></param>
    /// <returns></returns>
    private async Task<CvInformation> BuildCvMetadataAsync(
        ushort cv,
        CvDefinitionItemDto? specificDef,
        CvDefinitionItemDto? nmraDef)
    {
        var info = new CvInformation { Cv = cv };

        // 1. NAME
        string? name = GetLocalizedText(specificDef?.Name) ?? GetLocalizedText(nmraDef?.Name);

        (string Name, string Description) serviceData = (string.Empty, string.Empty);

        if (name is null)
        {
            // Fallback to Service
            serviceData = await nmraCvService.GetCVNameAndDescription(cv);
            if (!string.IsNullOrEmpty(serviceData.Name))
            {
                name = serviceData.Name;
            }
        }

        if (name is not null)
        {
            info.Name = name;
        }

        // 2. DESCRIPTION
        string? desc = GetLocalizedText(specificDef?.Description) ?? GetLocalizedText(nmraDef?.Description);

        if (desc is null)
        {
            // Fallback to Service
            // Note: serviceData might be populated from step 1, check first
            if (!string.IsNullOrEmpty(serviceData.Description))
            {
                desc = serviceData.Description;
            }
            else if (string.IsNullOrEmpty(serviceData.Name)) // If we haven't called service yet
            {
                // Try fetching just description or full data again if needed, 
                // but GetCVDescription exists on interface
                var sDesc = await nmraCvService.GetCVDescription(cv);
                if (string.IsNullOrEmpty(sDesc) is false)
                {
                    desc = sDesc;
                }
            }
        }

        if (desc is not null)
        {
            info.Description = desc;
        }

        // 3. BIT DETAILS (Flag Bits)
        // We iterate over ALL bits (0 to 7) in order to ensure that we catch bits
        // that only exists in the service, eventhough the JSON-files are empty ub the flag_bits
        // section.
        if (specificDef is CvSingle cs && cs.FlagBits is not null)
        {
            for (int i = 0; i < 8; i++)
            {
                var specificBit = cs.FlagBits.FirstOrDefault(b => b.Bit == i);
                var nmraBit = (nmraDef as CvSingle)?.FlagBits?.FirstOrDefault(b => b.Bit == i);

                var bitEnum = MapIntToBitEnum(i);

                // A. Bit Name
                // Prioritet: Specific decoder JSON -> NMRA decoder JSON -> NMRA Service
                string? bName = GetLocalizedText(specificBit?.Name) ?? GetLocalizedText(nmraBit?.Name);

                if (bName is null)
                {
                    var serviceBitName = await nmraCvService.GetCVBitName(cv, bitEnum);
                    if (!string.IsNullOrEmpty(serviceBitName))
                    {
                        bName = serviceBitName;
                    }
                }

                // If we have found a name OR if the bit is explicit defined in JSON (even
                // without a name) vi create information about it.
                if (bName is not null || specificBit is not null || nmraBit is not null)
                {
                    var bitInfo = new CvBitInformation()
                    {
                        // Bit = i,
                        Name = bName ?? string.Empty
                    };

                    // B. Bit Labels (0/1)

                    // Off Label ("0")
                    Dictionary<string, string>? specificZeroDict =
                        specificBit?.Values is not null && specificBit.Values.TryGetValue("0", out Dictionary<string, string>? value1) ? value1 : null;
                    Dictionary<string, string>? nmraZeroDict =
                        nmraBit?.Values is not null && nmraBit.Values.TryGetValue("0", out Dictionary<string, string>? value) ? value : null;

                    string? offLabel = GetLocalizedText(specificZeroDict) ?? GetLocalizedText(nmraZeroDict);

                    if (offLabel is null)
                    {
                        var bVals = await nmraCvService.GetCVBitValuesDescription(cv, bitEnum);
                        if (!string.IsNullOrEmpty(bVals.ZeroDescription))
                        {
                            offLabel = bVals.ZeroDescription;
                        }
                    }
                    if (offLabel is not null) bitInfo.OffLabel = offLabel;

                    // On Label ("1")
                    Dictionary<string, string>? specificOneDict =
                        specificBit?.Values is not null && specificBit.Values.TryGetValue("1", out Dictionary<string, string>? value2) ? value2 : null;
                    Dictionary<string, string>? nmraOneDict =
                        nmraBit?.Values is not null && nmraBit.Values.TryGetValue("1", out Dictionary<string, string>? value3) ? value3 : null;

                    string? onLabel = GetLocalizedText(specificOneDict) ?? GetLocalizedText(nmraOneDict);

                    if (onLabel is null)
                    {
                        // Reuse the call from before if possible, else make new call. 
                        // Her laver vi et nyt kald for sikkerheds skyld (eller antager bVals fra før kunne genbruges hvis scopet tillod det)
                        var bVals = await nmraCvService.GetCVBitValuesDescription(cv, bitEnum);
                        if (!string.IsNullOrEmpty(bVals.OneDescription))
                        {
                            onLabel = bVals.OneDescription;
                        }
                    }
                    if (onLabel is not null) bitInfo.OnLabel = onLabel;

                    info.BitDetails[i] = bitInfo;
                }
            }
        }

        // 4. Get Min/Max values and value bits.
        // Lookup on the selected decoder, if null then read from standard NMRA
        if (specificDef is not null && specificDef is CvSingle cs2)
        {
            if (specificDef.Source == "nmra")
            {
                var resultGetRawCvDefinition = await nmraCvService.GetRawCvDefinition(cv);
                if (resultGetRawCvDefinition?.ValueBits is not null)
                {
                    info.ValueBits = resultGetRawCvDefinition.ValueBits;
                    if (info.ValueBits.FromBit is null &&
                        info.ValueBits.ToBit is null &&
                        resultGetRawCvDefinition.FlagBits is null)
                    {
                        info.ValueBits.FromBit = 0;
                        info.ValueBits.ToBit = 7;
                    }
                }
                else if (resultGetRawCvDefinition?.FlagBits is null)
                {
                    info.ValueBits = new()
                    {
                        Min = 0,
                        Max = 255,
                        FromBit = 0,
                        ToBit = 7
                    };
                }

                if (resultGetRawCvDefinition?.FlagBits is not null)
                {
                    info.BitDetails = GetFlagBitsInfo(resultGetRawCvDefinition.FlagBits);
                }
            }
            else if (specificDef.Source == "custom")
            {
                if (cs2.Cv == 124)
                {
                    var t = "jj";
                }
                if (cs2.ValueBits is not null)
                {
                    //info.ValueBits = specificDef.  .ValueBits;
                }
                else if (cs2.FlagBits is null)
                {
                    info.ValueBits = new()
                    {
                        Min = 0,
                        Max = 255,
                        FromBit = 0,
                        ToBit = 7
                    };
                }

                if (cs2.FlagBits is not null)
                {
                    List<CvDefinition.CvFlagBit> flagBits = [];
                    foreach (var oneBit in cs2.FlagBits)
                    {
                        CvDefinition.CvFlagBit newBit = new()
                        {
                            Bit = oneBit.Bit,
                            Name = oneBit.Name ?? [],
                            Values = []
                        };

                        foreach (var valEntry in oneBit.Values ?? [])
                        {
                            if (int.TryParse(valEntry.Key, out var bitValue))
                            {
                                newBit.Values.Add(bitValue, valEntry.Value);
                            }
                        }

                        flagBits.Add(newBit);
                    }
                    info.BitDetails = GetFlagBitsInfo(flagBits);
                }
            }
        }

        return info;
    }

    /// <summary>
    /// Get information about flag bits for a CV and map it into
    /// our CvBitInformation structure.
    /// </summary>
    /// <param name="flagBits"></param>
    /// <returns></returns>
    private Dictionary<int, CvBitInformation> GetFlagBitsInfo(List<CvDefinition.CvFlagBit> flagBits)
    {
        Dictionary<int, CvBitInformation> dictBits = [];

        foreach (var entry in flagBits)
        {
            dictBits.Add(
                entry.Bit,
                    new()
                    {
                        Name = GetLocalizedText(entry.Name) ?? string.Empty,
                        OnLabel = GetLocalizedText(entry.Values is not null && entry.Values.Count() == 2 ? entry.Values[1] : null) ?? string.Empty,
                        OffLabel = GetLocalizedText(entry.Values is not null && entry.Values.Count() == 2 ? entry.Values[0] : null) ?? string.Empty
                    }
                );
        }

        return dictBits;
    }

    /// <summary>
    /// Helper to extract text from JSON dictionary based on current language or fallback to English.
    /// </summary>
    private string? GetLocalizedText(Dictionary<string, string>? dict)
    {
        if (dict is null)
        {
            return null;
        }
        if (dict.TryGetValue(languageSupportService.GetLanguageCode(), out var val))
        {
            return val;
        }
        if (dict.TryGetValue(LanguageSupportService.EnglishLangCode, out var valEn))
        {
            return valEn;
        }
        return null;
    }

    /// <summary>
    /// Finds the CV definition item (either CvSingle or CvRange) that
    /// matches the given CV number from a list of definitions.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="cv"></param>
    /// <returns></returns>
    private static CvDefinitionItemDto? FindDefinitionForCv(List<CvDefinitionItemDto> list, ushort cv)
    {
        foreach (var oneDefinition in list)
        {
            switch (oneDefinition)
            {
                case CvSingle cs:
                    if (cs.Cv == cv)
                    {
                        return oneDefinition;
                    }
                    break;
                case CvRange cr:
                    if (cv >= cr.CvStart && cv <= cr.CvEnd)
                    {
                        return oneDefinition;
                    }
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract CV numbers from a CV definition item.
    /// </summary>
    /// <param name="def"></param>
    /// <returns></returns>
    private static IEnumerable<ushort> ExtractCvNumbers(CvDefinitionItemDto def)
    {
        switch (def)
        {
            case CvSingle cs:
                yield return cs.Cv;
                break;
            case CvRange cr:
                if (cr.CvEnd >= cr.CvStart && (cr.CvEnd - cr.CvStart) < 1000)
                {
                    for (ushort i = cr.CvStart; i <= cr.CvEnd; i++)
                    {
                        yield return i;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Map from integer bit index (0-7) to our Bits enum for
    /// service calls and consistency.
    /// </summary>
    /// <param name="bitIndex"></param>
    /// <returns></returns>
    private static Bits MapIntToBitEnum(int bitIndex)
    {
        return bitIndex switch
        {
            0 => Bits.Zero,
            1 => Bits.One,
            2 => Bits.Two,
            3 => Bits.Three,
            4 => Bits.Four,
            5 => Bits.Five,
            6 => Bits.Six,
            7 => Bits.Seven,
            _ => Bits.Zero
        };
    }

    #endregion

    #region Private Helpers (File Loading)

    /// <summary>
    /// Loads a decoder file form the application resources (packaged with
    /// the app) and returns a list of DecoderListItemDto.
    /// </summary>
    /// <returns></returns>
    private async Task<Result<IReadOnlyList<DecoderListItemDto>>> LoadLocoDecoderIndexFromAppResourceAsync()
    {
        List<DecoderListItemDto> results = [];
        // "#if WINDOWS" is used as the method FileSystem.OpenAppPackageFileAsync only exists
        // on Windows. The application is only for Windows, but the compiler gives a warning
        // if the code is not conditioned.
#if WINDOWS
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("AllLocoDecoders.json");
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            var indexItems = JsonSerializer.Deserialize<List<DecoderIndexItemDto>>(content, _jsonOptions);

            if (indexItems is not null)
            {
                foreach (var item in indexItems)
                {
                    results.Add(new(
                        item.FileName,
                        item.Name,
                        item.Manufacturer,
                        DecoderFileLocation.Application
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            // "Error reading app resource: {Message}"
            var errorMessage = string.Format(SharedResources.ErrorReadingAppResource, "AllLocoDecoders.json", ex.Message);
            logger.LogCritical(ex, errorMessage, ex.Message);
            return Result<IReadOnlyList<DecoderListItemDto>>.Failure(errorMessage);
        }
#endif
        return Result<IReadOnlyList<DecoderListItemDto>>.Success([.. results]);
    }

    /// <summary>
    /// Loads decoder files from the local file system and returns a list of DecoderListItemDto.
    /// </summary>
    /// <returns></returns>
    private async Task<Result<IReadOnlyList<DecoderListItemDto>>> LoadLocoDecoderIndexFromLocalFileSystemAsync()
    {
        var _dashboardSettings = await dashboardStateService.GetSettingsAsync();
        if (Path.Exists(_dashboardSettings.LocalLocoDecoderFolder) is false)
        {
            var errorMessage = string.Format(SharedResources.LocalDecoderFolderNotFound, _dashboardSettings.LocalLocoDecoderFolder);
            logger.LogError(errorMessage);
            return Result<IReadOnlyList<DecoderListItemDto>>.Failure(errorMessage);
        }

        List<DecoderListItemDto> results = [];
        // "#if WINDOWS" is used as the method Directory.GetFiles only exists
        // on Windows. The application is only for Windows, but the compiler gives a warning
        // if the code is not conditioned.
# if WINDOWS
        try
        {
            List<ResultMessages> errorMessages = [];

            var files = Directory.GetFiles(_dashboardSettings.LocalLocoDecoderFolder, "*.json");
            foreach (var filePath in files)
            {
                try
                {
                    DecoderFileRootDto? decoderFile = await GetMetaInfoFromLocalLocoDecoderFile(filePath);
                    if (decoderFile?.Meta is not null &&
                        !string.IsNullOrWhiteSpace(decoderFile.Meta.Manufacturer) &&
                        !string.IsNullOrWhiteSpace(decoderFile.Meta.Model))
                    {
                        results.Add(new(
                            Path.GetFileName(filePath),
                            decoderFile.Meta.Model,
                            decoderFile.Meta.Manufacturer,
                            DecoderFileLocation.LocalStorage
                        ));
                    }
                }
                catch (Exception ex)
                {
                    errorMessages.Add(new()
                    {
                        Messages = [
                            string.Format(SharedResources.ErrorLoadingLocalDecoderFile, Path.GetFileName(filePath), ex.Message)
                            ]
                    });
                }
            }

            if (errorMessages.Count > 0)
            {
                return Result<IReadOnlyList<DecoderListItemDto>>.Failure(errorMessages);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(SharedResources.ErrorLoadingLocalDecoders, _dashboardSettings.LocalLocoDecoderFolder, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result<IReadOnlyList<DecoderListItemDto>>.Failure(errorMessage);
        }
#endif
        return Result<IReadOnlyList<DecoderListItemDto>>.Success([.. results]);
    }

    /// <summary>
    /// Loads a specific decoder definition file (either from app
    /// resources or local storage based on location and return the 
    /// deserialied JSON content.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    private async Task<Result<DecoderDefinitionFileDto>> GetDecoderDefinitionAsync(string fileName, DecoderFileLocation location)
    {
#if WINDOWS
        try
        {
            string decoderfileJsonContent = string.Empty;

            if (location is DecoderFileLocation.Application)
            {
                var fullPath = @".\LocoDecoders\" + fileName;
                using var stream = await FileSystem.OpenAppPackageFileAsync(fullPath);
                using var reader = new StreamReader(stream);
                decoderfileJsonContent = await reader.ReadToEndAsync();
            }
            else
            {
                var _dashboardSettings = await dashboardStateService.GetSettingsAsync();
                if (Path.Exists(_dashboardSettings.LocalLocoDecoderFolder) is false)
                {
                    return Result<DecoderDefinitionFileDto>.Failure(string.Format(SharedResources.LocalDecoderFolderNotFound, _dashboardSettings.LocalLocoDecoderFolder));
                }

                string fullPath = Path.Combine(_dashboardSettings.LocalLocoDecoderFolder, fileName);
                if (File.Exists(fullPath) is false)
                {
                    return Result<DecoderDefinitionFileDto>.Failure(string.Format(SharedResources.DecoderfileDoesNotExist, fullPath));
                }

                decoderfileJsonContent = await File.ReadAllTextAsync(fullPath);
            }

            DecoderDefinitionFileDto? specificDecoder = JsonSerializer.Deserialize<DecoderDefinitionFileDto>(decoderfileJsonContent, _jsonOptions);
            if (specificDecoder is null)
            {
                return Result<DecoderDefinitionFileDto>.Failure(
                    string.Format(string.Format(SharedResources.ErrorReadingDecoderFile, fileName, location, SharedResources.CannotDeserialize))
                    );
            }
            else
            {
                return Results.Success(specificDecoder);
            }
        }
        catch (Exception ex)
        {
            // "Failed to load decoder definition for {0} from {1}. Error: '{2}'"
            var errorMessage = string.Format(SharedResources.ErrorReadingDecoderFile, fileName, location, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result<DecoderDefinitionFileDto>.Failure(errorMessage);
        }
#else
        return Result<DecoderDefinitionFileDto>.Failure("Not supported on this platform.");
#endif
    }

    /// <summary>
    /// Loads the NMRA standard decoder definition file and returns
    /// it as a DecoderDefinitionFileDto.
    /// </summary>
    /// <returns></returns>
    private async Task<Result<DecoderDefinitionFileDto?>> LoadNmraDataDtoAsync()
    {
        try
        {
#if WINDOWS
            using var stream = await FileSystem.OpenAppPackageFileAsync(@".\LocoDecoders\0-NMRAMaxStandardDecoder.json");
            using var reader = new StreamReader(stream);
            var nmraContent = await reader.ReadToEndAsync();
            return Results.Success(JsonSerializer.Deserialize<DecoderDefinitionFileDto>(nmraContent, _jsonOptions));
        }
        catch (Exception ex)
        {
            // "Could not load NMRA standard file"
            var errorMessage = string.Format(SharedResources.ErrorLoadingNmraStandard, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result<DecoderDefinitionFileDto?>.Failure(errorMessage);
        }
#else
    return Result<DecoderDefinitionFileDto?>.Failure("Not supported on this platform.");
#endif
    }

    #endregion
}
