namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Ein OAuth-Access-Token samt Herkunft und - sofern bekannt - Ablaufzeitpunkt.
/// </summary>
/// <remarks>
/// Der Tokenwert wird bewusst NIE geloggt oder in <see cref="ToString"/> ausgegeben.
/// </remarks>
public sealed class AccessToken
{
    public AccessToken(string value, TokenSource source, DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
        Source = source;
        ExpiresAt = expiresAt;
    }

    /// <summary>Der rohe Tokenwert. Nur an den Authorization-Header weiterreichen.</summary>
    public string Value { get; }

    /// <summary>Woher das Token stammt - fuer Diagnose und UI-Hinweise.</summary>
    public TokenSource Source { get; }

    /// <summary>Ablaufzeitpunkt, falls die Quelle ihn mitliefert.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Prueft, ob das Token abgelaufen ist. Ohne bekannten Ablauf wird es als gueltig behandelt -
    /// die endgueltige Entscheidung trifft ohnehin der Server per 401.
    /// </summary>
    public bool IsExpired(DateTimeOffset now, TimeSpan skew) =>
        ExpiresAt is { } expiry && expiry - skew <= now;

    /// <summary>Maskierte Darstellung fuer Logs. Enthaelt nie das Geheimnis.</summary>
    public override string ToString() => $"AccessToken(source={Source}, expiresAt={ExpiresAt:o})";
}

/// <summary>Herkunft eines Tokens.</summary>
public enum TokenSource
{
    /// <summary>Umgebungsvariable CLAUDE_CODE_OAUTH_TOKEN.</summary>
    Environment,

    /// <summary>Eigene Anmeldung dieser Anwendung ueber OAuth mit PKCE.</summary>
    OAuth,

    /// <summary>Vom Nutzer hinterlegtes Langzeit-Token aus dem Secret-Store des Betriebssystems.</summary>
    SecretStore,

    /// <summary>Mitgelesen aus den Anmeldedaten der Claude-Code-CLI.</summary>
    ClaudeCli
}
