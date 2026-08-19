using System.Runtime.InteropServices;
using System.Text.Json;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Reads along the access token of the Claude Code CLI - on Windows and Linux
/// from ~/.claude/.credentials.json, on macOS from the keychain.
/// </summary>
/// <remarks>
/// Read-only throughout. An expired token is reported as such and NOT refreshed:
/// the refresh token rotates at Anthropic, and refreshing it here would
/// invalidate the CLI's own sign-in.
/// </remarks>
public sealed class ClaudeCliTokenProvider : ITokenProvider
{
    private readonly IClaudeCliCredentialReader _reader;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClaudeCliTokenProvider>? _logger;

    public ClaudeCliTokenProvider(
        IClaudeCliCredentialReader? reader = null,
        TimeProvider? timeProvider = null,
        ILogger<ClaudeCliTokenProvider>? logger = null)
    {
        _reader = reader ?? CreateDefaultReader();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    public string Name => "claude-cli";

    public async ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default)
    {
        var json = await _reader.ReadRawAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return Parse(json, _timeProvider.GetUtcNow(), _logger);
    }

    /// <summary>Parses the JSON content. Internal so that tests can reach it.</summary>
    internal static AccessToken? Parse(string json, DateTimeOffset now, ILogger? logger = null)
    {
        ClaudeCliCredentials? credentials;
        try
        {
            credentials = JsonSerializer.Deserialize(json, ClaudeUsageJsonContext.Default.ClaudeCliCredentials);
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Could not read the credentials of the Claude CLI.");
            return null;
        }

        var oauth = credentials?.ClaudeAiOAuth;
        if (oauth?.AccessToken is not { Length: > 0 } value)
        {
            return null;
        }

        DateTimeOffset? expiresAt = oauth.ExpiresAt is { } millis and > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(millis)
            : null;

        return new AccessToken(value, TokenSource.ClaudeCli, expiresAt);
    }

    private static IClaudeCliCredentialReader CreateDefaultReader() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? new MacOsKeychainCredentialReader()
            : new CredentialsFileReader();
}
