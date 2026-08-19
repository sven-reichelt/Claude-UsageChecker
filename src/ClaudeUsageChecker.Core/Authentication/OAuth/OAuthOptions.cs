namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>Parameter des Anmeldevorgangs.</summary>
public sealed class OAuthOptions
{
    /// <summary>Seite, auf der der Nutzer die Freigabe erteilt.</summary>
    public Uri AuthorizationEndpoint { get; init; } = new("https://claude.ai/oauth/authorize");

    /// <summary>
    /// Endpunkt, an dem Code gegen Token getauscht und erneuert wird.
    /// </summary>
    /// <remarks>
    /// Nicht console.anthropic.com: Der Pfad liegt dort nicht mehr und
    /// antwortet mit HTTP 404. Gemessen am 19.08.2026.
    /// </remarks>
    public Uri TokenEndpoint { get; init; } = new("https://platform.claude.com/v1/oauth/token");

    /// <summary>Kennung der Claude-Code-Anwendung.</summary>
    public string ClientId { get; init; } = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";

    /// <summary>
    /// Rueckleitung auf die Anzeigeseite von Anthropic. Der Nutzer kopiert den
    /// dort angezeigten Code von Hand zurueck; die Anwendung braucht dafuer
    /// keinen lokalen Webserver und keinen offenen Port.
    /// </summary>
    public Uri RedirectUri { get; init; } = new("https://console.anthropic.com/oauth/code/callback");

    /// <summary>
    /// Angeforderte Rechte. Der Nutzungsendpunkt verlangt <c>user:profile</c>;
    /// mehr braucht diese Anwendung nicht - insbesondere kein Recht, im Namen
    /// des Kontos Anfragen zu stellen oder API-Schluessel anzulegen.
    /// </summary>
    public string Scope { get; init; } = "user:profile";

    /// <summary>Kopfzeile, die auch beim Nutzungsabruf gesetzt wird.</summary>
    public string UserAgent { get; init; } = "claude-code/2.0.0";

    /// <summary>Vorlauf, mit dem ein Token vor Ablauf erneuert wird.</summary>
    public TimeSpan RefreshSkew { get; init; } = TimeSpan.FromMinutes(5);
}
