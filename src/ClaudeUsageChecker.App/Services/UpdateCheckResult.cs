using System;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Ergebnis einer Aktualisierungspruefung.</summary>
public sealed record UpdateCheckResult
{
    public required UpdateCheckStatus Status { get; init; }

    /// <summary>Version der gefundenen Aktualisierung.</summary>
    public Version? AvailableVersion { get; init; }

    /// <summary>Seite der Veroeffentlichung zum manuellen Herunterladen.</summary>
    public Uri? ReleasePage { get; init; }

    /// <summary>Erlaeuternder Text fuer die Oberflaeche.</summary>
    public string? Message { get; init; }

    public static UpdateCheckResult UpToDate(Version current) => new()
    {
        Status = UpdateCheckStatus.UpToDate,
        AvailableVersion = current,
        Message = $"Version {current} ist aktuell."
    };

    public static UpdateCheckResult Disabled(string reason) => new()
    {
        Status = UpdateCheckStatus.Disabled,
        Message = reason
    };

    public static UpdateCheckResult Failed(string message) => new()
    {
        Status = UpdateCheckStatus.Failed,
        Message = message
    };
}

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    Disabled,
    Failed
}
