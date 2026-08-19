using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Liefert das Token aus der eigenen Anmeldung und erneuert es selbsttaetig,
/// bevor es ablaeuft.
/// </summary>
/// <remarks>
/// Hier wird bewusst erneuert - anders als beim mitgelesenen Token der
/// Claude-Code-Installation. Diese Anmeldedaten gehoeren der Anwendung allein,
/// ein rotierender Refresh-Token entwertet also keine fremde Sitzung. Genau das
/// macht die Anwendung unabhaengig: Sie braucht keine laufende
/// Claude-Code-Installation mehr.
/// </remarks>
public sealed class OAuthTokenProvider(
    OAuthTokenStore tokenStore,
    AnthropicOAuthClient oauthClient,
    OAuthOptions? options = null,
    TimeProvider? timeProvider = null,
    ILogger<OAuthTokenProvider>? logger = null) : ITokenProvider, IDisposable
{
    private readonly OAuthOptions _options = options ?? new OAuthOptions();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public string Name => "oauth";

    /// <summary>Ob eine eigene Anmeldung vorliegt.</summary>
    public bool IsSignedIn => tokenStore.Read() is not null;

    public async ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = tokenStore.Read();
        if (tokens is null)
        {
            return null;
        }

        if (!tokens.NeedsRefresh(_timeProvider.GetUtcNow(), _options.RefreshSkew))
        {
            return ToAccessToken(tokens);
        }

        if (tokens.RefreshToken is not { Length: > 0 } refreshToken)
        {
            // Abgelaufen und nicht erneuerbar - lieber nichts liefern, damit die
            // naechste Quelle zum Zuge kommt.
            logger?.LogWarning("Die eigene Anmeldung ist abgelaufen und nicht erneuerbar.");
            return null;
        }

        return await RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AccessToken?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Ein paralleler Aufruf koennte inzwischen erneuert haben.
            var aktuell = tokenStore.Read();
            if (aktuell is not null && !aktuell.NeedsRefresh(_timeProvider.GetUtcNow(), _options.RefreshSkew))
            {
                return ToAccessToken(aktuell);
            }

            var erneuert = await oauthClient.RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            // Manche Server liefern beim Erneuern keinen neuen Refresh-Token mit;
            // dann behaelt der bisherige seine Gueltigkeit.
            var zuSpeichern = erneuert.RefreshToken is { Length: > 0 }
                ? erneuert
                : new OAuthTokens
                {
                    AccessToken = erneuert.AccessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = erneuert.ExpiresAt,
                    Scope = erneuert.Scope
                };

            tokenStore.Write(zuSpeichern);
            logger?.LogInformation("Die eigene Anmeldung wurde erneuert.");
            return ToAccessToken(zuSpeichern);
        }
        catch (OAuthException ex)
        {
            logger?.LogWarning(ex, "Die eigene Anmeldung konnte nicht erneuert werden.");
            return null;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static AccessToken ToAccessToken(OAuthTokens tokens) =>
        new(tokens.AccessToken, TokenSource.OAuth, tokens.ExpiresAt);

    public void Dispose() => _refreshGate.Dispose();
}
