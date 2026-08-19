namespace ClaudeUsageChecker.Core.Configuration;

/// <summary>Einstellungen des Nutzungsabrufs.</summary>
public sealed class UsageApiOptions
{
    /// <summary>Basisadresse der Anthropic-API.</summary>
    public Uri BaseAddress { get; init; } = new("https://api.anthropic.com/");

    /// <summary>Pfad des Nutzungsendpunkts.</summary>
    public string UsagePath { get; init; } = "api/oauth/usage";

    /// <summary>
    /// Wert des User-Agent-Headers. Pflicht: Ohne einen Claude-Code-User-Agent
    /// antwortet der Endpunkt dauerhaft mit HTTP 429.
    /// </summary>
    public string UserAgent { get; init; } = "claude-code/2.0.0";

    /// <summary>Wert des anthropic-beta-Headers fuer den OAuth-Zugriff.</summary>
    public string BetaHeader { get; init; } = "oauth-2025-04-20";

    /// <summary>Zeitueberschreitung eines einzelnen Abrufs.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(20);
}
