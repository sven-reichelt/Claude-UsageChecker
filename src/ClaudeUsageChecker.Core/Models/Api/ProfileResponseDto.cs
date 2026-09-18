using System.Text.Json.Serialization;

namespace ClaudeUsageChecker.Core.Models.Api;

/// <summary>
/// The part of GET /api/oauth/profile this application reads: the plan, and
/// nothing else.
/// </summary>
/// <remarks>
/// The answer also carries the account holder's name, e-mail address and
/// identifiers. They are deliberately not mapped - what is never read cannot be
/// logged, shown or kept by mistake. <c>TheProfileCarriesNothingPersonal</c>
/// holds it there.
/// </remarks>
internal sealed class ProfileResponseDto
{
    [JsonPropertyName("account")]
    public ProfileAccountDto? Account { get; set; }

    [JsonPropertyName("organization")]
    public ProfileOrganizationDto? Organization { get; set; }
}

internal sealed class ProfileAccountDto
{
    [JsonPropertyName("has_claude_max")]
    public bool? HasClaudeMax { get; set; }

    [JsonPropertyName("has_claude_pro")]
    public bool? HasClaudePro { get; set; }
}

internal sealed class ProfileOrganizationDto
{
    [JsonPropertyName("organization_type")]
    public string? OrganizationType { get; set; }

    [JsonPropertyName("rate_limit_tier")]
    public string? RateLimitTier { get; set; }
}
