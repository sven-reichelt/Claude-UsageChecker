using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.Core.Platform;

/// <summary>
/// Liest ~/.claude/.credentials.json (Windows und Linux).
/// </summary>
public sealed class CredentialsFileReader(string? path = null) : IClaudeCliCredentialReader
{
    /// <summary>Standardpfad der Anmeldedaten im Benutzerprofil.</summary>
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
            // Die CLI schreibt die Datei beim Token-Refresh neu - kurzzeitige Lesefehler
            // sind erwartbar und werden beim naechsten Abruf von selbst behoben.
            return null;
        }
    }
}
