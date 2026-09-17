using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The user guide: one per language, with every picture it refers to.
/// </summary>
/// <remarks>
/// The about window builds the address of the guide from the language code, so a
/// missing file would be a dead link inside the application - in a language
/// nobody here reads, most likely. These tests are the counterpart to that
/// arithmetic.
/// </remarks>
public class GuideTests
{
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
    public void EveryLanguageHasAGuide(string code)
    {
        Assert.True(File.Exists(GuideFile(code)), $"docs/guide/{code}.md is missing.");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryPictureTheGuideRefersToExists(string code)
    {
        var guide = GuideFile(code);
        var folder = Path.GetDirectoryName(guide)!;

        var missing = Regex.Matches(File.ReadAllText(guide), @"!\[[^\]]*\]\(([^)]+)\)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Where(reference => !File.Exists(Path.GetFullPath(Path.Combine(folder, reference))))
            .ToList();

        Assert.True(missing.Count == 0,
            $"docs/guide/{code}.md refers to pictures that do not exist: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Every scene in every language - including the ones a guide happens not to
    /// show, so that a guide can grow without the pictures having to be made again.
    /// </summary>
    [Theory]
    [MemberData(nameof(Languages))]
    public void EverySceneHasBeenDrawnForEveryLanguage(string code)
    {
        var folder = Path.Combine(RepositoryRoot(), "docs", "guide", "images", code);

        var missing = GuideScreenshots.Scenes
            .Where(scene => !File.Exists(Path.Combine(folder, scene + ".png")))
            .ToList();

        Assert.True(missing.Count == 0,
            $"Pictures missing for {code}: {string.Join(", ", missing)}. "
            + "Set CUC_GUIDE_DIR and run GuideScreenshots.");
    }

    /// <summary>The guides link to each other; every one of those links has to lead somewhere.</summary>
    [Theory]
    [MemberData(nameof(Languages))]
    public void TheLanguagePickerAtTheTopLeadsToEveryOtherGuide(string code)
    {
        // The picker is the line under the title, not the title itself.
        var header = string.Join('\n', File.ReadLines(GuideFile(code)).Take(5));

        foreach (var other in Language.All.Where(l => l.Code != code))
        {
            Assert.Contains($"({other.Code}.md)", header, StringComparison.Ordinal);
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Languages))]
    public void TheAboutWindowLeadsToTheGuideOfTheChosenLanguage(string code)
    {
        var language = Language.Find(code)!;

        var address = AboutWindow.GuideAddress(App.RepositoryUri, language);

        Assert.Equal(
            $"https://github.com/sven-reichelt/Claude-UsageChecker/blob/main/docs/guide/{code}.md",
            address.ToString());
    }

    private static string GuideFile(string code) =>
        Path.Combine(RepositoryRoot(), "docs", "guide", code + ".md");

    /// <summary>
    /// The repository, found by walking up from the test assembly until the
    /// solution file appears - the build output lives inside it.
    /// </summary>
    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClaudeUsageChecker.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("The repository root was not found above the test assembly.");
    }
}
