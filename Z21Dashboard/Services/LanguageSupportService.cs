using System.Globalization;
using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard.Services;

public class LanguageSupportService : ILanguageSupportService
{
    // English language code for fallback language
    public const string EnglishLangCode = "en";

    // Languages supported (besides english):
    public const string DanishLangCode = "da";

    private readonly IReadOnlyList<string> _supportedLanguages = [DanishLangCode, EnglishLangCode];

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
