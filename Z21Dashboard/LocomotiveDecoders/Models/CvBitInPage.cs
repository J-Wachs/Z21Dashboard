using Z21Client.Models;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Used to keep track of which CV and bit range is currently being edited in the UI,
/// so that we can update the correct value when the user changes it.
/// </summary>
internal class CvBitInPage : IEquatable<CvBitInPage>
{
    public ushort Cv { get; set; }
    public Bits? FromBit { get; set; }
    public Bits? ToBit { get; set; }

    public bool Equals(CvBitInPage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other)) return true;

        return Cv == other.Cv && FromBit == other.FromBit && ToBit == other.ToBit;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CvBitInPage);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cv, FromBit, ToBit);
    }
}
