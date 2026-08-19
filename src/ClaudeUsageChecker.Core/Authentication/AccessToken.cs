namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// An OAuth access token with its origin and - where known - its expiry.
/// </summary>
/// <remarks>
/// The token value is deliberately NEVER logged or exposed through
/// <see cref="ToString"/>.
/// </remarks>
public sealed class AccessToken
{
    public AccessToken(string value, TokenSource source, DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
        Source = source;
        ExpiresAt = expiresAt;
    }

    /// <summary>The raw token value. Only ever pass it to the Authorization header.</summary>
    public string Value { get; }

    /// <summary>Where the token came from - for diagnostics and interface hints.</summary>
    public TokenSource Source { get; }

    /// <summary>Expiry, if the source supplies one.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Whether the token has expired. Without a known expiry it counts as valid -
    /// the final say belongs to the server through a 401 anyway.
    /// </summary>
    public bool IsExpired(DateTimeOffset now, TimeSpan skew) =>
        ExpiresAt is { } expiry && expiry - skew <= now;

    /// <summary>Masked representation for logs. Never contains the secret.</summary>
    public override string ToString() => $"AccessToken(source={Source}, expiresAt={ExpiresAt:o})";
}

/// <summary>Where a token came from.</summary>
public enum TokenSource
{
    /// <summary>The CLAUDE_CODE_OAUTH_TOKEN environment variable.</summary>
    Environment,

    /// <summary>This application's own sign-in through OAuth with PKCE.</summary>
    OAuth,

    /// <summary>A long-lived token the user stored in the operating system's secret store.</summary>
    SecretStore,

    /// <summary>Read from the credentials of the Claude Code CLI.</summary>
    ClaudeCli
}
