using Microsoft.Extensions.Logging;
using System.Text.Json;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Services;

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

    private DccStandard? _dccStandard = null;

    /// <inheritdoc />
    public async Task<string> GetCVBitName(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
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
                    return nameOfBit;
                }
            }
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public async Task<string> GetCVName(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        string name = string.Empty;

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range  is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );

        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();
            name = cvDef.GetName(language);
        }

        return name;
    }

    /// <inheritdoc />
    public async Task<string> GetCVDescription(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        string description = string.Empty;

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );

        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();
            description = cvDef.GetDescription(language);
        }

        return description;
    }

    /// <inheritdoc />
    public async Task<string> GetCVBitDescription(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
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

        return description;
    }

    /// <inheritdoc />
    public async Task<(string ZeroDescription, string OneDescription)> GetCVBitValuesDescription(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
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
                        return (zeroDescription, oneDescription);
                    }
                }
            }
        }

        return (string.Empty, string.Empty);
    }

    /// <inheritdoc />
    public async Task<(string Name, string Description)> GetCVNameAndDescription(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.CvStart <= cvNumber && x.Range.CvEnd >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = languageSupportService.GetLanguageCode();

            var result = cvDef.GetNameAndDescription(language);
            return result;
        }

        return (string.Empty, string.Empty);
    }

    public async Task<CvDefinition?> GetRawCvDefinition(ushort cv)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cv ||
            x.Range is not null && x.Range.CvStart <= cv && x.Range.CvEnd >= cv
            );
        return cvDef;
    }

    /// <summary>
    /// Asynchronously retrieves the DCC standard configuration values from the application's package file.
    /// </summary>
    /// <remarks>The configuration is read from the "NmraCvs.json" file included in the application
    /// package.</remarks>
    /// <returns>A <see cref="DccStandard"/> object containing the configuration values if the file is found and successfully
    /// deserialized.</returns>
    private async Task GetListOfCVsAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("nmra_9_2_2_locomotive_base.json");
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync();

        try
        {
            // Deserialize
            _dccStandard = JsonSerializer.Deserialize<DccStandard>(json, options);
        }
        catch (Exception ex)
        {
            logger.LogCritical("An error has occurred when loading NMRA CV standard: '{ex.Message}'.", ex.Message);
        }
    }
}
