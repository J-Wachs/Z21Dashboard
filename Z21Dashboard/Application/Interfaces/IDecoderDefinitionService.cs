using Z21Dashboard.Application.Models;
using Z21Dashboard.Helpers;
using Z21Dashboard.Services.DTOs;

namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Interface for the service responsible for managing decoder definitions.
/// </summary>
internal interface IDecoderDefinitionService
{
    /// <summary>
    /// Retrieves a list of all available decoder definitions found in both 
    /// the application package and the local custom folder.
    /// </summary>
    /// <returns>A list of decoder items containing metadata.</returns>
    Task<Result<IReadOnlyList<DecoderListItemDto>?>> GetAvailableDecodersAsync();

    /// <summary>
    /// Get the meta section of a local decoder definition file without fully parsing the entire file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    Task<DecoderFileRootDto?> GetMetaInfoFromLocalDecoderFile(string filePath);

    /// <summary>
    /// The decoder definition file ia updated with the values from teh DecoderSettings structure.
    /// /// The method is to read the existing file, update the CV values in the file with the values
    /// from the DecoderSettings, and then save the updated file back to disk. This allows users
    /// to maintain their custom decoder definitions while ensuring that any changes made in the
    /// application are reflected in the file.
    /// </summary>
    /// <param name="decoder"></param>
    /// <param name="decoderSettings"></param>
    /// <returns></returns>
    Task<Result> SaveValuesToUserDefinedLocoDecoderAsync(DecoderListItemDto decoder, DecoderSettings decoderSettings);

    /// <summary>
    /// Loads a specific decoder definition file, processes inheritance from NMRA standards,
    /// and populates the provided DecoderSettings object with CVs and UI pages.
    /// </summary>
    /// <param name="decoder">The decoder definition.</param>
    /// <param name="decoderSettings">The settings object to populate.</param>
    Task<Result> SetCVsFromDecoderDefIntoSettings(DecoderListItemDto decoder, DecoderSettings decoderSettings);
}
