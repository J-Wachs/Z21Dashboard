using Z21Dashboard.Helpers;
using Z21Dashboard.Locomotives.Models;

namespace Z21Dashboard.Locomotives.Interfaces;

public interface ILocoDefinitionService
{
    /// <summary>
    /// Delete a locomotive definition file.
    /// </summary>
    /// <param name="filePath">The locomotive definition file to delete</param>
    /// <returns></returns>
    Task<Result> DeleteLocomotiveFileAsync(string filePath);

    /// <summary>
    /// Loads a locomotive definition file and sets the data to the internal
    /// locomotive information structure (class).
    /// </summary>
    /// <param name="locoDefFilePath"></param>
    /// <param name="locomotiveSettings"></param>
    /// <returns></returns>
    Task<Result> GetLocomotiveAndSetInfoIntoSettingsAsync(
        string locoDefFilePath,
        LocomotiveSettings locomotiveSettings
        );

    /// <summary>
    /// Gets a locomotive definition (meta) by the locomotive address.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    Task<Result<LocomotiveListItemDto?>> GetLocomotiveByAddressAsync(ushort address);

    /// <summary>
    /// Gets a list of locomotive files from the folder defined 
    /// in the settings.
    /// </summary>
    /// <returns></returns>
    Task<Result<IReadOnlyList<LocomotiveListItemDto>?>> GetLocomotivesAsync();

    /// <summary>
    /// Saves the locomotive data to a file in the locmotive
    /// folder.
    /// </summary>
    /// <param name="locoFileName"></param>
    /// <param name="locoSettings"></param>
    /// <returns></returns>
    Task<Result> SaveValuesToLocomotiveDefinitionFileAsync(
        string locoFileName,
        LocomotiveSettings locoSettings
        );
}
