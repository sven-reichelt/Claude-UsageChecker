using System;

using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Result of an update check.</summary>
public sealed record UpdateCheckResult
{
    public required UpdateCheckStatus Status { get; init; }

    /// <summary>Version of the update that was found.</summary>
    public ProgramVersion? AvailableVersion { get; init; }

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

    public static UpdateCheckResult UpToDate(ProgramVersion current)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new UpdateCheckResult
        {
            Status = UpdateCheckStatus.UpToDate,
            AvailableVersion = current,
            Message = T.UpdateUpToDate(current.ToString())
        };
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
