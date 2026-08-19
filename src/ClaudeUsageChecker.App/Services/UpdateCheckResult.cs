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

    /// <summary>Die ausfuehrbare Datei der neuen Fassung.</summary>
    public Uri? DownloadUrl { get; init; }

    /// <summary>
    /// Die zugehoerige Pruefsummendatei. Ohne sie wird nichts eingespielt -
    /// heruntergeladener Code, dessen Echtheit niemand belegt, wird nicht
    /// ausgefuehrt.
    /// </summary>
    public Uri? ChecksumUrl { get; init; }

    /// <summary>Erlaeuternder Text fuer die Oberflaeche.</summary>
    public string? Message { get; init; }

    /// <summary>Ob genug vorliegt, um die neue Fassung selbst einzuspielen.</summary>
    public bool CanInstall =>
        Status == UpdateCheckStatus.UpdateAvailable && DownloadUrl is not null && ChecksumUrl is not null;

    public static UpdateCheckResult UpToDate(Version current) => new()
    {
        Status = UpdateCheckStatus.UpToDate,
        AvailableVersion = current,
        Message = $"Version {Anzeigen(current)} ist aktuell."
    };

    /// <summary>
    /// Kuerzt auf drei Stellen. Assembly-Versionen haben immer vier, deren
    /// letzte hier nichts aussagt - "0.2.0.0" verwirrt nur.
    /// </summary>
    internal static string Anzeigen(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.Build >= 0 ? version.ToString(3) : version.ToString();
    }

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
