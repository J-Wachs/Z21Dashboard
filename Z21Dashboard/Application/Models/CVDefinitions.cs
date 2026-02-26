using System.Text.Json.Serialization;
using Z21Client.Models;

namespace Z21Dashboard.Application.Models;

public class DccStandard
{
    [JsonPropertyName("meta")]
    public required Meta Meta { get; set; }

    [JsonPropertyName("cv_definitions")]
    public required List<CvDefinition> CvDefinitions { get; set; }
}

public class Meta
{
    [JsonPropertyName("standard")]
    public required string Standard { get; set; }

    [JsonPropertyName("document")]
    public required string Document { get; set; }

    [JsonPropertyName("scope")]
    public required string Scope { get; set; }

    [JsonPropertyName("languages")]
    public required List<string> Languages { get; set; }
}

public class CvDefinition
{
    [JsonPropertyName("cv")]
    public int? Cv { get; set; }

    [JsonPropertyName("cv_range")]
    public CvRange? Range { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("name")]
    public required Dictionary<string, string> Name { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    [JsonPropertyName("value_bits")]
    public CvValueBits? ValueBits { get; set; }

    [JsonPropertyName("flag_bits")]
    public List<CvFlagBit>? FlagBits { get; set; }

    [JsonPropertyName("structure")]
    public string? Structure { get; set; }

    [JsonPropertyName("table_length")]
    public int? TableLength { get; set; }

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
        string? value = null;
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
                    Dictionary<string, string>? bitValue = null;
                    if (flagBits.Values.TryGetValue(0, out bitValue))
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

    public class CvRange
    {
        [JsonPropertyName("from")]
        public required int From { get; set; }

        [JsonPropertyName("to")]
        public required int To { get; set; }
    }

    public class CvValueBits
    {
        [JsonPropertyName("min")]
        public int? Min { get; set; }

        [JsonPropertyName("max")]
        public int? Max { get; set; }

        [JsonPropertyName("from_bit")]
        public int? FromBit { get; set; }

        [JsonPropertyName("to_bit")]
        public int? ToBit { get; set; }
    }

    public class CvFlagBit
    {
        [JsonPropertyName("bit")]
        public required int Bit { get; set; }

        [JsonPropertyName("name")]
        public required Dictionary<string, string> Name { get; set; }

        [JsonPropertyName("values")]
        public Dictionary<int, Dictionary<string, string>>? Values { get; set; }
    }
}