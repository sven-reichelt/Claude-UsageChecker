using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Models.Api;

/// <summary>
/// 1:1-Abbild der Antwort von GET /api/oauth/usage. Bewusst getrennt vom Domaenenmodell,
/// damit Aenderungen am Endpunkt nicht in die gesamte Anwendung durchschlagen.
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

    [JsonPropertyName("extra_usage")]
    public ExtraUsageDto? ExtraUsage { get; set; }
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
