using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;

namespace ClaudeUsageChecker.App.Services;

/// <summary>Ergebnis der Pruefung eines eingegebenen Tokens.</summary>
public sealed record TokenValidationResult(bool IsUsable, string Message)
{
    public static TokenValidationResult Usable(string message) => new(true, message);

    public static TokenValidationResult Unusable(string message) => new(false, message);
}

/// <summary>
/// Prueft ein eingegebenes Token gegen den Nutzungsendpunkt, bevor es
/// gespeichert wird.
/// </summary>
/// <remarks>
/// Ohne diese Pruefung laesst sich ein Token hinterlegen, das fuer den
/// Nutzungsstand untauglich ist - etwa eines aus <c>claude setup-token</c>,
/// dem der Geltungsbereich <c>user:profile</c> fehlt. Der Nutzer erfaehrt das
/// sonst erst indirekt daran, dass die Anzeige nicht mehr stimmt.
/// </remarks>
public sealed class TokenValidator(HttpClient httpClient, UsageApiOptions? options = null)
{
    private readonly UsageApiOptions _options = options ?? new UsageApiOptions();

    public async Task<TokenValidationResult> ValidateAsync(
        string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenValidationResult.Unusable("Bitte zuerst ein Token einfuegen.");
        }

        var provider = new StaticTokenProvider(token.Trim());
        var client = new AnthropicUsageApiClient(httpClient, [provider], _options);

        try
        {
            await client.GetUsageAsync(cancellationToken).ConfigureAwait(false);
            return TokenValidationResult.Usable("Token geprueft und angenommen.");
        }
        catch (UsageApiException ex) when (ex.Failure == UsageApiFailure.Unauthorized)
        {
            return TokenValidationResult.Unusable(ex.Message);
        }
        catch (UsageApiException ex)
        {
            // Netzwerkprobleme sagen nichts ueber das Token aus - nicht ablehnen.
            return TokenValidationResult.Usable(
                $"Token gespeichert, aber nicht pruefbar: {ex.Message}");
        }
    }

    private sealed class StaticTokenProvider(string value) : ITokenProvider
    {
        public string Name => "eingabe";

        public ValueTask<AccessToken?> TryGetTokenAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AccessToken?>(new AccessToken(value, TokenSource.SecretStore));
    }
}
