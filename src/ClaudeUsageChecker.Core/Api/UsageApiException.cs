using System.Net;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>A failure while fetching the usage status.</summary>
public sealed class UsageApiException(
    string message,
    UsageApiFailure failure,
    HttpStatusCode? statusCode = null,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>Broad failure category - drives what the tray icon shows.</summary>
    public UsageApiFailure Failure { get; } = failure;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    /// <summary>Wait time demanded by the server, where one was supplied.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>Failure categories of the usage call.</summary>
public enum UsageApiFailure
{
    /// <summary>No token available - sign-in required.</summary>
    NoToken,

    /// <summary>Token expired or invalid (401/403).</summary>
    Unauthorized,

    /// <summary>Too many requests (429).</summary>
    RateLimited,

    /// <summary>Network problem or timeout.</summary>
    Network,

    /// <summary>Server error (5xx).</summary>
    Server,

    /// <summary>Response could not be interpreted.</summary>
    InvalidResponse
}
