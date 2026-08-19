using ClaudeUsageChecker.Core.Authentication;

namespace ClaudeUsageChecker.Core.Tests.Authentication;

public class ChainedTokenProviderTests
{
    [Fact]
    public async Task NimmtDasErsteVerfuegbareToken()
    {
        var chain = new ChainedTokenProvider(
        [
            new StubProvider("leer", null),
            new StubProvider("treffer", new AccessToken("abc", TokenSource.SecretStore)),
            new StubProvider("spaeter", new AccessToken("xyz", TokenSource.ClaudeCli))
        ]);

        var token = await chain.TryGetTokenAsync();

        Assert.Equal("abc", token!.Value);
    }

    [Fact]
    public async Task EineFehlerhafteQuelleBeendetDieKetteNicht()
    {
        var chain = new ChainedTokenProvider(
        [
            new ThrowingProvider(),
            new StubProvider("treffer", new AccessToken("abc", TokenSource.ClaudeCli))
        ]);

        var token = await chain.TryGetTokenAsync();

        Assert.Equal("abc", token!.Value);
    }

    [Fact]
    public async Task LiefertNullWennKeineQuelleEtwasHat()
    {
        var chain = new ChainedTokenProvider([new StubProvider("leer", null)]);

        Assert.Null(await chain.TryGetTokenAsync());
    }

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
}
