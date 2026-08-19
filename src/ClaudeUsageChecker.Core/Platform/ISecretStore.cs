namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Verschluesselter Ablageort fuer Geheimnisse, bereitgestellt vom Betriebssystem
/// (Windows Credential Manager, macOS-Schluesselbund).
/// </summary>
public interface ISecretStore
{
    /// <summary>Ob dieser Store auf dem laufenden System nutzbar ist.</summary>
    bool IsSupported { get; }

    /// <summary>Liest ein Geheimnis oder liefert null, wenn keines hinterlegt ist.</summary>
    string? Read(string key);

    /// <summary>Legt ein Geheimnis verschluesselt und benutzergebunden ab.</summary>
    void Write(string key, string secret);

    /// <summary>Entfernt ein Geheimnis. Nicht vorhandene Eintraege sind kein Fehler.</summary>
    void Delete(string key);
}
