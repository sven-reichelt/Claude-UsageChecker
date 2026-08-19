using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks the version comparison of the update check. A fault here would either
/// conceal an available version or permanently announce one that
/// vermeintlich neue melden.
/// </summary>
public class UpdateVersionTests
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("V1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("v0.1.0", "0.1.0")]
    [InlineData("v1.2.3-beta.1", "1.2.3")]
    [InlineData("v1.2.3+build7", "1.2.3")]
    [InlineData("v1.2", "1.2")]
    public void TryParseTag_RecognisesTheUsualTagForms(string tag, string expected)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(tag, out var version));
        Assert.Equal(Version.Parse(expected), version);
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
    public void VersionComparison_ReportsOnlyRealUpdates(
        string veroeffentlicht, string installiert, bool istNeuer)
    {
        Assert.True(GitHubReleaseUpdateService.TryParseTag(veroeffentlicht, out var latest));
        Assert.True(GitHubReleaseUpdateService.TryParseTag(installiert, out var current));

        // Genau diese Bedingung entscheidet im Dienst ueber "Update available".
        Assert.Equal(istNeuer, latest > current);
    }

    [Theory]
    [InlineData(0, 2, 0, 0, "0.2.0")]
    [InlineData(1, 0, 0, 7, "1.0.0")]
    [InlineData(1, 2, 3, 0, "1.2.3")]
    public void VersionsAreShownWithThreeParts(int a, int b, int c, int d, string expected) =>
        // The fourth part comes from the assembly version and says nothing -
        // "Version 0.2.0.0 is up to date" is merely confusing.
        Assert.Equal(expected, UpdateCheckResult.Display(new Version(a, b, c, d)));

    [Fact]
    public void TwoPartVersionsStayUnchanged() =>
        Assert.Equal("1.2", UpdateCheckResult.Display(new Version(1, 2)));
}
