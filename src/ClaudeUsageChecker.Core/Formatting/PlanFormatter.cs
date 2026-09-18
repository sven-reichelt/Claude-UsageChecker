using System.Globalization;
using System.Text.RegularExpressions;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Formatting;

/// <summary>
/// Turns the raw plan values into a name such as "Claude Max 5×".
/// </summary>
/// <remarks>
/// Only Max 5x has been measured (<c>claude_max</c>,
/// <c>default_claude_max_5x</c>). Everything else follows from a rule rather
/// than from a list, so that a plan nobody has seen yet still comes out
/// readable: <c>claude_</c> dropped, the rest capitalised, and a trailing
/// <c>_20x</c> on the tier becoming "20×". Pro, Team and Enterprise are
/// assumed to answer <c>claude_pro</c>, <c>claude_team</c> and
/// <c>claude_enterprise</c>; should they not, the rule still shows what they
/// did send, and the tests name which values are assumptions.
///
/// Plan names are product names and stay as they are in every language.
/// </remarks>
public static partial class PlanFormatter
{
    private const string Prefix = "claude_";

    /// <summary>The plan's name, or null where the answer says nothing usable.</summary>
    public static string? Name(SubscriptionPlan? plan)
    {
        if (plan is null)
        {
            return null;
        }

        var kind = KindOf(plan);
        if (kind is null)
        {
            return null;
        }

        return Multiplier(plan.RateLimitTier) is { } multiplier
            ? $"Claude {kind} {multiplier}×"
            : $"Claude {kind}";
    }

    /// <summary>
    /// A type in the product's own family wins; after it the account's flags;
    /// a type from outside the family only when nothing else is there.
    /// </summary>
    private static string? KindOf(SubscriptionPlan plan)
    {
        var type = plan.OrganizationType?.Trim();

        if (type is not null && type.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = type[Prefix.Length..].Trim('_');
            if (rest.Length > 0)
            {
                return Capitalise(rest);
            }

            // "claude_" and nothing after it names no plan - and must not come
            // out as "Claude Claude" further down.
            type = null;
        }

        if (plan.HasClaudeMax)
        {
            return "Max";
        }

        if (plan.HasClaudePro)
        {
            return "Pro";
        }

        return type is { Length: > 0 } ? Capitalise(type) : null;
    }

    private static string Capitalise(string raw) => string.Join(' ', raw
        .Split('_', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));

    private static int? Multiplier(string? tier) =>
        tier is not null && MultiplierPattern().Match(tier) is { Success: true } match
            ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : null;

    [GeneratedRegex(@"_(\d{1,3})x$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MultiplierPattern();
}
