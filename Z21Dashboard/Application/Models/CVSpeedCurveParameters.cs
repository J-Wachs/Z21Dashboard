namespace Z21Dashboard.Application.Models;

internal class CVSpeedCurveParameters
{
    public bool UseSpeedTable { get; set; }

    // Simpel curve (3 points)
    public byte CV2_VStart { get; set; }
    public byte CV6_VMid { get; set; }
    public byte CV5_VHigh { get; set; }

    // Avanced curve (28 points)
    public byte[] SpeedTable { get; set; } = new byte[28];
    public byte CV66_ForwardTrim { get; set; } = 128;
    public byte CV95_ReverseTrim { get; set; } = 128;
}
