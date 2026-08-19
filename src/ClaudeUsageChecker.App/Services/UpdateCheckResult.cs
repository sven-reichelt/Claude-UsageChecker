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

    /// <summary>Es gibt (noch) keine Veroeffentlichung, gegen die geprueft werden koennte.</summary>
    public static UpdateCheckResult Unavailable(string reason) => new()
    {
        Status = UpdateCheckStatus.Unavailable,
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
    Unavailable,
    Failed
}
