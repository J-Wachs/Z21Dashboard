namespace Z21Dashboard.Application.Models;

using System.Linq;

public class CvState
{
    private short? _wholeValue;

    // Array der holder styr på hver enkelt bit (0-7).
    // null = Ukendt/Ingen ændring
    // true = 1
    // false = 0
    public bool?[] Bits { get; private set; }

    public CvState()
    {
        // Initialiser arrayet med 8 pladser, alle null
        Bits = new bool?[8];
    }

    /// <summary>
    /// Sætter eller henter hele CV-værdien (0-255).
    /// </summary>
    public short? WholeValue
    {
        get { return _wholeValue; }
        set
        {
            _wholeValue = value;

            if (value != null)
            {
                // Hvis vi kender hele tallet, kender vi også alle bits!
                // Opdater Bits-arrayet så det matcher tallet.
                int val = value.Value;
                for (int i = 0; i < 8; i++)
                {
                    // Tjek om bit 'i' er sat i 'val'
                    Bits[i] = (val & (1 << i)) != 0;
                }
            }
            else
            {
                // Hvis WholeValue sættes til null (ukendt),
                // nulstiller vi alle bits til ukendt.
                for (int i = 0; i < 8; i++)
                {
                    Bits[i] = null;
                }
            }
        }
    }

    /// <summary>
    /// Sætter en enkelt bit (bruges til Checkbokse/Dropdowns).
    /// </summary>
    public void SetBit(int bitIndex, bool? value)
    {
        if (bitIndex < 0 || bitIndex > 7) return;

        Bits[bitIndex] = value;

        // Efter en ændring skal vi se, om vi nu kender nok til at danne en WholeValue
        RecalculateWholeValue();
    }

    /// <summary>
    /// Henter en værdi ud fra en bitmaske (fx 0x7F for bit 0-6).
    /// Returnerer null, hvis bare én bit i masken er ukendt.
    /// </summary>
    public int? GetField(byte mask)
    {
        // Find ud af hvor meget vi skal shifte (hvis feltet starter på bit 4)
        int shift = 0;
        int tempMask = mask;
        while ((tempMask & 1) == 0 && tempMask != 0) { tempMask >>= 1; shift++; }

        int result = 0;

        for (int i = 0; i < 8; i++)
        {
            // Er denne bit en del af masken?
            if ((mask & (1 << i)) != 0)
            {
                // Hvis en bit i feltet er ukendt, er hele feltets værdi ukendt
                if (Bits[i] == null) return null;

                if (Bits[i] == true)
                {
                    result |= (1 << i);
                }
            }
        }

        // Returner værdien skubbet ned på plads (fx bit 4-6 bliver til 0-2)
        return result >> shift;
    }

    /// <summary>
    /// Sætter en værdi ned i en gruppe bits defineret af masken via bitwise operationer.
    /// Fx at skrive tallet "50" ned i bit 0-6 uden at røre bit 7.
    /// </summary>
    public void SetField(byte mask, int value)
    {
        // Beregn shift
        int shift = 0;
        int tempMask = mask;
        while ((tempMask & 1) == 0 && tempMask != 0) { tempMask >>= 1; shift++; }

        // Skub input-værdien op på plads
        int shiftedValue = value << shift;

        for (int i = 0; i < 8; i++)
        {
            // Hvis bit 'i' er en del af masken, skal vi opdatere den
            if ((mask & (1 << i)) != 0)
            {
                // Find ud af om bitten er 0 eller 1 i den nye værdi
                bool bitIsSet = (shiftedValue & (1 << i)) != 0;

                // Opdater vores hukommelse. Her sætter vi true/false, ikke null.
                Bits[i] = bitIsSet;
            }
            // Bits udenfor masken røres ikke (bliver stående som null, true eller false)
        }

        // Tjek om vi nu har et komplet billede af byten
        RecalculateWholeValue();
    }

    /// <summary>
    /// Intern hjælper: Tjekker om alle 8 bits er kendte (ikke-null).
    /// Hvis ja: Beregn og sæt _wholeValue.
    /// Hvis nej: Sæt _wholeValue til null.
    /// </summary>
    private void RecalculateWholeValue()
    {
        // Hvis bare én bit er ukendt (null), så er hele byten ukendt
        if (Bits.Any(b => b == null))
        {
            _wholeValue = null;
        }
        else
        {
            // Alle bits er kendte! Vi kan beregne tallet.
            int newVal = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Bits[i] == true)
                {
                    newVal |= (1 << i);
                }
            }
            _wholeValue = (short)newVal;
        }
    }
}
