namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// Vollstaendiger Nutzungsstand zu einem Zeitpunkt - das Ergebnis eines API-Abrufs.
/// </summary>
public sealed record UsageSnapshot
{
    /// <summary>Rollierendes 5-Stunden-Sitzungslimit.</summary>
    public required UsageWindow? Session { get; init; }

    /// <summary>Woechentliches Gesamtlimit (7 Tage).</summary>
    public required UsageWindow? Weekly { get; init; }

    /// <summary>Separates Wochenlimit fuer Opus-Modelle. Null, wenn im Abo nicht vorhanden.</summary>
    public UsageWindow? WeeklyOpus { get; init; }

    /// <summary>Separates Wochenlimit fuer Sonnet-Modelle. Null, wenn im Abo nicht vorhanden.</summary>
    public UsageWindow? WeeklySonnet { get; init; }

    /// <summary>Zusatzkontingent, sofern verfuegbar.</summary>
    public ExtraUsage? ExtraUsage { get; init; }

    /// <summary>Lokaler Zeitpunkt des erfolgreichen Abrufs.</summary>
    public required DateTimeOffset RetrievedAt { get; init; }
}
