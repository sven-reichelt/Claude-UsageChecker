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
public sealed class AnthropicUsageApiClient(
    HttpClient httpClient,
    ITokenProvider tokenProvider,
    UsageApiOptions? options = null,
    TimeProvider? timeProvider = null,
    ILogger<AnthropicUsageApiClient>? logger = null) : IUsageApiClient
{
    private readonly UsageApiOptions _options = options ?? new UsageApiOptions();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var token = await tokenProvider.TryGetTokenAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new UsageApiException(
                "Kein OAuth-Token gefunden. Bitte in den Einstellungen ein Token hinterlegen.",
                UsageApiFailure.NoToken);

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
                "Zeitueberschreitung beim Abruf des Nutzungsstands.",
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
            EnsureSuccess(response);

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

            logger?.LogDebug("Nutzungsstand erfolgreich abgerufen (Tokenquelle {Source}).", token.Source);
            return MapToSnapshot(dto, _timeProvider.GetUtcNow());
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

    private static void EnsureSuccess(HttpResponseMessage response)
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
                "Das Token wurde abgelehnt. Es ist abgelaufen oder ungueltig.",
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

    internal static UsageSnapshot MapToSnapshot(UsageResponseDto dto, DateTimeOffset retrievedAt) => new()
    {
        Session = MapWindow(dto.FiveHour),
        Weekly = MapWindow(dto.SevenDay),
        WeeklyOpus = MapWindow(dto.SevenDayOpus),
        WeeklySonnet = MapWindow(dto.SevenDaySonnet),
        ExtraUsage = dto.ExtraUsage is { } extra
            ? new ExtraUsage(extra.IsEnabled, extra.MonthlyLimit, extra.UsedCredits, extra.Utilization)
            : null,
        RetrievedAt = retrievedAt
    };

    private static UsageWindow? MapWindow(UsageWindowDto? dto) =>
        dto?.Utilization is { } utilization && dto.ResetsAt is { } resetsAt
            ? new UsageWindow(Math.Clamp(utilization, 0d, 100d), resetsAt.ToUniversalTime())
            : null;
}
