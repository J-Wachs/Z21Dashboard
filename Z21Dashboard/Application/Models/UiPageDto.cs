using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Properties of a UI page for the advanced decoder configuration widget.
/// </summary>
public class UiPageDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public Dictionary<string, string>? Title { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    [JsonPropertyName("page_content")]
    public required ObservableCollection<PageContentBase> PageContent { get; set; }
}
