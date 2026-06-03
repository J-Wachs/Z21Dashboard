namespace Z21Dashboard.Application.Models;

/// <summary>
/// Parameter class to pass between Loco Programming components.
/// </summary>
public class ProgrammingTargetAddr
{
    public ProgrammingTarget ProgTarget { get; set; }
    public short? LocoAddress { get; set; }
}
