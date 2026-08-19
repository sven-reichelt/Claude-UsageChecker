using System.Text.Json;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Legt die eigenen Anmeldedaten im verschluesselten Speicher des
/// Betriebssystems ab - getrennt vom Eintrag fuer ein von Hand hinterlegtes
/// Einzeltoken.
/// </summary>
public sealed class OAuthTokenStore(ISecretStore store, string key = "ClaudeUsageChecker:OAuth")
{
    /// <summary>Bezeichner des Eintrags im Secret-Store.</summary>
    public const string DefaultKey = "ClaudeUsageChecker:OAuth";

    /// <summary>Ob auf diesem System ueberhaupt sicher gespeichert werden kann.</summary>
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
            // Beschaedigter Eintrag zaehlt wie kein Eintrag - der Nutzer meldet sich neu an.
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
