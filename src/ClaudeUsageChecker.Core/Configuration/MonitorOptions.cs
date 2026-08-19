namespace ClaudeUsageChecker.Core.Configuration;

/// <summary>Einstellungen der Abrufschleife.</summary>
public sealed class MonitorOptions
{
    /// <summary>
    /// Untergrenze des Abrufintervalls. Der Endpunkt drosselt aggressiv;
    /// 180 Sekunden gelten als sicher. Kleinere Werte werden hochgesetzt.
    /// </summary>
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(180);

    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);

    /// <summary>Regulaeres Abrufintervall. Wird nie unter MinimumInterval gesetzt.</summary>
    public TimeSpan PollInterval
    {
        get => _pollInterval;
        init => _pollInterval = value < MinimumInterval ? MinimumInterval : value;
    }

    /// <summary>Erste Wartezeit nach einem Fehlschlag; verdoppelt sich bis MaxBackoff.</summary>
    public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Obergrenze der Wartezeit nach wiederholten Fehlschlaegen.</summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Ab dieser Auslastung gilt ein Fenster als kritisch (Warnsymbol).</summary>
    public double CriticalThreshold { get; init; } = 90d;

    /// <summary>Ab dieser Auslastung gilt ein Fenster als angespannt.</summary>
    public double WarningThreshold { get; init; } = 75d;
}
