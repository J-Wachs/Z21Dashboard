using System.Text.Json.Serialization;
using Z21Client.Models;

namespace Z21Dashboard.NmraDccStandard.Models;

/// <summary>
/// Definitions of the CVs as per the NMRA standard.
/// </summary>
public class NmraDccStandardCvDefinitionDto
{
    [JsonPropertyName("cv")]
    public int? Cv { get; set; }

    [JsonPropertyName("cv_range")]
    public NmraDccStandardCvRangeDto? Range { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("name")]
    public required Dictionary<string, string> Name { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    [JsonPropertyName("value_bits")]
    public NmraDccStandardCvValueBitsDto? ValueBits { get; set; }

    [JsonPropertyName("flag_bits")]
    public List<NmraDccStandardCvFlagBitDto>? FlagBits { get; set; }

    [JsonPropertyName("default_value")]
    public byte? DefaultValue { get; set; }

    [JsonPropertyName("readonly")]
    public bool? ReadOnly { get; set; } = false;

    /// <summary>
    /// Helper method to safely retrieve the name in a specific language.
    /// </summary>
    public string GetName(string langCode)
    {
        return Name.TryGetValue(langCode, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Helper method to safely retrieve the description in a specific language.
    /// </summary>
    public string GetDescription(string langCode)
    {
        string? value;
        if (Description is null)
        {
            return Name.TryGetValue(langCode, out value) ? value : string.Empty;
        }

        return Description.TryGetValue(langCode, out value) ? value : string.Empty;
    }

    /// <summary>
    /// Helper method to safely retrieve the name and description in a specific language.
    /// </summary>
    public (string Name, string Description) GetNameAndDescription(string langCode)
    {
        if (Name.TryGetValue(langCode, out var name) is false)
        {
            name = string.Empty;
        }

        if (Description is null || Description.TryGetValue(langCode, out var description) is false)
        {
            description = string.Empty;
        }

        return (name, description);
    }

    /// <summary>
    /// Helper method to safely retrieve a specific bit description.
    /// </summary>
    /// <param name="bit">The bit number (0-7).</param>
    /// <param name="langCode">The language code (e.g., "da").</param>
    /// <returns>The description string or fallback.</returns>
    public string GetBitDescription(int bit, string langCode)
    {
        if (FlagBits is null)
        {
            return string.Empty;
        }

        var flagBits = FlagBits.FirstOrDefault(x => x.Bit == bit);
        if (flagBits is not null)
        {
            string fullNameOfBit = string.Empty;
            if (flagBits.Name.TryGetValue(langCode, out var nameOfBit))
            {
                fullNameOfBit = nameOfBit;
                if (flagBits.Values is not null)
                {
                    bool hasSetSeperator = false;
                    string? description = null;
                    if (flagBits.Values.TryGetValue(0, out Dictionary<string, string>? bitValue))
                    {
                        if (bitValue.TryGetValue(langCode, out description))
                        {
                            fullNameOfBit += ": 0=" + description;
                            hasSetSeperator = true;
                        }
                    }
                    if (flagBits.Values.TryGetValue(1, out bitValue))
                    {
                        if (bitValue.TryGetValue(langCode, out description))
                        {
                            fullNameOfBit += (hasSetSeperator ? ", 1=" : ": 1=") + description;
                        }
                    }
                }
            }

            return fullNameOfBit;
        }

        return string.Empty;
    }

    /// <summary>
    /// Helper method to safely retrieve a specific bit name.
    /// </summary>
    /// <param name="bit"></param>
    /// <param name="langCode"></param>
    /// <returns></returns>
    public string GetBitName(Bits bit, string langCode)
    {
        if (FlagBits is not null)
        {
            var flagBits = FlagBits.FirstOrDefault(x => x.Bit == (int)bit);
            if (flagBits is not null)
            {
                string fullNameOfBit = string.Empty;
                if (flagBits.Name.TryGetValue(langCode, out var nameOfBit))
                {
                    return nameOfBit;
                }
            }
        }

        return string.Empty;
    }
}
