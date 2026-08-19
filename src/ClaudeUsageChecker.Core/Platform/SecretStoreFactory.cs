namespace ClaudeUsageChecker.Core.Platform;

/// <summary>Waehlt den zur laufenden Plattform passenden Secret-Store aus.</summary>
public static class SecretStoreFactory
{
    /// <summary>
    /// Liefert den Windows Credential Manager unter Windows, sonst einen
    /// Platzhalter, der ehrlich meldet, dass er nichts speichern kann.
    /// </summary>
    public static ISecretStore CreateForCurrentPlatform() =>
        OperatingSystem.IsWindows() ? new WindowsCredentialStore() : new UnsupportedSecretStore();
}
