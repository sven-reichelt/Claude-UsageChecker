namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Liest das Token aus der Umgebungsvariable CLAUDE_CODE_OAUTH_TOKEN.
/// Primaer fuer Entwicklung und automatisierte Tests gedacht.
/// </summary>
public sealed class EnvironmentTokenProvider(string variableName = "CLAUDE_CODE_OAUTH_TOKEN") : ITokenProvider
{
    public const string DefaultVariableName = "CLAUDE_CODE_OAUTH_TOKEN";

    public string Name => $"env:{variableName}";

    public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        var token = string.IsNullOrWhiteSpace(value)
            ? null
            : new AccessToken(value.Trim(), TokenSource.Environment);

        return ValueTask.FromResult(token);
    }
}
