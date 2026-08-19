using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Fragt mehrere Tokenquellen der Reihe nach ab und liefert das erste brauchbare Token.
/// Eine fehlerhafte Quelle beendet die Kette nicht.
/// </summary>
public sealed class ChainedTokenProvider(
    IReadOnlyList<ITokenProvider> providers,
    ILogger<ChainedTokenProvider>? logger = null) : ITokenProvider
{
    private readonly IReadOnlyList<ITokenProvider> _providers =
        providers ?? throw new ArgumentNullException(nameof(providers));

    public string Name => "chain";

    public async ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var token = await provider.TryGetTokenAsync(cancellationToken).ConfigureAwait(false);
                if (token is not null)
                {
                    logger?.LogDebug("Token aus Quelle {Source} bezogen.", provider.Name);
                    return token;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger?.LogWarning(ex, "Tokenquelle {Source} nicht verfuegbar.", provider.Name);
            }
        }

        return null;
    }
}
