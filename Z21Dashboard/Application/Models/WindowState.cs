namespace Z21Dashboard.Application.Models;

/// <summary>
/// Represents the persisted state of the application window.
/// </summary>
/// <param name="IsMaximized">Indicates whether the window was maximized when closed.</param>
public record WindowState(bool IsMaximized);
