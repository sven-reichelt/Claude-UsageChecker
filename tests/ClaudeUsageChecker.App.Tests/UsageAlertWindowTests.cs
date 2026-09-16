using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ClaudeUsageChecker.App.Settings;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Localization;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// The window that tells the user a limit has reached yellow, red, or its end.
/// </summary>
public class UsageAlertWindowTests : IDisposable
{
    private static readonly UsageAlertBehaviour Waiting = new(true, true, TimeSpan.FromSeconds(30));
    private static readonly UsageAlertBehaviour Passing = new(false, false, TimeSpan.FromSeconds(30));

    private readonly Language _before = Localizer.Current.Language;

    public UsageAlertWindowTests() => Localizer.Use(Language.Default);

    public void Dispose()
    {
        Localizer.Use(_before);
        GC.SuppressFinalize(this);
    }

    /// <summary>A limit going from yellow to red is one piece of news, not two.</summary>
    [AvaloniaFact]
    public void ANoticeAboutTheSameLimitReplacesTheEarlierOne()
    {
        var window = new UsageAlertWindow(Waiting);

        window.Add([Alert(UsageAlertLimit.Session, UsageAlertLevel.Warning, 76)]);
        window.Add([Alert(UsageAlertLimit.Session, UsageAlertLevel.Critical, 91)]);

        var alert = Assert.Single(window.Alerts);
        Assert.Equal(UsageAlertLevel.Critical, alert.Level);
        Assert.Single(window.FindControl<StackPanel>("EntriesPanel")!.Children);
    }

    [AvaloniaFact]
    public void DifferentLimitsJoinTheSameNotice()
    {
        var window = new UsageAlertWindow(Waiting);

        window.Add([Alert(UsageAlertLimit.Weekly, UsageAlertLevel.Warning, 80)]);
        window.Add([Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100)]);

