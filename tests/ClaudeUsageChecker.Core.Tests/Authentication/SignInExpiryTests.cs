using System.Net;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Time.Testing;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Distinguishes credentials discarded for good from a temporary disturbance.
/// </summary>
/// <remarks>
/// The difference matters: if a merely disturbed connection counts as a
/// rejection, the application deletes an intact sign-in and the user has to sign
/// in again for no reason. The other way round, on a real rejection the display
/// would carry on silently through Claude Code - and the independence would be
/// lost without anyone noticing.
/// </remarks>
public class SignInExpiryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ARejectionRemovesTheSignInAndReportsIt(HttpStatusCode status)
    {
        var handler = new OAuthFlowTests.StubHandler((status, """{"error":"invalid_grant"}"""));
        using var provider = CreateProvider(handler, ExpiringSignIn(), out var store);

        string? reported = null;
        provider.SignInExpired += (_, grund) => reported = grund;

        var token = await provider.TryGetTokenAsync();

        Assert.Null(token);
        Assert.Null(store.Read());
        Assert.NotNull(reported);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task ADisturbanceLeavesTheSignInUntouched(HttpStatusCode status)
    {
        var handler = new OAuthFlowTests.StubHandler((status, """{"error":"nicht erreichbar"}"""));
        using var provider = CreateProvider(handler, ExpiringSignIn(), out var store);

        var reported = false;
        provider.SignInExpired += (_, _) => reported = true;

        var token = await provider.TryGetTokenAsync();

        // No token this time - but the credentials are kept.
        Assert.Null(token);
        Assert.NotNull(store.Read());
        Assert.False(reported);
    }

    [Fact]
    public async Task ANetworkOutageLeavesTheSignInUntouched()
    {
        var handler = new ThrowingHandler();
        using var provider = CreateProvider(handler, ExpiringSignIn(), out var store);

        var reported = false;
        provider.SignInExpired += (_, _) => reported = true;

        Assert.Null(await provider.TryGetTokenAsync());
        Assert.NotNull(store.Read());
        Assert.False(reported);
    }

    [Fact]
    public async Task DieLebensdauerDesRefreshTokensWirdUebernommenWennGemeldet()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":28800,"refresh_token_expires_in":2592000}"""));
        using var provider = CreateProvider(handler, ExpiringSignIn(), out var store);

        await provider.TryGetTokenAsync();

        Assert.NotNull(store.Read()!.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task OhneAngabeBleibtDieLebensdauerUnbekannt()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":28800}"""));
        using var provider = CreateProvider(handler, ExpiringSignIn(), out var store);

        await provider.TryGetTokenAsync();

        Assert.Null(store.Read()!.RefreshTokenExpiresAt);
    }

    private static OAuthTokens ExpiringSignIn() => new()
    {
        AccessToken = "a1",
        RefreshToken = "r1",
        ExpiresAt = Now.AddMinutes(2)
    };

    private static OAuthTokenProvider CreateProvider(
        HttpMessageHandler handler, OAuthTokens saved, out OAuthTokenStore store)
    {
        store = new OAuthTokenStore(new FakeSecretStore());
        store.Write(saved);

        return new OAuthTokenProvider(
            store,
            new AnthropicOAuthClient(new HttpClient(handler), new OAuthOptions()),
            new OAuthOptions(),
            new FakeTimeProvider(Now));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Netzwerk nicht erreichbar");
    }

    private sealed class FakeSecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _entries = [];

        public bool IsSupported => true;

        public string? Read(string key) => _entries.GetValueOrDefault(key);

        public void Write(string key, string secret) => _entries[key] = secret;

        public void Delete(string key) => _entries.Remove(key);
    }
}
