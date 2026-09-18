using System.Net;
using System.Reflection;
using System.Text;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models.Api;
using Microsoft.Extensions.Time.Testing;

namespace ClaudeUsageChecker.Core.Tests.Api;

/// <summary>
/// Reading the plan from GET /api/oauth/profile.
/// </summary>
public class ProfileClientTests
{
    /// <summary>
    /// The shape measured on 2026-09-18, with every personal value made up. The
    /// personal fields are there on purpose: the client has to read past them.
    /// </summary>
    private const string ProfileJson = """
        {
          "account": {
            "uuid": "00000000-0000-0000-0000-000000000001",
            "full_name": "Example Person",
            "display_name": "Example",
            "email": "person@example.com",
            "has_claude_max": true,
            "has_claude_pro": false,
            "created_at": "2026-01-01T00:00:00Z"
          },
          "organization": {
            "uuid": "00000000-0000-0000-0000-000000000002",
            "name": "Example's Organization",
            "organization_type": "claude_max",
            "billing_type": "stripe_subscription",
            "rate_limit_tier": "default_claude_max_5x",
            "seat_tier": null,
            "has_extra_usage_enabled": false,
            "subscription_status": "active"
          },
          "application": { "uuid": "x", "name": "Claude Code", "slug": "claude-code" },
          "enabled_plugins": []
        }
        """;

    [Fact]
    public async Task ThePlanIsReadFromTheProfile()
    {
        var handler = new StubHandler((HttpStatusCode.OK, ProfileJson));

        var plan = await Client(handler).GetPlanAsync(Token("a"));

        Assert.NotNull(plan);
        Assert.Equal("claude_max", plan.OrganizationType);
        Assert.Equal("default_claude_max_5x", plan.RateLimitTier);
        Assert.True(plan.HasClaudeMax);
        Assert.False(plan.HasClaudePro);
    }

    [Fact]
    public async Task TheRequestLooksLikeTheUsageCall()
    {
        // Same scope, same headers: without the Claude Code user agent the
        // endpoints of this host answer 429 for good.
        var handler = new StubHandler((HttpStatusCode.OK, ProfileJson));

        await Client(handler).GetPlanAsync(Token("a"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.anthropic.com/api/oauth/profile", request.RequestUri!.ToString());
        Assert.Equal("Bearer token-a", request.Headers.Authorization!.ToString());
        Assert.Contains("claude-code/", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        Assert.Equal("oauth-2025-04-20", Assert.Single(request.Headers.GetValues("anthropic-beta")));
    }

    [Fact]
    public async Task TheSameTokenIsNotAskedTwice()
    {
        var handler = new StubHandler((HttpStatusCode.OK, ProfileJson));
        var client = Client(handler);

        await client.GetPlanAsync(Token("a"));
        var second = await client.GetPlanAsync(Token("a"));

        Assert.NotNull(second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ANewTokenIsAskedAgain()
    {
        // A new sign-in brings a new token - possibly for another account.
        var handler = new StubHandler((HttpStatusCode.OK, ProfileJson), (HttpStatusCode.OK, ProfileJson));
        var client = Client(handler);

        await client.GetPlanAsync(Token("a"));
        await client.GetPlanAsync(Token("b"));

        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "{}")]
    [InlineData(HttpStatusCode.TooManyRequests, "{}")]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "not json")]
    [InlineData(HttpStatusCode.OK, "{}")]
    public async Task AnythingButAPlanIsNullNotAnError(HttpStatusCode status, string body)
    {
        var plan = await Client(new StubHandler((status, body))).GetPlanAsync(Token("a"));

        Assert.Null(plan);
    }

    [Fact]
    public async Task ANetworkFailureIsNullNotAnError()
    {
        var plan = await Client(new ThrowingHandler()).GetPlanAsync(Token("a"));

        Assert.Null(plan);
    }

    [Fact]
    public async Task AFailedQuestionRestsForAnHour()
    {
        // Otherwise a refusing profile endpoint would double the calls to a host
        // that throttles.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero));
        var handler = new StubHandler(
            (HttpStatusCode.InternalServerError, "{}"),
            (HttpStatusCode.OK, ProfileJson));
        var client = Client(handler, time);

        Assert.Null(await client.GetPlanAsync(Token("a")));

        time.Advance(AnthropicProfileClient.RetryAfterFailure - TimeSpan.FromMinutes(1));
        Assert.Null(await client.GetPlanAsync(Token("a")));
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.NotNull(await client.GetPlanAsync(Token("a")));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task CancellationIsNotSwallowed()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Client(new StubHandler((HttpStatusCode.OK, ProfileJson))).GetPlanAsync(Token("a"), cancelled.Token));
    }

    /// <summary>
    /// The answer carries name, e-mail and identifiers. What is never mapped
    /// cannot be logged, shown or kept by mistake.
    /// </summary>
    [Fact]
    public void TheProfileCarriesNothingPersonal()
    {
        Type[] types = [typeof(ProfileResponseDto), typeof(ProfileAccountDto), typeof(ProfileOrganizationDto)];

        var mapped = types
            .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(p => p.Name)
            .ToList();

        foreach (var personal in new[] { "Email", "Name", "FullName", "DisplayName", "Uuid", "Id" })
        {
            Assert.DoesNotContain(mapped, name => name.Contains(personal, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static AnthropicProfileClient Client(HttpMessageHandler handler, TimeProvider? time = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") },
            new UsageApiOptions(),
            time);

    private static AccessToken Token(string name) => new($"token-{name}", TokenSource.OAuth);

    /// <summary>Returns the prepared responses in order and keeps what was sent.</summary>
    private sealed class StubHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var index = Requests.Count;
            Requests.Add(request);
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

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("no route to host");
    }
}
