using System.Net;
using ClaudeUsageChecker.Core.Authentication.OAuth;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Records the peculiarities of the token endpoint that only show up when
/// measured against the real server.
/// </summary>
public class OAuthEndpointTests
{
    [Fact]
    public void TheTokenEndpointPointsAtPlatformClaudeCom()
    {
        // console.anthropic.com answers HTTP 404 on this path.
        Assert.Equal(
            new Uri("https://platform.claude.com/v1/oauth/token"),
            new OAuthOptions().TokenEndpoint);
    }

    [Fact]
    public async Task TheExchangeAlwaysSendsAState()
    {
        // Without state the server rejects the body with "Invalid request format".
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a1","expires_in":3600}"""));
        var client = new AnthropicOAuthClient(new HttpClient(handler), new OAuthOptions());
        var request = client.CreateAuthorizationRequest();

        await client.ExchangeCodeAsync("code42", request);

        Assert.Contains($"\"state\":\"{request.State}\"", handler.LastBody, StringComparison.Ordinal);
    }
}
