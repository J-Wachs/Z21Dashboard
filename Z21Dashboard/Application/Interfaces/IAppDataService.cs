namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines the contract for a generic service that persists and retrieves
/// application data sets from a central storage file.
/// </summary>
public interface IAppDataService
{
    /// <summary>
    /// Retrieves a specific data set from storage.
    /// </summary>
    /// <typeparam name="T">The type of the data set to retrieve.</typeparam>
    /// <param name="key">The unique key identifying the data set (e.g., "OperatingTimes").</param>
    /// <returns>The deserialized data object, or the default value for T if not found.</returns>
    T? GetData<T>(string key);

    /// <summary>
    /// Saves a specific data set to storage.
    /// </summary>
    /// <typeparam name="T">The type of the data set to save.</typeparam>
    /// <param name="key">The unique key identifying the data set (e.g., "OperatingTimes").</param>
    /// <param name="data">The data object to save.</param>
    void SaveData<T>(string key, T data);
}
