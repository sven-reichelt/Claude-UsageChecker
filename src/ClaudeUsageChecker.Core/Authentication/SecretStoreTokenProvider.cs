using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Liest das vom Nutzer hinterlegte Langzeit-Token (erzeugt per <c>claude setup-token</c>)
/// aus dem verschluesselten Secret-Store des Betriebssystems. Dies ist die bevorzugte
/// Quelle, weil sie voellig unabhaengig von einer laufenden Claude-Code-Installation ist.
/// </summary>
public sealed class SecretStoreTokenProvider(ISecretStore store, string key = "ClaudeUsageChecker:OAuthToken") : ITokenProvider
{
    /// <summary>Bezeichner des Eintrags im Secret-Store.</summary>
    public const string DefaultKey = "ClaudeUsageChecker:OAuthToken";

    public string Name => "secret-store";

    public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!store.IsSupported)
        {
            return ValueTask.FromResult<AccessToken?>(null);
        }

        var secret = store.Read(key);
        var token = string.IsNullOrWhiteSpace(secret)
            ? null
            : new AccessToken(secret.Trim(), TokenSource.SecretStore);

        return ValueTask.FromResult(token);
    }
}
