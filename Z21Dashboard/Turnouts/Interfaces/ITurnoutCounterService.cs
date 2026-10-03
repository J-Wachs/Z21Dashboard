using Z21Dashboard.Turnouts.Models;

namespace Z21Dashboard.Turnouts.Interfaces;

/// <summary>
/// Defindes a service for handle data for turnouts.
/// </summary>
public interface ITurnoutCounterService
{
    /// <summary>
    /// Event to implement in classes that what need to subscribe to the event 
    /// for data updates for turnout changes.
    /// </summary>
    event Action OnDataUpdated;

    /// <summary>
    /// Gets information about the turnouts tracked by the service.
    /// </summary>
    /// <returns></returns>
    IEnumerable<TrackedTurnoutCount> GetTrackedTurnouts();

    /// <summary>
    /// Returns the count a turnout has been changed from one position to the other.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    long GetCount(ushort address);

    /// <summary>
    /// Increment the count that a turnout has swithed from one position to another.
    /// </summary>
    /// <param name="address"></param>
    void IncrementSwitchCount(ushort address);

    /// <summary>
    /// Reset the totoal and since last servie, switch count for a turnout.
    /// </summary>
    /// <param name="address"></param>
    void ResetCount(ushort address);

    /// <summary>
    /// Remove a turnout from the monotoring of switch count.
    /// </summary>
    /// <param name="address"></param>
    void RemoveTurnout(ushort address);

    /// <summary>
    /// Resets the count of switches since last service.
    /// </summary>
    /// <param name="address"></param>
    void ResetServiceCounter(ushort address);

    /// <summary>
    /// Gets the metadata for a turnout.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    TurnoutMetadata? GetMetadata(ushort address);

    /// <summary>
    /// Saves the metadata for a turnout.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    Task SaveMetadataAsync(
        ushort address,
        TurnoutMetadata metadata
        );
}
