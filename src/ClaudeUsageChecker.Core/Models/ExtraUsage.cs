namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// Zusatzkontingent ("extra usage"), sofern im Abo aktiviert.
/// </summary>
public sealed record ExtraUsage(
    bool IsEnabled,
    decimal? MonthlyLimit,
    decimal? UsedCredits,
    double? Utilization);
