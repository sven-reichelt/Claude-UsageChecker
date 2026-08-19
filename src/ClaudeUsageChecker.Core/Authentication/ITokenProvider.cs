namespace ClaudeUsageChecker.Core.Authentication;

/// <summary>
/// Obtains an OAuth access token. Implementations only ever read - no token is
/// refreshed or written back at any point, so that the credentials of the
/// Claude Code CLI stay untouched.
/// </summary>
public interface ITokenProvider
{
    /// <summary>Readable name for diagnostic output.</summary>
    string Name { get; }

    /// <summary>Returns a token, or null when this source has nothing to offer.</summary>
    ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default);
}
