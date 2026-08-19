namespace ClaudeUsageChecker.Core.Configuration;

/// <summary>Settings of the usage call.</summary>
public sealed class UsageApiOptions
{
    /// <summary>Base address of the Anthropic API.</summary>
    public Uri BaseAddress { get; init; } = new("https://api.anthropic.com/");

    /// <summary>Path of the usage endpoint.</summary>
    public string UsagePath { get; init; } = "api/oauth/usage";

    /// <summary>
    /// Value of the User-Agent header. Mandatory: without a Claude Code user
    /// agent the endpoint answers HTTP 429 permanently.
    /// </summary>
    public string UserAgent { get; init; } = "claude-code/2.0.0";

    /// <summary>Value of the anthropic-beta header for OAuth access.</summary>
    public string BetaHeader { get; init; } = "oauth-2025-04-20";

    /// <summary>Timeout of a single call.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(20);
}
