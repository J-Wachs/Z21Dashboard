using Microsoft.AspNetCore.Components;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Definition of the tabs in the TabControl.
/// </summary>
public class TabPageDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public RenderFragment Content { get; init; } = default!;
}
