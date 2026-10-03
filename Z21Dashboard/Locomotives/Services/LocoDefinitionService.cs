using Microsoft.Extensions.Logging;
using System.Text.Json;
using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.LocomotiveDecoders.Interfaces;
using Z21Dashboard.LocomotiveDecoders.Models;
using Z21Dashboard.Locomotives.Interfaces;
using Z21Dashboard.Locomotives.Models;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Ui.Interfaces;

namespace Z21Dashboard.Locomotives.Services;

/// <summary>
/// Service for the locomotive definitions files.
/// </summary>
/// <param name="dashboardStateService"></param>
/// <param name="decoderDefinitionService"></param>
/// <param name="logger"></param>
internal class LocoDefinitionService(
    IDashboardStateService dashboardStateService,
    IDecoderDefinitionService decoderDefinitionService,
    ILogger<LocoDefinitionService> logger
    ) : ILocoDefinitionService
{

    private List<LocomotiveListItemDto> _locomotivesList = [];
    private DateTime? _locomotivesLastRead;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    #region Exposed methods

    /// <inheritdoc />
    public async Task<Result> DeleteLocomotiveFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Result.Success();
        }
        else
        {
            // "Locomotive file not found"
            return Result.Failure(SharedResources.LocoFileNotFound);
        }
    }

    /// <inheritdoc />
    public async Task<Result> GetLocomotiveAndSetInfoIntoSettingsAsync(
        string locoDefFilePath,
        LocomotiveSettings locomotiveSettings
        )
    {
        try
        {
            var resultGetLocomotive = await GetLocomotiveAsync(locoDefFilePath);

            if (resultGetLocomotive.IsSuccess is false)
            {
                return Result.Failure(resultGetLocomotive.Messages);
            }
            var locomotive = resultGetLocomotive.Data!;

            // Prepare settings object
            locomotiveSettings.Clear();
            locomotiveSettings.SetLocomotive(locoDefFilePath);
            locomotiveSettings.State = LocomotiveState.Edit;

            if (locomotive.Meta is not null)
            {
                locomotiveSettings.LocomotiveMeta.Name = locomotive.Meta.Name;
                locomotiveSettings.LocomotiveMeta.ShortName = locomotive.Meta.ShortName;
                locomotiveSettings.LocomotiveMeta.Manufacturer = locomotive.Meta.Manufacturer;
                locomotiveSettings.LocomotiveMeta.CatalogueNumber = locomotive.Meta.CatalogueNumber;
                locomotiveSettings.LocomotiveMeta.Class = locomotive.Meta.Class;
                locomotiveSettings.LocomotiveMeta.UnitNumber = locomotive.Meta.UnitNumber;
                locomotiveSettings.LocomotiveMeta.Description = locomotive.Meta.Description;
                locomotiveSettings.LocomotiveMeta.Purchased = locomotive.Meta.Purchased;
                locomotiveSettings.LocomotiveMeta.PurchasePrice = locomotive.Meta.PurchasePrice;
                locomotiveSettings.LocomotiveMeta.Modifications = locomotive.Meta.Modifications;
                locomotiveSettings.LocomotiveMeta.Remarks = locomotive.Meta.Remarks;
            }

            if (locomotive.Decoder is not null and { DecoderFile: not null, DecoderFileLocation: not null })
            {
                // Read in the decoder file info into settings

                DecoderListItemDto decoder = new(
                    FileName: locomotive.Decoder.DecoderFile,
                    Name: string.Empty,
                    ManufacturerId: 0,
                    Manufacturer: string.Empty,
                    Location: locomotive.Decoder.DecoderFileLocation ?? DecoderFileLocation.Application
                    );

                var resultGetLocoDecoderDef = await decoderDefinitionService.GetLocoDecoderDefAndSetCVsIntoSettingsAsync(
                    decoder,
                    locomotiveSettings.DecoderSettings
                    );
                if (resultGetLocoDecoderDef.IsSuccess is false)
                {
                    return Result.Failure(resultGetLocoDecoderDef.Messages);
                }

                foreach (var cvValue in locomotive.Decoder.CvValues)
                {
                    // Look at metadata, if contains bits, get whole value
                    if (locomotiveSettings.DecoderSettings.CvMetadata.ContainsKey(cvValue.Key))
                    {
                        // Save CV, WholeValue
                        locomotiveSettings.DecoderSettings.SetCvWholeValue(cvValue.Key, cvValue.Value);
                    }
                }
            }
            locomotiveSettings.ClearChanged();

            return Result.Success();
        }
        catch (Exception ex)
        {
            // "Error reading locomotive  definition file '{0}': {1}"
            var errorMessage = string.Format(SharedResources.ErrorReadingLoco, locoDefFilePath, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LocomotiveListItemDto?>> GetLocomotiveByAddressAsync(ushort address)
    {
        var resultGetLocomotivesAsync = await GetLocomotivesAsync();
        if (resultGetLocomotivesAsync.IsFailure)
        {
            return Result<LocomotiveListItemDto?>.Failure(resultGetLocomotivesAsync.Messages);
        }

        var locomotive = resultGetLocomotivesAsync.Data?.FirstOrDefault(l => l.Address == address);
        // Here there is not need for a real message, so a dash will do.
        return locomotive is null ? Result<LocomotiveListItemDto?>.Failure("-") : Result<LocomotiveListItemDto?>.Success(locomotive!);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LocomotiveListItemDto>?>> GetLocomotivesAsync()
    {
        try
        {
            if (_locomotivesLastRead is null || DateTime.UtcNow - _locomotivesLastRead > TimeSpan.FromSeconds(5))
            {
                _locomotivesList.Clear();

                var settings = await dashboardStateService.GetSettingsAsync();
                if (string.IsNullOrEmpty(settings.LocalLocomotiveFolder))
                {
                    // "Locomotives folder not configured"
                    return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure(SharedResources.LocoFolderNotConfigured);
                }

                if (!Directory.Exists(settings.LocalLocomotiveFolder))
                {
                    // "Locomotives folder '{0}' not found"
                    return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure(string.Format(SharedResources.LocoFolderNotFound, settings.LocalLocomotiveFolder));
                }

                var jsonFiles = Directory.GetFiles(settings.LocalLocomotiveFolder, "*.json");

                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(filePath);

                        var locomotive = JsonSerializer.Deserialize<LocomotiveDefinitionDto>(jsonContent, _jsonOptions);

                        if (locomotive?.Meta is not null)
                        {
                            _locomotivesList.Add(new LocomotiveListItemDto
                            (
                                filePath,
                                (short)locomotive.Decoder.Address,
                                Path.GetFileName(filePath),
                                locomotive.Meta.Name ?? string.Empty,
                                locomotive.Meta.ShortName,
                                locomotive.Meta.Manufacturer,
                                locomotive.Meta.Class,
                                locomotive.Meta.UnitNumber
                            ));
                        }
                    }
                    catch (Exception ex)
                    {
                        // "Error loading locomotive from file '{0}': {1}"
                        var errorMessage = string.Format(SharedResources.ErrorGettingLocos, filePath, ex.Message);
                        logger.LogError(ex, errorMessage);
                        return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure(errorMessage);
                    }
                }

                _locomotivesLastRead = DateTime.UtcNow;

            }

            return Result<IReadOnlyList<LocomotiveListItemDto>?>.Success([.. _locomotivesList]);
        }
        catch (Exception ex)
        {
            // "Error loading locomotives from directory. The error is: {0}"
            var errorMessage = string.Format(SharedResources.ErrorGettingLocosFolder, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result<IReadOnlyList<LocomotiveListItemDto>?>.Failure(errorMessage);
        }
    }

    /// <inheritdoc/>
    public async Task<Result> SaveValuesToLocomotiveDefinitionFileAsync(
        string locoFileName,
        LocomotiveSettings locoSettings
        )
    {
        if (locoSettings.LocomotiveMeta is null)
        {
            return Result.Failure(SharedResources.LocoMetadataMissing);
        }

        // Validate required field
        if (string.IsNullOrWhiteSpace(locoSettings.LocomotiveMeta.Name))
        {
            return Result.Failure(SharedResources.LocoNameRequired);
        }

        if (string.IsNullOrWhiteSpace(locoSettings.LocomotiveMeta.ShortName))
        {
            return Result.Failure(SharedResources.LocoShortNameRequired);
        }
        // End validation of required fields

        var dashboardSettings = await GetSettingsAsync();
        if (string.IsNullOrEmpty(dashboardSettings.LocalLocomotiveFolder))
        {
            return Result.Failure(SharedResources.LocoFolderNotConfigured);
        }
        if (!Directory.Exists(dashboardSettings.LocalLocomotiveFolder))
        {
            // "Locomotives folder '{0}' not found"
            return Result.Failure(string.Format(SharedResources.LocoFolderNotFound, dashboardSettings.LocalLocomotiveFolder));
        }

        LocomotiveDefinitionDto locoDefinition = new()
        {
            Meta = new()
            {
                Name = locoSettings.LocomotiveMeta.Name,
                ShortName = locoSettings.LocomotiveMeta.ShortName,
                Manufacturer = locoSettings.LocomotiveMeta.Manufacturer,
                CatalogueNumber = locoSettings.LocomotiveMeta.CatalogueNumber,
                Class = locoSettings.LocomotiveMeta.Class,
                UnitNumber = locoSettings.LocomotiveMeta.UnitNumber,
                Description = locoSettings.LocomotiveMeta.Description,
                Purchased = locoSettings.LocomotiveMeta.Purchased,
                PurchasePrice = locoSettings.LocomotiveMeta.PurchasePrice,
                Modifications = locoSettings.LocomotiveMeta.Modifications,
                Remarks = locoSettings.LocomotiveMeta.Remarks
            },
            Decoder = new()
            {
                Address = (ushort)(locoSettings.DecoderSettings.GetLocoAddress() ?? 0),
                DecoderFile = locoSettings.DecoderSettings.DecoderFile,
                DecoderFileLocation = locoSettings.DecoderSettings.DecoderFileLocation,
                CvValues = []
            }
        };

        foreach (var cvValue in locoSettings.DecoderSettings.CvValues.OrderBy(rec => rec.Key))
        {
            // Look at metadata, if contains bits, get old value
            if (locoSettings.DecoderSettings.CvMetadata.TryGetValue(cvValue.Key, out var metadata))
            {
                // Save CV, WholeValue
                locoDefinition.Decoder.CvValues[cvValue.Key] = cvValue.Value.GetWholeValue() ?? 0;
            }
        }

        var locoFilePath = Path.Combine(dashboardSettings.LocalLocomotiveFolder, locoFileName);
        var resultSaveLocomotive = await SaveLocomotiveAsync(locoFilePath, locoDefinition);
        if (resultSaveLocomotive.IsFailure)
        {
            return Result.Failure(resultSaveLocomotive.Messages);
        }

        locoSettings.ClearChanged();

        return Result.Success();
    }

    #endregion Exposed methods

    /// <summary>
    /// Gets the locomotive definition file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private async Task<Result<LocomotiveDefinitionDto?>> GetLocomotiveAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                // "Locomotive definition file '{0}' is not found"
                var errorMessage = string.Format(SharedResources.LocoFileNotFoundPath, filePath);
                logger.LogError(errorMessage);
                return Result<LocomotiveDefinitionDto?>.Failure(errorMessage);
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var locomotive = JsonSerializer.Deserialize<LocomotiveDefinitionDto>(jsonContent, _jsonOptions);

            return Result<LocomotiveDefinitionDto?>.Success(locomotive);
        }
        catch (Exception ex)
        {
            // "Error getting and deserializing locomotive from file {0}. The error is: {1}"
            var errorMessage = string.Format(SharedResources.ErrorGetDesLocoFile, filePath, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result<LocomotiveDefinitionDto?>.Failure(errorMessage);
        }
    }

    /// <summary>
    /// The actual sace of the object to a JSON file.
    /// </summary>
    /// <param name="locoPathFileName"></param>
    /// <param name="locoDefinition"></param>
    /// <returns></returns>
    private async Task<Result> SaveLocomotiveAsync(
        string locoPathFileName,
        LocomotiveDefinitionDto locoDefinition
        )
    {
        try
        {
            string jsonContent = JsonSerializer.Serialize(locoDefinition, _jsonOptions);
            await File.WriteAllTextAsync(locoPathFileName, jsonContent);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // "Error saving locomotive definition file '{0}'. The error is: {1}"
            var errorMessage = string.Format(SharedResources.ErrorSavingLocoDef, locoPathFileName, ex.Message);
            logger.LogError(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Gets the Dashboard settings.
    /// </summary>
    /// <returns></returns>
    private async Task<DashboardSettings> GetSettingsAsync()
    {
        var dashboardSettings = await dashboardStateService.GetSettingsAsync();
        return dashboardSettings;
    }
}
