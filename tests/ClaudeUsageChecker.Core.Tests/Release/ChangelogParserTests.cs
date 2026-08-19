using ClaudeUsageChecker.Core.Release;

namespace ClaudeUsageChecker.Core.Tests.Release;

/// <summary>
/// Checks the reading of the changelog. The samples are modelled on the real
/// one, quirks included: an en dash before the date, wrapped bullet points,
/// indented follow-up paragraphs.
/// </summary>
public class ChangelogParserTests
{
    /// <summary>
    /// Deliberately the German changelog, although English is the source
    /// language: the parser has to cope with all nine translations, and this is
    /// where the non-ASCII characters get exercised - umlauts, the en dash
    /// before the date, quotation marks. An all-ASCII sample would let an
    /// encoding fault through unnoticed.
    /// </summary>
    private const string Sample = """
        # Änderungsverlauf

        Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/).

        ## [Unveröffentlicht]

        ## [0.5.0] – 2026-08-19

        ### Geändert
        - Der Zielort der Einrichtung ist jetzt
          `%LOCALAPPDATA%\Programs`.

          **Bereits eingerichtete Fassungen ziehen nicht um.** Sie laufen weiter.

        ## [0.4.2] – 2026-08-18

        ### Behoben
        - Ein Autostart-Eintrag zeigte auf den Download-Ordner.
        - Das **Abwählen** lässt die Anwendung, wo sie ist.

        ### Dokumentation
        - [SECURITY.md](SECURITY.md) listet auf, was wo abgelegt wird.
        """;

    [Fact]
    public void Parse_ReadsVersionsWithTheirDate()
    {
        var releases = ChangelogParser.Parse(Sample);

        Assert.Equal(2, releases.Count);
        Assert.Equal(new Version(0, 5, 0), releases[0].Version);
        Assert.Equal(new DateOnly(2026, 8, 19), releases[0].Date);
    }

    [Fact]
    public void Parse_SkipsTheUnreleasedSection()
    {
        var releases = ChangelogParser.Parse(Sample);

        Assert.DoesNotContain(releases, r => r.Version == new Version(0, 0));
    }

    [Fact]
    public void Parse_SeparatesTheSubsections()
    {
        var releases = ChangelogParser.Parse(Sample);
        var v042 = releases.Single(r => r.Version == new Version(0, 4, 2));

        Assert.Equal(["Behoben", "Dokumentation"], v042.Sections.Select(s => s.Title));
        Assert.Equal(2, v042.Sections[0].Entries.Count);
    }

    [Fact]
    public void Parse_JoinsWrappedLines()
    {
        var entry = ChangelogParser.Parse(Sample)[0].Sections[0].Entries[0];

        Assert.Equal(@"Der Zielort der Einrichtung ist jetzt %LOCALAPPDATA%\Programs.", entry.Text);
        Assert.False(entry.IsContinuation);
    }

    [Fact]
    public void Parse_RecognisesAFollowUpParagraphAsAContinuation()
    {
        var entries = ChangelogParser.Parse(Sample)[0].Sections[0].Entries;

        Assert.Equal(2, entries.Count);
        Assert.True(entries[1].IsContinuation);
        Assert.StartsWith("Bereits eingerichtete Fassungen", entries[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_StripsMarkup()
    {
        var releases = ChangelogParser.Parse(Sample);
        var behoben = releases.Single(r => r.Version == new Version(0, 4, 2)).Sections[0];
        var doku = releases.Single(r => r.Version == new Version(0, 4, 2)).Sections[1];

        Assert.Equal("Das Abwählen lässt die Anwendung, wo sie ist.", behoben.Entries[1].Text);
        Assert.Equal("SECURITY.md listet auf, was wo abgelegt wird.", doku.Entries[0].Text);
    }

    [Fact]
    public void Between_ReturnsOnlyTheVersionsInBetween()
    {
        var neu = ChangelogParser.Between(Sample, new Version(0, 4, 2), new Version(0, 5, 0));

        Assert.Single(neu);
        Assert.Equal(new Version(0, 5, 0), neu[0].Version);
    }

    [Fact]
    public void Between_TreatsTheFourPartAssemblyVersionAsTheSameRelease()
    {
        // Assembly.GetName().Version always has four parts, the changelog three.
        // Version counts the missing part as -1, so "0.5.0" is smaller than
        // "0.5.0.0" and drops out of the selection - which is exactly what is
        // wanted. The test pins that semantics down, so that a later rewrite to
        // comparisons of our own does not reverse it unnoticed.
        var neu = ChangelogParser.Between(Sample, new Version(0, 5, 0, 0), new Version(0, 5, 0, 0));

        Assert.Empty(neu);
    }

    [Fact]
    public void Between_SortsTheNewestVersionFirst()
    {
        var neu = ChangelogParser.Between(Sample, new Version(0, 4, 1), new Version(0, 5, 0));

        Assert.Equal([new Version(0, 5, 0), new Version(0, 4, 2)], neu.Select(r => r.Version));
    }

    [Fact]
    public void Between_DisregardsAnythingNewer()
    {
        // Someone starting an older release should not get to see the changes of
        // a later one.
        var neu = ChangelogParser.Between(Sample, new Version(0, 4, 1), new Version(0, 4, 2));

        Assert.Single(neu);
        Assert.Equal(new Version(0, 4, 2), neu[0].Version);
    }

    [Fact]
    public void Between_WithoutAPreviousVersionEverythingComesBack()
    {
        var neu = ChangelogParser.Between(Sample, after: null, new Version(0, 5, 0));

        Assert.Equal(2, neu.Count);
    }

    [Fact]
    public void Parse_CopesWithAnEmptyChangelog()
    {
        Assert.Empty(ChangelogParser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_SkipsVersionsWithoutContent()
    {
        var releases = ChangelogParser.Parse("## [1.0.0] – 2026-01-01\n\n## [0.9.0] – 2025-12-01\n\n### Behoben\n- Etwas.\n");

        Assert.Single(releases);
        Assert.Equal(new Version(0, 9, 0), releases[0].Version);
    }
}
