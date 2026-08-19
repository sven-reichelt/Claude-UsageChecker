namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// Extra usage credits, where enabled on the subscription.
/// </summary>
public sealed record ExtraUsage(
    bool IsEnabled,
    decimal? MonthlyLimit,
    decimal? UsedCredits,
    double? Utilization);
