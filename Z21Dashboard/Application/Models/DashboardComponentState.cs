using System.Text.Json.Serialization;

namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents the state of a single component on the user-configurable dashboard.
/// Uses absolute positioning for a free-form layout.
/// </summary>
public class DashboardComponentState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;

    // --- NEW: Absolute layout properties ---

    /// <summary>
    /// The X coordinate (distance from the left edge) in pixels.
    /// </summary>
    public int PositionX { get; set; }

    /// <summary>
    /// The Y coordinate (distance from the top edge) in pixels.
    /// </summary>
    public int PositionY { get; set; }

    /// <summary>
    /// The fixed width of the component in pixels.
    /// </summary>
    public int Width { get; set; } = 400; // A sensible default width

    /// <summary>
    /// The fixed height of the component in pixels. Can be 0 for auto-height.
    /// </summary>
    public int Height { get; set; } // Defaulting to 0 could mean 'auto'

    /// <summary>
    /// The stacking order (higher numbers are on top).
    /// </summary>
    public int ZIndex { get; set; } = 1;

    // --- End of new properties ---

    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonIgnore]
    public Type? ComponentType { get; set; }

    public Dictionary<string, object>? Parameters { get; set; }
}
