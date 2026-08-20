using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

public class ClaudeCliTokenProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 11, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Parse_ReadsAccessTokenAndExpiry()
    {
        var expiresAt = Now.AddHours(1);
        var json = $$"""
            {
              "claudeAiOauth": {
                "accessToken": "test-access-token",
                "refreshToken": "test-refresh-token",
                "expiresAt": {{expiresAt.ToUnixTimeMilliseconds()}},
                "subscriptionType": "max"
              }
            }
            """;

        var token = ClaudeCliTokenProvider.Parse(json, Now);

        Assert.NotNull(token);
        Assert.Equal("test-access-token", token!.Value);
        Assert.Equal(TokenSource.ClaudeCli, token.Source);
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public void Parse_ReturnsNullWithoutAnAccessToken()
    {
        const string json = """{ "claudeAiOauth": { "refreshToken": "nur-refresh" } }""";

        Assert.Null(ClaudeCliTokenProvider.Parse(json, Now));
    }

    [Fact]
    public void Parse_ReturnsNullForInvalidJson()
    {
        Assert.Null(ClaudeCliTokenProvider.Parse("kein json", Now));
    }

    [Fact]
    public void Parse_CopesWithoutAnExpiry()
    {
        const string json = """{ "claudeAiOauth": { "accessToken": "abc" } }""";

        var token = ClaudeCliTokenProvider.Parse(json, Now);

        Assert.NotNull(token);
        Assert.Null(token!.ExpiresAt);
        Assert.False(token.IsExpired(Now, TimeSpan.Zero));
    }

    [Fact]
    public void ToString_DoesNotGiveAwayTheTokenValue()
    {
        var token = new AccessToken("top-secret", TokenSource.SecretStore);

        Assert.DoesNotContain("top-secret", token.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-10, true)]
    [InlineData(10, false)]
    public void IsExpired_HonoursTheSkew(int minutesFromNow, bool expected)
    {
        var token = new AccessToken("t", TokenSource.ClaudeCli, Now.AddMinutes(minutesFromNow));

        Assert.Equal(expected, token.IsExpired(Now, TimeSpan.FromMinutes(5)));
    }
}
