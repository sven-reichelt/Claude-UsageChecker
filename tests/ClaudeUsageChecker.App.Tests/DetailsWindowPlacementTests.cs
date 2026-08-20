using Avalonia.Headless.XUnit;
using ClaudeUsageChecker.App.Views;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models;
using ClaudeUsageChecker.Core.Services;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>
/// Checks that the details window stays centred when its content grows.
/// </summary>
/// <remarks>
/// <para>
/// The window is created once and reused, so <c>CenterScreen</c> only ever
/// takes effect the first time it opens. The update notice, on the other hand,
/// arrives from a network call some seconds later and makes the window a good
/// hundred pixels taller - and a window that sizes itself to its content grows
/// downwards, from a top edge that was worked out for the smaller height. Its
/// middle therefore sits half the notice below the middle of the screen.
/// </para>
/// <para>
/// Noticed on a real desktop, not by a test: the window simply sat too low
/// whenever an update was on offer. It is the same trap as with the settings
/// window - a position worked out once and never revisited - only here it
/// merely looks wrong instead of hanging over the edge.
/// </para>
/// </remarks>
public class DetailsWindowPlacementTests
{
    [AvaloniaFact]
    public void TheWindowStaysCentredWhenTheUpdateNoticeAppears()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.Show();
        Settle();

        var screen = window.Screens.ScreenFromWindow(window)!;
        var before = Middle(window, screen.Scaling);

        window.SetUpdateNotice("0.6.2", new Uri("https://example.invalid/r"), canInstall: true);
        Settle();

        var after = Middle(window, screen.Scaling);

        window.Hide();

        Assert.True(Math.Abs(after - before) <= 2,
            $"The middle of the window moved from {before} to {after} when the notice appeared.");
    }

    /// <summary>
    /// The window is centred in the working area, not merely somewhere on it.
    /// </summary>
    [AvaloniaFact]
    public void TheWindowSitsInTheMiddleWithANotice()
    {
        var window = new DetailsWindow();
        window.Render(ReadyState());
        window.SetUpdateNotice("0.6.2", new Uri("https://example.invalid/r"), canInstall: true);
        window.Show();
        Settle();

        var screen = window.Screens.ScreenFromWindow(window)!;
        var wanted = screen.WorkingArea.Y + screen.WorkingArea.Height / 2;
        var actual = Middle(window, screen.Scaling);

        window.Hide();

        Assert.True(Math.Abs(actual - wanted) <= 2,
            $"The middle of the window is at {actual}, the middle of the working area at {wanted}.");
    }

    /// <summary>
    /// Lets the queued layout run - without it the window is measured before it
    /// has grown, and the test passes while the defect stands.
    /// </summary>
    private static void Settle()
    {
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    private static int Middle(DetailsWindow window, double scaling) =>
        window.Position.Y
        + (int)Math.Round((window.FrameSize ?? window.Bounds.Size).Height * scaling / 2);

    private static UsageState ReadyState()
    {
        var now = DateTimeOffset.UtcNow;

        return new UsageState
        {
            Kind = UsageStateKind.Ready,
            Snapshot = new UsageSnapshot
            {
                Session = new UsageWindow(9, now.AddHours(4)),
                Weekly = new UsageWindow(23, now.AddDays(2)),
                ScopedWeekly = [new ScopedUsageWindow("Fable", new UsageWindow(2, now.AddDays(2)))],
                RetrievedAt = now,
                TokenSource = TokenSource.OAuth
            }
        };
    }
}
