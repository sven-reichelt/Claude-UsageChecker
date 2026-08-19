using System.Globalization;

namespace ClaudeUsageChecker.Core.Localization;

/// <summary>
/// A language the application is available in.
/// </summary>
/// <param name="Code">
/// BCP 47 tag, and at the same time the name of the language file - "de" or "pt-BR".
/// </param>
/// <param name="NativeName">
/// The name of the language in itself. In a language picker it is deliberately
/// left untranslated: someone who switched the interface to Russian by accident
/// will recognise "Deutsch", but not "German" spelled in Cyrillic.
/// </param>
public sealed record Language(string Code, string NativeName)
{
    /// <summary>The project's source language. It is the fallback for everything.</summary>
    public static Language Default { get; } = new("en", "English");

    /// <summary>Every available language, in the order the picker shows them.</summary>
    /// <remarks>
    /// English and German first, the rest sorted by native name. Portuguese
    /// appears twice: the differences are everyday enough not to paper over -
    /// a file is "arquivo" in Brazil and "ficheiro" in Portugal.
    /// </remarks>
    public static IReadOnlyList<Language> All { get; } =
    [
        Default,
        new("de", "Deutsch"),
        new("es", "Español"),
        new("fr", "Français"),
        new("it", "Italiano"),
        new("pt-BR", "Português (Brasil)"),
        new("pt-PT", "Português (Portugal)"),
        new("ru", "Русский"),
        new("zh-Hans", "简体中文")
    ];

    /// <summary>Looks up a language by its tag.</summary>
    public static Language? Find(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : All.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Picks the language that best matches the system.
    /// </summary>
    /// <remarks>
    /// In three steps: the exact tag ("pt-BR"), then another variant of the same
    /// language ("pt-PT" for a Portuguese system without a region), and finally
    /// English. Chinese needs special handling because the script is not part of
    /// the language tag but of the region - "zh-CN" and "zh-SG" write simplified,
    /// "zh-TW" and "zh-HK" do not.
    /// </remarks>
    public static Language FromSystem(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        if (Find(culture.Name) is { } exact)
        {
            return exact;
        }

        if (string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase))
        {
            // No translation exists for traditional Chinese; there English is
            // closer to the expectation than anything else on the list.
            return WritesSimplified(culture) ? All.First(l => l.Code == "zh-Hans") : Default;
        }

        var twoLetter = culture.TwoLetterISOLanguageName;

        return All.FirstOrDefault(l =>
                   string.Equals(l.Code, twoLetter, StringComparison.OrdinalIgnoreCase))
               ?? All.FirstOrDefault(l =>
                   l.Code.StartsWith(twoLetter + "-", StringComparison.OrdinalIgnoreCase))
               ?? Default;
    }

    private static bool WritesSimplified(CultureInfo culture)
    {
        var name = culture.Name;

        return name.Contains("Hans", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith("-CN", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith("-SG", StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, "zh", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The culture used for numbers, dates and times.
    /// </summary>
    /// <remarks>
    /// Deliberately tied to the interface language rather than to the system:
    /// someone who switches the interface to French expects French dates there
    /// as well. If the system does not know the tag, its own culture stays in
    /// place - a time in the wrong format is more bearable than a crash.
    /// </remarks>
    public CultureInfo ToCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo(Code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentCulture;
        }
    }
}
