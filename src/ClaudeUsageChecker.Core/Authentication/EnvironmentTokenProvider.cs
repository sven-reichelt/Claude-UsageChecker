namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Reads the token from the CLAUDE_CODE_OAUTH_TOKEN environment variable.
/// Mainly meant for development and automated tests.
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
