using System.Text.Json;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Models.Api;

namespace ClaudeUsageChecker.Core.Tests.Api;

public class UsageResponseMappingTests
{
    /// <summary>The endpoint response in its documented form.</summary>
    private const string SampleJson = """
        {
            "five_hour":        { "utilization": 33.0, "resets_at": "2026-04-11T07:00:00.528743+00:00" },
            "seven_day":        { "utilization": 13.0, "resets_at": "2026-04-17T00:59:59.951713+00:00" },
            "seven_day_opus":   null,
            "seven_day_sonnet": { "utilization": 1.0,  "resets_at": "2026-04-16T03:00:00.951719+00:00" },
            "extra_usage": {
                "is_enabled": false, "monthly_limit": null, "used_credits": null, "utilization": null
            }
        }
        """;

    [Fact]
    public void MapToSnapshot_ReadsEveryWindow()
    {
        var dto = Deserialize(SampleJson);
        var retrievedAt = new DateTimeOffset(2026, 4, 11, 5, 0, 0, TimeSpan.Zero);

        var snapshot = AnthropicUsageApiClient.MapToSnapshot(dto, retrievedAt);

        Assert.Equal(33.0, snapshot.Session!.Utilization);
        Assert.Equal(13.0, snapshot.Weekly!.Utilization);
        Assert.Equal(1.0, snapshot.ScopedWeekly.Single(w => w.ModelName == "Sonnet").Window.Utilization);
        Assert.Equal(retrievedAt, snapshot.RetrievedAt);
    }

    [Fact]
    public void MapToSnapshot_TreatsMissingWindowsAsNull()
    {
        var dto = Deserialize(SampleJson);

        var snapshot = AnthropicUsageApiClient.MapToSnapshot(dto, DateTimeOffset.UnixEpoch);

        Assert.DoesNotContain(snapshot.ScopedWeekly, w => w.ModelName == "Opus");
    }

    [Fact]
    public void MapToSnapshot_TakesOverTheExtraUsage()
    {
        var dto = Deserialize(SampleJson);

        var snapshot = AnthropicUsageApiClient.MapToSnapshot(dto, DateTimeOffset.UnixEpoch);

        Assert.NotNull(snapshot.ExtraUsage);
        Assert.False(snapshot.ExtraUsage!.IsEnabled);
    }

    [Fact]
    public void MapToSnapshot_ClampsOutliersToOneHundredPercent()
    {
        const string json = """
            { "five_hour": { "utilization": 142.5, "resets_at": "2026-04-11T07:00:00+00:00" } }
            """;

        var snapshot = AnthropicUsageApiClient.MapToSnapshot(Deserialize(json), DateTimeOffset.UnixEpoch);

        Assert.Equal(100d, snapshot.Session!.Utilization);
    }

    [Fact]
    public void MapToSnapshot_IgnoresWindowsWithoutAResetMoment()
    {
        const string json = """{ "five_hour": { "utilization": 50.0 } }""";

        var snapshot = AnthropicUsageApiClient.MapToSnapshot(Deserialize(json), DateTimeOffset.UnixEpoch);

        Assert.Null(snapshot.Session);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static UsageResponseDto Deserialize(string json) =>
        JsonSerializer.Deserialize<UsageResponseDto>(json, JsonOptions)!;
}
