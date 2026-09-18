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

    // refreshToken is deliberately NOT mapped: this application refreshes no tokens.
    //
    // Neither is subscriptionType. It is written once, when Claude Code signs in,
    // and not kept up to date: on 2026-09-18 it said "pro" for an account the
    // profile endpoint reported as Max 5x. The plan comes from that endpoint
    // (AnthropicProfileClient), not from here.
}
