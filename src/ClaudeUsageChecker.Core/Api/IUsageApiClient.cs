using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>Fetches the current usage status of the subscription.</summary>
public interface IUsageApiClient
{
    /// <exception cref="UsageApiException">On any functional or technical failure.</exception>
    Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default);
}
