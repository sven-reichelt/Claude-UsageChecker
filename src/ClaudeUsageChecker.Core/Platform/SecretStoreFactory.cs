namespace ClaudeUsageChecker.Core.Platform;

/// <summary>Picks the secret store that fits the running platform.</summary>
public static class SecretStoreFactory
{
    /// <summary>
    /// The Windows Credential Manager on Windows, the keychain on macOS, and
    /// elsewhere a stand-in that says plainly that it cannot store anything.
    /// </summary>
    public static ISecretStore CreateForCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsCredentialStore();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsKeychainStore();
        }

        return new UnsupportedSecretStore();
    }
}
