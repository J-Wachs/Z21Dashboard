using System.Globalization;
using System.Text.Json;
using Z21Client.Models;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Services;

/// <summary>
/// Service to read and return description of all the CV defined by NRMA.
/// </summary>
public class NmraCvService : INmraCvService
{
    private readonly JsonSerializerOptions options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true
    };

    private DccStandard? _dccStandard = null;

    public async Task<string> GetCVBitName(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.From <= cvNumber && x.Range.To >= cvNumber
            );

        if (cvDef is not null && cvDef.FlagBits is not null)
        {
            var flagBits = cvDef.FlagBits.FirstOrDefault(x => x.Bit == (int)bit);
            if (flagBits is not null)
            {
                string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (language is not "da" and not "en")
                {
                    language = "en";
                }

                string fullNameOfBit = string.Empty;
                if (flagBits.Name.TryGetValue(language, out var nameOfBit))
                {
                    return nameOfBit;
                }
            }
        }

        return string.Empty;
    }

    public async Task<string> GetCVDescription(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        string description = string.Empty;

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.From <= cvNumber && x.Range.To >= cvNumber
            );

        if (cvDef is not null)
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (language is not "da" and not "en")
            {
                language = "en";
            }

            description = cvDef.GetDescription(language);
        }

        return description;
    }


    public async Task<string> GetCVBitDescription(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        string description = string.Empty;

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.From <= cvNumber && x.Range.To >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (language is not "da" and not "en")
            {
                language = "en";
            }

            description = cvDef.GetBitDescription((int)bit, language);
        }

        return description;
    }


    public async Task<(string ZeroDescription, string OneDescription)> GetCVBitValuesDescription(ushort cvNumber, Bits bit)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.From <= cvNumber && x.Range.To >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (language is not "da" and not "en")
            {
                language = "en";
            }

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
                        Dictionary<string, string>? bitValue = null;
                        if (flagBits.Values.TryGetValue(0, out bitValue))
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


    public async Task<(string Name, string Description)> GetCVNameAndDescription(ushort cvNumber)
    {
        if (_dccStandard is null)
        {
            await GetListOfCVsAsync();
        }

        var cvDef = _dccStandard?.CvDefinitions.FirstOrDefault(
            x => x.Cv is not null && x.Cv == cvNumber ||
            x.Range is not null && x.Range.From <= cvNumber && x.Range.To >= cvNumber
            );
        if (cvDef is not null)
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (language is not "da" and not "en")
            {
                language = "en";
            }

            var result = cvDef.GetNameAndDescription(language);
            return result;
        }

        return (string.Empty, string.Empty);
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
        using var stream = FileSystem.OpenAppPackageFileAsync("nmra_9_2_2_locomotive_base.json").GetAwaiter().GetResult();
        using var reader = new StreamReader(stream);

        var json = reader.ReadToEnd();

        // Deserialize
        _dccStandard = JsonSerializer.Deserialize<DccStandard>(json, options);
    }
}
