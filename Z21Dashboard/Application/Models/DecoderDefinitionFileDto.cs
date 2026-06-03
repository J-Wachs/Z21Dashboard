using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Sections in a decoder file.
/// </summary>
public class DecoderDefinitionFileDto
{
    [JsonPropertyName("meta")]
    public required DecoderMetaDto Meta { get; set; }

    [JsonPropertyName("cv_definitions")]
    public List<CvDefinitionItemDto> CvDefinitions { get; set; } = [];

    [JsonPropertyName("ui_pages")]
    public ObservableCollection<UiPageDto>? UiPages { get; set; }
}
