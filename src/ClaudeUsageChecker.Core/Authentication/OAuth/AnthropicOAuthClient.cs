using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace ClaudeUsageChecker.Core.Authentication.OAuth;

/// <summary>
/// Fuehrt den Autorisierungscode-Fluss mit PKCE gegen Anthropic durch.
/// </summary>
/// <remarks>
/// Damit erhaelt die Anwendung eigene Anmeldedaten mit dem Geltungsbereich
/// <c>user:profile</c> und ist nicht mehr darauf angewiesen, das Token einer
/// Claude-Code-Installation mitzulesen.
/// </remarks>
public sealed class AnthropicOAuthClient(HttpClient httpClient, OAuthOptions? options = null)
{
    private readonly OAuthOptions _options = options ?? new OAuthOptions();

    /// <summary>Die verwendeten Parameter - fuer Anzeige und Diagnose.</summary>
    public OAuthOptions Options => _options;

    /// <summary>
    /// Baut die Adresse, die der Nutzer im Browser oeffnet, samt frischem
    /// PKCE-Paar und Zufallswert gegen Verwechslung von Vorgaengen.
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
    /// Loest den vom Nutzer eingefuegten Code ein.
    /// </summary>
    /// <param name="pastedCode">
    /// Der Text von der Anthropic-Seite. Diese liefert ihn je nach Fall als
    /// blossen Code oder in der Form <c>CODE#STATE</c>.
    /// </param>
    /// <exception cref="OAuthException">Bei jedem Fehlschlag des Tauschs.</exception>
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
                "Der eingefügte Code gehört zu einem anderen Anmeldevorgang. "
                + "Bitte die Anmeldung erneut starten.");
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

        return await PostAsync(payload, "Der Code konnte nicht eingelöst werden", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Erneuert ein Token. Der Refresh-Token gehoert dieser Anwendung allein.</summary>
    /// <exception cref="OAuthException">Wenn die Erneuerung scheitert.</exception>
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

        return await PostAsync(payload, "Die Anmeldung konnte nicht erneuert werden", cancellationToken)
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
            throw new OAuthException($"{fehlertext}: Anthropic ist nicht erreichbar.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // 400/401 heisst: Der Server hat die Anmeldedaten selbst verworfen.
                // Alles andere (404, 5xx, Drosselung) sagt nichts ueber sie aus.
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
                throw new OAuthException($"{fehlertext}: Die Antwort war nicht lesbar.", ex);
            }

            if (dto?.AccessToken is not { Length: > 0 })
            {
                throw new OAuthException($"{fehlertext}: Die Antwort enthielt kein Token.");
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
    /// Trennt <c>CODE#STATE</c> auf. Die Anthropic-Seite zeigt den Code je nach
    /// Fall mit oder ohne angehaengten Zufallswert an.
    /// </summary>
    internal static (string Code, string? State) SplitPastedCode(string pasted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pasted);

        var teile = pasted.Trim().Split('#', 2);
        return teile.Length == 2 && teile[1].Length > 0
            ? (teile[0], teile[1])
            : (teile[0], null);
    }

    /// <summary>Zieht die Meldung aus einer Fehlerantwort, ohne sie ungefiltert durchzureichen.</summary>
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

/// <summary>Fehler im Anmeldevorgang.</summary>
public sealed class OAuthException(
    string message,
    Exception? innerException = null,
    bool isCredentialRejected = false) : Exception(message, innerException)
{
    /// <summary>
    /// Der Server hat die Anmeldedaten selbst abgewiesen - im Gegensatz zu einem
    /// Netzwerkproblem, das nichts ueber ihre Gueltigkeit aussagt. Nur in diesem
    /// Fall ist eine erneute Anmeldung noetig.
    /// </summary>
    public bool IsCredentialRejected { get; } = isCredentialRejected;
}

/// <summary>Eine begonnene Anmeldung: Adresse fuer den Browser plus die Geheimnisse dazu.</summary>
/// <param name="Url">Die im Browser zu oeffnende Adresse.</param>
/// <param name="CodeVerifier">Das PKCE-Geheimnis, erst beim Tausch mitzuschicken.</param>
/// <param name="State">Zufallswert zur Zuordnung des Vorgangs.</param>
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

    // Ob Anthropic dieses Feld ueberhaupt liefert, ist unbekannt - erfasst wird
    // es trotzdem, damit sich die Frage beim naechsten Erneuern von selbst klaert.
    [JsonPropertyName("refresh_token_expires_in")] public int? RefreshTokenExpiresIn { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TokenRequestDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(OAuthTokens))]
internal sealed partial class OAuthJsonContext : JsonSerializerContext;
