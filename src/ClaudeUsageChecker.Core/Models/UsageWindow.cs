namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// A single usage window (the five-hour session or the seven-day limit).
/// </summary>
/// <param name="Utilization">Consumed share in percent (0-100).</param>
/// <param name="ResetsAt">When the window resets (UTC).</param>
public sealed record UsageWindow(double Utilization, DateTimeOffset ResetsAt)
{
    /// <summary>Time left until the reset, relative to <paramref name="now"/>. Never negative.</summary>
    public TimeSpan TimeUntilReset(DateTimeOffset now)
    {
        var remaining = ResetsAt - now;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>Remaining share in percent (0-100).</summary>
    public double Remaining => Math.Clamp(100d - Utilization, 0d, 100d);

    /// <summary>The window is used up.</summary>
    public bool IsExhausted => Utilization >= 100d;
}
