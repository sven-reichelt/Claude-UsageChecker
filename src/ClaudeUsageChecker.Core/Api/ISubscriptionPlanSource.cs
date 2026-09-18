using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>Finds out which plan the account behind a token has.</summary>
public interface ISubscriptionPlanSource
{
    /// <summary>
    /// The plan, or null where it cannot be told. Never throws for anything but
    /// cancellation: a plan that cannot be read must not cost the usage figures.
    /// </summary>
    Task<SubscriptionPlan?> GetPlanAsync(AccessToken token, CancellationToken cancellationToken = default);
}
