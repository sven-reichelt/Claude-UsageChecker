using System.Text.Json;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Models.Api;

namespace ClaudeUsageChecker.Core.Tests.Api;

/// <summary>
/// Checks the reading of model-specific weekly limits from the <c>limits</c>
/// list.
/// </summary>
/// <remarks>
/// The occasion: the Fable weekly limit was shown nowhere. The old individual
/// fields carry the model name in their identifier - <c>seven_day_opus</c>,
/// <c>seven_day_sonnet</c> - and both stayed empty when Anthropic moved to
/// Fable. There is no field <c>seven_day_fable</c>; the limit lives only in the
/// list, with the name in its content. The samples here are modelled on a real
/// call from 2026-08-19.
/// </remarks>
public class ScopedLimitMappingTests
{
    /// <summary>The response in the form the endpoint delivers today.</summary>
    private const string HeutigeAntwort = """
        {
            "five_hour":        { "utilization": 6.0,  "resets_at": "2026-08-20T00:30:00+00:00" },
            "seven_day":        { "utilization": 18.0, "resets_at": "2026-08-23T01:00:00+00:00" },
            "seven_day_opus":   null,
            "seven_day_sonnet": null,
            "limits": [
                {
                    "kind": "session", "group": "session", "percent": 6,
                    "resets_at": "2026-08-20T00:30:00+00:00", "scope": null
                },
                {
                    "kind": "weekly_all", "group": "weekly", "percent": 18,
                    "resets_at": "2026-08-23T01:00:00+00:00", "scope": null
                },
                {
                    "kind": "weekly_scoped", "group": "weekly", "percent": 2,
                    "resets_at": "2026-08-23T01:00:00+00:00",
                    "scope": { "model": { "id": null, "display_name": "Fable" }, "surface": null }
                }
            ]
        }
        """;

    [Fact]
    public void MapToSnapshot_FindsTheModelSpecificWeeklyLimit()
    {
        var snapshot = Map(HeutigeAntwort);

        var fable = Assert.Single(snapshot.ScopedWeekly);
        Assert.Equal("Fable", fable.ModelName);
        Assert.Equal(2d, fable.Window.Utilization);
    }

    [Fact]
    public void MapToSnapshot_LiestSitzungUndWochenlimitAusDerListe()
    {
        var snapshot = Map(HeutigeAntwort);

        Assert.Equal(6d, snapshot.Session!.Utilization);
        Assert.Equal(18d, snapshot.Weekly!.Utilization);
    }

    [Fact]
    public void MapToSnapshot_TakesUpEveryReportedModel()
    {
        // Future models should appear without a code change - which is exactly
        // where the previous solution failed.
        const string json = """
            {
                "limits": [
                    {
                        "kind": "weekly_scoped", "percent": 5, "resets_at": "2026-08-23T01:00:00+00:00",
                        "scope": { "model": { "display_name": "Fable" } }
                    },
                    {
                        "kind": "weekly_scoped", "percent": 7, "resets_at": "2026-08-23T01:00:00+00:00",
                        "scope": { "model": { "display_name": "Ein neues Modell" } }
                    }
                ]
            }
            """;

        var snapshot = Map(json);

        Assert.Equal(["Fable", "Ein neues Modell"], snapshot.ScopedWeekly.Select(w => w.ModelName));
    }

    [Fact]
    public void MapToSnapshot_SkipsLimitsWithoutAModelName()
    {
        const string json = """
            {
                "limits": [
                    {
                        "kind": "weekly_scoped", "percent": 5, "resets_at": "2026-08-23T01:00:00+00:00",
                        "scope": { "model": { "display_name": null } }
                    }
                ]
            }
            """;

        // A row without a label would be worthless in the display.
        Assert.Empty(Map(json).ScopedWeekly);
    }

    /// <summary>
    /// Where the list falls away, the old individual fields have to step in -
    /// otherwise a step back by the endpoint would waste the whole display.
    /// </summary>
    [Fact]
    public void MapToSnapshot_FallsBackToTheIndividualFieldsWithoutTheList()
    {
        const string json = """
            {
                "five_hour":        { "utilization": 33.0, "resets_at": "2026-04-11T07:00:00+00:00" },
                "seven_day":        { "utilization": 13.0, "resets_at": "2026-04-17T00:59:59+00:00" },
                "seven_day_opus":   { "utilization": 40.0, "resets_at": "2026-04-16T03:00:00+00:00" },
                "seven_day_sonnet": { "utilization": 1.0,  "resets_at": "2026-04-16T03:00:00+00:00" }
            }
            """;

        var snapshot = Map(json);

        Assert.Equal(33d, snapshot.Session!.Utilization);
        Assert.Equal(["Opus", "Sonnet"], snapshot.ScopedWeekly.Select(w => w.ModelName));
    }

    /// <summary>
    /// The list takes precedence: were it to hold values differing from the old
    /// fields, the old ones would be the outdated ones.
    /// </summary>
    [Fact]
    public void MapToSnapshot_PrefersTheListOverTheIndividualFields()
    {
        const string json = """
            {
                "seven_day_sonnet": { "utilization": 99.0, "resets_at": "2026-04-16T03:00:00+00:00" },
                "limits": [
                    {
                        "kind": "weekly_scoped", "percent": 2, "resets_at": "2026-08-23T01:00:00+00:00",
                        "scope": { "model": { "display_name": "Fable" } }
                    }
                ]
            }
            """;

        var snapshot = Map(json);

        var einziges = Assert.Single(snapshot.ScopedWeekly);
        Assert.Equal("Fable", einziges.ModelName);
    }

    [Fact]
    public void MapToSnapshot_SkipsLimitsWithoutAResetMoment()
    {
        const string json = """
            {
                "limits": [
                    {
                        "kind": "weekly_scoped", "percent": 5,
                        "scope": { "model": { "display_name": "Fable" } }
                    }
                ]
            }
            """;

        Assert.Empty(Map(json).ScopedWeekly);
    }

    [Fact]
    public void MapToSnapshot_CopesWithAnEmptyList()
    {
        var snapshot = Map("""{ "limits": [] }""");

        Assert.Empty(snapshot.ScopedWeekly);
        Assert.Null(snapshot.Session);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static Core.Models.UsageSnapshot Map(string json) =>
        AnthropicUsageApiClient.MapToSnapshot(
            JsonSerializer.Deserialize<UsageResponseDto>(json, JsonOptions)!, DateTimeOffset.UnixEpoch);
}
