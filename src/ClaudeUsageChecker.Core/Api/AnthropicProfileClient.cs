using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Api;

/// <summary>
/// Reads the plan from GET /api/oauth/profile.
/// </summary>
/// <remarks>
/// <para>
/// The usage endpoint does not say which plan it is counting for; the profile
/// endpoint does, and it asks for no more than the <c>user:profile</c> scope
/// the usage call needs anyway. Measured on 2026-09-18 with both routes to a
/// token - the application's own sign-in and Claude Code's - and both answered
/// the same.
/// </para>
/// <para>
/// A plan changes far less often than a usage figure, so it is asked once per
/// token rather than once per poll: the application's own token lives eight
/// hours, Claude Code's about one. A new sign-in brings a new token and with it
/// a fresh answer. After a failure the question waits an hour before it is put
/// again, so that a profile endpoint which refuses does not double the calls of
/// a throttle-sensitive API.
/// </para>
/// <para>
/// The token itself is not kept. What is remembered is a hash of it, which is
/// enough to tell "the same token" from "another one".
/// </para>
/// </remarks>
public sealed class AnthropicProfileClient(
    HttpClient httpClient,
    UsageApiOptions? options = null,
    TimeProvider? timeProvider = null,
    ILogger<AnthropicProfileClient>? logger = null) : ISubscriptionPlanSource
{
    /// <summary>How long a failed question rests before it is asked again.</summary>
    public static readonly TimeSpan RetryAfterFailure = TimeSpan.FromHours(1);

    private readonly UsageApiOptions _options = options ?? new UsageApiOptions();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Lock _gate = new();

    private string? _tokenHash;
    private SubscriptionPlan? _plan;
    private DateTimeOffset _askedAt;

    public async Task<SubscriptionPlan?> GetPlanAsync(AccessToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var hash = HashOf(token);
        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            if (hash == _tokenHash && (_plan is not null || now - _askedAt < RetryAfterFailure))
            {
                return _plan;
            }
        }

        var plan = await FetchAsync(token, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _tokenHash = hash;
            _plan = plan;
            _askedAt = now;
        }

        return plan;
    }

    private async Task<SubscriptionPlan?> FetchAsync(AccessToken token, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.ProfilePath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            request.Headers.TryAddWithoutValidation("anthropic-beta", _options.BetaHeader);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogWarning("Profile not available: HTTP {Status}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var dto = await JsonSerializer
                .DeserializeAsync(stream, ClaudeUsageJsonContext.Default.ProfileResponseDto, cancellationToken)
                .ConfigureAwait(false);

            if (dto is null)
            {
                return null;
            }

            var plan = new SubscriptionPlan(
                dto.Organization?.OrganizationType,
                dto.Organization?.RateLimitTier,
                dto.Account?.HasClaudeMax ?? false,
                dto.Account?.HasClaudePro ?? false);

            return plan is { OrganizationType: null, RateLimitTier: null, HasClaudeMax: false, HasClaudePro: false }
                ? null
                : plan;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeouts, network trouble, an answer in an unexpected shape - none
            // of it is worth more than a missing line in the display.
            logger?.LogWarning(ex, "Profile could not be read.");
            return null;
        }
    }

    private static string HashOf(AccessToken token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Value)));
}
