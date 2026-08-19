using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Mirror of ~/.claude/.credentials.json, respectively of the keychain entry
/// "Claude Code-credentials" on macOS.
/// </summary>
internal sealed class ClaudeCliCredentials
{
    [JsonPropertyName("claudeAiOauth")]
    public ClaudeCliOAuth? ClaudeAiOAuth { get; set; }
}

internal sealed class ClaudeCliOAuth
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    /// <summary>Expiry as Unix time in milliseconds.</summary>
    [JsonPropertyName("expiresAt")]
    public long? ExpiresAt { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    // refreshToken is deliberately NOT mapped: this application refreshes no tokens.
}
