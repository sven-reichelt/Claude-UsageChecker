namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Encrypted storage for secrets, provided by the operating system (Windows
/// Credential Manager, macOS keychain).
/// </summary>
public interface ISecretStore
{
    /// <summary>Whether this store is usable on the running system.</summary>
    bool IsSupported { get; }

    /// <summary>Reads a secret, or returns null when none is stored.</summary>
    string? Read(string key);

    /// <summary>Stores a secret, encrypted and bound to the user account.</summary>
    void Write(string key, string secret);

    /// <summary>Removes a secret. A missing entry is not an error.</summary>
    void Delete(string key);
}
