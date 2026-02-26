namespace Z21Dashboard.Application.Models;

/// <summary>
/// Holds all RailCom-related CV parameters for a locomotive decoder
/// </summary>
public class CVRailComParameters
{
    // --- CV28 bits ---
    /// <summary>
    /// Bit 0 in CV28: Enable/disable RailCom data on channel 1
    /// </summary>
    public bool CV28_Bit0_Channel1 { get; set; }

    /// <summary>
    /// Bit 1 in CV28: Enable/disable RailCom data on channel 2
    /// </summary>
    public bool CV28_Bit1_Channel2 { get; set; }

    /// <summary>
    /// Bit 7 in CV28: Auto-login or RailCom-specific feature
    /// </summary>
    public bool CV28_Bit7_AutoLogin { get; set; }

    // --- CV29 bits ---
    /// <summary>
    /// Bit 3 in CV29: Enable or disable RailCom
    /// </summary>
    public bool CV29_3_RailComEnabled { get; set; }
}
