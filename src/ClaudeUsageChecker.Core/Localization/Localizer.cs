using System.Globalization;
using System.Text.Json;

namespace ClaudeUsageChecker.Core.Localization;

/// <summary>
/// Holds the interface texts in the selected language.
/// </summary>
/// <remarks>
/// <para>
/// The texts are embedded JSON files, one per language. Deliberately not
/// satellite assemblies from .resx: the release is a trimmed single file, and
/// resources through the ResourceManager are the route with the most pitfalls
/// there. One embedded file per language is easy to survey, can be maintained
/// by hand, and survives every trimming pass.
/// </para>
/// <para>
/// If a key is missing in the selected language, the English text steps in. A
/// missing text is annoying; an empty window would be worse. <c>LanguageFileTests</c>
/// makes sure the case does not arise in the first place.
/// </para>
/// </remarks>
public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, string> _texts;
    private readonly IReadOnlyDictionary<string, string> _fallback;

    private Localizer(
        Language language,
        IReadOnlyDictionary<string, string> texts,
        IReadOnlyDictionary<string, string> fallback)
    {
        Language = language;
        _texts = texts;
        _fallback = fallback;
    }

    /// <summary>The language this localizer answers in.</summary>
    public Language Language { get; }

    /// <summary>
    /// The localizer currently in force. Set at startup and on every language
    /// change.
    /// </summary>
    public static Localizer Current { get; private set; } = Load(Language.Default);

    /// <summary>Switches the application to another language.</summary>
    /// <remarks>
    /// Also sets the culture of the process. Numbers, dates and times then follow
    /// the selected language everywhere, without every piece of formatting having
    /// to know about it - someone who switches the interface to French expects
    /// French dates there as well.
    /// </remarks>
    public static Localizer Use(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        Current = Load(language);

        var culture = language.ToCulture();
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        return Current;
    }

    /// <summary>Loads the texts of a language without switching the one in force.</summary>
    public static Localizer Load(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var fallback = ReadFile(Language.Default.Code);

        return language.Code == Language.Default.Code
            ? new Localizer(language, fallback, fallback)
            : new Localizer(language, ReadFile(language.Code), fallback);
    }

    /// <summary>
    /// The text for a key. If it is missing in both languages, the key itself
    /// comes back - so that what is missing is at least visible.
    /// </summary>
    public string this[string key] =>
        _texts.TryGetValue(key, out var text) ? text
        : _fallback.TryGetValue(key, out var english) ? english
        : key;

    /// <summary>The text for a key, with its placeholders filled in.</summary>
    public string Format(string key, params object?[] args) =>
        string.Format(Language.ToCulture(), this[key], args);

    /// <summary>Whether this language carries a key itself - without the fallback.</summary>
    public bool Has(string key) => _texts.ContainsKey(key);

    /// <summary>Every key of this language. For the completeness check.</summary>
    public IEnumerable<string> Keys => _texts.Keys;

    private static IReadOnlyDictionary<string, string> ReadFile(string code)
    {
        var name = $"ClaudeUsageChecker.Core.Localization.Texts.{code}.json";

        using var stream = typeof(Localizer).Assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            // Without a file the language stays empty and the fallback takes over.
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize(stream, LocalizationJsonContext.Default.DictionaryStringString)
               ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>The tags of every embedded language file.</summary>
    internal static IEnumerable<string> EmbeddedLanguageCodes()
    {
        const string prefix = "ClaudeUsageChecker.Core.Localization.Texts.";

        return typeof(Localizer).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal)
                        && n.EndsWith(".json", StringComparison.Ordinal))
            .Select(n => n[prefix.Length..^".json".Length]);
    }
}

[System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class LocalizationJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
