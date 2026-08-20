using System.Net;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Time.Testing;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Checks the automatic refresh of the application's own sign-in.
/// </summary>
public class OAuthTokenProviderTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WithoutASignInNothingIsSupplied()
    {
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), saved: null, out _);

        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task AValidTokenIsSuppliedUnchanged()
    {
        var saved = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddHours(1)
        };
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), saved, out _);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a1", token!.Value);
        Assert.Equal(TokenSource.OAuth, token.Source);
    }

    [Fact]
    public async Task AnExpiringTokenIsRefreshed()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":3600}"""));
        var saved = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            // Expires in two minutes, while the lead time is five.
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, saved, out var store);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a2", token!.Value);
        Assert.Equal("r2", store.Read()!.RefreshToken);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task WithoutANewRefreshTokenThePreviousOneStaysValid()
    {
        // Not every server sends a new refresh token when refreshing.
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","expires_in":3600}"""));
        var saved = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, saved, out var store);

        await provider.TryGetTokenAsync();

        Assert.Equal("r1", store.Read()!.RefreshToken);
        Assert.Equal("a2", store.Read()!.AccessToken);
    }

    [Fact]
    public async Task AFailedRefreshSuppliesNothingAndBreaksNothing()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.BadRequest,
            """{"error":"invalid_grant"}"""));
        var saved = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, saved, out _);

        // Null rather than an exception: that way the next token source gets its turn.
        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task ExpiredAndNotRefreshableSuppliesNothing()
    {
        var saved = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = null,
            ExpiresAt = Jetzt.AddMinutes(-1)
        };
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), saved, out _);

        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task WithoutAKnownExpiryNothingIsRefreshedPreemptively()
    {
        var saved = new OAuthTokens { AccessToken = "a1", RefreshToken = "r1", ExpiresAt = null };
        var handler = new OAuthFlowTests.StubHandler();
        using var provider = CreateProvider(handler, saved, out _);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a1", token!.Value);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void ACorruptedEntryCountsAsNone()
    {
        var secretStore = new FakeSecretStore();
        secretStore.Write(OAuthTokenStore.DefaultKey, "kein json");

        Assert.Null(new OAuthTokenStore(secretStore).Read());
    }

    [Fact]
    public void StoredCredentialsSurviveARoundTrip()
    {
        var store = new OAuthTokenStore(new FakeSecretStore());
        var tokens = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt,
            Scope = "user:profile"
        };

        store.Write(tokens);
        var gelesen = store.Read()!;

        Assert.Equal(tokens.AccessToken, gelesen.AccessToken);
        Assert.Equal(tokens.RefreshToken, gelesen.RefreshToken);
        Assert.Equal(tokens.ExpiresAt, gelesen.ExpiresAt);
        Assert.Equal(tokens.Scope, gelesen.Scope);
    }

    [Fact]
    public void TheRepresentationGivesAwayNoSecret()
    {
        var text = new OAuthTokens { AccessToken = "top-secret", RefreshToken = "also-secret" }.ToString();

        Assert.DoesNotContain("top-secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("also-secret", text, StringComparison.Ordinal);
    }

    private static OAuthTokenProvider CreateProvider(
        OAuthFlowTests.StubHandler handler, OAuthTokens? saved, out OAuthTokenStore store)
    {
        var secretStore = new FakeSecretStore();
        store = new OAuthTokenStore(secretStore);
        if (saved is not null)
        {
            store.Write(saved);
        }

        var zeit = new FakeTimeProvider(Jetzt);
        return new OAuthTokenProvider(
            store,
            new AnthropicOAuthClient(new HttpClient(handler), new OAuthOptions()),
            new OAuthOptions(),
            zeit);
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
