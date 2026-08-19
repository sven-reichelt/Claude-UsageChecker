using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>
/// Fetches the usage status at a fixed cadence and reports every change of
/// state. After failures the interval is stretched exponentially, so that the
/// throttle-sensitive endpoint is not burdened further.
/// </summary>
public sealed class UsageMonitor : IAsyncDisposable
{
    private readonly IUsageApiClient _client;
    private readonly MonitorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UsageMonitor>? _logger;

    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();

    private Task? _loop;
    private TimeSpan _currentBackoff;
    private UsageState _state = UsageState.Initializing;

    public UsageMonitor(
        IUsageApiClient client,
        MonitorOptions? options = null,
        TimeProvider? timeProvider = null,
        ILogger<UsageMonitor>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? new MonitorOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        _currentBackoff = _options.InitialBackoff;
    }

    /// <summary>Raised on every change of state.</summary>
    public event EventHandler<UsageState>? StateChanged;

    /// <summary>The most recently known state.</summary>
    public UsageState State => Volatile.Read(ref _state);

    /// <summary>Starts the polling loop. Calling it again has no effect.</summary>
    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _loop = Task.Run(() => RunAsync(_shutdown.Token));
    }

    /// <summary>
    /// Forces an immediate call, for instance after a token has been stored.
    /// If a call is already running, this waits for its result.
    /// </summary>
    public async Task RefreshNowAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        await PollOnceAsync(linked.Token).ConfigureAwait(false);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = await PollOnceAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Performs exactly one call and returns the wait until the next.</summary>
    private async Task<TimeSpan> PollOnceAsync(CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = await _client.GetUsageAsync(cancellationToken).ConfigureAwait(false);

            _currentBackoff = _options.InitialBackoff;
            var delay = _options.PollInterval;
            Publish(new UsageState
            {
                Kind = UsageStateKind.Ready,
                Snapshot = snapshot,
                NextPollAt = _timeProvider.GetUtcNow() + delay
            });

            return delay;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UsageApiException ex)
        {
            return HandleFailure(ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while fetching the usage status.");
            return HandleFailure(new UsageApiException(
                T.ErrorUnexpectedFetch, UsageApiFailure.Network, innerException: ex));
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private TimeSpan HandleFailure(UsageApiException ex)
    {
        var previous = State.Snapshot;

        var kind = ex.Failure switch
        {
            UsageApiFailure.NoToken => UsageStateKind.NotConfigured,
            UsageApiFailure.Unauthorized => UsageStateKind.AuthenticationFailed,
            _ when previous is not null => UsageStateKind.Stale,
            _ => UsageStateKind.Unavailable
        };

        var delay = DetermineRetryDelay(ex);

        _logger?.LogWarning("Call failed ({Failure}). Next attempt in {Delay}.",
            ex.Failure, delay);

        Publish(new UsageState
        {
            Kind = kind,
            Snapshot = previous,
            Failure = ex.Failure,
            Message = ex.Message,
            NextPollAt = _timeProvider.GetUtcNow() + delay
        });

        return delay;
    }

    private TimeSpan DetermineRetryDelay(UsageApiException ex)
    {
        // An instruction from the server always takes precedence.
        if (ex.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero)
        {
            return retryAfter < MonitorOptions.MinimumInterval ? MonitorOptions.MinimumInterval : retryAfter;
        }

        // A missing sign-in does not fix itself by retrying quickly.
        if (ex.Failure is UsageApiFailure.NoToken or UsageApiFailure.Unauthorized)
        {
            return _options.PollInterval;
        }

        var delay = _currentBackoff;
        _currentBackoff = _currentBackoff * 2 > _options.MaxBackoff
            ? _options.MaxBackoff
            : _currentBackoff * 2;

        return delay;
    }

    private void Publish(UsageState state)
    {
        Volatile.Write(ref _state, state);
        StateChanged?.Invoke(this, state);
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync().ConfigureAwait(false);

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        _shutdown.Dispose();
        _refreshGate.Dispose();
    }
}
