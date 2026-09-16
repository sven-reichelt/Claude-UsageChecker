using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>How far a single limit has come, measured against the thresholds.</summary>
/// <remarks>
/// Ordered on purpose: a higher value is a later stage, and the tracker compares
/// them as numbers.
/// </remarks>
public enum UsageAlertLevel
{
    Normal = 0,
    Warning = 1,
    Critical = 2,
    Exhausted = 3
}

/// <summary>Which limit a notice is about.</summary>
public enum UsageAlertLimit
{
    Session,
    Weekly,
    WeeklyModel
}

/// <summary>
/// A limit that has just reached a stage worth telling the user about.
/// </summary>
/// <param name="Limit">Which kind of limit.</param>
/// <param name="ModelName">The model name for a model-specific weekly limit, as the API reports it.</param>
/// <param name="Level">The stage that was reached.</param>
/// <param name="Window">The figures at the moment it was reached.</param>
/// <param name="Threshold">The threshold that was passed, in percent - 100 for a limit used up.</param>
public sealed record UsageAlert(
    UsageAlertLimit Limit,
    string? ModelName,
    UsageAlertLevel Level,
    UsageWindow Window,
    double Threshold)
{
    /// <summary>
    /// Identifies the limit independently of the language, so that a notice
    /// about the same limit replaces the earlier one instead of joining it.
    /// </summary>
    public string Key => UsageAlertTracker.KeyFor(Limit, ModelName);
}

/// <summary>
/// When a notice is due: the thresholds of the icon, and which stages the user
/// wants to hear about.
/// </summary>
public sealed record UsageAlertRules(
    double WarningThreshold,
    double CriticalThreshold,
    bool OnWarning = true,
    bool OnCritical = true,
    bool OnExhausted = true)
{
    /// <summary>Whether a notice is wanted for this stage.</summary>
    public bool IsEnabled(UsageAlertLevel level) => level switch
    {
        UsageAlertLevel.Warning => OnWarning,
        UsageAlertLevel.Critical => OnCritical,
        UsageAlertLevel.Exhausted => OnExhausted,
        _ => false
    };

    /// <summary>The threshold that belongs to a stage, in percent.</summary>
    public double ThresholdOf(UsageAlertLevel level) => level switch
    {
        UsageAlertLevel.Warning => WarningThreshold,
        UsageAlertLevel.Critical => CriticalThreshold,
        UsageAlertLevel.Exhausted => 100d,
        _ => 0d
    };

    /// <summary>The stage a utilisation stands at.</summary>
    /// <remarks>
    /// Used up wins over red: with the critical threshold at 100, the two would
    /// otherwise be the same moment reported as the lesser of them.
    /// </remarks>
    public UsageAlertLevel LevelOf(double utilization) => utilization switch
    {
        >= 100d => UsageAlertLevel.Exhausted,
        var u when u >= CriticalThreshold => UsageAlertLevel.Critical,
        var u when u >= WarningThreshold => UsageAlertLevel.Warning,
        _ => UsageAlertLevel.Normal
    };
}

/// <summary>
/// What has already been reported for one limit, and until when that holds.
/// </summary>
/// <param name="Level">The stage the limit stood at when it was last looked at.</param>
/// <param name="ResetsAt">
/// The end of the period this applies to. Once it has passed, the limit has
/// started over, and whatever was reported before no longer counts.
/// </param>
public sealed record UsageAlertMark(UsageAlertLevel Level, DateTimeOffset ResetsAt);
