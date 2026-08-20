using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App;
using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that the changelog appears in the language that is set.
/// </summary>
/// <remarks>
/// The translations depend on entries in the project file. If one goes or a
/// file moves, the application would silently show the English version - for
/// somebody who reads no English, a mute failure. Only a test notices that.
/// </remarks>
public class ChangelogLanguageTests : IDisposable
{
    private readonly Language _before = Localizer.Current.Language;

    public void Dispose()
    {
        Localizer.Use(_before);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var language in Language.All)
        {
            data.Add(language.Code);
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void EveryLanguageHasAChangelog(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var (text, istUebersetzt) = ChangelogResource.Read();

        Assert.NotEmpty(text);
        Assert.True(istUebersetzt,
            $"The changelog translation for {code} is missing "
            + "(docs/changelog/" + code + ".md); es erscheint die deutsche Release.");
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void EveryTranslationCanBeParsed(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var alle = ChangelogResource.All();

        Assert.NotEmpty(alle);
        Assert.All(alle, r => Assert.NotEmpty(r.Sections));
    }

    /// <summary>
    /// Every translation has to know the same versions as the English source -
    /// otherwise a language would be missing a whole entry without anyone
    /// noticing.
    /// </summary>
    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void EveryTranslationKnowsTheSameVersions(string code)
    {
        Localizer.Use(Language.Default);
        var deutsch = ChangelogResource.All().Select(r => r.Version).ToList();

        Localizer.Use(Language.Find(code)!);
        var translated = ChangelogResource.All().Select(r => r.Version).ToList();

        Assert.Equal(deutsch, translated);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void EveryTranslationKnowsTheRunningVersion(string code)
    {
        Localizer.Use(Language.Find(code)!);

        var laufende = App.CurrentVersion.Number;

        Assert.Contains(ChangelogResource.All(), r => r.Version == laufende);
    }
}
