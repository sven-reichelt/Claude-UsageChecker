using System.Net;
using System.Text;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;

namespace ClaudeUsageChecker.Core.Tests.Api;

/// <summary>
/// Makes sure a rejected token source does not block the next one.
/// </summary>
/// <remarks>
/// The occasion is concrete: a token from <c>claude setup-token</c> is valid but
/// does not carry the <c>user:profile</c> scope and is turned away by the usage
/// endpoint with HTTP 403. If the application does not pass on to the
/// naechste Quelle weiter, legt ein solches Token sie vollstaendig lahm.
/// </remarks>
public class TokenFallbackTests
{
    private const string ScopeError =
        """{"type":"error","error":{"type":"permission_error","message":"OAuth token does not meet scope requirement user:profile"}}""";

    private const string UsageJson =
        """{"five_hour":{"utilization":19.0,"resets_at":"2026-04-11T07:00:00+00:00"}}""";

    [Fact]
    public async Task ARejectedTokenMovesOnToTheNextSource()
    {
        var handler = new StubHandler(
            (HttpStatusCode.Forbidden, ScopeError),
            (HttpStatusCode.OK, UsageJson));

        var snapshot = await CreateClient(handler,
            Quelle("secret-store", TokenSource.SecretStore),
            Quelle("claude-cli", TokenSource.ClaudeCli)).GetUsageAsync();

        Assert.Equal(19.0, snapshot.Session!.Utilization);
        Assert.Equal(TokenSource.ClaudeCli, snapshot.TokenSource);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task TheFirstUsableSourceWinsWithoutFurtherCalls()
    {
        var handler = new StubHandler((HttpStatusCode.OK, UsageJson));

        var snapshot = await CreateClient(handler,
            Quelle("secret-store", TokenSource.SecretStore),
            Quelle("claude-cli", TokenSource.ClaudeCli)).GetUsageAsync();

        Assert.Equal(TokenSource.SecretStore, snapshot.TokenSource);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task EmptySourcesAreSkipped()
    {
        var handler = new StubHandler((HttpStatusCode.OK, UsageJson));

        var snapshot = await CreateClient(handler,
            Leer("secret-store"),
            Quelle("claude-cli", TokenSource.ClaudeCli)).GetUsageAsync();

        Assert.Equal(TokenSource.ClaudeCli, snapshot.TokenSource);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task WhenAllAreRejectedTheLastMessageStands()
    {
        var handler = new StubHandler(
            (HttpStatusCode.Forbidden, ScopeError),
            (HttpStatusCode.Unauthorized, "{}"));

        var client = CreateClient(handler,
            Quelle("secret-store", TokenSource.SecretStore),
            Quelle("claude-cli", TokenSource.ClaudeCli));

        var ex = await Assert.ThrowsAsync<UsageApiException>(() => client.GetUsageAsync());

        Assert.Equal(UsageApiFailure.Unauthorized, ex.Failure);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task TheScopeHintIsPassedOn()
    {
        var handler = new StubHandler((HttpStatusCode.Forbidden, ScopeError));

        var client = CreateClient(handler, Quelle("secret-store", TokenSource.SecretStore));

        var ex = await Assert.ThrowsAsync<UsageApiException>(() => client.GetUsageAsync());

        Assert.Contains("user:profile", ex.Message, StringComparison.Ordinal);
        Assert.Contains("setup-token", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutAnySourceAMissingSetupIsReported()
    {
        var handler = new StubHandler();

        var client = CreateClient(handler, Leer("secret-store"), Leer("claude-cli"));

        var ex = await Assert.ThrowsAsync<UsageApiException>(() => client.GetUsageAsync());

        Assert.Equal(UsageApiFailure.NoToken, ex.Failure);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task AFaultySourceDoesNotEndTheSearch()
    {
        var handler = new StubHandler((HttpStatusCode.OK, UsageJson));

        var snapshot = await CreateClient(handler,
            new ThrowingProvider(),
            Quelle("claude-cli", TokenSource.ClaudeCli)).GetUsageAsync();

        Assert.Equal(TokenSource.ClaudeCli, snapshot.TokenSource);
    }

    [Fact]
    public async Task ANetworkErrorIsNotTreatedAsARejection()
    {
        // An outage must not lead to every source being tried and needless
        // requests being sent.
        var handler = new StubHandler((HttpStatusCode.ServiceUnavailable, "{}"));

        var client = CreateClient(handler,
            Quelle("secret-store", TokenSource.SecretStore),
            Quelle("claude-cli", TokenSource.ClaudeCli));

        var ex = await Assert.ThrowsAsync<UsageApiException>(() => client.GetUsageAsync());

        Assert.Equal(UsageApiFailure.Server, ex.Failure);
        Assert.Equal(1, handler.RequestCount);
    }

    private static AnthropicUsageApiClient CreateClient(StubHandler handler, params ITokenProvider[] providers) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") },
            providers,
            new UsageApiOptions());

    private static ITokenProvider Quelle(string name, TokenSource source) =>
        new StubProvider(name, new AccessToken($"token-{name}", source));

    private static ITokenProvider Leer(string name) => new StubProvider(name, null);

    private sealed class StubProvider(string name, AccessToken? token) : ITokenProvider
    {
        public string Name => name;

        public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(token);
    }

    private sealed class ThrowingProvider : ITokenProvider
    {
        public string Name => "kaputt";

        public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default) =>
            throw new IOException("Quelle nicht lesbar");
    }

    /// <summary>Returns the prepared responses in order.</summary>
    private sealed class StubHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = RequestCount++;
            if (index >= responses.Length)
            {
                throw new InvalidOperationException($"Unerwartete Anfrage Nr. {index + 1}.");
            }

            var (status, body) = responses[index];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
