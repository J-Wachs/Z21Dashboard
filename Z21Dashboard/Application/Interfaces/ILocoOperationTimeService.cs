using Z21Dashboard.Application.Models;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// A public record representing the tracked time and state for a single locomotive.
/// This is used to pass data to the UI without exposing internal data structures.
/// </summary>
public record TrackedLocoTime(
    ushort LocoAddress,
    int CurrentSpeedStep,
    NativeSpeedSteps NativeSpeedSteps,
    SpeedSteps SpeedSteps,
    byte SpeedStepsNumeric,
    long TotalOperatingSeconds,
    long OperatingSecondsSinceService,
    string? DisplayProtocol,
    LocoMode Protocol,
    DrivingDirection Direction,
    bool[] Functions,
    DateTime Timestamp
);

/// <summary>
/// Defines the contract for a service that tracks locomotive operating time.
/// </summary>
public interface ILocoOperatingTimeService
{
    /// <summary>
    /// An event that is raised whenever the operating time data is updated (typically once per second).
    /// </summary>
    event Action? OnDataUpdated;

    /// <summary>
    /// Gets a thread-safe snapshot of the currently tracked locomotives.
    /// </summary>
    /// <returns>A list of tracked locomotives and their operating times.</returns>
    IEnumerable<TrackedLocoTime> GetTrackedLocos();

    /// <summary>
    /// Resets the service counter for a specific locomotive back to zero.
    /// </summary>
    /// <param name="locoAddress">The address of the locomotive to reset.</param>
    void ResetServiceCounter(ushort locoAddress);
}