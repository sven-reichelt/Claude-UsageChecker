using System.Net;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>Fehler beim Abruf des Nutzungsstands.</summary>
public sealed class UsageApiException(
    string message,
    UsageApiFailure failure,
    HttpStatusCode? statusCode = null,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>Grobe Fehlerklasse - steuert die Darstellung im Infobereich.</summary>
    public UsageApiFailure Failure { get; } = failure;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    /// <summary>Vom Server vorgegebene Wartezeit, sofern uebermittelt.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>Fehlerklassen des Nutzungsabrufs.</summary>
public enum UsageApiFailure
{
    /// <summary>Kein Token verfuegbar - Einrichtung erforderlich.</summary>
    NoToken,

    /// <summary>Token abgelaufen oder ungueltig (401/403).</summary>
    Unauthorized,

    /// <summary>Zu viele Anfragen (429).</summary>
    RateLimited,

    /// <summary>Netzwerkproblem oder Zeitueberschreitung.</summary>
    Network,

    /// <summary>Serverfehler (5xx).</summary>
    Server,

    /// <summary>Antwort nicht interpretierbar.</summary>
    InvalidResponse
}
