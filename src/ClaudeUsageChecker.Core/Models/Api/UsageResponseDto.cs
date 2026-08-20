using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Models.Api;

/// <summary>
/// One-to-one mirror of the response of GET /api/oauth/usage. Kept separate from
/// the domain model on purpose, so that changes to the endpoint do not ripple
/// through the whole application.
/// </summary>
internal sealed class UsageResponseDto
{
    [JsonPropertyName("five_hour")]
    public UsageWindowDto? FiveHour { get; set; }

    [JsonPropertyName("seven_day")]
    public UsageWindowDto? SevenDay { get; set; }

    [JsonPropertyName("seven_day_opus")]
    public UsageWindowDto? SevenDayOpus { get; set; }

    [JsonPropertyName("seven_day_sonnet")]
    public UsageWindowDto? SevenDaySonnet { get; set; }

    /// <summary>
    /// The newer, model-independent representation of the same limits.
    /// </summary>
    /// <remarks>
    /// The fields above carry fixed model names in their identifiers and were
    /// therefore empty as soon as Anthropic limited a different model - the
    /// Fable weekly limit appeared in none of them. This list names the model in
    /// its content instead (<c>scope.model.display_name</c>). It therefore takes
    /// precedence; the old fields remain as a fallback in case an older version
    /// of the endpoint answers.
    /// </remarks>
    [JsonPropertyName("limits")]
    public List<LimitDto>? Limits { get; set; }

    [JsonPropertyName("extra_usage")]
    public ExtraUsageDto? ExtraUsage { get; set; }

    /// <summary>
    /// The newer representation of the extra usage quota.
    /// </summary>
    /// <remarks>
    /// Same relationship as between <c>limits</c> and the fixed window fields:
    /// both are delivered, and this one is the shape that says what the figures
    /// actually are. <c>extra_usage.used_credits</c> reads like a count of
    /// credits and is in fact an amount of money in its smallest unit - 2276
    /// meaning 22.76 EUR. Here that is spelled out, with the currency and the
    /// exponent beside the number, so nothing has to be guessed.
    /// </remarks>
    [JsonPropertyName("spend")]
    public SpendDto? Spend { get; set; }
}

/// <summary>The extra usage quota, expressed as money.</summary>
internal sealed class SpendDto
{
    [JsonPropertyName("used")]
    public MoneyDto? Used { get; set; }

    [JsonPropertyName("limit")]
    public MoneyDto? Limit { get; set; }

    [JsonPropertyName("percent")]
    public double? Percent { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>An amount of money in the smallest unit of its currency.</summary>
/// <remarks>
/// <c>amount_minor</c> 2276 with <c>exponent</c> 2 is 22.76. Keeping the
/// exponent beside the number rather than assuming two decimal places is what
/// makes this shape trustworthy - not every currency has two.
/// </remarks>
internal sealed class MoneyDto
{
    [JsonPropertyName("amount_minor")]
    public decimal? AmountMinor { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("exponent")]
    public int? Exponent { get; set; }
}

/// <summary>A single limit from the <c>limits</c> list.</summary>
internal sealed class LimitDto
{
    /// <summary>"session", "weekly_all" or "weekly_scoped".</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>"session" or "weekly".</summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("percent")]
    public double? Percent { get; set; }

    [JsonPropertyName("resets_at")]
    public DateTimeOffset? ResetsAt { get; set; }

    /// <summary>For "weekly_scoped", the model the limit applies to.</summary>
    [JsonPropertyName("scope")]
    public LimitScopeDto? Scope { get; set; }
}

internal sealed class LimitScopeDto
{
    [JsonPropertyName("model")]
    public LimitModelDto? Model { get; set; }
}

internal sealed class LimitModelDto
{
    /// <summary>
    /// Currently delivered empty - the display name is the only usable value.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

internal sealed class UsageWindowDto
{
    [JsonPropertyName("utilization")]
    public double? Utilization { get; set; }

    [JsonPropertyName("resets_at")]
    public DateTimeOffset? ResetsAt { get; set; }
}

internal sealed class ExtraUsageDto
{
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>The monthly cap, in the smallest unit of <see cref="Currency"/>.</summary>
    [JsonPropertyName("monthly_limit")]
    public decimal? MonthlyLimit { get; set; }

    /// <summary>What has been spent, in the smallest unit of <see cref="Currency"/>.</summary>
    /// <remarks>
    /// The name is misleading: these are not credits but money. Measured on
    /// 2026-08-20 against an account with the quota switched on, the field read
    /// 2276 while the account had spent 22.76 EUR.
    /// </remarks>
    [JsonPropertyName("used_credits")]
    public decimal? UsedCredits { get; set; }

    [JsonPropertyName("utilization")]
    public double? Utilization { get; set; }

    /// <summary>ISO code, "EUR" for instance. Absent from older responses.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// How many decimal places the amounts carry. Where it is missing, the
    /// numbers are taken as they are - guessing at two would turn 50 into 0.50.
    /// </summary>
    [JsonPropertyName("decimal_places")]
    public int? DecimalPlaces { get; set; }
}
