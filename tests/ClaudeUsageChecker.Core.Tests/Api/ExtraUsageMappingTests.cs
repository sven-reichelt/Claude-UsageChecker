using System.Globalization;
using System.Text.Json;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Models.Api;

namespace ClaudeUsageChecker.Core.Tests.Api;

/// <summary>
/// Checks how the extra usage quota is read.
/// </summary>
/// <remarks>
/// The occasion is concrete. With the quota switched on, the endpoint reports
/// <c>used_credits: 2276</c> - and that is not a count of credits but 22.76 EUR.
/// Taken at face value the application would have claimed "2276.00 of 5000.00
/// credits", wrong by a factor of a hundred and in the wrong unit. The numbers
/// below are the ones the live endpoint returned on 2026-08-20.
///
/// The currency belongs to the account: euros here, dollars for someone in the
/// United States, reais in Brazil. Nothing about it may be assumed, which is why
/// both the code and the number of decimal places are read from the response.
/// </remarks>
public class ExtraUsageMappingTests
{
    [Fact]
    public void MapToSnapshot_ReadsSpendAsMoney()
    {
        const string json = """
            {
                "spend": {
                    "used":  { "amount_minor": 2276, "currency": "EUR", "exponent": 2 },
                    "limit": { "amount_minor": 5000, "currency": "EUR", "exponent": 2 },
                    "percent": 46,
                    "enabled": true
                }
            }
            """;

        var extra = Snapshot(json).ExtraUsage;

        Assert.NotNull(extra);
        Assert.True(extra.IsEnabled);
        Assert.Equal(22.76m, extra.Used);
        Assert.Equal(50.00m, extra.Limit);
        Assert.Equal(46d, extra.Utilization);
        Assert.Equal("EUR", extra.Currency);
    }

    /// <summary>
    /// The older field carries the same figures, only with the scale in a
    /// separate place.
    /// </summary>
    [Fact]
    public void MapToSnapshot_ReadsExtraUsageWithItsDecimalPlaces()
    {
        const string json = """
            {
                "extra_usage": {
                    "is_enabled": true,
                    "monthly_limit": 5000,
                    "used_credits": 2276.0,
                    "utilization": 45.52,
                    "currency": "EUR",
                    "decimal_places": 2
                }
            }
            """;

        var extra = Snapshot(json).ExtraUsage;

        Assert.NotNull(extra);
        Assert.Equal(22.76m, extra.Used);
        Assert.Equal(50.00m, extra.Limit);
        Assert.Equal("EUR", extra.Currency);
    }

    /// <summary>
    /// Without a stated scale the numbers stand as they are.
    /// </summary>
    /// <remarks>
    /// Assuming two places would turn a limit of 50 into 0.50 - understating
    /// what somebody is allowed to spend is the worse of the two mistakes.
    /// </remarks>
    [Fact]
    public void MapToSnapshot_LeavesTheFiguresAloneWithoutADecimalPlace()
    {
        const string json = """
            {
                "extra_usage": {
                    "is_enabled": true,
                    "monthly_limit": 50,
                    "used_credits": 12.5,
                    "utilization": 25.0
                }
            }
            """;

        var extra = Snapshot(json).ExtraUsage;

        Assert.NotNull(extra);
        Assert.Equal(12.5m, extra.Used);
        Assert.Equal(50m, extra.Limit);
        Assert.Null(extra.Currency);
    }

    /// <summary>
    /// Where both shapes arrive, the one that says what it means wins.
    /// </summary>
    [Fact]
    public void MapToSnapshot_PrefersSpendOverExtraUsage()
    {
        const string json = """
            {
                "extra_usage": {
                    "is_enabled": true, "monthly_limit": 9900, "used_credits": 1100,
                    "currency": "EUR", "decimal_places": 2
                },
                "spend": {
                    "used":  { "amount_minor": 2276, "currency": "EUR", "exponent": 2 },
                    "limit": { "amount_minor": 5000, "currency": "EUR", "exponent": 2 },
                    "percent": 46, "enabled": true
                }
            }
            """;

        var extra = Snapshot(json).ExtraUsage;

        Assert.Equal(22.76m, extra!.Used);
        Assert.Equal(50.00m, extra.Limit);
    }

    /// <summary>
    /// A different account, a different currency - and a different number of
    /// decimal places is possible too.
    /// </summary>
    [Theory]
    [InlineData("USD", 2, 1250, 12.50)]
    [InlineData("BRL", 2, 9999, 99.99)]
    [InlineData("JPY", 0, 1500, 1500)]
    public void MapToSnapshot_TakesTheCurrencyFromTheResponse(
        string currency, int exponent, int minor, decimal expected)
    {
        var json = $$"""
            {
                "spend": {
                    "used": { "amount_minor": {{minor}}, "currency": "{{currency}}", "exponent": {{exponent}} },
                    "percent": 10, "enabled": true
                }
            }
            """;

        var extra = Snapshot(json).ExtraUsage;

        Assert.Equal(expected, extra!.Used);
        Assert.Equal(currency, extra.Currency);
    }

    /// <summary>
    /// The amount is written in the culture of the interface, the currency as
    /// the API named it.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "1.234,50 EUR")]
    [InlineData("en-US", "1,234.50 EUR")]
    public void Format_WritesTheAmountInTheCurrentCulture(string code, string expected)
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(code);
        try
        {
            Assert.Equal(expected, MoneyFormatter.Format(1234.5m, "EUR", 2));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void Format_LeavesOutACurrencyItWasNotGiven() =>
        Assert.DoesNotContain(
            " ", MoneyFormatter.Format(12.5m, null, 2), StringComparison.Ordinal);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static UsageSnapshot Snapshot(string json) =>
        AnthropicUsageApiClient.MapToSnapshot(
            JsonSerializer.Deserialize<UsageResponseDto>(json, JsonOptions)!, DateTimeOffset.UnixEpoch);
}
