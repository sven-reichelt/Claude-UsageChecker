using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

public class OAuthFlowTests
{
    [Fact]
    public void PkcePaar_ErfuelltDieVorgabenVonRfc7636()
    {
        var pkce = PkceChallenge.Create();

        // Der Verifier muss zwischen 43 und 128 Zeichen lang sein.
        Assert.InRange(pkce.Verifier.Length, 43, 128);

        // base64url: keine Auffuellzeichen, kein Plus, kein Schraegstrich.
        Assert.DoesNotContain("=", pkce.Verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("+", pkce.Challenge, StringComparison.Ordinal);
        Assert.DoesNotContain("/", pkce.Challenge, StringComparison.Ordinal);
        Assert.DoesNotContain("=", pkce.Challenge, StringComparison.Ordinal);
    }

    [Fact]
    public void PkceChallenge_IstDerSha256AbdruckDesVerifiers()
    {
        var pkce = PkceChallenge.Create();

        var erwartet = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(pkce.Verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.Equal(erwartet, pkce.Challenge);
    }

    [Fact]
    public void JedeAnfrageBekommtEigeneGeheimnisse()
    {
        var client = CreateClient(new StubHandler());

        var a = client.CreateAuthorizationRequest();
        var b = client.CreateAuthorizationRequest();

        Assert.NotEqual(a.CodeVerifier, b.CodeVerifier);
        Assert.NotEqual(a.State, b.State);
    }

    [Fact]
    public void DieAnmeldeadresseTraegtAlleErforderlichenParameter()
    {
        var client = CreateClient(new StubHandler());

        var request = client.CreateAuthorizationRequest();
        var query = HttpUtility.ParseQueryString(request.Url.Query);

        Assert.Equal("https://claude.ai/oauth/authorize", request.Url.GetLeftPart(UriPartial.Path));
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("9d1c250a-e61b-44d9-88ed-5944d1962f5e", query["client_id"]);
        Assert.Equal("https://console.anthropic.com/oauth/code/callback", query["redirect_uri"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(request.State, query["state"]);
        Assert.NotNull(query["code_challenge"]);
    }

    [Fact]
    public void DerAngeforderteGeltungsbereichBleibtBeimNoetigenMinimum()
    {
        var client = CreateClient(new StubHandler());

        var query = HttpUtility.ParseQueryString(client.CreateAuthorizationRequest().Url.Query);

        // Genau das verlangt der Nutzungsendpunkt - und nichts darueber hinaus.
        // Insbesondere kein user:inference und kein org:create_api_key.
        Assert.Equal("user:profile", query["scope"]);
    }

    [Theory]
    [InlineData("abc123", "abc123", null)]
    [InlineData("abc123#xyz", "abc123", "xyz")]
    [InlineData("  abc123#xyz  ", "abc123", "xyz")]
    [InlineData("abc123#", "abc123", null)]
    public void EingefuegterCodeWirdRichtigZerlegt(string eingabe, string code, string? state)
    {
        var (tatsaechlichCode, tatsaechlichState) = AnthropicOAuthClient.SplitPastedCode(eingabe);

        Assert.Equal(code, tatsaechlichCode);
        Assert.Equal(state, tatsaechlichState);
    }

    [Fact]
    public async Task DerTauschSchicktVerifierUndCodeMit()
    {
        var handler = new StubHandler((HttpStatusCode.OK,
            """{"access_token":"neu","refresh_token":"r1","expires_in":3600,"scope":"user:profile"}"""));
        var client = CreateClient(handler);
        var request = client.CreateAuthorizationRequest();

        var tokens = await client.ExchangeCodeAsync($"code42#{request.State}", request);

        Assert.Equal("neu", tokens.AccessToken);
        Assert.Equal("r1", tokens.RefreshToken);
        Assert.Equal("user:profile", tokens.Scope);
        Assert.NotNull(tokens.ExpiresAt);

        Assert.Contains("\"code\":\"code42\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains($"\"code_verifier\":\"{request.CodeVerifier}\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"grant_type\":\"authorization_code\"", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EinCodeAusEinemAnderenVorgangWirdAbgelehnt()
    {
        var handler = new StubHandler();
        var client = CreateClient(handler);
        var request = client.CreateAuthorizationRequest();

        var ex = await Assert.ThrowsAsync<OAuthException>(
            () => client.ExchangeCodeAsync("code42#fremder-vorgang", request));

        Assert.Contains("anderen Anmeldevorgang", ex.Message, StringComparison.Ordinal);
        // Ein solcher Code darf gar nicht erst abgeschickt werden.
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task EineFehlerantwortWirdVerstaendlichGemeldet()
    {
        var handler = new StubHandler((HttpStatusCode.BadRequest,
            """{"error":"invalid_grant","error_description":"Der Code ist abgelaufen."}"""));
        var client = CreateClient(handler);
        var request = client.CreateAuthorizationRequest();

        var ex = await Assert.ThrowsAsync<OAuthException>(
            () => client.ExchangeCodeAsync("code42", request));

        Assert.Contains("Der Code ist abgelaufen.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EineAntwortOhneTokenGiltAlsFehlschlag()
    {
        var handler = new StubHandler((HttpStatusCode.OK, """{"scope":"user:profile"}"""));
        var client = CreateClient(handler);
        var request = client.CreateAuthorizationRequest();

        await Assert.ThrowsAsync<OAuthException>(() => client.ExchangeCodeAsync("code42", request));
    }

    [Fact]
    public async Task DasErneuernSchicktDenRefreshTokenMit()
    {
        var handler = new StubHandler((HttpStatusCode.OK,
            """{"access_token":"neu2","refresh_token":"r2","expires_in":3600}"""));
        var client = CreateClient(handler);

        var tokens = await client.RefreshAsync("r1");

        Assert.Equal("neu2", tokens.AccessToken);
        Assert.Contains("\"grant_type\":\"refresh_token\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"refresh_token\":\"r1\"", handler.LastBody, StringComparison.Ordinal);
        // Beim Erneuern gehoeren Code und Verifier nicht in die Anfrage.
        Assert.DoesNotContain("code_verifier", handler.LastBody, StringComparison.Ordinal);
    }

    private static AnthropicOAuthClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler), new OAuthOptions());

    internal sealed class StubHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var index = RequestCount++;
            if (index >= responses.Length)
            {
                throw new InvalidOperationException($"Unerwartete Anfrage Nr. {index + 1}.");
            }

            var (status, body) = responses[index];
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
