using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Supplies the token from the application's own sign-in and refreshes it by
/// itself before it expires.
/// </summary>
/// <remarks>
/// Refreshing happens here on purpose - unlike with the token read from the
/// Claude Code installation. These credentials belong to the application alone,
/// so a rotating refresh token invalidates nobody else's session. That is
/// precisely what makes the application independent: it no longer needs a
/// running Claude Code installation.
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

    /// <summary>
    /// The application's own sign-in was refused for good by the server and has
    /// been removed. The user has to sign in again.
    /// </summary>
    public event EventHandler<string>? SignInExpired;

    /// <summary>Whether the application has a sign-in of its own.</summary>
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
            // Expired and not refreshable - better to supply nothing, so that the
            // next source gets its turn.
            logger?.LogWarning("The application's own sign-in has expired and cannot be refreshed.");
            return null;
        }

        return await RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AccessToken?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // A parallel call may have refreshed in the meantime.
            var aktuell = tokenStore.Read();
            if (aktuell is not null && !aktuell.NeedsRefresh(_timeProvider.GetUtcNow(), _options.RefreshSkew))
            {
                return ToAccessToken(aktuell);
            }

            var erneuert = await oauthClient.RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);

            // Some servers send no new refresh token when refreshing; the previous
            // one then keeps its validity.
            var zuSpeichern = erneuert.RefreshToken is { Length: > 0 }
                ? erneuert
                : new OAuthTokens
                {
                    AccessToken = erneuert.AccessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = erneuert.ExpiresAt,
                    RefreshTokenExpiresAt = erneuert.RefreshTokenExpiresAt,
                    Scope = erneuert.Scope
                };

            tokenStore.Write(zuSpeichern);
            logger?.LogInformation("The application's own sign-in was refreshed.");
            return ToAccessToken(zuSpeichern);
        }
        catch (OAuthException ex) when (ex.IsCredentialRejected)
        {
            // The server discarded the credentials - they are gone for good. They
            // are removed so that the interface does not falsely show an existing
            // sign-in, and the user is told about it. Falling back to Claude Code
            // in silence would be the worst outcome here: the display would carry
            // on, and the independence would be lost unnoticed.
            logger?.LogWarning(ex, "The application's own sign-in was refused and is being removed.");
            tokenStore.Clear();
            SignInExpired?.Invoke(this, ex.Message);
            return null;
        }
        catch (OAuthException ex)
        {
            // Network problem or server error: the credentials stay as they are.
            // The next attempt may well succeed.
            logger?.LogWarning(ex, "The application's own sign-in could not be refreshed just now.");
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
