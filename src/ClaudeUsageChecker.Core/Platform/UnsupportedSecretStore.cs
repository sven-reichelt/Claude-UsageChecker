namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Platzhalter fuer Plattformen ohne angebundenen Secret-Store.
/// Meldet ehrlich, dass nichts gespeichert werden kann, statt auf eine
/// unsichere Notloesung auszuweichen.
/// </summary>
public sealed class UnsupportedSecretStore : ISecretStore
{
    public bool IsSupported => false;

    public string? Read(string key) => null;

    public void Write(string key, string secret) =>
        throw new NotSupportedException(
            "Auf dieser Plattform steht noch kein sicherer Speicher zur Verfuegung.");

    public void Delete(string key)
    {
        // Nichts zu tun.
    }
}
