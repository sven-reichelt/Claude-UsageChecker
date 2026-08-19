using ClaudeUsageChecker.Core.Api;
using ClaudeUsageChecker.Core.Configuration;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.Core.Tests.Services;

public class UsageMonitorTests
{
    [Fact]
    public async Task ASuccessfulCallLeadsToReady()
    {
        var client = new StubClient { Snapshot = Snapshot(20) };
        await using var monitor = new UsageMonitor(client);

        await monitor.RefreshNowAsync();

        Assert.Equal(UsageStateKind.Ready, monitor.State.Kind);
        Assert.Equal(20d, monitor.State.Snapshot!.Session!.Utilization);
    }

    [Fact]
    public async Task AMissingTokenLeadsToNotConfigured()
    {
        var client = new StubClient
        {
            Failure = new UsageApiException("kein Token", UsageApiFailure.NoToken)
        };
        await using var monitor = new UsageMonitor(client);

        await monitor.RefreshNowAsync();

        Assert.Equal(UsageStateKind.NotConfigured, monitor.State.Kind);
    }

    [Fact]
    public async Task NachFehlschlagBleibenAlteDatenAlsStaleErhalten()
    {
        var client = new StubClient { Snapshot = Snapshot(42) };
        await using var monitor = new UsageMonitor(client);
        await monitor.RefreshNowAsync();

        client.Snapshot = null;
        client.Failure = new UsageApiException("offline", UsageApiFailure.Network);
        await monitor.RefreshNowAsync();

        Assert.Equal(UsageStateKind.Stale, monitor.State.Kind);
        Assert.Equal(42d, monitor.State.Snapshot!.Session!.Utilization);
    }

    [Fact]
    public async Task WithoutEarlierDataAFailureLeadsToUnavailable()
    {
        var client = new StubClient
        {
            Failure = new UsageApiException("offline", UsageApiFailure.Network)
        };
        await using var monitor = new UsageMonitor(client);

        await monitor.RefreshNowAsync();

        Assert.Equal(UsageStateKind.Unavailable, monitor.State.Kind);
    }

    [Fact]
    public async Task AStateChangeIsReported()
    {
        var client = new StubClient { Snapshot = Snapshot(5) };
        await using var monitor = new UsageMonitor(client);

        UsageState? observed = null;
        monitor.StateChanged += (_, state) => observed = state;

        await monitor.RefreshNowAsync();

        Assert.NotNull(observed);
        Assert.Equal(UsageStateKind.Ready, observed!.Kind);
    }

    [Fact]
    public void ThePollingIntervalNeverFallsBelowTheSafeMinimum()
    {
        var options = new MonitorOptions { PollInterval = TimeSpan.FromSeconds(5) };

        Assert.Equal(MonitorOptions.MinimumInterval, options.PollInterval);
    }

    private static UsageSnapshot Snapshot(double utilization) => new()
    {
        Session = new UsageWindow(utilization, DateTimeOffset.UtcNow.AddHours(3)),
        Weekly = new UsageWindow(utilization / 2, DateTimeOffset.UtcNow.AddDays(5)),
        RetrievedAt = DateTimeOffset.UtcNow
    };

    private sealed class StubClient : IUsageApiClient
    {
        public UsageSnapshot? Snapshot { get; set; }

        public UsageApiException? Failure { get; set; }

        public Task<UsageSnapshot> GetUsageAsync(CancellationToken cancellationToken = default) =>
            Failure is not null
                ? Task.FromException<UsageSnapshot>(Failure)
                : Task.FromResult(Snapshot!);
    }
}
