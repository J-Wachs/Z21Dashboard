using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Interfaces;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.NmraDccStandard.Interfaces;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Ui;
using Z21Dashboard.Ui.Interfaces;

namespace Z21Dashboard.LocomotiveDecoders.Services;

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
        AllowTrailingCommas = true,
        WriteIndented = true
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
            return Result<IReadOnlyList<DecoderListItemDto>?>.Failure(resultLoadFromLocalFileSys.Messages);
        }
        var localDecoders = resultLoadFromLocalFileSys.Data;
        decoderList.AddRange(localDecoders);

        return Result<IReadOnlyList<DecoderListItemDto>?>.Success([.. decoderList]);
    }

    /// <inheritdoc />
    public async Task<DecoderFileRootDto?> GetMetaInfoFromLocalLocoDecoderFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        return JsonSerializer.Deserialize<DecoderFileRootDto>(content, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task<Result> SaveValuesToUserDefinedLocoDecoderAsync(
        DecoderListItemDto decoder,
        DecoderSettings decoderSettings
        )
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
                var foundCv = decoderDefinition.CvDefinitions.FirstOrDefault(x => x is CvSingleDto && (x as CvSingleDto)?.Cv == cvValue.Key);
                if (foundCv is CvSingleDto cvSingleObject)
                {
                    // If all bits are set, then just write the value if it was changed
                    if (cvValue.Value.AnyBitNotSet() is false)
                    {
                        // Save CV, WholeValue
                        cvSingleObject.DefaultValue = cvValue.Value.GetWholeValue();
                    }
                    else
                    {
                        // Some bits are set, others not. We need to read the old value,
                        // patch the bits and then write back the whole value (only if changed)
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

        // Store metadata info:
        decoderDefinition.Meta.Model = decoderSettings.DecoderModel ?? string.Empty;
        decoderDefinition.Meta.Manufacturer = decoderSettings.DecoderMfg ?? string.Empty;
        decoderDefinition.Meta.ManufacturerId = (ushort?)decoderSettings.DecoderMfgId ?? 0;
        decoderDefinition.Meta.Version = decoderSettings.DecoderDefVersion ?? string.Empty;
        decoderDefinition.Meta.Protocols = decoderSettings.DecoderProtocols ?? string.Empty;
        decoderDefinition.Meta.Sound = decoderSettings.DecoderSound ?? false;

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
    public async Task<Result> GetLocoDecoderDefAndSetCVsIntoSettingsAsync(
        DecoderListItemDto decoder,
        DecoderSettings decoderSettings
        )
    {
        // 0. Prepare settings object
        decoderSettings.Clear();
        if (decoder is null)
        {
            return Result.Success();
        }

        Dictionary<ushort, short> finalCvValues = [];
        DecoderDefinitionFileDto? specificDecoder = null;

        try
        {
            // 1. Load the specific decoder file
            var resultGetDecoderDefinition = await GetDecoderDefinitionAsync(decoder.FileName, decoder.Location);
            if (resultGetDecoderDefinition.IsSuccess is false || resultGetDecoderDefinition.Data is null)
            {
                return Result.Failure(resultGetDecoderDefinition.Messages);
            }

            decoderSettings.SetDecoder(
                decoder.FileName,
                decoder.Location,
                resultGetDecoderDefinition.Data.Meta.Model,
                resultGetDecoderDefinition.Data.Meta.Manufacturer,
                resultGetDecoderDefinition.Data.Meta.ManufacturerId,
                resultGetDecoderDefinition.Data.Meta.Version,
                resultGetDecoderDefinition.Data.Meta.Protocols,
                resultGetDecoderDefinition.Data.Meta.Sound
                );

            specificDecoder = resultGetDecoderDefinition.Data;

            if (specificDecoder?.CvDefinitions is null)
            {
                // "Decoder file {0} from {1} has no CV definitions"
                var errorMessage = string.Format(SharedResources.DecoderFileNoCvDefinitions, decoder.FileName, decoder.Location);
                return Result.Failure(errorMessage);
            }

            // 2.1. We cannot go-on if no pages:
            if (specificDecoder.UiPages is null)
            {
                // "The selected decoder '{0}', file '{1}' from manufacturer '{2}', does not have any pages defined. Pages are needed"
                return Result.Failure(string.Format(SharedResources.NoPagesInDecoderFiles,
                                                    decoderSettings.DecoderModel,
                                                    decoderSettings.DecoderFile,
                                                    decoderSettings.DecoderMfg));
            }

            // 3. Identify ALL unique CV numbers involved (Specific + NMRA)
            HashSet<(ushort cv, string source)> allCvNumbers = [];

            // From Specific
            foreach (var def in specificDecoder.CvDefinitions)
                foreach (var cv in ExtractCvNumbers(def))
                    allCvNumbers.Add((cv, def.Source));

            // 4. Iterate all CVs to populate Values AND Metadata
            foreach (var cv in allCvNumbers.OrderBy(x => x.cv))
            {
                CvDefinitionItemDto? specificDef = null;
                // Find definition objects (can be null)
                specificDef = FindDefinitionForCv(
                    specificDecoder.CvDefinitions,
                    cv.cv
                    );
                if (specificDef is null)
                {
                    return Result.Failure(SharedResources.ErrorReadingDecoderFile);
                }

                // B. Build Metadata (Specific -> NMRA -> Service)
                var resultBuildCvMetadata = await BuildCvMetadataAsync(cv, specificDef);
                if (resultBuildCvMetadata.IsFailure)
                {
                    return Result.Failure(resultBuildCvMetadata.Messages);
                }
                var metadata = resultBuildCvMetadata.Data;
                if (metadata is not null)
                {
                    decoderSettings.CvMetadata[cv.cv] = metadata;
                }

                // A. Set Value (Specific wins over NMRA)
                byte? val;
                if (specificDef is CvSingleDto cvSingle)
                {
                    val = cvSingle.DefaultValue ?? metadata?.DefaultValue;
                }
                else
                {
                    val = metadata?.DefaultValue;
                }
               

                // This updates CvValues in settings (and handles bits)
                decoderSettings.SetCvWholeValue(cv.cv, val);
            }

            // 5. Write UI Pages to settings
            foreach (var pageDto in specificDecoder.UiPages)
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
    private async Task<Result> SaveDecoderDefinitionAsync(
        DecoderListItemDto decoder,
        DecoderDefinitionFileDto decoderDefinition
        )
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
    private async Task<Result<CvInformation>> BuildCvMetadataAsync(
        (ushort cv, string source) cv,
        CvDefinitionItemDto? specificDef
        )
    {
        var info = new CvInformation { Cv = cv.cv };

        if (cv.source.Equals("nmra", StringComparison.CurrentCultureIgnoreCase))
        {
            var resultGetRawCvDefinition = await nmraCvService.GetRawCvDefinitionAsync(cv.cv);
            if (resultGetRawCvDefinition.IsFailure)
            {
                return Results.Failure<CvInformation>(resultGetRawCvDefinition.Messages);
            }
            var cvDef = resultGetRawCvDefinition.Data;

            // 1: Name
            string? name = GetLocalizedText(cvDef!.Name);
            info.Name = name ?? string.Empty;

            // 2: Description:
            string? desc = GetLocalizedText(cvDef!.Description);
            info.Description = desc;

            info.ReadOnly = cvDef.ReadOnly;

            // 3: BIT DETAILS (Flag Bits)
            // We iterate over ALL bits (0 to 7) in order to ensure that we catch bits
            // that only exists in the service, eventhough the JSON-files are empty ub the flag_bits
            // section.
            if (cvDef.FlagBits is not null)
            {
                foreach (var entry in cvDef.FlagBits)
                {
                    var bitInfo = new CvBitInformation()
                    {
                        Name = GetLocalizedText(entry.Name) ?? string.Empty,
                        Values = []
                    };

                    if (entry.Values is not null)
                    {
                        foreach (var value in entry.Values)
                        {
                            bitInfo.Values.Add(value.Key, GetLocalizedText(value.Value) ?? string.Empty);
                        }
                    }

                    byte? bitTo = entry.BitTo == entry.Bit ? null : entry.BitTo;
                    info.BitDetails[(entry.Bit, bitTo)] = bitInfo;
                }
            }

            if (resultGetRawCvDefinition.Data?.ValueBits is not null)
            {
                info.ValueBits = resultGetRawCvDefinition.Data?.ValueBits!;
                if (info.ValueBits.FromBit is null &&
                    info.ValueBits.ToBit is null &&
                    resultGetRawCvDefinition.Data?.FlagBits is null)
                {
                    info.ValueBits.FromBit = 0;
                    info.ValueBits.ToBit = 7;
                }
            }
            else if (resultGetRawCvDefinition.Data?.FlagBits is null)
            {
                info.ValueBits = new()
                {
                    Min = 0,
                    Max = 255,
                    FromBit = 0,
                    ToBit = 7
                };
            }

            info.DefaultValue = resultGetRawCvDefinition.Data?.DefaultValue;
        }
        else if (cv.source.Equals("custom", StringComparison.CurrentCultureIgnoreCase))
        {
            // 1: Name
            string? name = GetLocalizedText(specificDef?.Name);
            info.Name = name ?? string.Empty;

            // 2: Description:
            string? desc = GetLocalizedText(specificDef?.Description);
            info.Description = desc;

            info.ReadOnly = specificDef?.ReadOnly;

            // 3. BIT DETAILS (Flag Bits)
            // We iterate over ALL bits (0 to 7) in order to ensure that we catch bits
            // that only exists in the service, eventhough the JSON-files are empty ub the flag_bits
            // section.
            if (specificDef is CvSingleDto cs)
            {
                if (cs.FlagBits is not null)
                {
                    foreach (var entry in cs.FlagBits)
                    {
                        var bitInfo = new CvBitInformation()
                        {
                            Name = GetLocalizedText(entry.Name) ?? string.Empty,
                            Values = []
                        };

                        if (entry.Values is not null)
                        {
                            foreach (var value in entry.Values)
                            {
                                bitInfo.Values.Add(value.Key, GetLocalizedText(value.Value) ?? string.Empty);
                            }
                        }

                        byte? bitTo = entry.BitTo == entry.Bit ? null : entry.BitTo;
                        info.BitDetails[(entry.Bit, bitTo)] = bitInfo;
                    }
                }

                if (cs.ValueBits is not null)
                {
                    info.ValueBits = cs.ValueBits;
                }
                else if (cs.FlagBits is null)
                {
                    info.ValueBits = new()
                    {
                        Min = 0,
                        Max = 255,
                        FromBit = 0,
                        ToBit = 7
                    };
                }

                if (cs.FlagBits is not null)
                {
                    List<CvFlagBitDto> flagBits = [];
                    foreach (var oneBitSet in cs.FlagBits)
                    {
                        CvFlagBitDto newBitSet = new()
                        {
                            Bit = oneBitSet.Bit,
                            BitTo = oneBitSet.BitTo,
                            Name = oneBitSet.Name ?? [],
                            Values = []
                        };

                        foreach (var valEntry in oneBitSet.Values ?? [])
                        {
                            newBitSet.Values.Add(valEntry.Key, valEntry.Value);
                        }

                        flagBits.Add(newBitSet);
                    }
                }
                info.DefaultValue = cs?.DefaultValue;
            }
        }

        return Results.Success(info);
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
    private static CvDefinitionItemDto? FindDefinitionForCv(
        List<CvDefinitionItemDto> list,
        ushort cv
        )
    {
        foreach (var oneDefinition in list)
        {
            switch (oneDefinition)
            {
                case CvSingleDto cs:
                    if (cs.Cv == cv)
                    {
                        return oneDefinition;
                    }
                    break;
                case CvRangeDto cr:
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
            case CvSingleDto cs:
                yield return cs.Cv;
                break;
            case CvRangeDto cr:
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
                        item.ManufacturerId,
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
            logger.LogCritical(ex, errorMessage);
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
                    DecoderFileRootDto? decoderFile = await GetMetaInfoFromLocalLocoDecoderFileAsync(filePath);
                    if (decoderFile?.Meta is not null &&
                        !string.IsNullOrWhiteSpace(decoderFile.Meta.Manufacturer) &&
                        !string.IsNullOrWhiteSpace(decoderFile.Meta.Model))
                    {
                        results.Add(new(
                            Path.GetFileName(filePath),
                            decoderFile.Meta.Model,
                            decoderFile.Meta.ManufacturerId,
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
    private async Task<Result<DecoderDefinitionFileDto>> GetDecoderDefinitionAsync(
        string fileName,
        DecoderFileLocation location
        )
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

    #endregion
}
