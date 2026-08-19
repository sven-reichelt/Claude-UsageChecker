using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the decision of when the summary of changes is due.
/// </summary>
public class ReleaseHistoryTests
{
    [Fact]
    public void ShouldShow_YesAfterAnUpdate()
    {
        Assert.True(Check(new Version(0, 5, 0), new Version(0, 6, 0)));
    }

    [Fact]
    public void ShouldShow_NotOnTheFirstStart()
    {
        // Someone who has only just installed the application needs no list of
        // what they have never seen.
        Assert.False(ReleaseHistory.ShouldShow(
            previous: null, new Version(0, 6, 0), isFirstInstall: true));
    }

    /// <summary>
    /// The transitional case: the previous version did not know the field yet,
    /// but the application has run - recognisable from the settings file.
    /// </summary>
    /// <remarks>
    /// Without this branch the very version introducing the summary would show
    /// none: anyone updating from an older one has nothing recorded in the file.
    /// </remarks>
    [Fact]
    public void ShouldShow_YesWithoutARecordedVersionButWithASettingsFile()
    {
        Assert.True(ReleaseHistory.ShouldShow(
            previous: null, new Version(0, 6, 0), isFirstInstall: false));
    }

    [Fact]
    public void ShouldShow_NotForAnUnchangedVersion()
    {
        Assert.False(Check(new Version(0, 6, 0), new Version(0, 6, 0)));
    }

    [Fact]
    public void ShouldShow_NotForTheFourPartAssemblyVersionEither()
    {
        // Three parts are recorded while the assembly reports four. Without care
        // the summary would come back on every start.
        Assert.False(Check(new Version(0, 6, 0), new Version(0, 6, 0, 0)));
    }

    [Fact]
    public void ShouldShow_NotOnAStepBack()
    {
        Assert.False(Check(new Version(0, 6, 0), new Version(0, 5, 0)));
    }

    /// <summary>
    /// Shorthand for the regular case: a version is recorded, so the question of
    /// a first install is beside the point.
    /// </summary>
    private static bool Check(Version previous, Version current) =>
        ReleaseHistory.ShouldShow(previous, current, isFirstInstall: false);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("keine Version")]
    public void Parse_UnusableCountsAsNotRecorded(string? saved)
    {
        Assert.Null(ReleaseHistory.Parse(saved));
    }

    [Fact]
    public void Parse_ReadsARecordedVersion()
    {
        Assert.Equal(new Version(0, 5, 0), ReleaseHistory.Parse("0.5.0"));
    }

    [Fact]
    public void Format_WritesThreeParts()
    {
        Assert.Equal("0.6.0", ReleaseHistory.Format(new Version(0, 6, 0, 0)));
    }

    /// <summary>
    /// The route the application takes at startup: record what runs, and compare
    /// against it next time.
    /// </summary>
    [Fact]
    public void RecordedVersion_CanBeReadBack()
    {
        var recorded = ReleaseHistory.Format(new Version(0, 6, 0, 0));

        Assert.False(Check(ReleaseHistory.Parse(recorded)!, new Version(0, 6, 0, 0)));
        Assert.True(Check(ReleaseHistory.Parse(recorded)!, new Version(0, 6, 1, 0)));
    }
}
