using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Tests.Formatting;

/// <summary>
/// The sentences of a usage notice.
/// </summary>
/// <remarks>
/// Expected moments are derived rather than written out: the CI runs in
/// English and in another time zone, and "16:30" here is "4:30 PM" there.
/// </remarks>
public class AlertFormatterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToAlertExplanation_NamesTheThresholdTheUserSet()
    {
        var alert = Alert(UsageAlertLevel.Critical, threshold: 85);

        var text = UsageFormatter.ToAlertExplanation(alert);

        Assert.Contains("85 %", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToAlertExplanation_EveryStageHasASentenceOfItsOwn()
    {
        var sentences = new[] { UsageAlertLevel.Warning, UsageAlertLevel.Critical, UsageAlertLevel.Exhausted }
            .Select(level => UsageFormatter.ToAlertExplanation(Alert(level, threshold: 80)))
            .ToList();

        Assert.Equal(3, sentences.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ToAlertHeading_EveryStageHasAHeadingOfItsOwn()
    {
        var headings = new[] { UsageAlertLevel.Warning, UsageAlertLevel.Critical, UsageAlertLevel.Exhausted }
            .Select(UsageFormatter.ToAlertHeading)
            .ToList();

        Assert.Equal(3, headings.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ToAlertLabel_AModelSpecificLimitCarriesTheModelName()
    {
        var alert = Alert(UsageAlertLevel.Warning, threshold: 75) with
        {
            Limit = UsageAlertLimit.WeeklyModel,
            ModelName = "Fable"
        };

        Assert.Contains("Fable", UsageFormatter.ToAlertLabel(alert), StringComparison.Ordinal);
    }

    [Fact]
    public void ToAlertReset_SaysWhenAndHowLong()
    {
        var reset = Now.AddHours(2).AddMinutes(14);

        var text = UsageFormatter.ToAlertReset(new UsageWindow(80, reset), Now);

        Assert.Contains(DurationFormatter.ToResetMoment(reset, Now), text, StringComparison.Ordinal);
        Assert.Contains(DurationFormatter.ToCompact(reset - Now), text, StringComparison.Ordinal);
    }

    /// <summary>A notice left open past the reset says so instead of counting down to nothing.</summary>
    [Fact]
    public void ToAlertReset_AResetThatIsDueIsNamedAsSuch()
    {
        var reset = Now.AddMinutes(-3);

        var text = UsageFormatter.ToAlertReset(new UsageWindow(100, reset), Now);

        Assert.DoesNotContain(DurationFormatter.ToCompact(TimeSpan.Zero), text, StringComparison.Ordinal);
        Assert.Contains(DurationFormatter.ToResetMoment(reset, Now), text, StringComparison.Ordinal);
    }

    private static UsageAlert Alert(UsageAlertLevel level, double threshold) => new(
        UsageAlertLimit.Session, null, level, new UsageWindow(90, Now.AddHours(2)), threshold);
}
