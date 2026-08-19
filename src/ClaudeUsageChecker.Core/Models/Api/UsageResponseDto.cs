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

    [JsonPropertyName("monthly_limit")]
    public decimal? MonthlyLimit { get; set; }

    [JsonPropertyName("used_credits")]
    public decimal? UsedCredits { get; set; }

    [JsonPropertyName("utilization")]
    public double? Utilization { get; set; }
}
