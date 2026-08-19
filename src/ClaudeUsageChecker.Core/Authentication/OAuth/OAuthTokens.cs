using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// This application's own credentials - strictly separate from those of the
/// Claude Code installation.
/// </summary>
/// <remarks>
/// Unlike the token read from the CLI, this one may and must be refreshed: it
/// belongs to the application alone, so a rotating refresh token invalidates
/// nobody else's sign-in.
/// </remarks>
public sealed class OAuthTokens
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Expiry of the refresh token, where the server states one. Where it is
    /// unknown, nothing can be said about how long the sign-in will last.
    /// </summary>
    [JsonPropertyName("refreshTokenExpiresAt")]
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>Whether the token expires within the lead time.</summary>
    public bool NeedsRefresh(DateTimeOffset now, TimeSpan skew) =>
        ExpiresAt is { } expiry && expiry - skew <= now;

    /// <summary>Masked representation. Never contains a secret.</summary>
    public override string ToString() =>
        $"OAuthTokens(expiresAt={ExpiresAt:o}, scope={Scope}, refreshable={RefreshToken is not null})";
}
