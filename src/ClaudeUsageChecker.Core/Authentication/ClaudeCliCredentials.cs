using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Abbild der Datei ~/.claude/.credentials.json bzw. des Keychain-Eintrags
/// "Claude Code-credentials" unter macOS.
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

    /// <summary>Ablaufzeitpunkt als Unix-Zeit in Millisekunden.</summary>
    [JsonPropertyName("expiresAt")]
    public long? ExpiresAt { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    // refreshToken wird bewusst NICHT abgebildet: Die Anwendung erneuert keine Tokens.
}
