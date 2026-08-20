using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Formatting;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Models.Api;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>
/// Calls GET /api/oauth/usage on the Anthropic API.
/// </summary>
/// <remarks>
/// The token sources are tried in order - including the case where a source does
/// supply a token but the API rejects it. That is not a nicety: a token from
/// <c>claude setup-token</c> does not carry the <c>user:profile</c> scope and is
/// turned away here with HTTP 403, although it is perfectly valid for inference.
/// Without moving on, such a token would paralyse the application instead of
/// merely failing itself.
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
                // This source is no good - the next one gets its turn.
                logger?.LogWarning("Token from {Source} was rejected: {Message}", provider.Name, ex.Message);
                rejection = ex;
            }
        }

        throw rejection ?? new UsageApiException(
            sawToken
                ? T.ErrorNoToken
                : T.ErrorNotSignedIn,
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
            logger?.LogWarning(ex, "Token source {Source} not available.", provider.Name);
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
                T.ErrorTimeout,
                UsageApiFailure.Network, innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new UsageApiException(
                T.ErrorUnreachable,
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
                    T.ErrorUnreadable,
                    UsageApiFailure.InvalidResponse, response.StatusCode, innerException: ex);
            }

            if (dto is null)
            {
                throw new UsageApiException(
                    T.ErrorEmptyResponse,
                    UsageApiFailure.InvalidResponse, response.StatusCode);
            }

            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                logger.LogDebug("Usage status fetched successfully (token source {Source}).", token.Source);
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
                T.ErrorRateLimited,
                UsageApiFailure.RateLimited, response.StatusCode, retryAfter),

            >= HttpStatusCode.InternalServerError => new UsageApiException(
                T.ErrorServer((int)response.StatusCode),
                UsageApiFailure.Server, response.StatusCode, retryAfter),

            _ => new UsageApiException(
                T.ErrorUnexpectedResponse((int)response.StatusCode),
                UsageApiFailure.InvalidResponse, response.StatusCode, retryAfter)
        };
    }

    /// <summary>
    /// Distinguishes an expired token from one with insufficient scope - a large
    /// difference for the user, because one calls for a new sign-in and the
    /// other for a different token.
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
            return T.ErrorMissingScope;
        }

        return T.ErrorTokenRejected;
    }

    internal static UsageSnapshot MapToSnapshot(
        UsageResponseDto dto, DateTimeOffset retrievedAt, TokenSource tokenSource = TokenSource.ClaudeCli) => new()
    {
        // The individual fields still supply session and weekly limit reliably;
        // only the model-specific limits are missing there. They therefore stay
        // the base, and the list adds to or overrides what it knows.
        Session = MapFromLimits(dto, "session") ?? MapWindow(dto.FiveHour),
        Weekly = MapFromLimits(dto, "weekly_all") ?? MapWindow(dto.SevenDay),
        ScopedWeekly = MapScopedWeekly(dto),
        ExtraUsage = MapSpend(dto.Spend) ?? MapExtraUsage(dto.ExtraUsage),
        RetrievedAt = retrievedAt,
        TokenSource = tokenSource
    };

    /// <summary>
    /// Reads the extra usage quota from <c>spend</c>, the shape that says what
    /// its figures mean.
    /// </summary>
    /// <remarks>
    /// Amounts arrive in the smallest unit of the currency with the exponent
    /// beside them, so 2276 with exponent 2 is 22.76. The currency comes along
    /// as well: an account billed in euros reports EUR, one in Brazil BRL. Both
    /// are carried into the model rather than assumed.
    /// </remarks>
    private static ExtraUsage? MapSpend(SpendDto? spend)
    {
        if (spend is null)
        {
            return null;
        }

        var used = spend.Used is { AmountMinor: { } usedMinor }
            ? MoneyFormatter.FromMinorUnits(usedMinor, spend.Used.Exponent)
            : (decimal?)null;

        var limit = spend.Limit is { AmountMinor: { } limitMinor }
            ? MoneyFormatter.FromMinorUnits(limitMinor, spend.Limit.Exponent)
            : (decimal?)null;

        return new ExtraUsage(
            spend.Enabled,
            used,
            limit,
            spend.Percent,
            spend.Used?.Currency ?? spend.Limit?.Currency,
            spend.Used?.Exponent ?? spend.Limit?.Exponent);
    }

    /// <summary>
    /// Reads the same quota from the older <c>extra_usage</c>, for as long as it
    /// keeps being delivered.
    /// </summary>
    /// <remarks>
    /// Its numbers are in the smallest unit too, only with the scale in a
    /// separate field. Where that field is missing - an older answer - the
    /// numbers are taken as they stand: dividing a limit of 50 by a hundred
    /// would understate what someone is allowed to spend.
    /// </remarks>
    private static ExtraUsage? MapExtraUsage(ExtraUsageDto? extra)
    {
        if (extra is null)
        {
            return null;
        }

        var scale = extra.DecimalPlaces;

        return new ExtraUsage(
            extra.IsEnabled,
            extra.UsedCredits is { } used ? MoneyFormatter.FromMinorUnits(used, scale) : null,
            extra.MonthlyLimit is { } limit ? MoneyFormatter.FromMinorUnits(limit, scale) : null,
            extra.Utilization,
            extra.Currency,
            scale);
    }

    /// <summary>Finds a limit of a given kind in the <c>limits</c> list.</summary>
    private static UsageWindow? MapFromLimits(UsageResponseDto dto, string kind) =>
        dto.Limits?
            .Where(l => string.Equals(l.Kind, kind, StringComparison.OrdinalIgnoreCase))
            .Select(MapLimit)
            .FirstOrDefault(w => w is not null);

    /// <summary>
    /// The model-specific weekly limits.
    /// </summary>
    /// <remarks>
    /// The <c>limits</c> list takes precedence, because only it carries the model
    /// name in its content. Where it is missing - because an older version of the
    /// endpoint answers, say - the old individual fields step in. Their names sit
    /// in the identifier and have to be added here by hand; that was exactly why
    /// Fable turned up nowhere.
    /// </remarks>
    private static IReadOnlyList<ScopedUsageWindow> MapScopedWeekly(UsageResponseDto dto)
    {
        if (dto.Limits is { Count: > 0 } limits)
        {
            var ausListe = limits
                .Where(l => string.Equals(l.Kind, "weekly_scoped", StringComparison.OrdinalIgnoreCase))
                .Select(l => (Name: l.Scope?.Model?.DisplayName, Window: MapLimit(l)))
                .Where(e => !string.IsNullOrWhiteSpace(e.Name) && e.Window is not null)
                .Select(e => new ScopedUsageWindow(e.Name!, e.Window!))
                .ToList();

            if (ausListe.Count > 0)
            {
                return ausListe;
            }
        }

        var ausEinzelfeldern = new List<ScopedUsageWindow>(2);

        if (MapWindow(dto.SevenDayOpus) is { } opus)
        {
            ausEinzelfeldern.Add(new ScopedUsageWindow("Opus", opus));
        }

        if (MapWindow(dto.SevenDaySonnet) is { } sonnet)
        {
            ausEinzelfeldern.Add(new ScopedUsageWindow("Sonnet", sonnet));
        }

        return ausEinzelfeldern;
    }

    private static UsageWindow? MapLimit(LimitDto dto) =>
        dto.Percent is { } percent && dto.ResetsAt is { } resetsAt
            ? new UsageWindow(Math.Clamp(percent, 0d, 100d), resetsAt.ToUniversalTime())
            : null;

    private static UsageWindow? MapWindow(UsageWindowDto? dto) =>
        dto?.Utilization is { } utilization && dto.ResetsAt is { } resetsAt
            ? new UsageWindow(Math.Clamp(utilization, 0d, 100d), resetsAt.ToUniversalTime())
            : null;
}
