namespace Z21Dashboard.Application.Models;

public sealed class FirmwareVersion(byte major, byte minor)
{
    public Version Version { get; } = new Version(major, minor);

    public override string ToString()
    {
        return $"V{Version.Major}.{Version.Minor:D2}";
    }
}
