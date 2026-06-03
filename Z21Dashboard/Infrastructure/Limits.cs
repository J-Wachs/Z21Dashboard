namespace Z21Dashboard.Infrastructure;

/// <summary>
/// Limits for various information in the application, such as highest allowed locomotive address, etc.
/// </summary>
internal static class Limits
{
    internal const ushort HighestLongAddress = 9999;
    internal const ushort HighestShortAddress1 = 99;
    internal const ushort HighestShortAddress2 = 127;
}
