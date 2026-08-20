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
        Assert.True(Check(V(0, 5, 0), V(0, 6, 0)));
    }

    [Fact]
    public void ShouldShow_NotOnTheFirstStart()
    {
        // Someone who has only just installed the application needs no list of
        // what they have never seen.
        Assert.False(ReleaseHistory.ShouldShow(
            previous: null, V(0, 6, 0), isFirstInstall: true));
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
            previous: null, V(0, 6, 0), isFirstInstall: false));
    }

    [Fact]
    public void ShouldShow_NotForAnUnchangedVersion()
    {
        Assert.False(Check(V(0, 6, 0), V(0, 6, 0)));
    }

    [Fact]
    public void ShouldShow_NotForTheFourPartAssemblyVersionEither()
    {
        // Three parts are recorded while the assembly reports four. Without care
        // the summary would come back on every start.
        Assert.False(Check(V(0, 6, 0), new ProgramVersion(new Version(0, 6, 0, 0))));
    }

    [Fact]
    public void ShouldShow_NotOnAStepBack()
    {
        Assert.False(Check(V(0, 6, 0), V(0, 5, 0)));
    }

    /// <summary>
    /// Arriving at the finished version counts, even though the number has not
    /// moved.
    /// </summary>
    /// <remarks>
    /// This is what went missing on the real way out of a test build: the
    /// recorded value held three numbers and no label, so 0.7.1-beta.5 and the
    /// finished 0.7.1 left the same trace and the step between them was
    /// invisible. Whoever tested a build has reached the release now, and the
    /// entry describing it may well have grown since the first test build.
    /// </remarks>
    [Fact]
    public void ShouldShow_YesWhenAPreReleaseBecomesTheFinishedVersion()
    {
        Assert.True(Check(Beta(0, 7, 1, "beta.5"), V(0, 7, 1)));
    }

    /// <summary>
    /// Between two test builds of the same number it stays quiet.
    /// </summary>
    /// <remarks>
    /// The changelog has nothing new to say there - test builds get no entries
    /// of their own - and repeating the same page at every hop is noise.
    /// </remarks>
    [Fact]
    public void ShouldShow_NotBetweenTwoPreReleasesOfTheSameVersion()
    {
        Assert.False(Check(Beta(0, 7, 1, "beta.4"), Beta(0, 7, 1, "beta.5")));
    }

    /// <summary>A test build of the next version is a step forward all the same.</summary>
    [Fact]
    public void ShouldShow_YesForAPreReleaseOfAHigherVersion()
    {
        Assert.True(Check(V(0, 7, 1), Beta(0, 8, 0, "beta.1")));
    }

    /// <summary>And back onto a test build of what is already installed is not.</summary>
    [Fact]
    public void ShouldShow_NotOnAStepBackOntoAPreRelease()
    {
        Assert.False(Check(V(0, 7, 1), Beta(0, 7, 1, "beta.5")));
    }

    /// <summary>
    /// Shorthand for the regular case: a version is recorded, so the question of
    /// a first install is beside the point.
    /// </summary>
    private static bool Check(ProgramVersion previous, ProgramVersion current) =>
        ReleaseHistory.ShouldShow(previous, current, isFirstInstall: false);

    private static ProgramVersion V(int major, int minor, int build) =>
        new(new Version(major, minor, build));

    private static ProgramVersion Beta(int major, int minor, int build, string label) =>
        new(new Version(major, minor, build), label);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no version at all")]
    public void Parse_UnusableCountsAsNotRecorded(string? saved)
    {
        Assert.Null(ReleaseHistory.Parse(saved));
    }

    [Fact]
    public void Parse_ReadsARecordedVersion()
    {
        Assert.Equal(V(0, 5, 0), ReleaseHistory.Parse("0.5.0"));
    }

    /// <summary>
    /// Entries written by earlier versions carry three numbers and no label.
    /// </summary>
    [Fact]
    public void Parse_ReadsAnEntryFromBeforeTheLabelExisted()
    {
        var version = ReleaseHistory.Parse("0.6.0");

        Assert.NotNull(version);
        Assert.False(version.IsPreRelease);
    }

    [Fact]
    public void Format_WritesThreeParts()
    {
        Assert.Equal("0.6.0", ReleaseHistory.Format(new ProgramVersion(new Version(0, 6, 0, 0))));
    }

    /// <summary>The label is recorded too - without it the step out of a test build is lost.</summary>
    [Fact]
    public void Format_KeepsThePreReleaseLabel()
    {
        Assert.Equal("0.7.1-beta.5", ReleaseHistory.Format(Beta(0, 7, 1, "beta.5")));
    }

    /// <summary>
    /// The route the application takes at startup: record what runs, and compare
    /// against it next time.
    /// </summary>
    [Fact]
    public void RecordedVersion_CanBeReadBack()
    {
        var recorded = ReleaseHistory.Format(new ProgramVersion(new Version(0, 6, 0, 0)));

        Assert.False(Check(ReleaseHistory.Parse(recorded)!, V(0, 6, 0)));
        Assert.True(Check(ReleaseHistory.Parse(recorded)!, V(0, 6, 1)));
    }

    /// <summary>
    /// The same route out of a test build, through the settings file.
    /// </summary>
    /// <remarks>
    /// The pieces are right on their own; this checks the joint, because that is
    /// where the fault sat - what was written down could not express the
    /// difference the comparison was asked about.
    /// </remarks>
    [Fact]
    public void RecordedPreRelease_LeadsToTheSummaryOnTheFinishedVersion()
    {
        var recorded = ReleaseHistory.Format(Beta(0, 7, 1, "beta.5"));

        Assert.Equal("0.7.1-beta.5", recorded);
        Assert.True(Check(ReleaseHistory.Parse(recorded)!, V(0, 7, 1)));
    }
}
