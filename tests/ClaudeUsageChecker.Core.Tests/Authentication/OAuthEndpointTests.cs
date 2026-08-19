using System.Net;
using ClaudeUsageChecker.Core.Authentication.OAuth;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Haelt die Eigenheiten des Tokenendpunkts fest, die sich nur durch Messung
/// gegen den echten Server zeigen.
/// </summary>
public class OAuthEndpointTests
{
    [Fact]
    public void DerTokenendpunktZeigtAufPlatformClaudeCom()
    {
        // console.anthropic.com antwortet auf diesem Pfad mit HTTP 404.
        Assert.Equal(
            new Uri("https://platform.claude.com/v1/oauth/token"),
            new OAuthOptions().TokenEndpoint);
    }

    [Fact]
    public async Task DerTauschSchicktImmerEinenStateMit()
    {
        // Ohne state weist der Server den Rumpf mit "Invalid request format" ab.
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a1","expires_in":3600}"""));
        var client = new AnthropicOAuthClient(new HttpClient(handler), new OAuthOptions());
        var request = client.CreateAuthorizationRequest();

        await client.ExchangeCodeAsync("code42", request);

        Assert.Contains($"\"state\":\"{request.State}\"", handler.LastBody, StringComparison.Ordinal);
    }
}
