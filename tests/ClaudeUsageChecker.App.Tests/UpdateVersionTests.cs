using ClaudeUsageChecker.App.Services;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the version comparison of the update check. A fault here would either
/// conceal an available version or keep announcing one that does not exist.
/// </summary>
public class UpdateVersionTests
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("V1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v0.1.0", "0.1.0")]
    [InlineData("v1.2.3+build7", "1.2.3")]
    [InlineData("v1.2", "1.2.0")]
    public void TryParseTag_RecognisesTheUsualTagForms(string tag, string expected)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(tag, out var version));
        Assert.Equal(expected, version.ToString());
    }

    /// <summary>The label is kept - it is what tells a pre-release apart.</summary>
    [Theory]
    [InlineData("v1.2.3-beta.1", "1.2.3", "beta.1")]
    [InlineData("v1.2.3-rc1+abc123", "1.2.3", "rc1")]
    public void TryParseTag_KeepsThePreReleaseLabel(string tag, string number, string label)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(tag, out var version));

        Assert.Equal(Version.Parse(number), version.Number);
        Assert.Equal(label, version.PreRelease);
        Assert.True(version.IsPreRelease);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("release")]
    [InlineData("v")]
    public void TryParseTag_RejectsUnusableTags(string? tag) =>
        Assert.False(GitHubReleaseUpdateService.TryParseTag(tag, out _));

    [Theory]
    [InlineData("v0.2.0", "v0.1.0", true)]
    [InlineData("v0.1.0", "v0.2.0", false)]
    [InlineData("v0.1.0", "v0.1.0", false)]
    [InlineData("v0.1.1", "v0.1.0", true)]
    [InlineData("v1.0.0", "v0.9.9", true)]
    // The finished version supersedes the pre-release of the same number. Without
    // this, whoever tested a pre-release would stay on it for good.
    [InlineData("v0.7.1", "v0.7.1-beta.1", true)]
    [InlineData("v0.7.1-beta.2", "v0.7.1-beta.1", true)]
    [InlineData("v0.7.1-beta.1", "v0.7.1", false)]
    [InlineData("v0.7.1-beta.1", "v0.7.1-beta.1", false)]
    // A pre-release of the next version is still newer than the current release.
    [InlineData("v0.8.0-beta.1", "v0.7.1", true)]
    public void VersionComparison_ReportsOnlyRealUpdates(
        string published, string installed, bool isNewer)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(published, out var latest));
        Assert.True(GitHubReleaseUpdateService.TryParseTag(installed, out var current));

        // This is the very comparison the service uses to decide "update
        // available".
        Assert.Equal(isNewer, latest > current);
    }

    /// <summary>
    /// Pre-release labels are counted, not spelled.
    /// </summary>
    /// <remarks>
    /// This was written off as a thought experiment - "nobody counts that far"
    /// - and then the tenth test build of a single day could not be offered to
    /// the person testing it: as text, "beta.10" sorts below "beta.9", because
    /// "1" comes before "9". The rules of semantic versioning apply now: a part
    /// made of digits compares as a number.
    /// </remarks>
    [Theory]
    [InlineData("v0.8.0-beta.10", "v0.8.0-beta.9", true)]
    [InlineData("v0.8.0-beta.9", "v0.8.0-beta.10", false)]
    [InlineData("v0.8.0-beta.2", "v0.8.0-beta.10", false)]
    [InlineData("v0.8.0-beta.11", "v0.8.0-beta.10", true)]
    // A label with more parts outranks the one it extends.
    [InlineData("v0.8.0-beta.1", "v0.8.0-beta", true)]
    [InlineData("v0.8.0-beta", "v0.8.0-beta.1", false)]
    // Text beats digits, so a release candidate outranks a beta.
    [InlineData("v0.8.0-rc.1", "v0.8.0-beta.99", true)]
    public void PreReleaseLabelsAreOrderedByNumberWhereTheyAreNumbers(
        string published, string installed, bool isNewer)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(published, out var latest));
        Assert.True(GitHubReleaseUpdateService.TryParseTag(installed, out var current));

        Assert.Equal(isNewer, latest > current);
    }

    [Theory]
    [InlineData(0, 2, 0, 0, "0.2.0")]
    [InlineData(1, 0, 0, 7, "1.0.0")]
    [InlineData(1, 2, 3, 0, "1.2.3")]
    public void VersionsAreShownWithThreeParts(int a, int b, int c, int d, string expected) =>
        // The fourth part comes from the assembly version and says nothing -
        // "Version 0.2.0.0 is up to date" is merely confusing.
        Assert.Equal(expected, new ProgramVersion(new Version(a, b, c, d)).ToString());

    [Fact]
    public void APreReleaseIsShownWithItsLabel() =>
        Assert.Equal("0.7.1-beta.1", new ProgramVersion(new Version(0, 7, 1), "beta.1").ToString());

    /// <summary>
    /// The label comes out of the informational version - the only one that can
    /// carry it.
    /// </summary>
    [Theory]
    [InlineData("0.7.1-beta.1+9a8b7c6", "0.7.1-beta.1")]
    [InlineData("0.7.1+9a8b7c6", "0.7.1")]
    [InlineData("0.7.1", "0.7.1")]
    public void TheInformationalVersionIsWhereTheLabelLives(string informational, string expected)
    {
        Assert.True(ProgramVersion.TryParse(informational, out var version));
        Assert.Equal(expected, version.ToString());
    }

    /// <summary>
    /// The running program knows its own version - and it is the one the build
    /// stamped in, not a default.
    /// </summary>
    [Fact]
    public void TheRunningVersionIsTheOneThatWasBuilt() =>
        Assert.NotEqual(new Version(0, 0, 0), ProgramVersion.Current.Number);

    /// <summary>
    /// A test build says so in words, not only through its label.
    /// </summary>
    /// <remarks>
    /// The label alone is easy to overlook, and whoever is offered a test build
    /// should know that is what it is before installing it.
    /// </remarks>
    [Fact]
    public void APreReleaseIsNamedAsOneWhenItIsUpToDate()
    {
        var beta = UpdateCheckResult.UpToDate(new ProgramVersion(new Version(0, 7, 1), "beta.1"));
        var finished = UpdateCheckResult.UpToDate(new ProgramVersion(new Version(0, 7, 1)));

        Assert.Equal(T.UpdateUpToDatePreRelease("0.7.1-beta.1"), beta.Message);
        Assert.Equal(T.UpdateUpToDate("0.7.1"), finished.Message);
        Assert.NotEqual(beta.Message, finished.Message);
    }
}
