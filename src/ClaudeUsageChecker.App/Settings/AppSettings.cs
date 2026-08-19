using System;
using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.App.Settings;

/// <summary>
/// Benutzereinstellungen. Enthaelt bewusst keinerlei Geheimnisse -
/// das Token liegt ausschliesslich im Secret-Store des Betriebssystems.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Abrufintervall in Sekunden. Wird auf mindestens 180 angehoben.</summary>
    [JsonPropertyName("pollIntervalSeconds")]
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Anwendung mit Windows starten.</summary>
    [JsonPropertyName("launchAtLogin")]
    public bool LaunchAtLogin { get; set; }

    /// <summary>Beim Start automatisch auf Aktualisierungen pruefen.</summary>
    [JsonPropertyName("checkForUpdates")]
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Warnschwelle in Prozent.</summary>
    [JsonPropertyName("warningThreshold")]
    public double WarningThreshold { get; set; } = 75d;

    /// <summary>Kritische Schwelle in Prozent.</summary>
    [JsonPropertyName("criticalThreshold")]
    public double CriticalThreshold { get; set; } = 90d;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(PollIntervalSeconds, 180));
}
