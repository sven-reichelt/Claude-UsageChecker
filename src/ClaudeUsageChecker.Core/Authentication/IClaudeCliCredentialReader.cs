namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Beschafft den rohen JSON-Inhalt der CLI-Anmeldedaten. Die plattformspezifische
/// Ablage (Datei bzw. Schluesselbund) wird hinter dieser Schnittstelle gekapselt.
/// </summary>
public interface IClaudeCliCredentialReader
{
    /// <summary>Liefert den JSON-Inhalt oder null, wenn keine Anmeldedaten vorliegen.</summary>
    ValueTask<string?> ReadRawAsync(CancellationToken cancellationToken = default);
}
