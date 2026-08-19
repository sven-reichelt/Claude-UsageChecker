using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Reads ~/.claude/.credentials.json (Windows and Linux).
/// </summary>
public sealed class CredentialsFileReader(string? path = null) : IClaudeCliCredentialReader
{
    /// <summary>Default path of the credentials inside the user profile.</summary>
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude",
        ".credentials.json");

    private readonly string _path = path ?? DefaultPath;

    public async ValueTask<string?> ReadRawAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            return await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The CLI rewrites the file when it refreshes its token - brief read
            // failures are to be expected and resolve themselves on the next call.
            return null;
        }
    }
}
