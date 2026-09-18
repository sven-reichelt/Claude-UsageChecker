using System.Net;
using System.Text;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models;

namespace ClaudeUsageChecker.Core.Tests.Api;

/// <summary>
/// The plan travels with the usage figures, and belongs to the same account.
/// </summary>
public class UsagePlanTests
{
    private const string UsageJson =
        """{"five_hour":{"utilization":19.0,"resets_at":"2026-04-11T07:00:00+00:00"}}""";

    private const string ScopeError =
        """{"type":"error","error":{"type":"permission_error","message":"OAuth token does not meet scope requirement user:profile"}}""";

    private static readonly SubscriptionPlan Max = new("claude_max", "default_claude_max_5x", HasClaudeMax: true);

    [Fact]
    public async Task TheSnapshotCarriesThePlan()
    {
        var plans = new RecordingPlanSource(Max);

        var snapshot = await Client(new StubHandler((HttpStatusCode.OK, UsageJson)), plans,
            Source("own", TokenSource.OAuth)).GetUsageAsync();

        Assert.Equal(Max, snapshot.Plan);
        Assert.Equal(19.0, snapshot.Session!.Utilization);
    }

    [Fact]
    public async Task ThePlanIsAskedWithTheTokenThatFetchedTheFigures()
    {
        // The first source is turned away, the second one fetches the figures.
        // Asking with the first would show the plan of an account whose figures
        // are not on screen.
        var plans = new RecordingPlanSource(Max);

        await Client(new StubHandler((HttpStatusCode.Forbidden, ScopeError), (HttpStatusCode.OK, UsageJson)), plans,
            Source("own", TokenSource.OAuth),
            Source("cli", TokenSource.ClaudeCli)).GetUsageAsync();

        var asked = Assert.Single(plans.AskedWith);
        Assert.Equal("token-cli", asked.Value);
    }

    [Fact]
    public async Task WithoutFiguresThePlanIsNotAsked()
    {
        var plans = new RecordingPlanSource(Max);

        await Assert.ThrowsAsync<UsageApiException>(() =>
            Client(new StubHandler((HttpStatusCode.InternalServerError, "{}")), plans,
                Source("own", TokenSource.OAuth)).GetUsageAsync());

        Assert.Empty(plans.AskedWith);
    }

    [Fact]
    public async Task AnUnknownPlanLeavesTheFiguresAlone()
    {
        var snapshot = await Client(new StubHandler((HttpStatusCode.OK, UsageJson)), new RecordingPlanSource(null),
            Source("own", TokenSource.OAuth)).GetUsageAsync();

        Assert.Null(snapshot.Plan);
        Assert.Equal(19.0, snapshot.Session!.Utilization);
    }

    [Fact]
    public async Task WithoutAPlanSourceNothingChanges()
    {
        var handler = new StubHandler((HttpStatusCode.OK, UsageJson));

        var snapshot = await new AnthropicUsageApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") },
            [Source("own", TokenSource.OAuth)],
            new UsageApiOptions()).GetUsageAsync();

        Assert.Null(snapshot.Plan);
        Assert.Equal(1, handler.RequestCount);
    }

    private static AnthropicUsageApiClient Client(
        StubHandler handler, ISubscriptionPlanSource plans, params ITokenProvider[] providers) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") },
            providers,
            new UsageApiOptions(),
            planSource: plans);

    private static ITokenProvider Source(string name, TokenSource source) =>
        new StubProvider(name, new AccessToken($"token-{name}", source));

    private sealed class RecordingPlanSource(SubscriptionPlan? plan) : ISubscriptionPlanSource
    {
        public List<AccessToken> AskedWith { get; } = [];

        public Task<SubscriptionPlan?> GetPlanAsync(AccessToken token, CancellationToken cancellationToken = default)
        {
            AskedWith.Add(token);
            return Task.FromResult(plan);
        }
    }

    private sealed class StubProvider(string name, AccessToken? token) : ITokenProvider
    {
        public string Name => name;

        public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(token);
    }

    private sealed class StubHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = RequestCount++;
            if (index >= responses.Length)
            {
                throw new InvalidOperationException($"Unexpected request no. {index + 1}.");
            }

            var (status, body) = responses[index];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
