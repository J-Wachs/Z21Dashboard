using System.Collections.ObjectModel;
using Z21Dashboard.Ui;

namespace Z21Dashboard.LocomotiveDecoders.Models;

/// <summary>
/// Represents a UI page defined in the decoder definition JSON file.
/// </summary>
public class DecoderPage
{
    /// <summary>
    /// The Id of the page.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Default type is "dynamic_list" unless specified otherwise in JSON
    /// </summary>
    public string Type { get; set; } = DecoderUIPageTypes.DynamicList;

    /// <summary>
    /// Title of the page. Usally displayed in the tab.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of the page (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Content for "dynamic_list" type pages.
    /// Contains a standardized list of items (CVs or specific bits).
    /// </summary>
    public required ObservableCollection<PageContentBase> PageContent { get; set; }
}
