namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Obtains the raw JSON of the CLI credentials. The platform-specific storage
/// (file or keychain) is hidden behind this interface.
/// </summary>
public interface IClaudeCliCredentialReader
{
    /// <summary>Returns the JSON content, or null when no credentials exist.</summary>
    ValueTask<string?> ReadRawAsync(CancellationToken cancellationToken = default);
}
