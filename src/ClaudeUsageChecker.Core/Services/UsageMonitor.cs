using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace ClaudeUsageChecker.Core.Services;

/// <summary>
/// Ruft den Nutzungsstand in festem Takt ab und meldet jede Zustandsaenderung.
/// Nach Fehlschlaegen wird das Intervall exponentiell gedehnt, damit der
/// drosselungsempfindliche Endpunkt nicht weiter belastet wird.
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

    /// <summary>Wird bei jeder Zustandsaenderung ausgeloest.</summary>
    public event EventHandler<UsageState>? StateChanged;

    /// <summary>Der zuletzt bekannte Zustand.</summary>
    public UsageState State => Volatile.Read(ref _state);

    /// <summary>Startet die Abrufschleife. Mehrfachaufrufe sind wirkungslos.</summary>
    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _loop = Task.Run(() => RunAsync(_shutdown.Token));
    }

    /// <summary>
    /// Erzwingt einen sofortigen Abruf, etwa nach dem Hinterlegen eines Tokens.
    /// Laeuft bereits ein Abruf, wird auf dessen Ergebnis gewartet.
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

    /// <summary>Fuehrt genau einen Abruf durch und liefert die Wartezeit bis zum naechsten.</summary>
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
            _logger?.LogError(ex, "Unerwarteter Fehler beim Abruf des Nutzungsstands.");
            return HandleFailure(new UsageApiException(
                "Unerwarteter Fehler beim Abruf.", UsageApiFailure.Network, innerException: ex));
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

        _logger?.LogWarning("Abruf fehlgeschlagen ({Failure}). Naechster Versuch in {Delay}.",
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
        // Eine Vorgabe des Servers hat immer Vorrang.
        if (ex.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero)
        {
            return retryAfter < MonitorOptions.MinimumInterval ? MonitorOptions.MinimumInterval : retryAfter;
        }

        // Fehlende Einrichtung behebt sich nicht durch schnelles Wiederholen.
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
                // Erwartet beim Herunterfahren.
            }
        }

        _shutdown.Dispose();
        _refreshGate.Dispose();
    }
}
