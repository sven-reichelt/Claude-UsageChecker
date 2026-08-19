using System.Globalization;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Tests.Formatting;

public class FormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 5, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "gleich")]
    [InlineData(30, "1 Min")]
    [InlineData(90, "2 Min")]
    [InlineData(3600, "1 Std")]
    [InlineData(8040, "2 Std 14 Min")]
    [InlineData(356400, "4 Tg 3 Std")]
    [InlineData(345600, "4 Tg")]
    public void ToCompact_FormatiertRestzeiten(int seconds, string expected) =>
        Assert.Equal(expected, DurationFormatter.ToCompact(
            TimeSpan.FromSeconds(seconds), CultureInfo.InvariantCulture));

    [Fact]
    public void ToTooltip_ZeigtSitzungUndWoche()
    {
        var state = ReadyState(33, 13);

        var tooltip = UsageFormatter.ToTooltip(state, Now, CultureInfo.InvariantCulture);

        Assert.Contains("Sitzung 33 %", tooltip, StringComparison.Ordinal);
        Assert.Contains("Woche 13 %", tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_BleibtInnerhalbDerWindowsGrenze()
    {
        var tooltip = UsageFormatter.ToTooltip(ReadyState(99.9, 88.8), Now, CultureInfo.InvariantCulture);

        Assert.True(tooltip.Length <= UsageFormatter.WindowsTooltipMaxLength,
            $"Tooltip ist {tooltip.Length} Zeichen lang.");
    }

    [Fact]
    public void ToTooltip_WeistAufFehlendesTokenHin()
    {
        var state = new UsageState
        {
            Kind = UsageStateKind.NotConfigured,
            Failure = UsageApiFailure.NoToken
        };

        Assert.Contains("kein Token", UsageFormatter.ToTooltip(state, Now), StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_MarkiertVeralteteDaten()
    {
        var state = ReadyState(33, 13) with { Kind = UsageStateKind.Stale };

        Assert.Contains("veraltet", UsageFormatter.ToTooltip(state, Now, CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static UsageState ReadyState(double session, double weekly) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(session, Now.AddHours(2).AddMinutes(14)),
            Weekly = new UsageWindow(weekly, Now.AddDays(4).AddHours(3)),
            RetrievedAt = Now
        }
    };
}
