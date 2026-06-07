using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard.Services;

/// <summary>
/// Provides functionality for setting and notifying changes to the application's title bar text.
/// </summary>
/// <remarks>This service allows components to update the title and subtitle displayed in the application's title
/// bar and to subscribe to notifications when these values change. It is typically used to coordinate title bar updates
/// across different parts of an application.</remarks>
public class TitleBarService : ITitleBarService
{
    /// <inheritdoc/>
    public void SetTitle(string? title) => TitleChanged?.Invoke(title);

    /// <inheritdoc/>
    public void SetSubtitle(string? subtitle) => SubtitleChanged?.Invoke(subtitle);

    /// <inheritdoc/>
    public event Action<string?>? TitleChanged;

    /// <inheritdoc/>
    public event Action<string?>? SubtitleChanged;
}
