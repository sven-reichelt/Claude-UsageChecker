using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Jede Tokenquelle braucht eine eigene Beschriftung. Ein Sammelfall waere hier
/// schaedlich: Die Fusszeile wuerde dann eine falsche Herkunft behaupten - genau
/// das ist bei der neu hinzugekommenen eigenen Anmeldung passiert.
/// </summary>
public class TokenSourceLabelTests
{
    [Theory]
    [InlineData(TokenSource.OAuth, "eigene Anmeldung")]
    [InlineData(TokenSource.SecretStore, "hinterlegtes Token")]
    [InlineData(TokenSource.Environment, "Umgebungsvariable")]
    [InlineData(TokenSource.ClaudeCli, "Claude Code")]
    public void JedeQuelleBekommtIhreEigeneBeschriftung(TokenSource source, string erwartet) =>
        Assert.Equal(erwartet, DetailsWindow.QuellenName(source));

    [Fact]
    public void KeineZweiQuellenTeilenSichEineBeschriftung()
    {
        var namen = Enum.GetValues<TokenSource>().Select(DetailsWindow.QuellenName).ToList();

        Assert.Equal(namen.Count, namen.Distinct(StringComparer.Ordinal).Count());
    }
}
