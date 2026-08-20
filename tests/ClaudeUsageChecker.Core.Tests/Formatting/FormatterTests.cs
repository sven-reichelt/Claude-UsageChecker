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
    [InlineData(0, "now")]
    [InlineData(30, "1 min")]
    [InlineData(90, "2 min")]
    [InlineData(3600, "1 h")]
    [InlineData(8040, "2 h 14 min")]
    [InlineData(356400, "4 d 3 h")]
    [InlineData(345600, "4 d")]
    public void ToCompact_FormatsRemainingTimes(int seconds, string expected) =>
        Assert.Equal(expected, DurationFormatter.ToCompact(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void ToResetMoment_ShowsOnlyTheTimeOnTheSameDay()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddHours(2);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        Assert.Equal(reset.ToString("t", CultureInfo.InvariantCulture), text);
    }

    [Fact]
    public void ToResetMoment_NamesTheWeekdayWithinTheWeek()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddDays(3);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        var expectedDay = CultureInfo.InvariantCulture.DateTimeFormat
            .GetAbbreviatedDayName(reset.DayOfWeek);
        Assert.StartsWith(expectedDay, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shortest weekday name is a single letter in German: "S" would stand
    /// for Samstag as well as for Sonntag, "D" for Dienstag as well as for
    /// Donnerstag. Such a label would be worthless.
    /// </summary>
    [Fact]
    public void ToResetMoment_AvoidsAmbiguousWeekdayAbbreviations()
    {
        var german = new CultureInfo("de-DE");
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();

        var namesUsed = new List<string>();
        for (var offset = 1; offset <= 6; offset++)
        {
            var text = DurationFormatter.ToResetMoment(now.AddDays(offset), now, german);
            namesUsed.Add(text.Split(' ')[0]);
        }

        Assert.All(namesUsed, name => Assert.True(name.Length >= 2, $"'{name}' is too short."));
        Assert.Equal(namesUsed.Count, namesUsed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ToResetMoment_NamesTheDateFromAWeekOnwards()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddDays(10);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        Assert.Contains(reset.ToString("d", CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_ShowsSessionAndWeek()
    {
        var tooltip = UsageFormatter.ToTooltip(ReadyState(33, 13), Now);

        Assert.Contains("Session 33 %", tooltip, StringComparison.Ordinal);
        Assert.Contains("Week 13 %", tooltip, StringComparison.Ordinal);
    }

    /// <summary>The reset time is written in the culture that is set.</summary>
    /// <remarks>
    /// This test used to build its expectation with the invariant culture while
    /// the tooltip formats with the current one. On a German machine both write
    /// "07:14" and it passed by coincidence; the English CI runner writes
    /// "7:14 AM" and it failed there. Since a language change now sets the
    /// culture of the process, the format is a promise worth pinning down - and
    /// the English row is the one that would catch a fall back to the invariant
    /// culture.
    ///
    /// The time itself is derived rather than written out: it depends on the
    /// time zone of whoever runs the tests.
    /// </remarks>
    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void ToTooltip_WritesTheResetTimeInTheCurrentCulture(string code)
    {
        var culture = CultureInfo.GetCultureInfo(code);
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            var state = ReadyState(33, 13);
            var expected = state.Snapshot!.Session!.ResetsAt.ToLocalTime().ToString("t", culture);

            var tooltip = UsageFormatter.ToTooltip(state, Now);

            Assert.Contains(expected, tooltip, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void ToTooltip_StaysWithinTheWindowsLimit()
    {
        // Worst case: three-digit values and a distant reset that is printed as
        // a date rather than a weekday.
        var state = new UsageState
        {
            Kind = UsageStateKind.Stale,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(100, Now.AddDays(30)),
                Weekly = new UsageWindow(100, Now.AddDays(30)),
                RetrievedAt = Now
            },
            Message = "The Anthropic API is unreachable."
        };

        var tooltip = UsageFormatter.ToTooltip(state, Now);

        Assert.True(tooltip.Length <= UsageFormatter.WindowsTooltipMaxLength,
            $"Tooltip is {tooltip.Length} characters long: {tooltip}");
    }

    [Fact]
    public void ToTooltip_PointsOutTheMissingSignIn()
    {
        var state = new UsageState
        {
            Kind = UsageStateKind.NotConfigured,
            Failure = UsageApiFailure.NoToken
        };

        Assert.Contains("not signed in", UsageFormatter.ToTooltip(state, Now), StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_MarksOutdatedData()
    {
        var state = ReadyState(33, 13) with { Kind = UsageStateKind.Stale };

        Assert.Contains("outdated", UsageFormatter.ToTooltip(state, Now),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ToMenuLine_NamesPercentAndRemainingTimeWithoutTheClock()
    {
        var window = new UsageWindow(33, Now.AddHours(2).AddMinutes(14));

        var line = UsageFormatter.ToMenuLine("Session (5 h)", window, Now);

        Assert.Equal("Session (5 h): 33 % - 2 h 14 min left", line);
    }

    [Fact]
    public void EnumerateWindows_ReturnsOnlyReportedWindowsInAFixedOrder()
    {
        var snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(10, Now.AddHours(1)),
            Weekly = new UsageWindow(20, Now.AddDays(3)),
            ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(30, Now.AddDays(4)))],
            RetrievedAt = Now
        };

        var labels = UsageFormatter.EnumerateWindows(snapshot).Select(w => w.Label).ToList();

        Assert.Equal(["Session (5 h)", "Weekly total", "Weekly Fable"], labels);
    }

    [Fact]
    public void EnumerateWindows_CopesWithAMissingSnapshot() =>
        Assert.Empty(UsageFormatter.EnumerateWindows(null));

    [Fact]
    public void ToExtraUsageLine_ReturnsNullWhenNotActive()
    {
        Assert.Null(UsageFormatter.ToExtraUsageLine(null));
        Assert.Null(UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(IsEnabled: false, Used: 12m, Limit: 50m, Utilization: 24d)));
    }

    [Fact]
    public void ToExtraUsageLine_NamesUtilisationAndAmount()
    {
        var line = InEnglish(() => UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(IsEnabled: true, Used: 22.76m, Limit: 50m, Utilization: 46d,
                Currency: "EUR", Decimals: 2)));

        Assert.Equal("Extra usage: 46 % - 22.76 EUR of 50.00 EUR", line);
    }

    /// <summary>
    /// The currency comes from the account, not from the application.
    /// </summary>
    [Theory]
    [InlineData("USD", "12.50 USD")]
    [InlineData("BRL", "12.50 BRL")]
    public void ToExtraUsageLine_UsesTheCurrencyOfTheAccount(string currency, string expected)
    {
        var line = InEnglish(() => UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(IsEnabled: true, Used: 12.5m, Limit: null, Utilization: null,
                Currency: currency, Decimals: 2)));

        Assert.Equal($"Extra usage: {expected} used", line);
    }

    [Fact]
    public void ToExtraUsageLine_CopesWithPartialFigures()
    {
        var onlyUsed = InEnglish(() => UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(IsEnabled: true, Used: 12m, Limit: null, Utilization: null,
                Currency: "EUR", Decimals: 2)));

        Assert.Equal("Extra usage: 12.00 EUR used", onlyUsed);
    }

    [Fact]
    public void ToExtraUsageLine_ReportsActiveWithoutFigures()
    {
        var line = UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(IsEnabled: true, Used: null, Limit: null, Utilization: null));

        Assert.Equal("Extra usage: active", line);
    }

    /// <summary>
    /// Runs a piece of formatting under a fixed culture.
    /// </summary>
    /// <remarks>
    /// Amounts are written the way the interface language writes numbers, so a
    /// test that compares against a literal has to say which language it means.
    /// The machine here is German, the CI runner English; without this the same
    /// test passes in one place and fails in the other.
    /// </remarks>
    private static string? InEnglish(Func<string?> format)
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            return format();
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
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
