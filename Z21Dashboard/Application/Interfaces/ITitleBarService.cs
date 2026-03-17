namespace Z21Dashboard.Application.Interfaces;

// <summary>
/// Interface defining methods and events for managing the title bar's title and subtitle.
/// </summary>
public interface ITitleBarService
{
    /// <summary>
    /// Sets the title of the title bar.
    /// </summary>
    /// <param name="title"></param>
    void SetTitle(string? title);

    /// <summary>
    /// Sets the subtitle of the title bar.
    /// </summary>
    /// <param name="subtitle"></param>
    void SetSubtitle(string? subtitle);

    /// <summary>
    /// Event triggered when the title changes.
    /// </summary>
    event Action<string?>? TitleChanged;

    /// <summary>
    /// Event triggered when the subtitle changes.
    /// </summary>
    event Action<string?>? SubtitleChanged;
}
