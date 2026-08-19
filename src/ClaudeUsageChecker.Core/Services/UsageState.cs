using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>The application state as the tray icon presents it.</summary>
public enum UsageStateKind
{
    /// <summary>No call has been made yet.</summary>
    Initializing,

    /// <summary>Current data is available.</summary>
    Ready,

    /// <summary>The last call failed, but older data is still around.</summary>
    Stale,

    /// <summary>No token available - sign-in needed.</summary>
    NotConfigured,

    /// <summary>Token expired or rejected.</summary>
    AuthenticationFailed,

    /// <summary>No call possible and no usable earlier data.</summary>
    Unavailable
}

/// <summary>
/// Immutable snapshot of the application state.
/// </summary>
public sealed record UsageState
{
    public required UsageStateKind Kind { get; init; }

    /// <summary>The most recent data fetched successfully, if any.</summary>
    public UsageSnapshot? Snapshot { get; init; }

    /// <summary>Category of the last failure.</summary>
    public UsageApiFailure? Failure { get; init; }

    /// <summary>Human-readable error message.</summary>
    public string? Message { get; init; }

    /// <summary>When the next call is scheduled.</summary>
    public DateTimeOffset? NextPollAt { get; init; }

    public static UsageState Initializing { get; } = new() { Kind = UsageStateKind.Initializing };

    /// <summary>Whether there is anything worth displaying.</summary>
    public bool HasData => Snapshot is not null;
}
