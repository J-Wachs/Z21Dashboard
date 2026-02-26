namespace Z21Dashboard.Application.Models;


public class DecoderSettings
{
    // Vores dictionary gemmer nu det smarte objekt
    public Dictionary<ushort, CvState> CvValues { get; set; } = [];

    // Hjælpe-metode til UI
    public void SetCvBit(ushort cv, int bit, bool? value)
    {
        if (!CvValues.ContainsKey(cv))
            CvValues[cv] = new CvState();

        CvValues[cv].SetBit(bit, value);
    }

    // Hjælpe-metode til almindelige CV'er (fx CV 1)
    public void SetCvByte(ushort cv, short? value)
    {
        if (!CvValues.ContainsKey(cv))
            CvValues[cv] = new CvState();

        CvValues[cv].WholeValue = value;
        // Nulstil bits, da hele byten overskriver alt
        for (int i = 0; i < 8; i++) CvValues[cv].Bits[i] = null;
    }


    public short? GetCvByte(ushort cv)
    {
        if (CvValues.TryGetValue(cv, out var cvState))
        {
            return cvState?.WholeValue;
        }

        return null;
    }
}