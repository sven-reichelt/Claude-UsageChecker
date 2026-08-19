using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Reads the long-lived token the user stored (created with <c>claude setup-token</c>)
/// from the encrypted secret store of the operating system. This source is
/// preferred over the CLI credentials because it is entirely independent of a
/// running Claude Code installation.
/// </summary>
public sealed class SecretStoreTokenProvider(ISecretStore store, string key = "ClaudeUsageChecker:OAuthToken") : ITokenProvider
{
    /// <summary>Name of the entry in the secret store.</summary>
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
