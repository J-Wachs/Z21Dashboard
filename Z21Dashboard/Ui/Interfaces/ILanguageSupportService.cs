namespace Z21Dashboard.Ui.Interfaces;

/// <summary>
/// Language support service interface for retrieving the current language code used in the application.
/// </summary>
public interface ILanguageSupportService
{
    /// <summary>
    /// Returns the current language code used in the application.
    /// </summary>
    /// <returns></returns>
    string GetLanguageCode();
}
