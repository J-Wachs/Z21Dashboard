using Z21Client.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.Resources.Localization;

namespace Z21Dashboard.Application.Models;

public class CvState
{
    private readonly CvInformation _cvInformation = default!;

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
        _cvInformation = cvInformation;

        var cvNbr = cvInformation.Cv;

        if (_cvInformation.ValueBits is not null)
        {
            if (_cvInformation.ValueBits.FromBit is not null)
            {
                if (_cvInformation.ValueBits.FromBit < 0)
                {
                    _fromBit = 0;
                }
                else
                {
                    _fromBit = (int)_cvInformation.ValueBits.FromBit;
                }
            }

            if (_cvInformation.ValueBits.ToBit is not null)
            {
                if (_cvInformation.ValueBits.ToBit > 7)
                {
                    _toBit = 7;
                }
                else
                {
                    _toBit = (int)_cvInformation.ValueBits.ToBit;
                }
            }

            _bitCount = (_toBit ?? 7) - (_fromBit ?? 0) + 1;

            // Calculate Min and Max values
            if (_cvInformation.ValueBits.Min is not null)
            {
                if (_cvInformation.ValueBits.Min < 0)
                {
                    _minValue = 0;
                }
                else
                {
                    _minValue = (int)_cvInformation.ValueBits.Min;
                }
            }

            if (_cvInformation.ValueBits.Max is not null)
            {
                if (_fromBit is null || _toBit is null)
                {
                    throw new InvalidOperationException("If FromBit or ToBit is set, Max value must be calculated based on bit range, not directly from CV definition.");
                }

                var tempMaxValue = (1 << _bitCount) - 1;
                if (_cvInformation.ValueBits.Max > tempMaxValue)
                {
                    _maxValue = tempMaxValue;
                }
                else
                {
                    _maxValue = (int)_cvInformation.ValueBits.Max;
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
    /// Sætter en enkelt bit (bruges til Checkbokse/Dropdowns).
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

            // Palce it correctly i the array
            Bits[(_fromBit ?? (int)Z21Client.Models.Bits.Zero) + i] = bit;
        }
        IsChanged = true;

        return Result.Success();
    }


    public void ClearChanged()
    {
        IsChanged = false;
    }
}
