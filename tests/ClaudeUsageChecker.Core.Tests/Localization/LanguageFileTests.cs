using System.Globalization;
using System.Reflection;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Tests.Localization;

/// <summary>
/// Checks the language files for completeness and usability.
/// </summary>
/// <remarks>
/// Missing translations show up nowhere else: the localizer falls back to
/// English in silence, and an English sentence in the middle of a Spanish
/// interface is noticed only by whoever opens that particular window. These
/// tests turn it into a build failure instead.
/// </remarks>
public class LanguageFileTests
{
    /// <summary>Every key of the source language, minus the notes with a leading underscore.</summary>
    private static IReadOnlyList<string> SourceKeys =>
        [.. Localizer.Load(Language.Default).Keys.Where(k => !k.StartsWith('_')).Order(StringComparer.Ordinal)];

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var language in Language.All)
        {
            data.Add(language.Code);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryLanguageKnowsEveryKey(string code)
    {
        var language = Language.Find(code)!;
        var localizer = Localizer.Load(language);

        var missing = SourceKeys.Where(k => !localizer.Has(k)).ToList();

        Assert.True(missing.Count == 0,
            $"{code}.json is missing {missing.Count} keys: {string.Join(", ", missing.Take(15))}");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void NoLanguageHasSurplusKeys(string code)
    {
        // A key the source language does not know is fetched nowhere - usually a
        // typo or a leftover from a feature that has been removed.
        var localizer = Localizer.Load(Language.Find(code)!);

        var surplus = localizer.Keys
            .Where(k => !k.StartsWith('_'))
            .Except(SourceKeys, StringComparer.Ordinal)
            .ToList();

        Assert.True(surplus.Count == 0,
            $"{code}.json holds unknown keys: {string.Join(", ", surplus)}");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void NoLanguageLeavesATextEmpty(string code)
    {
        var localizer = Localizer.Load(Language.Find(code)!);

        var empty = SourceKeys
            .Where(k => localizer.Has(k) && string.IsNullOrWhiteSpace(localizer[k]))
            .ToList();

        Assert.True(empty.Count == 0, $"{code}.json has empty texts: {string.Join(", ", empty)}");
    }

    /// <summary>
    /// Placeholders have to be the same in every language.
    /// </summary>
    /// <remarks>
    /// A "{2}" in a text that only receives two values throws a FormatException
    /// at runtime - and only once somebody using that language opens that
    /// particular window. The order may differ, sentence structure sometimes
    /// demands it; the set may not.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Languages))]
    public void ThePlaceholdersMatchTheSourceLanguage(string code)
    {
        var source = Localizer.Load(Language.Default);
        var localizer = Localizer.Load(Language.Find(code)!);

        var deviations = new List<string>();

        foreach (var key in SourceKeys)
        {
            if (!localizer.Has(key))
            {
                continue;
            }

            var expected = Placeholders(source[key]);
            var present = Placeholders(localizer[key]);

            if (!expected.SetEquals(present))
            {
                deviations.Add($"{key} (expected {Describe(expected)}, found {Describe(present)})");
            }
        }

        Assert.True(deviations.Count == 0,
            $"{code}.json has diverging placeholders: {string.Join("; ", deviations)}");
    }

    /// <summary>
    /// Every text <see cref="T"/> offers has to exist in the language file.
    /// </summary>
    /// <remarks>
    /// The localizer returns an unknown key itself. That is exactly how a typo
    /// shows: if the interface reads "settings.titel" instead of a heading, the
    /// entry is missing.
    /// </remarks>
    [Fact]
    public void EveryTextInTheAccessClassIsStored()
    {
        var missing = new List<string>();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.GetValue(null) is string value && LooksLikeAKey(value))
            {
                missing.Add($"{property.Name} -> {value}");
            }
        }

        Assert.True(missing.Count == 0,
            $"These texts are missing from the source language file: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// The same for the texts with placeholders - they are methods rather than
    /// properties, and the test above would overlook them.
    /// </summary>
    [Fact]
    public void EveryTextWithPlaceholdersIsStored()
    {
        var missing = new List<string>();

        foreach (var method in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(m => !m.IsSpecialName && m.ReturnType == typeof(string)))
        {
            var arguments = method.GetParameters().Select(SampleValue).ToArray();

            if (method.Invoke(null, arguments) is string value && LooksLikeAKey(value))
            {
                missing.Add($"{method.Name} -> {value}");
            }
        }

        Assert.True(missing.Count == 0,
            $"These texts are missing from the source language file: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryOfferedLanguageHasAFile()
    {
        var embedded = Localizer.EmbeddedLanguageCodes().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var without = Language.All.Where(l => !embedded.Contains(l.Code)).Select(l => l.Code).ToList();

        Assert.True(without.Count == 0,
            $"These languages are on offer but have no file: {string.Join(", ", without)}");
    }

    [Fact]
    public void EveryFileBelongsToAnOfferedLanguage()
    {
        var offered = Language.All.Select(l => l.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var without = Localizer.EmbeddedLanguageCodes().Where(c => !offered.Contains(c)).ToList();

        Assert.True(without.Count == 0,
            $"These files have no entry in Language.All: {string.Join(", ", without)}");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryLanguageKnowsACultureForNumbersAndDates(string code)
    {
        var culture = Language.Find(code)!.ToCulture();

        Assert.NotEqual(CultureInfo.InvariantCulture, culture);
    }

    private static bool LooksLikeAKey(string value) =>
        value.Contains('.', StringComparison.Ordinal)
        && !value.Contains(' ', StringComparison.Ordinal)
        && value.All(c => char.IsAsciiLetter(c) || c == '.');

    private static object SampleValue(ParameterInfo parameter) => parameter.ParameterType switch
    {
        var t when t == typeof(string) => "x",
        var t when t == typeof(int) => 1,
        var t when t == typeof(double) => 1d,
        var t when t == typeof(decimal) => 1m,
        var t when t == typeof(DateTimeOffset) => DateTimeOffset.UnixEpoch,
        _ => throw new NotSupportedException(
            $"The test knows no sample value for {parameter.ParameterType}.")
    };

    private static HashSet<string> Placeholders(string text)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < text.Length - 1; i++)
        {
            if (text[i] != '{' || !char.IsAsciiDigit(text[i + 1]))
            {
                continue;
            }

            // Read up to the closing brace; a format suffix such as "{0:0.#}"
            // belongs to the same placeholder and may differ per language - only
            // the number is counted.
            var end = text.IndexOf('}', i);
            if (end < 0)
            {
                continue;
            }

            var content = text[(i + 1)..end];
            var colon = content.IndexOf(':', StringComparison.Ordinal);
            found.Add(colon >= 0 ? content[..colon] : content);
            i = end;
        }

        return found;
    }

    /// <summary>
    /// No text carries the tell-tale signature of a double encoding.
    /// </summary>
    /// <remarks>
    /// It has happened: a tool read the files as UTF-8, took the replacement
    /// text as Latin-1 and wrote it out as UTF-8 again. Out of "PrÃ¼fsumme" came
    /// "PrÃÂ¼fsumme", in 93 lines across eight languages. Nothing broke - English
    /// is pure ASCII and stayed clean, every test was green, and the damage
    /// would have shown only to whoever ran the application in German.
    ///
    /// It is recognisable because the mangling leaves a UTF-8 lead byte
    /// (U+00C0 to U+00FF) followed by a continuation byte (U+0080 to U+00BF).
    /// That pair occurs in no real text: the continuation range holds control
    /// characters that no language uses.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Languages))]
    public void NoTextIsDoublyEncoded(string code)
    {
        var localizer = Localizer.Load(Language.Find(code)!);

        var damaged = localizer.Keys
            .Where(k => IsDoublyEncoded(localizer[k]))
            .ToList();

        Assert.True(damaged.Count == 0,
            $"{code}.json holds mangled text under: {string.Join(", ", damaged.Take(15))}");
    }

    private static bool IsDoublyEncoded(string text)
    {
        for (var i = 0; i < text.Length - 1; i++)
        {
            // Written as escapes: the continuation range holds control
            // characters that would be invisible in the source.
            if (text[i] >= '\u00c0' && text[i] <= '\u00ff'
                && text[i + 1] >= '\u0080' && text[i + 1] <= '\u00bf')
            {
                return true;
            }
        }

        return false;
    }

    private static string Describe(HashSet<string> placeholders) =>
        placeholders.Count == 0 ? "none" : string.Join("/", placeholders.Order(StringComparer.Ordinal));
}
