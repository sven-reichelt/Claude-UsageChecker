using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ClaudeUsageChecker.Core.Localization;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Runs the authorization code flow with PKCE against Anthropic.
/// </summary>
/// <remarks>
/// This gives the application credentials of its own with the <c>user:profile</c>
/// scope, so that it no longer depends on reading the token of a Claude Code
/// installation.
/// </remarks>
public sealed class AnthropicOAuthClient(HttpClient httpClient, OAuthOptions? options = null)
{
    private readonly OAuthOptions _options = options ?? new OAuthOptions();

    /// <summary>The parameters in use - for display and diagnostics.</summary>
    public OAuthOptions Options => _options;

    /// <summary>
    /// Builds the address the user opens in the browser, complete with a fresh
    /// PKCE pair and a random value guarding against mixed-up flows.
    /// </summary>
    public AuthorizationRequest CreateAuthorizationRequest()
    {
        var pkce = PkceChallenge.Create();
        var state = Guid.NewGuid().ToString("N");

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["code"] = "true";
        query["client_id"] = _options.ClientId;
        query["response_type"] = "code";
        query["redirect_uri"] = _options.RedirectUri.ToString();
        query["scope"] = _options.Scope;
        query["code_challenge"] = pkce.Challenge;
        query["code_challenge_method"] = PkceChallenge.Method;
        query["state"] = state;

        var builder = new UriBuilder(_options.AuthorizationEndpoint) { Query = query.ToString() };
        return new AuthorizationRequest(builder.Uri, pkce.Verifier, state);
    }

    /// <summary>
    /// Redeems the code the user pasted.
    /// </summary>
    /// <param name="pastedCode">
    /// The text from the Anthropic page. Depending on the case it arrives as a
    /// bare code or in the form <c>CODE#STATE</c>.
    /// </param>
    /// <exception cref="OAuthException">On any failure of the exchange.</exception>
    public async Task<OAuthTokens> ExchangeCodeAsync(
        string pastedCode,
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (code, state) = SplitPastedCode(pastedCode);

        if (state is not null && !string.Equals(state, request.State, StringComparison.Ordinal))
        {
            throw new OAuthException(
                T.OAuthWrongFlow);
        }

        var payload = new TokenRequestDto
        {
            GrantType = "authorization_code",
            Code = code,
            State = request.State,
            ClientId = _options.ClientId,
            RedirectUri = _options.RedirectUri.ToString(),
            CodeVerifier = request.CodeVerifier
        };

        return await PostAsync(payload, T.OAuthRedeemFailed, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Refreshes a token. The refresh token belongs to this application alone.</summary>
    /// <exception cref="OAuthException">When the refresh fails.</exception>
    public async Task<OAuthTokens> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var payload = new TokenRequestDto
        {
            GrantType = "refresh_token",
            RefreshToken = refreshToken,
            ClientId = _options.ClientId
        };

        return await PostAsync(payload, T.OAuthRefreshFailed, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<OAuthTokens> PostAsync(
        TokenRequestDto payload, string fehlertext, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = JsonContent.Create(payload, OAuthJsonContext.Default.TokenRequestDto)
        };
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new OAuthException(T.OAuthUnreachable(fehlertext), ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // 400/401 means the server discarded the credentials themselves.
                // Everything else (404, 5xx, throttling) says nothing about them.
                var abgewiesen = response.StatusCode is System.Net.HttpStatusCode.BadRequest
                    or System.Net.HttpStatusCode.Unauthorized;

                throw new OAuthException(
                    $"{fehlertext} (HTTP {(int)response.StatusCode}). {Beschreibe(body)}",
                    isCredentialRejected: abgewiesen);
            }

            TokenResponseDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize(body, OAuthJsonContext.Default.TokenResponseDto);
            }
            catch (JsonException ex)
            {
                throw new OAuthException(T.OAuthUnreadable(fehlertext), ex);
            }

            if (dto?.AccessToken is not { Length: > 0 })
            {
                throw new OAuthException(T.OAuthNoToken(fehlertext));
            }

            return new OAuthTokens
            {
                AccessToken = dto.AccessToken,
                RefreshToken = dto.RefreshToken,
                ExpiresAt = dto.ExpiresIn is { } seconds and > 0
                    ? DateTimeOffset.UtcNow.AddSeconds(seconds)
                    : null,
                RefreshTokenExpiresAt = dto.RefreshTokenExpiresIn is { } refreshSeconds and > 0
                    ? DateTimeOffset.UtcNow.AddSeconds(refreshSeconds)
                    : null,
                Scope = dto.Scope
            };
        }
    }

    /// <summary>
    /// Splits <c>CODE#STATE</c>. Depending on the case, the Anthropic page shows
    /// the code with or without the random value appended.
    /// </summary>
    internal static (string Code, string? State) SplitPastedCode(string pasted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pasted);

        var teile = pasted.Trim().Split('#', 2);
        return teile.Length == 2 && teile[1].Length > 0
            ? (teile[0], teile[1])
            : (teile[0], null);
    }

    /// <summary>Extracts the message from an error response without passing it through unfiltered.</summary>
    private static string Beschreibe(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error_description", out var beschreibung))
            {
                return beschreibung.GetString() ?? string.Empty;
            }

            if (doc.RootElement.TryGetProperty("error", out var fehler))
            {
                return fehler.ValueKind == JsonValueKind.String
                    ? fehler.GetString() ?? string.Empty
                    : fehler.ToString();
            }
        }
        catch (JsonException)
        {
            // Kein JSON - dann eben nichts Genaueres.
        }

        return string.Empty;
    }
}

/// <summary>A failure in the sign-in flow.</summary>
public sealed class OAuthException(
    string message,
    Exception? innerException = null,
    bool isCredentialRejected = false) : Exception(message, innerException)
{
    /// <summary>
    /// The server refused the credentials themselves - as opposed to a network
    /// problem, which says nothing about their validity. Only in this case is a
    /// fresh sign-in needed.
    /// </summary>
    public bool IsCredentialRejected { get; } = isCredentialRejected;
}

/// <summary>A sign-in in progress: the browser address plus the secrets that belong to it.</summary>
/// <param name="Url">The address to open in the browser.</param>
/// <param name="CodeVerifier">The PKCE secret, sent only when the code is exchanged.</param>
/// <param name="State">Random value that ties the response to this flow.</param>
public sealed record AuthorizationRequest(Uri Url, string CodeVerifier, string State);

internal sealed class TokenRequestDto
{
    [JsonPropertyName("grant_type")] public required string GrantType { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("client_id")] public required string ClientId { get; init; }
    [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; init; }
    [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; init; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
}

internal sealed class TokenResponseDto
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int? ExpiresIn { get; set; }

    // Whether Anthropic supplies this field at all is unknown - it is captured
    // regardless, so that the question answers itself on the next refresh.
    [JsonPropertyName("refresh_token_expires_in")] public int? RefreshTokenExpiresIn { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TokenRequestDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(OAuthTokens))]
internal sealed partial class OAuthJsonContext : JsonSerializerContext;
