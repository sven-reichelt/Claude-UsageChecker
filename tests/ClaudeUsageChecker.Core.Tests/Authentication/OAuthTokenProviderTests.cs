using System.Net;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Time.Testing;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Prueft die selbsttaetige Erneuerung der eigenen Anmeldung.
/// </summary>
public class OAuthTokenProviderTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OhneAnmeldungWirdNichtsGeliefert()
    {
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), gespeichert: null, out _);

        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task EinGueltigesTokenWirdUnveraendertGeliefert()
    {
        var gespeichert = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddHours(1)
        };
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), gespeichert, out _);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a1", token!.Value);
        Assert.Equal(TokenSource.OAuth, token.Source);
    }

    [Fact]
    public async Task EinAblaufendesTokenWirdErneuert()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":3600}"""));
        var gespeichert = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            // Laeuft in zwei Minuten ab, die Vorlaufzeit betraegt fuenf.
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, gespeichert, out var store);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a2", token!.Value);
        Assert.Equal("r2", store.Read()!.RefreshToken);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task OhneNeuenRefreshTokenBleibtDerBisherigeGueltig()
    {
        // Nicht jeder Server liefert beim Erneuern einen neuen Refresh-Token mit.
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","expires_in":3600}"""));
        var gespeichert = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, gespeichert, out var store);

        await provider.TryGetTokenAsync();

        Assert.Equal("r1", store.Read()!.RefreshToken);
        Assert.Equal("a2", store.Read()!.AccessToken);
    }

    [Fact]
    public async Task EineGescheiterteErneuerungLiefertNichtsUndReisstNichtsMit()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.BadRequest,
            """{"error":"invalid_grant"}"""));
        var gespeichert = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = "r1",
            ExpiresAt = Jetzt.AddMinutes(2)
        };
        using var provider = CreateProvider(handler, gespeichert, out _);

        // Null statt Ausnahme: So kommt die naechste Tokenquelle zum Zuge.
        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task AbgelaufenUndNichtErneuerbarLiefertNichts()
    {
        var gespeichert = new OAuthTokens
        {
            AccessToken = "a1",
            RefreshToken = null,
            ExpiresAt = Jetzt.AddMinutes(-1)
        };
        using var provider = CreateProvider(new OAuthFlowTests.StubHandler(), gespeichert, out _);

        Assert.Null(await provider.TryGetTokenAsync());
    }

    [Fact]
    public async Task OhneBekanntenAblaufWirdNichtVorsorglichErneuert()
    {
        var gespeichert = new OAuthTokens { AccessToken = "a1", RefreshToken = "r1", ExpiresAt = null };
        var handler = new OAuthFlowTests.StubHandler();
        using var provider = CreateProvider(handler, gespeichert, out _);

        var token = await provider.TryGetTokenAsync();

        Assert.Equal("a1", token!.Value);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void EinBeschaedigterEintragZaehltWieKeiner()
    {
        var secretStore = new FakeSecretStore();
        secretStore.Write(OAuthTokenStore.DefaultKey, "kein json");

        Assert.Null(new OAuthTokenStore(secretStore).Read());
    }

    [Fact]
    public void GespeicherteAnmeldedatenUeberstehenEinenRundlauf()
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
    public void DieDarstellungGibtKeinGeheimnisPreis()
    {
        var text = new OAuthTokens { AccessToken = "streng-geheim", RefreshToken = "auch-geheim" }.ToString();

        Assert.DoesNotContain("streng-geheim", text, StringComparison.Ordinal);
        Assert.DoesNotContain("auch-geheim", text, StringComparison.Ordinal);
    }

    private static OAuthTokenProvider CreateProvider(
        OAuthFlowTests.StubHandler handler, OAuthTokens? gespeichert, out OAuthTokenStore store)
    {
        var secretStore = new FakeSecretStore();
        store = new OAuthTokenStore(secretStore);
        if (gespeichert is not null)
        {
            store.Write(gespeichert);
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
