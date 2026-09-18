using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Tray;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The plan in the three places it is shown: under the usage in the menu, under
/// the source in the details window, under the sign-ins in the settings.
/// </summary>
/// <remarks>
/// The plan name is compared rather than the whole line: it is a product name
/// and the same in every language, whereas the label in front of it is not.
/// </remarks>
public class PlanDisplayTests
{
    private const string MaxName = "Claude Max 5×";

    private static readonly SubscriptionPlan Max = new("claude_max", "default_claude_max_5x", HasClaudeMax: true);
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheMenuNamesThePlanUnderTheUsage()
    {
        var lines = TrayIconController.BuildStatusLines(State(Max, withExtraUsage: true), Now);

        // Last of all - below the extra usage too, which is still usage.
        Assert.Contains(MaxName, lines[^1], StringComparison.Ordinal);
        Assert.Single(lines, l => l.Contains(MaxName, StringComparison.Ordinal));
    }

    [Fact]
    public void TheMenuHasNoPlanLineWhileThePlanIsUnknown()
    {
        var known = TrayIconController.BuildStatusLines(State(Max), Now);
        var unknown = TrayIconController.BuildStatusLines(State(plan: null), Now);

        Assert.Equal(known.Count - 1, unknown.Count);
        Assert.DoesNotContain(unknown, l => l.Contains("Claude", StringComparison.Ordinal));
    }

    [Fact]
    public void AMenuWithoutLimitsStillSaysSoAboveThePlan()
    {
        // No limits reported is a statement about the figures; the plan does not
        // replace it.
        var state = new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot { Session = null, Weekly = null, Plan = Max, RetrievedAt = Now }
        };

        var lines = TrayIconController.BuildStatusLines(state, Now);

        Assert.Equal(2, lines.Count);
        Assert.Contains(MaxName, lines[1], StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void TheDetailsWindowNamesThePlanUnderTheSource()
    {
        var window = new DetailsWindow();
        window.Render(State(Max));

        var footer = window.FindControl<TextBlock>("FooterText")!;
        var plan = window.FindControl<TextBlock>("PlanText")!;

        Assert.True(plan.IsVisible);
        Assert.Contains(MaxName, plan.Text, StringComparison.Ordinal);

        // Directly under the line with time and source, in the same column.
        var column = Assert.IsType<StackPanel>(footer.Parent);
        Assert.Same(column, plan.Parent);
        Assert.Equal(column.Children.IndexOf(footer) + 1, column.Children.IndexOf(plan));
    }

    [AvaloniaFact]
    public void TheDetailsWindowHidesThePlanWhileItIsUnknown()
    {
        var window = new DetailsWindow();
        window.Render(State(Max));
        window.Render(State(plan: null));

        // Hidden again, not left standing from the render before.
        Assert.False(window.FindControl<TextBlock>("PlanText")!.IsVisible);
    }

    [AvaloniaFact]
    public void TheSettingsNameThePlanUnderTheSignIns()
    {
        using var file = new TemporaryFile();
        var window = Settings(file, Max);

        var status = window.FindControl<TextBlock>("AccountPlanStatus")!;
        var label = window.FindControl<TextBlock>("AccountPlanLabel")!;

        Assert.True(status.IsVisible);
        Assert.True(label.IsVisible);
        Assert.Equal(MaxName, status.Text);
        Assert.False(string.IsNullOrWhiteSpace(label.Text));

        // The row below both sign-ins.
        Assert.Equal(Grid.GetRow(window.FindControl<TextBlock>("AccountOwnStatus")!) + 1, Grid.GetRow(status));
        Assert.Equal(Grid.GetRow(status), Grid.GetRow(label));
    }

    [AvaloniaFact]
    public void TheSettingsHideThePlanWhileItIsUnknown()
    {
        using var file = new TemporaryFile();
        var window = Settings(file, plan: null);

        Assert.False(window.FindControl<TextBlock>("AccountPlanStatus")!.IsVisible);
        Assert.False(window.FindControl<TextBlock>("AccountPlanLabel")!.IsVisible);
    }

    private static SettingsWindow Settings(TemporaryFile file, SubscriptionPlan? plan) =>
        new(new SettingsStore(file.Path), new AppSettings(),
            applyAutostart: _ => { },
            readClaudeCodeToken: () => Task.FromResult<AccessToken?>(null),
            plan: plan);

    private static UsageState State(SubscriptionPlan? plan, bool withExtraUsage = false) => new()
    {
        Kind = UsageStateKind.Ready,
        Snapshot = new UsageSnapshot
        {
            Session = new UsageWindow(6, Now.AddHours(3)),
            Weekly = new UsageWindow(18, Now.AddDays(3)),
            ExtraUsage = withExtraUsage ? new ExtraUsage(true, 12m, 50m, 24d, "EUR", 2) : null,
            Plan = plan,
            RetrievedAt = Now,
            TokenSource = TokenSource.OAuth
        }
    };

    private sealed class TemporaryFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cuc-plan-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
