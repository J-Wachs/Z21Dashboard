using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines a service for managing user-defined metadata for locomotives,
/// such as names and service information.
/// </summary>
public interface ILocoMetadataService
{
    /// <summary>
    /// Event that is triggered when the metadata collection is updated.
    /// </summary>
    event Action? OnMetadataUpdated;

    /// <summary>
    /// Gets all stored metadata for a specific locomotive.
    /// </summary>
    /// <param name="locoAddress">The address of the locomotive.</param>
    /// <returns>A LocoMetadata object, or null if no metadata exists.</returns>
    LocoMetadata? GetMetadata(ushort locoAddress);

    /// <summary>
    /// Gets the formatted display name for a locomotive.
    /// Formats as "Address: Name" if a name exists, otherwise just "Address".
    /// </summary>
    /// <param name="locoAddress">The address of the locomotive.</param>
    /// <returns>The formatted display name as a string.</returns>
    string GetDisplayName(ushort locoAddress);

    /// <summary>
    /// Saves or updates the metadata for a specific locomotive.
    /// </summary>
    /// <param name="locoAddress">The address of the locomotive.</param>
    /// <param name="metadata">The metadata object to save.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveMetadata(ushort locoAddress, LocoMetadata metadata);

    /// <summary>
    /// Removes metadata for a specific locomotive.
    /// </summary>
    /// <param name="locoAddress">The address of the locomotive.</param>
    void RemoveMetadata(ushort locoAddress);
}