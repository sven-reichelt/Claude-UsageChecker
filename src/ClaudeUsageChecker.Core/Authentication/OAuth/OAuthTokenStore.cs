using System.Text.Json;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Stores the application's own credentials in the encrypted storage of the
/// operating system - separate from the entry for a manually stored single
/// token.
/// </summary>
public sealed class OAuthTokenStore(ISecretStore store, string key = "ClaudeUsageChecker:OAuth")
{
    /// <summary>Name of the entry in the secret store.</summary>
    public const string DefaultKey = "ClaudeUsageChecker:OAuth";

    /// <summary>Whether this system can store anything securely at all.</summary>
    public bool IsSupported => store.IsSupported;

    public OAuthTokens? Read()
    {
        if (!store.IsSupported)
        {
            return null;
        }

        var json = store.Read(key);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, OAuthJsonContext.Default.OAuthTokens);
        }
        catch (JsonException)
        {
            // A corrupted entry counts as no entry - the user signs in again.
            return null;
        }
    }

    public void Write(OAuthTokens tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        store.Write(key, JsonSerializer.Serialize(tokens, OAuthJsonContext.Default.OAuthTokens));
    }

    public void Clear() => store.Delete(key);
}
