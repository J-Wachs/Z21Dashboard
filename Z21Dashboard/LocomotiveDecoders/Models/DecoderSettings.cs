using System.ComponentModel.DataAnnotations;
using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.NmraDccStandard;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Represents the runtime settings for a specific decoder, including loaded CV values and UI layout.
/// </summary>
public class DecoderSettings
{
    private bool _isChanged = false;

    public string? DecoderFile { get; private set; }

    public DecoderFileLocation? DecoderFileLocation { get; private set; }
    
    [MaxLength(30)]
    public string? DecoderModel
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(50)]
    public string? DecoderMfg
    {
        get;
        set => SetField(ref field, value);
    }

    [Range(0, 999)]
    public int? DecoderMfgId
    {
        get;
        set => SetField(ref field, value);
    }

    [MaxLength(10)]
    public string? DecoderDefVersion
    {
        get;
        set => SetField(ref field, value);
    }

    public bool? DecoderSound
    {
        get;
        set => SetField(ref field, value);
     }

    [MaxLength(30)]
    public string? DecoderProtocols
    {
        get;
        set => SetField(ref field, value);
    }

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
        return _isChanged || CvValues.Values.Any(x => x.IsChanged);
    }

    /// <summary>
    /// Sets a specific bit in a CV.
    /// </summary>
    public Result SetCvBitValue(ushort cv, Bits bit, bool? value)
    {
        if (CvValues.ContainsKey(cv) is false)
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
        if (CvValues.ContainsKey(cv) is false)
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
        if (CvValues.TryGetValue(cv, out var cvState))
        {
            return cvState?.GetWholeValue();
        }
        return null;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Sets the source file information for the current decoder settings.
    /// </summary>
    public void SetDecoder(
        string decoderFile,
        DecoderFileLocation fileLocation,
        string? model,
        string? manufacturer,
        ushort? manufacturerId,
        string? version,
        string? protocols,
        bool? sound
        )
    {
        DecoderFile = decoderFile;
        DecoderFileLocation = fileLocation;
        DecoderModel = model;
        DecoderMfg = manufacturer;
        DecoderMfgId = manufacturerId;
        DecoderDefVersion = version;
        DecoderProtocols = protocols;
        DecoderSound = sound;

        _isChanged = true;
    }

    /// <summary>
    /// Clears all loaded settings, CV values, metadata, and pages.
    /// </summary>
    public void Clear()
    {
        DecoderFile = null;
        DecoderFileLocation = null;
        DecoderModel = null;
        DecoderMfg = null;
        DecoderMfgId = null;
        DecoderDefVersion = null;
        DecoderProtocols = null;
        DecoderSound = null;

        CvValues.Clear();
        CvMetadata.Clear(); // Clear metadata as well
        Pages.Clear();
        _isChanged = false;
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
        _isChanged = false;
    }

    #endregion State Management

    #region Configuration

    /// <summary>
    /// Returns the address of the decoder, based on the CV 29 bit 5 (Short/long address).
    /// </summary>
    /// <returns></returns>
    public short? GetLocoAddress()
    {
        bool? useLongAddress = false;

        // This is done because for Mäklin Motorola decoders, the only CV in the file
        // is CV 1 to hold the address.
        if (CvValues.ContainsKey(Cvs.ConfigurationData1) is true)
        {
            useLongAddress = GetCvBit(Cvs.ConfigurationData1, Bits.Five);
        }

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

    /// <summary>
    /// Does the decoder in question supports RailCom? We look at bit 3 in CV 29.
    /// </summary>
    /// <returns></returns>
    public bool SupportsRailCom()
    {
        if (CvValues.TryGetValue(Cvs.ConfigurationData1, out var cvValue))
        {
            if (cvValue.GetBit(Bits.Three) is true)
            {
                return true;
            }
        }
        return false;
    }

    #endregion Configuration

    #region Helpers

    /// <summary>
    /// Helper method to set a field value and mark the object as changed if the value is different from the current value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="field"></param>
    /// <param name="value"></param>
    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value) is false)
        {
            field = value;
            _isChanged = true;
        }
    }

    #endregion Helpers

}
