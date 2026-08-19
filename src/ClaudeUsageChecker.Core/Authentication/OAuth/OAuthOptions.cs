namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>Parameters of the sign-in flow.</summary>
public sealed class OAuthOptions
{
    /// <summary>The page on which the user grants access.</summary>
    public Uri AuthorizationEndpoint { get; init; } = new("https://claude.ai/oauth/authorize");

    /// <summary>
    /// Endpoint where the code is exchanged for tokens and refreshed.
    /// </summary>
    /// <remarks>
    /// Not console.anthropic.com: the path no longer lives there and answers
    /// with HTTP 404. Measured on 2026-08-19.
    /// </remarks>
    public Uri TokenEndpoint { get; init; } = new("https://platform.claude.com/v1/oauth/token");

    /// <summary>Client id of the Claude Code application.</summary>
    public string ClientId { get; init; } = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";

    /// <summary>
    /// Redirect to Anthropic's display page. The user copies the code shown
    /// there back by hand; the application needs neither a local web server nor
    /// an open port for it.
    /// </summary>
    public Uri RedirectUri { get; init; } = new("https://console.anthropic.com/oauth/code/callback");

    /// <summary>
    /// Requested scope. The usage endpoint demands <c>user:profile</c>; this
    /// application needs nothing beyond it - in particular no right to make
    /// requests on behalf of the account or to create API keys.
    /// </summary>
    public string Scope { get; init; } = "user:profile";

    /// <summary>Header that is also set on the usage call.</summary>
    public string UserAgent { get; init; } = "claude-code/2.0.0";

    /// <summary>How far ahead of expiry a token is refreshed.</summary>
    public TimeSpan RefreshSkew { get; init; } = TimeSpan.FromMinutes(5);
}
