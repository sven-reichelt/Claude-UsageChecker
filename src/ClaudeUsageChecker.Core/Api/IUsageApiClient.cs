using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>Ruft den aktuellen Nutzungsstand des Abonnements ab.</summary>
public interface IUsageApiClient
{
    /// <exception cref="UsageApiException">Bei jedem fachlichen oder technischen Fehlschlag.</exception>
    Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default);
}
