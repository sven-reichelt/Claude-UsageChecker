using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Stand-in for platforms without a secret store of their own. Says plainly that
/// nothing can be stored, instead of falling back on an insecure improvisation.
/// </summary>
public sealed class UnsupportedSecretStore : ISecretStore
{
    public bool IsSupported => false;

    public string? Read(string key) => null;

    public void Write(string key, string secret) =>
        throw new NotSupportedException(T.ErrorNoSecureStore);

    public void Delete(string key)
    {
        // Nothing to do.
    }
}
