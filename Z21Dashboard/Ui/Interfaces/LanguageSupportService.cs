using System.Globalization;

namespace Z21Dashboard.Ui.Interfaces;

public class LanguageSupportService : ILanguageSupportService
{
    // English language code for fallback language
    public const string EnglishLangCode = "en";

    // Languages supported (besides english):
    public const string DanishLangCode = "da";
    public const string GermanLangCode = "de";
    public const string FrenchLangCode = "fr";

    private readonly IReadOnlyList<string> _supportedLanguages = [DanishLangCode, EnglishLangCode, GermanLangCode, FrenchLangCode];

    public string GetLanguageCode()
    {
        string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (_supportedLanguages.Contains(langCode, StringComparer.CurrentCultureIgnoreCase) is false)
        {
            langCode = EnglishLangCode;
        }

        return langCode.ToLowerInvariant();
    }
}
