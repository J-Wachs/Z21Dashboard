namespace Z21Dashboard.Application.Models;

/// <summary>
/// A lightweight class specifically for storing user-specific dashboard layout data.
/// It does NOT contain developer-defined properties like Width, Height, or Name.
/// </summary>
public class DashboardComponentStorage
{
    public Guid Id { get; set; }
    public string ComponentTypeName { get; set; } = string.Empty;
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public bool IsVisible { get; set; } = true;
    public int ZIndex { get; set; } = 1;
}
