using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Every token source needs a label of its own. A catch-all would be harmful
/// here: the footer would then claim the wrong origin - which is exactly what
/// happened when the application's own sign-in was added.
/// </summary>
public class TokenSourceLabelTests
{
    [Theory]
    [InlineData(TokenSource.OAuth, "own sign-in")]
    [InlineData(TokenSource.SecretStore, "stored token")]
    [InlineData(TokenSource.Environment, "environment variable")]
    [InlineData(TokenSource.ClaudeCli, "Claude Code")]
    public void EverySourceGetsItsOwnLabel(TokenSource source, string expected) =>
        Assert.Equal(expected, DetailsWindow.SourceName(source));

    [Fact]
    public void NoTwoSourcesShareALabel()
    {
        var namen = Enum.GetValues<TokenSource>().Select(DetailsWindow.SourceName).ToList();

        Assert.Equal(namen.Count, namen.Distinct(StringComparer.Ordinal).Count());
    }
}