        Assert.Equal(2, window.FindControl<StackPanel>("EntriesPanel")!.Children.Count);
        // In the order the limits appear everywhere else: session first.
        Assert.Equal(UsageAlertLimit.Session, window.Alerts[0].Limit);
    }

    [AvaloniaFact]
    public void TheHeadingFollowsTheHighestStage()
    {
        var window = new UsageAlertWindow(Waiting);

        window.Add(
        [
            Alert(UsageAlertLimit.Weekly, UsageAlertLevel.Warning, 80),
            Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100)
        ]);

        Assert.Equal(T.AlertHeadingExhausted, window.FindControl<TextBlock>("HeadingText")!.Text);
    }

    /// <summary>
    /// The entry says when the limit resets - the one thing the user needs to
    /// plan around.
    /// </summary>
    [AvaloniaFact]
    public void EveryEntrySaysWhenTheLimitResets()
    {
        var window = new UsageAlertWindow(Waiting);
        var alert = Alert(UsageAlertLimit.Session, UsageAlertLevel.Critical, 92);

        window.Add([alert]);

        var texts = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains(texts, t => t is not null
            && t.Contains(Core.Formatting.DurationFormatter.ToResetMoment(alert.Window.ResetsAt, DateTimeOffset.Now),
                StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void ConfirmingClosesTheNotice()
    {
        var window = new UsageAlertWindow(Waiting);
        window.Add([Alert(UsageAlertLimit.Session, UsageAlertLevel.Warning, 76)]);
        window.Show();

        window.FindControl<Button>("AcknowledgeButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(window.IsVisible);
        Assert.False(window.WaitsForConfirmation);
    }

    /// <summary>
    /// Where confirming was asked for, closing it any other way is turned down -
    /// but a shutdown is never held up by a notice.
    /// </summary>
    [Theory]
    [InlineData(true, false, WindowCloseReason.WindowClosing, true)]
    [InlineData(false, false, WindowCloseReason.WindowClosing, false)]
    [InlineData(true, true, WindowCloseReason.WindowClosing, false)]
    [InlineData(true, false, WindowCloseReason.ApplicationShutdown, false)]
    [InlineData(true, false, WindowCloseReason.OSShutdown, false)]
    public void OnlyTheUserIsKeptFromClosingANoticeThatWaits(
        bool waits, bool isProgrammatic, WindowCloseReason reason, bool refused)
    {
        Assert.Equal(refused, UsageAlertWindow.RefusesToClose(waits, isProgrammatic, reason));
    }

    [AvaloniaFact]
    public void ANoticeThatWaitsStaysInFront()
    {
        var window = new UsageAlertWindow(Waiting);

        window.Present();

        Assert.True(window.Topmost);
        window.FindControl<Button>("AcknowledgeButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    [AvaloniaFact]
    public void ANoticeNotMeantToStayInFrontLetsGoOfIt()
    {
        var window = new UsageAlertWindow(Passing);

        window.Present();

        Assert.False(window.Topmost);
        window.Close();
    }

    /// <summary>
    /// The notice must not take the keyboard: an Enter meant for another window
    /// would otherwise confirm it unread.
    /// </summary>
    [AvaloniaFact]
    public void TheNoticeDoesNotTakeTheKeyboard()
    {
        var window = new UsageAlertWindow(Waiting);

        Assert.False(window.ShowActivated);
        Assert.False(window.FindControl<Button>("AcknowledgeButton")!.IsDefault);
    }

    [AvaloniaFact]
    public void OnlyANoticeThatClosesByItselfSaysSo()
    {
        var waiting = new UsageAlertWindow(Waiting);
        var passing = new UsageAlertWindow(Passing);

        Assert.False(waiting.FindControl<TextBlock>("AutoCloseText")!.IsVisible);
        Assert.True(passing.FindControl<TextBlock>("AutoCloseText")!.IsVisible);
    }

    [AvaloniaFact]
    public void ANoticeThatDoesNotWaitClosesByItself()
    {
        var window = new UsageAlertWindow(new UsageAlertBehaviour(false, false, TimeSpan.FromMilliseconds(200)));
        window.Add([Alert(UsageAlertLimit.Session, UsageAlertLevel.Warning, 76)]);
        window.Show();
        Assert.True(window.IsCountingDown, "the countdown did not start");

        // Timers fire only inside the dispatcher's main loop - RunJobs runs the
        // queue and nothing else, so it would wait here forever. The loop is left
        // as soon as the window closes, and after five seconds at the latest.
        using var giveUp = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        window.Closed += (_, _) => giveUp.Cancel();
        Dispatcher.UIThread.MainLoop(giveUp.Token);

        Assert.False(window.IsVisible);
    }

    /// <summary>
    /// Whoever comes back after the session has started over should not have to
    /// confirm that it is used up.
    /// </summary>
    [AvaloniaFact]
    public void AnEntryWhoseLimitHasResetIsDropped()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var window = new UsageAlertWindow(Waiting, timeProvider: clock);
        var session = Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100);
        var weekly = Alert(UsageAlertLimit.Weekly, UsageAlertLevel.Warning, 80);
        window.Add([session, weekly]);
        window.Show();

        clock.Now = session.Window.ResetsAt.AddSeconds(1);
        window.Tick();

        Assert.True(window.IsVisible);
        Assert.Equal(UsageAlertLimit.Weekly, Assert.Single(window.Alerts).Limit);
        Assert.Equal(T.AlertHeadingWarning, window.FindControl<TextBlock>("HeadingText")!.Text);

        window.Close();
    }

    /// <summary>With nothing left to confirm, a notice that waits closes as well.</summary>
    [AvaloniaFact]
    public void ANoticeWithEveryLimitResetClosesEvenWhereItWaits()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var window = new UsageAlertWindow(Waiting, timeProvider: clock);
        var session = Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100);
        window.Add([session]);
        window.Show();

        clock.Now = session.Window.ResetsAt.AddSeconds(1);
        window.Tick();

        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void BeforeTheResetNothingIsDropped()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var window = new UsageAlertWindow(Waiting, timeProvider: clock);
        var session = Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100);
        window.Add([session]);
        window.Show();

        clock.Now = session.Window.ResetsAt.AddSeconds(-1);
        window.Tick();

        Assert.True(window.IsVisible);
        Assert.Single(window.Alerts);

        window.FindControl<Button>("AcknowledgeButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    [AvaloniaFact]
    public void OnlyThePreviewCallsItselfOne()
    {
        var real = new UsageAlertWindow(Waiting);
        var preview = new UsageAlertWindow(Waiting, isPreview: true);

        Assert.False(real.FindControl<TextBlock>("PreviewText")!.IsVisible);
        Assert.True(preview.FindControl<TextBlock>("PreviewText")!.IsVisible);
    }

    internal static UsageAlert Alert(UsageAlertLimit limit, UsageAlertLevel level, double utilization, string? model = null)
    {
        var now = DateTimeOffset.Now;
        var reset = limit == UsageAlertLimit.Session ? now.AddHours(2).AddMinutes(14) : now.AddDays(3).AddHours(5);
        var threshold = level switch
        {
            UsageAlertLevel.Exhausted => 100d,
            UsageAlertLevel.Critical => 90d,
            _ => 75d
        };

        return new UsageAlert(limit, model, level, new UsageWindow(utilization, reset), threshold);
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>One entry at every stage, the way the preview in the settings fills it.</summary>
    internal static UsageAlertWindow Filled(bool waits = true)
    {
        var window = new UsageAlertWindow(waits ? Waiting : Passing);
        window.Add(
        [
            Alert(UsageAlertLimit.Session, UsageAlertLevel.Exhausted, 100),
            Alert(UsageAlertLimit.Weekly, UsageAlertLevel.Critical, 92),
            Alert(UsageAlertLimit.WeeklyModel, UsageAlertLevel.Warning, 81, "Fable")
        ]);
        return window;
    }
}
