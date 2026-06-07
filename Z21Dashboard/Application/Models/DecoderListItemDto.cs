namespace Z21Dashboard.Application.Models;

/// <summary>
/// Data Transfer Object representing a summary of a decoder definition.
/// Used for populating selection lists in the UI.
/// </summary>
/// <param name="FileName">The filename (e.g., "esu_v5.json").</param>
/// <param name="Name">The display name or model name of the decoder.</param>
/// <param name="Manufacturer">The manufacturer name.</param>
/// <param name="Location">Where the file is stored (App or Local).</param>
internal record DecoderListItemDto(
    string FileName,
    string Name,
    string Manufacturer,
    DecoderFileLocation Location
);
