using Z21Client.Models;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Used to keep track of which CV and bit is currently being edited in the UI,
/// so that we can update the correct value when the user changes it.
/// </summary>
internal class CvBitInPage : IEquatable<CvBitInPage>
{
    public ushort Cv { get; set; }
    public Bits? Bit { get; set; }


    public bool Equals(CvBitInPage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other)) return true;

        return Cv == other.Cv && Bit == other.Bit;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CvBitInPage);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cv, Bit);
    }
}
