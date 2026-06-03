using System.Collections.ObjectModel;
using Z21Dashboard.Shared.Dashboard.Components.ProgrammingDecoder.AdvTabs;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents a UI page defined in the decoder definition JSON file.
/// </summary>
public class DecoderPage
{
    public string Id { get; set; } = string.Empty;

    // Default type is "dynamic_list" unless specified otherwise in JSON
    public string Type { get; set; } = DecoderUIPageTypes.DynamicList;

    // Title
    public string Title { get; set; } = string.Empty;

    // Description (optional)
    public string? Description { get; set; }

    // Content for "dynamic_list" type pages.
    // Contains a standardized list of items (CVs or specific bits).
    public required ObservableCollection<PageContentBase> PageContent { get; set; }

    // Configuration for other page types (e.g., speed curve start/end CVs).
    // Maps keys (like "cv_start") to integer values.
    public Dictionary<string, int> Config { get; set; } = [];
}
