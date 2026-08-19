using System.Net;
using ClaudeUsageChecker.Core.Authentication.OAuth;
using ClaudeUsageChecker.Core.Platform;
using Microsoft.Extensions.Time.Testing;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

/// <summary>
/// Unterscheidet endgueltig verworfene Anmeldedaten von voruebergehenden
/// Stoerungen.
/// </summary>
/// <remarks>
/// Der Unterschied ist folgenreich: Wird eine bloss gestoerte Verbindung als
/// Ablehnung gewertet, loescht die Anwendung eine intakte Anmeldung und der
/// Nutzer muss sich grundlos neu anmelden. Umgekehrt liefe die Anzeige bei
/// einer echten Ablehnung stillschweigend ueber Claude Code weiter - die
/// Unabhaengigkeit waere unbemerkt verloren.
/// </remarks>
public class SignInExpiryTests
{
    private static readonly DateTimeOffset Jetzt = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task EineAblehnungEntferntDieAnmeldungUndMeldetSie(HttpStatusCode status)
    {
        var handler = new OAuthFlowTests.StubHandler((status, """{"error":"invalid_grant"}"""));
        using var provider = CreateProvider(handler, AblaufendeAnmeldung(), out var store);

        string? gemeldet = null;
        provider.SignInExpired += (_, grund) => gemeldet = grund;

        var token = await provider.TryGetTokenAsync();

        Assert.Null(token);
        Assert.Null(store.Read());
        Assert.NotNull(gemeldet);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task EineStoerungLaesstDieAnmeldungUnangetastet(HttpStatusCode status)
    {
        var handler = new OAuthFlowTests.StubHandler((status, """{"error":"nicht erreichbar"}"""));
        using var provider = CreateProvider(handler, AblaufendeAnmeldung(), out var store);

        var gemeldet = false;
        provider.SignInExpired += (_, _) => gemeldet = true;

        var token = await provider.TryGetTokenAsync();

        // Kein Token diesmal - aber die Anmeldedaten bleiben erhalten.
        Assert.Null(token);
        Assert.NotNull(store.Read());
        Assert.False(gemeldet);
    }

    [Fact]
    public async Task EinNetzwerkausfallLaesstDieAnmeldungUnangetastet()
    {
        var handler = new ThrowingHandler();
        using var provider = CreateProvider(handler, AblaufendeAnmeldung(), out var store);

        var gemeldet = false;
        provider.SignInExpired += (_, _) => gemeldet = true;

        Assert.Null(await provider.TryGetTokenAsync());
        Assert.NotNull(store.Read());
        Assert.False(gemeldet);
    }

    [Fact]
    public async Task DieLebensdauerDesRefreshTokensWirdUebernommenWennGemeldet()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":28800,"refresh_token_expires_in":2592000}"""));
        using var provider = CreateProvider(handler, AblaufendeAnmeldung(), out var store);

        await provider.TryGetTokenAsync();

        Assert.NotNull(store.Read()!.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task OhneAngabeBleibtDieLebensdauerUnbekannt()
    {
        var handler = new OAuthFlowTests.StubHandler((HttpStatusCode.OK,
            """{"access_token":"a2","refresh_token":"r2","expires_in":28800}"""));
        using var provider = CreateProvider(handler, AblaufendeAnmeldung(), out var store);

        await provider.TryGetTokenAsync();

        Assert.Null(store.Read()!.RefreshTokenExpiresAt);
    }

    private static OAuthTokens AblaufendeAnmeldung() => new()
    {
        AccessToken = "a1",
        RefreshToken = "r1",
        ExpiresAt = Jetzt.AddMinutes(2)
    };

    private static OAuthTokenProvider CreateProvider(
        HttpMessageHandler handler, OAuthTokens gespeichert, out OAuthTokenStore store)
    {
        store = new OAuthTokenStore(new FakeSecretStore());
        store.Write(gespeichert);

        return new OAuthTokenProvider(
            store,
            new AnthropicOAuthClient(new HttpClient(handler), new OAuthOptions()),
            new OAuthOptions(),
            new FakeTimeProvider(Jetzt));
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
