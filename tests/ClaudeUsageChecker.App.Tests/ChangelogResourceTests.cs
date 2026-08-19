using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App;
using ClaudeUsageChecker.App.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that the changelog really is inside the program and can be read.
/// </summary>
/// <remarks>
/// The resource depends on an entry in the project file. If that entry goes or
/// the file moves, the summary of changes would stay empty - without anything
/// failing. Only a test notices that.
/// </remarks>
public class ChangelogResourceTests
{
    [AvaloniaFact]
    public void TheChangelogIsBundledIn()
    {
        Assert.NotEmpty(ChangelogResource.Read().Text);
    }

    [AvaloniaFact]
    public void TheChangelogCanBeParsed()
    {
        var alle = ChangelogResource.All();

        Assert.NotEmpty(alle);
        Assert.All(alle, r => Assert.NotEmpty(r.Sections));
    }

    /// <summary>
    /// Forces the changelog to be maintained together with the version number.
    /// </summary>
    /// <remarks>
    /// Without an entry for the running version, the application would show an
    /// empty window after the update - the worst conceivable moment to notice
    /// that the changelog was forgotten. Whoever raises the version in
    /// Directory.Build.props therefore adds to CHANGELOG.md first.
    /// </remarks>
    [AvaloniaFact]
    public void TheChangelogKnowsTheRunningVersion()
    {
        var laufende = ReleaseHistory.ThreePart(App.CurrentVersion);

        Assert.Contains(ChangelogResource.All(), r => r.Version == laufende);
    }

    [AvaloniaFact]
    public void BetweenTwoVersionsOnlyWhatLiesBetweenComesBack()
    {
        var alle = ChangelogResource.All();
        if (alle.Count < 2)
        {
            return;
        }

        var vorletzte = alle[1].Version;
        var neueste = alle[0].Version;

        var changes = ChangelogResource.Between(vorletzte, neueste);

        Assert.Single(changes);
        Assert.Equal(neueste, changes[0].Version);
    }
}
