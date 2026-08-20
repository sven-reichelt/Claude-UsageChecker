using System.Globalization;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Tests.Formatting;

/// <summary>
/// Checks what is written about a window whose reset has already fallen due.
/// </summary>
/// <remarks>
/// <para>
/// This case has its own sentences, and the reason is a wart from the early
/// days: the building block for a duration was simply pushed into a slot that
/// expects one. Out came "Session: 39 % - now left" in English and "noch
/// gleich" in German - no sentence in any of the nine languages.
/// </para>
/// <para>
/// It shows only between the moment a window runs out and the next call, which
/// is up to the polling interval, so nobody had ever caught it in the act.
/// </para>
/// </remarks>
public class DueWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The duration building block appears in no sentence any more.</summary>
    /// <remarks>
    /// Deliberately checked against the word rather than against the whole line:
    /// what matters is not how the new sentence reads but that the old one is
    /// gone. In every language, because that is where it was wrong.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Languages))]
    public void NoSentenceUsesTheDurationBlockForADueWindow(string code)
    {
        var before = Localizer.Current.Language;
        Localizer.Use(Language.Find(code)!);
        try
        {
            var window = new UsageWindow(39, Now.AddMinutes(-5));
            var now = T.DurationNow;

            Assert.DoesNotContain(now, UsageFormatter.ToMenuLine("Session", window, Now), StringComparison.Ordinal);
            Assert.DoesNotContain(now, UsageFormatter.ToDetailLine("Session", window, Now), StringComparison.Ordinal);
            Assert.DoesNotContain(now, UsageFormatter.ToTooltip(Ready(window), Now), StringComparison.Ordinal);
        }
        finally
        {
            Localizer.Use(before);
        }
    }

    [Fact]
    public void TheMenuLineSaysTheResetIsDue()
    {
        var line = UsageFormatter.ToMenuLine("Session", new UsageWindow(39, Now.AddMinutes(-5)), Now);

        Assert.Equal("Session: 39 % - reset due", line);
    }

    [Fact]
    public void TheDetailLineNamesTheMomentItWasDue()
    {
        var line = UsageFormatter.ToDetailLine("Session", new UsageWindow(39, Now.AddMinutes(-5)), Now);

        Assert.StartsWith("Session: 39 % used - reset due (was ", line, StringComparison.Ordinal);
    }

    /// <summary>A window that still has time keeps the sentence it always had.</summary>
    [Fact]
    public void AWindowWithTimeLeftIsUnchanged()
    {
        var line = UsageFormatter.ToMenuLine("Session", new UsageWindow(39, Now.AddHours(2)), Now);

        Assert.Equal("Session: 39 % - 2 h left", line);
    }

    /// <summary>
    /// Exactly at the moment of the reset the window already counts as due.
    /// </summary>
    /// <remarks>
    /// There is no remaining time to report at zero, and rounding it up to "1
    /// min" would be an invention.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-600)]
    public void TheBoundaryCountsAsDue(int seconds) =>
        Assert.True(DurationFormatter.IsDue(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void AnythingLeftIsNotDue(int seconds) =>
        Assert.False(DurationFormatter.IsDue(TimeSpan.FromSeconds(seconds)));

    /// <summary>The tooltip keeps its length limit in the due case too.</summary>
    [Theory]
    [MemberData(nameof(Languages))]
    public void TheTooltipStaysWithinTheWindowsLimit(string code)
    {
        var before = Localizer.Current.Language;
        Localizer.Use(Language.Find(code)!);
        try
        {
            var tooltip = UsageFormatter.ToTooltip(Ready(new UsageWindow(99.9, Now.AddDays(-3))), Now);

            Assert.True(tooltip.Length <= UsageFormatter.WindowsTooltipMaxLength,
                $"{code}: {tooltip.Length} characters, {UsageFormatter.WindowsTooltipMaxLength} allowed.");
        }
        finally
        {
            Localizer.Use(before);
        }
    }

    public static TheoryData<string> Languages()
    {
        var data = new TheoryData<string>();
        foreach (var language in Language.All)
        {
            data.Add(language.Code);
        }

        return data;
    }

    private static UsageState Ready(UsageWindow window) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = window,
            Weekly = window,
            RetrievedAt = Now
        }
    };
}
