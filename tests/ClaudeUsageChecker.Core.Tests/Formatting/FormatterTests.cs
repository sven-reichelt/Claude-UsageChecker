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
    public void ToResetMoment_ZeigtAmSelbenTagNurDieUhrzeit()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddHours(2);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        Assert.Equal(reset.ToString("t", CultureInfo.InvariantCulture), text);
    }

    [Fact]
    public void ToResetMoment_NenntDenWochentagInnerhalbDerWoche()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddDays(3);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        var expectedDay = CultureInfo.InvariantCulture.DateTimeFormat
            .GetAbbreviatedDayName(reset.DayOfWeek);
        Assert.StartsWith(expectedDay, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der kuerzestmoegliche Wochentagsname ist im Deutschen ein einzelner
    /// Buchstabe: "S" stuende fuer Samstag wie fuer Sonntag, "D" fuer Dienstag
    /// wie fuer Donnerstag. Eine solche Angabe waere wertlos.
    /// </summary>
    [Fact]
    public void ToResetMoment_VerwendetKeineMehrdeutigeWochentagsabkuerzung()
    {
        var german = new CultureInfo("de-DE");
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();

        var namesUsed = new List<string>();
        for (var offset = 1; offset <= 6; offset++)
        {
            var text = DurationFormatter.ToResetMoment(now.AddDays(offset), now, german);
            namesUsed.Add(text.Split(' ')[0]);
        }

        Assert.All(namesUsed, name => Assert.True(name.Length >= 2, $"'{name}' ist zu kurz."));
        Assert.Equal(namesUsed.Count, namesUsed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ToResetMoment_NenntAbEinerWocheDasDatum()
    {
        var now = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero).ToLocalTime();
        var reset = now.AddDays(10);

        var text = DurationFormatter.ToResetMoment(reset, now, CultureInfo.InvariantCulture);

        Assert.Contains(reset.ToString("d", CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_ZeigtSitzungUndWoche()
    {
        var tooltip = UsageFormatter.ToTooltip(ReadyState(33, 13), Now, CultureInfo.InvariantCulture);

        Assert.Contains("Sitzung 33 %", tooltip, StringComparison.Ordinal);
        Assert.Contains("Woche 13 %", tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_EnthaeltDieResetUhrzeit()
    {
        var state = ReadyState(33, 13);
        var expected = DurationFormatter.ToResetMoment(
            state.Snapshot!.Session!.ResetsAt, Now, CultureInfo.InvariantCulture);

        var tooltip = UsageFormatter.ToTooltip(state, Now, CultureInfo.InvariantCulture);

        Assert.Contains(expected, tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void ToTooltip_BleibtInnerhalbDerWindowsGrenze()
    {
        // Ungünstigster Fall: dreistellige Werte und ein weit entfernter Reset,
        // der als Datum statt als Wochentag ausgegeben wird.
        var state = new UsageState
        {
            Kind = UsageStateKind.Stale,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(100, Now.AddDays(30)),
                Weekly = new UsageWindow(100, Now.AddDays(30)),
                RetrievedAt = Now
            },
            Message = "Die Anthropic-API ist nicht erreichbar."
        };

        var tooltip = UsageFormatter.ToTooltip(state, Now, CultureInfo.InvariantCulture);

        Assert.True(tooltip.Length <= UsageFormatter.WindowsTooltipMaxLength,
            $"Tooltip ist {tooltip.Length} Zeichen lang: {tooltip}");
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

    [Fact]
    public void ToMenuLine_NenntProzentUndRestzeitOhneUhrzeit()
    {
        var window = new UsageWindow(33, Now.AddHours(2).AddMinutes(14));

        var line = UsageFormatter.ToMenuLine("Sitzung (5 Std)", window, Now, CultureInfo.InvariantCulture);

        Assert.Equal("Sitzung (5 Std): 33 % - noch 2 Std 14 Min", line);
    }

    [Fact]
    public void EnumerateWindows_LiefertNurGemeldeteFensterInFesterReihenfolge()
    {
        var snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(10, Now.AddHours(1)),
            Weekly = new UsageWindow(20, Now.AddDays(3)),
            WeeklyOpus = null,
            WeeklySonnet = new UsageWindow(30, Now.AddDays(4)),
            RetrievedAt = Now
        };

        var labels = UsageFormatter.EnumerateWindows(snapshot).Select(w => w.Label).ToList();

        Assert.Equal(["Sitzung (5 Std)", "Woche gesamt", "Woche Sonnet"], labels);
    }

    [Fact]
    public void EnumerateWindows_KommtMitFehlendemSnapshotZurecht() =>
        Assert.Empty(UsageFormatter.EnumerateWindows(null));

    [Fact]
    public void ToExtraUsageLine_LiefertNullWennNichtAktiv()
    {
        Assert.Null(UsageFormatter.ToExtraUsageLine(null));
        Assert.Null(UsageFormatter.ToExtraUsageLine(new ExtraUsage(false, 50m, 12m, 24d)));
    }

    [Fact]
    public void ToExtraUsageLine_NenntAuslastungUndCredits()
    {
        var line = UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(true, 50m, 12m, 24d), CultureInfo.InvariantCulture);

        Assert.Equal("Zusatzkontingent: 24 % - 12.00 von 50.00 Credits", line);
    }

    [Fact]
    public void ToExtraUsageLine_KommtMitTeilangabenZurecht()
    {
        var onlyUsed = UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(true, null, 12m, null), CultureInfo.InvariantCulture);

        Assert.Equal("Zusatzkontingent: 12.00 Credits verbraucht", onlyUsed);
    }

    [Fact]
    public void ToExtraUsageLine_MeldetAktivOhneZahlen()
    {
        var line = UsageFormatter.ToExtraUsageLine(
            new ExtraUsage(true, null, null, null), CultureInfo.InvariantCulture);

        Assert.Equal("Zusatzkontingent: aktiv", line);
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
