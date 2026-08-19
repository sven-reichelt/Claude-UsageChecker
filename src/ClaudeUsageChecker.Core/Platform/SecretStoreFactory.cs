namespace ClaudeUsageChecker.Core.Platform;

/// <summary>Picks the secret store that fits the running platform.</summary>
public static class SecretStoreFactory
{
    /// <summary>
    /// Returns the Windows Credential Manager on Windows, otherwise a stand-in
    /// that says plainly that it cannot store anything.
    /// </summary>
    public static ISecretStore CreateForCurrentPlatform() =>
        OperatingSystem.IsWindows() ? new WindowsCredentialStore() : new UnsupportedSecretStore();
}
