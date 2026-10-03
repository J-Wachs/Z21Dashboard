using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Holds the state of each CV.
/// As a CV in some cases is not just a simple number, the value must be handled
/// as bits. Some CV consist of several flags, and must be handled as such.
/// </summary>
public class CvState
{
    private readonly int _bitCount;
    private readonly int? _toBit;
    private readonly int? _fromBit;
    private readonly int? _minValue;
    private readonly int? _maxValue;

    /// <summary>
    /// Array that keeps track of each bit in the CV (0-7).
    /// null = Unknown/no change to make
    /// true = 1
    /// false = 0
    /// </summary>
    public bool?[] Bits
    {
        get;
        set
        {
            field = value;
            IsChanged = true;
        }
    } = new bool?[8];

    /// <summary>
    /// Flag that indicates if the CV value has been changed since it was loaded or
    /// since the last ClearChanged() call.
    /// </summary>
    public bool IsChanged { get; private set; } = false;

    /// <summary>
    /// The value of the CV based on the defined bit range for the value (e.g. CV 19, bits 0 - 6 amd bit 7 is a direction flag). 
    /// </summary>
    public byte? Value
    {
        get => GetValue();
        set => SetValue(value);
    }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="cvInformation"></param>
    internal CvState(CvInformation cvInformation)
    {
        if (cvInformation.ValueBits is not null)
        {
            if (cvInformation.ValueBits.FromBit is not null)
            {
                if (cvInformation.ValueBits.FromBit < 0)
                {
                    _fromBit = 0;
                }
                else
                {
                    _fromBit = (int)cvInformation.ValueBits.FromBit;
                }
            }

            if (cvInformation.ValueBits.ToBit is not null)
            {
                if (cvInformation.ValueBits.ToBit > 7)
                {
                    _toBit = 7;
                }
                else
                {
                    _toBit = (int)cvInformation.ValueBits.ToBit;
                }
            }

            _bitCount = (_toBit ?? 7) - (_fromBit ?? 0) + 1;

            // Calculate Min and Max values
            if (cvInformation.ValueBits.Min is not null)
            {
                if (cvInformation.ValueBits.Min < 0)
                {
                    _minValue = 0;
                }
                else
                {
                    _minValue = (int)cvInformation.ValueBits.Min;
                }
            }

            if (cvInformation.ValueBits.Max is not null)
            {
                if (_fromBit is null || _toBit is null)
                {
                    // "When FromBit or ToBit is set in CV {0}, Max value must be calculated based on bit range, not directly from CV definition"
                    var errorMessage = string.Format(SharedResources.FromToBitCalculation, cvInformation.Cv);
                    throw new InvalidOperationException(errorMessage);
                }

                var tempMaxValue = (1 << _bitCount) - 1;
                if (cvInformation.ValueBits.Max > tempMaxValue)
                {
                    _maxValue = tempMaxValue;
                }
                else
                {
                    _maxValue = (int)cvInformation.ValueBits.Max;
                }
            }
        }
        else
        {
            _bitCount = (_toBit ?? 7) - (_fromBit ?? 0) + 1;
        }
    }

    /// <summary>
    /// Patches a value field (input) with the bits that are set in the Bits array.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public byte? PatchWithWholeValue(byte value)
    {
        byte tempValue = (byte)value;

        for (int i = 0; i < 8; i++)
        {
            if (Bits[i] == true)
            {
                tempValue |= (byte)(1 << i);
            }
            else if (Bits[i] == false)
            {
                tempValue &= (byte)~(1 << i);
            }
        }

        return tempValue;
    }

    /// <summary>
    /// Sets a bit.value in the current CV.
    /// </summary>
    public Result SetBit(Bits bit, bool? value)
    {
        if ((int)bit >= _fromBit && (int)bit <= _toBit)
        {
            // "Input bit {0} must not be in range '{1}' to '{2}'"
            return Result.Failure(string.Format(SharedResources.BitOutOfRange, bit, _fromBit, _toBit));
        }

        if (Bits[(ushort)bit] != value)
        {
            Bits[(ushort)bit] = value;
            IsChanged = true;
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets a specific bit from the current CV.
    /// </summary>
    /// <param name="bit"></param>
    /// <returns></returns>
    public bool? GetBit(Bits bit)
    {
        return Bits[(ushort)bit];
    }

    /// <summary>
    /// Calculates the 'whole' value of the CV by looking at each bit in the defined bit range
    /// for the value (e.g. CV 19, bits 0 - 6 and bit 7 is a direction flag). If any of the bits
    /// in the range is null, null is returned, indicating that the whole value cannot be determined
    /// (e.g. because not all bits have been set or loaded).
    /// </summary>
    /// <returns></returns>
    public byte? GetWholeValue()
    {
        int result = 0;

        bool atLeastOneBitSet = false;

        for (int i = 0; i < 8; i++)
        {
            if (Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] is true)
            {
                result |= (1 << i);
                atLeastOneBitSet = true;
            }
            else if (Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] is false)
            {
                atLeastOneBitSet = true;
            }
        }

        if (atLeastOneBitSet)
        {
            return (byte?)result;
        }
        return null;
    }

