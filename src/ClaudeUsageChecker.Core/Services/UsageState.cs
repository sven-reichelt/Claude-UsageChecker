using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>Zustand der Anwendung, wie er im Infobereich dargestellt wird.</summary>
public enum UsageStateKind
{
    /// <summary>Noch kein Abruf erfolgt.</summary>
    Initializing,

    /// <summary>Aktuelle Daten liegen vor.</summary>
    Ready,

    /// <summary>Letzter Abruf schlug fehl, es liegen aber noch aeltere Daten vor.</summary>
    Stale,

    /// <summary>Kein Token hinterlegt - Einrichtung noetig.</summary>
    NotConfigured,

    /// <summary>Token abgelaufen oder abgelehnt.</summary>
    AuthenticationFailed,

    /// <summary>Kein Abruf moeglich und keine verwertbaren Altdaten.</summary>
    Unavailable
}

/// <summary>
/// Unveraenderliche Momentaufnahme des Anwendungszustands.
/// </summary>
public sealed record UsageState
{
    public required UsageStateKind Kind { get; init; }

    /// <summary>Zuletzt erfolgreich abgerufene Daten, falls vorhanden.</summary>
    public UsageSnapshot? Snapshot { get; init; }

    /// <summary>Fehlerklasse des letzten Fehlschlags.</summary>
    public UsageApiFailure? Failure { get; init; }

    /// <summary>Für Menschen lesbare Fehlermeldung.</summary>
    public string? Message { get; init; }

    /// <summary>Zeitpunkt des naechsten geplanten Abrufs.</summary>
    public DateTimeOffset? NextPollAt { get; init; }

    public static UsageState Initializing { get; } = new() { Kind = UsageStateKind.Initializing };

    /// <summary>Ob es sich lohnt, Nutzungswerte anzuzeigen.</summary>
    public bool HasData => Snapshot is not null;
}
