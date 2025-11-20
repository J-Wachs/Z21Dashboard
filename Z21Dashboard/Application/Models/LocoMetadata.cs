namespace Z21Dashboard.Application.Models;

/// <summary>
/// A model for storing user-defined metadata for a single locomotive.
/// This data is typically set by the user and changes infrequently.
/// </summary>
public class LocoMetadata
{
    /// <summary>
    /// The user-defined name for the locomotive.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The user-defined service interval in operating hours.
    /// If null or zero, no service interval is set.
    /// </summary>
    public int? ServiceIntervalHours { get; set; }
}
