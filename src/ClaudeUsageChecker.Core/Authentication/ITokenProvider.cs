namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Beschafft ein OAuth-Access-Token. Implementierungen lesen ausschliesslich -
/// es wird zu keinem Zeitpunkt ein Token erneuert oder zurueckgeschrieben,
/// damit die Anmeldedaten der Claude-Code-CLI unangetastet bleiben.
/// </summary>
public interface ITokenProvider
{
    /// <summary>Sprechender Name fuer Diagnoseausgaben.</summary>
    string Name { get; }

    /// <summary>Liefert ein Token oder null, wenn diese Quelle nichts anzubieten hat.</summary>
    ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default);
}
