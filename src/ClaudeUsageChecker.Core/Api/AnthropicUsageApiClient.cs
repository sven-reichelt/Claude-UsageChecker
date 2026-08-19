using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Models.Api;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>
/// Ruft GET /api/oauth/usage bei der Anthropic-API ab.
/// </summary>
/// <remarks>
/// Die Tokenquellen werden der Reihe nach durchprobiert - und zwar auch dann,
/// wenn eine Quelle zwar ein Token liefert, die API dieses aber ablehnt. Das ist
/// keine Feinheit: Ein Token aus <c>claude setup-token</c> traegt den
/// Geltungsbereich <c>user:profile</c> nicht und wird hier mit HTTP 403
/// abgewiesen, obwohl es fuer Inferenz voellig gueltig ist. Ohne dieses
/// Weiterreichen wuerde ein solches Token die Anwendung lahmlegen, statt nur
/// selbst zu scheitern.
/// </remarks>
public sealed class AnthropicUsageApiClient(
    HttpClient httpClient,
    IReadOnlyList<ITokenProvider> tokenProviders,
    UsageApiOptions? options = null,
    TimeProvider? timeProvider = null,
    ILogger<AnthropicUsageApiClient>? logger = null) : IUsageApiClient
{
    private readonly IReadOnlyList<ITokenProvider> _tokenProviders =
        tokenProviders ?? throw new ArgumentNullException(nameof(tokenProviders));

    private readonly UsageApiOptions _options = options ?? new UsageApiOptions();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        UsageApiException? rejection = null;
        var sawToken = false;

        foreach (var provider in _tokenProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var token = await TryGetTokenAsync(provider, cancellationToken).ConfigureAwait(false);
            if (token is null)
            {
                continue;
            }

            sawToken = true;

            try
            {
                return await FetchAsync(token, cancellationToken).ConfigureAwait(false);
            }
            catch (UsageApiException ex) when (ex.Failure == UsageApiFailure.Unauthorized)
            {
                // Diese Quelle taugt nicht - die naechste bekommt ihre Chance.
                logger?.LogWarning("Token aus {Source} wurde abgelehnt: {Message}", provider.Name, ex.Message);
                rejection = ex;
            }
        }

        throw rejection ?? new UsageApiException(
            sawToken
                ? "Kein verwendbares Token gefunden."
                : "Kein Zugriffsrecht vorhanden. Bitte in den Einstellungen anmelden.",
            UsageApiFailure.NoToken);
    }

    private async ValueTask<AccessToken?> TryGetTokenAsync(
        ITokenProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.TryGetTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogWarning(ex, "Tokenquelle {Source} nicht verfügbar.", provider.Name);
            return null;
        }
    }

    private async Task<UsageSnapshot> FetchAsync(AccessToken token, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(token);

        HttpResponseMessage response;
        try
        {
            response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UsageApiException(
                "Zeitüberschreitung beim Abruf des Nutzungsstands.",
                UsageApiFailure.Network, innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new UsageApiException(
                "Die Anthropic-API ist nicht erreichbar.",
                UsageApiFailure.Network, innerException: ex);
        }

        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            UsageResponseDto? dto;
            try
            {
                dto = await JsonSerializer
                    .DeserializeAsync(stream, ClaudeUsageJsonContext.Default.UsageResponseDto, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new UsageApiException(
                    "Die Antwort der API konnte nicht ausgewertet werden.",
                    UsageApiFailure.InvalidResponse, response.StatusCode, innerException: ex);
            }

            if (dto is null)
            {
                throw new UsageApiException(
                    "Die API lieferte eine leere Antwort.",
                    UsageApiFailure.InvalidResponse, response.StatusCode);
            }

            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug("Nutzungsstand erfolgreich abgerufen (Tokenquelle {Source}).", token.Source);
            }

            return MapToSnapshot(dto, _timeProvider.GetUtcNow(), token.Source);
        }
    }

    private HttpRequestMessage BuildRequest(AccessToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _options.UsagePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        request.Headers.TryAddWithoutValidation("anthropic-beta", _options.BetaHeader);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var retryAfter = response.Headers.RetryAfter?.Delta
                         ?? (response.Headers.RetryAfter?.Date is { } date
                             ? date - DateTimeOffset.UtcNow
                             : null);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UsageApiException(
                await DescribeRejectionAsync(response, cancellationToken).ConfigureAwait(false),
                UsageApiFailure.Unauthorized, response.StatusCode),

            HttpStatusCode.TooManyRequests => new UsageApiException(
                "Die API drosselt die Abrufe (HTTP 429).",
                UsageApiFailure.RateLimited, response.StatusCode, retryAfter),

            >= HttpStatusCode.InternalServerError => new UsageApiException(
                $"Serverfehler der Anthropic-API (HTTP {(int)response.StatusCode}).",
                UsageApiFailure.Server, response.StatusCode, retryAfter),

            _ => new UsageApiException(
                $"Unerwartete Antwort der Anthropic-API (HTTP {(int)response.StatusCode}).",
                UsageApiFailure.InvalidResponse, response.StatusCode, retryAfter)
        };
    }

    /// <summary>
    /// Unterscheidet ein abgelaufenes Token von einem mit unzureichendem
    /// Geltungsbereich - fuer den Nutzer ein grosser Unterschied, weil das eine
    /// eine neue Anmeldung erfordert und das andere ein anderes Token.
    /// </summary>
    private static async Task<string> DescribeRejectionAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            body = string.Empty;
        }

        if (body.Contains("user:profile", StringComparison.OrdinalIgnoreCase))
        {
            return "Dem Token fehlt der Geltungsbereich \"user:profile\". "
                   + "Tokens aus \"claude setup-token\" taugen nur für Inferenz, nicht für den Nutzungsstand.";
        }

        return "Das Token wurde abgelehnt. Es ist abgelaufen oder ungültig.";
    }

    internal static UsageSnapshot MapToSnapshot(
        UsageResponseDto dto, DateTimeOffset retrievedAt, TokenSource tokenSource = TokenSource.ClaudeCli) => new()
    {
        Session = MapWindow(dto.FiveHour),
        Weekly = MapWindow(dto.SevenDay),
        WeeklyOpus = MapWindow(dto.SevenDayOpus),
        WeeklySonnet = MapWindow(dto.SevenDaySonnet),
        ExtraUsage = dto.ExtraUsage is { } extra
            ? new ExtraUsage(extra.IsEnabled, extra.MonthlyLimit, extra.UsedCredits, extra.Utilization)
            : null,
        RetrievedAt = retrievedAt,
        TokenSource = tokenSource
    };

    private static UsageWindow? MapWindow(UsageWindowDto? dto) =>
        dto?.Utilization is { } utilization && dto.ResetsAt is { } resetsAt
            ? new UsageWindow(Math.Clamp(utilization, 0d, 100d), resetsAt.ToUniversalTime())
            : null;
}
