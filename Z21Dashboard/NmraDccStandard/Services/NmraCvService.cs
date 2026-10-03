using Microsoft.Extensions.Logging;
using System.Text.Json;
using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.NmraDccStandard.Interfaces;
using Z21Dashboard.NmraDccStandard.Models;
using Z21Dashboard.Resources.Localization;
using Z21Dashboard.Ui.Interfaces;

namespace Z21Dashboard.NmraDccStandard.Services;

/// <summary>
/// Service to read and return description of all the CV defined by NRMA.
/// </summary>
internal class NmraCvService(
    ILogger<NmraCvService> logger,
    ILanguageSupportService languageSupportService
    ) : INmraCvService
{
    private readonly JsonSerializerOptions options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true
    };

    private NmraDccStandardDto? _dccStandard = null;

    #region Exposed methods

    /// <inheritdoc />
    public async Task<Result<string>> GetCVBitNameAsync(
        ushort cvNumber,
        Bits bit
        )
    {
        if (_dccStandard is null)
        {
            var result = await GetListOfCVsAsync();
            if (result.IsFailure)
            {
                return Result<string>.Failure(result.Messages);
            }
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );

        if (cvDef is not null && cvDef.FlagBits is not null)
        {
            var flagBits = cvDef.FlagBits.FirstOrDefault(x => x.Bit == (int)bit);
            if (flagBits is not null)
            {
                string language = languageSupportService.GetLanguageCode();

                string fullNameOfBit = string.Empty;
                if (flagBits.Name.TryGetValue(language, out var nameOfBit))
                {
                    return Results.Success(nameOfBit);
                }
            }
        }

        return Results.Success(string.Empty);
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetCVBitDescriptionAsync(
        ushort cvNumber,
        Bits bit
        )
    {
        if (_dccStandard is null)
        {
            var result = await GetListOfCVsAsync();
            if (result.IsFailure)
            {
                return Result<string>.Failure(result.Messages);
            }
        }

        string description = string.Empty;

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();

            description = cvDef.GetBitDescription((int)bit, language);
        }

        return Results.Success(description);
    }

    /// <inheritdoc />
    public async Task<Result<(string ZeroDescription, string OneDescription)>> GetCVBitValuesDescriptionAsync(
        ushort cvNumber,
        Bits bit
        )
    {
        if (_dccStandard is null)
        {
            var result = await GetListOfCVsAsync();
            if (result.IsFailure)
            {
                return Result<(string, string)>.Failure(result.Messages);
            }
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();

            if (cvDef.FlagBits is not null)
            {
                var flagBits = cvDef.FlagBits.FirstOrDefault(x => x.Bit == (int)bit);
                if (flagBits is not null)
                {
                    if (flagBits.Values is not null)
                    {
                        // We must return string and not string? Hence the fields are set here,
                        // and further down, they are assigned empty strings, as TryGetValue
                        // returns null.
                        string? zeroDescription = string.Empty;
                        string? oneDescription = string.Empty;
                        if (flagBits.Values.TryGetValue(0, out Dictionary<string, string>? bitValue))
                        {
                            if (bitValue.TryGetValue(language, out zeroDescription) is false)
                            {
                                zeroDescription = string.Empty;
                            }
                        }
                        if (flagBits.Values.TryGetValue(1, out bitValue))
                        {
                            if (bitValue.TryGetValue(language, out oneDescription) is false)
                            {
                                oneDescription = string.Empty;
                            }
                        }
                        return Results.Success((zeroDescription, oneDescription));
                    }
                }
            }
        }

        return Results.Success((string.Empty, string.Empty));
    }

    /// <inheritdoc />
    public async Task<Result<(string Name, string Description)>> GetCVNameAndDescriptionAsync(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            var result = await GetListOfCVsAsync();
            if (result.IsFailure)
            {
                return Result<(string, string)>.Failure(result.Messages);
            }
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();

            var nameDescription = cvDef.GetNameAndDescription(language);
            return Results.Success(nameDescription);
        }

        return Results.Success((string.Empty, string.Empty));
    }

    /// <inheritdoc />
    public async Task<Result<NmraDccStandardCvDefinitionDto>> GetRawCvDefinitionAsync(ushort cv)
    {
        if (_dccStandard is null)
        {
            var result = await GetListOfCVsAsync();
            if (result.IsFailure)
            {
                return Result<NmraDccStandardCvDefinitionDto>.Failure(result.Messages);
            }
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cv ||
            x.Range is not null && x.Range.CvStart <= cv && x.Range.CvEnd >= cv
            );
        
        if (cvDef is null)
        {
            return Result<NmraDccStandardCvDefinitionDto>.FailureNotFound(SharedResources.CVNotFoundInNmra);
        }

        return Results.Success(cvDef);
    }

    #endregion Exposed methods

    #region CV helpers

    /// <summary>
    /// Asynchronously retrieves the DCC standard configuration values from the application's package file.
    /// </summary>
    /// <remarks>The configuration is read from the "NmraCvs.json" file included in the application
    /// package.</remarks>
    /// <returns>A <see cref="DccStandard"/> object containing the configuration values if the file is found and successfully
    /// deserialized.</returns>
    private async Task<Result> GetListOfCVsAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("nmra_9_2_2_locomotive_base.json");
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync();

        try
        {
            // Deserialize
            _dccStandard = JsonSerializer.Deserialize<NmraDccStandardDto>(json, options);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // "An error has occurred when loading NMRA CV standard: '{ex.Message}'"
            var errorMessage = string.Format(SharedResources.ErrorLoadNMRAStd, ex.Message);
            logger.LogCritical(ex, errorMessage);
            return Result.Failure(errorMessage);
        }
    }

    #endregion CV helpers
}
