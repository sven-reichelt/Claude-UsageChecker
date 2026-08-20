namespace ClaudeUsageChecker.Core.Models;

/// <summary>
/// The extra usage quota, where enabled on the subscription.
/// </summary>
/// <param name="IsEnabled">Whether the quota is switched on at all.</param>
/// <param name="Used">What has been spent, in whole currency units.</param>
/// <param name="Limit">The monthly cap, in whole currency units.</param>
/// <param name="Utilization">The share used, in percent.</param>
/// <param name="Currency">
/// The ISO code the API named - EUR, USD, BRL. Null where it named none.
/// </param>
/// <param name="Decimals">
/// How many decimal places the currency carries, as the API stated it.
/// </param>
/// <remarks>
/// These are amounts of money, not counts of credits, whatever the older field
/// name <c>used_credits</c> suggests. The currency belongs to the account, not
/// to the application, and is therefore carried along rather than assumed.
/// </remarks>
public sealed record ExtraUsage(
    bool IsEnabled,
    decimal? Used,
    decimal? Limit,
    double? Utilization,
    string? Currency = null,
    int? Decimals = null);
