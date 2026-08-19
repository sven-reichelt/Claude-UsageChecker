using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Die eigenen Anmeldedaten dieser Anwendung - streng getrennt von denen der
/// Claude-Code-Installation.
/// </summary>
/// <remarks>
/// Anders als beim mitgelesenen Token darf und muss dieses hier erneuert
/// werden: Es gehoert der Anwendung allein, ein rotierender Refresh-Token
/// entwertet also keine fremde Anmeldung.
/// </remarks>
public sealed class OAuthTokens
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>Ob das Token innerhalb der Vorlaufzeit ablaeuft.</summary>
    public bool NeedsRefresh(DateTimeOffset now, TimeSpan skew) =>
        ExpiresAt is { } expiry && expiry - skew <= now;

    /// <summary>Maskierte Darstellung. Enthaelt nie ein Geheimnis.</summary>
    public override string ToString() =>
        $"OAuthTokens(expiresAt={ExpiresAt:o}, scope={Scope}, refreshable={RefreshToken is not null})";
}
