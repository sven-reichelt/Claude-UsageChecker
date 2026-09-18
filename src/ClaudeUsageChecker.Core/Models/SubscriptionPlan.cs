namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// The plan of the account a token belongs to, as GET /api/oauth/profile
/// reports it.
/// </summary>
/// <param name="OrganizationType">
/// For example <c>claude_max</c>. Measured for Max only; what Pro, Team and
/// Enterprise send is assumed, not known.
/// </param>
/// <param name="RateLimitTier">
/// For example <c>default_claude_max_5x</c> - the only field that tells Max 5x
/// from Max 20x.
/// </param>
/// <param name="HasClaudeMax">The account's own flag, for when the type says nothing.</param>
/// <param name="HasClaudePro">The same for Pro.</param>
/// <remarks>
/// The raw values are kept rather than an enumeration: a plan nobody has
/// measured yet should still reach the display in some readable form, not be
/// turned into "unknown" by a list written in advance.
/// </remarks>
public sealed record SubscriptionPlan(
    string? OrganizationType,
    string? RateLimitTier,
    bool HasClaudeMax = false,
    bool HasClaudePro = false);
