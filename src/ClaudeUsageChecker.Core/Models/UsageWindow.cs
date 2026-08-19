namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// Ein einzelnes Nutzungsfenster (z. B. die 5-Stunden-Sitzung oder das 7-Tage-Limit).
/// </summary>
/// <param name="Utilization">Verbrauchsgrad in Prozent (0-100).</param>
/// <param name="ResetsAt">Zeitpunkt, zu dem das Fenster zurueckgesetzt wird (UTC).</param>
public sealed record UsageWindow(double Utilization, DateTimeOffset ResetsAt)
{
    /// <summary>Verbleibende Zeit bis zum Reset, bezogen auf <paramref name="now"/>. Nie negativ.</summary>
    public TimeSpan TimeUntilReset(DateTimeOffset now)
    {
        var remaining = ResetsAt - now;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    /// <summary>Verbleibender Anteil in Prozent (0-100).</summary>
    public double Remaining => Math.Clamp(100d - Utilization, 0d, 100d);

    /// <summary>Das Fenster ist ausgeschoepft.</summary>
    public bool IsExhausted => Utilization >= 100d;
}
