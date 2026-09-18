using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Tests.Formatting;

/// <summary>
/// How the raw plan values become a name.
/// </summary>
/// <remarks>
/// Only the first test is a measurement. Every other plan is an assumption about
/// what the profile endpoint sends, recorded as one - when a real answer
/// arrives, the test it contradicts is the place to correct.
/// </remarks>
public class PlanFormatterTests
{
    [Fact]
    public void Name_MaxFiveTimes_AsMeasured()
    {
        // Measured on 2026-09-18 with both routes to a token.
        var plan = new SubscriptionPlan("claude_max", "default_claude_max_5x", HasClaudeMax: true);

        Assert.Equal("Claude Max 5×", PlanFormatter.Name(plan));
    }

    [Theory]
    [InlineData("claude_max", "default_claude_max_20x", "Claude Max 20×")]   // assumed
    [InlineData("claude_pro", "default_claude_ai", "Claude Pro")]             // assumed
    [InlineData("claude_team", null, "Claude Team")]                          // assumed
    [InlineData("claude_enterprise", null, "Claude Enterprise")]              // assumed
    public void Name_AssumedPlans(string type, string? tier, string expected) =>
        Assert.Equal(expected, PlanFormatter.Name(new SubscriptionPlan(type, tier)));

    [Fact]
    public void Name_APlanNobodyHasSeenStillComesOutReadable() =>
        Assert.Equal(
            "Claude Team Premium 3×",
            PlanFormatter.Name(new SubscriptionPlan("claude_team_premium", "tier_3x")));

    [Fact]
    public void Name_WithoutATypeTheAccountFlagsDecide()
    {
        Assert.Equal("Claude Max", PlanFormatter.Name(new SubscriptionPlan(null, null, HasClaudeMax: true)));
        Assert.Equal("Claude Pro", PlanFormatter.Name(new SubscriptionPlan(null, null, HasClaudePro: true)));
    }

    [Fact]
    public void Name_ATypeFromOutsideTheFamilyYieldsToTheFlags() =>
        // "claude_" is the family the type is expected from; anything else is
        // less trustworthy than the account's own flag.
        Assert.Equal(
            "Claude Pro",
            PlanFormatter.Name(new SubscriptionPlan("personal", null, HasClaudePro: true)));

    [Fact]
    public void Name_ATypeFromOutsideTheFamilyIsStillShownWhenNothingElseIs() =>
        Assert.Equal("Claude Personal", PlanFormatter.Name(new SubscriptionPlan("personal", null)));

    [Fact]
    public void Name_NothingToGoOnIsNull()
    {
        Assert.Null(PlanFormatter.Name(null));
        Assert.Null(PlanFormatter.Name(new SubscriptionPlan(null, null)));
        Assert.Null(PlanFormatter.Name(new SubscriptionPlan("  ", "default_claude_max_5x")));
        Assert.Null(PlanFormatter.Name(new SubscriptionPlan("claude_", null)));
    }

    [Fact]
    public void ToPlanLine_CarriesTheNameUntranslated()
    {
        // A product name, the same in every language; only the label around it
        // is translated.
        var line = UsageFormatter.ToPlanLine(new SubscriptionPlan("claude_max", "default_claude_max_5x"));

        Assert.NotNull(line);
        Assert.Contains("Claude Max 5×", line, StringComparison.Ordinal);
    }

    [Fact]
    public void ToPlanLine_AnUnknownPlanHasNoLine()
    {
        Assert.Null(UsageFormatter.ToPlanLine(null));
        Assert.Null(UsageFormatter.ToPlanLine(new SubscriptionPlan(null, null)));
    }

    [Theory]
    [InlineData("default_claude_ai")]
    [InlineData("x")]
    [InlineData("max_5x_extended")]
    [InlineData("tier_1234x")]
    public void Name_OnlyATrailingMultiplierCounts(string tier) =>
        Assert.Equal("Claude Max", PlanFormatter.Name(new SubscriptionPlan("claude_max", tier)));
}
