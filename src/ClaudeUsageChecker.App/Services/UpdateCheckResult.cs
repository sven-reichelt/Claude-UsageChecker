using System;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Result of an update check.</summary>
public sealed record UpdateCheckResult
{
    public required UpdateCheckStatus Status { get; init; }

    /// <summary>Version of the update that was found.</summary>
    public Version? AvailableVersion { get; init; }

    /// <summary>Release page for downloading by hand.</summary>
    public Uri? ReleasePage { get; init; }

    /// <summary>The executable of the new version.</summary>
    public Uri? DownloadUrl { get; init; }

    /// <summary>
    /// The matching checksum file. Without it nothing is installed - downloaded
    /// code whose authenticity nobody vouches for is not executed.
    /// </summary>
    public Uri? ChecksumUrl { get; init; }

    /// <summary>Explanatory text for the interface.</summary>
    public string? Message { get; init; }

    /// <summary>Whether there is enough to install the new version ourselves.</summary>
    public bool CanInstall =>
        Status == UpdateCheckStatus.UpdateAvailable && DownloadUrl is not null && ChecksumUrl is not null;

    public static UpdateCheckResult UpToDate(Version current) => new()
    {
        Status = UpdateCheckStatus.UpToDate,
        AvailableVersion = current,
        Message = T.UpdateUpToDate(Display(current))
    };

    /// <summary>
    /// Cuts down to three parts. Assembly versions always have four, and the
    /// last says nothing here - "0.2.0.0" is merely confusing.
    /// </summary>
    internal static string Display(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return version.Build >= 0 ? version.ToString(3) : version.ToString();
    }

    /// <summary>There is no release (yet) to check against.</summary>
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
