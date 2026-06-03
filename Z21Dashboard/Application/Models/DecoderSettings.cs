using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.Infrastructure;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents the runtime settings for a specific decoder, including loaded CV values and UI layout.
/// </summary>
public class DecoderSettings
{
    public string? DecoderFile { get; private set; }
    public DecoderFileLocation? DecoderFileLocation { get; private set; }
    public string? DecoderName { get; private set; }
    public string? DecoderMfg { get; private set; }

    // Dictionary storing CV values as smart objects (handling both byte and bit access)
    // Assumes CvState is defined in the same namespace
    public Dictionary<ushort, CvState> CvValues { get; set; } = [];
    // Dictionary storing metadata (Names, Descriptions, Bit labels) for CVs.
    // Key = CV Number.
    public Dictionary<ushort, CvInformation> CvMetadata { get; set; } = [];

    // UI Pages loaded from the JSON definition
    public List<DecoderPage> Pages { get; set; } = [];

    #region CV Handling Methods

    /// <summary>
    /// The method calles the IsChanged property of the CvState objects to determine
    /// if any CV value has been modified since it was loaded or since the last
    /// ClearChanged() call.
    /// </summary>
    /// <returns></returns>
    public bool IsChanged()
    {
        return CvValues.Values.Any(x => x.IsChanged);
    }

    /// <summary>
    /// Sets a specific bit in a CV.
    /// </summary>
    public Result SetCvBitValue(ushort cv, Bits bit, bool? value)
    {
        if (!CvValues.ContainsKey(cv))
        {
            var cvMetadata = CvMetadata[cv];
            if (cvMetadata is null)
            {
                // "Information about cv-address {0} was not found. Operation aborted"
                return Result.Failure(string.Format(SharedResources.CvMetadataNotFound, cv));
            }

            if (cvMetadata.ValueBits is null)
            {
                // "Information about ValueBits are missing for cv {0}"
                return Result.Failure(string.Format(SharedResources.CvMetadataValueBitsMissing, cv));
            }

            CvValues[cv] = new CvState(cvMetadata);
        }

        return CvValues[cv].SetBit(bit, value);
    }

    /// <summary>
    /// Sets a byte value for a CV and synchronizes the individual bits.
    /// </summary>
    public Result SetCvValue(ushort cv, byte? value)
    {
        if (!CvValues.ContainsKey(cv))
        {
            var cvMetadata = CvMetadata[cv];
            if (cvMetadata is null)
            {
                // "Information about cv-address {0} was not found. Operation aborted"
                return Result.Failure(string.Format(SharedResources.CvMetadataNotFound, cv));
            }

            if (cvMetadata.ValueBits is null)
            {
                // "Information about ValueBits are missing for cv {0}"
                return Result.Failure(string.Format(SharedResources.CvMetadataValueBitsMissing, cv));
            }

            if (value is not null && (value < cvMetadata.ValueBits.Min || value > cvMetadata.ValueBits.Max))
            {
                // "The value '{0}' of cv {1} is outside the valid range '{2}' to '{3}'"
                return Result.Failure(string.Format(SharedResources.CvValueOutsideRange, value, cv, cvMetadata.ValueBits.Min, cvMetadata.ValueBits.Max));
            }

            CvValues[cv] = new CvState(cvMetadata);
        }

        CvValues[cv].SetValue(value);
        return Result.Success();
    }

    /// <summary>
    /// Sets the whole byte to the given value and synchronizes the individual bits.
    /// This method is used to set the value for the programming settings, when the 
    /// value is read from the decoder.
    /// </summary>
    /// <param name="cv"></param>
    /// <param name="wholeValue"></param>
    /// <returns></returns>
    public Result SetCvWholeValue(ushort cv, byte? wholeValue)
    {
        if (CvValues.ContainsKey(cv) is false)
        {
            var cvMetadata = CvMetadata[cv];
            if (cvMetadata is null)
            {
                // "Information about cv-address {0} was not found. Operation aborted"
                return Result.Failure(string.Format(SharedResources.CvMetadataNotFound, cv));
            }

            CvValues[cv] = new(cvMetadata);
        }
        CvValues[cv].SetWholeValue(wholeValue);

        return Result.Success();
    }

    /// <summary>
    /// Gets the value of a specific bit in a CV.
    /// </summary>
    /// <param name="cv"></param>
    /// <param name="bit"></param>
    /// <returns></returns>
    public bool? GetCvBit(ushort cv, Bits bit)
    {
        return CvValues[cv].GetBit(bit);
    }

    /// <summary>
    /// Returns the value of the cv's value bits (e.g. CV 10).
    /// That is, the method returns the value that represent the cv
    /// value. This is important for certain cvs that has e.g. bit 0
    /// ti 6 as a value and bit 7 is a flag (e.g. CV 19 where bits 0
    /// to 6 is the address and bit 7 is the direction).
    /// </summary>
    /// <param name="cv"></param>
    /// <returns></returns>
    public byte? GetCvValue(ushort cv)
    {
        return CvValues[cv].GetValue();
    }

    /// <summary>
    /// Gets the byte value of a CV. Returns null if not set.
    /// </summary>
    public byte? GetCvWholeValue(ushort cv)
    {
        if (CvValues.TryGetValue(cv, out var cvState))
        {
            return cvState?.GetWholeValue();
        }
        return null;
    }

    #endregion

    #region Support Methods

    /// <summary>
    /// Returns the address of the decoder, based on the CV 29 bit 5 (Short/long address).
    /// </summary>
    /// <returns></returns>
    public short? GetLocoAddress()
    {
        if (CvValues.ContainsKey(Cvs.ConfigurationData1) is false)
        {
            return null;
        }

        var useLongAddress = GetCvBit(Cvs.ConfigurationData1, Bits.Five);

        if (useLongAddress is false && CvValues.ContainsKey(Cvs.ShortAddress) is false)
        {
            return null;
        }

        if (useLongAddress is true && (CvValues.ContainsKey(Cvs.LongAddressHighByte) is false || CvValues.ContainsKey(Cvs.LongAddressLowByte) is false))
        {
            return null;
        }

        if (useLongAddress is false)
        {
            // 2 digit address
            return GetCvValue(Cvs.ShortAddress);
        }
        else
        {
            // 4 digit address
            var highByte = GetCvValue(Cvs.LongAddressHighByte);
            var lowByte = GetCvValue(Cvs.LongAddressLowByte);
            if (highByte.HasValue && lowByte.HasValue)
            {
                return (short?)(((highByte.Value - 192) * 256) + lowByte.Value);
            }
            return null;
        }
    }

    #endregion

    #region State Management

    /// <summary>
    /// Sets the source file information for the current decoder settings.
    /// </summary>
    public void SetDecoder(string decoderFile, DecoderFileLocation fileLocation, string? name, string? manufacturer)
    {
        DecoderFile = decoderFile;
        DecoderFileLocation = fileLocation;
        DecoderName = name;
        DecoderMfg = manufacturer;
    }

    /// <summary>
    /// Clears all loaded settings, CV values, metadata, and pages.
    /// </summary>
    public void Clear()
    {
        DecoderFile = null;
        DecoderFileLocation = null;
        CvValues.Clear();
        CvMetadata.Clear(); // Clear metadata as well
        Pages.Clear();
    }

    /// <summary>
    /// Resets the IsChanged flag to false.
    /// </summary>
    public void ClearChanged()
    {
        var listOfChangedCvs = CvValues.Values.Where(x => x.IsChanged).ToList();
        foreach (var cvValue in listOfChangedCvs)
        {
            cvValue.ClearChanged();
        }
    }

    #endregion State Management
}
