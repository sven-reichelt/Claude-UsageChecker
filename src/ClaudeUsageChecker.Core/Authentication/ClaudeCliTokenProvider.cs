using System.Runtime.InteropServices;
using System.Text.Json;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Liest das Access-Token der Claude-Code-CLI mit - unter Windows/Linux aus
/// ~/.claude/.credentials.json, unter macOS aus dem Schluesselbund.
/// </summary>
/// <remarks>
/// Ausschliesslich lesend. Ein abgelaufenes Token wird als solches gemeldet und
/// NICHT erneuert: Der Refresh-Token rotiert bei Anthropic, ein Erneuern durch diese
/// Anwendung wuerde die Anmeldung der CLI ungueltig machen.
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

    /// <summary>Zerlegt den JSON-Inhalt. Intern fuer Tests zugaenglich.</summary>
    internal static AccessToken? Parse(string json, DateTimeOffset now, ILogger? logger = null)
    {
        ClaudeCliCredentials? credentials;
        try
        {
            credentials = JsonSerializer.Deserialize(json, ClaudeUsageJsonContext.Default.ClaudeCliCredentials);
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Anmeldedaten der Claude-CLI konnten nicht gelesen werden.");
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