    /// <summary>
    /// Calculates the CV value based on the defined bit range for the value
    /// (e.g. CV 19, bits 0 - 6 and bit 7 is a direction flag). If any of the
    /// bits are null, null is returned, indicating that the value cannot be determined
    /// </summary>
    /// <returns></returns>
    public byte? GetValue()
    {
        int result = 0;

        bool atLeastOneBitSet = false;

        for (int i = 0; i < _bitCount; i++)
        {
            if (Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] is true)
            {
                result |= (1 << i);
                atLeastOneBitSet = true;
            }
            else if (Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] is false)
            {
                atLeastOneBitSet = true;
            }
        }

        if (atLeastOneBitSet)
        {
            return (byte?)result;
        }
        return null;
    }

    /// <summary>
    /// Calculates the CV value based on a specific bit range.
    /// If any of the bits are null, null is returned, indicating
    /// that the value cannot be determined.
    /// </summary>
    /// <param name="fromBit"></param>
    /// <param name="toBit"></param>
    /// <returns></returns>
    public byte? GetValueInBitRange(Bits fromBit, Bits toBit)
    {
        int result = 0;

        int bitCount = (int)toBit - (int)fromBit + 1;

        bool atLeastOneBitSet = false;

        for (int i = 0; i < bitCount; i++)
        {
            if (Bits[(int)fromBit + i] is true)
            {
                result |= (1 << i);
                atLeastOneBitSet = true;
            }
            else if (Bits[(int)fromBit + i] is false)
            {
                atLeastOneBitSet = true;
            }
        }

        if (atLeastOneBitSet)
        {
            return (byte?)result;
        }
        return null;
    }

    /// <summary>
    /// Checks if any of the 8 bit is null. That is the signal
    /// that the CV is bit based and we must set the value in
    /// the decoder by bit, not a whole value.
    /// </summary>
    /// <returns>True if at least one bit is null (not set)</returns>
    public bool AnyBitNotSet()
    {
        return Bits.Any(x => x is null);
    }

    /// <summary>
    /// Sets the whole cv value, not looking at each bit.
    /// </summary>
    /// <param name="wholeValue"></param>
    public void SetWholeValue(byte? wholeValue)
    {
        for (int i = 0; i < 8; i++)
        {
            // Take bit 'i' from value (LSB first)
            bool? bit = null;
            var bit2 = (wholeValue >> i) & 1;
            if (bit2 is not null)
            {
                bit = ((wholeValue >> i) & 1) == 1;
            }

            // Place correctly in the array.
            Bits[i] = bit;
        }
    }

    /// <summary>
    /// Sets the value (value bits) of the CV and synchronizes the individual bits.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Result SetValue(byte? value)
    {
        if (value is not null && (value < _minValue || value > _maxValue))
        {
            // "Value must be between {0} and {1}"
            return Result.Failure(string.Format(SharedResources.ValueOutOfRange, _minValue, _maxValue));
        }

        for (int i = 0; i < _bitCount; i++)
        {
            bool? bit = null;
            // Take bit 'i' from value (LSB first)
            var bit2 = ((value >> i) & 1);
            if (bit2 is not null)
            {
                bit = bit2 == 1;
            }

            // Place it correctly i the array
            Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] = bit;
        }
        IsChanged = true;

        return Result.Success();
    }

    /// <summary>
    /// Sets the value of the CV in a specific bit range and synchronizes the individual bits.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fromBit"></param>
    /// <param name="toBit"></param>
    /// <returns></returns>
    public Result SetValueInBitRange(byte? value, Bits fromBit, Bits toBit)
    {
        int bitCount = (int)toBit - (int)fromBit + 1;
        if (value is not null && (value < 0 || value > (1 << bitCount) - 1))
        {
            // "Value must be between {0} and {1}"
            return Result.Failure(string.Format(SharedResources.ValueOutOfRange, 0, (1 << bitCount) - 1));
        }
        for (int i = 0; i < bitCount; i++)
        {
            bool? bit = null;
            // Take bit 'i' from value (LSB first)
            var bit2 = ((value >> i) & 1);
            if (bit2 is not null)
            {
                bit = bit2 == 1;
            }
            // Place it correctly in the array
            Bits[(int)fromBit + i] = bit;
        }
        IsChanged = true;
        return Result.Success();
    }

    /// <summary>
    /// Clears the 'IsChanged' flag for this CV.
    /// </summary>
    public void ClearChanged()
    {
        IsChanged = false;
    }
}
