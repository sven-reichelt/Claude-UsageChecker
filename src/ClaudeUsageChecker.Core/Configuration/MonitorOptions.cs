namespace ClaudeUsageChecker.Core.Configuration;

/// <summary>Settings of the polling loop.</summary>
public sealed class MonitorOptions
{
    /// <summary>
    /// Lower bound of the polling interval. The endpoint throttles aggressively;
    /// 180 seconds is considered safe. Smaller values are raised to it.
    /// </summary>
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(180);

    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    /// <summary>Regular polling interval. Never set below MinimumInterval.</summary>
    public TimeSpan PollInterval
    {
        get => _pollInterval;
        init => _pollInterval = value < MinimumInterval ? MinimumInterval : value;
    }

    /// <summary>First wait after a failure; doubles up to MaxBackoff.</summary>
    public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound of the wait after repeated failures.</summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(30);

    // The warning and critical thresholds used to live here. The monitor never
    // read them: it fetches values, it does not judge them. Judging happens in
    // exactly one place - in TrayIconSeverityResolver, from the user settings.
    // Two places for the same setting would be an invitation to later turn the
    // wrong one.
}
